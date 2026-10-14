using Avalonia.Controls;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>Explicit human decision, never auto-trust from a discovery event.</summary>
public partial class HostKeyApprovalWindow : Window
{
    public HostKeyApprovalWindow(DiscoveredHostKey candidate, TrustedHostKey? previous)
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        Endpoint.Text = candidate.Host + ":" + candidate.Port;
        Algorithm.Text = candidate.Algorithm;
        Fingerprint.Text = candidate.Sha256;
        if (previous is not null)
        {
            Heading.Text = "⚠ CAMBIO DE CLAVE SSH";
            PreviousLabel.IsVisible = true;
            PreviousFingerprint.IsVisible = true;
            PreviousFingerprint.Text = previous.Algorithm + " · " + previous.Sha256;
            WarningText.Text =
                "¡ALERTA! La clave de este servidor CAMBIÓ. Podría ser una reinstalación o un ataque. " +
                "No la reemplaces sin comprobar la NUEVA huella por un canal independiente (por ejemplo, la consola del proveedor).";
            Acknowledged.Content = "Verifiqué la nueva huella por un canal independiente y autorizo reemplazar la clave.";
            ConfirmButton.Content = "Reemplazar clave verificada";
        }
        else
        {
            Acknowledged.Content = "Entiendo el riesgo de confiar en esta primera conexión (TOFU) si no verifiqué la clave por otro canal.";
        }
    }

    private void AcknowledgementChanged(object? sender, RoutedEventArgs e) =>
        ConfirmButton.IsEnabled = Acknowledged.IsChecked == true;

    private void ConfirmClicked(object? sender, RoutedEventArgs e) =>
        Close(Acknowledged.IsChecked == true);

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(false);
}
