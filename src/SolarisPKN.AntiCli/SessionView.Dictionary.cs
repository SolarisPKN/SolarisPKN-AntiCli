using Avalonia.Controls;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// The dictionary belongs to THIS authenticated SSH session; it is never
/// copied between unrelated hosts or considered an authorization decision.
/// </summary>
public partial class SessionView
{
    private RemoteCommandDictionary? _remoteDictionary;

    private string AvailabilityMark(string? command)
    {
        if (_remoteDictionary is null) return "?";
        var name = RemoteCommandDictionary.Root(command);
        return name is null ? "?" : _remoteDictionary.Has(name) ? "✓" : "○";
    }

    private void UpdateRemoteDictionaryStatus(string? text = null)
    {
        if (InstalledDictionaryStatus is null) return;
        InstalledDictionaryStatus.Text = text ??
            (_remoteDictionary is null
                ? "Herramientas remotas: sin comprobar"
                : "Herramientas detectadas en PATH: " + _remoteDictionary.FoundCount +
                  "/" + _remoteDictionary.CheckedCount +
                  " · " + _remoteDictionary.CheckedUtc.ToLocalTime().ToString("HH:mm:ss"));
    }

    private async Task RefreshRemoteDictionaryAsync(SshProfile profile, SshCredentials credentials)
    {
        _remoteDictionary = null;
        UpdateRemoteDictionaryStatus("Consultando herramientas disponibles en el PATH remoto...");
        var candidates = RemoteCommandDictionary.CandidateNames(profile.Platform);
        if (candidates.Count == 0)
        {
            UpdateRemoteDictionaryStatus("No hay herramientas en el diccionario para este sistema.");
            return;
        }
        try
        {
            var query = RemoteCommandDictionary.BuildProbe(profile.Platform, candidates);
            var result = await _ssh.ExecuteAsync(profile, credentials, query, TimeSpan.FromSeconds(15));
            // A profile/connection may be invalidated while SSH is in flight.
            if (!ReferenceEquals(profile, _connectedProfile)) return;
            _remoteDictionary = RemoteCommandDictionary.FromProbe(profile.Platform, candidates, result);
            UpdateRemoteDictionaryStatus();
        }
        catch (Exception e)
        {
            // Inventory is purely advisory. A probe failure never disables
            // manually entered commands or a previously successful SSH login.
            if (ReferenceEquals(profile, _connectedProfile))
                UpdateRemoteDictionaryStatus("Detección no disponible (" +
                    e.GetType().Name + "). Podés seguir ejecutando comandos.");
        }
        if (ReferenceEquals(profile, _connectedProfile))
        {
            PopulateActionButtons();
            ReloadSuggestions();
            PopulateUniversalResults();
            UpdateMissingToolPrompt();
        }
    }

    private async void RefreshDictionaryClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy || _connectedProfile is null || _connectedCredentials is null) return;
        _busy = true;
        try
        {
            await RefreshRemoteDictionaryAsync(_connectedProfile, _connectedCredentials);
        }
        finally { FinishOperation(); }
    }
}
