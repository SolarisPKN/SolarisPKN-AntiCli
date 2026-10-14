using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SolarisPKN.AntiCli;

/// <summary>Per-session dockable console. Input bar remains in row 1 for every mode.</summary>
public partial class SessionView
{
    private enum ConsoleViewMode { Docked, Expanded, Hidden }
    private ConsoleViewMode _consoleMode = ConsoleViewMode.Docked;

    private void ApplyConsoleMode()
    {
        if (CommandsWorkspace is null) return;
        switch (_consoleMode)
        {
            case ConsoleViewMode.Docked:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("3*,*");
                BrowsePane.IsVisible = true;
                SideConsolePane.IsVisible = true;
                Grid.SetColumn(SideConsolePane, 1);
                Grid.SetColumnSpan(SideConsolePane, 1);
                Grid.SetColumnSpan(CommandBar, 2);
                ExpandConsoleButton.Content = "Expandir";
                ShowConsoleButton.IsVisible = false;
                break;
            case ConsoleViewMode.Expanded:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("*,0");
                BrowsePane.IsVisible = false;
                SideConsolePane.IsVisible = true;
                Grid.SetColumn(SideConsolePane, 0);
                Grid.SetColumnSpan(SideConsolePane, 2);
                Grid.SetColumnSpan(CommandBar, 2);
                ExpandConsoleButton.Content = "Restaurar 25%";
                ShowConsoleButton.IsVisible = false;
                break;
            default:
                CommandsWorkspace.ColumnDefinitions = new ColumnDefinitions("*,0");
                BrowsePane.IsVisible = true;
                SideConsolePane.IsVisible = false;
                Grid.SetColumnSpan(CommandBar, 2);
                ShowConsoleButton.IsVisible = true;
                break;
        }
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
}
