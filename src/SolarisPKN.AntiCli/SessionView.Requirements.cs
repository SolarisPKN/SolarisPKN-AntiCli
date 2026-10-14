using System.Text.RegularExpressions;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli;

/// <summary>Installed APT package inventory for a guided PackageList operand.</summary>
public partial class SessionView
{
    private static readonly Regex SafePackageName = new(
        @"\A[a-zA-Z0-9][a-zA-Z0-9.+-]{0,79}\z", RegexOptions.CultureInvariant);
    private IReadOnlyList<string> _pendingRequiredPackages = [];
    private string _checkedPackageSnapshot = "";

    private string? PackageListValue()
    {
        if (_activeGuide is null) return null;
        var field = _activeGuide.Operands.FirstOrDefault(x =>
            x.ValueKind == GuidedValueKind.PackageList);
        return field is not null && _guideFields.TryGetValue(field.Name, out var input)
            ? input.Text : null;
    }

    private void UpdatePackageRequirementsPanel()
    {
        if (PackageRequirementsPanel is null) return;
        _pendingRequiredPackages = [];
        _checkedPackageSnapshot = "";
        InstallRequiredPackagesButton.IsVisible = false;
        PackageRequirementsStatus.Text = "Sin comprobar.";
        PackageRequirementsPanel.IsVisible = _connectedProfile?.Platform
                is TargetPlatform.Ubuntu or TargetPlatform.Debian &&
            _activeGuide?.Operands.Any(x => x.ValueKind == GuidedValueKind.PackageList) == true;
    }

    private static string[] ValidPackages(string? text)
    {
        var packages = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (packages.Length is < 1 or > 50 || packages.Any(x => !SafePackageName.IsMatch(x)))
            throw new ArgumentException("Completá una lista válida de 1 a 50 paquetes separados por espacios.");
        return packages;
    }

    private async void CheckPackageRequirementsClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy || _connectedProfile is null || _connectedCredentials is null) return;
        try
        {
            var packages = ValidPackages(PackageListValue());
            var snapshot = PackageListValue() ?? "";
            var profile = _connectedProfile;
            _pendingRequiredPackages = [];
            _checkedPackageSnapshot = "";
            InstallRequiredPackagesButton.IsVisible = false;
            // Query the actual package database, not the executable PATH.
            var quoted = string.Join(" ", packages.Select(GuidedCommandCatalog.ShellQuote));
            const string format = "${Package}\\t${db:Status-Abbrev}\\n";
            var probe = "dpkg-query -W -f=" + GuidedCommandCatalog.ShellQuote(format) +
                " -- " + quoted;
            _busy = true;
            PackageRequirementsStatus.Text = "Consultando los " + packages.Length +
                " paquetes en el host (sin instalar nada)...";
            var result = await _ssh.ExecuteAsync(profile, _connectedCredentials,
                probe, TimeSpan.FromSeconds(25));
            if (_invalidateAfterBusy || !ReferenceEquals(profile, _connectedProfile))
                throw new InvalidOperationException("Cambió el servidor durante la consulta.");
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.TrimEnd('\r').Split('\t');
                if (parts.Length >= 2 && parts[1].StartsWith("ii", StringComparison.Ordinal))
                    installed.Add(parts[0].Trim());
            }
            if (installed.Count == 0 && result.ExitCode != 0 &&
                result.StdErr.Contains("command not found", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("dpkg-query no está disponible en el servidor.");
            var missing = packages.Where(x => !installed.Contains(x)).ToArray();
            _pendingRequiredPackages = missing;
            _checkedPackageSnapshot = snapshot;
            PackageRequirementsStatus.Text = missing.Length == 0
                ? "✓ Están instalados los " + packages.Length +
                  " paquetes de la lista. No hace falta instalar nada."
                : "Encontrados: " + installed.Count + "/" + packages.Length +
                  ". Faltan o no figuran como instalados: " + string.Join(", ", missing) +
                  ". Revisá la versión de tu distribución antes de proceder.";
            InstallRequiredPackagesButton.IsVisible = missing.Length > 0;
            InstallRequiredPackagesButton.Content = "Instalar " + missing.Length +
                " paquete(s) faltante(s) con APT";
        }
        catch (Exception ex)
        {
            _pendingRequiredPackages = [];
            InstallRequiredPackagesButton.IsVisible = false;
            PackageRequirementsStatus.Text = "No se pudo comprobar: " + ex.Message;
        }
        finally { FinishOperation(); }
    }

    private async void InstallRequiredPackagesClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy || _pendingRequiredPackages.Count == 0) return;
        if (!string.Equals(_checkedPackageSnapshot, PackageListValue(), StringComparison.Ordinal))
        {
            PackageRequirementsStatus.Text = "La lista cambió. Comprobá nuevamente los paquetes antes de instalar.";
            _pendingRequiredPackages = [];
            InstallRequiredPackagesButton.IsVisible = false;
            return;
        }
        var quoted = string.Join(" ", _pendingRequiredPackages
            .Select(GuidedCommandCatalog.ShellQuote));
        var script = "apt-get update && apt-get install -y -- " + quoted;
        PackageRequirementsStatus.Text = "Iniciando APT. Usá sudo si tu usuario remoto no es root. " +
            "Se instalarán versiones disponibles en los repositorios configurados.";
        await ExecuteApprovedCommandAsync(script);
        if (ExecutionStatus.Text?.Contains("Código de salida: 0", StringComparison.Ordinal) == true)
        {
            PackageRequirementsStatus.Text = "APT informó éxito. Volvé a comprobar " +
                "la lista para verificar que todos los paquetes figuren instalados.";
            _pendingRequiredPackages = [];
            InstallRequiredPackagesButton.IsVisible = false;
        }
    }
}
