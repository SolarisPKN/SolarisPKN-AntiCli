using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli;

/// <summary>One offline automation composer for every command and module.</summary>
public partial class SessionView
{
    private readonly List<string> _automationSteps = [];
    private string _lastScript = "";

    private void RefreshAutomationSteps(int selected = -1)
    {
        AutomationStepsList.ItemsSource = null;
        AutomationStepsList.ItemsSource = _automationSteps
            .Select((x, i) => (i + 1) + ". " + x).ToArray();
        AutomationStepsList.SelectedIndex = selected >= 0 ? selected : _automationSteps.Count - 1;
        _lastScript = "";
    }

    private void AutomationAddStepClicked(object? sender, RoutedEventArgs e)
    {
        var value = (AutomationStepInput.Text ?? "").Trim();
        if (value.Length is < 1 or > 3000 || value.Any(c => c is '\n' or '\r' or '\0'))
        {
            AutomationStatus.Text = "Ingresá una plantilla de una línea (máximo 3000 caracteres).";
            return;
        }
        if (_automationSteps.Count >= 30)
        {
            AutomationStatus.Text = "Máximo 30 pasos por flujo.";
            return;
        }
        _automationSteps.Add(value);
        AutomationStepInput.Text = "";
        RefreshAutomationSteps();
        AutomationStatus.Text = "Paso agregado. Generá la vista previa antes de guardar.";
    }

    private void AutomationAddCurrentClicked(object? sender, RoutedEventArgs e)
    {
        AutomationStepInput.Text = (AssistedCommand.Text ?? "").Trim();
        AutomationAddStepClicked(sender, e);
    }

    private void AutomationMoveUpClicked(object? sender, RoutedEventArgs e) => MoveAutomation(-1);
    private void AutomationMoveDownClicked(object? sender, RoutedEventArgs e) => MoveAutomation(1);

    private void MoveAutomation(int shift)
    {
        var idx = AutomationStepsList.SelectedIndex;
        if (idx < 0 || idx + shift < 0 || idx + shift >= _automationSteps.Count) return;
        (_automationSteps[idx], _automationSteps[idx + shift]) =
            (_automationSteps[idx + shift], _automationSteps[idx]);
        RefreshAutomationSteps(idx + shift);
    }

    private void AutomationRemoveStepClicked(object? sender, RoutedEventArgs e)
    {
        var idx = AutomationStepsList.SelectedIndex;
        if (idx < 0 || idx >= _automationSteps.Count) return;
        _automationSteps.RemoveAt(idx);
        RefreshAutomationSteps(Math.Min(idx, _automationSteps.Count - 1));
    }

