using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Each TabItem owns a SessionView and its own SSH credentials, commands and logs.
/// Windows are independent; closing a window does not close the others.
/// </summary>
public partial class MainWindow : Window
{
    private readonly List<(TabItem Tab, SessionView View)> _sessions = [];
    private int _nextTabNumber = 1;

    public MainWindow() : this(createInitialSession: true) { }

    public MainWindow(bool createInitialSession)
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        KeyDown += HandleKeyboardShortcuts;
        if (createInitialSession) AddSession(new SessionView());
    }

    private SessionView? CurrentSession =>
        _sessions.FirstOrDefault(x => ReferenceEquals(x.Tab, SessionTabs.SelectedItem)).View;

    public void AddSession(SessionView view)
    {
        var ordinal = _nextTabNumber++;
        var label = new TextBlock
        {
            Text = view.SessionTitle + " · " + ordinal,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 210,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        var close = new Button { Content = "×", Padding = new Thickness(5, 1), Margin = new Thickness(9, 0, 0, 0) };
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(label);
        header.Children.Add(close);

        var item = new TabItem { Header = header, Content = view };
        view.SessionTitleChanged += (_, _) => label.Text = view.SessionTitle + " · " + ordinal;
        close.Click += (_, _) => CloseSession(item, view);

        _sessions.Add((item, view));
        SessionTabs.Items.Add(item);
        SessionTabs.SelectedItem = item;
    }

    private void CloseSession(TabItem item, SessionView view)
    {
        if (view.IsBusy)
        {
            SessionTabs.SelectedItem = item;
            view.DisplayTabMessage("Hay un comando en ejecución. Terminá esa consulta antes de cerrar esta pestaña.");
            return;
        }

        _sessions.RemoveAll(x => ReferenceEquals(x.Tab, item));
        SessionTabs.Items.Remove(item);
        view.Dispose();
        if (_sessions.Count == 0)
            AddSession(new SessionView()); // The shell remains useful after closing its final tab.
    }

    private void NewSessionClicked(object? sender, RoutedEventArgs e) =>
        AddSession(new SessionView());

    private void CloneSessionClicked(object? sender, RoutedEventArgs e)
    {
        var current = CurrentSession;
        var clone = current?.CloneAuthenticated();
        if (clone is null)
        {
            current?.DisplayTabMessage("Autenticá SSH primero. Solo se pueden clonar sesiones verificadas que no estén ocupadas.");
            return;
        }
        AddSession(clone);
    }

    private void NewWindowClicked(object? sender, RoutedEventArgs e)
    {
        var window = new MainWindow(createInitialSession: true);
        window.Show();
    }

    private void CloneWindowClicked(object? sender, RoutedEventArgs e)
    {
        var current = CurrentSession;
        var clone = current?.CloneAuthenticated();
        if (clone is null)
        {
            current?.DisplayTabMessage("Primero autenticá la sesión seleccionada para clonarla en otra ventana.");
            return;
        }
        var window = new MainWindow(createInitialSession: false);
        window.AddSession(clone);
        window.Show();
    }

    private void SettingsClicked(object? sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        _ = settings.ShowDialog(this);
    }

    private void HandleKeyboardShortcuts(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        var preferences = AppSettingsStore.Shared;

        if (preferences.Matches("NewSession", e))
            NewSessionClicked(this, new RoutedEventArgs());
        else if (preferences.Matches("CloneSession", e))
            CloneSessionClicked(this, new RoutedEventArgs());
        else if (preferences.Matches("NewWindow", e))
            NewWindowClicked(this, new RoutedEventArgs());
        else if (preferences.Matches("CloneWindow", e))
            CloneWindowClicked(this, new RoutedEventArgs());
        else if (preferences.Matches("CloseSession", e))
        {
            var tab = SessionTabs.SelectedItem as TabItem;
            var view = CurrentSession;
            if (tab is not null && view is not null)
                CloseSession(tab, view);
        }
        else
            return;

        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        foreach (var (_, view) in _sessions) view.Dispose();
        _sessions.Clear();
        base.OnClosed(e);
    }
}
