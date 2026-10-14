# Third-party notices / Avisos de terceros

SolarisPKN-AntiCli source is licensed under **GNU GPL v3 only** (`GPL-3.0-only`). Third-party libraries have their **own** copyright holders and license terms; the project does not claim ownership over them.

| Direct dependency | Version in `csproj` | License / upstream |
| --- | --- | --- |
| Avalonia | 11.3.22 | MIT — [upstream licence](https://github.com/AvaloniaUI/Avalonia/blob/main/licence.md) |
| Avalonia.Desktop | 11.3.22 | MIT — same framework |
| Avalonia.Themes.Fluent | 11.3.22 | MIT — same framework |
| SSH.NET | 2026.0.0 | MIT — [upstream licence](https://github.com/sshnet/SSH.NET/blob/develop/LICENSE) |
| System.Security.Cryptography.ProtectedData | 8.0.0 | .NET Foundation/Microsoft MIT — [runtime licence](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |

Included original license and notice files:

- [Avalonia license](third_party/Avalonia-LICENSE.txt)
- [Avalonia notices](third_party/Avalonia-NOTICE.md)
- [SSH.NET license](third_party/SSH.NET-LICENSE.txt)
- [.NET runtime license](third_party/dotnet-runtime-LICENSE.txt)

**Important:** A self-contained .NET/Avalonia build also includes **transitive** packages and native dependencies. The complete dependency graph and their redistributed notices must be checked at build/release time. This file identifies the main direct dependencies; it does not certify an exhaustive software bill of materials.

**Español:** Las bibliotecas de terceros no pasan a ser propiedad de SolarisPKN por incorporarlas al programa. Se conservan los textos originales de sus licencias y atribuciones. Antes de distribuir nuevas versiones, verificá las dependencias transitivas y sus condiciones de redistribución.
