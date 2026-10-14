using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Installer suggestions are based on a small reviewed package map, NOT on
/// guessed executable-to-package conversions. Operators opt in by clicking
/// install; no background installation or automatic sudo credential reuse.
/// </summary>
public partial class SessionView
{
    private sealed class InstallProviders
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, string> Mappings { get; set; } = [];
    }

    private static readonly IReadOnlyDictionary<string, string> Packages = LoadPackages();
    private string? _suggestedAptPackage;

    private static IReadOnlyDictionary<string, string> LoadPackages()
    {
        using var s = typeof(SessionView).Assembly.GetManifestResourceStream("AntiCli.InstallProviders");
        if (s is null) return new Dictionary<string, string>();
        var data = JsonSerializer.Deserialize<InstallProviders>(s);
        return data?.SchemaVersion == 1
            ? data.Mappings : new Dictionary<string, string>();
    }

    private void UpdateMissingToolPrompt()
    {
        if (MissingToolPanel is null || _activeGuide is null ||
            _connectedProfile is null) return;
        _suggestedAptPackage = null;
        MissingToolPanel.IsVisible = false;

        var exe = _activeGuide.Executable;
        if (_remoteDictionary is null)
        {
            HelpImportStatus.Text += " · Disponibilidad: todavía no se comprobó el PATH.";
            return;
        }
        if (_remoteDictionary.Has(exe)) return;

        MissingToolPanel.IsVisible = true;
        MissingToolCandidate.Text = "";
        MissingToolMessage.Text = "○ No se encontró «" + exe +
            "» en el PATH remoto. Puede estar instalado fuera del PATH, " +
            "faltarle activación del entorno o no estar instalado.";
        InstallMissingToolButton.IsVisible = false;

        if (_connectedProfile.Platform is not (TargetPlatform.Ubuntu or TargetPlatform.Debian))
        {
            MissingToolCandidate.Text = "No hay instalador APT compatible confirmado para esta distribución.";
            return;
        }
        if (!Packages.TryGetValue(exe, out var package))
        {
            MissingToolCandidate.Text = "No hay un paquete APT mapeado con seguridad para esta herramienta. " +
                "Podés instalarla manualmente usando su documentación oficial.";
            return;
        }
        if (!_remoteDictionary.Has("apt-get"))
        {
            MissingToolCandidate.Text = "No se encontró apt-get en el PATH remoto. " +
                "No se propone una instalación automática.";
            return;
        }
        _suggestedAptPackage = package;
        MissingToolCandidate.Text = "Paquete sugerido: " + package +
            ". Se instalará la versión candidata en los repositorios APT configurados " +
            "(no necesariamente la última publicada en Internet).";
        InstallMissingToolButton.IsVisible = true;
        InstallMissingToolButton.Content = "Instalar «" + package + "» desde APT";
    }

    private async void InstallMissingToolClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy || _connectedProfile is null || _connectedCredentials is null ||
            _suggestedAptPackage is null) return;
        var package = _suggestedAptPackage;
        if (!Regex.IsMatch(package, @"\A[a-z0-9][a-z0-9.+-]{0,80}\z"))
        {
            MissingToolCandidate.Text = "Identificador de paquete inválido.";
            return;
        }
        var profile = _connectedProfile;
        var credentials = _connectedCredentials;
        var sudo = UseSudoToggle.IsChecked == true;
        var rootPrefix = sudo ? "sudo -n -- " : "";
        // The package is sourced from the reviewed embedded mapping, not remote data.
        var remoteCommand = rootPrefix + "apt-get update && " + rootPrefix +
            "apt-get install -y -- " + GuidedCommandCatalog.ShellQuote(package);
        var started = DateTimeOffset.UtcNow;
        try
        {
            _busy = true;
            MissingToolCandidate.Text = "Actualizando índices e instalando " + package +
                ". El proceso puede modificar el servidor. Sin sudo requiere usuario root.";
            ExecutionStatus.Text = "Instalación APT solicitada por el administrador...";
            Output.Text = "$ " + remoteCommand + "\n\nEn curso...";
            var result = await _ssh.ExecuteAsync(profile, credentials,
                remoteCommand, TimeSpan.FromMinutes(20));
            Output.Text = "$ " + remoteCommand + "\n\n" + result.StdOut +
                (string.IsNullOrEmpty(result.StdErr) ? "" : "\n[STDERR]\n" + result.StdErr);
            RecordCommand(started, profile.Host, profile.Port, remoteCommand, sudo,
                result.ExitCode, result.StdOut, result.StdErr);
            ExecutionStatus.Text = "APT finalizó con código " + result.ExitCode;
            if (result.ExitCode == 0 && !_invalidateAfterBusy)
            {
                MissingToolCandidate.Text = "APT completó la instalación. " +
                    "Volvé a actualizar el diccionario de ejecutables para verificar la disponibilidad.";
                _suggestedAptPackage = null;
                InstallMissingToolButton.IsVisible = false;
            }
            else
                MissingToolCandidate.Text = "APT no completó la instalación. Revisá la consola " +
                    "y comprobá repositorios, dependencias y permisos sudo.";
        }
        catch (Exception ex)
        {
            RecordCommand(started, profile.Host, profile.Port, remoteCommand, sudo,
                null, "", "", ex.Message);
            MissingToolCandidate.Text = "No se pudo instalar: " + ex.Message;
        }
        finally { FinishOperation(); }
    }
}
