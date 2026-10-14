using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli;

public partial class SessionView
{
    private async void FlowSaveClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = AutomationGraphFile.Serialize(CurrentFlowGraph());
            await SaveAutomationText(text, "workflow-diagram.json", "*.json");
            FlowStatus.Text = "Diagrama JSON preparado para guardar.";
        }
        catch (Exception error) { FlowStatus.Text = error.Message; }
    }

    private async void FlowLoadClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (provider is null) return;
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Abrir diagrama", AllowMultiple = false
            });
            if (files.Count == 0) return;
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            var graph = AutomationGraphFile.Parse(await reader.ReadToEndAsync());
            _flowNodes.Clear(); _flowNodes.AddRange(graph.Nodes);
            _flowEdges.Clear(); _flowEdges.AddRange(graph.Edges);
            _flowSelected = null;
            _flowEdited = true;
            RenderFlowDiagram();
            FlowStatus.Text = "Diagrama cargado y validado.";
        }
        catch (Exception error) { FlowStatus.Text = error.Message; }
    }
}
