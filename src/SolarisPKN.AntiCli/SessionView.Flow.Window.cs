using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Reparents the EXISTING diagram control into one standalone window. Graph
/// data, node selection, canvas and event handlers remain part of this session:
/// there is no separate workflow copy, SSH session or duplicate editor.
/// </summary>
public partial class SessionView
{
    private Window? _flowWindow;
    private ContentControl? _flowWindowHost;
    private Control? _flowWindowContent;

    private void OpenFlowWindowClicked(object? sender, RoutedEventArgs e)
    {
        if (_flowWindow is not null)
        {
            if (_flowWindow.WindowState == WindowState.Minimized)
                _flowWindow.WindowState = WindowState.Normal;
            _flowWindow.Activate();
            return;
        }
        if (_disposed) return;

        var diagram = FlowDesignerTab.Content as Control;
        if (diagram is null)
        {
            AutomationStatus.Text = "No se pudo abrir el diagrama. Su vista no está disponible.";
            return;
        }

        var window = new Window
        {
            Title = "Solaris AntiCLI · Diagrama de automatizaciones",
            Width = 1220,
            Height = 790,
            MinWidth = 630,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = true,
            ShowInTaskbar = true
        };
        AppSettingsStore.Shared.ApplyWindowBackground(window);

        var root = new Grid
        {
            Margin = new Thickness(10),
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 8,
            MinHeight = 0
        };
        var header = new WrapPanel { ItemSpacing = 9, LineSpacing = 5 };
        var label = new TextBlock
        {
            Text = "Diagrama del flujo · independiente de la ventana principal",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        var maximize = new Button { Content = "Maximizar" };
        var restore = new Button { Content = "Volver a AntiCLI ↙" };
        void ToggleMaximize()
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
            maximize.Content = window.WindowState == WindowState.Maximized
                ? "Restaurar tamaño" : "Maximizar";
        }
        maximize.Click += (_, _) => ToggleMaximize();
        restore.Click += (_, _) => window.Close();
        window.KeyDown += (_, args) =>
        {
            if (args.Key != Key.F11) return;
            ToggleMaximize();
            args.Handled = true;
        };
        header.Children.Add(label);
        header.Children.Add(maximize);
        header.Children.Add(restore);
        root.Children.Add(header);

        // Detach before attaching to the floating window; Avalonia forbids
        // reusing one visual with two parents.
        FlowDesignerTab.Content = null;
        _flowWindowContent = diagram;
        _flowWindowHost = new ContentControl { Content = diagram, MinHeight = 0 };
        Grid.SetRow(_flowWindowHost, 1);
        root.Children.Add(_flowWindowHost);
        window.Content = root;
        _flowWindow = window;

        window.Closed += (_, _) =>
        {
            // Restore the SAME controls and graph when closing the designer;
            // the next button click reopens the exact project, including edits.
            if (_flowWindowHost is not null)
                _flowWindowHost.Content = null;
            if (_flowWindowContent is not null)
                FlowDesignerTab.Content = _flowWindowContent;
            _flowWindowHost = null;
            _flowWindowContent = null;
            _flowWindow = null;
        };

        RefreshFlowDiagramFromAutomation();
        window.Show(); // Independent: move/maximize freely on a second monitor.
        window.Activate();
    }

    private void CloseFlowWindowForDisposal()
    {
        _flowWindow?.Close();
    }
}
