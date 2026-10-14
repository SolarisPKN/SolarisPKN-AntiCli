using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli.Services;

/// <summary>
/// Saves compact, user-local command documentation gathered from an authorized
/// SSH host. Untrusted --help text never becomes executable templates on its own.
/// </summary>
public static class GuidedHelpStore
{
    private sealed record CachedHelp(
        string Executable, DateTimeOffset CapturedUtc, string HelpSha256,
        IReadOnlyList<GuidedFlag> Flags);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly Regex SafeExecutable = new(
        @"\A[a-zA-Z][a-zA-Z0-9_.+-]{0,59}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FlagToken = new(
        @"(?<!\S)(--[a-zA-Z][a-zA-Z0-9_-]{0,64}|-[a-zA-Z0-9])(?=\W|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string Filename(string host, int port, string executable)
    {
        var id = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(host.ToLowerInvariant().Trim() + ":" + port + ":" + executable)));
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SolarisPKN", "AntiCLI", "command-help", id + ".json");
    }

    public static string ProbeCommand(string executable)
    {
        if (!SafeExecutable.IsMatch(executable))
            throw new ArgumentException("Nombre de ejecutable inválido para importar ayuda.");
        return executable + " --help";
    }

    public static IReadOnlyList<GuidedFlag> Parse(string help)
    {
        var result = new List<GuidedFlag>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in help.Split('\n').Take(700))
        {
            if (line.Length == 0 || !char.IsWhiteSpace(line[0])) continue;
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("-", StringComparison.Ordinal)) continue;
            var matches = FlagToken.Matches(trimmed);
            if (matches.Count == 0) continue;
            // Prefer GNU long option when short and long aliases appear together.
            var chosen = matches.Cast<Match>()
                .FirstOrDefault(x => x.Value.StartsWith("--", StringComparison.Ordinal))
                ?? matches[0];
            var token = chosen.Value;
            if (!seen.Add(token)) continue;

            var remainder = trimmed[(chosen.Index + chosen.Length)..];
            var description = Regex.Match(remainder, @"\s{2,}(?<desc>\S.{0,220})");
            var original = description.Success ? description.Groups["desc"].Value.Trim() :
                "Opción detectada por --help (consultá la ayuda original).";
            // For --option=VALUE or --option VALUE: add a typed value field.
            var usesValue = Regex.IsMatch(remainder,
                @"^\s*(?:=|<|\[=)[A-Za-z_<\[][a-zA-Z0-9_>.-]*",
                RegexOptions.CultureInvariant);
            result.Add(new GuidedFlag(
                token, token + " · importada", original,
                usesValue ? GuidedValueKind.Text : GuidedValueKind.None,
                usesValue ? "Valor de la opción" : ""));
            if (result.Count >= 150) break;
        }
        return result;
    }

    public static void Save(string host, int port, string executable, string help)
    {
        var flags = Parse(help);
        var record = new CachedHelp(executable, DateTimeOffset.UtcNow,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(help))), flags);
        var file = Filename(host, port, executable);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(record, Options));
            File.Move(temp, file, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static GuidedCommand Enrich(GuidedCommand guide, string host, int port)
    {
        var file = Filename(host, port, guide.Executable);
        if (!File.Exists(file)) return guide;
        try
        {
            var cached = JsonSerializer.Deserialize<CachedHelp>(File.ReadAllText(file), Options);
            if (cached is null || cached.Executable != guide.Executable)
                return guide;
            var flags = guide.Flags.ToList();
            foreach (var imported in cached.Flags)
                if (SafeExecutable.IsMatch(guide.Executable) &&
                    FlagToken.IsMatch(imported.Token) &&
                    !flags.Any(x => x.Token == imported.Token))
                    flags.Add(imported);
            var template = guide.Template.Contains("{flags}", StringComparison.Ordinal)
                ? guide.Template : guide.Template + " {flags}";
            return guide with { Flags = flags, Template = template };
        }
        catch (Exception) { return guide; } // Corrupt optional cache must not prevent SSH.
    }
}
