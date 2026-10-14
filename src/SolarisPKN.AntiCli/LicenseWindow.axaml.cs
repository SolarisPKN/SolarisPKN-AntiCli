using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

public partial class LicenseWindow : Window
{
    public LicenseWindow()
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        try
        {
            using var data = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("AntiCli.LICENSE");
            if (data is null)
                throw new InvalidOperationException("La licencia no fue incluida en la compilación.");
            using var reader = new StreamReader(data, Encoding.UTF8);
            LicenseText.Text = reader.ReadToEnd();
        }
        catch (Exception e)
        {
            LicenseText.Text = "No se pudo cargar el texto de GPLv3. Consultá el archivo LICENSE junto al código fuente.\n" + e.Message;
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e) => Close();
}
