using System.Text;
using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

/// <summary>
/// Deterministic flowchart -> POSIX shell state machine. True/False edges,
/// backward jumps and intentional unbounded retry cycles work without a
/// synthetic Loop node. This exporter NEVER executes anything on a server.
/// </summary>
public static class AutomationGraphRouter
{
    private static readonly Regex Variable = new(
        @"\{\{(?<key>[A-Za-z_][A-Za-z0-9_]{0,39})\}\}", RegexOptions.Compiled);

    public static string Generate(AutomationWorkflow workflow, AutomationGraph graph)
    {
        AutomationWorkflowFile.Validate(workflow);
        AutomationGraphFile.Validate(graph);
        if (graph.Nodes.Count == 0)
            throw new InvalidDataException("Agregá al menos un nodo.");
        if (graph.Nodes.Any(n => n.Kind is not
            (FlowNodeKind.Command or FlowNodeKind.If or FlowNodeKind.Loop)))
            throw new NotSupportedException(
                "AND/OR/XOR y paralelo necesitan un ejecutor específico; no se simulan como secuencias.");
        if (graph.Edges.Any(e => e.Kind == FlowEdgeKind.Parallel))
            throw new NotSupportedException("Las flechas paralelas aún no son ejecutables.");
        if (workflow.Order == AutomationOrder.PerStep && workflow.Rows.Count > 1)
            throw new NotSupportedException(
                "Los ciclos se ejecutan por fila. Desactivá «Primero un paso para todas las filas».");

        var nodes = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var incoming = graph.Edges.GroupBy(e => e.ToId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var roots = graph.Nodes.Where(n => !incoming.ContainsKey(n.Id)).ToArray();
        var start = graph.StartNodeId ??
            (roots.Length == 1 ? roots[0].Id : graph.Nodes[0].Id);

        // An administrator can draw incomplete graphs. Only connected, unambiguous
        // graphs become scripts. No cap is silently inserted on conditional cycles.
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var edge in graph.Edges.Where(e => e.FromId == id))
                if (seen.Add(edge.ToId)) queue.Enqueue(edge.ToId);
        }
        if (seen.Count != graph.Nodes.Count)
            throw new InvalidDataException(
                "Hay nodos inaccesibles desde Inicio. Conectalos o marcá otro bloque inicial.");

        FlowEdge? Next(FlowNode node, FlowEdgeKind kind)
        {
            var edges = graph.Edges.Where(e => e.FromId == node.Id && e.Kind == kind).ToArray();
            if (edges.Length > 1)
                throw new InvalidDataException(
                    "Más de una salida " + kind + " del nodo " + node.Label);
            return edges.FirstOrDefault();
        }

        foreach (var node in graph.Nodes)
        {
            var edges = graph.Edges.Where(e => e.FromId == node.Id).ToArray();
            switch (node.Kind)
            {
                case FlowNodeKind.Command:
                    if (edges.Length > 1 ||
                        edges.Any(e => e.Kind is not (FlowEdgeKind.Next or FlowEdgeKind.LoopBack)))
                        throw new InvalidDataException(
                            "Un comando admite una sola salida Siguiente o Retorno: " + node.Label);
                    break;
                case FlowNodeKind.If:
                    if (edges.Any(e => e.Kind is not (FlowEdgeKind.True or FlowEdgeKind.False)) ||
                        Next(node, FlowEdgeKind.True) is null ||
                        edges.Length > 2)
                        throw new InvalidDataException(
                            "IF necesita una salida Verdadero y, opcionalmente, Falso: " + node.Label);
                    break;
                case FlowNodeKind.Loop:
                    if (edges.Any(e => e.Kind is not (FlowEdgeKind.LoopBody or FlowEdgeKind.Next)) ||
                        Next(node, FlowEdgeKind.LoopBody) is null || edges.Length > 2)
                        throw new InvalidDataException(
                            "Bucle necesita un Cuerpo y puede tener un Siguiente: " + node.Label);
                    break;
            }
            if (node.Kind is FlowNodeKind.If or FlowNodeKind.Loop &&
                string.IsNullOrWhiteSpace(node.CommandTemplate))
                throw new InvalidDataException("Condición vacía en " + node.Label);
        }

        // Map graph IDs to numeric, shell-safe state labels, not user text.
        var stateIds = graph.Nodes.Select((n, i) => (n.Id, Key: "s" + i))
            .ToDictionary(x => x.Id, x => x.Key, StringComparer.Ordinal);
        string State(FlowEdge? edge) =>
            GuidedCommandCatalog.ShellQuote(edge is null ? "" : stateIds[edge.ToId]);

        string Substitute(string template, IReadOnlyDictionary<string, string> row)
        {
            var result = Variable.Replace(template, match =>
            {
                var key = match.Groups["key"].Value;
                if (!row.TryGetValue(key, out var value) &&
                    !workflow.Constants.TryGetValue(key, out value))
                    throw new InvalidDataException("Falta variable: " + key);
                return GuidedCommandCatalog.ShellQuote(value!);
            });
            if (result.Contains("{{", StringComparison.Ordinal) ||
                result.Contains("}}", StringComparison.Ordinal))
                throw new InvalidDataException("Variable incompleta en: " + template);
            return result;
        }

