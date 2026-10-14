## Regresión de teclado CLI y diagrama flotante — octubre 2026

- Autenticar SSH y abrir **Comandos y bloques**. Escribir `uname -a` en el campo manual y presionar **Enter**: debe ejecutar por SSH, generar una entrada cronológica y mostrar el resultado. Nunca debe aplicar la sugerencia en lugar de ejecutar.
- Escribir `una` o un comando parcialmente escrito y pulsar **Tab**: debe completar la sugerencia seleccionada en la entrada, **sin ejecutar**. Probar flechas arriba/abajo para navegar, y Tab sin sugerencias (debe moverse el foco normalmente).
- Comprobar que el botón **Ejecutar** y el atajo configurado **Ctrl+Enter** siguen realizando una única ejecución, sin confirmaciones adicionales.
- Abrir **4 · Automatizaciones**. Solo debe verse el constructor y la vista de script, **no el diagrama**; abrirlo únicamente mediante el botón **Abrir diagrama ↗**.
- Abrir la ventana de diagrama, moverla a otro monitor, maximizar con el botón y con F11, arrastrar nodos y guardar. Volver a pulsar el botón desde AntiCLI: debe enfocar **la misma ventana**, nunca duplicarla.
- Cerrar la ventana del diagrama y volver a abrirla: nodos, flechas y edición en memoria deben mantenerse. El diagrama no debe quedar intercalado dentro de la pestaña original.
- Cerrar la sesión/pestaña de AntiCLI con la ventana flotante abierta: debe cerrarse también sin dejar ventana huérfana.
- Probar creación de pasos y diagrama desde la pestaña con la ventana abierta/cerrada; el mismo grafo debe reflejar los cambios, salvo las modificaciones manuales que ya estén marcadas como edición de grafo.
- Validar en Windows, tanto ventana normal como maximizada y monitores de distintas resoluciones. GitHub Actions solo prueba compilación y empaquetado; estos comportamientos necesitan prueba manual de UI.

## Regresión — lectura de archivos, disponibilidad y consola unificada

- En Linux de pruebas con `cat` disponible, buscar **«leer»**: deben aparecer las fichas `cat`, `head`, `tail`, `less`, `more`, `nano` y `vim` cuando correspondan; los ejecutables ausentes se muestran con ○ y riesgo rojo, los instalados con ✓. Comprobar también la situación inicial **?** antes de actualizar el inventario.
- Elegir **Detectar sistema** (`cat /etc/os-release`) y marcar `-n`: el comando previsto debe ser `cat -n -- '/etc/os-release'`; nunca `cat /etc/os-release --version`.
- Elegir **Configuración de montaje** (`cat /etc/fstab`) y **Estado RAID** (`cat /proc/mdstat`) y comprobar que la ruta preseleccionada corresponde al acceso rápido seleccionado (no siempre `/etc/os-release`).
- Importar `cat --help` desde una VM autorizada y comprobar que la ficha muestra únicamente opciones de ese ejecutable y no inserta `--version`/`--help` junto al archivo. Las plantillas genéricas sin ranura `{flags}` no deben aceptar opciones arbitrarias.
- Ejecutar `cat /etc/os-release`, después `uname -a`, después otro comando con stderr y otro con código de error. **Una sola consola visible** debe contener los comandos y salidas en orden, sin separar «última salida» e «historial» ni sobrescribir los anteriores.
- Desacoplar la consola y comprobar que la ventana flotante tiene **un único visor** de toda la sesión, sin segundo campo de comando, `sudo` o botón de ejecución. La barra manual en la ventana principal es el único punto de ingreso.
- Con ventana estrecha (mínimo soportado), comprobar el reparto aproximado **55% trabajo / 45% terminal**, botones de consola sin recorte, salida visible, scroll independiente y comando fijo inferior compacto. Comprobar también maximizada y con terminal a la izquierda.
- Confirmar que `nano`, `vim`, `less` y `more` avisan de que requieren PTY y que la versión actual todavía no implementa ese modo interactivo.
- Verificar búsqueda vacía, sin inventario, inventario remoto incompleto y reconexión a otro servidor; la disponibilidad debe recalcularse para la sesión actual.

