using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

// These types describe WHAT a command needs. The UI does not know command names.
public enum GuidedValueKind
{
    None, Text, UserGroup, AbsolutePath, PathOrName, PositiveId, Account,
    PackageList, Identifier, UrlHttps, Suite, Components, Fingerprint,
    Recipe, Task, CleanTask
}
public sealed record GuidedFlag(
    string Token, string LabelEs, string DescriptionEs,
    GuidedValueKind ValueKind = GuidedValueKind.None,
    string Placeholder = "", bool SelectedByDefault = false,
    string InitialValue = "");
public sealed record GuidedOperand(
    string Name, string LabelEs, string DescriptionEs,
    GuidedValueKind ValueKind, string Placeholder = "",
    bool Required = true, string InitialValue = "");
public sealed record GuidedFlagChoice(string Token, string Value);
public sealed record GuidedPlan(
    string Command, CommandRisk Risk, int Operations, string Note, int TimeoutSeconds = 300);
public sealed record GuidedCommand(
    string Id, string Executable, string TitleEs, string ExplanationEs, string SearchTerms,
    CommandRisk Risk, string Template,
    IReadOnlyList<GuidedFlag> Flags,
    IReadOnlyList<GuidedOperand> Operands,
    bool SupportsBatch = false, int TimeoutSeconds = 300,
    string? BatchOperand = null, string[]? RequiredAnyFlags = null,
    string[]? BatchDisallowedFlags = null, string[]? AutoFlags = null,
    string? DocumentationUrl = null,
    string[]? PresetCommands = null,
    string[]? Platforms = null);

/// <summary>
/// Data-driven command form engine. A single renderer consumes JSON definitions
/// for UNIX tools, APT and Yocto. It never branches on a particular command ID.
/// </summary>
public static class GuidedCommandCatalog
{
    private sealed class GuideFile
    {
        public int SchemaVersion { get; set; }
        public List<GuidedCommand> Commands { get; set; } = [];
    }
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly Regex Placeholder = new(
        @"(?<!\$)\{(?<name>[a-z][a-z0-9_]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Name = new(
        @"^[a-z_][a-z0-9_-]{0,31}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Token = new(
        @"^--?[a-zA-Z0-9][a-zA-Z0-9_-]{0,64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RecipeName = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9_+.-]{0,119}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static IReadOnlyList<GuidedCommand> Load()
    {
        using var stream = typeof(GuidedCommandCatalog).Assembly
            .GetManifestResourceStream("AntiCli.GuidedCommands")
            ?? throw new InvalidDataException("Falta guided-commands.json.");
        var data = JsonSerializer.Deserialize<GuideFile>(stream, Options)
            ?? throw new InvalidDataException("El catálogo guiado no es válido.");
        if (data.SchemaVersion != 1 || data.Commands.Count == 0 ||
            data.Commands.GroupBy(x => x.Id).Any(g => g.Count() != 1))
            throw new InvalidDataException("Versión o IDs del catálogo guiado no válidos.");
        var modules = data.Commands.ToList();
        foreach (var command in modules) ValidateSchema(command);

        // User-owned JSON modules are read at startup; no recompilation required.
        // They cannot silently replace a built-in definition, and they default RED.
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SolarisPKN", "AntiCLI", "modules");
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json").Take(100))
            {
                try
                {
                    var local = JsonSerializer.Deserialize<GuideFile>(
                        File.ReadAllText(file), Options);
                    if (local?.SchemaVersion != 1 || local.Commands.Count > 200) continue;
                    foreach (var module in local.Commands)
                    {
                        if (modules.Any(x => x.Id == module.Id)) continue;
                        ValidateSchema(module);
                        modules.Add(module with { Risk = CommandRisk.Red });
                    }
                }
                catch (Exception)
                {
                    // A damaged external file cannot stop users authenticating SSH.
                    // The author can validate it independently against docs.
                }
            }
        }
        return modules;
    }

