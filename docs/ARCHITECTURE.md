# Arquitectura — Solaris AntiCLI System

```text
Avalonia UI
  ├─ Pestaña Entorno y SSH
  │     ├─ Plataforma seleccionada (Debian / Ubuntu / Windows / Otro Linux)
  │     ├─ ProfileStore → %APPDATA%/SolarisPKN/AntiCLI/profiles.json
  │     │    ├─ host, puerto, plataforma y huella de host (metadatos de perfil)
  │     │    └─ cuatro DPAPI opt-in: usuario / clave privada / contraseña / passphrase
  │     └─ SshProbe → conexión SSH.NET, host key pin, verificación de plataforma
  ├─ Pestaña Comandos y bloques (habilitada tras autenticación)
  │     ├─ PlatformReadOnlyActions → catálogo por plataforma
  │     ├─ botones por acción y tokens por comando
  │     ├─ autocompletado Tab / flechas / Enter
  │     └─ ejecución SSH de consulta exacta aprobada
  └─ Pestaña Crear módulos (habilitada tras autenticación)
        ├─ HelpDiscoveryService → --help con consentimiento en servidor de prueba
        └─ ModuleDraftStore → logs y borradores sin permiso de ejecución
```

El cliente tiene interfaz gráfica local. El servidor solamente necesita SSH y las herramientas que vaya a utilizar. La autenticación inicial comprueba usuario, clave de servidor y SO antes de habilitar comandos. Cada acción posterior vuelve a abrir una conexión y verifica la host key.

## Persistencia

`profiles.json` no incluye usuario ni contraseñas o clave privada en texto plano de esta versión. **Las cuatro casillas independientes** determinan si cada campo se cifra con DPAPI CurrentUser. Cuando se decide no recordar, no se serializa el valor ni la ruta de clave. Los metadatos de conexión (nombre de perfil, host, puerto, SO y huella) siguen sin cifrar; evitar máquinas corporativas no autorizadas.

## Contratos para próximos módulos

Módulos declarativos: ID estable, SO, versión, dependencias, permisos, acciones y argumentos validados, descripción en español, seguridad de escape de shell, límites, hashes de fuentes y pruebas. Los borradores creados por la IA nunca son ejecutables sin validación.

## Pendientes

- Compilar y ejecutar la GUI en un Windows de prueba.
- Pruebas de DPAPI por campo y verificación de host + OS.
- Bloqueo criptográfico de la bóveda con Windows Hello/TPM o contraseña maestra fuerte.
- Compatibilidad de almacenamiento en clientes Linux/macOS.
- Constructor de formularios dinámicos y revisión de módulos con IA.
