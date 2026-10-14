using System.Text.Json;
using System.Text.Json.Serialization;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

public enum CommandRisk { Green, Yellow, Red }

public sealed record AdminCommand(
    string Id, string Module, string GroupName, string LabelEs,
    string Command, CommandRisk Risk, string DescriptionEs);

/// <summary>
/// All platform actions are loaded from a versioned JSON resource, not hardcoded
/// buttons. A new tool can be added by editing data, without a new UI class.
/// </summary>
public static class AdminCommandCatalog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private sealed class CatalogFile
    {
        public int SchemaVersion { get; set; }
        public List<AdminCommand> Linux { get; set; } = [];
        public List<AdminCommand> Windows { get; set; } = [];
    }

    private static readonly CatalogFile File = ReadCatalog();

    private static CatalogFile ReadCatalog()
    {
        var stream = typeof(AdminCommandCatalog).Assembly.GetManifestResourceStream("AntiCli.AdminCommands")
            ?? throw new InvalidDataException("Falta el recurso incorporado admin-commands.json.");
        using (stream)
        {
            var data = JsonSerializer.Deserialize<CatalogFile>(stream, Options)
                ?? throw new InvalidDataException("No se pudo leer el catálogo administrativo.");
            if (data.SchemaVersion != 1 || data.Linux.Count == 0)
                throw new InvalidDataException("Versión o contenido inválido de admin-commands.json.");
            if (data.Linux.Concat(data.Windows).GroupBy(x => x.Id).Any(g => g.Count() != 1))
                throw new InvalidDataException("IDs de acciones duplicados en admin-commands.json.");
            return data;
        }
    }

    public static IReadOnlyList<AdminCommand> Linux => File.Linux;

    public static IReadOnlyList<AdminCommand> For(TargetPlatform platform) =>
        platform == TargetPlatform.Windows ? File.Windows : File.Linux;

    public static AdminCommand? Find(TargetPlatform platform, string command) =>
        For(platform).FirstOrDefault(x =>
            string.Equals(x.Command, command, StringComparison.Ordinal));

    public static CommandRisk Classify(TargetPlatform platform, string? command)
    {
        var input = (command ?? "").Trim();
        if (input.Length == 0) return CommandRisk.Green;
        var known = Find(platform, input);
        if (known is not null) return known.Risk;
        if (PlatformReadOnlyActions.For(platform).Any(x =>
            string.Equals(x.Command, input, StringComparison.Ordinal)))
            return CommandRisk.Green;
        // Manual or composite commands get a conservative red indicator.
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
