using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarisPKN.AntiCli.Models;

/// <summary>Data-only visual workflow; coordinates and connectors are not shell code.</summary>
public enum FlowNodeKind { Command, If, And, Or, Xor, Parallel, Join }
public enum FlowEdgeKind { Next, True, False, Parallel }

public sealed record FlowNode(
    string Id, FlowNodeKind Kind, string Label, string CommandTemplate, double X, double Y);
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
        // Cycle detection; branching and loops must not accidentally deadlock.
        var incoming = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in graph.Edges) incoming[edge.ToId]++;
        var open = new Queue<string>(incoming.Where(x => x.Value == 0).Select(x => x.Key));
        var visited = 0;
        while (open.Count > 0)
        {
            var id = open.Dequeue();
            visited++;
            foreach (var edge in graph.Edges.Where(e => e.FromId == id))
            {
                incoming[edge.ToId]--;
                if (incoming[edge.ToId] == 0) open.Enqueue(edge.ToId);
            }
        }
        if (visited != graph.Nodes.Count)
            throw new InvalidDataException("El diagrama contiene ciclos. Los bucles deben ser explícitos en futuras versiones.");
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
