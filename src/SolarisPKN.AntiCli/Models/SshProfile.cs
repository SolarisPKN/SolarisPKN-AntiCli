namespace SolarisPKN.AntiCli.Models;

public enum TargetPlatform { Debian, Ubuntu, Windows, OtherLinux }
public enum SshAuthMode { PrivateKey, Password }

public sealed class SshProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public TargetPlatform Platform { get; set; } = TargetPlatform.Debian;
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string HostKeySha256 { get; set; } = "";
    public SshAuthMode AuthMode { get; set; } = SshAuthMode.PrivateKey;

    // Every Remember* checkbox is an independent opt-in.
    public bool RememberUser { get; set; }
    public bool RememberKey { get; set; }
    public bool RememberPassword { get; set; }
    public bool RememberPassphrase { get; set; }

    // DPAPI CurrentUser protected Base64; never serialize plain user/key/password/passphrase.
    public string UserDpapi { get; set; } = "";
    public string KeyDpapi { get; set; } = "";
    public string PasswordDpapi { get; set; } = "";
    public string PassphraseDpapi { get; set; } = "";

    public override string ToString() => $"{Name} · {Platform} · {Host}:{Port}";
}

// Transient session data: not serializable to profiles.json.
public sealed record SshCredentials(string User, string Password, string Passphrase, byte[]? KeyBytes);
