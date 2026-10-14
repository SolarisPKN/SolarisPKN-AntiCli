using System.Security.Cryptography;
using Renci.SshNet;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Services;

public sealed record RemoteCommandResult(string StdOut, string StdErr, int ExitCode);

/// <summary>The host key presented during an unauthenticated SSH key exchange.</summary>
public sealed record DiscoveredHostKey(string Host, int Port, string Algorithm, string Sha256);

public sealed class SshProbe
{
    /// <summary>
    /// Collect ONLY the public host key. Reject the key-exchange result unconditionally;
    /// hence SSH user authentication is never attempted and no credentials are transmitted.
    /// </summary>
    public Task<DiscoveredHostKey> DiscoverHostKeyAsync(string host, int port) => Task.Run(() =>
    {
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
            throw new ArgumentException("Ingresá un host y un puerto SSH válidos.");
        var endpoint = host.Trim();
        // A fabricated fixed username, with NO password, key or user-provided credentials.
        const string probeUser = "anticli-hostkey-discovery";
        var info = new ConnectionInfo(endpoint, port, probeUser, new NoneAuthenticationMethod(probeUser))
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        using var client = new SshClient(info);
        DiscoveredHostKey? discovered = null;
        client.HostKeyReceived += (_, e) =>
        {
            var digest = SHA256.HashData(e.HostKey);
            discovered = new DiscoveredHostKey(endpoint, port, e.HostKeyName,
                "SHA256:" + Convert.ToBase64String(digest).TrimEnd('='));
            // CRITICAL: Do not accept even if the SSH library would otherwise allow it.
            // This aborts the handshake before ANY authentication method is attempted.
            e.CanTrust = false;
        };
        try { client.Connect(); }
        catch (Exception) when (discovered is not null)
        {
            // The rejection after HostKeyReceived is EXPECTED; report only the observed key.
        }
        if (discovered is null)
            throw new InvalidOperationException(
                "El servidor no entregó una clave SSH. Comprobá IP, puerto y servicio SSH.");
        return discovered;
    });


    public Task<string> AuthenticateAsync(SshProfile profile, SshCredentials credentials) =>
        Task.Run(() => WithConnection(profile, credentials, client =>
        {
            var probe = profile.Platform == TargetPlatform.Windows
                ? "cmd /c ver" : "cat /etc/os-release";
            using var test = client.CreateCommand(probe);
            test.CommandTimeout = TimeSpan.FromSeconds(20);
            var output = test.Execute();
            if (test.ExitStatus != 0)
                throw new InvalidOperationException("No se pudo verificar el entorno remoto configurado.");
            if (profile.Platform == TargetPlatform.Windows)
            {
                if (!output.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El equipo no respondió como Windows.");
                return "Windows";
            }

            var identity = ParseOsId(output);
            if (string.IsNullOrWhiteSpace(identity))
                throw new InvalidOperationException("No se detectó el ID del sistema Linux remoto.");
            if (profile.Platform == TargetPlatform.Debian && identity != "debian"
                || profile.Platform == TargetPlatform.Ubuntu && identity != "ubuntu")
                throw new InvalidOperationException($"El servidor usa {identity}, pero elegiste {profile.Platform}. Revisá la plataforma.");
            return identity;
        }));

    public Task<RemoteCommandResult> ExecuteAsync(
        SshProfile profile, SshCredentials credentials, string command, TimeSpan? timeout = null) =>
        Task.Run(() => WithConnection(profile, credentials, client =>
        {
            using var remote = client.CreateCommand(command);
            // Scanned --help stays bounded (20 s), explicit admin commands can
            // take longer (APT, Docker etc.) without blocking the UI thread.
            remote.CommandTimeout = timeout ?? TimeSpan.FromSeconds(20);
            var stdout = remote.Execute();
            return new RemoteCommandResult(Truncate(stdout), Truncate(remote.Error), remote.ExitStatus ?? -1);
        }));

    private static T WithConnection<T>(SshProfile profile, SshCredentials credentials, Func<SshClient, T> execute)
    {
        // A previously trusted key is authoritative and cannot be bypassed by changing
        // the editable fingerprint field, a cloned tab, or an old saved profile.
        var trusted = new TrustedHostKeyStore().Find(profile.Host, profile.Port);
        if (trusted is not null &&
            !string.Equals(trusted.Sha256, profile.HostKeySha256, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "La huella del perfil difiere de la clave confiable para este host:puerto. " +
                "Revisá el cambio desde el botón SSH antes de autenticar.");
        if (string.IsNullOrWhiteSpace(profile.HostKeySha256))
            throw new InvalidOperationException("Antes de autenticar, verificá la clave SSH del servidor.");
        using var keyStream = credentials.KeyBytes is null ? null : new MemoryStream(credentials.KeyBytes, false);
        AuthenticationMethod auth = profile.AuthMode switch
        {
            SshAuthMode.Password => new PasswordAuthenticationMethod(credentials.User, credentials.Password),
            SshAuthMode.PrivateKey => new PrivateKeyAuthenticationMethod(
                credentials.User,
                string.IsNullOrEmpty(credentials.Passphrase)
                    ? new PrivateKeyFile(keyStream ?? throw new InvalidOperationException("Falta la clave SSH"))
                    : new PrivateKeyFile(keyStream ?? throw new InvalidOperationException("Falta la clave SSH"),
                        credentials.Passphrase)),
            _ => throw new NotSupportedException("Autenticación SSH no soportada")
        };
        var info = new ConnectionInfo(profile.Host, profile.Port, credentials.User, auth)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        using var client = new SshClient(info);
        var fingerprint = "";
        var verified = false;
        client.HostKeyReceived += (_, e) =>
        {
            var digest = SHA256.HashData(e.HostKey);
            fingerprint = "SHA256:" + Convert.ToBase64String(digest).TrimEnd('=');
            verified = MatchesPin(profile.HostKeySha256, digest) &&
                (trusted is null || string.Equals(trusted.Algorithm, e.HostKeyName, StringComparison.Ordinal));
            e.CanTrust = verified;
        };
        try { client.Connect(); }
        catch (Exception ex) when (!verified && fingerprint.Length > 0)
        {
            throw new InvalidOperationException(
                $"La huella del servidor no coincide. Recibida: {fingerprint}. Verificá por otro canal antes de guardarla.", ex);
        }
        if (!client.IsConnected || !verified)
            throw new InvalidOperationException("Autenticación SSH fallida o huella del host no verificada.");
        try { return execute(client); }
        finally { if (client.IsConnected) client.Disconnect(); }
    }

    private static bool MatchesPin(string? expected, byte[] actual)
    {
        var value = (expected ?? "").Trim();
        if (!value.StartsWith("SHA256:", StringComparison.Ordinal)) return false;
        try
        {
            var raw = value["SHA256:".Length..];
            var pinned = Convert.FromBase64String(raw.PadRight(((raw.Length + 3) / 4) * 4, '='));
            return pinned.Length == actual.Length && CryptographicOperations.FixedTimeEquals(pinned, actual);
        }
        catch (FormatException) { return false; }
    }

    private static string ParseOsId(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("ID=", StringComparison.Ordinal))
                return line[3..].Trim().Trim('"', '\'').ToLowerInvariant();
        }
        return "";
    }

    private static string Truncate(string? output) =>
        output is null ? "" : output.Length > 32000 ? output[..32000] + "\n[Salida truncada]" : output;
}
