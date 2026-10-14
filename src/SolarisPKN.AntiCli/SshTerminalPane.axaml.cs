using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace SolarisPKN.AntiCli;
public partial class SshTerminalPane : UserControl
{
 public SshTerminalPane(){InitializeComponent();}
 public Button ExpandConsoleButtonRef => ExpandConsoleButton;
 public Button DetachConsoleButtonRef => DetachConsoleButton;
 public Button DockPositionButtonRef => DockPositionButton;
 public Button HideConsoleButtonRef => HideConsoleButton;
 public TextBox OutputRef => Output;
 public ListBox CommandHistoryListRef => CommandHistoryList;
 public Button CopyHistoryCommandButtonRef => CopyHistoryCommandButton;
 public Button ReuseHistoryCommandButtonRef => ReuseHistoryCommandButton;
 public Button CopyLogButtonRef => CopyLogButton;
 public Button ExportLogButtonRef => ExportLogButton;
 public Button ClearHistoryButtonRef => ClearHistoryButton;
 public TextBox CommandHistoryLogRef => CommandHistoryLog;
}
