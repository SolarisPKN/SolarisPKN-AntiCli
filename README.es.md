# SolarisPKN-AntiCli

**Solaris AntiCLI System** — cliente gráfico de escritorio para administrar herramientas de línea de comandos en servidores remotos mediante SSH, con acciones guiadas, construcción visual de comandos y descubrimiento controlado de documentación CLI.

🇦🇷 **Español** · [English](README.md)

> **Versión preliminar 0.1.0 — código en desarrollo.** No se considera una herramienta auditada para administrar equipos de producción. La última compilación local registrada es anterior a algunos de los cambios de interfaz; se requiere recompilar y probar esta versión antes de distribuir un ejecutable.

## Objetivo

Acercar la potencia de una terminal a una interfaz visual de bajo consumo: identificar qué sistema remoto estás usando, autenticarte por SSH y explorar acciones seguras sin tener que recordar cada argumento. No requiere instalar un entorno gráfico en el servidor remoto.

## Funciones presentes en el código

- **Entornos remotos:** perfiles para Debian, Ubuntu, Windows y otras distribuciones Linux; el sistema elegido se verifica al autenticar.
- **SSH y primera conexión:** autenticación con contraseña o clave privada con passphrase. Con un VPS nuevo, AntiCLI detecta la clave pública **sin enviar credenciales**, muestra el SHA256 y pide confirmación explícita para confiar (TOFU) o comparar con el proveedor. En conexiones posteriores comprueba la clave guardada por host:puerto y bloquea cambios no revisados.
- **Cuatro preferencias DPAPI independientes:** recordar usuario, contenido de clave privada, contraseña y passphrase. **Todas son optativas**; no se guardan cuando no se marcan. Los perfiles se almacenan fuera del repositorio.
- **Varias sesiones:** nueva pestaña, clonar autenticación en otra pestaña, nueva ventana y clonar sesión en ventana independiente. Cada sesión administra sus propios comandos y resultados.
- **Consola administrativa dinámica:** 146 acciones Linux y 7 Windows (17 categorías Linux) con puntos de riesgo 🟢/🟡/🔴. Buscador por intención y **un solo constructor visual universal** que se genera desde fichas JSON (flags ordenables, parámetros y lotes numerados); 20 fichas detalladas y formularios genéricos para el catálogo base. También permite comandos manuales; lo desconocido aparece rojo. **Ejecutar** cambia de color y actúa con un clic. Casilla **sudo** optativa por sesión.
- **Módulos detectados:** exploración acotada de `--help` en laboratorios autorizados; permite descubrir nuevas sugerencias, con evidencias. El administrador puede ejecutar comandos sugeridos, pero el descubrimiento no certifica su seguridad.
- **Ajustes:** botón ⚙, tema claro/oscuro/sistema, acento y fondo configurables, más seis atajos de teclado personalizables.
- **Interfaz local:** C# / .NET 8 + Avalonia, sin navegador Chromium embebido.

## Inicio rápido

**Requisitos para compilar:** Windows x64, SDK de .NET 8 o posterior, acceso a NuGet para restaurar paquetes. El cliente local usa DPAPI de Windows. Las máquinas remotas necesitan SSH funcional y los permisos adecuados.

```powershell
git clone https://github.com/SolarisPKN/SolarisPKN-AntiCli.git
cd SolarisPKN-AntiCli

# Restaurar, compilar y abrir desde el código:
dotnet run --project .\src\SolarisPKN.AntiCli\SolarisPKN.AntiCli.csproj

# O generar un .exe autocontenido (win-x64):
.\COMPILAR_ANTICLI.ps1
```

También podés ejecutar `COMPILAR_ANTICLI.cmd`. La publicación local queda en `artifacts/release/win-x64/`, que está excluida de Git. Para probar el código sin publicar: `./COMPILAR_ANTICLI.ps1 -BuildOnly`.

**Uso básico:** creá una pestaña, elegí plataforma y completá host, usuario, puerto y credenciales. Pulsá **Autenticar**: si es un servidor nuevo, AntiCLI detecta la huella SHA256 y pide que la revises y aceptes antes del inicio de sesión. Para una clave que cambió, usá **Detectar / revisar clave del servidor** y verificá el cambio con el proveedor. Solo al validar identidad, credenciales y sistema aparecen los paneles de comandos y creación de módulos.

