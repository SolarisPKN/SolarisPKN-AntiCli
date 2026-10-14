using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli.Services;

public sealed record SmartCliSuggestion(
    string Replacement, string Label, string Detail,
    string CommandName, bool IsCorrection, bool? Installed,
    string Kind)
{
    public override string ToString() =>
        (IsCorrection ? "✏ " : "") +
        (Installed == true ? "✓ " : Installed == false ? "○ " : "· ") +
        Label + " — " + Detail;
}

/// <summary>
/// Conservative fuzzy completion for remote CLI commands and documented flags.
/// Never executes commands and NEVER silently rewrites input.
/// Tab/Enter apply only the option explicitly selected by the user.
/// </summary>
public static class SmartCliCompletion
{
    private static readonly Regex Identifier = new(
        @"\A[a-zA-Z_][a-zA-Z0-9_.+-]*\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<SmartCliSuggestion> Search(
        string? input, TargetPlatform platform, RemoteCommandDictionary? remote,
        IEnumerable<CommandHint>? learned = null)
    {
        var query = input ?? "";
        var leading = query.Length - query.TrimStart().Length;
        var text = query[leading..];
        if (text.Contains('\n') || text.Contains('\r')) return [];

        var known = RemoteCommandDictionary.CandidateNames(platform);
        // Built-in command documentation may include additional module executables.
        var guides = GuidedCommandCatalog.All
            .Where(x => GuidedCommandCatalog.SupportsPlatform(x, platform))
            .ToArray();
        var names = known.Concat(guides.Select(x => x.Executable))
            .Concat((learned ?? []).Select(x => RemoteCommandDictionary.Root(x.Command)))
            .Where(s => !string.IsNullOrEmpty(s) && Identifier.IsMatch(s))
            .Select(s => s!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var first = Regex.Match(text, @"\A(?<root>[a-zA-Z_][a-zA-Z0-9_.+-]*)(?<tail>.*)\z",
            RegexOptions.Singleline);
        if (!first.Success) return [];
        var root = first.Groups["root"].Value;
        var tail = first.Groups["tail"].Value;

        var knownRoot = names.Any(n => n.Equals(root, StringComparison.OrdinalIgnoreCase));
        if (knownRoot && tail.Length > 0)
            return SuggestOptions(query, platform, remote, guides, root, tail, learned);

        var commandCandidates = names
            .Select(name =>
            {
                var similarity = FuzzyDistance(root, name);
                var prefix = name.StartsWith(root, StringComparison.OrdinalIgnoreCase);
                var contains = name.Contains(root, StringComparison.OrdinalIgnoreCase);
                var exact = name.Equals(root, StringComparison.OrdinalIgnoreCase);
                var maxTypo = Math.Max(root.Length, name.Length) switch
                {
                    <= 3 => 1,
                    <= 6 => 2,
                    <= 10 => 4,
                    _ => 5
                };
                if (!exact && !prefix && !contains && similarity > maxTypo)
                    return (name, score: int.MaxValue, exact, prefix, similarity);
                var installed = remote?.Has(name);
                var score = exact ? -100 : prefix ? 0 : contains ? 15 : 30 + similarity * 4;
                if (installed == true) score -= 10;
                else if (remote is not null) score += 10;
                return (name, score, exact, prefix, similarity);
            })
            .Where(x => x.score != int.MaxValue)
            .OrderBy(x => x.score).ThenBy(x => x.similarity)
            .ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase)
            .Take(14)
            .Select(x =>
            {
                var guide = guides.FirstOrDefault(g =>
                    g.Executable.Equals(x.name, StringComparison.OrdinalIgnoreCase));
                var action = AdminCommandCatalog.For(platform).FirstOrDefault(a =>
                    RemoteCommandDictionary.Root(a.Command)?.Equals(x.name, StringComparison.OrdinalIgnoreCase) == true);
                var corrected = !x.exact;
                var present = remote is null ? null : remote.Has(x.name);
                var detail = guide?.TitleEs ?? action?.LabelEs ?? "Comando disponible en el catálogo";
                return new SmartCliSuggestion(
                    query[..leading] + x.name + tail,
                    x.name, detail, x.name, corrected, present, "ejecutable");
            }).ToArray();
        return commandCandidates;
    }

