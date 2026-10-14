using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace SolarisPKN.AntiCli.Services;

public sealed class AntiCliSettings
{
    public string Theme { get; set; } = "Dark";
    public string AccentHex { get; set; } = "#7C4DFF";
    // Vacío = fondo del tema Fluent; un color opcional se valida por contraste.
    public string WindowBackgroundHex { get; set; } = "";
    public string NewSession { get; set; } = "Ctrl+T";
    public string CloneSession { get; set; } = "Ctrl+Shift+T";
    public string NewWindow { get; set; } = "Ctrl+Shift+N";
    public string CloneWindow { get; set; } = "Ctrl+Alt+N";
    public string CloseSession { get; set; } = "Ctrl+W";
    public string ExecuteCommand { get; set; } = "Ctrl+Enter";
}

public sealed class AppSettingsStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolarisPKN", "AntiCLI");
    private static readonly string SettingsFile = Path.Combine(Folder, "settings.json");
    private static readonly string[] ShortcutNames =
        ["NewSession", "CloneSession", "NewWindow", "CloneWindow", "CloseSession", "ExecuteCommand"];
    private static readonly Regex AccentPattern = new(@"^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettingsStore Shared { get; } = new();
    public AntiCliSettings Current { get; private set; }

    private AppSettingsStore()
    {
        try
        {
            Current = File.Exists(SettingsFile)
                ? JsonSerializer.Deserialize<AntiCliSettings>(File.ReadAllText(SettingsFile)) ?? new AntiCliSettings()
                : new AntiCliSettings();
            Validate(Current);
        }
        catch (Exception)
        {
            // Do not overwrite malformed settings until the user explicitly saves.
            Current = new AntiCliSettings();
        }
    }

    public AntiCliSettings Snapshot() => new()
    {
        Theme = Current.Theme,
        AccentHex = Current.AccentHex,
        WindowBackgroundHex = Current.WindowBackgroundHex,
        NewSession = Current.NewSession,
        CloneSession = Current.CloneSession,
        NewWindow = Current.NewWindow,
        CloneWindow = Current.CloneWindow,
        CloseSession = Current.CloseSession,
        ExecuteCommand = Current.ExecuteCommand
    };

    public static IReadOnlyList<(string Property, string Label)> ShortcutLabels { get; } =
    [
        ("NewSession", "Nueva pestaña"),
        ("CloneSession", "Clonar pestaña SSH"),
        ("NewWindow", "Nueva ventana"),
        ("CloneWindow", "Clonar en ventana"),
        ("CloseSession", "Cerrar pestaña"),
        ("ExecuteCommand", "Ejecutar comando aprobado")
    ];

    public static string GetShortcut(AntiCliSettings value, string property) => property switch
    {
        "NewSession" => value.NewSession,
        "CloneSession" => value.CloneSession,
        "NewWindow" => value.NewWindow,
        "CloneWindow" => value.CloneWindow,
        "CloseSession" => value.CloseSession,
        "ExecuteCommand" => value.ExecuteCommand,
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };

    public static void SetShortcut(AntiCliSettings value, string property, string shortcut)
    {
        switch (property)
        {
            case "NewSession": value.NewSession = shortcut; break;
            case "CloneSession": value.CloneSession = shortcut; break;
            case "NewWindow": value.NewWindow = shortcut; break;
            case "CloneWindow": value.CloneWindow = shortcut; break;
            case "CloseSession": value.CloseSession = shortcut; break;
            case "ExecuteCommand": value.ExecuteCommand = shortcut; break;
            default: throw new ArgumentOutOfRangeException(nameof(property));
        }
    }

    public bool Matches(string action, KeyEventArgs e)
    {
        var shortcut = GetShortcut(Current, action);
        try
        {
            var gesture = KeyGesture.Parse(shortcut);
            return gesture.Key == e.Key && gesture.KeyModifiers == e.KeyModifiers;
        }
        catch (Exception) { return false; }
    }

    public static void Validate(AntiCliSettings value)
    {
        if (value.Theme is not ("System" or "Dark" or "Light"))
            throw new ArgumentException("El tema debe ser Sistema, Oscuro o Claro.");
        if (string.IsNullOrWhiteSpace(value.AccentHex) || !AccentPattern.IsMatch(value.AccentHex))
            throw new ArgumentException("El color de acento debe tener formato hexadecimal #RRGGBB.");
        if (!string.IsNullOrEmpty(value.WindowBackgroundHex))
        {
            if (!AccentPattern.IsMatch(value.WindowBackgroundHex))
                throw new ArgumentException("El fondo de ventana requiere un hexadecimal #RRGGBB.");
            if (value.Theme == "System")
                throw new ArgumentException("Para personalizar el fondo elegí tema Claro u Oscuro, no Sistema.");

            var c = Color.Parse(value.WindowBackgroundHex);
            // Minimum ~4.5:1 against white text on dark or black text on light.
            var luminance = RelativeLuminance(c);
            var contrast = value.Theme == "Dark"
                ? 1.05 / (luminance + 0.05)
                : (luminance + 0.05) / 0.05;
            if (contrast < 4.5)
                throw new ArgumentException("El fondo no contrasta lo suficiente con el texto del tema elegido.");
        }

        var gestures = new Dictionary<KeyGesture, string>();
        foreach (var name in ShortcutNames)
        {
            var text = GetShortcut(value, name);
            if (string.IsNullOrWhiteSpace(text) || text.Length > 40)
                throw new ArgumentException("Atajo vacío o demasiado largo: " + name);

            KeyGesture gesture;
            try { gesture = KeyGesture.Parse(text.Trim()); }
            catch (Exception) { throw new ArgumentException("Atajo inválido: " + text); }

            // Do not steal ordinary typing. Only Ctrl/Alt with A-Z, F1-F12 or Enter.
            var key = gesture.Key;
            var letter = key >= Key.A && key <= Key.Z;
            var function = key >= Key.F1 && key <= Key.F12;
            if ((gesture.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0
                || !(letter || function || key == Key.Enter)
                || (gesture.KeyModifiers & ~(KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift)) != 0)
                throw new ArgumentException("Usá Ctrl o Alt junto con una letra, F1–F12 o Enter: " + text);

            if (gestures.TryGetValue(gesture, out var conflicting))
                throw new ArgumentException("Atajos repetidos: " + conflicting + " y " + name + ".");
            gestures[gesture] = name;
        }
    }

    private static double RelativeLuminance(Color c)
    {
        static double Channel(byte component)
        {
            var x = component / 255.0;
            return x <= 0.04045 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    public void Save(AntiCliSettings next)
    {
        Validate(next);
        Directory.CreateDirectory(Folder);
        var temp = Path.Combine(Folder, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(next, JsonOptions));
            File.Move(temp, SettingsFile, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        Current = next;
        ApplyAppearance();
    }

    public void ApplyAppearance()
    {
        if (Application.Current is not { } app) return;
        app.RequestedThemeVariant = Current.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        app.Resources["AntiCliAccentBrush"] = new SolidColorBrush(Color.Parse(Current.AccentHex));
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
                ApplyWindowBackground(window);
        }
    }

    public void ApplyWindowBackground(Window window)
    {
        if (string.IsNullOrEmpty(Current.WindowBackgroundHex))
            window.ClearValue(Window.BackgroundProperty);
        else
            window.Background = new SolidColorBrush(Color.Parse(Current.WindowBackgroundHex));
    }
}
