# Changelog

All notable changes to SolarisPKN-AntiCli are documented here.
The project follows [Semantic Versioning](https://semver.org/) as a goal; early previews may change APIs and profile formats.

## [0.1.0] — 2026-10-14 (first source preview)

### Added
- Avalonia desktop UI for remote SSH environments: Debian, Ubuntu, Windows and other Linux systems.
- Host-key SHA256 verification before accepting SSH authentication.
- Password and SSH private-key authentication with optional passphrase.
- Independent opt-in DPAPI storage for username, private-key material, password and passphrase.
- Guided read-only command catalogs, visual tokens and command suggestions.
- Multiple independent session tabs, cloned authentication contexts and windows.
- Appearance settings, theme and accent selection, custom window background and remappable shortcuts.
- Experimental `--help` scanner with bounded discovery, local logs and non-executable draft modules.
- Bilingual entrypoint documentation, GPL-3.0-only license and Windows CI workflow.

### Known limitations
- Fresh end-to-end tests are still required for this source revision.
- SSH commands run in separate connections; there is no persistent shell/PTY.
- Discovered modules cannot execute; automatic AI translation and a module approval UI remain future work.
- Secure credential persistence currently uses Windows DPAPI and is therefore Windows-only.
- The source code is published as an **early preview**, not as production-ready infrastructure software.
