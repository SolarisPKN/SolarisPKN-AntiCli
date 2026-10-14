using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli;

public partial class SessionView
{
    private readonly List<FlowNode> _flowNodes = [];
    private readonly List<FlowEdge> _flowEdges = [];
    private readonly Dictionary<string, Border> _flowBorders = [];
    private string? _flowSelected;
    private string? _flowFrom;
    private FlowEdgeKind? _flowConnecting;
    private bool _flowEdited, _flowSyncing;
    private int _flowSerial;
    private string? _flowDragging;
    private Point _flowPointerStart, _flowNodeStart;

    private AutomationGraph CurrentFlowGraph() =>
        new(1, _flowNodes.ToArray(), _flowEdges.ToArray());

    private void RefreshFlowDiagramFromAutomation()
    {
        if (_flowEdited || _flowSyncing || FlowCanvas is null) return;
        _flowNodes.Clear(); _flowEdges.Clear();
        for (var i = 0; i < _automationSteps.Count; i++)
        {
            var id = "auto-" + (i + 1);
            _flowNodes.Add(new FlowNode(id, FlowNodeKind.Command,
                "Paso " + (i + 1), _automationSteps[i],
                40 + i % 3 * 330, 60 + i / 3 * 155));
            if (i > 0) _flowEdges.Add(new FlowEdge("auto-" + i, id));
        }
        RenderFlowDiagram();
    }

    private bool PrepareFlowForScript(out string issue)
    {
        issue = "";
        if (!_flowEdited) return true;
        var graph = CurrentFlowGraph();
        try { AutomationGraphFile.Validate(graph); }
        catch (Exception ex) { issue = ex.Message; return false; }
        if (graph.Nodes.Count == 0 || !AutomationGraphFile.CanExportAsLinearScript(graph) ||
            graph.Edges.Count != graph.Nodes.Count - 1)
        {
            issue = "Este diagrama tiene IF, lógica, paralelismo o nodos desconectados. " +
                "Podés guardar su JSON, pero no exportar una secuencia .sh incorrecta.";
            return false;
        }
        var roots = graph.Nodes.Where(n => !graph.Edges.Any(e => e.ToId == n.Id)).ToArray();
        if (roots.Length != 1)
        {
            issue = "Necesitás una única cadena de comandos conectados.";
            return false;
        }
        var ordered = new List<string>();
        var node = roots[0];
        while (true)
        {
            ordered.Add(node.CommandTemplate);
            var edge = graph.Edges.FirstOrDefault(e => e.FromId == node.Id);
            if (edge is null) break;
            node = graph.Nodes.First(n => n.Id == edge.ToId);
        }
        if (ordered.Count != graph.Nodes.Count)
        {
            issue = "Hay comandos sueltos; unilos con flechas.";
            return false;
        }
        _flowSyncing = true;
        try
        {
            _automationSteps.Clear();
            _automationSteps.AddRange(ordered);
            RefreshAutomationSteps();
        }
        finally { _flowSyncing = false; }
        return true;
    }

