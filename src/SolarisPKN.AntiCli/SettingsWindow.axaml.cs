using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

public partial class SettingsWindow : Window
{
    private bool _initialized;

    private static readonly (string Name, string Hex)[] Accents =
    [
        ("Violeta", "#7C4DFF"),
        ("Azul", "#2563EB"),
        ("Verde", "#059669"),
        ("Rosa", "#DB2777"),
        ("Naranja", "#C2410C"),
        ("Personalizado", "")
    ];

    public SettingsWindow()
    {
        InitializeComponent();
        AppSettingsStore.Shared.ApplyWindowBackground(this);
        ThemeSelector.ItemsSource = new[] { "Sistema", "Oscuro", "Claro" };
        AccentSelector.ItemsSource = Accents.Select(x => x.Name).ToArray();
        FillForm(AppSettingsStore.Shared.Snapshot());
        _initialized = true;
    }

    private void FillForm(AntiCliSettings values)
    {
        _initialized = false;
        ThemeSelector.SelectedIndex = values.Theme switch
        {
            "System" => 0,
            "Light" => 2,
            _ => 1
        };
        var colorIndex = Array.FindIndex(Accents, x =>
            string.Equals(x.Hex, values.AccentHex, StringComparison.OrdinalIgnoreCase));
        AccentSelector.SelectedIndex = colorIndex >= 0 ? colorIndex : Accents.Length - 1;
        CustomAccent.Text = values.AccentHex;
        WindowBackgroundInput.Text = values.WindowBackgroundHex;
        CustomAccent.IsEnabled = AccentSelector.SelectedIndex == Accents.Length - 1;
        NewSessionShortcut.Text = values.NewSession;
        CloneSessionShortcut.Text = values.CloneSession;
        NewWindowShortcut.Text = values.NewWindow;
        CloneWindowShortcut.Text = values.CloneWindow;
        CloseSessionShortcut.Text = values.CloseSession;
        ExecuteCommandShortcut.Text = values.ExecuteCommand;
        UpdatePreview();
        _initialized = true;
    }

    private string SelectedAccentHex()
    {
        var preset = AccentSelector.SelectedIndex;
        return preset >= 0 && preset < Accents.Length - 1
            ? Accents[preset].Hex
            : (CustomAccent.Text ?? "").Trim();
    }

    private void AccentSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        var custom = AccentSelector.SelectedIndex == Accents.Length - 1;
        CustomAccent.IsEnabled = custom;
        if (!custom && AccentSelector.SelectedIndex >= 0)
            CustomAccent.Text = Accents[AccentSelector.SelectedIndex].Hex;
        UpdatePreview();
    }

    private void CustomAccentChanged(object? sender, TextChangedEventArgs e)
    {
        if (_initialized) UpdatePreview();
    }

    private void UpdatePreview()
    {
        var value = SelectedAccentHex();
        try
        {
            AccentPreview.Background = new SolidColorBrush(Color.Parse(value));
            ValidationMessage.Text = "";
        }
        catch (Exception)
        {
            ValidationMessage.Text = "Color inválido. Usá seis cifras hexadecimales, por ejemplo #7C4DFF.";
        }
    }

    private AntiCliSettings ReadForm() => new()
    {
        Theme = ThemeSelector.SelectedIndex switch
        {
            0 => "System",
            2 => "Light",
            _ => "Dark"
        },
        AccentHex = SelectedAccentHex(),
        WindowBackgroundHex = (WindowBackgroundInput.Text ?? "").Trim(),
        NewSession = NewSessionShortcut.Text?.Trim() ?? "",
        CloneSession = CloneSessionShortcut.Text?.Trim() ?? "",
        NewWindow = NewWindowShortcut.Text?.Trim() ?? "",
        CloneWindow = CloneWindowShortcut.Text?.Trim() ?? "",
        CloseSession = CloseSessionShortcut.Text?.Trim() ?? "",
        ExecuteCommand = ExecuteCommandShortcut.Text?.Trim() ?? ""
    };

    private void SaveClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            AppSettingsStore.Shared.Save(ReadForm());
            Close();
        }
        catch (Exception ex)
        {
            ValidationMessage.Text = "No se pudieron guardar los ajustes: " + ex.Message;
        }
    }

    private void LicenseClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new LicenseWindow();
        _ = dialog.ShowDialog(this);
    }

    private void DefaultsClicked(object? sender, RoutedEventArgs e)
    {
        FillForm(new AntiCliSettings());
        ValidationMessage.Text = "Valores predeterminados cargados. Guardá para aplicarlos.";
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close();
}