## Pruebas responsive y módulos Avalonia — octubre 2026

- Abrir **Comandos y bloques** y verificar tres controles visibles: área de trabajo arriba izquierda (~75% de ancho), consola arriba derecha (~25% de ancho) y entrada manual debajo (~30% del alto).
- Reducir y aumentar el tamaño de la ventana (mínimo permitido y maximizada). **Ninguno de los tres debe desaparecer ni empujar a otros fuera de la pantalla**. El control inferior debe conservar siempre visible el `TextBox` de comando manual.
- Crear suficientes resultados de búsqueda para desbordar el área de trabajo y desplazarse **solo dentro de ella**. El panel terminal y el comando inferior deben permanecer inmóviles.
- Abrir un formulario guiado con muchas opciones; comprobar que su scroll es independiente y que al cerrarlo se recupera el catálogo sin saltos.
- Generar registros SSH largos, desplazarse en la terminal y comprobar que el catálogo y el campo manual no se mueven. La barra de controles de terminal debe envolver sus botones si no caben horizontalmente.
- Hacer clic en **Mover a izquierda**, verificar distribución **terminal 25% izquierda + trabajo 75% derecha**; restaurar derecha. Deben continuar operativas las búsquedas, acciones y el historial.
- Expandir, ocultar, restaurar y desacoplar consola. Desacoplarla en otro monitor, maximizar con el botón/F11, ejecutar comando desde esa ventana y confirmar sincronización con el historial original. Cerrar la ventana desacoplada y confirmar que vuelve a acoplarse sin borrar salida.
- En la franja inferior, mostrar sugerencias o mensajes largos y desplazar **solo sus controles auxiliares**; el campo de comando manual debe permanecer visible.
- Repetir con dos sesiones SSH abiertas para confirmar que los historiales de cada pestaña siguen separados y cerrar una sesión no cierra la consola de otra.
- Validar a 1366×768, 1920×1080 y en ventanas reducidas. Las pruebas visuales son manuales: una compilación CI verde no sustituye revisar el scroll y los tamaños reales.

## Interfaz y flujos administrativos — octubre 2026 (pendientes de prueba manual)

- **Consola:** después de SSH, comprobá que el panel derecho ocupa cerca del 25% del ancho y que la barra inferior de comandos sigue visible aunque desplaces el catálogo. Expandir debe mostrar la consola a ancho completo sin tapar el campo manual; restaurar vuelve a 75/25 y ocultar deja visible el catálogo y el botón mostrar. Cada pestaña mantiene su propio historial; copiar/exportar deben conservar las redacciones del operador.
- **Formulario sobre catálogo:** buscar `uname` o `adduser` y abrir su ficha. Debe superponerse al área donde se seleccionó, sin agregar altura al final de la lista; al cerrarla debe mostrarse de nuevo el catálogo. No cambia el ejecutable SSH ni las flags por cambiar la geometría.
- **Editor de módulos:** crear un `local.prueba`, editar título, flags y operandos por filas visuales; borrar una fila, regenerar JSON, validar y guardar. Confirmar fichero en `%APPDATA%\\SolarisPKN\\AntiCLI\\modules` y aparición inmediata en la búsqueda. Probar errores: ID sin `local.`, JSON malformado, tipo de parámetro desconocido, bandera duplicada. Clonar una definición integrada no debe sobrescribir archivos integrados.
- **Herramienta ausente:** conectarse a un servidor Debian/Ubuntu de laboratorio con un ejecutable mapeado que no aparezca en PATH. El programa debe mostrar un aviso, explicar que eso **no prueba desinstalación** y ofrecer el paquete mapeado. Sin pulsar instalar, no modificar el servidor. Si el mapa no conoce el ejecutable, no sugerir un paquete inventado. En Windows, otras distros o entornos Yocto no mapeados, no mostrar instalador APT engañoso.
- **Instalación explícita:** con autorización y una VM desechable, pulsar instalar. Verificar que se ejecuta `apt-get update` seguido de `apt-get install -y` para el paquete conocido; con sudo habilitado debe usar `sudo -n`, sin sudo necesita privilegios propios. Revisar código de salida, errores y registro del historial; actualizar inventario PATH antes de ofrecer la acción otra vez.
- **Preset Yocto:** buscar «Preparar dependencias ... Yocto», comprobar que muestra 20 paquetes y admite editar el parámetro; simular primero con la flag `-s`. En Ubuntu moderno probar deliberadamente la falta de `libsdl1.2-dev` para mostrar el error de repositorio, sin afirmar que el paquete se instaló.
- **Regresión SSH:** autenticación y pin de host-key, manual `uname -a`, comando libre, sudo no interactivo, historial por pestaña y cierre de conexión continúan operativos. Comprobar que la GUI no se bloquea visualmente mientras ejecuta una orden de hasta 20 minutos.

