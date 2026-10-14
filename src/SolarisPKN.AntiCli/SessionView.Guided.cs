using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Forms are derived from typed command schemas rather than hard-coded shell
/// fragments. A user can always edit the preview to issue a custom command.
/// </summary>
public partial class SessionView
{
    private GuidedCommand? _activeGuide;
    private GuidedPlan? _activePlan;
    private readonly List<GuidedFlagChoice> _orderedGuideFlags = [];
    private TextBox? _accountNameBox;
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

        var text = (UniversalSearch.Text ?? "").Trim();
        var normalized = Regex.Replace(text, @"\b\d+\b", "").Trim();
        var guides = GuidedCommandCatalog.Search(normalized);
        foreach (var guide in guides.Take(4))
        {
            var selected = guide;
            var button = new Button
            {
                Content = AdminCommandCatalog.Dot(selected.Risk) + " " +
                    (selected.Id == "adduser" ? "Crear usuario(s)" : selected.TitleEs) +
                    " · asistente",
                Margin = new Thickness(0, 0, 8, 8)
            };
            ToolTip.SetTip(button, guide.ExplanationEs);
            button.Click += (_, _) =>
            {
                var countMatch = Regex.Match(text, @"\b(?<num>\d{1,3})\s+(?:usuarios|cuentas|alumnos)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var count = selected.SupportsBatch && countMatch.Success
                    ? Math.Clamp(int.Parse(countMatch.Groups["num"].Value), 1, 100) : 1;
                BeginGuided(selected, count);
            };
            UniversalResults.Children.Add(button);
        }

        if (text.Length == 0) return;
        var searchTerms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matches = AdminCommandCatalog.For(_connectedProfile.Platform)
            .Where(x => searchTerms.All(word =>
                (x.LabelEs + " " + x.DescriptionEs + " " + x.GroupName +
                 " " + x.Command).Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Take(8);
        foreach (var action in matches)
        {
            var found = action;
            var button = new Button
            {
                Content = AdminCommandCatalog.Dot(found.Risk) + " " + found.LabelEs,
                Margin = new Thickness(0, 0, 8, 8)
            };
            ToolTip.SetTip(button, action.DescriptionEs + " — " + action.Command);
            button.Click += (_, _) => SelectAction(
                new ReadOnlyAction(found.Id, found.LabelEs, found.Command));
            UniversalResults.Children.Add(button);
        }
    }

    private void BeginGuided(GuidedCommand definition, int quantity = 1)
    {
        if (_connectedProfile is null) return;
        if (_connectedProfile.Platform == TargetPlatform.Windows)
        {
            ExecutionStatus.Text = "El constructor guiado Linux no corresponde a Windows.";
            return;
        }
        _activeGuide = definition;
        _activePlan = null;
        _orderedGuideFlags.Clear();
        foreach (var option in definition.Flags.Where(x => x.SelectedByDefault))
            _orderedGuideFlags.Add(new GuidedFlagChoice(option.Token, option.InitialValue));

        GuidedPanel.IsVisible = true;
        GuidedTitle.Text = AdminCommandCatalog.Dot(definition.Risk) + " " + definition.TitleEs +
            " · " + definition.Executable;
        GuidedDescription.Text = definition.ExplanationEs;
        GuidedError.Text = "";
        _batchEnabled = definition.SupportsBatch && quantity > 1;
        CreateGuidedArguments(quantity);
        RebuildGuidedFlags();
        RecomputeGuidedPreview();
    }

    private void CreateGuidedArguments(int quantity)
    {
        GuidedArguments.Children.Clear();
        _accountNameBox = _batchCountBox = _batchStartBox = _batchDigitsBox = null;
        if (_activeGuide?.Id != "adduser") return;
        GuidedArguments.Children.Add(new TextBlock
        {
            Text = "Nombre de usuario o prefijo del lote", FontWeight = Avalonia.Media.FontWeight.SemiBold
        });
        _accountNameBox = new TextBox { Watermark = "Ej.: alumno", Text = "" };
        _accountNameBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        GuidedArguments.Children.Add(_accountNameBox);

        var batchSwitch = new CheckBox
        {
            Content = "Crear varios usuarios con un solo comando (ej.: alumno01…alumno20)",
            IsChecked = _batchEnabled
        };
        GuidedArguments.Children.Add(batchSwitch);
        var batchFields = new StackPanel { Spacing = 6, IsVisible = _batchEnabled };
        GuidedArguments.Children.Add(batchFields);
        batchFields.Children.Add(new TextBlock
        {
            Text = "Cantidad de usuarios (1–100)"
        });
        _batchCountBox = new TextBox
        {
            Text = Math.Max(1, quantity).ToString(),
            Watermark = "20", Width = 120, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        batchFields.Children.Add(_batchCountBox);
        batchFields.Children.Add(new TextBlock { Text = "Número inicial" });
        _batchStartBox = new TextBox
        {
            Text = "1", Watermark = "1", Width = 120,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        batchFields.Children.Add(_batchStartBox);
        batchFields.Children.Add(new TextBlock { Text = "Dígitos mínimos del sufijo" });
        _batchDigitsBox = new TextBox
        {
            Text = "2", Watermark = "2", Width = 120,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        batchFields.Children.Add(_batchDigitsBox);
        batchFields.Children.Add(new TextBlock
        {
            Text = "Ejemplo: prefijo alumno, cantidad 20, inicio 1, dígitos 2 → alumno01…alumno20. " +
                   "Si un usuario falla, el lote se detiene con el código de error.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        _batchCountBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        _batchStartBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        _batchDigitsBox.TextChanged += (_, _) => RecomputeGuidedPreview();
        batchSwitch.Checked += (_, _) =>
        {
            _batchEnabled = true;
            batchFields.IsVisible = true;
            RecomputeGuidedPreview();
        };
        batchSwitch.Unchecked += (_, _) =>
        {
            _batchEnabled = false;
            batchFields.IsVisible = false;
            RecomputeGuidedPreview();
        };
    }

    private void RebuildGuidedFlags()
    {
        GuidedOptions.Children.Clear();
        if (_activeGuide is null) return;
        var selected = _activeGuide.Flags
            .Where(o => _orderedGuideFlags.Any(x => x.Token == o.Token))
            .OrderBy(o => _orderedGuideFlags.FindIndex(x => x.Token == o.Token));
        var unselected = _activeGuide.Flags
            .Where(o => !_orderedGuideFlags.Any(x => x.Token == o.Token));
        foreach (var option in selected.Concat(unselected))
        {
            var item = option;
            var position = _orderedGuideFlags.FindIndex(x => x.Token == item.Token);
            var enabled = position >= 0;
            var outer = new StackPanel { Spacing = 3 };
            var top = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };
            var check = new CheckBox
            {
                IsChecked = enabled,
                Content = item.LabelEs + "  (" + item.Token + ")",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            ToolTip.SetTip(check, item.DescriptionEs);
            top.Children.Add(check);
            if (enabled)
            {
                var up = new Button
                {
                    Content = "↑", IsEnabled = position > 0, MinWidth = 33
                };
                var down = new Button
                {
                    Content = "↓", IsEnabled = position < _orderedGuideFlags.Count - 1, MinWidth = 33
                };
                up.Click += (_, _) => MoveGuidedOption(item.Token, -1);
                down.Click += (_, _) => MoveGuidedOption(item.Token, 1);
                top.Children.Add(up);
                top.Children.Add(down);
            }
            outer.Children.Add(top);
            outer.Children.Add(new TextBlock
            {
                Text = item.DescriptionEs, Opacity = 0.75,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(22, 0, 0, 0)
            });
            if (enabled && item.ValueKind != GuidedValueKind.None)
            {
                var input = new TextBox
                {
                    Watermark = item.Placeholder,
                    Text = _orderedGuideFlags[position].Value,
                    Margin = new Thickness(22, 0, 0, 0)
                };
                input.TextChanged += (_, _) =>
                {
                    var index = _orderedGuideFlags.FindIndex(x => x.Token == item.Token);
                    if (index < 0) return;
                    _orderedGuideFlags[index] =
                        _orderedGuideFlags[index] with { Value = input.Text ?? "" };
                    RecomputeGuidedPreview();
                };
                outer.Children.Add(input);
            }
            check.Checked += (_, _) =>
            {
                if (_orderedGuideFlags.All(x => x.Token != item.Token))
                    _orderedGuideFlags.Add(new GuidedFlagChoice(item.Token, item.InitialValue));
                RebuildGuidedFlags();
                RecomputeGuidedPreview();
            };
            check.Unchecked += (_, _) =>
            {
                _orderedGuideFlags.RemoveAll(x => x.Token == item.Token);
                RebuildGuidedFlags();
                RecomputeGuidedPreview();
            };
            GuidedOptions.Children.Add(outer);
        }
    }

    private void MoveGuidedOption(string token, int step)
    {
        var index = _orderedGuideFlags.FindIndex(x => x.Token == token);
        var other = index + step;
        if (index < 0 || other < 0 || other >= _orderedGuideFlags.Count) return;
        (_orderedGuideFlags[index], _orderedGuideFlags[other]) =
            (_orderedGuideFlags[other], _orderedGuideFlags[index]);
        RebuildGuidedFlags();
        RecomputeGuidedPreview();
    }

    private void RecomputeGuidedPreview()
    {
        if (_activeGuide is null) return;
        try
        {
            var quantity = _batchEnabled ? ParsePositive(_batchCountBox?.Text, -1) : 1;
            var first = _batchEnabled ? ParsePositive(_batchStartBox?.Text, -1) : 1;
            var digits = _batchEnabled ? ParsePositive(_batchDigitsBox?.Text, -1) : 2;
            _activePlan = GuidedCommandCatalog.Build(
                _activeGuide, _orderedGuideFlags,
                _accountNameBox?.Text, quantity, first, digits);
            GuidedError.Text = "";
            GuidedPlanInfo.Text = _activePlan.Note +
                " · Operaciones previstas: " + _activePlan.Operations +
                ". El comando es editable abajo; los cambios manuales se clasificarán en rojo.";
            AssistedCommand.Text = _activePlan.Command;
            UpdateRiskDisplay();
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            _activePlan = null;
            GuidedError.Text = "Completá o corregí el formulario: " + ex.Message;
            GuidedPlanInfo.Text = "No hay comando válido para ejecutar.";
            AssistedCommand.Text = "";
            UpdateRiskDisplay();
        }
    }

    private void CloseGuidedClicked(object? sender, RoutedEventArgs e) =>
        HideGuidedBuilder();

    private void HideGuidedBuilder()
    {
        _activeGuide = null;
        _activePlan = null;
        _orderedGuideFlags.Clear();
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
