using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

public enum CommandRisk { Green, Yellow, Red }

public sealed record AdminCommand(
    string Id, string Module, string GroupName, string LabelEs,
    string Command, CommandRisk Risk, string DescriptionEs);

public sealed class AdminCommandData
{
    public int SchemaVersion { get; set; }
    public List<AdminCommand> Linux { get; set; } = [];
    public List<AdminCommand> Windows { get; set; } = [];
}

/// <summary>
/// All catalog entries live in Data/admin-commands.json and ship as an
/// embedded resource. Adding a task NEVER requires modifying the UI.
/// </summary>
public static class AdminCommandCatalog
{
    private static readonly Lazy<AdminCommandData> Data = new(Load);

    public static IReadOnlyList<AdminCommand> Linux => Data.Value.Linux;

    private static AdminCommandData Load()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("AntiCli.AdminCommands")
            ?? throw new InvalidOperationException("No se encontró el catálogo de comandos integrado.");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var loaded = JsonSerializer.Deserialize<AdminCommandData>(stream, options)
            ?? throw new InvalidDataException("El catálogo de comandos está vacío.");
        if (loaded.SchemaVersion != 1)
            throw new InvalidDataException("Versión de catálogo incompatible.");
        if (loaded.Linux.Concat(loaded.Windows).Any(x =>
                string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Command) ||
                string.IsNullOrWhiteSpace(x.LabelEs)))
            throw new InvalidDataException("Hay acciones incompletas en el catálogo.");
        if (loaded.Linux.Concat(loaded.Windows).Select(x => x.Id).Distinct().Count() !=
            loaded.Linux.Count + loaded.Windows.Count)
            throw new InvalidDataException("Hay IDs de acciones duplicados.");
        return loaded;
    }

    public static IReadOnlyList<AdminCommand> For(TargetPlatform platform) =>
        platform == TargetPlatform.Windows ? Data.Value.Windows : Data.Value.Linux;

    public static AdminCommand? Find(TargetPlatform platform, string command) =>
        For(platform).FirstOrDefault(x =>
            string.Equals(x.Command, command, StringComparison.Ordinal));

    public static CommandRisk Classify(TargetPlatform platform, string? command)
    {
        var text = (command ?? "").Trim();
        if (text.Length == 0) return CommandRisk.Green;
        var known = Find(platform, text);
        if (known is not null) return known.Risk;
        if (PlatformReadOnlyActions.For(platform).Any(x =>
            string.Equals(x.Command, text, StringComparison.Ordinal)))
            return CommandRisk.Green;
        return CommandRisk.Red;
    }

    public static string Dot(CommandRisk risk) => risk switch
    {
        CommandRisk.Green => "🟢",
        CommandRisk.Yellow => "🟡",
        _ => "🔴"
    };

    public static string RiskName(CommandRisk risk) => risk switch
    {
        CommandRisk.Green => "Bajo / lectura",
        CommandRisk.Yellow => "Precaución / cambios limitados o salida sensible",
        _ => "Alto / cambios importantes o comando no catalogado"
    };
}
