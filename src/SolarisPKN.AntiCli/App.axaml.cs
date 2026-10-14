using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppSettingsStore.Shared.ApplyAppearance();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Opening extra independent windows must not quit when the first closes.
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
