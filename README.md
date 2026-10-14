# SolarisPKN-AntiCli

**Solaris AntiCLI System** — a desktop GUI for working with command-line tools on remote servers through SSH, featuring guided actions, visual command composition, and controlled CLI help discovery.

🇬🇧 **English** · [Español](README.es.md)

> **Early preview 0.1.0 — work in progress.** This software has not been security-audited for production infrastructure. The most recent recorded local build predates some UI changes; rebuild and test the current sources before distributing a binary.

## Purpose

Bring terminal capabilities into a lightweight visual application: identify the remote operating system, authenticate via SSH, and explore reviewed actions without memorizing arguments. Remote hosts do **not** need a graphical desktop.

## Features implemented in source

- **Remote environments:** profiles for Debian, Ubuntu, Windows, and other Linux distributions; the selected OS is checked after SSH authentication.
- **SSH and first contact:** username + password or private key with passphrase. On a new VPS, AntiCLI discovers the host's public key **without sending credentials**, shows its SHA256 fingerprint, and asks you to explicitly trust it (TOFU) or compare it with your provider first. Subsequent connections enforce the stored host:port pin; changed keys are blocked unless explicitly reviewed.
- **Four independent DPAPI opt-ins:** remember username, private-key contents, password, and passphrase. **All are optional** and unchecked fields are not persisted. Profiles live outside the repository.
- **Multiple sessions:** open an empty tab, clone authenticated credentials into a tab, create another window, or clone a session into a separate window. Each session owns its command state and output.
- **Administrator command builder:** 146 Linux quick actions across 17 modules, grouped by purpose, with 🟢/🟡/🔴 risk dots. Every manually entered command can execute; unknown commands default to red. The Execute button reflects the risk and runs immediately, without a second confirmation. Optional per-session **sudo** toggle uses non-interactive SSH execution.
- **Discoverable modules:** bounded `--help` exploration on authorized test servers, with evidence logs and draft suggestions. Administrators can execute suggestions, including custom commands; a draft does not imply safety.
- **Preferences:** ⚙ settings, system/dark/light themes, configurable accent/background, and six customizable keyboard shortcuts.
- **Desktop technology:** C# / .NET 8 + Avalonia — no bundled Chromium browser.

## Quick start

**Build requirements:** Windows x64, .NET 8 SDK or newer, NuGet access for initial package restore. The local client relies on Windows DPAPI. Remote machines require a working SSH server and appropriate permissions.

```powershell
git clone https://github.com/SolarisPKN/SolarisPKN-AntiCli.git
cd SolarisPKN-AntiCli

# Restore, build and launch from source:
dotnet run --project .\src\SolarisPKN.AntiCli\SolarisPKN.AntiCli.csproj

# Or publish a self-contained win-x64 executable:
.\COMPILAR_ANTICLI.ps1
```

You can also double-click `COMPILAR_ANTICLI.cmd`. The local published executable is generated under `artifacts/release/win-x64/`, excluded from Git. For a build without publishing, run `./COMPILAR_ANTICLI.ps1 -BuildOnly`.

**Typical workflow:** open a tab, select remote OS, enter the host, user, port and credentials, then select **Authenticate via SSH**. When the host is new, review its automatically detected SHA256 key and explicitly accept or cancel before SSH authentication. For a changed key, choose **Detect/review server key** and verify it with your provider first. Command and module panels only unlock after the SSH and platform checks succeed.

## Security and privacy

- No credentials belong in this repository. Profiles, preferences, and discovery logs are kept under `%APPDATA%\SolarisPKN\AntiCLI\`.
- Windows DPAPI `CurrentUser` is **not** a master password: another process running under the same Windows user may be able to access protected material.
- Cloning copies credentials **in memory only**; it does not save secrets. It is not a persistent interactive shell: each operation opens a fresh SSH connection.
- `--help` is **not guaranteed to be side-effect free**. Only explore help on explicitly authorized test machines.
- A risk color is guidance only. **Red commands run with one click**, can be destructive, and execute with your SSH privileges. **sudo** uses `sudo -n` and requires noninteractive sudo authorization.
- Fully automated AI translations, module activation workflows, and persistent SSH terminals are **not yet implemented**.
- Do not access corporate credentials or infrastructure without permission.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Security](docs/SECURITY.md)
- [Assisted CLI](docs/ASSISTED_CLI.md)
- [Administrative command modules: 146 actions](docs/COMMAND_MODULES.md) ([Español](docs/COMMAND_MODULES.es.md))
- [Multiple sessions](docs/MULTI-SESSION.md)
- [Appearance and keyboard preferences](docs/SETTINGS.md)
- [Help-discovery security](docs/SCANNER-SECURITY.md)
- [Test plan](docs/TEST-PLAN.md)
- [Skill contract for a future AI integration](SKILL.md)

> Supporting documentation is currently mostly in Spanish. English contributions are welcome.

## Publishing with GitHub Desktop

**GitHub CLI is not required.** Add this existing local Git repository through **GitHub Desktop → File → Add local repository**. Review the source changes, **Commit to main**, then **Publish repository** as `SolarisPKN-AntiCli` under account **SolarisPKN**. To make it public, uncheck **Keep this code private**. See the [GitHub Desktop walkthrough](docs/GITHUB_DESKTOP.md) ([Spanish](docs/GITHUB_DESKTOP.es.md)).

You can publish the **source only** first. To prepare a downloadable Windows preview later, run `PREPARAR_RELEASE.cmd`. It recompiles the current source and creates a ZIP with the executable and legal notices plus SHA256 checksums under `artifacts/release/`. These binaries are excluded from Git; upload them separately through your repository's **GitHub Releases** page after verifying the CI build.

## Version 0.1.0 status

This is the **first public preview of the source code**, not a stable product. End-to-end tests of real SSH targets, GUI behavior, AI-assisted translations, module editing, and cross-platform client support are still pending. The immediate goal is a clean, reproducible CI build and verification of a binary built from the same commit.

## License

Copyright © 2026 **SolarisPKN contributors**.

Licensed under the **GNU General Public License, version 3 only (GPL-3.0-only)**. See [LICENSE](LICENSE) for the full text and [third-party notices](THIRD_PARTY_NOTICES.md) for component credits. Provided **without warranty**. Third-party libraries and dependencies retain their own applicable licenses.

---

Built as part of the **SolarisPKN** ecosystem. Contributions and reproducible bug reports are welcome; please do not include credentials or private system information.
