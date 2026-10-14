using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// A detached view of ONE SessionView's existing SSH command output and journal.
/// No duplicate connection, credentials or command history are created.
/// </summary>
public partial class DetachedConsoleWindow : Window
{
    private Func<string, bool, Task>? _execute;
    private Action? _dock;
    private TargetPlatform _platform = TargetPlatform.OtherLinux;
    private bool _connected;
    private bool _hostBusy;
    private bool _executing;

    // Avalonia needs a parameterless constructor to load this XAML.
    public DetachedConsoleWindow()
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        KeyDown += HandleKeyboard;
        Closed += (_, _) =>
        {
            _execute = null;
            var dock = _dock;
            _dock = null;
            dock?.Invoke();
        };
    }

    public DetachedConsoleWindow(Func<string, bool, Task> execute, Action dock) : this()
    {
        _execute = execute;
        _dock = dock;
    }

    public void SetInitialInput(string text, bool sudo)
    {
        ConsoleCommand.Text = text;
        ConsoleSudo.IsChecked = sudo;
    }

    /// <summary>The source controls remain owned by the original SessionView.</summary>
    public void UpdateSnapshot(string session, string output, string history, string status,
        bool connected, TargetPlatform platform, bool hostBusy)
    {
        SessionLabel.Text = session;
        ConsoleOutput.Text = output;
        ConsoleHistory.Text = history;
        StatusLabel.Text = status;
        _platform = platform;
        _connected = connected;
        _hostBusy = hostBusy;
        ConsoleSudo.IsVisible = platform != TargetPlatform.Windows;
        UpdateCommandRisk();
    }

    private void CommandTextChanged(object? sender, TextChangedEventArgs e) => UpdateCommandRisk();
    private void SudoChanged(object? sender, RoutedEventArgs e) => UpdateCommandRisk();

    private void UpdateCommandRisk()
    {
        if (ConsoleExecute is null || ConsoleCommand is null || RiskLabel is null ||
            ConsoleSudo is null) return;
        var command = (ConsoleCommand.Text ?? "").Trim();
        var risk = AdminCommandCatalog.Classify(_platform, command);
        if (ConsoleSudo.IsChecked == true && _platform != TargetPlatform.Windows &&
            risk == CommandRisk.Green)
            risk = CommandRisk.Yellow;
        var dot = AdminCommandCatalog.Dot(risk);
        RiskLabel.Text = dot + " " + AdminCommandCatalog.RiskName(risk);
        ConsoleExecute.Content = dot + " Ejecutar" +
            (ConsoleSudo.IsChecked == true && _platform != TargetPlatform.Windows ? " con sudo" : "");
        ConsoleExecute.Background = risk switch
        {
            CommandRisk.Green => Brushes.SeaGreen,
            CommandRisk.Yellow => Brushes.Goldenrod,
            _ => Brushes.Firebrick
        };
        ConsoleExecute.Foreground = risk == CommandRisk.Yellow ? Brushes.Black : Brushes.White;
        ConsoleExecute.IsEnabled = _connected && !_hostBusy && !_executing && command.Length > 0;
    }

    private async void ExecuteClicked(object? sender, RoutedEventArgs e)
    {
        if (_execute is null || !_connected || _executing) return;
        var cmd = (ConsoleCommand.Text ?? "").Trim();
        if (cmd.Length == 0) return;
        var sudo = ConsoleSudo.IsChecked == true && _platform != TargetPlatform.Windows;
        _executing = true;
        UpdateCommandRisk();
        try { await _execute(cmd, sudo); }
        catch (Exception ex) { StatusLabel.Text = "Error al enviar comando: " + ex.Message; }
        finally { _executing = false; UpdateCommandRisk(); }
    }

    private async void CopyOutputClicked(object? sender, RoutedEventArgs e) =>
        await CopyTextAsync(ConsoleOutput.Text ?? "");

    private async void CopyLogClicked(object? sender, RoutedEventArgs e) =>
        await CopyTextAsync(ConsoleHistory.Text ?? "");

    private async Task CopyTextAsync(string text)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) throw new InvalidOperationException("Portapapeles no disponible.");
            await clipboard.SetTextAsync(text);
        }
        catch (Exception ex) { StatusLabel.Text = "No se pudo copiar: " + ex.Message; }
    }

    private void ToggleMaximizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "Restaurar tamaño" : "Maximizar";
    }

    private void HandleKeyboard(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F11) return;
        ToggleMaximizeClicked(this, new RoutedEventArgs());
        e.Handled = true;
    }

    private void DockConsoleClicked(object? sender, RoutedEventArgs e) => Close();
}
