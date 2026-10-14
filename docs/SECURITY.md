# Seguridad de Solaris AntiCLI

## Medidas actuales

- El operador **elige plataforma** antes de autenticarse: Debian, Ubuntu, Windows u otra distribución Linux.
- Primera conexión (TOFU): se consulta la clave pública del servidor durante el intercambio SSH con autenticación «none» y se rechaza la clave durante ese sondeo: no se envían usuario real, contraseña ni clave privada. El operador debe aceptar expresamente la huella observada en una ventana de advertencia; idealmente la compara con el panel del proveedor.
- Los pines aceptados se guardan localmente en `%APPDATA%\\SolarisPKN\\AntiCLI\\known-hosts.json`, asociados a **host, puerto y algoritmo**. La huella guardada es prioritaria respecto de los datos editables del perfil y se verifica también en cada comando.
- Cambios de clave SSH: se **bloquea** la autenticación. El botón «Detectar / revisar clave» permite revisar y reemplazar una clave antigua exclusivamente tras confirmación consciente de haberla verificado por un canal independiente.
- Se comprueba el sistema remoto con una consulta fija antes de habilitar botones de comandos.
- Usuario, archivo de clave privada, contraseña y passphrase se recuerdan **solo por opt-in separado**. Si el usuario lo marca, DPAPI CurrentUser cifra ese dato en Windows; si no, no se escribe en `profiles.json`.
- Guardar clave privada cifra su contenido con DPAPI, no deja una copia en claro dentro del proyecto.
- Los catálogos contienen exclusivamente consultas predefinidas y son específicos del sistema remoto. Ni las sugerencias creadas por IA ni los borradores pueden ejecutarse por SSH automáticamente.
- El módulo de ayuda tiene límite, consentimiento y ejecución solo en servidor de pruebas.

## Limitaciones

- **TOFU no autentica la identidad de un servidor desconocido:** si aceptás una clave no verificada, podrías fijar la de un atacante. Confirmá la huella con el proveedor antes de confiar.
- La base de host keys contiene metadatos no secretos y aplica pinning en conexiones posteriores; no equivale a una autoridad certificadora. Si se pierde o borra, se deberá repetir la aceptación informada. Claves anteriores a esta funcionalidad que solo estén en perfiles siguen exigiendo su SHA256.


- DPAPI CurrentUser **no es un segundo factor** ni protege contra procesos maliciosos que corran bajo la misma cuenta Windows.
- PIN propio, Windows Hello y bóveda con hardware están pendientes.
- Los metadatos del servidor (host, puerto, plataforma, huella) siguen en texto plano. La memoria de proceso puede contener temporalmente secretos y cadenas de .NET inmutables que no pueden borrarse garantizadamente.
- Un programa puede ignorar `--help` o tener efectos secundarios; jamás asumir que la exploración es un sandbox.
- No hay pruebas funcionales terminadas. No usar en producción empresarial sin autorización.
- Perfiles guardados por la versión anterior pueden contener campos heredados. Reconfigurar preferencias, examinar el archivo anterior si existiera y evitar publicar copias históricas.

## Reglas de uso laboral

Verificar permisos de la empresa, credenciales autorizadas y huella del servidor por otro canal. No guardar ni publicar secretos en repositorios. Las consultas de descubrimiento pueden revelar datos corporativos en logs; revisarlos antes de compartirlos. Las operaciones administrativas necesitan un sistema independiente de permisos y aprobación, todavía no implementado.
