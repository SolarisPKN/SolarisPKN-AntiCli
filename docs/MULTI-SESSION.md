# AntiCLI — pestañas, clones y ventanas independientes

> Estado: implementación en código, pendiente de compilación y pruebas de interfaz en Windows.

## Controles superiores

| Acción | Resultado |
| --- | --- |
| ＋ Nueva sesión | Crea otra pestaña con Entorno y SSH en blanco para conectarse a otro host. |
| ⧉ Clonar sesión | Crea otra pestaña con el mismo perfil y una copia de las credenciales ya autenticadas en RAM. |
| ↗ Nueva ventana | Abre otra ventana independiente con una pestaña de conexión nueva. |
| ↗ Clonar en ventana | Abre otra ventana con un clon del perfil autenticado y sus credenciales copiadas a RAM. |
| × de pestaña | Cierra solamente esa pestaña, sin interrumpir a otras. Impide cerrar si el comando está ocupado. |

Atajos: **Ctrl+T** nueva pestaña; **Ctrl+Mayús+T** clonar sesión; **Ctrl+Mayús+N** nueva ventana; **Ctrl+W** cerrar pestaña actual.

Cada pestaña contiene **sus tres subpestañas independientes**: Entorno y SSH, Comandos y bloques, Crear módulos.

## Semántica de sesión

- El clon no duplica una conexión TCP, TTY, shell interactiva, directorio de trabajo o procesos activos. Copia **el perfil SSH verificado** y credenciales de autenticación en memoria para realizar consultas independientes.
- Como en el prototipo original, cada comando establece una conexión SSH nueva, revalida la huella SHA256 del servidor y se ejecuta en un proceso remoto independiente.
- Cada pestaña conserva sus campos de conexión, selección del catálogo, comando en construcción, respuestas de stdout/stderr y progreso de exploración.
- Las operaciones concurrentes de diferentes pestañas tienen sus banderas `_busy` separadas. No comparten controles ni la misma instancia de credenciales.
- La clave SSH se copia en un arreglo de bytes nuevo por clon. Cerrar una pestaña limpia su propio arreglo en memoria, sin invalidar las otras. Nunca se guardan credenciales por el hecho de clonar.
- Contraseñas y passphrases residen temporalmente en cadenas .NET: no se garantiza su borrado criptográfico. Los checkboxes DPAPI siguen siendo opt-in y se aplican **solo** al guardar un perfil explícitamente.
- No clonar sesiones sin autenticación exitosa ni mientras la sesión origen ejecuta una operación.
- Cuando una pestaña se está usando para una consulta, no se permite cerrarla desde el botón `×`. Al cerrar una ventana, la limpieza de claves de sesiones ocupadas queda diferida hasta que finalicen sus operaciones.
- Las ventanas comparten únicamente el proceso y los archivos de perfiles locales, no el estado visual ni las credenciales en memoria de cada pestaña.
- Avalonia se configura para cerrar la aplicación cuando se cierre la **última ventana**, no la primera.

## Próximos cambios posibles

- Sesiones SSH verdaderamente persistentes (cliente conectado y, opcionalmente, PTY) para mantener contexto de shell y comandos con estado.
- Mover pestañas entre ventanas mediante drag & drop.
- Reconexión automática cuando se corta la red, limitada por política y con confirmación.
- Indicadores de progreso y actividad en encabezados de pestañas.

## Verificación

Ejecutar `COMPILAR_ANTICLI.cmd` en Windows, iniciar el EXE publicado y verificar clonación, sesiones simultáneas, rechazo de clon no autenticado, cierre por pestaña, cierre de una de varias ventanas y memoria de credenciales. Aún no hay certificado de build ni prueba funcional.
