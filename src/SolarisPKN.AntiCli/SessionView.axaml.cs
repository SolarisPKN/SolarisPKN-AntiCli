using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;
using SolarisPKN.AntiCli.Services;

namespace SolarisPKN.AntiCli;

public partial class SessionView : UserControl, IDisposable
{
    private readonly ProfileStore _store = new();
    private readonly SshProbe _ssh = new();
    private readonly TrustedHostKeyStore _hostKeys = new();
    private readonly List<string> _blocks = [];
    private IReadOnlyList<SmartCliSuggestion> _matches = [];
    private Guid _editingId = Guid.NewGuid();
    private bool _loadingProfile;
    private bool _busy;
    private bool _invalidateAfterBusy;
    private SshProfile? _connectedProfile;
    private SshCredentials? _connectedCredentials;
    private bool _disposed;
    private bool _disposeAfterBusy;

    public event EventHandler? SessionTitleChanged;
    public string SessionTitle =>
        _connectedProfile is not null ? _connectedProfile.Name + " · " + _connectedProfile.Host
        : string.IsNullOrWhiteSpace(ProfileName.Text) ? "Nuevo SSH" : ProfileName.Text!.Trim();

    public bool IsAuthenticated => _connectedProfile is not null && _connectedCredentials is not null;
    public bool IsBusy => _busy;

    /// <summary>Each tab owns independent credentials, UI state, command history and execution.</summary>
    public SessionView? CloneAuthenticated()
    {
        if (_disposed || _busy || !IsAuthenticated) return null;
        var previous = _connectedProfile!;
        var creds = _connectedCredentials!;
        var clone = new SessionView();
        clone.LoadClonedSession(CloneProfile(previous), new SshCredentials(
            creds.User, creds.Password, creds.Passphrase, creds.KeyBytes?.ToArray()));
        // A cloned authenticated session targets the same SSH endpoint and user.
        clone._remoteDictionary = _remoteDictionary;
        clone.UpdateRemoteDictionaryStatus();
        clone.PopulateActionButtons();
        clone.ReloadSuggestions();
        return clone;
    }

    private static SshProfile CloneProfile(SshProfile p) => new()
    {
        Id = p.Id, Name = p.Name, Host = p.Host, Port = p.Port,
        Platform = p.Platform, HostKeySha256 = p.HostKeySha256,
        AuthMode = p.AuthMode,
        RememberUser = p.RememberUser, RememberKey = p.RememberKey,
        RememberPassword = p.RememberPassword, RememberPassphrase = p.RememberPassphrase,
        UserDpapi = p.UserDpapi, KeyDpapi = p.KeyDpapi,
        PasswordDpapi = p.PasswordDpapi, PassphraseDpapi = p.PassphraseDpapi
    };

