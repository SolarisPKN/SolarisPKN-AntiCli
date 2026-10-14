# Apariencia y atajos — Solaris AntiCLI

La rueda **⚙ Ajustes** está en la esquina superior derecha de cada ventana. Se puede abrir antes de autenticarse por SSH.

## Tema y colores

- Tema **Sistema**, **Oscuro** o **Claro** mediante la propiedad nativa de Avalonia `Application.RequestedThemeVariant`.
- Color de acento en una paleta (violeta, azul, verde, rosa y naranja) o un color propio `#RRGGBB`.
- Fondo de ventana opcional: vacío usa el fondo del tema Fluent; con `#RRGGBB` pinta el fondo de todas las ventanas.
- Un fondo personalizado exige tema Claro u Oscuro y contraste calculado de al menos 4.5:1 con el color base de texto; así se previenen ventanas ilegibles.
- Cambios de tema y recursos visuales aplicados a todas las ventanas abiertas, sin reiniciar la aplicación. Las siguientes ventanas usan las preferencias guardadas.

## Atajos configurables

| Acción | Predeterminado |
| --- | --- |
| Nueva sesión | Ctrl+T |
| Clonar sesión | Ctrl+Shift+T |
| Nueva ventana | Ctrl+Shift+N |
| Clonar en ventana | Ctrl+Alt+N |
| Cerrar pestaña | Ctrl+W |
| Ejecutar comando aprobado | Ctrl+Enter |

Escribí combinaciones como `Ctrl+T`, `Alt+N`, `Ctrl+Shift+T` o `F5` **junto con Ctrl o Alt**. Sólo se aceptan combinaciones con Control/Alt y letra A-Z, F1-F12 o Enter. Se rechazan duplicados entre acciones, atajos inválidos y combinaciones que capturarían escritura normal. Las teclas Tab, Enter y flechas dentro de la semi-CLI conservan su función de autocompletado/navegación.

**Restaurar valores** rellena los predeterminados, pero no aplica nada hasta pulsar **Guardar cambios**. **Cancelar** descarta modificaciones pendientes.

## Persistencia y privacidad

Los ajustes se escriben en `%APPDATA%\SolarisPKN\AntiCLI\settings.json`, no en el repositorio Git público. No se guardan secretos SSH ni passphrases en las preferencias visuales. Se guarda atómicamente, y al volver a abrir el programa se cargan los valores elegidos.

## Verificación pendiente

Pendiente compilar y comprobar en Windows: cambio de tema en vivo con dos ventanas, persistencia tras reiniciar, atajos personalizados en cada ventana, detección de duplicados, contraste de fondo y restauración al tema automático.

El Bridge puede guardar código del proyecto, pero aún no expone una herramienta para ejecutar `dotnet build` sobre el Windows del usuario. No marcar como verificado hasta ejecutar la compilación local.
