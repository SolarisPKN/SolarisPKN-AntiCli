# Catálogo de módulos administrativos de AntiCLI

**Edición:** 2026-10-14 · **146 acciones de 17 módulos**.

AntiCLI permite ejecutar **todos los comandos escritos por el administrador**. Las acciones de esta tabla son atajos documentados; no representan la totalidad de Linux. Un comando manual que no coincida exactamente con uno catalogado se muestra **🔴** por precaución. El color es orientativo, no un mecanismo de bloqueo.

- **🟢 Verde:** consulta o comprobación de bajo riesgo.
- **🟡 Amarillo:** modificación limitada, dato sensible o precaución.
- **🔴 Rojo:** cambios importantes, borrado, interrupción, acceso elevado o comando desconocido.

**Sin doble confirmación:** el botón de ejecutar cambia al color del riesgo y la acción se dispara con un clic. **Sudo** se habilita manualmente con una casilla, por sesión, para ejecutar mediante `sudo -n -- sh -c '…'`. Requiere permisos sudo no interactivos; si el servidor exige contraseña de sudo, el comando fallará y mostrará el error (AntiCLI no reutiliza silenciosamente la contraseña SSH).

**Compatibilidad:** módulos Linux, Debian y Ubuntu; la presencia y sintaxis de cada ejecutable dependen del servidor remoto. La lista de paquetes Ubuntu aportada se usó para priorizar Salt, Docker, Icinga2, Azure CLI, LVM/RAID, UFW y herramientas habituales. **No se incluyó el inventario completo del servidor ni sus versiones en el repositorio público.** Los ejemplos con `anticli-test`, `/dev/sdb1` o `/dev/md0` son ilustrativos: adaptalos antes de ejecutar; algunos tienen consecuencias destructivas.