    private void LoadClonedSession(SshProfile profile, SshCredentials credentials)
    {
        _loadingProfile = true;
        try
        {
            _editingId = profile.Id;
            PlatformSelector.SelectedIndex = (int)profile.Platform;
            AuthModeSelector.SelectedIndex = profile.AuthMode == SshAuthMode.Password ? 1 : 0;
            ProfileName.Text = profile.Name;
            HostInput.Text = profile.Host;
            PortInput.Text = profile.Port.ToString();
            // The saved fingerprint travels with the authenticated tab, but every
            // SSH operation checks the global host:port pin as well.
            FingerprintInput.Text = profile.HostKeySha256;
            UserInput.Text = credentials.User;
            RememberUser.IsChecked = profile.RememberUser;
            RememberKey.IsChecked = profile.RememberKey;
            RememberPassword.IsChecked = profile.RememberPassword;
            RememberPassphrase.IsChecked = profile.RememberPassphrase;
            KeyPathInput.Text = "";
            PasswordInput.Text = "";
            PassphraseInput.Text = "";
            _connectedProfile = profile;
            _connectedCredentials = credentials;
            CommandsTab.IsEnabled = true;
            ModulesTab.IsEnabled = true;
            AuthStatus.Text = "Perfil reutilizado en una pestaña independiente. Cada comando abre su propia conexión SSH con huella verificada.";
            RemoteInfo.Text = "Clon de " + profile.Name + " · " + profile.Host + " · " + profile.Platform;
            BuildActionButtons();
            RefreshModuleEditorSources();
            ReloadSuggestions();
            MainTabs.SelectedIndex = 1;
        }
        finally { _loadingProfile = false; }
        SessionTitleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DisplayTabMessage(string message)
    {
        AuthStatus.Text = message;
        if (MainTabs.SelectedIndex == 1) ExecutionStatus.Text = message;
        if (MainTabs.SelectedIndex == 2) ScanStatus.Text = message;
    }

    public SessionView()
    {
        InitializeComponent();
        WireCommandPanels();
        PlatformSelector.ItemsSource = new[] { "Debian", "Ubuntu", "Windows", "Otra distribución Linux" };
        PlatformSelector.SelectedIndex = -1; // The user must explicitly choose the target OS.
        AuthModeSelector.ItemsSource = new[] { "Clave privada SSH", "Contraseña SSH" };
        AuthModeSelector.SelectedIndex = 0;
        foreach (var field in new[] { HostInput, PortInput, UserInput, KeyPathInput, PasswordInput, PassphraseInput, FingerprintInput })
            field.TextChanged += ConnectionTextChanged;
        AuthModeSelector.SelectionChanged += ConnectionModeChanged;
        KeyDown += SessionShortcutKeyDown;
        ProfileName.TextChanged += (_, _) => SessionTitleChanged?.Invoke(this, EventArgs.Empty);
        // One data source per tab: the detached terminal is a synchronized view.
        Output.PropertyChanged += ConsoleSourcePropertyChanged;
        CommandHistoryLog.PropertyChanged += ConsoleSourcePropertyChanged;
        ExecutionStatus.PropertyChanged += ConsoleSourcePropertyChanged;
        ReloadProfiles();
    }

    private TargetPlatform SelectedPlatform()
    {
        return PlatformSelector.SelectedIndex switch
        {
            0 => TargetPlatform.Debian,
            1 => TargetPlatform.Ubuntu,
            2 => TargetPlatform.Windows,
            3 => TargetPlatform.OtherLinux,
            _ => throw new InvalidOperationException("Primero elegí la plataforma del servidor.")
        };
    }

    private void ReloadProfiles() => ProfilesList.ItemsSource = _store.Load();

    private void DisconnectSession()
    {
        if (_connectedCredentials?.KeyBytes is { } key) CryptographicOperations.ZeroMemory(key);
        _connectedCredentials = null;
        _connectedProfile = null;
        CommandsTab.IsEnabled = false;
        ModulesTab.IsEnabled = false;
        OptionButtons.Children.Clear();
        TokenButtons.Children.Clear();
        _blocks.Clear();
        BlockPreview.Text = "Bloques: (vacío)";
        RemoteInfo.Text = "Sesión no autenticada";
        _remoteDictionary = null;
        UpdateRemoteDictionaryStatus();
        UseSudoToggle.IsChecked = false;
        SyncDetachedConsole();
        SessionTitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ConnectionTextChanged(object? sender, TextChangedEventArgs e)
    {
        // A fingerprint belongs to a particular host AND port. Do not show an
        // old endpoint's pin when the operator enters a different VPS.
        if (!_loadingProfile && (ReferenceEquals(sender, HostInput) || ReferenceEquals(sender, PortInput)))
        {
            try
            {
                var host = HostInput.Text?.Trim() ?? "";
                if (host.Length > 0 && int.TryParse(PortInput.Text, out var port) &&
                    port is >= 1 and <= 65535)
                {
                    var saved = _hostKeys.Find(host, port);
                    DisplayAcceptedFingerprint(saved?.Sha256 ?? "");
                    HostKeyStatus.Text = saved is null
                        ? "Servidor aún desconocido. Se pedirá confirmar su clave en la primera conexión."
                        : "Clave confiable encontrada para " + host + ":" + port + ".";
                }
                else
                {
                    DisplayAcceptedFingerprint("");
                    HostKeyStatus.Text = "Ingresá una dirección y un puerto SSH válidos.";
                }
            }
            catch (Exception ex)
            {
                HostKeyStatus.Text = "No se pudo consultar el almacén de claves: " + ex.Message;
                DisplayAcceptedFingerprint("");
            }
        }
        InvalidateCurrentConnection();
    }

    private void ConnectionModeChanged(object? sender, SelectionChangedEventArgs e) => InvalidateCurrentConnection();

    private void InvalidateCurrentConnection()
    {
        if (_loadingProfile) return;
        if (_busy)
        {
            _invalidateAfterBusy = true;
            AuthStatus.Text = "Cambiaste la conexión durante una operación. Se cancelará su activación.";
            return;
        }
        if (_connectedProfile is null) return;
        DisconnectSession();
        MainTabs.SelectedIndex = 0;
        AuthStatus.Text = "Cambiaste datos de la conexión. Volvé a autenticarte para aplicar los cambios.";
    }

    private void FinishOperation()
    {
        _busy = false;
        if (_disposeAfterBusy)
        {
            _disposeAfterBusy = false;
            DisconnectSession();
            return;
        }
        if (_invalidateAfterBusy)
        {
            _invalidateAfterBusy = false;
            InvalidateCurrentConnection();
        }
    }

    private void PlatformChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_loadingProfile) return;
        if (_busy) { _invalidateAfterBusy = true; return; }
        DisconnectSession();
        AuthStatus.Text = "Plataforma seleccionada. Configurá y comprobá SSH para acceder a comandos.";
    }

