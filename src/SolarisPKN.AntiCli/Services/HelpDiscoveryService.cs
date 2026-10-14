using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Services;

// A --help flag is NOT inherently safe: this scanner must only run on an
// explicitly authorized TEST target. No arbitrary command execution or brute-force.
public sealed class HelpDiscoveryService
{
    private readonly SshProbe _ssh;
    public HelpDiscoveryService(SshProbe ssh) => _ssh = ssh;

    private static readonly Regex SafeWord = new("^[a-zA-Z][a-zA-Z0-9_.+-]{0,59}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Dictionary<string, string[]> Families =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["salt"] = ["salt", "salt-call", "salt-run", "salt-key", "salt-cp", "salt-ssh"],
            ["git"] = ["git"],
            ["docker"] = ["docker"],
            ["bitbake"] = ["bitbake"],
            ["apt"] = ["apt"]
        };

    // Only parse subcommands under a clearly marked help heading.
    private static readonly Regex Subcommand = new(
        @"^\s{2,}(?<name>[a-zA-Z][a-zA-Z0-9_-]{0,39})\s{2,}(?<desc>\S.{0,160})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<(ModuleDraft Draft, string DraftPath)> ScanAsync(
        SshProfile profile, SshCredentials credentials, string executable, Action<string> progress)
    {
        var root = (executable ?? "").Trim();
        if (!SafeWord.IsMatch(root))
            throw new ArgumentException("Indicá un ejecutable simple, sin rutas, argumentos ni caracteres de shell.");

        var draft = new ModuleDraft { Id = "discovered." + root.ToLowerInvariant(), RootCommand = root };
        var roots = Families.TryGetValue(root, out var known) ? known : [root];
        var commandsToProbe = new List<string>();
        foreach (var tool in roots) commandsToProbe.Add(tool + " --help");

        var index = 0;
        // Max 14 SSH commands, one level of proven CLI subcommands and no retries with risky flags.
        while (index < commandsToProbe.Count && index < 14)
        {
            var command = commandsToProbe[index++];
            progress("Consultando " + index + "/14: " + command);
            RemoteCommandResult result;
            try { result = await _ssh.ExecuteAsync(profile, credentials, command); }
            catch (Exception e)
            {
                // Record only the failure class; errors may contain connection metadata.
                progress("Consulta fallida (" + e.GetType().Name + ") en " + command);
                continue;
            }

            var hash = ModuleDraftStore.StoreLog(root, command, result);
            var description = TranslateSummary(command, result.ExitCode);
            draft.Commands.Add(new DiscoveredCommand
            {
                Command = command,
                DescriptionOriginal = FirstMeaningfulLine(result.StdOut),
                DescriptionEs = description,
                ExitCode = result.ExitCode,
                HelpLogSha256 = hash,
                Flags = ExtractFlags(result.StdOut).ToList()
            });

            // Only enumerate under an explicit "Commands" heading; no guessing Salt modules or
            // running "salt <target> <function>" which could change remote machines.
            if (result.ExitCode == 0 && command == root + " --help")
            {
                foreach (var child in ExtractSubcommands(result.StdOut).Take(8))
                {
                    var next = root + " " + child + " --help";
                    if (!commandsToProbe.Contains(next, StringComparer.Ordinal))
                        commandsToProbe.Add(next);
                }
            }
        }

        var draftPath = ModuleDraftStore.SaveDraft(draft);
        progress("Finalizado: " + draft.Commands.Count + " ayudas registradas. Revisión humana pendiente.");
        return (draft, draftPath);
    }

    private static string FirstMeaningfulLine(string text) =>
        text.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.Length is > 0 and < 180)
        ?? "(sin descripción en la ayuda)";

    private static IEnumerable<string> ExtractSubcommands(string help)
    {
        var headerFound = false;
        foreach (var line in help.Split('\n').Take(500))
        {
            var trimmed = line.Trim().TrimEnd(':');
            if (trimmed.Equals("commands", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("available commands", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("subcommands", StringComparison.OrdinalIgnoreCase))
            {
                headerFound = true;
                continue;
            }

            if (!headerFound) continue;
            if (line.Length == 0) continue;
            if (!char.IsWhiteSpace(line[0])) yield break;
            var found = Subcommand.Match(line);
            if (found.Success)
            {
                var word = found.Groups["name"].Value;
                if (SafeWord.IsMatch(word) && !word.Equals("help", StringComparison.OrdinalIgnoreCase))
                    yield return word;
            }
        }
    }

    // Default summary is honest: full Spanish translation is an AI-adapter task.
    private static IEnumerable<DiscoveredFlag> ExtractFlags(string help)
    {
        var added = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in help.Split('\n').Take(500))
        {
            if (line.Length < 5 || !char.IsWhiteSpace(line[0])) continue;
            var at = line.IndexOf("--", StringComparison.Ordinal);
            if (at < 0 || at > 45) continue;
            var flag = Regex.Match(line[at..], @"^--[a-zA-Z][a-zA-Z0-9-]{0,50}");
            if (!flag.Success) continue;
            var token = flag.Value;
            if (!added.Add(token)) continue;
            var after = line[(at + token.Length)..];
            var explanation = Regex.Match(after, @"\s{2,}(?<desc>\S.{2,180})$");
            var desc = explanation.Success ? explanation.Groups["desc"].Value.Trim()
                : "Descripción no identificada";
            var beforeDesc = explanation.Success ? after[..explanation.Index] : after;
            var expectsValue = Regex.IsMatch(beforeDesc, @"(?:=|<)[a-zA-Z0-9_-]+|[A-Z][A-Z0-9_-]{2,}");
            yield return new DiscoveredFlag
            {
                Token = token,
                DescriptionOriginal = desc,
                DescriptionEs = "Opción " + token + " · traducción de ayuda pendiente",
                RequiresValue = expectsValue,
                ValueHint = expectsValue ? "Requiere valor: consultar texto original" : ""
            };
            if (added.Count >= 60) yield break;
        }
    }

    private static string TranslateSummary(string command, int code)
    {
        var description = command.EndsWith("--help", StringComparison.Ordinal)
            ? "Opciones y subcomandos documentados para " + command[..^7]
            : "Ayuda del comando " + command;
        if (code != 0) description += " (salida de ayuda con error " + code + ")";
        return description + " · traducción detallada pendiente";
    }
}