**Referencias:** [LinuxCommand.org — índice de manuales](https://linuxcommand.org/lc3_man_page_index.php) · [Manual Ubuntu](https://manpages.ubuntu.com/). El catálogo es elaboración propia a partir de comandos Linux y del entorno de prueba; no importa ni copia automáticamente textos de páginas externas.

## Sistema (`system`) — 9 comandos

Identidad, actividad, reloj y encendido/apagado del servidor.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Detectar sistema | `cat /etc/os-release` | Distribución Linux |
| 🟢 | Kernel y arquitectura | `uname -a` | Versión del kernel |
| 🟢 | Tiempo encendido | `uptime` | Estado del sistema |
| 🟢 | Sesiones activas | `who` | Usuarios conectados |
| 🟢 | Fecha del equipo | `date -Is` | Reloj actual |
| 🟢 | Nombre del equipo | `hostnamectl status` | Identidad y sistema |
| 🔴 | Reiniciar servidor | `systemctl reboot` | Reinicia el servidor y corta SSH |
| 🔴 | Apagar servidor | `systemctl poweroff` | Apaga el servidor remoto |
| 🟡 | Sincronizar escrituras | `sync` | Solicita escritura de buffers pendientes |

## Archivos (`files`) — 12 comandos

Inspección, creación, copia, borrado de archivos y sus permisos.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Listar archivos | `ls -lah` | Listado detallado del directorio |
| 🟢 | Ruta actual | `pwd` | Directorio de ejecución remoto |
| 🟢 | Uso por carpeta | `du -h --max-depth=1 .` | Espacio ocupado por subcarpeta |
| 🟢 | Buscar archivos recientes | `find . -maxdepth 2 -type f -mtime -1` | Archivos modificados en un día |
| 🟢 | Árbol de carpetas | `find . -maxdepth 2 -print` | Estructura de carpetas |
| 🟢 | Tamaño de archivos | `stat /etc/os-release` | Metadatos de archivo |
| 🟡 | Crear carpeta temporal | `mkdir -p /tmp/anticli-test` | Crea directorio temporal de prueba |
| 🟡 | Crear archivo temporal | `touch /tmp/anticli-test/marker` | Crea o modifica un archivo |
| 🟡 | Copiar archivo de prueba | `cp /etc/os-release /tmp/anticli-os-release-copy` | Genera copia en /tmp |
| 🔴 | Eliminar copia temporal | `rm -f /tmp/anticli-os-release-copy` | Elimina un archivo sin papelera |
| 🟡 | Cambiar permisos de ejemplo | `chmod 600 /tmp/anticli-test/marker` | Cambia permisos del archivo indicado |
| 🔴 | Cambiar propietario | `chown root:root /tmp/anticli-test/marker` | Modifica propiedad; requiere privilegios |

## Texto y documentación (`text`) — 9 comandos

Lectura, filtrado, ordenación, conteo y hashes de textos.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Primeras líneas | `head -n 20 /etc/passwd` | Lee 20 líneas |
| 🟢 | Últimos logs | `tail -n 50 /var/log/syslog` | Lee últimas líneas si accesibles |
| 🟢 | Buscar usuario root | `grep '^root:' /etc/passwd` | Busca patrón literal |
| 🟢 | Contar cuentas | `wc -l /etc/passwd` | Cuenta registros |
| 🟢 | Extraer usuarios | `cut -d: -f1 /etc/passwd` | Muestra los nombres de usuario |
| 🟢 | Ordenar usuarios | `cut -d: -f1 /etc/passwd \| sort` | Ordena un listado |
| 🟢 | Huella de archivo | `sha256sum /etc/os-release` | Calcula SHA256 |
| 🟢 | Ayuda del sistema | `man -k network` | Busca manuales por tema |
| 🟡 | Variables de entorno | `env` | Puede mostrar variables sensibles |

## Procesos (`process`) — 7 comandos

Inspección de CPU, memoria y procesos, terminación y prioridades.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Procesos | `ps aux` | Lista los procesos y propietarios |
| 🟢 | Procesos por CPU | `ps aux --sort=-%cpu \| head -n 20` | Mayor consumo de CPU |
| 🟢 | Memoria RAM | `free -h` | Memoria total, usada y libre |
| 🟢 | Estadísticas de CPU | `vmstat 1 3` | Muestras de memoria y CPU |
| 🔴 | Finalizar proceso de prueba | `pkill -f anticli-test-process` | Termina procesos que coincidan |
| 🟡 | Prioridad de ejemplo | `renice 10 -p 12345` | Ajusta prioridad de PID de ejemplo |
| 🟡 | Trazar proceso | `strace -p 12345 -c` | Adjunta rastreador al PID indicado |

## Red (`network`) — 10 comandos

Interfaces, IP, rutas, DNS, conectividad y servicios de red.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Interfaces de red | `ip -brief address` | IP por interfaz |
| 🟢 | Rutas de red | `ip route show` | Tabla de rutas |
| 🟢 | Sockets abiertos | `ss -tulpen` | Puertos y procesos |
| 🟢 | Comprobar DNS | `ping -c 4 1.1.1.1` | Comprueba conectividad |
| 🟢 | Rastrear red | `tracepath 1.1.1.1` | Rastreo de ruta de paquetes |
| 🟢 | Resolver hostname | `getent hosts example.org` | Consulta nombre DNS |
| 🟢 | Enlaces de red | `ip -s link` | Estado y contadores de interfaces |
| 🟡 | Consultar IP pública | `curl -fsS https://api.ipify.org` | Envía solicitud HTTPS al servicio externo |
| 🟡 | Vaciar caché DNS | `resolvectl flush-caches` | Borra caché DNS del servidor |
| 🔴 | Reiniciar resolved | `systemctl restart systemd-resolved` | Interrumpe temporalmente la resolución DNS |

## Servicios (`services`) — 9 comandos

Consulta y administración de servicios systemd.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Todos los servicios | `systemctl list-units --type=service --all --no-pager` | Lista unidades systemd |
| 🟢 | Servicios fallidos | `systemctl --failed --no-pager` | Muestra fallas de unidades |
| 🟢 | Estado SSH | `systemctl status ssh --no-pager` | Consulta el servicio SSH |
| 🟢 | Estado Docker | `systemctl status docker --no-pager` | Consulta el servicio Docker |
| 🟡 | Recargar configuración systemd | `systemctl daemon-reload` | Recarga unidades del sistema |
| 🔴 | Reiniciar cron | `systemctl restart cron` | Reinicia servicio cron |
| 🔴 | Detener cron | `systemctl stop cron` | Detiene el servicio cron |
| 🟡 | Habilitar cron al inicio | `systemctl enable cron` | Modifica arranque automático |
| 🟡 | Deshabilitar cron al inicio | `systemctl disable cron` | Modifica arranque automático |

## Paquetes APT (`packages`) — 9 comandos

APT: consulta, actualización, instalación y desinstalación.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Paquetes instalados | `dpkg-query -W` | Inventario APT instalado |
| 🟢 | Paquetes actualizables | `apt list --upgradable` | Muestra actualizaciones disponibles |
| 🟢 | Repositorios | `apt-cache policy` | Fuentes de paquetes y prioridades |
| 🟢 | Paquetes retenidos | `apt-mark showhold` | Paquetes bloqueados |
| 🟡 | Actualizar índices | `apt-get update` | Descarga índices APT |
| 🔴 | Actualizar paquetes | `apt-get upgrade -y` | Cambia paquetes instalados sin confirmación interactiva |
| 🔴 | Eliminar dependencias huérfanas | `apt-get autoremove -y` | Desinstala paquetes sobrantes |
| 🔴 | Instalar herramienta jq | `apt-get install -y jq` | Instala paquetes del sistema |
| 🔴 | Quitar herramienta jq | `apt-get remove -y jq` | Desinstala jq |

## Discos y RAID (`disks`) — 14 comandos

Discos, archivos, particiones, LVM y RAID.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Uso de disco | `df -hT` | Montajes y utilización |
| 🟢 | Bloques y particiones | `lsblk -o NAME,SIZE,FSTYPE,MOUNTPOINTS` | Mapa de discos |
| 🟢 | Montajes | `findmnt` | Puntos montados |
| 🟢 | Estado RAID | `cat /proc/mdstat` | Arreglos MD RAID |
| 🟢 | Detalle MD RAID | `mdadm --detail --scan` | Resumen de arreglos RAID |
| 🟢 | Volúmenes LVM | `lvs -a` | Volúmenes lógicos |
| 🟢 | Grupos LVM | `vgs` | Grupos de volúmenes |
| 🟢 | Discos LVM | `pvs` | Volúmenes físicos |
| 🟢 | Tablas de particiones | `fdisk -l` | Inventario de particiones |
| 🟢 | Configuración de montaje | `cat /etc/fstab` | Montajes configurados |
| 🟡 | Revisar sistema ext4 | `fsck -n /dev/sdb1` | Chequeo no destructivo en dispositivo de ejemplo; no usar sobre FS montado |
| 🔴 | Detener RAID de ejemplo | `mdadm --stop /dev/md0` | Detiene un arreglo RAID; riesgo de disponibilidad |
| 🔴 | Formatear partición de ejemplo | `mkfs.ext4 /dev/sdb1` | DESTRUCTIVO: elimina estructura previa de datos |
| 🔴 | Eliminar volumen de ejemplo | `lvremove -y /dev/vg0/lv0` | DESTRUCTIVO: elimina volumen LVM |

## Firewall (`firewall`) — 8 comandos

Consulta y cambios en UFW, nftables e iptables.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Estado UFW | `ufw status verbose` | Reglas del firewall UFW |
| 🟢 | Reglas nftables | `nft list ruleset` | Tabla de reglas nftables |
| 🟢 | Reglas iptables | `iptables -S` | Configuración iptables |
| 🔴 | Activar firewall | `ufw --force enable` | Puede bloquear SSH y cortarte el acceso |
| 🔴 | Desactivar firewall | `ufw disable` | Expone servicios previamente protegidos |
| 🟡 | Permitir SSH | `ufw allow 22/tcp` | Abre puerto SSH TCP |
| 🔴 | Bloquear SSH | `ufw deny 22/tcp` | Puede cortar el acceso remoto |
| 🟡 | Recargar firewall | `ufw reload` | Reaplica reglas activas |

## Usuarios y seguridad (`security`) — 8 comandos

Cuentas, grupos, SSH y administración de usuarios.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Identidad actual | `id` | UID, grupos y privilegios |
| 🟢 | Grupos del usuario | `groups` | Membresías |
| 🟢 | Usuarios locales | `getent passwd` | Lista cuentas locales |
| 🟡 | Permisos sudo | `sudo -n -l` | Consulta privilegios sin solicitar contraseña |
| 🟢 | Huella del host | `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256` | Fingerprint SSH del servidor |
| 🔴 | Crear usuario de ejemplo | `useradd -m anticli_test` | Crea una cuenta Linux |
| 🔴 | Bloquear usuario de ejemplo | `passwd -l anticli_test` | Bloquea inicio de sesión de la cuenta |
| 🔴 | Eliminar usuario de ejemplo | `userdel -r anticli_test` | Elimina cuenta y directorio personal |

## Docker y Compose (`docker`) — 11 comandos

Contenedores, imágenes, recursos y cargas Docker Compose.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Versión de Docker | `docker version` | Versiones de motor/cliente |
| 🟢 | Contenedores | `docker ps -a` | Lista todos los contenedores |
| 🟢 | Imágenes | `docker image ls` | Lista imágenes locales |
| 🟢 | Redes Docker | `docker network ls` | Lista redes Docker |
| 🟢 | Volúmenes Docker | `docker volume ls` | Lista volúmenes Docker |
| 🟢 | Recursos de contenedores | `docker stats --no-stream` | Consumo instantáneo |
| 🟢 | Servicios Compose | `docker compose ps` | Lista servicios Compose |
| 🔴 | Limpiar imágenes sin uso | `docker image prune -f` | Elimina imágenes Docker no usadas |
| 🔴 | Reiniciar contenedor de ejemplo | `docker restart anticli-test` | Interrumpe el contenedor indicado |
| 🔴 | Crear o actualizar Compose | `docker compose up -d` | Puede recrear contenedores y modificar infraestructura |
| 🔴 | Detener Compose | `docker compose down` | Detiene y elimina recursos Compose |

## SaltStack (`salt`) — 8 comandos

Salt: grains, pillar, estados y gestión de claves.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Versión Salt | `salt-call --version` | Versión del minion Salt |
| 🟢 | Grains locales | `salt-call --local grains.items --out=json` | Datos del minion |
| 🟡 | Pillar local | `salt-call --local pillar.items --out=json` | PUEDE revelar secretos; no compartir salida |
| 🟡 | Simular highstate | `salt-call --local state.apply test=True --out=json` | Simula estados Salt; requiere permisos |
| 🔴 | Aplicar estados | `salt-call --local state.apply --out=json` | Modifica sistema según definiciones Salt |
| 🟢 | Estado minion | `salt-call test.ping --out=json` | Comprueba Salt minion |
| 🟡 | Claves Salt | `salt-key -L` | Muestra claves del master, si instalado |
| 🔴 | Aceptar claves pendientes | `salt-key -A -y` | Confía en claves Salt pendientes |

## Monitorización (`monitor`) — 7 comandos

Monitorización Icinga2, validación y diagnósticos.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Estado Icinga2 | `systemctl status icinga2 --no-pager` | Estado servicio Icinga |
| 🟢 | Validar configuración Icinga2 | `icinga2 daemon -C` | Valida config sin activarla |
| 🟡 | Recargar Icinga2 | `systemctl reload icinga2` | Actualiza configuración en ejecución |
| 🔴 | Reiniciar Icinga2 | `systemctl restart icinga2` | Reinicia servicio monitorización |
| 🟢 | Logs críticos | `journalctl -p err -n 50 --no-pager` | Últimos errores del journal |
| 🟢 | Logs de Icinga2 | `journalctl -u icinga2 -n 60 --no-pager` | Eventos recientes del servicio |
| 🟢 | Temperaturas y sensores | `sensors` | Requiere lm-sensors |

## Nube Azure (`cloud`) — 7 comandos

Azure CLI: versiones, suscripciones y recursos.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Azure CLI | `az version` | Versiones Azure CLI |
| 🟡 | Cuenta activa | `az account show` | Muestra suscripción y tenant de la sesión |
| 🟡 | Suscripciones | `az account list --output table` | Enumera metadatos de suscripciones |
| 🟢 | Grupos de recursos | `az group list --output table` | Recursos del tenant autorizado |
| 🟢 | Máquinas virtuales | `az vm list --output table` | Lista máquinas virtuales |
| 🟡 | Cerrar sesión de Azure | `az logout` | Cierra la sesión de Azure CLI |
| 🟡 | Autenticar Azure | `az login --use-device-code` | Inicia login interactivo; puede no funcionar en SSH sin TTY |

## Logs y auditoría (`logs`) — 6 comandos

Journal, SSH, autenticación y rotación de registros.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Logs recientes | `journalctl -n 100 --no-pager` | Últimos eventos del sistema |
| 🟢 | Inicios del sistema | `journalctl --list-boots --no-pager` | Registro de reinicios |
| 🟢 | Fallos SSH | `journalctl -u ssh -n 100 --no-pager` | Historial SSH |
| 🟢 | Syslog | `tail -n 100 /var/log/syslog` | Puede revelar datos operativos |
| 🟡 | Eventos de autenticación | `tail -n 80 /var/log/auth.log` | Logs con información sensible |
| 🔴 | Forzar rotación de logs | `logrotate -f /etc/logrotate.conf` | Fuerza rotación del sistema |

## Git (`git`) — 7 comandos

Git: estado, historial, ramas y operaciones de actualización.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Versión de Git | `git --version` | Comprueba instalación |
| 🟢 | Estado del repositorio | `git status --short` | Estado en el directorio remoto |
| 🟢 | Historial reciente | `git log -5 --oneline` | Últimos commits |
| 🟢 | Ramas locales | `git branch --list` | Ramas del repositorio |
| 🟡 | Descargar refs remotas | `git fetch --all --prune` | Actualiza refs locales de Git |
| 🔴 | Actualizar rama | `git pull --ff-only` | Modifica árbol de trabajo remoto |
| 🔴 | Descartar cambios locales | `git reset --hard HEAD` | DESTRUCTIVO: descarta cambios sin confirmar |

## Copias y compresión (`backup`) — 5 comandos

Rsync, archivos tar, copias y sincronización con borrado.

| Riesgo | Acción | Comando | Descripción |
|---|---|---|---|
| 🟢 | Simulación copia local | `rsync -avn /etc/ /tmp/anticli-etc-backup/` | Simula sincronización |
| 🟢 | Listar tar de ejemplo | `tar -tf /tmp/backup.tar` | Lista archivo, si existe |
| 🟡 | Copiar configuración | `rsync -av /etc/ /tmp/anticli-etc-backup/` | Copia archivos en /tmp |
| 🟡 | Comprimir configuración | `tar -czf /tmp/anticli-etc-backup.tar.gz /etc` | Escribe archivo comprimido; puede contener secretos |
| 🔴 | Sincronizar /tmp | `rsync -av --delete /tmp/a/ /tmp/b/` | DESTRUCTIVO en destino: --delete elimina archivos |

