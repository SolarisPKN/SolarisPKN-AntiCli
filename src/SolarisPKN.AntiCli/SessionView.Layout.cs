using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SolarisPKN.AntiCli;

/// <summary>Per-session dockable console. Input bar remains in row 1 for every mode.</summary>
public partial class SessionView
{
    private enum ConsoleViewMode { Docked, Expanded, Hidden, Detached }
    private ConsoleViewMode _consoleMode = ConsoleViewMode.Docked;
    private bool _terminalOnLeft;
    private DetachedConsoleWindow? _detachedWindow;

    private void ApplyConsoleMode()
    {
        if (CommandsWorkspace is null) return;
        switch (_consoleMode)
        {
            case ConsoleViewMode.Docked:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions(
                    _terminalOnLeft ? "9*,11*" : "11*,9*");
                BrowsePane.IsVisible = true;
                SideConsolePane.IsVisible = true;
                Grid.SetColumn(BrowsePane, _terminalOnLeft ? 1 : 0);
                Grid.SetColumn(SideConsolePane, _terminalOnLeft ? 0 : 1);
                Grid.SetColumnSpan(BrowsePane, 1);
                Grid.SetColumnSpan(SideConsolePane, 1);
                Grid.SetColumnSpan(CommandBar, 2);
                ExpandConsoleButton.Content = "Expandir";
                SideConsolePane.DockPositionButtonRef.Content = _terminalOnLeft
                    ? "Mover a derecha" : "Mover a izquierda";
                ShowConsoleButton.IsVisible = false;
                break;
            case ConsoleViewMode.Expanded:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("*,0");
                BrowsePane.IsVisible = false;
                SideConsolePane.IsVisible = true;
                Grid.SetColumn(BrowsePane, 0);
                Grid.SetColumn(SideConsolePane, 0);
                Grid.SetColumnSpan(SideConsolePane, 2);
                Grid.SetColumnSpan(CommandBar, 2);
                ExpandConsoleButton.Content = "Restaurar 25%";
                ShowConsoleButton.IsVisible = false;
                break;
            case ConsoleViewMode.Detached:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("*,0");
                BrowsePane.IsVisible = true;
                SideConsolePane.IsVisible = false;
                Grid.SetColumn(BrowsePane, 0);
                Grid.SetColumn(SideConsolePane, 1);
                Grid.SetColumnSpan(CommandBar, 2);
                ShowConsoleButton.IsVisible = true;
                ShowConsoleButton.Content = "Acoplar consola ↙";
                break;
            default:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("*,0");
                BrowsePane.IsVisible = true;
                SideConsolePane.IsVisible = false;
                Grid.SetColumn(BrowsePane, 0);
                Grid.SetColumn(SideConsolePane, 1);
                Grid.SetColumnSpan(CommandBar, 2);
                ShowConsoleButton.IsVisible = true;
                ShowConsoleButton.Content = "Mostrar consola";
                break;
        }
    }

    private void ToggleDockPositionClicked(object? sender, RoutedEventArgs e)
    {
        _terminalOnLeft = !_terminalOnLeft;
        if (_consoleMode == ConsoleViewMode.Docked)
            ApplyConsoleMode();
    }

    private void ToggleConsoleExpandClicked(object? sender, RoutedEventArgs e)
    {
        _consoleMode = _consoleMode == ConsoleViewMode.Expanded
            ? ConsoleViewMode.Docked : ConsoleViewMode.Expanded;
        ApplyConsoleMode();
    }

    private void HideConsoleClicked(object? sender, RoutedEventArgs e)
    {
        _consoleMode = ConsoleViewMode.Hidden;
        ApplyConsoleMode();
    }

    private void ShowConsoleClicked(object? sender, RoutedEventArgs e)
    {
        if (_consoleMode == ConsoleViewMode.Detached && _detachedWindow is not null)
        {
            _detachedWindow.Close(); // Closing the detached view re-docks the console.
            return;
        }
        _consoleMode = ConsoleViewMode.Docked;
        ApplyConsoleMode();
    }

    private void ShowConsoleForOperation()
    {
        // Never change expansion chosen by the operator; reveal only a hidden console.
        if (_consoleMode == ConsoleViewMode.Hidden)
        {
            _consoleMode = ConsoleViewMode.Docked;
            ApplyConsoleMode();
        }
    }
    private void DetachConsoleClicked(object? sender, RoutedEventArgs e)
    {
        if (_detachedWindow is not null)
        {
            _detachedWindow.Activate();
            return;
        }
        if (!IsAuthenticated)
        {
            ExecutionStatus.Text = "Autenticá SSH antes de desacoplar la consola.";
            return;
        }

        var window = new DetachedConsoleWindow(DetachedWindowClosed);
        _detachedWindow = window;
        _consoleMode = ConsoleViewMode.Detached;
        ApplyConsoleMode();
        SyncDetachedConsole();
        window.Show(); // Unowned: can be moved/maximized on a second monitor independently.
        window.Activate();
    }

    private void DetachedWindowClosed()
    {
        _detachedWindow = null;
        if (_consoleMode == ConsoleViewMode.Detached)
        {
            _consoleMode = ConsoleViewMode.Docked;
            ApplyConsoleMode();
        }
    }

    private void CloseDetachedConsoleForDisposal()
    {
        // Closing the original tab/window must not leave a detached shell or
        // stale callback retaining the session's SSH credentials in memory.
        _detachedWindow?.Close();
    }

    private void ConsoleSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty || e.Property == TextBlock.TextProperty)
            SyncDetachedConsole();
    }

    private void SyncDetachedConsole()
    {
        if (_detachedWindow is null) return;
        var endpoint = _connectedProfile is null
            ? "Sesión SSH desconectada"
            : _connectedProfile.Name + " · " + _connectedProfile.Host + ":" +
              _connectedProfile.Port;
        _detachedWindow.UpdateSnapshot(endpoint,
            CommandHistoryLog.Text ?? "", ExecutionStatus.Text ?? "");
    }

}
