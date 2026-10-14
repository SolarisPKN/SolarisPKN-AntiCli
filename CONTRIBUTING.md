# Contributing to SolarisPKN-AntiCli

Thank you for helping improve Solaris AntiCLI. This is an early-stage desktop project.

## Ground rules

- Keep source contributions compatible with **GPL-3.0-only** and preserve third-party notices and license requirements.
- Never commit passwords, SSH private keys, passphrases, API tokens, host-specific secrets, DPAPI blobs, user profile directories or confidential command output.
- Do not enable execution of commands merely because they appear in `--help` or are suggested by an AI model.
- New modules must describe their intended host platforms, arguments, validation, permissions and risks. Modules remain drafts until explicitly reviewed.
- Test new UI logic on Windows and provide reproducible build output. Never claim SSH integration success without actually testing against an authorized host.
- Keep the source under `src/SolarisPKN.AntiCli/` and documentation under `docs/`.

## Build from source

```powershell
dotnet restore ./src/SolarisPKN.AntiCli/SolarisPKN.AntiCli.csproj
dotnet build ./src/SolarisPKN.AntiCli/SolarisPKN.AntiCli.csproj -c Release
```

Use descriptive pull request titles, summarize risks, and state whether tests were run. If translating user-facing text, preserve exact command syntax and security terms.
