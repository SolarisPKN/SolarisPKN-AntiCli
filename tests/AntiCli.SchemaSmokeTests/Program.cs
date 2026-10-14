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

Verify(AdminCommandCatalog.For(TargetPlatform.Ubuntu).Count == 146,
    "146 Linux quick actions load from embedded JSON");
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
Console.WriteLine("ALL SCHEMA SMOKE TESTS PASSED.");
