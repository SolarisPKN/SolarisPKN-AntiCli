using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarisPKN.AntiCli.Models;

/// <summary>Data-only visual workflow; coordinates and connectors are not shell code.</summary>
public enum FlowNodeKind { Command, If, And, Or, Xor, Parallel, Join, Loop }
public enum FlowLoopMode { For, While, Until }
public enum FlowEdgeKind { Next, True, False, Parallel, LoopBody, LoopBack }

public sealed record FlowNode(
    string Id, FlowNodeKind Kind, string Label, string CommandTemplate, double X, double Y,
    FlowLoopMode LoopMode = FlowLoopMode.For, int MaxIterations = 20);
public sealed record FlowEdge(string FromId, string ToId, FlowEdgeKind Kind = FlowEdgeKind.Next);

public sealed record AutomationGraph(int SchemaVersion,
    IReadOnlyList<FlowNode> Nodes, IReadOnlyList<FlowEdge> Edges);

public static class AutomationGraphFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(AutomationGraph graph)
    {
        Validate(graph);
        return JsonSerializer.Serialize(graph, Options);
    }

    public static AutomationGraph Parse(string json)
    {
        if (json.Length > 250000) throw new InvalidDataException("Diagrama demasiado grande.");
        var graph = JsonSerializer.Deserialize<AutomationGraph>(json, Options)
            ?? throw new InvalidDataException("Diagrama JSON vacío.");
        Validate(graph);
        return graph;
    }

    public static void Validate(AutomationGraph graph)
    {
        if (graph.SchemaVersion != 1 || graph.Nodes.Count > 100 || graph.Edges.Count > 200)
            throw new InvalidDataException("Diagrama versión 1: máximo 100 nodos y 200 flechas.");
        var ids = graph.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != graph.Nodes.Count || graph.Nodes.Any(n =>
            string.IsNullOrWhiteSpace(n.Id) || n.Id.Length > 80 ||
            string.IsNullOrWhiteSpace(n.Label) || n.Label.Length > 120 ||
            n.CommandTemplate.Length > 3000 ||
            n.CommandTemplate.Any(c => c is '\n' or '\r' or '\0') ||
            !double.IsFinite(n.X) || !double.IsFinite(n.Y) ||
            n.X is < 0 or > 4000 || n.Y is < 0 or > 4000))
            throw new InvalidDataException("Un nodo tiene nombre, posición o comando inválido.");
        if (graph.Edges.Any(e => !ids.Contains(e.FromId) ||
            !ids.Contains(e.ToId) || e.FromId == e.ToId))
            throw new InvalidDataException("Una flecha conecta nodos inexistentes o conecta el nodo consigo mismo.");
        if (graph.Edges.Distinct().Count() != graph.Edges.Count)
            throw new InvalidDataException("Hay conexiones duplicadas.");
        var byId = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        if (graph.Nodes.Any(n => n.Kind == FlowNodeKind.Loop &&
            (n.MaxIterations is < 1 or > 1000 || !Enum.IsDefined(n.LoopMode))))
            throw new InvalidDataException("Un bucle necesita un tipo válido y un máximo entre 1 y 1000 iteraciones.");
        if (graph.Edges.Any(e => e.Kind == FlowEdgeKind.LoopBack &&
            (byId[e.ToId].Kind != FlowNodeKind.Loop ||
             byId[e.FromId].Kind != FlowNodeKind.Command)) ||
            graph.Edges.Any(e => e.Kind == FlowEdgeKind.LoopBody &&
                byId[e.FromId].Kind != FlowNodeKind.Loop))
            throw new InvalidDataException("El retorno debe llegar a un bloque Bucle y el cuerpo debe salir de ese bloque.");
        // Explicit LoopBack edges are controlled cycles; all other cycles are rejected.
        // A loop's return is validated for structured shell export separately.
        var incoming = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in graph.Edges.Where(e => e.Kind != FlowEdgeKind.LoopBack))
            incoming[edge.ToId]++;
        var open = new Queue<string>(incoming.Where(x => x.Value == 0).Select(x => x.Key));
        var visited = 0;
        while (open.Count > 0)
        {
            var id = open.Dequeue();
            visited++;
            foreach (var edge in graph.Edges.Where(e =>
                e.FromId == id && e.Kind != FlowEdgeKind.LoopBack))
            {
                incoming[edge.ToId]--;
                if (incoming[edge.ToId] == 0) open.Enqueue(edge.ToId);
            }
        }
        if (visited != graph.Nodes.Count)
            throw new InvalidDataException("Hay un ciclo sin retorno explícito Bucle ↺. Usá un bloque Bucle y la flecha de retorno.");
    }

    public static bool CanExportAsLinearScript(AutomationGraph graph)
    {
        if (graph.Nodes.Count == 0) return true;
        if (graph.Nodes.Any(n => n.Kind != FlowNodeKind.Command) ||
            graph.Edges.Any(e => e.Kind != FlowEdgeKind.Next))
            return false;
        if (graph.Edges.GroupBy(e => e.FromId).Any(g => g.Count() > 1) ||
            graph.Edges.GroupBy(e => e.ToId).Any(g => g.Count() > 1))
            return false;
        return true;
    }
}