    private static void ValidateSchema(GuidedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Id) ||
            command.Template is null || command.Template.Length is < 1 or > 10000 ||
            command.Flags is null || command.Operands is null ||
            command.Flags.Count > 160 || command.Operands.Count > 32 ||
            command.TimeoutSeconds is < 1 or > 14400 ||
            command.Flags.Any(f => !Token.IsMatch(f.Token)) ||
            command.Operands.Any(o => !Regex.IsMatch(o.Name, "^[a-z][a-z0-9_]*$")))
            throw new InvalidDataException("Ficha guiada inválida: " + command.Id);
    }

    private static IReadOnlyList<GuidedCommand> Definitions = Load();

    // An editor may add or modify user-owned JSON; rebuild without restarting.
    public static void Reload()
    {
        Definitions = Load();
        All = BuildAll();
    }

    public static IReadOnlyList<GuidedCommand> ValidateExternalJson(string json)
    {
        var data = JsonSerializer.Deserialize<GuideFile>(json, Options)
            ?? throw new InvalidDataException("El archivo JSON está vacío o no es una ficha.");
        if (data.SchemaVersion != 1 || data.Commands.Count is < 1 or > 200)
            throw new InvalidDataException("Se requiere SchemaVersion 1 y entre 1 y 200 comandos.");
        if (data.Commands.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != data.Commands.Count)
            throw new InvalidDataException("Hay IDs de módulos duplicados.");
        foreach (var command in data.Commands)
        {
            ValidateSchema(command);
            if (All.Any(x => x.Id == command.Id && !command.Id.StartsWith("local.", StringComparison.Ordinal)))
                throw new InvalidDataException("El ID ya existe en el catálogo: " + command.Id +
                    ". Usá un ID nuevo con prefijo local.");
        }
        return data.Commands;
    }


    // Covers every catalogue entry even without a manually authored detailed schema:
    // generic, immediately editable form. Built-in detailed schemas take precedence.
    public static IReadOnlyList<GuidedCommand> All { get; private set; } = BuildAll();

    private static IReadOnlyList<GuidedCommand> BuildAll() =>
        Definitions.Concat(
            AdminCommandCatalog.For(TargetPlatform.OtherLinux)
                .Concat(AdminCommandCatalog.For(TargetPlatform.Windows))
                .Where(action => !Definitions.Any(d =>
                    d.PresetCommands?.Contains(action.Command, StringComparer.Ordinal) == true))
                .Select(action => new GuidedCommand(
                    "action." + action.Id, action.Command.Split(' ')[0],
                    action.LabelEs, action.DescriptionEs,
                    action.LabelEs + " " + action.DescriptionEs + " " + action.GroupName,
                    action.Risk, action.Command, [], [],
                    Platforms: action.Id.StartsWith("windows.", StringComparison.Ordinal)
                        ? ["Windows"] : ["Linux"])))
            .ToArray();

    public static GuidedCommand? Find(string id) =>
        All.FirstOrDefault(x => x.Id == id);

    public static GuidedCommand Resolve(AdminCommand action) =>
        Definitions.FirstOrDefault(d =>
            d.PresetCommands?.Contains(action.Command, StringComparer.Ordinal) == true)
        ?? All.First(x => x.Id == "action." + action.Id);

    public static bool SupportsPlatform(GuidedCommand command, TargetPlatform platform)
    {
        var family = platform == TargetPlatform.Windows ? "Windows" : "Linux";
        return command.Platforms?.Contains(family, StringComparer.OrdinalIgnoreCase)
            ?? platform != TargetPlatform.Windows;
    }

    public static IReadOnlyList<GuidedCommand> Search(string? text, TargetPlatform platform)
    {
        var terms = (text ?? "").Trim().ToLowerInvariant();
        if (terms.Length == 0) return All.Where(x => SupportsPlatform(x, platform)).Take(30).ToArray();
        var words = terms.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return All.Where(x =>
        {
            if (!SupportsPlatform(x, platform)) return false;
            var corpus = (x.TitleEs + " " + x.SearchTerms + " " + x.Executable +
                " " + x.ExplanationEs).ToLowerInvariant();
            return words.All(corpus.Contains);
        }).Take(25).ToArray();
    }

    public static GuidedPlan Build(
        GuidedCommand guide, IReadOnlyList<GuidedFlagChoice> orderedChoices,
        IReadOnlyDictionary<string, string> argumentValues,
        int quantity = 1, int firstNumber = 1, int minimumDigits = 2)
    {
        if (quantity is < 1 or > 100 || (quantity > 1 && !guide.SupportsBatch))
            throw new ArgumentException("La cantidad de operaciones debe estar entre 1 y 100 y el módulo debe admitir lotes.");
        if (firstNumber < 0 || firstNumber + quantity > 1_000_000 ||
            minimumDigits is < 1 or > 8)
            throw new ArgumentException("Numeración de lote inválida.");
        if (orderedChoices.DistinctBy(x => x.Token).Count() != orderedChoices.Count)
            throw new ArgumentException("No repitas una opción en el mismo comando.");
        if (guide.RequiredAnyFlags is { Length: > 0 } mandatory &&
            !orderedChoices.Any(x => mandatory.Contains(x.Token)))
            throw new ArgumentException("El módulo necesita una de estas opciones: " + string.Join(", ", mandatory));
        if (quantity > 1 && guide.BatchDisallowedFlags is { Length: > 0 } forbidden &&
            orderedChoices.Any(x => forbidden.Contains(x.Token)))
            throw new ArgumentException("El lote no permite las opciones: " + string.Join(", ", forbidden));

        var flags = new List<string>();
        foreach (var choice in orderedChoices)
        {
            var flag = guide.Flags.FirstOrDefault(f => f.Token == choice.Token)
                ?? throw new ArgumentException("Opción no definida: " + choice.Token);
            flags.Add(flag.ValueKind == GuidedValueKind.None ? flag.Token :
                flag.Token + " " + Format(flag.ValueKind, choice.Value));
        }
        if (guide.AutoFlags is not null)
            foreach (var implicitFlag in guide.AutoFlags)
            {
                var implicitToken = implicitFlag.Split(' ', 2)[0];
                if (!orderedChoices.Any(x => x.Token == implicitToken))
                    flags.Add(implicitFlag);
            }

        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["flags"] = string.Join(" ", flags)
        };
        foreach (var field in guide.Operands)
        {
            argumentValues.TryGetValue(field.Name, out var raw);
            raw ??= field.InitialValue;
            if (field.Required && string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Completá «" + field.LabelEs + "».");
            if (string.IsNullOrEmpty(raw) && !field.Required)
            {
                map[field.Name] = "";
                continue;
            }
            if (guide.BatchOperand == field.Name && quantity > 1)
            {
                Validate(field.ValueKind, raw);
                if (!Name.IsMatch(raw) || raw.Length > 26)
                    throw new ArgumentException("El prefijo debe ser corto y contener solo caracteres válidos de usuario.");
                map[field.Name] = "\"" + raw + "${suffix}\"";
            }
            else
                map[field.Name] = Format(field.ValueKind, raw);
        }

        var rendered = Placeholder.Replace(guide.Template, match =>
        {
            var key = match.Groups["name"].Value;
            if (!map.TryGetValue(key, out var val))
                throw new ArgumentException("Campo no definido en ficha: " + key);
            return val;
        }).Trim();
        if (rendered.Length == 0 || rendered.Length > 12000)
            throw new ArgumentException("El comando generado es inválido o demasiado extenso.");
        if (quantity == 1)
            return new GuidedPlan(rendered, guide.Risk, 1,
                guide.ExplanationEs, guide.TimeoutSeconds);

        var suffixes = Enumerable.Range(firstNumber, quantity)
            .Select(x => x.ToString(CultureInfo.InvariantCulture).PadLeft(minimumDigits, '0'))
            .ToArray();
        var batch = "for suffix in " + string.Join(" ", suffixes) +
            "; do " + rendered + " || exit $?; done";
        return new GuidedPlan(batch, guide.Risk, quantity,
            "Se generarán " + quantity + " operaciones: " + suffixes[0] +
            " a " + suffixes[^1] + ". Se detiene en el primer fallo.", guide.TimeoutSeconds);
    }

    private static string Format(GuidedValueKind kind, string value)
    {
        Validate(kind, value);
        if (kind == GuidedValueKind.PackageList)
            return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(ShellQuote));
        return ShellQuote(kind == GuidedValueKind.Fingerprint ? value.ToUpperInvariant() : value);
    }

    private static void Validate(GuidedValueKind kind, string value)
    {
        if (value.Length > 512 || value.Any(char.IsControl))
            throw new ArgumentException("Valor demasiado largo o con caracteres de control.");
        switch (kind)
        {
            case GuidedValueKind.None:
            case GuidedValueKind.Text:
                return;
            case GuidedValueKind.UserGroup:
            case GuidedValueKind.Account:
                if (!Name.IsMatch(value))
                    throw new ArgumentException("Nombre Linux inválido.");
                return;
            case GuidedValueKind.Identifier:
                if (!Regex.IsMatch(value, @"^[a-z][a-z0-9-]{0,39}$"))
                    throw new ArgumentException("Usá letras minúsculas, números o guiones.");
                return;
            case GuidedValueKind.AbsolutePath:
                if (!value.StartsWith("/", StringComparison.Ordinal) ||
                    value.Split('/').Contains(".."))
                    throw new ArgumentException("Se necesita una ruta Linux absoluta y sin '..'.");
                return;
            case GuidedValueKind.PathOrName:
                if ((!value.StartsWith("/", StringComparison.Ordinal) &&
                     !RecipeName.IsMatch(value)) || value.Split('/').Contains(".."))
                    throw new ArgumentException("Ingresá ruta absoluta o nombre simple de build.");
                return;
            case GuidedValueKind.PositiveId:
                if (!int.TryParse(value, out var id) || id is < 1 or > 60000)
                    throw new ArgumentException("Número fuera de rango.");
                return;
            case GuidedValueKind.UrlHttps:
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
                    uri.Host.Length == 0)
                    throw new ArgumentException("La URL debe ser HTTPS y no contener credenciales.");
                return;
            case GuidedValueKind.Fingerprint:
                if (!Regex.IsMatch(value, @"\A(?:[0-9A-Fa-f]{40}|[0-9A-Fa-f]{64})\z"))
                    throw new ArgumentException("Ingresá la huella GPG completa, de 40 o 64 dígitos hexadecimales.");
                return;
            case GuidedValueKind.PackageList:
            case GuidedValueKind.Components:
                if (value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is < 1 or > 50 ||
                    value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Any(x => !RecipeName.IsMatch(x)))
                    throw new ArgumentException("Lista inválida: nombres separados por espacios.");
                return;
            case GuidedValueKind.Suite:
            case GuidedValueKind.Recipe:
            case GuidedValueKind.Task:
                if (!RecipeName.IsMatch(value))
                    throw new ArgumentException("Ingresá un nombre simple de receta, suite o tarea.");
                return;
            case GuidedValueKind.CleanTask:
                if (value is not ("clean" or "cleansstate" or "cleanall"))
                    throw new ArgumentException("Elegí clean, cleansstate o cleanall.");
                return;
        }
        throw new ArgumentException("Tipo de parámetro no admitido.");
    }

    public static string ShellQuote(string text) =>
        "'" + text.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
}
