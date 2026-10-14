## Diseño modular adaptable y scroll independiente

La pantalla **2 · Comandos y bloques** ahora está compuesta por **tres controles Avalonia independientes**, con código y vista propios:

- `CommandWorkspaceView.axaml` — zona de trabajo superior para búsqueda universal, módulos, flags, parámetros y formulario superpuesto. El catálogo tiene su propio scroll, y el formulario guiado también puede desplazarse sin mover la consola.
- `SshTerminalPane.axaml` — terminal SSH con salida, comandos anteriores, historial editable y botones para **desacoplar**, **maximizar el área** o **mover el panel a izquierda/derecha**. Tiene scroll propio para logs largos.
- `CommandEntryPane.axaml` — franja inferior **siempre visible** con campo manual y controles de ejecutar/\`sudo\`. Los controles accesorios tienen su propio scroll; la entrada de texto se queda fija arriba.

El contenedor `SessionView.axaml` distribuye proporcionalmente la pantalla: **70% de altura para la zona superior**, **30% para los comandos inferiores**; en vista normal, la zona superior se reparte **75% trabajo / 25% terminal**, o viceversa cuando el operador mueve la terminal a la izquierda. Los tamaños se recalculan con la ventana; cada módulo contiene su scroll en lugar de extender la altura de toda la pantalla.

Al ocultar, expandir o desacoplar la terminal se redimensiona solo la región superior. El módulo inferior no desaparece al variar la resolución. Si se desacopla en un segundo monitor, la nueva ventana sigue consultando la misma sesión SSH y los mismos registros, sin copiar credenciales.

**Nota:** el reparto de tamaño y los scrolls tienen compilación automatizada, pero necesitan validación visual en Windows a distintas resoluciones (especialmente 1366×768, 1920×1080 y ventanas estrechas).

## Consola SSH desacoplable (dos monitores)

Dentro de la pestaña de comandos, la consola aparece a la derecha (aprox. 25% de ancho). Pulsá **Desacoplar ↗** para abrir una **ventana de consola independiente**. Arrastrala al segundo monitor y pulsá **Maximizar** (también funciona `F11`). De este modo, el catálogo, los formularios y la barra fija de comandos manuales siguen visibles en la ventana principal; la salida y el historial ocupan la otra pantalla.

La consola desacoplada incluye un campo para ejecutar comandos en **la misma sesión SSH**, con la indicación de riesgo 🟢🟡🔴 y la casilla opcional `sudo -n`. No abre otra sesión ni crea una segunda copia de las credenciales. Las órdenes ejecutadas desde ambas ventanas comparten el mismo historial; si una operación está activa, no se inicia una segunda en paralelo. La salida actual se refleja al inicio y al final de la ejecución: **todavía no existe streaming de stdout/stderr a medida que llega**.

Con **Acoplar al sistema ↙**, o simplemente cerrando la ventana desacoplada, la consola vuelve a la posición derecha. Al cerrar la pestaña original o salir de AntiCLI se cierra también su consola independiente, para evitar ventanas huérfanas. El historial sigue siendo propio de esa pestaña y no se comparte entre sesiones distintas.

# Comandos visuales, consola asistida y módulos

## Secuencia de pestañas

**1. Entorno y SSH:** elegir Debian/Ubuntu/Windows/otro Linux y configurar autenticación. Las cuatro casillas DPAPI (usuario, clave, contraseña y passphrase) son independientes y optativas.

**2. Comandos y formularios:** bloqueada hasta comprobar SSH, host key y sistema operativo. El **buscador universal por intención** encuentra tareas como «crear usuarios», «actualizar paquetes» o «ver kernel»; al elegir una, el **mismo renderer genérico** interpreta su ficha JSON y muestra descripción, casillas para flags, campos de operandos y cambios de orden ↑/↓. La vista previa genera un comando editable. Un módulo con soporte de lote puede crear nombres correlativos como `alumno01` a `alumno20`, interrumpiéndose ante un error. **No se crean vistas distintas por comando.** Las 153 acciones base utilizan el mismo renderer, mientras que las fichas enriquecidas ofrecen controles más específicos. Se pueden escribir comandos manuales y usar sugerencias con Tab y flechas. Los indicadores son 🟢 consulta, 🟡 precaución, 🔴 alto riesgo/desconocido; **Ejecutar actúa con un solo clic, sin confirmación adicional**.

**3. Crear módulos:** separada de la operación diaria. Requiere SSH válido y autorización explícita sobre una máquina de pruebas; genera logs de ayuda y borradores JSON `draft-unreviewed`. Es posible reutilizar esos comandos y ejecutarlos, pero siguen sin validación de seguridad.

## Descubrimiento de Salt y otras herramientas

Salt emplea diferentes ejecutables: `salt`, `salt-call`, `salt-run`, `salt-key`, `salt-cp`, `salt-ssh`. Se puede explorar cada uno por `--help`. No ejecutar combinaciones arbitrarias ni asumir que `salt create` existe. El descubrimiento de funciones Salt remotas y sus minions necesita un contrato adicional con permisos, no se hace en esta fase.

El catálogo inicial incluye [17 módulos y 146 comandos documentados](COMMAND_MODULES.es.md). **Fichas JSON:** `src/SolarisPKN.AntiCli/Data/guided-commands.json` (definiciones detalladas) y `src/SolarisPKN.AntiCli/Data/admin-commands.json` (catálogo base). La aplicación permite incorporar ficheros `*.json` del usuario bajo `%APPDATA%\SolarisPKN\AntiCLI\modules\` para ampliar los formularios sin recompilar; no sustituyen automáticamente las definiciones integradas. Cada esquema describe: nombre, búsqueda, flags, tipo de valor, operandos, plantilla, riesgo, compatibilidad, lote y timeout. Para una explicación del contrato, ver [FORMULARIOS_DINAMICOS.md](FORMULARIOS_DINAMICOS.md). Véase también [SKILL.md](../SKILL.md).

## Diccionario por servidor y corrección inteligente (Tab)

Al autenticar por SSH, AntiCLI toma los nombres de ejecutables del catálogo JSON y de las fichas guiadas y comprueba su presencia en el `PATH` de **esa cuenta remota**. Linux utiliza `command -v` (sin ejecutar las aplicaciones); Windows utiliza `Get-Command` mediante PowerShell. La consulta se ejecuta como el usuario conectado, **sin sudo y sin instalar paquetes**. Se guarda solo en memoria, de manera independiente por pestaña. Podés repetirla con **Actualizar herramientas**.

- **✓** Comando detectado en el `PATH` de esa sesión.
- **○** No detectado en el `PATH` actual: quizá no esté instalado, falte iniciar el entorno de Yocto (`oe-init-build-env`), o esté disponible con otro usuario/shell.
- **?** Detección no realizada o estado desconocido.

El buscador del catálogo y el de autocompletado pueden usar estas señales para priorizar opciones disponibles. Los nombres de comandos y flags vienen de JSON, complementados por ayudas `--help` previamente importadas desde ese servidor. El motor usa búsqueda por prefijos y distancia Damerau-Levenshtein acotada para **sugerir errores tipográficos**, por ejemplo `umame` → `uname` o `aof-ghet` → `apt-get`. **Tab o un clic** aplican la sugerencia elegida, sin ejecutar el comando. Los argumentos existentes no se descartan al corregir el ejecutable; las flags documentadas también se completan. No hay corrección silenciosa al pulsar Ejecutar.

**Limitaciones:** esto es una detección acotada de ejecutables conocidos por el catálogo, **no** un inventario de todos los paquetes ni todas las funciones/shell aliases del servidor. Un ejecutable que no aparece no queda prohibido. La existencia de la herramienta no certifica permisos, compatibilidad de flags ni ausencia de riesgos. La corrección no sustituye la comprobación del riesgo ni la revisión de la línea antes de ejecutarla.

## Consola directa e historial de operaciones

El mismo panel permite escribir cualquier comando SSH directamente. **Copiar comando**, **Pegar comando** y **Copiar salida** usan el portapapeles de Windows, sin necesidad de abrir un navegador. El botón de ejecución respeta la clasificación visual de riesgo y la casilla sudo.

Cada intento de ejecución (éxito o error) se agrega al historial de la pestaña con fecha UTC, host/puerto, sudo activo, comando enviado, código de salida, STDOUT y STDERR. El registro se puede consultar, reutilizar, copiar, **editar antes de compartir** y exportar a `.txt`. El contenido es local a la sesión de la aplicación; la exportación necesita una elección explícita de destino. **No se sube ningún log a GitHub automáticamente.**

**Privacidad:** los argumentos, variables de entorno, logs, cabeceras, tokens o salidas pueden contener secretos. Antes de copiar/exportar, revisá y redactá el texto en la caja editable del historial. Los últimos cambios de la caja son el contenido que se copia/exporta. Limpiar historial elimina las entradas en memoria de esa pestaña, sin modificar archivos exportados.

## Seguridad

Los logs de exploración se guardan fuera del repositorio en `%APPDATA%\SolarisPKN\AntiCLI\discovery`. Pueden contener detalles del entorno remoto y no se comparten automáticamente. Los comandos detectados se presentan como sugerencias; cuando el administrador los elige, se pueden ejecutar por SSH como cualquier comando manual. La clasificación de comandos desconocidos es 🔴 y no existe una segunda confirmación. El botón sudo agrega `sudo -n -- sh -c '…'` a la ejecución Linux, sin introducir contraseñas ni pedir UAC en la PC Windows.
