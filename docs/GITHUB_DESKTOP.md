# Publishing SolarisPKN-AntiCli with GitHub Desktop

This repository is already initialized locally on branch **main**. You do **not** need GitHub CLI (`gh`), PowerShell Git commands, or a second local repository.

## Publish the first source preview

1. Open **GitHub Desktop** and sign in to your GitHub account.
2. Select **File → Add local repository → Choose…** and navigate to the existing AntiCLI folder. Select **Add repository** (do not clone or create a nested repository):
   `...\SolarisPKN-AntiCli`
3. Open **Changes** and review the files. Make sure `LICENSE`, `README.md` (English), `README.es.md` (Spanish), `src/`, `docs/`, `third_party/`, and `.github/workflows/ci.yml` are present.
4. **Do not commit** `artifacts/`, `bin/`, `obj/`, `.env`, SSH keys, secrets, profiles, local settings, logs, `.pdb`, or personal backups. These should already be excluded by `.gitignore`. If anything unexpected appears, stop and uncheck it.
5. Enter a commit summary such as `feat: first GPL-3.0 AntiCLI preview v0.1.0` and click **Commit to main**.
6. Click **Publish repository**. Set repository name to **SolarisPKN-AntiCli** and select account/owner **SolarisPKN**. **Uncheck "Keep this code private"** to make it public. Confirm **Publish repository**.
7. Open the GitHub project in your browser and review `README.md`, its Spanish link, and the **Actions → Windows build (preview)** workflow. A failing CI build means that release binaries must wait until it is fixed.

**Important:** GitHub Desktop handles commits and repository publishing. It does **not** create a downloadable GitHub Release with binary assets during the Publish Repository operation.

## Optional downloadable preview (after successful validation)

Double-click `PREPARAR_RELEASE.cmd` to compile **the current source** and create:
- `artifacts/release/SolarisPKN-AntiCli-v0.1.0-win-x64.zip`
- `artifacts/release/SHA256SUMS.txt`

These local files are deliberately excluded from Git. The ZIP bundles the executable, GPLv3 license, both READMEs, release notes, and third-party notices.

After the repository is public and its GitHub Actions build has passed, use the **GitHub website** → your repository → **Releases** → **Draft a new release** → create/select tag `v0.1.0` → mark **Pre-release** → upload the ZIP and checksum text file → publish. Use [RELEASE_NOTES.md](../RELEASE_NOTES.md) as the announcement. You can publish just the source first and create this optional release later.

## Troubleshooting

- **Already an existing Git repository:** use **Add local repository**, not **Create new repository**.
- **Publish repository button missing:** check that you selected the local AntiCLI repo, committed the changes, and have no `origin` remote. This folder was initially created without a remote.
- **GitHub account not showing SolarisPKN:** sign into the correct account. If SolarisPKN is a personal account, choose your personal account; if it is an organization, select that organization.
- **A previous local commit exists:** review its file history before publishing; `.gitignore` cannot retroactively erase secrets that were already committed.
- **The preview EXE does not compile:** keep the source on GitHub as an early preview and fix the CI error before attaching any executable.
- **Two languages:** `README.md` must remain English and `README.es.md` Spanish.

Official docs: [Add a local repository](https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-a-repository-from-your-local-computer-to-github-desktop) · [Publish an existing project](https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop).