    private void NewProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) { AuthStatus.Text = "Esperá a que termine la consulta SSH actual."; return; }
        _loadingProfile = true;
        try
        {
            DisconnectSession();
            _editingId = Guid.NewGuid();
            ProfilesList.SelectedItem = null;
            PlatformSelector.SelectedIndex = -1;
            AuthModeSelector.SelectedIndex = 0;
            ProfileName.Text = HostInput.Text = FingerprintInput.Text = "";
            HostKeyStatus.Text = "Primera conexión: detectá la huella SSH y decidí si confiar.";
            UserInput.Text = KeyPathInput.Text = PasswordInput.Text = PassphraseInput.Text = "";
            PortInput.Text = "22";
            RememberUser.IsChecked = RememberKey.IsChecked =
                RememberPassword.IsChecked = RememberPassphrase.IsChecked = false;
            AuthStatus.Text = "Elegí el sistema remoto y completá las credenciales.";
            MainTabs.SelectedIndex = 0;
            SessionTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        finally { _loadingProfile = false; }
    }

    private void ProfileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_loadingProfile || ProfilesList.SelectedItem is not SshProfile p) return;
        if (_busy) { _invalidateAfterBusy = true; return; }
        _loadingProfile = true;
        try
        {
            DisconnectSession();
            _editingId = p.Id;
            PlatformSelector.SelectedIndex = (int)p.Platform;
            AuthModeSelector.SelectedIndex = p.AuthMode == SshAuthMode.Password ? 1 : 0;
            ProfileName.Text = p.Name;
            HostInput.Text = p.Host;
            PortInput.Text = p.Port.ToString();
            var trusted = _hostKeys.Find(p.Host, p.Port);
            FingerprintInput.Text = trusted?.Sha256 ?? p.HostKeySha256;
            HostKeyStatus.Text = trusted is null
                ? "Sin clave confiable para este host:puerto. Detectala y confirmala antes de autenticar."
                : "Clave del servidor ya guardada para " + p.Host + ":" + p.Port + ".";
            RememberUser.IsChecked = p.RememberUser;
            RememberKey.IsChecked = p.RememberKey;
            RememberPassword.IsChecked = p.RememberPassword;
            RememberPassphrase.IsChecked = p.RememberPassphrase;
            UserInput.Text = _store.StoredUser(p);
            KeyPathInput.Text = PasswordInput.Text = PassphraseInput.Text = "";
            AuthStatus.Text = "Perfil cargado. Los campos protegidos se descifran solo al conectar; clave no recordada: volvé a seleccionarla.";
            MainTabs.SelectedIndex = 0;
            SessionTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { AuthStatus.Text = "No se pudo cargar el perfil: " + ex.Message; }
        finally { _loadingProfile = false; }
    }

    private async void ChooseKeyClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var picker = TopLevel.GetTopLevel(this)?.StorageProvider ?? throw new InvalidOperationException("La pestaña no está adjunta a una ventana.");
            var files = await picker.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccioná tu archivo de clave privada SSH",
                AllowMultiple = false
            });
            if (files.Count > 0) KeyPathInput.Text = files[0].TryGetLocalPath();
        }
        catch (Exception ex) { AuthStatus.Text = "Error al seleccionar clave SSH: " + ex.Message; }
    }

    private SshProfile FormProfile()
    {
        var p = new SshProfile
        {
            Id = _editingId,
            Name = ProfileName.Text?.Trim() ?? "",
            Platform = SelectedPlatform(),
            Host = HostInput.Text?.Trim() ?? "",
            Port = int.TryParse(PortInput.Text, out var port) ? port : 0,
            HostKeySha256 = FingerprintInput.Text?.Trim() ?? "",
            AuthMode = AuthModeSelector.SelectedIndex == 1 ? SshAuthMode.Password : SshAuthMode.PrivateKey,
            RememberUser = RememberUser.IsChecked == true,
            RememberKey = RememberKey.IsChecked == true,
            RememberPassword = RememberPassword.IsChecked == true,
            RememberPassphrase = RememberPassphrase.IsChecked == true
        };

        var previous = _store.Load().FirstOrDefault(x => x.Id == p.Id);
        if (previous is not null)
        {
            p.UserDpapi = previous.UserDpapi;
            p.KeyDpapi = previous.KeyDpapi;
            p.PasswordDpapi = previous.PasswordDpapi;
            p.PassphraseDpapi = previous.PassphraseDpapi;
        }
        return p;
    }

    private void SaveProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        try
        {
            DisconnectSession();
            var profile = FormProfile();
            var pinned = _hostKeys.Find(profile.Host, profile.Port);
            if (pinned is not null && pinned.Sha256 != profile.HostKeySha256)
                throw new InvalidOperationException(
                    "La huella difiere de la clave confiable: revisá el cambio de servidor antes de guardar.");
            _store.Save(profile, UserInput.Text ?? "", PasswordInput.Text ?? "",
                PassphraseInput.Text ?? "", KeyPathInput.Text);
            ReloadProfiles();
            AuthStatus.Text = "Perfil guardado. Solo se cifraron los datos que marcaste para recordar.";
        }
        catch (Exception ex) { AuthStatus.Text = "No se pudo guardar el perfil: " + ex.Message; }
    }

    private void DisplayAcceptedFingerprint(string sha256)
    {
        // Programmatic trust display must not count as an interactive edit.
        var priorLoading = _loadingProfile;
        _loadingProfile = true;
        try { FingerprintInput.Text = sha256; }
        finally { _loadingProfile = priorLoading; }
    }

    /// <summary>
    /// First-contact SSH key exchange. Never sees password, passphrase or private key.
    /// A stored pin is authoritative. Key rotation requires the explicit review button.
    /// </summary>
    private async Task<bool> EnsureServerKeyAsync(SshProfile profile, bool forceDiscovery)
    {
        if (string.IsNullOrWhiteSpace(profile.Host) || profile.Port is < 1 or > 65535)
            throw new InvalidOperationException("Ingresá un host y un puerto SSH válidos.");

        var existing = _hostKeys.Find(profile.Host, profile.Port);
        if (existing is not null && !forceDiscovery)
        {
            if (string.IsNullOrWhiteSpace(profile.HostKeySha256))
            {
                profile.HostKeySha256 = existing.Sha256;
                DisplayAcceptedFingerprint(existing.Sha256);
            }
            if (profile.HostKeySha256 != existing.Sha256)
                throw new InvalidOperationException(
                    "La huella del perfil no coincide con la clave confiable. " +
                    "Usá «Detectar / revisar clave» para comprobar qué servidor respondió.");
            HostKeyStatus.Text = "Clave SSH confiable guardada: " + profile.Host + ":" + profile.Port;
            return true;
        }

        AuthStatus.Text = "Detectando clave pública SSH sin enviar usuario, contraseña ni clave privada...";
        var observed = await _ssh.DiscoverHostKeyAsync(profile.Host, profile.Port);

        if (existing is not null && observed.Sha256 == existing.Sha256 &&
            observed.Algorithm == existing.Algorithm)
        {
            profile.HostKeySha256 = existing.Sha256;
            DisplayAcceptedFingerprint(existing.Sha256);
            HostKeyStatus.Text = "La clave pública del servidor coincide con la ya confiable.";
            return true;
        }

        if (existing is not null && !forceDiscovery)
            throw new InvalidOperationException(
                "¡ADVERTENCIA! La clave del servidor cambió. La autenticación está bloqueada. " +
                "Revisá la nueva clave y verificála con el proveedor antes de confiar.");

        if (existing is null && !string.IsNullOrWhiteSpace(profile.HostKeySha256) &&
            profile.HostKeySha256 != observed.Sha256)
            throw new InvalidOperationException(
                "La huella recibida NO coincide con la que ingresaste. " +
                "Verificá el servidor por otro canal; no se confió en la clave.");

        var owner = TopLevel.GetTopLevel(this) as Window
            ?? throw new InvalidOperationException("No se encontró la ventana de AntiCLI.");
        var dialog = new HostKeyApprovalWindow(observed, existing);
        var accepted = await dialog.ShowDialog<bool>(owner);
        if (!accepted)
        {
            HostKeyStatus.Text = "Clave no aceptada. No se guardó ninguna confianza.";
            return false;
        }

        // Never trust an endpoint different from the one the user saw in the dialog.
        if (!string.Equals(HostInput.Text?.Trim(), observed.Host, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(PortInput.Text, out var currentPort) || currentPort != observed.Port)
            throw new InvalidOperationException(
                "El host o puerto cambió durante la confirmación. Repetí la detección.");

        if (existing is null)
            _hostKeys.TrustFirst(observed.Host, observed.Port, observed.Algorithm, observed.Sha256);
        else
            _hostKeys.ReplaceAfterReview(observed.Host, observed.Port, existing,
                observed.Algorithm, observed.Sha256);

        profile.HostKeySha256 = observed.Sha256;
        DisplayAcceptedFingerprint(observed.Sha256);
        HostKeyStatus.Text = (existing is null ? "Primera clave confiable guardada" : "Clave reemplazada tras revisión")
            + " para " + observed.Host + ":" + observed.Port + ".";
        return true;
    }

    private async void DiscoverHostKeyClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        try
        {
            var probe = new SshProfile
            {
                Host = HostInput.Text?.Trim() ?? "",
                Port = int.TryParse(PortInput.Text, out var port) ? port : 0,
                HostKeySha256 = FingerprintInput.Text?.Trim() ?? ""
            };
            DisconnectSession();
            _busy = true;
            if (await EnsureServerKeyAsync(probe, forceDiscovery: true))
                AuthStatus.Text = "Clave SSH revisada. Ya podés autenticarte con tus credenciales.";
            else
                AuthStatus.Text = "Operación cancelada. No se enviaron credenciales.";
        }
        catch (Exception ex)
        {
            AuthStatus.Text = "No se pudo verificar la clave del servidor: " + ex.Message;
        }
        finally { FinishOperation(); }
    }

    private async void ConnectClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        SshCredentials? credentials = null;
        try
        {
            DisconnectSession();
            var profile = FormProfile();
            if (string.IsNullOrWhiteSpace(profile.Host) || profile.Port is < 1 or > 65535)
                throw new InvalidOperationException("Ingresá un host y un puerto SSH válidos.");
            _busy = true;
            if (!await EnsureServerKeyAsync(profile, forceDiscovery: false))
            {
                AuthStatus.Text = "Conexión cancelada. La clave pública del servidor no fue aceptada.";
                return;
            }
            if (_invalidateAfterBusy)
                throw new InvalidOperationException(
                    "Cambiaste los datos de la conexión durante la comprobación SSH. Reintentá.");

            // Credential resolution starts only AFTER an explicit host-key trust decision.
            credentials = _store.Resolve(profile, UserInput.Text ?? "", PasswordInput.Text ?? "",
                PassphraseInput.Text ?? "", KeyPathInput.Text);
            AuthStatus.Text = "Verificando clave pública del servidor, credenciales y sistema remoto...";
            var detectedOs = await _ssh.AuthenticateAsync(profile, credentials);
            if (_invalidateAfterBusy)
                throw new InvalidOperationException(
                    "Se modificó la configuración durante la autenticación. No se activó la sesión.");

            _connectedProfile = profile;
            _connectedCredentials = credentials;
            credentials = null;
            // Discover PATH availability once per authenticated SSH connection.
            // Failures are advisory only and never block manual admin commands.
            await RefreshRemoteDictionaryAsync(profile, _connectedCredentials);
            if (_invalidateAfterBusy)
                throw new InvalidOperationException(
                    "Se cambió el destino durante la detección de herramientas. Reconectá.");
            CommandsTab.IsEnabled = true;
            ModulesTab.IsEnabled = true;
            RemoteInfo.Text = "Conectado: " + profile.Name + " · " + profile.Host + " · " + detectedOs;
            AuthStatus.Text = "SSH autenticado y sistema verificado: " + detectedOs + ". Ya podés usar los módulos.";
            BuildActionButtons();
            ReloadSuggestions();
            MainTabs.SelectedIndex = 1;
            SessionTitleChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AuthStatus.Text = "No se pudo autenticar: " + ex.Message;
            MainTabs.SelectedIndex = 0;
        }
        finally
        {
            if (credentials?.KeyBytes is { } key) CryptographicOperations.ZeroMemory(key);
            FinishOperation();
        }
    }

    private void DisconnectClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        DisconnectSession();
        PasswordInput.Text = PassphraseInput.Text = "";
        if (RememberKey.IsChecked != true) KeyPathInput.Text = "";
        if (RememberUser.IsChecked != true) UserInput.Text = "";
        MainTabs.SelectedIndex = 0;
        AuthStatus.Text = "Conexión bloqueada. Para volver a operar tenés que autenticarte.";
    }

    private void BuildActionButtons()
    {
        if (_connectedProfile is null) return;
        var modules = AdminCommandCatalog.For(_connectedProfile.Platform);
        var groups = modules.Select(x => x.GroupName).Distinct().ToList();
        if (_connectedProfile.Platform == TargetPlatform.Windows)
            groups.Add("Windows");
        else
            groups.Add("Todos los módulos");
        ModuleGroupSelector.ItemsSource = groups;
        ModuleGroupSelector.SelectedIndex = 0;
        UseSudoToggle.IsVisible = _connectedProfile.Platform != TargetPlatform.Windows;
        PopulateActionButtons();
        PopulateUniversalResults();
        UpdateRiskDisplay();
    }

    private void ModuleGroupChanged(object? sender, SelectionChangedEventArgs e) => PopulateActionButtons();

    private void PopulateActionButtons()
    {
        OptionButtons.Children.Clear();
        if (_connectedProfile is null) return;
        var selected = ModuleGroupSelector.SelectedItem as string ?? "";
        if (_connectedProfile.Platform == TargetPlatform.Windows)
        {
            foreach (var action in PlatformReadOnlyActions.For(TargetPlatform.Windows))
            {
                var actionCopy = action;
                var button = new Button
                {
                    Content = "🟢 " + AvailabilityMark(action.Command) + " " + action.Label,
                    Margin = new Thickness(0, 0, 8, 8)
                };
                button.Click += (_, _) => SelectAction(actionCopy);
                OptionButtons.Children.Add(button);
            }
            return;
        }
        var commands = AdminCommandCatalog.For(_connectedProfile.Platform)
            .Where(x => selected == "Todos los módulos" || x.GroupName == selected);
        foreach (var cmd in commands)
        {
            var captured = cmd;
            var unavailable = _remoteDictionary is not null &&
                RemoteCommandDictionary.Root(cmd.Command) is { } exe &&
                !_remoteDictionary.Has(exe);
            var displayRisk = unavailable ? CommandRisk.Red : cmd.Risk;
            var button = new Button
            {
                Content = AdminCommandCatalog.Dot(displayRisk) + " " +
                    AvailabilityMark(cmd.Command) + " " + cmd.LabelEs,
                Margin = new Thickness(0, 0, 6, 5),
                Padding = new Thickness(9, 5)
            };
            ToolTip.SetTip(button, cmd.Command + " — " + cmd.DescriptionEs +
                (unavailable ? "\nNo encontrado en PATH remoto; revisá instalación o entorno." : ""));
            button.Click += (_, _) => SelectAction(
                new ReadOnlyAction(captured.Id, captured.LabelEs, captured.Command));
            OptionButtons.Children.Add(button);
        }
    }

    private void SudoToggleChanged(object? sender, RoutedEventArgs e) => UpdateRiskDisplay();

    private void UpdateRiskDisplay()
    {
        if (RiskIndicator is null || RunAssistedButton is null || UseSudoToggle is null)
            return;
        var command = AssistedCommand?.Text?.Trim() ?? "";
        var platform = _connectedProfile?.Platform ?? TargetPlatform.OtherLinux;
        var risk = TryGetGuidedRisk(command, out var guidedRisk)
            ? guidedRisk : AdminCommandCatalog.Classify(platform, command);
        var useSudo = UseSudoToggle.IsChecked == true && platform != TargetPlatform.Windows;
        if (useSudo && risk == CommandRisk.Green)
            risk = CommandRisk.Yellow;
        var dot = AdminCommandCatalog.Dot(risk);
        RiskIndicator.Text = dot + " " + AdminCommandCatalog.RiskName(risk) +
            (useSudo ? " · sudo ACTIVO" : "");
        RunAssistedButton.Content = dot + " Ejecutar comando" + (useSudo ? " con sudo" : "");
        RunAssistedButton.Background = risk switch
        {
            CommandRisk.Green => Brushes.SeaGreen,
            CommandRisk.Yellow => Brushes.Goldenrod,
            _ => Brushes.Firebrick
        };
        RunAssistedButton.Foreground = risk == CommandRisk.Yellow ? Brushes.Black : Brushes.White;
        ToolTip.SetTip(RunAssistedButton,
            "Un clic ejecuta por SSH. " + AdminCommandCatalog.RiskName(risk));
    }

    private void SelectAction(ReadOnlyAction action)
    {
        // Match a JSON schema; never manufacture buttons by splitting command text.
        // Any catalog entry without a detailed schema still has a generic form.
        var admin = _connectedProfile is null ? null :
            AdminCommandCatalog.Find(_connectedProfile.Platform, action.Command);
        var definition = admin is not null
            ? GuidedCommandCatalog.Resolve(admin)
            : GuidedCommandCatalog.All.FirstOrDefault(x =>
                x.PresetCommands?.Contains(action.Command, StringComparer.Ordinal) == true)
              ?? GuidedCommandCatalog.All.FirstOrDefault(x =>
                string.Equals(x.Template, action.Command, StringComparison.Ordinal));
        if (definition is not null)
        {
            BeginGuided(definition);
            HintDetail.Text = action.Label + " — " + definition.ExplanationEs;
            return;
        }

        // Legacy fallback for an action loaded from an older plugin.
        HideGuidedBuilder();
        AssistedCommand.Text = action.Command;
        HintDetail.Text = action.Label + " — " + action.Command;
    }

    private void UpdateBlockPreview() =>
        BlockPreview.Text = "Bloques: " + (_blocks.Count == 0 ? "(vacío)" : string.Join("  →  ", _blocks));

    private void UndoBlockClicked(object? sender, RoutedEventArgs e)
    {
        if (_blocks.Count > 0) _blocks.RemoveAt(_blocks.Count - 1);
        AssistedCommand.Text = string.Join(" ", _blocks);
        UpdateBlockPreview();
    }

    private void ClearCommandClicked(object? sender, RoutedEventArgs e)
    {
        _blocks.Clear();
        AssistedCommand.Text = "";
        UpdateBlockPreview();
    }

    private void ReloadSuggestions()
    {
        if (_connectedProfile is null) return;
        // Autocomplete uses the local catalog, server-specific help and the
        // discovered PATH dictionary. Unknown tools remain usable manually.
        var learned = _connectedProfile.Platform == TargetPlatform.Windows
            ? Enumerable.Empty<CommandHint>() : ModuleDraftStore.LoadHints();
        _matches = SmartCliCompletion.Search(
            AssistedCommand.Text, _connectedProfile.Platform, _remoteDictionary, learned);
        Suggestions.ItemsSource = _matches;
        Suggestions.SelectedIndex = _matches.Count > 0 ? 0 : -1;
        SuggestionButtons.Children.Clear();
        foreach (var hint in _matches.Take(8))
        {
            var candidate = hint;
            var choice = new Button
            {
                Content = (candidate.Installed == true ? "✓ " :
                           candidate.Installed == false ? "○ " : "? ") +
                          (candidate.IsCorrection ? "✏ " : "") + candidate.Label,
                Margin = new Thickness(0, 0, 6, 6)
            };
            ToolTip.SetTip(choice, candidate.Detail +
                (candidate.Installed == false ? " · No detectado en el PATH actual." : ""));
            choice.Click += (_, _) => ApplySmartSuggestion(candidate);
            SuggestionButtons.Children.Add(choice);
        }
        if (_matches.Count == 0)
            HintDetail.Text = "Sin sugerencias para este texto. Podés ejecutar el comando manualmente.";
    }

    private void ApplySmartSuggestion(SmartCliSuggestion suggestion)
    {
        AssistedCommand.Text = suggestion.Replacement;
        AssistedCommand.CaretIndex = suggestion.Replacement.Length;
        AssistedCommand.Focus();
        HintDetail.Text = suggestion.Label + " — " + suggestion.Detail +
            (suggestion.Installed == false
                ? " · No encontrado en PATH; podría requerir instalación o activar su entorno."
                : suggestion.Installed == true ? " · Disponible en PATH." :
                " · Estado no comprobado.");
    }

    private void AssistedTextChanged(object? sender, TextChangedEventArgs e)
    {
        ReloadSuggestions();
        UpdateRiskDisplay();
    }

    private void SuggestionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (Suggestions.SelectedItem is SmartCliSuggestion hint)
            HintDetail.Text = hint.Label + " — " + hint.Detail +
                (hint.Installed == true ? " · Encontrado en PATH." :
                 hint.Installed == false ? " · No detectado en PATH." :
                 " · Estado remoto desconocido.");
    }

    private void SessionShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && MainTabs.SelectedItem == CommandsTab
            && AppSettingsStore.Shared.Matches("ExecuteCommand", e))
        {
            _ = ExecuteApprovedCommandAsync(AssistedCommand.Text ?? "");
            e.Handled = true;
        }
    }

    private void AssistedKeyDown(object? sender, KeyEventArgs e)
    {
        // Terminal muscle memory: Enter RUNS the current text; Tab completes.
        // Keep the configured Ctrl+Enter / alternate Execute shortcut as well.
        if (AppSettingsStore.Shared.Matches("ExecuteCommand", e) ||
            (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None))
        {
            _ = ExecuteApprovedCommandAsync(AssistedCommand.Text ?? "");
            e.Handled = true;
        }
        else if (e.Key is Key.Down or Key.Up && e.KeyModifiers == KeyModifiers.None)
        {
            if (_matches.Count == 0) return;
            Suggestions.SelectedIndex = Math.Clamp(Suggestions.SelectedIndex +
                (e.Key == Key.Down ? 1 : -1), 0, _matches.Count - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && e.KeyModifiers == KeyModifiers.None)
        {
            // Without a suggestion, let normal Tab focus navigation work.
            if (Suggestions.SelectedItem is not SmartCliSuggestion) return;
            UseSelectedSuggestion();
            e.Handled = true;
        }
    }

    private void UseSelectedSuggestion()
    {
        if (Suggestions.SelectedItem is not SmartCliSuggestion hint) return;
        ApplySmartSuggestion(hint);
    }

    private void UseSuggestionClicked(object? sender, RoutedEventArgs e) => UseSelectedSuggestion();

    private async void RunAssistedClicked(object? sender, RoutedEventArgs e) =>
        await ExecuteApprovedCommandAsync(AssistedCommand.Text ?? "");

    private async Task ExecuteApprovedCommandAsync(string input)
    {
        if (_busy) return;
        if (_connectedProfile is null || _connectedCredentials is null)
        {
            ExecutionStatus.Text = "Primero autenticá SSH en la pestaña Entorno y SSH.";
            return;
        }
        var command = input.Trim();
        if (!CommandHints.CanRun(_connectedProfile.Platform, command))
        {
            ExecutionStatus.Text = "Escribí un comando para ejecutar.";
            return;
        }
        // There is deliberately NO second confirmation. The button and status
        // expose the risk before executing; the SSH identity was verified at login.
        var useSudo = UseSudoToggle.IsChecked == true &&
            _connectedProfile.Platform != TargetPlatform.Windows;
        var hasExistingSudo = command.StartsWith("sudo ", StringComparison.Ordinal);
        var remoteCommand = command;
        if (useSudo && !hasExistingSudo)
        {
            // Execute the complete shell expression as root, including pipes and
            // redirects. A single-quoted shell argument cannot escape by injecting '.
            var escaped = command.Replace("'", "'" + '"' + "'" + '"' + "'", StringComparison.Ordinal);
            remoteCommand = "sudo -n -- sh -c '" + escaped + "'";
        }
        var started = DateTimeOffset.UtcNow;
        var auditHost = _connectedProfile.Host;
        var auditPort = _connectedProfile.Port;
        try
        {
            _busy = true;
            ExecutionStatus.Text = "Ejecutando por SSH " + (useSudo ? "con sudo -n..." : "...");
            // Side log is already docked; do not steal focus from the fixed command bar.
            Output.Text = "$ " + remoteCommand + "\n";
            ShowCommandInProgress(started, auditHost, auditPort, remoteCommand);
            var timeoutSeconds = _activePlan is not null &&
                string.Equals(command, _activePlan.Command, StringComparison.Ordinal)
                ? _activePlan.TimeoutSeconds :
                command.Contains("apt-get install", StringComparison.Ordinal)
                    ? 1200 : 300;
            var result = await _ssh.ExecuteAsync(_connectedProfile, _connectedCredentials,
                remoteCommand, TimeSpan.FromSeconds(timeoutSeconds));
            Output.Text = "$ " + remoteCommand + "\n\n" + result.StdOut
                + (string.IsNullOrWhiteSpace(result.StdErr) ? "" : "\n[STDERR]\n" + result.StdErr);
            RecordCommand(started, auditHost, auditPort, remoteCommand, useSudo,
                result.ExitCode, result.StdOut, result.StdErr);
            ExecutionStatus.Text = "Comando completado. Código de salida: " + result.ExitCode +
                (useSudo && result.ExitCode != 0 ? " · sudo -n exige permisos sin contraseña interactiva." : "");
        }
        catch (Exception ex)
        {
            RecordCommand(started, auditHost, auditPort, remoteCommand,
                useSudo, null, "", "", ex.Message);
            ExecutionStatus.Text = "Consulta fallida: " + ex.Message;
        }
        finally
        {
            FinishOperation();
            SyncDetachedConsole();
        }
    }

    private async void ScanHelpClicked(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_connectedProfile is null || _connectedCredentials is null)
        {
            ScanStatus.Text = "Autenticá SSH antes de explorar herramientas."; return;
        }
        if (_connectedProfile.Platform == TargetPlatform.Windows)
        {
            ScanStatus.Text = "El descubridor Windows todavía no está disponible."; return;
        }
        if (HelpConsent.IsChecked != true)
        {
            ScanStatus.Text = "Confirmá que el destino es un servidor de pruebas autorizado."; return;
        }

        try
        {
            _busy = true;
            ScanStatus.Text = "Consultando ayuda...";
            var scan = new HelpDiscoveryService(_ssh);
            var result = await scan.ScanAsync(_connectedProfile, _connectedCredentials,
                ToolToDiscover.Text ?? "", message => Dispatcher.UIThread.Post(() => ScanStatus.Text = message));
            ScanOutput.Text = "Borrador: " + result.Draft.Id + "\nConsultas: "
                + result.Draft.Commands.Count + "\nRuta: " + result.DraftPath
                + "\n\nComandos agregados como sugerencias. Podés ejecutarlos desde la consola (riesgo desconocido: rojo).";
            ReloadSuggestions();
        }
        catch (Exception ex) { ScanStatus.Text = "No se pudo explorar: " + ex.Message; }
        finally
        {
            FinishOperation();
            HelpConsent.IsChecked = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseDetachedConsoleForDisposal();
        // Do not clear key bytes while an SSH operation is still using them.
        if (_busy)
        {
            _disposeAfterBusy = true;
            return;
        }
        DisconnectSession();
    }
}
