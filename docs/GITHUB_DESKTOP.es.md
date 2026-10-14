# Publicar SolarisPKN-AntiCli con GitHub Desktop

Esta carpeta **ya es un repositorio Git** y usa la rama local **main**. No hace falta instalar GitHub CLI (`gh`), ejecutar comandos de Git ni crear otra carpeta.

## Publicar el código de la primera versión

1. Abrí **GitHub Desktop** e iniciá sesión en tu cuenta.
2. Elegí **File → Add local repository → Choose…** (Archivo → Agregar repositorio local → Elegir). Seleccioná la carpeta existente:
   `...\SolarisPKN-AntiCli`
   Luego pulsá **Add repository**. No crees otro proyecto adentro ni lo clones.
3. En **Changes/Cambios**, revisá los archivos. Deben figurar `LICENSE`, `README.md` (**inglés**), `README.es.md` (**español**), `src/`, `docs/`, `third_party/` y `.github/workflows/ci.yml`.
4. **No publiques** `artifacts/`, `bin/`, `obj/`, `.env`, claves SSH, secretos, perfiles, configuraciones locales, registros, `.pdb` ni respaldos personales. Esos archivos están excluidos en `.gitignore`; si algún archivo privado aparece en la lista de cambios, detené la publicación y desmarcalo.
5. En **Summary (required)**, poné `feat: first GPL-3.0 AntiCLI preview v0.1.0` y pulsá **Commit to main**.
6. Pulsá **Publish repository / Publicar repositorio**. Indicá nombre **SolarisPKN-AntiCli** y propietario **SolarisPKN**. Para que sea público **desmarcá «Keep this code private / Mantener este código privado»**. Confirmá **Publish repository**.
7. Abrí el repositorio en GitHub y comprobá `README.md`, el enlace a español y **Actions → Windows build (preview)**. Si falla la compilación de CI, corregila antes de ofrecer un ejecutable.

**Importante:** GitHub Desktop publica repositorios y cambios, pero el botón **Publish repository** no crea una GitHub Release descargable con adjuntos binarios.

## ZIP ejecutable opcional

Podés publicar **solo el código fuente** primero, sin instalar nada extra. Para preparar después una descarga Windows, hacé doble clic en `PREPARAR_RELEASE.cmd`. El proceso recompila el código actual y genera:

- `artifacts/release/SolarisPKN-AntiCli-v0.1.0-win-x64.zip`
- `artifacts/release/SHA256SUMS.txt`

Estos archivos se mantienen **fuera de Git**. El ZIP incluye el EXE, GPLv3, documentación en español/inglés, notas de lanzamiento y avisos legales de terceros.

Cuando el repositorio esté publicado y el workflow compile correctamente, entrá a **GitHub desde el navegador → Releases → Draft a new release**. Elegí o creá la etiqueta `v0.1.0`, marcá **Pre-release**, adjuntá el ZIP y `SHA256SUMS.txt`, copiá las notas de `RELEASE_NOTES.es.md` (o inglés) y publicá.

## Problemas comunes

- **Ya existe un repositorio Git:** usá **Add local repository**, no **Create new repository**.
- **No aparece «Publish repository»:** revisá que estés en el proyecto AntiCLI, que hayas hecho el commit y que no tengas un remoto `origin` configurado. La carpeta inicial no tenía remoto.
- **No aparece SolarisPKN como propietario:** verificá la cuenta de GitHub Desktop. Si es una cuenta personal, seleccioná esa cuenta; si es una organización, elegila en el menú.
- **Ya hay commits locales anteriores:** inspeccioná History antes de publicar. El `.gitignore` no borra secretos de commits anteriores.
- **Falla la compilación:** se puede publicar el código como preliminar, pero no publiques un EXE hasta validar el commit correspondiente.
- **Convención de idiomas:** `README.md` en inglés y `README.es.md` en español.

Guías oficiales: [Agregar repositorio local](https://docs.github.com/es/desktop/adding-and-cloning-repositories/adding-a-repository-from-your-local-computer-to-github-desktop?platform=windows) · [Publicar proyecto existente](https://docs.github.com/es/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop).
