using Avalonia.Controls;

namespace SolarisPKN.AntiCli;

/// <summary>
/// A thin compatibility presenter: SessionView owns SSH coordination and
/// the three independent UserControls own their visual trees and scrolling.
/// All bindings point to the SAME controls; no duplicated state/SSH sessions.
/// </summary>
public partial class SessionView
{
    private Button RefreshDictionaryButton => BrowsePane.RefreshDictionaryButtonRef;
    private Button ImportGuidedHelpButton => BrowsePane.ImportGuidedHelpButtonRef;
    private Button CheckPackageRequirementsButton => BrowsePane.CheckPackageRequirementsButtonRef;
    private Button CloseGuidedButton => BrowsePane.CloseGuidedButtonRef;
    private TextBlock RemoteInfo => BrowsePane.RemoteInfoRef;
    private TextBox UniversalSearch => BrowsePane.UniversalSearchRef;
    private WrapPanel UniversalResults => BrowsePane.UniversalResultsRef;
    private TextBlock InstalledDictionaryStatus => BrowsePane.InstalledDictionaryStatusRef;
    private ComboBox ModuleGroupSelector => BrowsePane.ModuleGroupSelectorRef;
    private WrapPanel SuggestionButtons => BrowsePane.SuggestionButtonsRef;
    private ListBox Suggestions => BrowsePane.SuggestionsRef;
    private TextBlock HintDetail => BrowsePane.HintDetailRef;
    private WrapPanel OptionButtons => BrowsePane.OptionButtonsRef;
    private Border GuidedPanel => BrowsePane.GuidedPanelRef;
    private TextBlock GuidedTitle => BrowsePane.GuidedTitleRef;
    private TextBlock GuidedDescription => BrowsePane.GuidedDescriptionRef;
    private TextBlock HelpImportStatus => BrowsePane.HelpImportStatusRef;
    private Border MissingToolPanel => BrowsePane.MissingToolPanelRef;
    private TextBlock MissingToolMessage => BrowsePane.MissingToolMessageRef;
    private TextBlock MissingToolCandidate => BrowsePane.MissingToolCandidateRef;
    private Button InstallMissingToolButton => BrowsePane.InstallMissingToolButtonRef;
    private StackPanel GuidedOptions => BrowsePane.GuidedOptionsRef;
    private StackPanel GuidedArguments => BrowsePane.GuidedArgumentsRef;
    private Border PackageRequirementsPanel => BrowsePane.PackageRequirementsPanelRef;
    private TextBlock PackageRequirementsStatus => BrowsePane.PackageRequirementsStatusRef;
    private Button InstallRequiredPackagesButton => BrowsePane.InstallRequiredPackagesButtonRef;
    private TextBlock GuidedError => BrowsePane.GuidedErrorRef;
    private TextBlock GuidedPlanInfo => BrowsePane.GuidedPlanInfoRef;
    private Button ExpandConsoleButton => SideConsolePane.ExpandConsoleButtonRef;
    private Button DetachConsoleButton => SideConsolePane.DetachConsoleButtonRef;
    private Button DockPositionButton => SideConsolePane.DockPositionButtonRef;
    private Button HideConsoleButton => SideConsolePane.HideConsoleButtonRef;
    private TextBox Output => SideConsolePane.OutputRef;
    private ListBox CommandHistoryList => SideConsolePane.CommandHistoryListRef;
    private Button CopyHistoryCommandButton => SideConsolePane.CopyHistoryCommandButtonRef;
    private Button ReuseHistoryCommandButton => SideConsolePane.ReuseHistoryCommandButtonRef;
    private Button CopyLogButton => SideConsolePane.CopyLogButtonRef;
    private Button ExportLogButton => SideConsolePane.ExportLogButtonRef;
    private Button ClearHistoryButton => SideConsolePane.ClearHistoryButtonRef;
    private TextBox CommandHistoryLog => SideConsolePane.CommandHistoryLogRef;
    private Button ShowConsoleButton => CommandBar.ShowConsoleButtonRef;
    private TextBox AssistedCommand => CommandBar.AssistedCommandRef;
    private Button CopyCommandButton => CommandBar.CopyCommandButtonRef;
    private Button PasteCommandButton => CommandBar.PasteCommandButtonRef;
    private Button CopyOutputButton => CommandBar.CopyOutputButtonRef;
    private Button UseSuggestionButton => CommandBar.UseSuggestionButtonRef;
    private CheckBox UseSudoToggle => CommandBar.UseSudoToggleRef;
    private Button RunAssistedButton => CommandBar.RunAssistedButtonRef;
    private TextBlock RiskIndicator => CommandBar.RiskIndicatorRef;
    private TextBlock ExecutionStatus => CommandBar.ExecutionStatusRef;
    private WrapPanel TokenButtons => CommandBar.TokenButtonsRef;
    private TextBlock BlockPreview => CommandBar.BlockPreviewRef;
}
