using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli.Services;

/// <summary>
/// Per-connection inventory of commands exposed by the current SSH user's
/// PATH. No packages are installed, no sudo is used and only command presence
/// is checked (never execute --help or --version during inventory).
/// A missing PATH entry is NOT proof the package is uninstalled.
/// </summary>
public sealed class RemoteCommandDictionary
{
    private static readonly Regex SafeName = new(
        @"\A[a-zA-Z_][a-zA-Z0-9_.+-]{0,59}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HashSet<string> _available;
    public DateTimeOffset CheckedUtc { get; } = DateTimeOffset.UtcNow;
    public int CheckedCount { get; }
    public int FoundCount => _available.Count;
    public bool IsWindows { get; }

    private RemoteCommandDictionary(IEnumerable<string> installed, int checkedCount, bool windows)
    {
        IsWindows = windows;
        CheckedCount = checkedCount;
        _available = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
    }

    public bool Has(string? command) =>
        !string.IsNullOrWhiteSpace(command) && _available.Contains(command);

    public IReadOnlyCollection<string> InstalledNames => _available;

    public static string? Root(string? command)
    {
        var src = (command ?? "").TrimStart();
        if (src.Length == 0) return null;
        // A simple first-token parse for displaying *advisory* availability.
        // It is not a shell parser: grouped commands or assignments remain unknown.
        var match = Regex.Match(src, @"\A(?:sudo\s+(?:-n\s+)?(?:--\s+)?)?(?<name>[a-zA-Z_][a-zA-Z0-9_.+-]{0,59})(?=\s|$)");
        return match.Success ? match.Groups["name"].Value : null;
    }

    public static IReadOnlyList<string> CandidateNames(TargetPlatform platform)
    {
        var names = AdminCommandCatalog.For(platform).Select(a => Root(a.Command))
            .Concat(GuidedCommandCatalog.All.Where(x => GuidedCommandCatalog.SupportsPlatform(x, platform))
                .Select(x => x.Executable))
            .Concat(platform == TargetPlatform.Windows
                ? ["cmd", "powershell", "where", "sc", "ipconfig", "whoami", "hostname"]
                : ["uname", "apt-get", "apt-cache", "dpkg", "bitbake", "bitbake-layers",
                    "devtool", "recipetool", "adduser", "useradd", "sudo", "sh", "bash",
                    "docker", "salt", "salt-call", "systemctl", "ls", "cat"]);
        return names.Where(x => x is not null && SafeName.IsMatch(x))
            .Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(300).ToArray();
    }

    /// <summary>Build a bounded and shell-safe query from locally validated names.</summary>
    public static string BuildProbe(TargetPlatform platform, IReadOnlyList<string> candidates)
    {
        if (candidates.Count is < 1 or > 300 || candidates.Any(x => !SafeName.IsMatch(x)))
            throw new ArgumentException("Diccionario inválido para comprobar herramientas SSH.");
        if (platform == TargetPlatform.Windows)
        {
            // Windows SSH default shell may be cmd.exe or PowerShell; cmd.exe
            // is normally available in either case.
            var literal = string.Join(",", candidates.Select(x => "'" + x + "'"));
            return "powershell.exe -NoProfile -NonInteractive -Command " +
                "\"$ErrorActionPreference='SilentlyContinue'; @(" + literal +
                ") | ForEach-Object { if (Get-Command -Name $_ -ErrorAction SilentlyContinue) " +
                "{ [Console]::WriteLine('ANTICLI:' + $_) } }\"";
        }
        // command -v is a shell builtin; this never runs the target binary.
        var safe = string.Join(" ", candidates.Select(x => "'" + x + "'"));
        return "for anticli_exe in " + safe +
            "; do if command -v \"$anticli_exe\" >/dev/null 2>&1; then " +
            "printf 'ANTICLI:%s\\n' \"$anticli_exe\"; fi; done";
    }

    public static RemoteCommandDictionary FromProbe(
        TargetPlatform platform, IReadOnlyList<string> candidates, RemoteCommandResult result)
    {
        if (result.ExitCode != 0)
            throw new InvalidOperationException("No se pudo consultar el PATH remoto (exit " +
                                                result.ExitCode + ").");
        var allow = new HashSet<string>(candidates, StringComparer.OrdinalIgnoreCase);
        var present = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimEnd('\r'))
            .Where(line => line.StartsWith("ANTICLI:", StringComparison.Ordinal))
            .Select(line => line["ANTICLI:".Length..])
            .Where(allow.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return new RemoteCommandDictionary(present, candidates.Count, platform == TargetPlatform.Windows);
    }
}
