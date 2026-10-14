using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace SolarisPKN.AntiCli;
public partial class CommandWorkspaceView : UserControl
{
 public CommandWorkspaceView(){InitializeComponent();}
 public TextBlock RemoteInfoRef => RemoteInfo;
 public TextBox UniversalSearchRef => UniversalSearch;
 public WrapPanel UniversalResultsRef => UniversalResults;
 public TextBlock InstalledDictionaryStatusRef => InstalledDictionaryStatus;
 public ComboBox ModuleGroupSelectorRef => ModuleGroupSelector;
 public WrapPanel SuggestionButtonsRef => SuggestionButtons;
 public ListBox SuggestionsRef => Suggestions;
 public TextBlock HintDetailRef => HintDetail;
 public WrapPanel OptionButtonsRef => OptionButtons;
 public Border GuidedPanelRef => GuidedPanel;
 public TextBlock GuidedTitleRef => GuidedTitle;
 public TextBlock GuidedDescriptionRef => GuidedDescription;
 public TextBlock HelpImportStatusRef => HelpImportStatus;
 public Border MissingToolPanelRef => MissingToolPanel;
 public TextBlock MissingToolMessageRef => MissingToolMessage;
 public TextBlock MissingToolCandidateRef => MissingToolCandidate;
 public Button InstallMissingToolButtonRef => InstallMissingToolButton;
 public StackPanel GuidedOptionsRef => GuidedOptions;
 public StackPanel GuidedArgumentsRef => GuidedArguments;
 public Border PackageRequirementsPanelRef => PackageRequirementsPanel;
 public TextBlock PackageRequirementsStatusRef => PackageRequirementsStatus;
 public Button InstallRequiredPackagesButtonRef => InstallRequiredPackagesButton;
 public TextBlock GuidedErrorRef => GuidedError;
 public TextBlock GuidedPlanInfoRef => GuidedPlanInfo;
}
