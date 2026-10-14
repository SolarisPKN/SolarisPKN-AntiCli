# Yocto / BitBake — paquetes host para Debian y Ubuntu

**Origen:** comando APT aportado por el usuario para preparar un entorno de trabajo Yocto. La lista se incorporó al **motor universal de fichas JSON**, no a una nueva vista específica. Ficha: `yocto-host-dependencies`.

## Preset editable (20 paquetes)

```bash
sudo apt-get install gawk wget git-core diffstat unzip texinfo gcc-multilib \
  build-essential chrpath socat cpio python-is-python3 python3 python3-pip \
  python3-pexpect xz-utils debianutils iputils-ping libsdl1.2-dev xterm
```

En AntiCLI: buscá **«Preparar dependencias Ubuntu/Debian para Yocto»**, revisá la lista completa, editá los paquetes si es necesario, activá `sudo` (si la cuenta remota lo necesita) y ejecutá. El preset de la interfaz utiliza `apt-get install -y` para el flujo SSH no interactivo. También ofrece `-s` para simular. No instala nada por sí solo.

**Compatibilidad:** el paquete `libsdl1.2-dev` está ausente de algunos repositorios Ubuntu/Debian modernos; `git-core` puede ser un paquete transicional o tener otro nombre según distribución. Un `apt-get install` con un paquete no disponible puede fallar y dejar sin instalar parte o toda la lista. Comprobá `apt-cache policy` y consultá [requisitos actuales de Yocto](https://docs.yoctoproject.org/ref-manual/system-requirements.html) para la versión concreta que vas a compilar.

El formulario de cualquier módulo con lista de paquetes incluye ahora **«Comprobar paquetes instalados»**: consulta `dpkg-query` en el servidor, muestra cuáles están instalados y cuáles faltan, y permite **instalar únicamente los faltantes** con un botón. No se ejecuta instalación durante la comprobación. Revisá el resultado, ya que puede haber paquetes no disponibles en los repositorios configurados.

La interfaz de **herramienta ausente** solo ofrece instalación cuando conoce un paquete mapeado de forma explícita, comprueba que el ejecutable no aparece en el `PATH` y el servidor es Debian/Ubuntu con APT. La ausencia en `PATH` no demuestra necesariamente que no esté instalado: algunos comandos exigen activar un entorno. Al pulsar instalar, AntiCLI actualiza los índices APT e instala el candidato disponible en los repositorios configurados; no garantiza la última versión de upstream. Una instalación modifica el servidor y puede requerir `sudo -n`.

## Documentación y mantenimiento

La lista de paquetes se guarda en `src/SolarisPKN.AntiCli/Data/guided-commands.json`. Los nombres de ejecutables y paquetes APT que AntiCLI puede sugerir para instalación se encuentran en `src/SolarisPKN.AntiCli/Data/install-providers.json`. Ambas fuentes son JSON independientes del código de la interfaz.

Para ampliar el catálogo, ver [formularios dinámicos](FORMULARIOS_DINAMICOS.md). Las instalaciones remotas deben probarse en un sistema autorizado y revisarse con sus permisos correspondientes.
