namespace SolarisPKN.AntiCli.Models;

public sealed class ModuleDraft
{
    public string SchemaVersion { get; set; } = "1";
    public string Id { get; set; } = "";
    public string RootCommand { get; set; } = "";
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "draft-unreviewed";
    public List<DiscoveredCommand> Commands { get; set; } = [];
}

public sealed class DiscoveredCommand
{
    public string Command { get; set; } = "";
    public string DescriptionOriginal { get; set; } = "";
    public string DescriptionEs { get; set; } = "";
    public string HelpVariant { get; set; } = "--help";
    public int ExitCode { get; set; }
    public string HelpLogSha256 { get; set; } = "";
    public bool ApprovedForExecution { get; set; } = false;
    public List<DiscoveredFlag> Flags { get; set; } = [];
}

public sealed class DiscoveredFlag
{
    public string Token { get; set; } = "";
    public string DescriptionOriginal { get; set; } = "";
    public string DescriptionEs { get; set; } = "";
    public bool RequiresValue { get; set; }
    public string ValueHint { get; set; } = "";
}
