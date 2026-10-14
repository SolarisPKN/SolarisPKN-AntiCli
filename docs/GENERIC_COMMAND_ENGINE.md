# AntiCLI — Universal data-driven command forms

AntiCLI does **not** implement one window per command. One Avalonia form renders a versioned JSON schema describing the executable, selectable ordered flags, typed operands, validation, repeatable batches, command template and risk. Arbitrary custom SSH commands remain available.

## Data files

- `src/SolarisPKN.AntiCli/Data/admin-commands.json` — 146 Linux and 7 Windows quick actions; title, purpose, sample command and risk.
- `src/SolarisPKN.AntiCli/Data/guided-commands.json` — detailed schemas for GNU uname, account creation, APT operations, signed APT repositories, Yocto/BitBake, layer management and devtool.
- `GuidedCommandCatalog.cs` — generic schema loader, validation and template renderer. Every action without a specialized schema receives a basic editable form; **there are no per-tool screen classes**.
- `SessionView.Guided.cs` — one shared UI rendering options, contextual help, operands, flag order and repeatable batches.
- `%APPDATA%\SolarisPKN\AntiCLI\modules\*.json` — extensible user-owned schema files loaded at startup **without rebuilding**. Local modules are marked red by default and cannot silently override bundled entries.
- `%APPDATA%\SolarisPKN\AntiCLI\command-help\*.json` — option documentation fetched by clicking **Load flags from --help**, cached per SSH host, port and executable. Imported descriptions are unreviewed and may be in English.

Search for the task (“create users”, “kernel”, “install package”, “Yocto build”), select a matching entry, configure flags via checkboxes and ↑ ↓ controls, fill typed operands, inspect the shell preview and execute using the risk-colored button. Red actions execute on one click with no second prompt. The optional `sudo` toggle uses noninteractive `sudo -n`; it never sends the SSH password as a sudo password.

The account-creation schema supports batch numbering such as `alumno01…alumno20`. The shell batch stops at the first error. GNU uname outputs fields in its own fixed order, even when input flags are rearranged.

### APT sources and GPG trust

The external repository schema requires repository URL, suite, components, public GPG key URL and expected full fingerprint. It validates that the fetched key matches the supplied fingerprint, installs an isolated keyring in `/etc/apt/keyrings/`, writes a deb822 `.sources` file with `Signed-By` and runs `apt-get update`. No deprecated global `apt-key`, signature bypass, or hidden trust acceptance. Administrative rights, `curl` and `gpg` are required. Verify publisher credentials independently.

### Yocto / BitBake

The Yocto schemas cover recipe/image builds, recipe versions, layer listing/addition, recipes, clean tasks, environment inspection and devtool workflows. They source `oe-init-build-env` before invoking the tool. The remote build must have its dependencies and layers installed. Tool/recipe variations are handled through metadata and importing `--help`, not tool-specific Avalonia classes. Long builds still lack persistent streamed output.

### Adding a JSON module

Create a `SchemaVersion: 1` document containing a `Commands` array at `%APPDATA%\SolarisPKN\AntiCLI\modules\example.json`. Each entry defines `Id`, `Executable`, `TitleEs`, `ExplanationEs`, `SearchTerms`, `Risk`, `Template`, `Flags`, `Operands` and `Platforms`. Refer to the [Spanish guide and example](GENERIC_COMMAND_ENGINE.es.md) and bundled [schema data](../src/SolarisPKN.AntiCli/Data/guided-commands.json). Restart the app to load new modules.

Templates are executable shell programs, so do **not** import untrusted JSON without reviewing it. Risk colors are advisory, not an execution sandbox. The remote SSH account enforces actual permissions. `--help` is also not guaranteed to be side-effect-free.

Official references: [Yocto docs](https://docs.yoctoproject.org/), [BitBake](https://docs.yoctoproject.org/bitbake/), [Yocto layers](https://docs.yoctoproject.org/dev/dev-manual/layers.html), [APT sources](https://manpages.ubuntu.com/manpages/noble/en/man5/sources.list.5.html).

**Testing:** CI compilation is not a live SSH/Yocto/APT repository integration test; use disposable authorized environments to validate these workflows.
