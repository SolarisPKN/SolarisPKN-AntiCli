# SolarisPKN-AntiCli 0.1.0 — First public preview

Solaris AntiCLI is an experimental open-source Avalonia/.NET desktop client for managing reviewed CLI actions through SSH.

**Highlights:** remote OS selection, SHA256 host-key pinning, opt-in DPAPI protection for individual SSH credentials, multiple independent tabs/windows, cloning of authenticated SSH contexts, visual command building, command autocompletion and experimental `--help` discovery into **non-executable** module drafts.

**Supported client for this preview:** Windows x64. Remote hosts: Debian, Ubuntu, other Linux distributions and Windows with an SSH server.

**Important:** this is a preview, not audited or recommended for production systems. Rebuild and validate the exact release commit, verify server fingerprints independently, and only scan `--help` on authorized test machines. There is no persistent interactive shell and no automatic module execution or AI translation.

**License:** GNU GPL v3 only (`GPL-3.0-only`). The complete source code for this release is available from the corresponding Git tag.

See [README.md](README.md) · [README.es.md](README.es.md) · [LICENSE](LICENSE).