        var sb = new StringBuilder("#!/bin/sh\n");
        sb.AppendLine("# AntiCLI · Diagrama con saltos condicionales y ciclos voluntarios");
        sb.AppendLine("# Para detener: interrumpir el proceso (Ctrl+C / SIGINT) o cerrarlo desde el sistema.");
        sb.AppendLine("# No hay máximo de repeticiones implícito. Revisar antes de ejecutar.");
        sb.AppendLine("set -u");
        for (var rowIndex = 0; rowIndex < workflow.Rows.Count; rowIndex++)
        {
            var row = workflow.Rows[rowIndex];
            sb.AppendLine("# --- Fila " + (rowIndex + 1) + " ---");
            foreach (var node in graph.Nodes.Where(n => n.Kind == FlowNodeKind.Loop))
                sb.Append("__ac_count_").Append(stateIds[node.Id]).AppendLine("=0");
            sb.Append("__ac_state=").AppendLine(GuidedCommandCatalog.ShellQuote(stateIds[start]));
            sb.AppendLine("while [ -n \"$__ac_state\" ]; do");
            sb.AppendLine("  case \"$__ac_state\" in");
            foreach (var node in graph.Nodes)
            {
                sb.Append("    ").Append(stateIds[node.Id]).AppendLine(")");
                var cmd = GuidedCommandCatalog.ShellQuote(Substitute(node.CommandTemplate, row));
                if (node.Kind == FlowNodeKind.Command)
                {
                    var edge = graph.Edges.SingleOrDefault(e => e.FromId == node.Id);
                    sb.Append("      if sh -c ").Append(cmd).AppendLine("; then");
                    sb.Append("        __ac_state=").AppendLine(State(edge));
                    sb.AppendLine("      else");
                    if (workflow.StopOnError)
                        sb.AppendLine("        __ac_rc=$?; exit \"$__ac_rc\"");
                    else
                    {
                        sb.Append("        printf '%s\\n' ")
                            .Append(GuidedCommandCatalog.ShellQuote(
                                "[AntiCLI] Falló: " + node.Label))
                            .AppendLine(" >&2");
                        sb.Append("        __ac_state=").AppendLine(State(edge));
                    }
                    sb.AppendLine("      fi");
                }
                else if (node.Kind == FlowNodeKind.If)
                {
                    sb.Append("      if sh -c ").Append(cmd).AppendLine("; then");
                    sb.Append("        __ac_state=")
                        .AppendLine(State(Next(node, FlowEdgeKind.True)));
                    sb.AppendLine("      else");
                    sb.Append("        __ac_state=")
                        .AppendLine(State(Next(node, FlowEdgeKind.False)));
                    sb.AppendLine("      fi");
                }
                else // Optional structured FOR/WHILE/UNTIL: 0 = unlimited for WHILE/UNTIL.
                {
                    var key = "__ac_count_" + stateIds[node.Id];
                    var body = State(Next(node, FlowEdgeKind.LoopBody));
                    var exit = State(Next(node, FlowEdgeKind.Next));
                    if (node.MaxIterations > 0)
                    {
                        sb.Append("      if [ \"$").Append(key).Append("\" -ge ")
                            .Append(node.MaxIterations).AppendLine(" ]; then");
                        sb.Append("        ").Append(key).AppendLine("=0");
                        sb.Append("        __ac_state=").AppendLine(exit);
                        sb.AppendLine("      else");
                    }
                    if (node.LoopMode == FlowLoopMode.For)
                        sb.Append("      ").Append(key).Append("=$((").Append(key)
                            .AppendLine(" + 1))");
                    else
                    {
                        sb.Append("      if sh -c ").Append(cmd).AppendLine("; then");
                        sb.Append("        __ac_state=").AppendLine(
                            node.LoopMode == FlowLoopMode.While ? body : exit);
                        sb.AppendLine("      else");
                        sb.Append("        __ac_state=").AppendLine(
                            node.LoopMode == FlowLoopMode.While ? exit : body);
                        sb.AppendLine("      fi");
                    }
                    if (node.LoopMode == FlowLoopMode.For)
                        sb.Append("      __ac_state=").AppendLine(body);
                    else
                    {
                        sb.Append("      if [ \"$__ac_state\" = ").Append(body)
                            .Append(" ]; then ").Append(key).Append("=$((")
                            .Append(key).AppendLine(" + 1)); else " + key + "=0; fi");
                    }
                    if (node.MaxIterations > 0)
                        sb.AppendLine("      fi");
                }
                sb.AppendLine("      ;;");
            }
            sb.AppendLine("    *) printf '%s\\n' 'AntiCLI: estado desconocido' >&2; exit 2 ;;");
            sb.AppendLine("  esac");
            sb.AppendLine("done");
        }
        if (sb.Length > 350000)
            throw new InvalidDataException("Script demasiado extenso; dividí las filas.");
        return sb.ToString();
    }
}