    private static string[] ParseTableLine(string line, char separator)
    {
        var fields = new List<string>();
        var buffer = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    buffer.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == separator && !quoted)
            {
                fields.Add(buffer.ToString());
                buffer.Clear();
            }
            else buffer.Append(ch);
        }
        if (quoted) throw new ArgumentException("Comillas sin cerrar en tabla de valores.");
        fields.Add(buffer.ToString());
        return fields.ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadAutomationConstants(string? text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in (text ?? "").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var eq = line.IndexOf('=');
            if (eq < 1) throw new ArgumentException("Variables fijas: nombre=valor, una por línea.");
            var key = line[..eq].Trim();
            if (!values.TryAdd(key, line[(eq + 1)..].TrimEnd('\r')))
                throw new ArgumentException("Variable fija duplicada: " + key);
        }
        return values;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadAutomationRows(string? text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n',
            StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return [new Dictionary<string, string>(StringComparer.Ordinal)];
        var delimiter = lines[0].Contains('\t') ? '\t' : ';';
        var headers = ParseTableLine(lines[0], delimiter).Select(x => x.Trim()).ToArray();
        if (headers.Length is < 1 or > 40 || lines.Length is < 2 or > 101)
            throw new ArgumentException("Tabla: entre 1 y 40 columnas y de 1 a 100 filas.");
        var results = new List<IReadOnlyDictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            var fields = ParseTableLine(line.TrimEnd('\r'), delimiter);
            if (fields.Length != headers.Length)
                throw new ArgumentException("Hay una fila con diferente cantidad de columnas.");
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++)
            {
                if (!dict.TryAdd(headers[i], fields[i]))
                    throw new ArgumentException("Encabezado repetido: " + headers[i]);
            }
            results.Add(dict);
        }
        return results;
    }

    private AutomationWorkflow ComposeAutomation() =>
        new(1, (AutomationName.Text ?? "").Trim(),
            _automationSteps.Select((x, i) => new AutomationStep("step-" + (i + 1),
                "Paso " + (i + 1), x)).ToArray(),
            ReadAutomationConstants(AutomationConstants.Text),
            ReadAutomationRows(AutomationRows.Text),
            AutomationStepMajor.IsChecked == true ? AutomationOrder.PerStep : AutomationOrder.PerRow,
            AutomationContinueOnError.IsChecked != true);

    private void AutomationNumberedRowsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var columns = (AutomationColumns.Text ?? "").Split(';', StringSplitOptions.TrimEntries);
            var prefixes = (AutomationPrefixes.Text ?? "").Split(';');
            if (columns.Length is < 1 or > 40 || columns.Length != prefixes.Length)
                throw new ArgumentException("Necesitás un prefijo por columna, separados por ;");
            if (!int.TryParse(AutomationCount.Text, out var count) || count is < 1 or > 100 ||
                !int.TryParse(AutomationStart.Text, out var first) || first < 0 ||
                !int.TryParse(AutomationDigits.Text, out var digits) || digits is < 1 or > 8 ||
                first + count > 1_000_000)
                throw new ArgumentException("Lote: cantidad 1-100, inicio válido y 1-8 dígitos.");
            var rows = new StringBuilder(string.Join(";", columns));
            for (var i = 0; i < count; i++)
            {
                var suffix = (first + i).ToString().PadLeft(digits, '0');
                rows.Append('\n').Append(string.Join(";", prefixes.Select(x =>
                    x.Replace("{n}", suffix, StringComparison.Ordinal))));
            }
            // Validate the generated table before replacing the editor.
            ReadAutomationRows(rows.ToString());
            AutomationRows.Text = rows.ToString();
            _lastScript = "";
            AutomationStatus.Text = "Generadas " + count + " filas de valores independientes.";
        }
        catch (Exception ex) { AutomationStatus.Text = "Error de lote: " + ex.Message; }
    }

    private void AutomationPreviewClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!PrepareFlowForScript(out var flowProblem))
                throw new InvalidOperationException(flowProblem);
            var definition = ComposeAutomation();
            _lastScript = AutomationScriptExporter.Generate(definition);
            AutomationScriptPreview.Text = _lastScript;
            AutomationStatus.Text = "Vista previa de " + definition.Rows.Count + " filas × " +
                definition.Steps.Count + " pasos. No se ejecutó nada.";
            OnAutomationStepsChanged();
        }
        catch (Exception ex)
        {
            _lastScript = "";
            AutomationScriptPreview.Text = "";
            AutomationStatus.Text = "No se pudo generar: " + ex.Message;
        }
    }

    private async void AutomationSaveScriptClicked(object? sender, RoutedEventArgs e)
    {
        // Generate from current fields so a stale preview cannot be exported by accident.
        AutomationPreviewClicked(sender, e);
        if (_lastScript.Length == 0) return;
        await SaveAutomationText(_lastScript, "anti-cli-automation.sh", "*.sh");
    }

    private async void AutomationSaveProjectClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var json = AutomationWorkflowFile.Serialize(ComposeAutomation());
            await SaveAutomationText(json, "anti-cli-workflow.json", "*.json");
        }
        catch (Exception ex) { AutomationStatus.Text = "No se pudo guardar: " + ex.Message; }
    }

    private async Task SaveAutomationText(string text, string filename, string pattern)
    {
        try
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider
                ?? throw new InvalidOperationException("No hay selector de archivos.");
            var path = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar automatización AntiCLI", SuggestedFileName = filename,
                FileTypeChoices = [new FilePickerFileType("Archivo")
                    { Patterns = [pattern] }]
            });
            if (path is null) return;
            await using var stream = await path.OpenWriteAsync();
            if (stream.CanSeek) stream.SetLength(0);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(text);
            AutomationStatus.Text = "Archivo guardado. No se ejecutó en ningún servidor.";
        }
        catch (Exception ex) { AutomationStatus.Text = "Error al guardar: " + ex.Message; }
    }

    private async void AutomationLoadProjectClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var provider = TopLevel.GetTopLevel(this)?.StorageProvider
                ?? throw new InvalidOperationException("No hay selector de archivos.");
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Abrir automatización JSON", AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Proyecto JSON")
                    { Patterns = ["*.json"] }]
            });
            if (files.Count == 0) return;
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            var workflow = AutomationWorkflowFile.Parse(await reader.ReadToEndAsync());
            AutomationName.Text = workflow.Name;
            _automationSteps.Clear();
            _automationSteps.AddRange(workflow.Steps.Where(x => x.Enabled)
                .Select(x => x.CommandTemplate));
            RefreshAutomationSteps();
            AutomationConstants.Text = string.Join("\n", workflow.Constants
                .Select(x => x.Key + "=" + x.Value));
            var columns = workflow.Rows.SelectMany(x => x.Keys)
                .Distinct(StringComparer.Ordinal).ToArray();
            AutomationRows.Text = columns.Length == 0 ? "" :
                string.Join(";", columns) + "\n" + string.Join("\n", workflow.Rows
                    .Select(row => string.Join(";", columns.Select(key =>
                        row.TryGetValue(key, out var value) ? value : ""))));
            AutomationStepMajor.IsChecked = workflow.Order == AutomationOrder.PerStep;
            AutomationContinueOnError.IsChecked = !workflow.StopOnError;
            AutomationScriptPreview.Text = "";
            AutomationStatus.Text = "Proyecto cargado. Revisá y generá la vista previa.";
            OnAutomationStepsChanged();
        }
        catch (Exception ex) { AutomationStatus.Text = "No se pudo abrir: " + ex.Message; }
    }

    private void OnAutomationStepsChanged() => RefreshFlowDiagramFromAutomation();
}
