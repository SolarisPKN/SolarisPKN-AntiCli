using SolarisPKN.AntiCli.Models;
using SolarisPKN.AntiCli.Modules;

static void Verify(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("[FAIL] " + message);
    Console.WriteLine("[PASS] " + message);
}
static Dictionary<string, string> Data(params (string Key, string Value)[] pairs) =>
    pairs.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
static GuidedCommand Module(string name) =>
    GuidedCommandCatalog.Find(name) ??
        throw new InvalidOperationException("Missing module: " + name);

Verify(AdminCommandCatalog.For(TargetPlatform.Ubuntu).Count >= 146,
    "Linux quick actions load from embedded JSON");
Verify(AdminCommandCatalog.For(TargetPlatform.Windows).Count == 7,
    "7 Windows quick actions load from embedded JSON");
Verify(GuidedCommandCatalog.All.Count >= 155,
    "Every catalog action has a generic form");
Verify(GuidedCommandCatalog.Find("uname")!.Flags.Count >= 10,
    "GNU uname schema has ordered option metadata");
Verify(GuidedCommandCatalog.Search("crear usuarios", TargetPlatform.Ubuntu)
    .Any(x => x.Id == "adduser"), "Natural-language user task search");
Verify(GuidedCommandCatalog.Search("yocto bitbake", TargetPlatform.Ubuntu)
    .Any(x => x.Id == "yocto-bitbake-build"), "Natural-language Yocto task search");
Verify(!GuidedCommandCatalog.Search("bitbake", TargetPlatform.Windows).Any(),
    "Linux-only operations do not appear for Windows");
Verify(GuidedCommandCatalog.SupportsPlatform(
    GuidedCommandCatalog.Find("action.windows.os")!, TargetPlatform.Windows),
    "Windows form compatibility derives from metadata");
Verify(AdminCommandCatalog.Classify(TargetPlatform.Ubuntu, "rm -rf /important") ==
    CommandRisk.Red, "Unknown/dangerous command defaults red");

var uname = GuidedCommandCatalog.Build(Module("uname"),
    [new GuidedFlagChoice("-a", "")], Data());
Verify(uname.Command == "uname -a" && uname.Risk == CommandRisk.Green,
    "uname -a built from flag checkbox (no command-specific C# branch)");

var add = GuidedCommandCatalog.Build(Module("adduser"),
    [new GuidedFlagChoice("--disabled-password", ""),
     new GuidedFlagChoice("--gecos", "")],
    Data(("account", "alumno")), quantity: 20);
Verify(add.Operations == 20 && add.Command.Contains("01 02 03") &&
    add.Command.Contains(" 20; do adduser") &&
    add.Command.Contains("alumno" + "$" + "{suffix}") && add.Command.Contains("|| exit $?"),
    "20 users expand as alumno01 through alumno20 with fail-fast");
try
{
    _ = GuidedCommandCatalog.Build(Module("adduser"),
        [new GuidedFlagChoice("--disabled-password", "")],
        Data(("account", "alumno;cat /etc/shadow")));
    throw new InvalidOperationException("Invalid account accepted");
}
catch (ArgumentException)
{
    Console.WriteLine("[PASS] User name validation rejects shell metacharacters");
}

var apt = GuidedCommandCatalog.Build(Module("apt-install"),
    [new GuidedFlagChoice("--no-install-recommends", "")],
    Data(("packages", "curl jq git")));
Verify(apt.Command.Contains("'curl' 'jq' 'git'") &&
       apt.Command.Contains("apt-get"), "APT handles several packages safely");

var repo = GuidedCommandCatalog.Build(Module("apt-repository"), [],
    Data(("repo_id", "example"), ("repo_uri", "https://packages.example.org/debian"),
         ("suite", "noble"), ("components", "main"),
         ("key_uri", "https://packages.example.org/key.asc"),
         ("fingerprint", "0123456789ABCDEF0123456789ABCDEF01234567")));
Verify(repo.Command.Contains("Signed-By:") &&
    repo.Command.Contains("/etc/apt/keyrings/") &&
    repo.Command.Contains("gpg --batch --yes --dearmor") &&
    repo.Command.Contains("apt-get update") &&
    !repo.Command.Contains("apt-key"),
    "APT repository uses fingerprint check and signed-by keyring");
