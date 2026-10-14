using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Two editable representations of the same versioned schema: a convenient
/// field editor and the complete JSON for advanced options. Neither generates
/// command-specific UI code. Save is an explicit local filesystem action.
/// </summary>
public partial class SessionView
{
    private static readonly JsonSerializerOptions ModuleJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private GuidedCommand? _moduleEditorBase;
    private bool _loadingModuleEditor;

    private static string ModuleDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SolarisPKN", "AntiCLI", "modules");

    private void RefreshModuleEditorSources()
    {
        if (ModuleEditorSource is null) return;
        _loadingModuleEditor = true;
        try
        {
            var currentId = (ModuleEditorSource.SelectedItem as GuidedCommand)?.Id;
            var list = GuidedCommandCatalog.All.OrderBy(x => x.TitleEs,
                StringComparer.CurrentCultureIgnoreCase).ToArray();
            ModuleEditorSource.ItemsSource = list;
            ModuleEditorSource.SelectedItem = list.FirstOrDefault(x => x.Id == currentId);
        }
        finally { _loadingModuleEditor = false; }
    }

    private void ModuleEditorSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loadingModuleEditor && ModuleEditorSource.SelectedItem is GuidedCommand guide)
            ModuleEditorStatus.Text = "Plantilla seleccionada: " + guide.Id +
                ". Pulsá «Cargar plantilla» para editar una copia sin alterar los módulos integrados.";
    }

    private void ModuleEditorNewClicked(object? sender, RoutedEventArgs e)
    {
        _moduleEditorBase = null;
        ModuleEditorId.Text = "local.mi-comando";
        ModuleEditorTitle.Text = "";
        ModuleEditorExecutable.Text = "";
        ModuleEditorDescription.Text = "";
        ModuleEditorKeywords.Text = "";
        ModuleEditorTemplate.Text = "";
        ModuleEditorFlags.Text = "";
        ModuleEditorOperands.Text = "";
        RebuildModuleEditRows();
        ModuleEditorJson.Text = "";
        ModuleEditorStatus.Text = "Módulo vacío. Definí un ID local, completá los campos y generá el JSON.";
    }

    private void ModuleEditorLoadClicked(object? sender, RoutedEventArgs e)
    {
        if (ModuleEditorSource.SelectedItem is not GuidedCommand selected)
        {
            ModuleEditorStatus.Text = "Elegí una plantilla existente.";
            return;
        }
        // Built-in definitions cannot be overwritten. The fork keeps all advanced
        // fields (batch strategy, platform, timeout, references) in JSON.
        var id = selected.Id.StartsWith("local.", StringComparison.Ordinal)
            ? selected.Id : "local." + Regex.Replace(selected.Id, @"[^a-zA-Z0-9_.-]", "-");
        _moduleEditorBase = selected with { Id = id };
        LoadModuleIntoFields(_moduleEditorBase);
        RebuildModuleEditRows();
        ModuleEditorJson.Text = JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Commands = new[] { _moduleEditorBase }
        }, ModuleJsonOptions);
        ModuleEditorStatus.Text = "Cargada copia editable de «" + selected.TitleEs + "». " +
            "Los cambios a campos y flags requieren «Generar JSON» antes de guardar.";
    }

    private void LoadModuleIntoFields(GuidedCommand cmd)
    {
        ModuleEditorId.Text = cmd.Id;
        ModuleEditorTitle.Text = cmd.TitleEs;
        ModuleEditorExecutable.Text = cmd.Executable;
        ModuleEditorDescription.Text = cmd.ExplanationEs;
        ModuleEditorKeywords.Text = cmd.SearchTerms;
        ModuleEditorTemplate.Text = cmd.Template;
        ModuleEditorFlags.Text = string.Join("\n", cmd.Flags.Select(f =>
            string.Join(" | ", f.Token, f.LabelEs, f.DescriptionEs,
                f.ValueKind.ToString(), f.InitialValue,
                f.SelectedByDefault ? "true" : "false")));
        ModuleEditorOperands.Text = string.Join("\n", cmd.Operands.Select(o =>
            string.Join(" | ", o.Name, o.LabelEs, o.DescriptionEs,
                o.ValueKind.ToString(), o.Required ? "true" : "false", o.InitialValue)));
    }

    private static string[][] ParseEditorRows(string? text, string name)
    {
        var results = new List<string[]>();
        foreach (var raw in (text ?? "").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var parts = raw.TrimEnd('\r').Split('|').Select(p => p.Trim()).ToArray();
            if (parts.Length != 6)
                throw new ArgumentException("Cada línea de " + name +
                    " necesita 6 columnas separadas por |: " + raw.Trim());
            results.Add(parts);
        }
        return results.ToArray();
    }

    private GuidedCommand ReadModuleForm()
    {
        var id = (ModuleEditorId.Text ?? "").Trim();
        var exe = (ModuleEditorExecutable.Text ?? "").Trim();
        var template = (ModuleEditorTemplate.Text ?? "").Trim();
        if (!Regex.IsMatch(id, @"\Alocal\.[a-zA-Z0-9][a-zA-Z0-9_.-]{0,95}\z"))
            throw new ArgumentException("El ID debe empezar por local. y usar letras, números, guiones o puntos.");
        if (!Regex.IsMatch(exe, @"\A[a-zA-Z_][a-zA-Z0-9_.+-]{0,59}\z"))
            throw new ArgumentException("El ejecutable debe ser un nombre simple, sin rutas ni parámetros.");
        if (template.Length == 0)
            throw new ArgumentException("Completá la plantilla del comando.");

        var flags = ParseEditorRows(ModuleEditorFlags.Text, "flags")
            .Select(p =>
            {
                if (!Enum.TryParse<GuidedValueKind>(p[3], true, out var kind))
                    throw new ArgumentException("Tipo de flag no reconocido: " + p[3]);
                if (!bool.TryParse(p[5], out var initial))
                    throw new ArgumentException("Predeterminado debe ser true o false.");
                return new GuidedFlag(p[0], p[1], p[2], kind, "", initial, p[4]);
            }).ToArray();

        var operands = ParseEditorRows(ModuleEditorOperands.Text, "parámetros")
            .Select(p =>
            {
                if (!Enum.TryParse<GuidedValueKind>(p[3], true, out var kind))
                    throw new ArgumentException("Tipo de parámetro no reconocido: " + p[3]);
                if (!bool.TryParse(p[4], out var required))
                    throw new ArgumentException("Requerido debe ser true o false.");
                return new GuidedOperand(p[0], p[1], p[2], kind, "", required, p[5]);
            }).ToArray();
        var model = _moduleEditorBase;
        return new GuidedCommand(
            id, exe,
            (ModuleEditorTitle.Text ?? "").Trim(),
            (ModuleEditorDescription.Text ?? "").Trim(),
            (ModuleEditorKeywords.Text ?? "").Trim(),
            model?.Risk ?? CommandRisk.Red,
            template,
            flags, operands,
            model?.SupportsBatch ?? false,
            model?.TimeoutSeconds ?? 300,
            model?.BatchOperand,
            model?.RequiredAnyFlags,
            model?.BatchDisallowedFlags,
            model?.AutoFlags,
            model?.DocumentationUrl,
            // Forked presets are editable; original alias may remain in the JSON
            // until the operator customizes it.
            model?.PresetCommands,
            model?.Platforms ?? ["Linux"]);
    }

    private void ModuleEditorGenerateClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var guide = ReadModuleForm();
            var json = JsonSerializer.Serialize(new
                { SchemaVersion = 1, Commands = new[] { guide } }, ModuleJsonOptions);
            GuidedCommandCatalog.ValidateExternalJson(json);
            ModuleEditorJson.Text = json;
            ModuleEditorStatus.Text = "JSON generado y validado. Ahora podés editar directamente " +
                "la sección avanzada o guardar.";
        }
        catch (Exception ex)
        {
            ModuleEditorStatus.Text = "No se pudo generar: " + ex.Message;
        }
    }

    private void ModuleEditorValidateClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var definitions = GuidedCommandCatalog.ValidateExternalJson(ModuleEditorJson.Text ?? "");
            ModuleEditorStatus.Text = "✓ JSON válido: " + definitions.Count +
                " módulo(s). Aún no se guardó nada.";
        }
        catch (Exception ex) { ModuleEditorStatus.Text = "JSON inválido: " + ex.Message; }
    }

    private void ModuleEditorSaveClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var json = ModuleEditorJson.Text ?? "";
            var definitions = GuidedCommandCatalog.ValidateExternalJson(json);
            if (definitions.Count != 1)
                throw new InvalidDataException("Guardá una ficha por archivo desde este editor.");
            var id = definitions[0].Id;
            if (!Regex.IsMatch(id, @"\Alocal\.[a-zA-Z0-9][a-zA-Z0-9_.-]{0,95}\z"))
                throw new InvalidDataException("Para guardar, el ID debe empezar por local.");
            Directory.CreateDirectory(ModuleDirectory);
            var target = Path.Combine(ModuleDirectory, id + ".json");
            var temp = Path.Combine(ModuleDirectory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // The save dialog/editor never executes the JSON: it is data.
                File.WriteAllText(temp, JsonSerializer.Serialize(
                    JsonDocument.Parse(json).RootElement, ModuleJsonOptions));
                File.Move(temp, target, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            GuidedCommandCatalog.Reload();
            RefreshModuleEditorSources();
            PopulateUniversalResults();
            ModuleEditorStatus.Text = "✓ Guardado y activado: " + target +
                " · Ahora podés encontrarlo en el buscador universal.";
        }
        catch (Exception ex)
        {
            ModuleEditorStatus.Text = "No se guardó el módulo: " + ex.Message;
        }
    }
}
