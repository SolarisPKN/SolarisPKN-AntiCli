using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli;

/// <summary>Graph visual renderer uses a scrollable Avalonia Canvas, never SSH.</summary>
public partial class SessionView
{
    private void HighlightFlowSelection()
    {
        foreach (var kv in _flowBorders)
            kv.Value.BorderBrush = kv.Key == _flowSelected
                ? Brushes.Cyan : Brushes.LightGray;
    }

    private void FlowLine(Point first, Point second, IBrush color)
    {
        FlowCanvas.Children.Add(new Line
        {
            StartPoint = first, EndPoint = second, Stroke = color,
            StrokeThickness = 2.5, IsHitTestVisible = false
        });
    }

    private void DrawFlowEdge(FlowNode a, FlowNode b, FlowEdgeKind kind)
    {
        const double w = 190, h = 84;
        var forward = b.X >= a.X;
        var start = new Point(a.X + (forward ? w : 0), a.Y + h / 2);
        var end = new Point(b.X + (forward ? 0 : w), b.Y + h / 2);
        var color = kind switch
        {
            FlowEdgeKind.True => Brushes.LightGreen,
            FlowEdgeKind.False => Brushes.OrangeRed,
            FlowEdgeKind.Parallel => Brushes.Plum,
            FlowEdgeKind.LoopBody => Brushes.LightSkyBlue,
            FlowEdgeKind.LoopBack => Brushes.Orange,
            _ => Brushes.LightSlateGray
        };
        FlowLine(start, end, color);
        var dx = end.X - start.X; var dy = end.Y - start.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var x = dx / length; var y = dy / length;
        var tail = new Point(end.X - x * 13, end.Y - y * 13);
        FlowLine(end, new Point(tail.X - y * 7, tail.Y + x * 7), color);
        FlowLine(end, new Point(tail.X + y * 7, tail.Y - x * 7), color);
        if (kind != FlowEdgeKind.Next)
        {
            var tag = new TextBlock
            {
                Text = kind.ToString(),
                Foreground = color, Background = Brushes.Black,
                Padding = new Thickness(3), IsHitTestVisible = false
            };
            Canvas.SetLeft(tag, (start.X + end.X) / 2 - 15);
            Canvas.SetTop(tag, (start.Y + end.Y) / 2 - 20);
            FlowCanvas.Children.Add(tag);
        }
    }

    private void RenderFlowDiagram()
    {
        if (FlowCanvas is null) return;
        FlowCanvas.Children.Clear();
        _flowBorders.Clear();
        foreach (var edge in _flowEdges)
        {
            var source = _flowNodes.FirstOrDefault(n => n.Id == edge.FromId);
            var dest = _flowNodes.FirstOrDefault(n => n.Id == edge.ToId);
            if (source is not null && dest is not null)
                DrawFlowEdge(source, dest, edge.Kind);
        }
        foreach (var node in _flowNodes)
        {
            var border = new Border
            {
                Width = 190, Height = 84, CornerRadius = new CornerRadius(7),
                Padding = new Thickness(8), BorderThickness = new Thickness(2),
                BorderBrush = Brushes.LightGray,
                Background = node.Kind switch
                {
                    FlowNodeKind.Command => Brushes.SteelBlue,
                    FlowNodeKind.If => Brushes.DarkGoldenrod,
                    FlowNodeKind.Parallel => Brushes.MediumPurple,
                    FlowNodeKind.Join => Brushes.Teal,
                    FlowNodeKind.Loop => Brushes.DarkCyan,
                    _ => Brushes.DarkSlateBlue
                },
                Child = new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = (node.Kind == FlowNodeKind.Loop
                                ? "↺ " + node.LoopMode + " ≤ " + node.MaxIterations
                                : node.Kind.ToString()) + " · " + node.Label,
                            FontWeight = FontWeight.Bold, Foreground = Brushes.White,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = node.Kind == FlowNodeKind.Loop && node.LoopMode == FlowLoopMode.For
                                ? "Repetir el cuerpo hasta " + node.MaxIterations + " veces"
                                : node.CommandTemplate.Length > 68
                                    ? node.CommandTemplate[..68] + "…" : node.CommandTemplate,
                            FontSize = 11, Foreground = Brushes.White,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };
            var captured = node;
            border.PointerPressed += (_, e) => FlowNodePressed(captured, border, e);
            border.PointerMoved += (_, e) => FlowNodeMoved(border, e);
            border.PointerReleased += (_, e) => FlowNodeReleased(border, e);
            Canvas.SetLeft(border, node.X); Canvas.SetTop(border, node.Y);
            _flowBorders[node.Id] = border;
            FlowCanvas.Children.Add(border);
        }
        HighlightFlowSelection();
    }
}