**Nota:** una compilación de GitHub Actions que termina en verde solo verifica compilación/empaquetado. Estas pruebas contra servidores reales, resolución y disposición visual siguen requiriendo validación humana antes de un lanzamiento estable.

# Plan de verificación del rediseño

**Estado:** pendientes de ejecución en Windows. Este documento no certifica que compile ni que SSH funcione.

## Compilación

```powershell
dotnet build ./src/SolarisPKN.AntiCli/SolarisPKN.AntiCli.csproj
dotnet run --project ./src/SolarisPKN.AntiCli/SolarisPKN.AntiCli.csproj
```

## Pestañas, clones y ventanas

- Al iniciar: una pestaña nueva con formulario de conexión.
- Pulsar **Nueva sesión**: aparece otra pestaña vacía, sin host ni usuario heredados.
- Autenticar servidor A en una pestaña y servidor B en otra: cada pestaña debe mostrar sus propios comandos, resultados, bloques y estados, y trabajar sin cerrar a la otra.
- **Clonar sesión** en A: debe crear nueva pestaña autenticada del mismo host, con credenciales SSH copiadas en memoria, aunque se haya optado por NO recordarlas en DPAPI.
- Sin sesión autenticada o con comando en progreso: el clon debe rechazarse.
- **Clonar en ventana**: abre otra ventana con las credenciales en memoria independientes; cerrar original no debe bloquear el clon.
- **Nueva ventana**: debe mostrar una única pestaña vacía; cerrar esta ventana no debe cerrar otras existentes.
- Cerrar una pestaña sin trabajo activo: elimina solo esa pestaña y limpia sus bytes de clave privada. Si era la última, aparece otra vacía.
- Intentar cerrar una pestaña con operación activa: se rechaza hasta finalizar.
- **Ctrl+T**, **Ctrl+Mayús+T**, **Ctrl+Mayús+N** y **Ctrl+W** actúan sobre la ventana/pestaña actual.
- Verificar que clonar no escriba valores nuevos en `profiles.json`, y que los cuatro checkboxes DPAPI permanezcan independientes.
- Confirmar que el clon NO comparte un directorio remoto activo ni una sesión interactiva persistente; cada consulta abre y verifica SSH.
- Si una host key cambia después de clonar, los comandos del clon deben fallar de forma segura.

## Preferencias generales y ruedita de ajustes

- Botón **⚙ Ajustes** visible desde la ventana principal incluso sin autenticación SSH.
- Probar tema **Sistema / Oscuro / Claro** en varias ventanas abiertas sin reiniciar.
- Cambiar acento a las cinco variantes de paleta y un `#RRGGBB` personalizado.
- Definir fondo de ventana válido según tema; rechazar colores sin contraste o fondo personalizado con tema Sistema.
- Dejar fondo en blanco: restaurar estilo Fluent del tema activo en cada ventana.
- Modificar cada uno de los seis atajos, guardar y comprobar que funcionan desde distintas ventanas.
- Dos acciones con el mismo atajo o sintaxis inválida: no debe guardar.
- Los atajos con únicamente letras o sin Ctrl/Alt deben rechazarse.
- El atajo configurable **Ejecutar comando** debe reemplazar Ctrl+Enter en la consola asistida, sin saltarse el catálogo permitido.
- Cancelar cambios: no persistir. Restaurar predeterminados + Guardar: recuperar configuración inicial.
- Reiniciar app: todas las preferencias persisten desde `%APPDATA%\\SolarisPKN\\AntiCLI\\settings.json`.
- Verificar que `settings.json` no contiene credenciales SSH ni rutas de claves.