    private void FlowAddCommandClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Command);
    private void FlowAddLoopClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Loop);
    private void FlowAddIfClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.If);
    private void FlowAddAndClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.And);
    private void FlowAddOrClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Or);
    private void FlowAddXorClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Xor);
    private void FlowAddParallelClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Parallel);
    private void FlowAddJoinClicked(object? s, RoutedEventArgs e) => AddFlow(FlowNodeKind.Join);

    private void AddFlow(FlowNodeKind kind)
    {
        if (_flowNodes.Count >= 100) { FlowStatus.Text = "Máximo 100 bloques."; return; }
        var command = (AutomationStepInput.Text ?? "").Trim();
        if (command.Length == 0 &&
            AutomationStepsList.SelectedIndex is >= 0 and var index &&
            index < _automationSteps.Count)
            command = _automationSteps[index];
        if (command.Length == 0)
            command = (AssistedCommand.Text ?? "").Trim();
        if (kind == FlowNodeKind.Loop) command = "true"; // FOR does not evaluate this; WHILE/UNTIL do.
        if (command.Length == 0) command = kind == FlowNodeKind.Command
            ? "echo 'configurar paso'" : "test -f /etc/os-release";
        var id = "flow-" + (++_flowSerial) + "-" + Guid.NewGuid().ToString("N")[..6];
        _flowNodes.Add(new FlowNode(id, kind, kind + " " + _flowSerial,
            command, 35 + _flowSerial % 4 * 265, 50 + _flowSerial / 4 % 5 * 150));
        _flowEdited = true;
        RenderFlowDiagram();
        SelectFlow(id);
    }

    private void FlowConnectNextClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.Next);
    private void FlowConnectBodyClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.LoopBody);
    private void FlowConnectBackClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.LoopBack);
    private void FlowConnectTrueClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.True);
    private void FlowConnectFalseClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.False);
    private void FlowConnectParallelClicked(object? s, RoutedEventArgs e) => ConnectFlow(FlowEdgeKind.Parallel);

    private void ConnectFlow(FlowEdgeKind kind)
    {
        _flowConnecting = kind;
        _flowFrom = null;
        FlowStatus.Text = "Flecha «" + kind + "»: tocá origen y destino.";
    }

    private void SelectFlow(string id)
    {
        _flowSelected = id;
        var node = _flowNodes.FirstOrDefault(n => n.Id == id);
        if (node is null) return;
        FlowNodeTitle.Text = node.Label;
        FlowNodeCommand.Text = node.CommandTemplate;
        FlowLoopSettings.IsVisible = node.Kind == FlowNodeKind.Loop;
        if (node.Kind == FlowNodeKind.Loop)
        {
            FlowLoopModeSelector.SelectedIndex = node.LoopMode switch
            {
                FlowLoopMode.For => 0,
                FlowLoopMode.While => 1,
                _ => 2
            };
            FlowLoopMaxCount.Text = node.MaxIterations.ToString();
        }
        HighlightFlowSelection();
    }

    private void FlowNodePressed(FlowNode node, Border border, PointerPressedEventArgs e)
    {
        if (_flowConnecting is { } kind)
        {
            if (_flowFrom is null)
            {
                _flowFrom = node.Id;
                FlowStatus.Text = "Ahora elegí el bloque de destino.";
            }
            else
            {
                var edge = new FlowEdge(_flowFrom, node.Id, kind);
                try
                {
                    AutomationGraphFile.Validate(new AutomationGraph(1,
                        _flowNodes.ToArray(), _flowEdges.Append(edge).ToArray()));
                    _flowEdges.Add(edge); _flowEdited = true;
                    RenderFlowDiagram();
                    FlowStatus.Text = "Flecha añadida: " + kind;
                }
                catch (Exception ex) { FlowStatus.Text = ex.Message; }
                _flowConnecting = null; _flowFrom = null;
            }
            e.Handled = true; return;
        }
        SelectFlow(node.Id);
        _flowDragging = node.Id;
        _flowPointerStart = e.GetPosition(FlowCanvas);
        _flowNodeStart = new Point(node.X, node.Y);
        e.Pointer.Capture(border);
        e.Handled = true;
    }

    private void FlowNodeMoved(Border border, PointerEventArgs e)
    {
        if (_flowDragging is null) return;
        var p = e.GetPosition(FlowCanvas);
        Canvas.SetLeft(border, Math.Clamp(_flowNodeStart.X + p.X - _flowPointerStart.X, 0, 4000));
        Canvas.SetTop(border, Math.Clamp(_flowNodeStart.Y + p.Y - _flowPointerStart.Y, 0, 4000));
        FlowCanvas.Width = Math.Max(1200, Canvas.GetLeft(border) + 260);
        FlowCanvas.Height = Math.Max(900, Canvas.GetTop(border) + 160);
    }

    private void FlowNodeReleased(Border border, PointerReleasedEventArgs e)
    {
        if (_flowDragging is null) return;
        var idx = _flowNodes.FindIndex(n => n.Id == _flowDragging);
        if (idx >= 0)
            _flowNodes[idx] = _flowNodes[idx] with
            {
                X = Canvas.GetLeft(border), Y = Canvas.GetTop(border)
            };
        _flowDragging = null; _flowEdited = true;
        e.Pointer.Capture(null);
        RenderFlowDiagram();
    }

    private void FlowUpdateNodeClicked(object? s, RoutedEventArgs e)
    {
        var idx = _flowNodes.FindIndex(n => n.Id == _flowSelected);
        if (idx < 0) return;
        var label = (FlowNodeTitle.Text ?? "").Trim();
        var command = (FlowNodeCommand.Text ?? "").Trim();
        if (label.Length is < 1 or > 120 || command.Length > 3000 ||
            command.Any(c => c is '\n' or '\r' or '\0'))
        {
            FlowStatus.Text = "Título o plantilla inválida."; return;
        }
        var existing = _flowNodes[idx];
        var loopMode = FlowLoopMode.For;
        var limit = existing.MaxIterations;
        if (existing.Kind == FlowNodeKind.Loop)
        {
            loopMode = FlowLoopModeSelector.SelectedIndex switch
            {
                0 => FlowLoopMode.For,
                1 => FlowLoopMode.While,
                2 => FlowLoopMode.Until,
                _ => throw new ArgumentException("Modo de bucle inválido.")
            };
            if (!int.TryParse(FlowLoopMaxCount.Text, out limit) || limit is < 1 or > 1000)
            {
                FlowStatus.Text = "El límite de repeticiones debe estar entre 1 y 1000.";
                return;
            }
            if (loopMode is FlowLoopMode.While or FlowLoopMode.Until &&
                string.IsNullOrWhiteSpace(command))
            {
                FlowStatus.Text = "WHILE y UNTIL necesitan un comando de condición.";
                return;
            }
        }
        var updated = existing with { Label = label, CommandTemplate = command,
            LoopMode = loopMode, MaxIterations = limit };
        var proposed = _flowNodes.ToArray();
        proposed[idx] = updated;
        try { AutomationGraphFile.Validate(new AutomationGraph(1, proposed, _flowEdges.ToArray())); }
        catch (Exception ex) { FlowStatus.Text = "No se puede guardar el nodo: " + ex.Message; return; }
        _flowNodes[idx] = updated;
        _flowEdited = true;
        RenderFlowDiagram();
        FlowStatus.Text = "Nodo actualizado.";
    }

    private void FlowRemoveClicked(object? s, RoutedEventArgs e)
    {
        _flowNodes.RemoveAll(n => n.Id == _flowSelected);
        _flowEdges.RemoveAll(x => x.FromId == _flowSelected || x.ToId == _flowSelected);
        _flowSelected = null; _flowEdited = true;
        FlowLoopSettings.IsVisible = false;
        RenderFlowDiagram();
    }
}
