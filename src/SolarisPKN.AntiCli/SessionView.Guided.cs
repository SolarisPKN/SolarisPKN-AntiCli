using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Modules;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// ONE renderer for every command defined in JSON, whether uname, adduser,
/// apt-get, Yocto/BitBake or the long tail of imported Linux tools.
/// No UI code may branch on the name/ID of a command.
/// </summary>
public partial class SessionView
{
    private GuidedCommand? _activeGuide;
    private GuidedPlan? _activePlan;
    private readonly List<GuidedFlagChoice> _orderedGuideFlags = [];
    private readonly Dictionary<string, TextBox> _guideFields = new(StringComparer.Ordinal);
    private TextBox? _batchCountBox;
    private TextBox? _batchStartBox;
    private TextBox? _batchDigitsBox;
    private bool _batchEnabled;

    private static int ParsePositive(string? text, int fallback) =>
        int.TryParse(text, out var n) ? n : fallback;

    private void UniversalSearchChanged(object? sender, TextChangedEventArgs e) =>
        PopulateUniversalResults();

    private void PopulateUniversalResults()
    {
        if (UniversalResults is null) return;
        UniversalResults.Children.Clear();
        if (_connectedProfile is null) return;

        var query = (UniversalSearch.Text ?? "").Trim();
        var stripped = Regex.Replace(query, @"\b\d+\b", "").Trim();
        var countMatch = Regex.Match(query,
            @"\b(?<count>\d{1,3})\s+(?:usuarios|cuentas|alumnos)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var suggestedCount = countMatch.Success
            ? Math.Clamp(int.Parse(countMatch.Groups["count"].Value), 1, 100) : 1;
        var candidates = GuidedCommandCatalog.Search(stripped, _connectedProfile.Platform);
        foreach (var guide in candidates.Take(query.Length == 0 ? 6 : 12))
        {
            var item = guide;
            var button = new Button
            {
                Content = AdminCommandCatalog.Dot(item.Risk) + " " + item.TitleEs,
                Margin = new Thickness(0, 0, 8, 8)
            };
            ToolTip.SetTip(button, item.ExplanationEs);
            button.Click += (_, _) =>
                BeginGuided(item, item.SupportsBatch ? suggestedCount : 1);
            UniversalResults.Children.Add(button);
        }
    }

    private void BeginGuided(GuidedCommand definition, int quantity = 1)
    {
        if (_connectedProfile is null) return;
        if (!GuidedCommandCatalog.SupportsPlatform(definition, _connectedProfile.Platform))
        {
            ExecutionStatus.Text = "Este módulo no corresponde al sistema remoto.";
            return;
        }

        definition = GuidedHelpStore.Enrich(definition, _connectedProfile.Host,
            _connectedProfile.Port);
        _activeGuide = definition;
        HelpImportStatus.Text = "Ficha: " + definition.Id +
            " · flags: " + definition.Flags.Count +
            " · datos JSON + opciones importadas de este servidor, si existen.";
        _activePlan = null;
        _orderedGuideFlags.Clear();
        foreach (var flag in definition.Flags.Where(x => x.SelectedByDefault))
            _orderedGuideFlags.Add(new GuidedFlagChoice(flag.Token, flag.InitialValue));

        GuidedPanel.IsVisible = true;
        GuidedTitle.Text = AdminCommandCatalog.Dot(definition.Risk) + " " + definition.TitleEs +
            " · " + definition.Executable;
        GuidedDescription.Text = definition.ExplanationEs;
        GuidedError.Text = "";
        _batchEnabled = definition.SupportsBatch && quantity > 1;
        BuildOperandInputs(quantity);
        RebuildGuidedFlags();
        RecomputeGuidedPreview();
    }

    private void BuildOperandInputs(int initialQuantity)
    {
        GuidedArguments.Children.Clear();
        _guideFields.Clear();
        _batchCountBox = _batchStartBox = _batchDigitsBox = null;
        if (_activeGuide is null) return;

        foreach (var definition in _activeGuide.Operands)
        {
            var field = definition;
            GuidedArguments.Children.Add(new TextBlock
            {
                Text = definition.LabelEs + (definition.Required ? " *" : ""),
                FontWeight = Avalonia.Media.FontWeight.SemiBold
            });
            GuidedArguments.Children.Add(new TextBlock
            {
                Text = definition.DescriptionEs,
                Opacity = 0.7,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
            var input = new TextBox
            {
                Text = definition.InitialValue,
                Watermark = definition.Placeholder
            };
            input.TextChanged += (_, _) => RecomputeGuidedPreview();
            _guideFields.Add(field.Name, input);
            GuidedArguments.Children.Add(input);
        }

        if (!_activeGuide.SupportsBatch) return;
        var batchToggle = new CheckBox
        {
            Content = "Crear un lote con nombres o valores numerados",
            IsChecked = _batchEnabled
        };
        GuidedArguments.Children.Add(batchToggle);
        var fields = new StackPanel { Spacing = 6, IsVisible = _batchEnabled };
        GuidedArguments.Children.Add(fields);
        fields.Children.Add(new TextBlock { Text = "Cantidad (1–100)" });
        _batchCountBox = new TextBox { Text = initialQuantity.ToString(), Width = 110,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        fields.Children.Add(_batchCountBox);
        fields.Children.Add(new TextBlock { Text = "Número inicial" });
        _batchStartBox = new TextBox { Text = "1", Width = 110,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        fields.Children.Add(_batchStartBox);
        fields.Children.Add(new TextBlock { Text = "Mínimo de dígitos" });
        _batchDigitsBox = new TextBox { Text = "2", Width = 110,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        fields.Children.Add(_batchDigitsBox);
        fields.Children.Add(new TextBlock
        {
            Text = "Ej.: prefijo alumno, cantidad 20, número inicial 1 → alumno01…alumno20. " +
                "Se detiene en el primer error.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        _batchCountBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        _batchStartBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        _batchDigitsBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        batchToggle.PropertyChanged += (_, e) =>
        {
            if (e.Property != ToggleButton.IsCheckedProperty) return;
            _batchEnabled = batchToggle.IsChecked == true;
            fields.IsVisible = _batchEnabled;
            RecomputeGuidedPreview();
        };
    }

    private void RebuildGuidedFlags()
    {
        GuidedOptions.Children.Clear();
        if (_activeGuide is null) return;
        var selected = _activeGuide.Flags
            .Where(x => _orderedGuideFlags.Any(c => c.Token == x.Token))
            .OrderBy(x => _orderedGuideFlags.FindIndex(c => c.Token == x.Token));
        var unselected = _activeGuide.Flags
            .Where(x => !_orderedGuideFlags.Any(c => c.Token == x.Token));

        foreach (var flag in selected.Concat(unselected))
        {
            var choice = flag;
            var position = _orderedGuideFlags.FindIndex(x => x.Token == choice.Token);
            var enabled = position >= 0;
            var container = new StackPanel { Spacing = 3 };
            var controls = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 7
            };
            var toggle = new CheckBox
            {
                IsChecked = enabled,
                Content = choice.LabelEs + " (" + choice.Token + ")",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            ToolTip.SetTip(toggle, choice.DescriptionEs);
            controls.Children.Add(toggle);
            if (enabled)
            {
                var up = new Button { Content = "↑", IsEnabled = position > 0 };
                var down = new Button { Content = "↓",
                    IsEnabled = position < _orderedGuideFlags.Count - 1 };
                up.Click += (_, _) => MoveGuidedOption(choice.Token, -1);
                down.Click += (_, _) => MoveGuidedOption(choice.Token, 1);
                controls.Children.Add(up);
                controls.Children.Add(down);
            }
            container.Children.Add(controls);
            container.Children.Add(new TextBlock
            {
                Text = choice.DescriptionEs,
                Opacity = 0.75,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(20, 0, 0, 0)
            });
            if (enabled && choice.ValueKind != GuidedValueKind.None)
            {
                var valueInput = new TextBox
                {
                    Watermark = choice.Placeholder,
                    Text = _orderedGuideFlags[position].Value,
                    Margin = new Thickness(20, 0, 0, 0)
                };
                valueInput.TextChanged += (_, _) =>
                {
                    var idx = _orderedGuideFlags.FindIndex(x => x.Token == choice.Token);
                    if (idx < 0) return;
                    _orderedGuideFlags[idx] = _orderedGuideFlags[idx] with
                    {
                        Value = valueInput.Text ?? ""
                    };
                    RecomputeGuidedPreview();
                };
                container.Children.Add(valueInput);
            }
            toggle.PropertyChanged += (_, e) =>
            {
                if (e.Property != ToggleButton.IsCheckedProperty) return;
                if (toggle.IsChecked == true)
                {
                    if (_orderedGuideFlags.All(x => x.Token != choice.Token))
                        _orderedGuideFlags.Add(
                            new GuidedFlagChoice(choice.Token, choice.InitialValue));
                }
                else
                    _orderedGuideFlags.RemoveAll(x => x.Token == choice.Token);
                RebuildGuidedFlags();
                RecomputeGuidedPreview();
            };
            GuidedOptions.Children.Add(container);
        }
    }

    private void MoveGuidedOption(string token, int direction)
    {
        var current = _orderedGuideFlags.FindIndex(x => x.Token == token);
        var next = current + direction;
        if (current < 0 || next < 0 || next >= _orderedGuideFlags.Count) return;
        (_orderedGuideFlags[current], _orderedGuideFlags[next]) =
            (_orderedGuideFlags[next], _orderedGuideFlags[current]);
        RebuildGuidedFlags();
        RecomputeGuidedPreview();
    }

    private void RecomputeGuidedPreview()
    {
        if (_activeGuide is null) return;
        try
        {
            var quantity = _batchEnabled ? ParsePositive(_batchCountBox?.Text, -1) : 1;
            var start = _batchEnabled ? ParsePositive(_batchStartBox?.Text, -1) : 1;
            var digits = _batchEnabled ? ParsePositive(_batchDigitsBox?.Text, -1) : 2;
            var values = _guideFields.ToDictionary(x => x.Key,
                x => x.Value.Text ?? "", StringComparer.Ordinal);
            _activePlan = GuidedCommandCatalog.Build(
                _activeGuide, _orderedGuideFlags, values, quantity, start, digits);
            GuidedError.Text = "";
            GuidedPlanInfo.Text = _activePlan.Note +
                " · Operaciones previstas: " + _activePlan.Operations +
                ". Podés editar el comando antes de ejecutarlo.";
            AssistedCommand.Text = _activePlan.Command;
            UpdateRiskDisplay();
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            _activePlan = null;
            GuidedError.Text = "Completá el formulario: " + ex.Message;
            GuidedPlanInfo.Text = "No hay un comando válido para ejecutar.";
            AssistedCommand.Text = "";
            UpdateRiskDisplay();
        }
    }

    private async void ImportGuidedHelpClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy || _activeGuide is null ||
            _connectedProfile is null || _connectedCredentials is null) return;
        var guide = _activeGuide;
        var endpoint = _connectedProfile.Host;
        var port = _connectedProfile.Port;
        try
        {
            var command = GuidedHelpStore.ProbeCommand(guide.Executable);
            _busy = true;
            HelpImportStatus.Text = "Consultando " + command + " sin sudo (máximo 20 segundos)...";
            var result = await _ssh.ExecuteAsync(
                _connectedProfile, _connectedCredentials, command, TimeSpan.FromSeconds(20));
            var help = result.StdOut + "\n" + result.StdErr;
            var flags = GuidedHelpStore.Parse(help);
            if (flags.Count == 0)
                throw new InvalidOperationException(
                    "La ayuda no contiene flags reconocibles. Podés seguir usando el comando manualmente.");
            if (_invalidateAfterBusy || _activeGuide.Id != guide.Id)
                throw new InvalidOperationException("Cambió el destino o el módulo durante la consulta.");
            GuidedHelpStore.Save(endpoint, port, guide.Executable, help);
            _activeGuide = GuidedHelpStore.Enrich(guide, endpoint, port);
            RebuildGuidedFlags();
            RecomputeGuidedPreview();
            HelpImportStatus.Text = "Importadas " + flags.Count +
                " flags desde la ayuda. Guardadas en AppData para este host:puerto. " +
                "Las descripciones importadas pueden necesitar revisión.";
        }
        catch (Exception ex)
        {
            HelpImportStatus.Text = "No se pudieron importar opciones: " + ex.Message;
        }
        finally { FinishOperation(); }
    }

    private void CloseGuidedClicked(object? sender, RoutedEventArgs e) =>
        HideGuidedBuilder();

    private void HideGuidedBuilder()
    {
        _activeGuide = null;
        _activePlan = null;
        _orderedGuideFlags.Clear();
        _guideFields.Clear();
        GuidedPanel.IsVisible = false;
        GuidedError.Text = "";
    }

    private bool TryGetGuidedRisk(string? command, out CommandRisk risk)
    {
        if (_activePlan is not null &&
            string.Equals((command ?? "").Trim(), _activePlan.Command, StringComparison.Ordinal))
        {
            risk = _activePlan.Risk;
            return true;
        }
        risk = CommandRisk.Red;
        return false;
    }
}