    private static IReadOnlyList<SmartCliSuggestion> SuggestOptions(
        string query, TargetPlatform platform, RemoteCommandDictionary? remote,
        IReadOnlyList<GuidedCommand> guides, string root, string tail,
        IEnumerable<CommandHint>? learned)
    {
        var guide = guides.FirstOrDefault(g => g.Executable.Equals(root, StringComparison.OrdinalIgnoreCase));
        var options = (guide?.Flags ?? [])
            .Select(x => (Token:x.Token, Description:x.DescriptionEs))
            .Concat((learned ?? []).Where(x =>
                x.Command.StartsWith(root + " ", StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var s = x.Command[(root.Length + 1)..].Split(' ', 2)[0];
                    return (Token:s, Description:x.DescriptionEs);
                }))
            .Where(x => x.Token.StartsWith("-", StringComparison.Ordinal))
            .DistinctBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var trimmedTail = tail.TrimEnd();
        var boundary = trimmedTail.LastIndexOfAny([' ', '\t']);
        var typed = trimmedTail[(boundary + 1)..];
        var prefix = query[..(query.Length - typed.Length)];
        // Offer flags only when operator is at a flag position.
        if (typed.Length > 0 && !typed.StartsWith("-", StringComparison.Ordinal))
        {
            // Subcommand correction, e.g. "apt-get instal" -> "apt-get install".
            var variants = AdminCommandCatalog.For(platform)
                .Select(x => x.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length > 1 && x[0].Equals(root, StringComparison.OrdinalIgnoreCase))
                .Select(x => x[1]).Where(x => Identifier.IsMatch(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return RankTokens(variants.Select(x => (Token:x, Description:"Subcomando")),
                typed, prefix, root, remote, "subcomando");
        }
        if (typed.Length == 0 && !tail.EndsWith(' ') && !tail.EndsWith('\t')) return [];
        return RankTokens(options, typed, prefix, root, remote, "flag");
    }

    private static IReadOnlyList<SmartCliSuggestion> RankTokens(
        IEnumerable<(string Token,string Description)> options,
        string typed, string prefix, string root,
        RemoteCommandDictionary? remote, string kind)
    {
        return options.Select(o =>
            {
                var d = FuzzyDistance(typed, o.Token);
                var starts = o.Token.StartsWith(typed, StringComparison.OrdinalIgnoreCase);
                var max = Math.Max(typed.Length, o.Token.Length);
                var fuzzy = typed.Length > 0 && d <= (max <= 6 ? 2 : 4);
                return (o.Token,o.Description,score:starts ? 0 : fuzzy ? 10 + d : int.MaxValue);
            })
            .Where(x => x.score != int.MaxValue)
            .OrderBy(x => x.score).ThenBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .Select(x => new SmartCliSuggestion(prefix + x.Token,
                x.Token, x.Description, root,
                !x.Token.Equals(typed, StringComparison.OrdinalIgnoreCase),
                remote is null ? null : remote.Has(root), kind))
            .ToArray();
    }

    public static int FuzzyDistance(string left, string right)
    {
        left = left.ToLowerInvariant();
        right = right.ToLowerInvariant();
        var rows = left.Length + 1;
        var cols = right.Length + 1;
        var d = new int[rows, cols];
        for (var i = 0; i < rows; i++) d[i,0] = i;
        for (var j = 0; j < cols; j++) d[0,j] = j;
        for (var i = 1; i < rows; i++)
        for (var j = 1; j < cols; j++)
        {
            var cost = left[i-1] == right[j-1] ? 0 : 1;
            var value = Math.Min(d[i-1,j] + 1,
                Math.Min(d[i,j-1] + 1,d[i-1,j-1] + cost));
            if (i > 1 && j > 1 && left[i-1] == right[j-2] && left[i-2] == right[j-1])
                value = Math.Min(value, d[i-2,j-2] + 1);
            d[i,j] = value;
        }
        return d[left.Length, right.Length];
    }
}
