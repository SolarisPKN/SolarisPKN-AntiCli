using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Services;

public sealed class ProfileStore
{
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolarisPKN", "AntiCLI");
    private static readonly string PathToProfiles = Path.Combine(Root, "profiles.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<SshProfile> Load()
    {
        if (!File.Exists(PathToProfiles)) return [];
        var source = File.ReadAllText(PathToProfiles);
        var profiles = JsonSerializer.Deserialize<List<SshProfile>>(source, JsonOptions) ?? [];
        // Older prototype profiles contained an unencrypted UserName and PrivateKeyPath.
        // Scrub legacy fields; no persistence is allowed without the new explicit checkboxes.
        if (source.Contains("\"UserName\"", StringComparison.Ordinal)
            || source.Contains("\"PrivateKeyPath\"", StringComparison.Ordinal)
            || source.Contains("\"SecretDpapiBase64\"", StringComparison.Ordinal))
            WriteAtomic(profiles);
        return profiles;
    }

    public SshProfile Save(SshProfile profile, string user, string password, string passphrase, string? selectedKeyPath)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("La bóveda DPAPI CurrentUser requiere Windows.");
        Validate(profile, user);
        var all = Load().ToList();
        var existing = all.FirstOrDefault(p => p.Id == profile.Id);
        // Empty fields preserve prior DPAPI content only while the corresponding opt-in stays checked.
        profile.UserDpapi = profile.RememberUser
            ? ProtectTextOrPrevious(user, existing?.UserDpapi) : "";
        profile.PasswordDpapi = profile.RememberPassword
            ? ProtectTextOrPrevious(password, existing?.PasswordDpapi) : "";
        profile.PassphraseDpapi = profile.RememberPassphrase
            ? ProtectTextOrPrevious(passphrase, existing?.PassphraseDpapi) : "";
        if (profile.RememberKey)
        {
            if (!string.IsNullOrWhiteSpace(selectedKeyPath))
            {
                var keyBytes = File.ReadAllBytes(selectedKeyPath);
                try { profile.KeyDpapi = Protect(keyBytes); }
                finally { CryptographicOperations.ZeroMemory(keyBytes); }
            }
            else profile.KeyDpapi = existing?.KeyDpapi ?? "";
        }
        else profile.KeyDpapi = "";
        if (profile.RememberKey && string.IsNullOrEmpty(profile.KeyDpapi))
            throw new ArgumentException("Para recordar una clave SSH seleccioná primero su archivo privado.");

        all.RemoveAll(p => p.Id == profile.Id);
        all.Add(profile);
        WriteAtomic(all);
        return profile;
    }

    public string StoredUser(SshProfile profile) =>
        profile.RememberUser ? UnprotectText(profile.UserDpapi) : "";

    public SshCredentials Resolve(SshProfile profile, string user, string password, string passphrase, string? selectedKeyPath)
    {
        var resolvedUser = string.IsNullOrEmpty(user) ? (profile.RememberUser ? UnprotectText(profile.UserDpapi) : "") : user;
        var resolvedPassword = string.IsNullOrEmpty(password) && profile.RememberPassword
            ? UnprotectText(profile.PasswordDpapi) : password;
        var resolvedPassphrase = string.IsNullOrEmpty(passphrase) && profile.RememberPassphrase
            ? UnprotectText(profile.PassphraseDpapi) : passphrase;
        byte[]? key = null;
        if (profile.AuthMode == SshAuthMode.PrivateKey)
        {
            if (!string.IsNullOrWhiteSpace(selectedKeyPath))
                key = File.ReadAllBytes(selectedKeyPath);
            else if (profile.RememberKey && !string.IsNullOrEmpty(profile.KeyDpapi))
                key = Unprotect(profile.KeyDpapi);
            if (key is null || key.Length == 0)
                throw new InvalidOperationException("Seleccioná un archivo de clave SSH o activá 'Recordar clave SSH' antes de reconectar.");
        }
        if (string.IsNullOrWhiteSpace(resolvedUser))
            throw new InvalidOperationException("Ingresá el usuario SSH.");
        return new SshCredentials(resolvedUser, resolvedPassword, resolvedPassphrase, key);
    }

    private static string ProtectTextOrPrevious(string value, string? previous) =>
        string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(previous) ? previous : ProtectText(value);

    private static string ProtectText(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        try { return Protect(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static string UnprotectText(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var bytes = Unprotect(value);
        try { return Encoding.UTF8.GetString(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static string Protect(byte[] bytes)
    {
        // This explicit guard is required by CA1416: these helpers are also
        // reachable through Load/Resolve without first calling Save().
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("El cifrado DPAPI de credenciales requiere Windows.");
        return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
    }

    private static byte[] Unprotect(string base64)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("El descifrado DPAPI de credenciales requiere Windows.");
        return ProtectedData.Unprotect(Convert.FromBase64String(base64), null, DataProtectionScope.CurrentUser);
    }

    private static void Validate(SshProfile profile, string user)
    {
        if (string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.Host))
            throw new ArgumentException("Nombre y dirección de servidor son obligatorios.");
        if (profile.Port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(profile.Port));
        if (profile.RememberUser && string.IsNullOrEmpty(user) && string.IsNullOrEmpty(profile.UserDpapi))
            throw new ArgumentException("Escribí el usuario SSH antes de elegir recordarlo.");
    }

    private static void WriteAtomic(List<SshProfile> profiles)
    {
        Directory.CreateDirectory(Root);
        var tmp = Path.Combine(Root, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(profiles, JsonOptions), Encoding.UTF8);
            File.Move(tmp, PathToProfiles, overwrite: true);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}
