using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Session logic wires to reusable command, terminal and manual-entry views.
/// Each view lays out and scrolls independently, while one SessionView retains
/// authoritative SSH credentials, command history and execution lifecycle.
/// </summary>
public partial class SessionView
{
    private void WireCommandPanels()
    {
        // Upper work area — editable/searchable command definitions.
        UniversalSearch.TextChanged += UniversalSearchChanged;
        ModuleGroupSelector.SelectionChanged += ModuleGroupChanged;
        Suggestions.SelectionChanged += SuggestionSelected;
        RefreshDictionaryButton.Click += RefreshDictionaryClicked;
        ImportGuidedHelpButton.Click += ImportGuidedHelpClicked;
        InstallMissingToolButton.Click += InstallMissingToolClicked;
        CheckPackageRequirementsButton.Click += CheckPackageRequirementsClicked;
        InstallRequiredPackagesButton.Click += InstallRequiredPackagesClicked;
        CloseGuidedButton.Click += CloseGuidedClicked;

        // SSH terminal — history controls and dock management.
        ExpandConsoleButton.Click += ToggleConsoleExpandClicked;
        DetachConsoleButton.Click += DetachConsoleClicked;
        SideConsolePane.DockPositionButtonRef.Click += ToggleDockPositionClicked;
        SideConsolePane.HideConsoleButtonRef.Click += HideConsoleClicked;
        CommandHistoryList.SelectionChanged += CommandHistorySelected;
        SideConsolePane.CopyHistoryCommandButtonRef.Click += CopyHistoryCommandClicked;
        SideConsolePane.ReuseHistoryCommandButtonRef.Click += ReuseHistoryCommandClicked;
        SideConsolePane.CopyLogButtonRef.Click += CopyLogClicked;
        SideConsolePane.ExportLogButtonRef.Click += ExportLogClicked;
        SideConsolePane.ClearHistoryButtonRef.Click += ClearHistoryClicked;

        // Reserved bottom row — stays on screen when upper panels scroll.
        AssistedCommand.TextChanged += AssistedTextChanged;
        AssistedCommand.KeyDown += AssistedKeyDown;
        UseSudoToggle.IsCheckedChanged += SudoToggleChanged;
        RunAssistedButton.Click += RunAssistedClicked;
        ShowConsoleButton.Click += ShowConsoleClicked;
        CommandBar.CopyCommandButtonRef.Click += CopyCommandClicked;
        CommandBar.PasteCommandButtonRef.Click += PasteCommandClicked;
        CommandBar.CopyOutputButtonRef.Click += CopyOutputClicked;
        CommandBar.UseSuggestionButtonRef.Click += UseSuggestionClicked;

        ApplyConsoleMode();
    }
}
