# AntiCLI — Motor universal de módulos guiados

AntiCLI **no programa una pantalla por comando**. Un mismo formulario Avalonia se genera leyendo las fichas JSON que describen comandos, opciones, operandos, validaciones y plantillas. El administrador también puede escribir y ejecutar comandos libres.

## Arquitectura

1. `src/SolarisPKN.AntiCli/Data/admin-commands.json`: **146 acciones Linux + 7 Windows** con clasificación de riesgo, descripción e invocación inicial.
2. `src/SolarisPKN.AntiCli/Data/guided-commands.json`: fichas detalladas para las acciones complejas; incluye `uname`, `adduser`, operaciones APT, repositorios firmados, Yocto/BitBake, devtool y capas.
3. `GuidedCommandCatalog.cs`: carga, valida y combina ambos archivos. Si una acción no tiene ficha detallada, se genera un formulario genérico y editable para ella.
4. `SessionView.Guided.cs`: **un solo renderer** para todas las fichas, independiente del nombre del programa. Genera casillas, descripciones, campos de valores, flechas para ordenar flags, numeración y lotes.
5. `%APPDATA%\SolarisPKN\AntiCLI\modules\*.json`: fichas adicionales propiedad del usuario. Se leen al abrir AntiCLI, sin recompilar. No reemplazan las fichas incluidas y aparecen 🔴 hasta su revisión.
6. `%APPDATA%\SolarisPKN\AntiCLI\command-help\*.json`: opciones importadas automáticamente con el botón «Cargar flags desde --help del servidor». La caché se separa por host, puerto y ejecutable. Las descripciones no traducidas se identifican como importadas.

Una nueva herramienta necesita **datos**, no una nueva clase de ventana. Las opciones importadas por `--help` son un punto de partida, **no documentación completa validada**: algunos programas no anuncian todas sus flags y otros usan sintaxis inusual.

## Cómo se ve el flujo

- Buscador universal: «crear usuarios», «ver kernel», «instalar paquetes», «Yocto receta». Elegís el resultado.
- Panel guiado: título y descripción en español, flags con checkbox y explicación, flechas ↑ ↓ para reordenarlas, campos con sus nombres y ejemplos.
- Comando generado visible y editable. Si lo cambiás libremente, su clasificación de riesgo pasa a 🔴.
- Botón **Ejecutar** con el color 🟢 🟡 🔴 del riesgo. **Un clic, sin segunda confirmación**.
- Casilla `sudo` optativa: envuelve la expresión completa con `sudo -n -- sh -c`; no pide UAC local ni intenta pasar la contraseña SSH a sudo.

> Los colores advierten; no son un límite de permisos ni un analizador de seguridad. Una acción roja puede provocar pérdidas de datos. Usá máquinas autorizadas.

## Ejemplo práctico — uname

Abrí «Información del sistema». Marcá «Kernel», «Arquitectura de máquina» y «Sistema operativo». Reordená las flags y consultá la vista previa. GNU uname puede imprimir los valores en orden fijo, distinto del orden de las flags.

## Ejemplo práctico — 20 alumnos

Abrí «Crear usuario o lote de usuarios», completá `alumno`, activá lote, cantidad `20`, inicio `1`, dígitos `2`. Elegí «Sin contraseña inicial» y las opciones de grupo/shell necesarias. El generador produce un único bucle remoto que crea `alumno01` a `alumno20`; se detiene al primer fallo y conserva su código de error. No genera contraseñas ni claves.

## APT y repositorios externos

Fichas: instalar, quitar, purgar, buscar, actualizar índices, upgrade, dist-upgrade y autoremove. Paquetes y flags son campos editables. Es posible importar más opciones desde el `--help` real del servidor.

«Agregar repositorio externo con clave verificada» solicita:
- Identificador de repositorio.
- URL HTTPS del repositorio.
- Suite y componentes (por ejemplo `noble` y `main`).
- URL HTTPS de la **clave GPG pública** y la **huella completa esperada** obtenida del proveedor.

