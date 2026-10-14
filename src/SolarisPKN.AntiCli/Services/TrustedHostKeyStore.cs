using System.Security.Cryptography;
using System.Text.Json;

namespace SolarisPKN.AntiCli.Services;

/// <summary>
/// A verified SSH host-key pin is scoped to host AND port, not to an SSH username.
/// No passwords, user private keys or authentication material are stored here.
/// </summary>
public sealed record TrustedHostKey(
    string Host, int Port, string Algorithm, string Sha256, DateTimeOffset AcceptedUtc);

public sealed class TrustedHostKeyStore
{
    private static readonly object Sync = new();
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SolarisPKN", "AntiCLI", "known-hosts.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string NormalizeHost(string host) => host.Trim().ToLowerInvariant();

    public TrustedHostKey? Find(string host, int port)
    {
        lock (Sync)
        {
            return Load().FirstOrDefault(x =>
                x.Port == port && string.Equals(x.Host, NormalizeHost(host), StringComparison.Ordinal));
        }
    }

    public void TrustFirst(string host, int port, string algorithm, string sha256)
    {
        Validate(host, port, algorithm, sha256);
        lock (Sync)
        {
            var pins = Load();
            var normalized = NormalizeHost(host);
            var existing = pins.FirstOrDefault(x => x.Host == normalized && x.Port == port);
            if (existing is not null)
            {
                if (existing.Sha256 != sha256 || existing.Algorithm != algorithm)
                    throw new InvalidOperationException(
                        "Ya existe una huella distinta guardada para este host y puerto. " +
                        "No se puede sobrescribir desde la primera conexión.");
                return;
            }
            pins.Add(new TrustedHostKey(normalized, port, algorithm, sha256, DateTimeOffset.UtcNow));
            WriteAtomic(pins);
        }
    }

    /// <summary>Only called by the explicit host-key ROTATION confirmation dialog.</summary>
    public void ReplaceAfterReview(
        string host, int port, TrustedHostKey expectedOld, string algorithm, string sha256)
    {
        Validate(host, port, algorithm, sha256);
        lock (Sync)
        {
            var pins = Load();
            var normalized = NormalizeHost(host);
            var current = pins.FirstOrDefault(x => x.Host == normalized && x.Port == port);
            if (current is null || current.Sha256 != expectedOld.Sha256 ||
                current.Algorithm != expectedOld.Algorithm)
                throw new InvalidOperationException(
                    "La clave guardada cambió durante la revisión. Reiniciá la verificación.");
            pins.Remove(current);
            pins.Add(new TrustedHostKey(normalized, port, algorithm, sha256, DateTimeOffset.UtcNow));
            WriteAtomic(pins);
        }
    }

    private static void Validate(string host, int port, string algorithm, string sha256)
    {
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(algorithm) || algorithm.Length > 128)
            throw new ArgumentException("Host, puerto o algoritmo SSH inválido.");
        if (sha256 is null || !sha256.StartsWith("SHA256:", StringComparison.Ordinal))
            throw new ArgumentException("La huella SSH debe empezar por SHA256:");
        try
        {
            var raw = sha256["SHA256:".Length..];
            var bytes = Convert.FromBase64String(raw.PadRight(((raw.Length + 3) / 4) * 4, '='));
            if (bytes.Length != 32) throw new FormatException();
        }
        catch (FormatException) { throw new ArgumentException("La huella SSH SHA256 es inválida."); }
    }

    private static List<TrustedHostKey> Load()
    {
        if (!File.Exists(FilePath)) return [];
        return JsonSerializer.Deserialize<List<TrustedHostKey>>(File.ReadAllText(FilePath), JsonOptions)
            ?? throw new InvalidDataException("La base local de claves SSH está dañada.");
    }

    private static void WriteAtomic(List<TrustedHostKey> pins)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var temp = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(pins, JsonOptions));
            File.Move(temp, FilePath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
