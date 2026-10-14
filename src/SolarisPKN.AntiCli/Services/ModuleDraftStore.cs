using System.Text;
using System.Text.Json;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Services;

public static class ModuleDraftStore
{
    private static readonly string RootPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SolarisPKN", "AntiCLI", "discovery");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string StoreLog(string root, string command, RemoteCommandResult result)
    {
        Directory.CreateDirectory(RootPath);
        var id = Guid.NewGuid().ToString("N");
        var path = Path.Combine(RootPath, id + ".help.log");
        var content = "Root: " + root + "\nCommand: " + command +
            "\nUTC: " + DateTimeOffset.UtcNow.ToString("O") +
            "\nExitCode: " + result.ExitCode +
            "\n[STDOUT]\n" + result.StdOut + "\n[STDERR]\n" + result.StdErr;
        File.WriteAllText(path, content, Encoding.UTF8);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    public static string SaveDraft(ModuleDraft draft)
    {
        Directory.CreateDirectory(RootPath);
        var name = "module-" + draft.RootCommand + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            + "-" + Guid.NewGuid().ToString("N")[..8] + ".json";
        var path = Path.Combine(RootPath, name);
        File.WriteAllText(path, JsonSerializer.Serialize(draft, Options), Encoding.UTF8);
        return path;
    }

    public static IReadOnlyList<CommandHint> LoadHints()
    {
        if (!Directory.Exists(RootPath)) return [];
        var hints = new List<CommandHint>();
        foreach (var file in Directory.EnumerateFiles(RootPath, "module-*.json").TakeLast(30))
        {
            try
            {
                var item = JsonSerializer.Deserialize<ModuleDraft>(File.ReadAllText(file));
                if (item is null || item.Status != "draft-unreviewed") continue;
                foreach (var c in item.Commands.Take(40))
                {
                    if (c.Command.Length is > 0 and < 256)
                        hints.Add(new CommandHint(c.Command, c.DescriptionEs, true));
                    var prefix = c.Command.EndsWith(" --help", StringComparison.Ordinal)
                        ? c.Command[..^7] : c.Command;
                    foreach (var flag in c.Flags.Take(60))
                        if (flag.Token.StartsWith("--", StringComparison.Ordinal))
                            hints.Add(new CommandHint(prefix + " " + flag.Token,
                                flag.DescriptionEs + (flag.RequiresValue ? " [valor]" : ""), true));
                }
            }
            catch (Exception) { /* Ignore corrupt drafts; never execute their content. */ }
        }
        return hints.DistinctBy(x => x.Command).Take(200).ToArray();
    }
}
