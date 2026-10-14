using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli.Models;

public sealed record CommandHint(string Command, string DescriptionEs, bool Approved)
{
    public override string ToString() => Command + " — " + DescriptionEs
        + (Approved ? "" : " [ayuda detectada]");
}

public static class CommandHints
{
    public static IEnumerable<CommandHint> ForPlatform(TargetPlatform platform) =>
        PlatformReadOnlyActions.For(platform)
            .Select(a => new CommandHint(a.Command, a.Label, true))
            .Concat(AdminCommandCatalog.For(platform)
                .Select(a => new CommandHint(a.Command, a.DescriptionEs, true)))
            .DistinctBy(x => x.Command, StringComparer.Ordinal);

    public static IReadOnlyList<CommandHint> Search(string? input, IEnumerable<CommandHint> candidates)
    {
        var prefix = (input ?? "").Trim();
        return candidates
            .Where(x => prefix.Length == 0 ||
                x.Command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                x.Command.Contains(prefix, StringComparison.OrdinalIgnoreCase) ||
                x.DescriptionEs.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Command, StringComparer.Ordinal)
            .Take(16).ToArray();
    }

    // AntiCLI is an administrator's console. Risk colors are advisory only.
    // Unknown commands are allowed but visually classified RED, never GREEN.
    public static bool CanRun(TargetPlatform platform, string? input) =>
        !string.IsNullOrWhiteSpace(input);
}
