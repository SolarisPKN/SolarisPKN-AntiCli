# Comandos visuales, consola asistida y módulos

## Secuencia de pestañas

**1. Entorno y SSH:** elegir Debian/Ubuntu/Windows/otro Linux y configurar autenticación. Las cuatro casillas DPAPI (usuario, clave, contraseña y passphrase) son independientes y optativas.

**2. Comandos y formularios:** bloqueada hasta comprobar SSH, host key y sistema operativo. El **buscador universal por intención** encuentra tareas como «crear usuarios», «actualizar paquetes» o «ver kernel»; al elegir una, el **mismo renderer genérico** interpreta su ficha JSON y muestra descripción, casillas para flags, campos de operandos y cambios de orden ↑/↓. La vista previa genera un comando editable. Un módulo con soporte de lote puede crear nombres correlativos como `alumno01` a `alumno20`, interrumpiéndose ante un error. **No se crean vistas distintas por comando.** Las 153 acciones base utilizan el mismo renderer, mientras que las fichas enriquecidas ofrecen controles más específicos. Se pueden escribir comandos manuales y usar sugerencias con Tab y flechas. Los indicadores son 🟢 consulta, 🟡 precaución, 🔴 alto riesgo/desconocido; **Ejecutar actúa con un solo clic, sin confirmación adicional**.

**3. Crear módulos:** separada de la operación diaria. Requiere SSH válido y autorización explícita sobre una máquina de pruebas; genera logs de ayuda y borradores JSON `draft-unreviewed`. Es posible reutilizar esos comandos y ejecutarlos, pero siguen sin validación de seguridad.

## Descubrimiento de Salt y otras herramientas

Salt emplea diferentes ejecutables: `salt`, `salt-call`, `salt-run`, `salt-key`, `salt-cp`, `salt-ssh`. Se puede explorar cada uno por `--help`. No ejecutar combinaciones arbitrarias ni asumir que `salt create` existe. El descubrimiento de funciones Salt remotas y sus minions necesita un contrato adicional con permisos, no se hace en esta fase.

El catálogo inicial incluye [17 módulos y 146 comandos documentados](COMMAND_MODULES.es.md). **Fichas JSON:** `src/SolarisPKN.AntiCli/Data/guided-commands.json` (definiciones detalladas) y `src/SolarisPKN.AntiCli/Data/admin-commands.json` (catálogo base). La aplicación permite incorporar ficheros `*.json` del usuario bajo `%APPDATA%\SolarisPKN\AntiCLI\modules\` para ampliar los formularios sin recompilar; no sustituyen automáticamente las definiciones integradas. Cada esquema describe: nombre, búsqueda, flags, tipo de valor, operandos, plantilla, riesgo, compatibilidad, lote y timeout. Para una explicación del contrato, ver [FORMULARIOS_DINAMICOS.md](FORMULARIOS_DINAMICOS.md). Véase también [SKILL.md](../SKILL.md).

## Seguridad

Los logs de exploración se guardan fuera del repositorio en `%APPDATA%\SolarisPKN\AntiCLI\discovery`. Pueden contener detalles del entorno remoto y no se comparten automáticamente. Los comandos detectados se presentan como sugerencias; cuando el administrador los elige, se pueden ejecutar por SSH como cualquier comando manual. La clasificación de comandos desconocidos es 🔴 y no existe una segunda confirmación. El botón sudo agrega `sudo -n -- sh -c '…'` a la ejecución Linux, sin introducir contraseñas ni pedir UAC en la PC Windows.
