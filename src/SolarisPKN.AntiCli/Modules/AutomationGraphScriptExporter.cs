using System.Text;
using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

/// <summary>
/// Exports strict structured command + loop graphs. Anything ambiguous
/// (unstructured cycles, IF branches, parallel forks, nested loops) fails
/// closed instead of silently changing the meaning of the diagram.
/// Export only; never starts an SSH session.
/// </summary>
public static class AutomationGraphScriptExporter
{
    private static readonly Regex Token = new(
        @"\{\{(?<name>[A-Za-z_][A-Za-z0-9_]{0,39})\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private sealed record Instruction(FlowNode Node, IReadOnlyList<FlowNode>? Body = null);

    public static string Generate(AutomationWorkflow workflow, AutomationGraph graph)
    {
        AutomationWorkflowFile.Validate(workflow);
        AutomationGraphFile.Validate(graph);
        if (graph.Nodes.Count == 0)
            throw new InvalidDataException("El diagrama está vacío.");
        if (graph.Nodes.Any(n => n.Kind is not (FlowNodeKind.Command or FlowNodeKind.Loop)) ||
            graph.Edges.Any(e => e.Kind is not (FlowEdgeKind.Next or FlowEdgeKind.LoopBody or FlowEdgeKind.LoopBack)))
            throw new NotSupportedException(
                "La exportación de IF, AND, OR, XOR y paralelismo todavía no está habilitada.");
        if (workflow.Order == AutomationOrder.PerStep && workflow.Rows.Count > 1)
            throw new NotSupportedException(
                "Los bucles del diagrama usan orden por fila. Desactivá «Ejecutar primero un paso para todas las filas».");

        var byId = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var incoming = graph.Edges.Where(e => e.Kind != FlowEdgeKind.LoopBack)
            .GroupBy(e => e.ToId).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var roots = graph.Nodes.Where(n => !incoming.ContainsKey(n.Id)).ToArray();
        if (roots.Length != 1)
            throw new InvalidDataException("El diagrama necesita un único bloque inicial.");

        FlowEdge? UniqueNext(FlowNode node, FlowEdgeKind kind)
        {
            var edges = graph.Edges.Where(e => e.FromId == node.Id && e.Kind == kind).ToArray();
            if (edges.Length > 1)
                throw new InvalidDataException("Más de una flecha «" + kind + "» desde " + node.Label);
            return edges.FirstOrDefault();
        }

        var instructions = new List<Instruction>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var consumedEdges = new HashSet<FlowEdge>();
        var current = roots[0];

        while (true)
        {
            if (!visited.Add(current.Id))
                throw new InvalidDataException("Se detectó un ciclo no estructurado.");
            if (current.Kind == FlowNodeKind.Loop)
            {
                var bodyEdge = UniqueNext(current, FlowEdgeKind.LoopBody)
                    ?? throw new InvalidDataException(
                        "El bucle «" + current.Label + "» necesita una flecha «Cuerpo».");
                consumedEdges.Add(bodyEdge);
                var body = new List<FlowNode>();
                var part = byId[bodyEdge.ToId];
                while (true)
                {
                    if (part.Kind != FlowNodeKind.Command || !visited.Add(part.Id))
                        throw new InvalidDataException(
                            "El cuerpo de un bucle debe ser una cadena de comandos sin bifurcaciones.");
                    body.Add(part);
                    var back = UniqueNext(part, FlowEdgeKind.LoopBack);
                    var next = UniqueNext(part, FlowEdgeKind.Next);
                    if (back is not null)
                    {
                        if (next is not null || back.ToId != current.Id)
                            throw new InvalidDataException(
                                "El último comando del bucle debe volver solo al mismo bloque Bucle.");
                        consumedEdges.Add(back);
                        break;
                    }
                    if (next is null)
                        throw new InvalidDataException(
                            "El cuerpo del bucle no tiene retorno. Conectá «Bucle ↺» al bloque Bucle.");
                    consumedEdges.Add(next);
                    part = byId[next.ToId];
                }
                instructions.Add(new Instruction(current, body));
            }
            else
                instructions.Add(new Instruction(current));

            var continueEdge = UniqueNext(current, FlowEdgeKind.Next);
            if (continueEdge is null) break;
            consumedEdges.Add(continueEdge);
            current = byId[continueEdge.ToId];
        }
        if (visited.Count != graph.Nodes.Count || consumedEdges.Count != graph.Edges.Count)
            throw new InvalidDataException(
                "Hay nodos o flechas desconectadas, o conexiones sin una semántica de ejecución definida.");

        var output = new StringBuilder("#!/bin/sh\n");
        output.AppendLine("# AntiCLI · Diagrama estructurado con bucles limitados");
        output.AppendLine("# Los pasos pueden modificar el servidor. Revisar antes de ejecutar.");
        output.AppendLine(workflow.StopOnError ? "set -eu" : "set -u");

        string Substitute(string template, IReadOnlyDictionary<string, string> row)
        {
            var rendered = Token.Replace(template, m =>
            {
                var key = m.Groups["name"].Value;
                if (!row.TryGetValue(key, out var val) &&
                    !workflow.Constants.TryGetValue(key, out val))
                    throw new InvalidDataException("Variable no definida: " + key);
                return GuidedCommandCatalog.ShellQuote(val!);
            });
            if (rendered.Contains("{{", StringComparison.Ordinal) ||
                rendered.Contains("}}", StringComparison.Ordinal))
                throw new InvalidDataException("Plantilla con variable mal escrita: " + template);
            return rendered;
        }
        void PrintCommand(FlowNode node, IReadOnlyDictionary<string, string> row, string padding)
        {
            var cmd = Substitute(node.CommandTemplate, row);
            if (string.IsNullOrWhiteSpace(cmd))
                throw new InvalidDataException("Comando vacío en " + node.Label);
            output.Append(padding).Append("# ").AppendLine(node.Label.Replace("\n", " "));
            if (workflow.StopOnError)
                output.Append(padding).Append("sh -c ")
                    .AppendLine(GuidedCommandCatalog.ShellQuote(cmd));
            else
                output.Append(padding).Append("{ sh -c ")
                    .Append(GuidedCommandCatalog.ShellQuote(cmd))
                    .Append("; } || printf '%s\\n' ")
                    .Append(GuidedCommandCatalog.ShellQuote(
                        "[AntiCLI] Falló el comando: " + node.Label))
                    .AppendLine(" >&2");
        }

        foreach (var (row, rowIndex) in workflow.Rows.Select((r, i) => (r, i)))
        {
            output.AppendLine("# -------- Fila " + (rowIndex + 1) + " --------");
            var loopNumber = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Body is null)
                {
                    PrintCommand(instruction.Node, row, "");
                    continue;
                }
                var node = instruction.Node;
                var counter = "__anticli_loop_" + rowIndex + "_" + loopNumber++;
                output.Append("# Bucle ").Append(node.LoopMode).Append(" · máximo ")
                    .Append(node.MaxIterations).AppendLine(" iteraciones");
                output.Append(counter).AppendLine("=0");
                output.Append("while [ \"$").Append(counter).Append("\" -lt ")
                    .Append(node.MaxIterations).AppendLine(" ]; do");
                output.Append("  ").Append(counter).Append("=$((").Append(counter)
                    .AppendLine(" + 1))");
                if (node.LoopMode is FlowLoopMode.While or FlowLoopMode.Until)
                {
                    var condition = Substitute(node.CommandTemplate, row);
                    if (string.IsNullOrWhiteSpace(condition))
                        throw new InvalidDataException(
                            "Los bucles WHILE y UNTIL requieren un comando de condición.");
                    output.Append("  if sh -c ").Append(GuidedCommandCatalog.ShellQuote(condition))
                        .AppendLine(node.LoopMode == FlowLoopMode.Until
                            ? "; then break; fi" : "; then :; else break; fi");
                }
                foreach (var bodyNode in instruction.Body)
                    PrintCommand(bodyNode, row, "  ");
                output.AppendLine("done");
            }
        }

        if (output.Length > 350000)
            throw new InvalidDataException("El script supera 350 KB; dividí el flujo.");
        return output.ToString();
    }
}