## Primera conexión SSH, TOFU y rotación de claves (pendiente)

- VPS nuevo, sin huella: pulsar «Autenticar» → detectar clave sin enviar credenciales; mostrar host:puerto, algoritmo y SHA256 y exigir una decisión explícita.
- Elegir Cancelar o cerrar ventana → no persistir pin, no autenticar y no habilitar comandos.
- Aceptar sin verificar por otro canal → permitir solo después de marcar conocimiento del riesgo TOFU; guardar el pin por host:puerto y pedir credenciales después.
- Comparar la huella con la consola del proveedor; al guardarla no persistir contraseñas ni claves privadas en `known-hosts.json`.
- Reconectar con clave idéntica → no volver a preguntar, autenticar normal.
- Modificar la clave presentada por un VPS de prueba → bloquear autenticación, con huella nueva en error, y no enviar credenciales.
- Forzar «Detectar / revisar clave» sobre un servidor modificado → advertir de manera destacada, mostrar huellas vieja y nueva, cancelar sin cambios o reemplazar solo con confirmación de verificación independiente.
- Cambiar puerto o host → no reutilizar la clave del endpoint anterior; deben existir pines distintos.
- Clonar sesión y luego rotar el pin del mismo endpoint → el clon desactualizado debe bloquear comandos hasta volver a autenticar.
- Probar credenciales erróneas tras confiar en clave: SSH falla sin invalidar el pin del host.
- Probar huella manual que difiere del servidor y perfiles viejos: jamás aprobar automáticamente.

## Interfaz y flujo de autenticación

- Abrir: la pestaña **Entorno y SSH** aparece y exige elegir un sistema operativo.
- Cambiar Debian, Ubuntu, Windows y otro Linux; no aparecen comandos hasta un SSH realmente autenticado.
- Crear perfil y guardar sin recordar datos: al cerrar y reabrir, no se guardan usuario, ruta/contenido de clave, contraseña ni passphrase.
- Marcar y desmarcar cada uno de los cuatro checkboxes DPAPI de forma independiente; verificar `profiles.json` y reinicio.
- Autenticar con clave privada y con contraseña (en servidores de pruebas); probar passphrase cifrada y no cifrada.
- Huella SSH incorrecta → rechazo; plataforma elegida incorrecta → no habilita comandos.
- Cambiar un dato de conexión después de entrar → invalidación de sesión.
- Desconectar → pestañas de comandos/módulos bloqueadas y secreto de clave en memoria limpiado.
- Perfil viejo de prototipo anterior → reconfigurar credenciales y comprobar que no se conservan campos heredados al guardar.

## Comandos visuales

- Después de autenticarse, aparecen botones con acciones de plataforma; elegir opción genera botones por token del comando.
- Armar un comando con clics, deshacer, borrar y ejecutar si coincide exactamente con la lista de consultas.
- Autocompletado por texto, Tab, Enter y flechas ↑↓; bloquea acciones no revisadas.
- Windows: mostrar acciones Windows, no Linux. Debian/Ubuntu: consultar comandos Linux, no `ipconfig`.

## Módulos

- Se encuentra únicamente en pestaña **Crear módulos**.
- Sin autorización de pruebas o sin login: no escanear.
- Con Salt `--help`, registrar máximo 14 consultas en pruebas y guardar JSON `draft-unreviewed`; no ejecutar acciones reales ni habilitar comandos recién descubiertos.
- Los logs permanecen fuera de Git y sin envío a IA externa.

## Invariantes de seguridad

- No aparecen claves privadas, contraseñas ni passphrases en perfiles JSON como texto plano.
- Si no se marcó recordar, su valor no debe persistir.
- No se habilitan módulos remotos si falló SSH o el sistema no coincide.
- Nada de `sudo`, shell arbitraria ni ejecuciones mutantes generadas por IA.
