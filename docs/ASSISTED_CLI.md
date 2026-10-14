# Comandos visuales, consola asistida y módulos

## Secuencia de pestañas

**1. Entorno y SSH:** elegir Debian/Ubuntu/Windows/otro Linux y configurar autenticación. Las cuatro casillas DPAPI (usuario, clave, contraseña y passphrase) son independientes y optativas.

**2. Comandos y bloques:** bloqueada hasta comprobar SSH, host key y sistema operativo. Cada plataforma tiene su catálogo de consultas. Un botón de acción crea botones para cada segmento del comando, que se pueden tocar para construirlo. Se pueden deshacer fragmentos, escribir manualmente o usar sugerencias con Tab y flechas. Los comandos documentados se muestran con indicador de riesgo (🟢 consulta, 🟡 precaución, 🔴 alto riesgo). **Se puede ejecutar cualquier comando** escrito manualmente; si no está catalogado, aparece 🔴. El botón Ejecutar adopta ese color y **ejecuta en un solo clic sin confirmación adicional**.

**3. Crear módulos:** separada de la operación diaria. Requiere SSH válido y autorización explícita sobre una máquina de pruebas; genera logs de ayuda y borradores JSON `draft-unreviewed`. Es posible reutilizar esos comandos y ejecutarlos, pero siguen sin validación de seguridad.

## Descubrimiento de Salt y otras herramientas

Salt emplea diferentes ejecutables: `salt`, `salt-call`, `salt-run`, `salt-key`, `salt-cp`, `salt-ssh`. Se puede explorar cada uno por `--help`. No ejecutar combinaciones arbitrarias ni asumir que `salt create` existe. El descubrimiento de funciones Salt remotas y sus minions necesita un contrato adicional con permisos, no se hace en esta fase.

El catálogo inicial incluye [17 módulos y 146 comandos documentados](COMMAND_MODULES.es.md). La traducción detallada mediante IA y la generación de formularios específicos permanecen pendientes. Véase [SKILL.md](../SKILL.md).

## Seguridad

Los logs de exploración se guardan fuera del repositorio en `%APPDATA%\SolarisPKN\AntiCLI\discovery`. Pueden contener detalles del entorno remoto y no se comparten automáticamente. Los comandos detectados se presentan como sugerencias; cuando el administrador los elige, se pueden ejecutar por SSH como cualquier comando manual. La clasificación de comandos desconocidos es 🔴 y no existe una segunda confirmación. El botón sudo agrega `sudo -n -- sh -c '…'` a la ejecución Linux, sin introducir contraseñas ni pedir UAC en la PC Windows.