El flujo descarga la clave en un directorio temporal, compara la huella obtenida mediante GPG con la esperada, la convierte a keyring y escribe un archivo `.sources` estilo deb822 en `/etc/apt/sources.list.d/`. Usa `Signed-By` limitado a `/etc/apt/keyrings/<identificador>.gpg`, y después `apt-get update`. **No usa `apt-key` global ni desactiva la comprobación de firmas.** Requiere `curl`, `gpg`, permisos de administrador remoto y un proveedor confiable. No hay descarga automática si faltan datos; ninguna URL de ejemplo es una credencial real.

## Yocto / BitBake

Fichas iniciales: compilar receta/imagen; consultar versiones; listar capas; listar recetas; añadir capa; limpiar resultados; inspeccionar entorno; estado y modificación de `devtool`. Elegí el directorio Poky y el directorio de build; el comando resultante **inicializa el entorno** con `oe-init-build-env` antes de ejecutar BitBake.

Los comandos, versiones y tareas admitidos dependen de la versión de Yocto, las capas y las recetas de **ese** proyecto. Para esto está el importador `--help` por host: no se intenta codificar cada receta o tarea de Yocto en Avalonia. Las compilaciones tienen timeout extendido, pero AntiCLI todavía no ofrece una terminal interactiva persistente ni streaming de logs de build.

Referencias oficiales: [Yocto Documentation](https://docs.yoctoproject.org/), [BitBake User Manual](https://docs.yoctoproject.org/bitbake/), [Layer Management](https://docs.yoctoproject.org/dev/dev-manual/layers.html), [APT sources.list(5)](https://manpages.ubuntu.com/manpages/noble/en/man5/sources.list.5.html), [APT apt-get(8)](https://manpages.ubuntu.com/manpages/noble/en/man8/apt-get.8.html).

## Agregar un módulo sin recompilar

Creá `%APPDATA%\SolarisPKN\AntiCLI\modules\ejemplo.json`:

```json
{
  "SchemaVersion": 1,
  "Commands": [
    {
      "Id": "custom.espacio",
      "Executable": "du",
      "TitleEs": "Consultar tamaño de carpeta",
      "ExplanationEs": "Muestra el espacio usado por la ruta indicada.",
      "SearchTerms": "espacio directorio tamaño du",
      "Risk": "Green",
      "Template": "du {flags} {path}",
      "Flags": [
        {
          "Token": "-h",
          "LabelEs": "Tamaño legible",
          "DescriptionEs": "Usa KB, MB y GB",
          "ValueKind": "None",
          "SelectedByDefault": true
        }
      ],
      "Operands": [
        {
          "Name": "path",
          "LabelEs": "Directorio",
          "DescriptionEs": "Ruta de la carpeta a inspeccionar",
          "ValueKind": "AbsolutePath",
          "Placeholder": "/var/log",
          "Required": true
        }
      ],
      "Platforms": ["Linux"]
    }
  ]
}
```

Reiniciá AntiCLI. El buscador y el formulario reconocerán la ficha automáticamente. Los campos se escapan para shell con comillas simples; **las plantillas son código de shell**, así que no importes archivos JSON de terceros sin auditarlos. Las fichas externas se marcan rojas aunque declaren otro nivel.

## Pendientes de prueba real

- Comparar flags importadas con `uname --help`, `apt-get --help` y `bitbake --help` en los destinos autorizados.
- Instalar varios paquetes APT y agregar un repositorio *de laboratorio* con huella GPG conocida.
- Ejecutar el lote de usuarios en una VM descartable; comprobar el corte en caso de error y los permisos de `sudo -n`.
- Verificar el flujo Yocto con un checkout Poky configurado. Las pruebas de CI comprueban compilación, no comportamiento contra un servidor real.
