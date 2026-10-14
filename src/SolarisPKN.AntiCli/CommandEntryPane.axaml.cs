using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace SolarisPKN.AntiCli;
public partial class CommandEntryPane : UserControl
{
 public CommandEntryPane(){InitializeComponent();}
 public Button ShowConsoleButtonRef => ShowConsoleButton;
 public TextBox AssistedCommandRef => AssistedCommand;
 public Button CopyCommandButtonRef => CopyCommandButton;
 public Button PasteCommandButtonRef => PasteCommandButton;
 public Button CopyOutputButtonRef => CopyOutputButton;
 public Button UseSuggestionButtonRef => UseSuggestionButton;
 public CheckBox UseSudoToggleRef => UseSudoToggle;
 public Button RunAssistedButtonRef => RunAssistedButton;
 public TextBlock RiskIndicatorRef => RiskIndicator;
 public TextBlock ExecutionStatusRef => ExecutionStatus;
 public WrapPanel TokenButtonsRef => TokenButtons;
 public TextBlock BlockPreviewRef => BlockPreview;
}