try
{
    _ = GuidedCommandCatalog.Build(Module("apt-repository"), [],
        Data(("repo_id", "example"), ("repo_uri", "http://insecure.example.org"),
             ("suite", "noble"), ("components", "main"),
             ("key_uri", "https://packages.example.org/key.asc"),
             ("fingerprint", "0123456789ABCDEF0123456789ABCDEF01234567")));
    throw new InvalidOperationException("Insecure repository URL accepted");
}
catch (ArgumentException)
{
    Console.WriteLine("[PASS] Third-party repository requires HTTPS");
}

var yocto = GuidedCommandCatalog.Build(Module("yocto-bitbake-build"),
    [new GuidedFlagChoice("-n", "")],
    Data(("poky", "/opt/poky"), ("build", "build"),
         ("recipe", "core-image-minimal")));
Verify(yocto.Command.Contains("oe-init-build-env") &&
    yocto.Command.Contains("bitbake -n") &&
    yocto.TimeoutSeconds == 7200,
    "Yocto initializes build environment and uses extended timeout");
var layer = GuidedCommandCatalog.Build(Module("yocto-add-layer"), [],
    Data(("poky", "/opt/poky"), ("build", "build"),
         ("layer", "/opt/meta-example")));
Verify(layer.Command.Contains("bitbake-layers add-layer"),
    "Yocto layer management uses the same generic renderer and compiler");
// Intentional conditional retry: 1 -> 2 -> IF -> 4 -> 1 until condition turns false.
// The graph has no zero-indegree node; the author supplies an explicit Start.
var retryWorkflow = new AutomationWorkflow(1, "retry until ready",
    [new AutomationStep("a", "check", "echo check")],
    Data(), [Data()], AutomationOrder.PerRow, true);
var retryGraph = new AutomationGraph(1,
    [
        new FlowNode("c1", FlowNodeKind.Command, "Paso 1", "echo 1", 10, 10),
        new FlowNode("c2", FlowNodeKind.Command, "Paso 2", "echo 2", 210, 10),
        new FlowNode("if3", FlowNodeKind.If, "¿Seguir?", "test -f /tmp/anticli-retry", 410, 10),
        new FlowNode("c4", FlowNodeKind.Command, "Paso 4", "sleep 1", 610, 10)
    ],
    [
        new FlowEdge("c1", "c2"),
        new FlowEdge("c2", "if3"),
        new FlowEdge("if3", "c4", FlowEdgeKind.True),
        new FlowEdge("c4", "c1", FlowEdgeKind.LoopBack)
    ],
    "c1");
AutomationGraphFile.Validate(retryGraph);
var retryScript = AutomationGraphRouter.Generate(retryWorkflow, retryGraph);
Verify(retryScript.Contains("while [ -n \"$__ac_state\" ]") &&
       retryScript.Contains("if sh -c 'test -f /tmp/anticli-retry'") &&
       retryScript.Contains("__ac_state='s0'") &&
       retryScript.Contains("__ac_state=''") &&
       !retryScript.Contains("20.000"),
       "IF + return to command 1 exports an unlimited conditional retry");
var serialized = AutomationGraphFile.Serialize(retryGraph);
Verify(AutomationGraphFile.Parse(serialized).StartNodeId == "c1",
    "Cyclic graph keeps its explicit entry point after JSON save/load");

var unlimitedUntil = new AutomationGraph(1,
    [
        new FlowNode("loop", FlowNodeKind.Loop, "Until ready",
            "test -f /tmp/ready", 50, 50, FlowLoopMode.Until, 0),
        new FlowNode("body", FlowNodeKind.Command, "Try again", "sleep 1", 300, 50)
    ],
    [
        new FlowEdge("loop", "body", FlowEdgeKind.LoopBody),
        new FlowEdge("body", "loop", FlowEdgeKind.LoopBack)
    ],
    "loop");
AutomationGraphFile.Validate(unlimitedUntil);
Verify(AutomationGraphRouter.Generate(retryWorkflow, unlimitedUntil)
    .Contains("if sh -c 'test -f /tmp/ready'"),
    "UNTIL without iteration cap exits when its test succeeds");
try
{
    _ = AutomationGraphRouter.Generate(retryWorkflow, retryGraph with
    {
        Edges = retryGraph.Edges.Append(new FlowEdge("if3", "c1", FlowEdgeKind.True)).ToArray()
    });
    throw new InvalidOperationException("Ambiguous IF was accepted.");
}
catch (InvalidDataException)
{
    Console.WriteLine("[PASS] Duplicate IF true branch is rejected on export");
}

Console.WriteLine("ALL SCHEMA SMOKE TESTS PASSED.");
