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
    IReadOnlyList<FlowNode> Nodes, IReadOnlyList<FlowEdge> Edges,
    string? StartNodeId = null);

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
            !ids.Contains(e.ToId)))
            throw new InvalidDataException("Una flecha conecta nodos inexistentes o conecta el nodo consigo mismo.");
        if (graph.Edges.Distinct().Count() != graph.Edges.Count)
            throw new InvalidDataException("Hay conexiones duplicadas.");
        var byId = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        if (graph.StartNodeId is not null && !ids.Contains(graph.StartNodeId))
            throw new InvalidDataException("El nodo inicial elegido no existe.");
        if (graph.Nodes.Any(n => n.Kind == FlowNodeKind.Loop &&
            (n.MaxIterations is < 0 or > 1000 ||
             (n.LoopMode == FlowLoopMode.For && n.MaxIterations == 0) ||
             !Enum.IsDefined(n.LoopMode))))
            throw new InvalidDataException("Un bucle necesita un tipo válido y un máximo entre 1 y 1000 iteraciones.");
        if (graph.Edges.Any(e => e.Kind == FlowEdgeKind.LoopBack &&
            (byId[e.ToId].Kind != FlowNodeKind.Loop ||
             byId[e.FromId].Kind != FlowNodeKind.Command)) ||
            graph.Edges.Any(e => e.Kind == FlowEdgeKind.LoopBody &&
                byId[e.FromId].Kind != FlowNodeKind.Loop))
            throw new InvalidDataException("El retorno debe llegar a un bloque Bucle y el cuerpo debe salir de ese bloque.");
        // Cycles are allowed in the visual graph. IF true/false exits and arbitrary
        // back edges are interpreted by the script router; topology checking belongs
        // at export time, not while an administrator is still drawing arrows.
        // No implicit maximum for a conditional cycle: the flow may wait forever.

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
