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
