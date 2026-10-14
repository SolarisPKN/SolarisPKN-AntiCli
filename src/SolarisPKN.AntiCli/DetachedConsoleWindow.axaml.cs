using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Independent, read-only monitor of the original session's unified SSH log.
/// All command input remains exclusively in AntiCLI's main bottom entry module.
/// </summary>
public partial class DetachedConsoleWindow : Window
{
    private Action? _dock;

    public DetachedConsoleWindow()
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        KeyDown += HandleKeyboard;
        Closed += (_, _) =>
        {
            var callback = _dock;
            _dock = null;
            callback?.Invoke();
        };
    }

    public DetachedConsoleWindow(Action dock) : this() => _dock = dock;

    public void UpdateSnapshot(string session, string transcript, string status)
    {
        SessionLabel.Text = session;
        StatusLabel.Text = status;
        if (ConsoleHistory.Text == transcript) return;
        var nearBottom = ConsoleHistory.CaretIndex >= (ConsoleHistory.Text?.Length ?? 0) - 2;
        ConsoleHistory.Text = transcript;
        if (nearBottom)
            ConsoleHistory.CaretIndex = transcript.Length;
    }

    private async void CopyLogClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard
                ?? throw new InvalidOperationException("Portapapeles no disponible.");
            await clipboard.SetTextAsync(ConsoleHistory.Text ?? "");
        }
        catch (Exception ex) { StatusLabel.Text = "No se pudo copiar: " + ex.Message; }
    }

    private void ToggleMaximizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "Restaurar" : "Maximizar";
    }

    private void HandleKeyboard(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F11) return;
        ToggleMaximizeClicked(this, new RoutedEventArgs());
        e.Handled = true;
    }

    private void DockConsoleClicked(object? sender, RoutedEventArgs e) => Close();
}