## Seguridad y privacidad

- No se guardan credenciales en el repositorio. Los perfiles, configuraciones y logs se almacenan en `%APPDATA%\SolarisPKN\AntiCLI\`.
- DPAPI `CurrentUser` no es una contraseña maestra: otro proceso ejecutado con permisos de la misma cuenta Windows puede acceder a datos protegidos.
- Clonar sesión copia credenciales **solo en memoria**, no genera secretos persistentes. No duplica una shell interactiva: cada operación establece una conexión SSH nueva.
- `--help` **no garantiza ausencia de efectos secundarios**. Ejecutar exploración únicamente contra sistemas de prueba autorizados.
- Los colores de riesgo son informativos: **los comandos rojos se ejecutan con un clic**, pueden destruir datos y usan los permisos de la cuenta SSH. El interruptor `sudo` utiliza `sudo -n` y necesita permisos no interactivos.
- Las traducciones automáticas detalladas con IA, autorización de módulos y terminal SSH persistente **todavía no están implementadas**.
- No usar cuentas, claves ni infraestructura empresarial sin autorización.

## Documentación

- [Arquitectura](docs/ARCHITECTURE.md)
- [Seguridad](docs/SECURITY.md)
- [Consola asistida](docs/ASSISTED_CLI.md)
- [Motor de formularios dinámicos](docs/FORMULARIOS_DINAMICOS.md)
- [Catálogo administrativo: 146 acciones y 17 módulos](docs/COMMAND_MODULES.es.md) ([English](docs/COMMAND_MODULES.md))
- [Motor universal JSON, APT y Yocto](docs/GENERIC_COMMAND_ENGINE.es.md) ([English](docs/GENERIC_COMMAND_ENGINE.md))
- [Sesiones múltiples](docs/MULTI-SESSION.md)
- [Ajustes de apariencia y teclado](docs/SETTINGS.md)
- [Política de descubrimiento de ayuda](docs/SCANNER-SECURITY.md)
- [Plan de verificación](docs/TEST-PLAN.md)
- [Skill para futura integración con IA](SKILL.md)

## Publicar con GitHub Desktop

**No necesitás GitHub CLI.** Abrí **GitHub Desktop → File → Add local repository** y seleccioná esta carpeta Git existente. Revisá los archivos, elegí **Commit to main** y luego **Publish repository** con nombre `SolarisPKN-AntiCli` y propietario **SolarisPKN**. Para hacerlo público, desmarcá **Keep this code private**. Mirá la [guía detallada en español](docs/GITHUB_DESKTOP.es.md) ([English](docs/GITHUB_DESKTOP.md)).

Podés publicar **primero solo el código fuente**. Si después querés ofrecer una descarga Windows, ejecutá `PREPARAR_RELEASE.cmd`: recompila el código actual y genera un ZIP con EXE, licencias y suma SHA256 dentro de `artifacts/release/`. No se sube a Git automáticamente; adjuntalo desde **GitHub → Releases**, una vez verificada la compilación de CI.

## Estado de la versión 0.1.0

Esta es la **primera vista pública preliminar del código fuente**, no un producto estable. Todavía faltan pruebas de extremo a extremo para conexiones reales, validación de GUI, automatización de traducciones, editor de módulos y comportamiento multiplataforma del cliente. El objetivo inmediato es reproducir una compilación limpia con GitHub Actions y verificar el ejecutable correspondiente al mismo commit.

## Licencia

Copyright © 2026 **SolarisPKN contributors**.

Este proyecto se distribuye bajo **GNU General Public License, versión 3 únicamente (GPL-3.0-only)**. Consultá el texto completo de [LICENSE](LICENSE) y los [avisos de terceros](THIRD_PARTY_NOTICES.md). No se ofrece garantía. Las bibliotecas y dependencias de terceros conservan sus respectivas licencias.

---

Desarrollado dentro del ecosistema **SolarisPKN**. Las contribuciones y reportes de errores son bienvenidos, con reproducción de problemas y sin compartir credenciales ni datos privados.
