using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

public enum CommandRisk { Green, Yellow, Red }

public sealed record AdminCommand(
    string Id, string Module, string GroupName, string LabelEs,
    string Command, CommandRisk Risk, string DescriptionEs);

/// <summary>
/// Curated quick actions for administrators. An action's dot is advisory: the
/// remote user and OS still enforce the actual permissions. No secrets or
/// server inventory are embedded in this public catalog.
/// </summary>
public static class AdminCommandCatalog
{
    public static IReadOnlyList<AdminCommand> Linux { get; } =
    [
        new("system.os", "system", "Sistema", "Detectar sistema", "cat /etc/os-release", CommandRisk.Green, "Distribución Linux"),
        new("system.kernel", "system", "Sistema", "Kernel y arquitectura", "uname -a", CommandRisk.Green, "Versión del kernel"),
        new("system.uptime", "system", "Sistema", "Tiempo encendido", "uptime", CommandRisk.Green, "Estado del sistema"),
        new("system.users", "system", "Sistema", "Sesiones activas", "who", CommandRisk.Green, "Usuarios conectados"),
        new("system.date", "system", "Sistema", "Fecha del equipo", "date -Is", CommandRisk.Green, "Reloj actual"),
        new("system.host", "system", "Sistema", "Nombre del equipo", "hostnamectl status", CommandRisk.Green, "Identidad y sistema"),
        new("system.reboot", "system", "Sistema", "Reiniciar servidor", "systemctl reboot", CommandRisk.Red, "Reinicia el servidor y corta SSH"),
        new("system.poweroff", "system", "Sistema", "Apagar servidor", "systemctl poweroff", CommandRisk.Red, "Apaga el servidor remoto"),
        new("system.sync", "system", "Sistema", "Sincronizar escrituras", "sync", CommandRisk.Yellow, "Solicita escritura de buffers pendientes"),
        new("files.list", "files", "Archivos", "Listar archivos", "ls -lah", CommandRisk.Green, "Listado detallado del directorio"),
        new("files.pwd", "files", "Archivos", "Ruta actual", "pwd", CommandRisk.Green, "Directorio de ejecución remoto"),
        new("files.du", "files", "Archivos", "Uso por carpeta", "du -h --max-depth=1 .", CommandRisk.Green, "Espacio ocupado por subcarpeta"),
        new("files.find", "files", "Archivos", "Buscar archivos recientes", "find . -maxdepth 2 -type f -mtime -1", CommandRisk.Green, "Archivos modificados en un día"),
        new("files.tree", "files", "Archivos", "Árbol de carpetas", "find . -maxdepth 2 -print", CommandRisk.Green, "Estructura de carpetas"),
        new("files.size", "files", "Archivos", "Tamaño de archivos", "stat /etc/os-release", CommandRisk.Green, "Metadatos de archivo"),
        new("files.mkdir", "files", "Archivos", "Crear carpeta temporal", "mkdir -p /tmp/anticli-test", CommandRisk.Yellow, "Crea directorio temporal de prueba"),
        new("files.touch", "files", "Archivos", "Crear archivo temporal", "touch /tmp/anticli-test/marker", CommandRisk.Yellow, "Crea o modifica un archivo"),
        new("files.copy", "files", "Archivos", "Copiar archivo de prueba", "cp /etc/os-release /tmp/anticli-os-release-copy", CommandRisk.Yellow, "Genera copia en /tmp"),
        new("files.remove", "files", "Archivos", "Eliminar copia temporal", "rm -f /tmp/anticli-os-release-copy", CommandRisk.Red, "Elimina un archivo sin papelera"),
        new("files.chmod", "files", "Archivos", "Cambiar permisos de ejemplo", "chmod 600 /tmp/anticli-test/marker", CommandRisk.Yellow, "Cambia permisos del archivo indicado"),
        new("files.chown", "files", "Archivos", "Cambiar propietario", "chown root:root /tmp/anticli-test/marker", CommandRisk.Red, "Modifica propiedad; requiere privilegios"),
        new("text.head", "text", "Texto y documentación", "Primeras líneas", "head -n 20 /etc/passwd", CommandRisk.Green, "Lee 20 líneas"),
        new("text.tail", "text", "Texto y documentación", "Últimos logs", "tail -n 50 /var/log/syslog", CommandRisk.Green, "Lee últimas líneas si accesibles"),
        new("text.grep", "text", "Texto y documentación", "Buscar usuario root", "grep '^root:' /etc/passwd", CommandRisk.Green, "Busca patrón literal"),
        new("text.wc", "text", "Texto y documentación", "Contar cuentas", "wc -l /etc/passwd", CommandRisk.Green, "Cuenta registros"),
        new("text.cut", "text", "Texto y documentación", "Extraer usuarios", "cut -d: -f1 /etc/passwd", CommandRisk.Green, "Muestra los nombres de usuario"),
        new("text.sort", "text", "Texto y documentación", "Ordenar usuarios", "cut -d: -f1 /etc/passwd | sort", CommandRisk.Green, "Ordena un listado"),
        new("text.hash", "text", "Texto y documentación", "Huella de archivo", "sha256sum /etc/os-release", CommandRisk.Green, "Calcula SHA256"),
        new("text.help", "text", "Texto y documentación", "Ayuda del sistema", "man -k network", CommandRisk.Green, "Busca manuales por tema"),
        new("text.env", "text", "Texto y documentación", "Variables de entorno", "env", CommandRisk.Yellow, "Puede mostrar variables sensibles"),
        new("process.ps", "process", "Procesos", "Procesos", "ps aux", CommandRisk.Green, "Lista los procesos y propietarios"),
        new("process.top", "process", "Procesos", "Procesos por CPU", "ps aux --sort=-%cpu | head -n 20", CommandRisk.Green, "Mayor consumo de CPU"),
        new("process.memory", "process", "Procesos", "Memoria RAM", "free -h", CommandRisk.Green, "Memoria total, usada y libre"),
        new("process.iostat", "process", "Procesos", "Estadísticas de CPU", "vmstat 1 3", CommandRisk.Green, "Muestras de memoria y CPU"),
        new("process.kill", "process", "Procesos", "Finalizar proceso de prueba", "pkill -f anticli-test-process", CommandRisk.Red, "Termina procesos que coincidan"),
        new("process.renice", "process", "Procesos", "Prioridad de ejemplo", "renice 10 -p 12345", CommandRisk.Yellow, "Ajusta prioridad de PID de ejemplo"),
        new("process.strace", "process", "Procesos", "Trazar proceso", "strace -p 12345 -c", CommandRisk.Yellow, "Adjunta rastreador al PID indicado"),
        new("network.addr", "network", "Red", "Interfaces de red", "ip -brief address", CommandRisk.Green, "IP por interfaz"),
        new("network.routes", "network", "Red", "Rutas de red", "ip route show", CommandRisk.Green, "Tabla de rutas"),
        new("network.ports", "network", "Red", "Sockets abiertos", "ss -tulpen", CommandRisk.Green, "Puertos y procesos"),
        new("network.ping", "network", "Red", "Comprobar DNS", "ping -c 4 1.1.1.1", CommandRisk.Green, "Comprueba conectividad"),
        new("network.trace", "network", "Red", "Rastrear red", "tracepath 1.1.1.1", CommandRisk.Green, "Rastreo de ruta de paquetes"),
        new("network.dns", "network", "Red", "Resolver hostname", "getent hosts example.org", CommandRisk.Green, "Consulta nombre DNS"),
        new("network.interfaces", "network", "Red", "Enlaces de red", "ip -s link", CommandRisk.Green, "Estado y contadores de interfaces"),
        new("network.public", "network", "Red", "Consultar IP pública", "curl -fsS https://api.ipify.org", CommandRisk.Yellow, "Envía solicitud HTTPS al servicio externo"),
        new("network.dnsflush", "network", "Red", "Vaciar caché DNS", "resolvectl flush-caches", CommandRisk.Yellow, "Borra caché DNS del servidor"),
        new("network.networkrestart", "network", "Red", "Reiniciar resolved", "systemctl restart systemd-resolved", CommandRisk.Red, "Interrumpe temporalmente la resolución DNS"),
        new("services.all", "services", "Servicios", "Todos los servicios", "systemctl list-units --type=service --all --no-pager", CommandRisk.Green, "Lista unidades systemd"),
        new("services.failed", "services", "Servicios", "Servicios fallidos", "systemctl --failed --no-pager", CommandRisk.Green, "Muestra fallas de unidades"),
        new("services.ssh", "services", "Servicios", "Estado SSH", "systemctl status ssh --no-pager", CommandRisk.Green, "Consulta el servicio SSH"),
        new("services.docker", "services", "Servicios", "Estado Docker", "systemctl status docker --no-pager", CommandRisk.Green, "Consulta el servicio Docker"),
        new("services.reload", "services", "Servicios", "Recargar configuración systemd", "systemctl daemon-reload", CommandRisk.Yellow, "Recarga unidades del sistema"),
        new("services.restart", "services", "Servicios", "Reiniciar cron", "systemctl restart cron", CommandRisk.Red, "Reinicia servicio cron"),
        new("services.stop", "services", "Servicios", "Detener cron", "systemctl stop cron", CommandRisk.Red, "Detiene el servicio cron"),
        new("services.enable", "services", "Servicios", "Habilitar cron al inicio", "systemctl enable cron", CommandRisk.Yellow, "Modifica arranque automático"),
        new("services.disable", "services", "Servicios", "Deshabilitar cron al inicio", "systemctl disable cron", CommandRisk.Yellow, "Modifica arranque automático"),
        new("packages.installed", "packages", "Paquetes APT", "Paquetes instalados", "dpkg-query -W", CommandRisk.Green, "Inventario APT instalado"),
        new("packages.upgradable", "packages", "Paquetes APT", "Paquetes actualizables", "apt list --upgradable", CommandRisk.Green, "Muestra actualizaciones disponibles"),
        new("packages.sources", "packages", "Paquetes APT", "Repositorios", "apt-cache policy", CommandRisk.Green, "Fuentes de paquetes y prioridades"),
        new("packages.audit", "packages", "Paquetes APT", "Paquetes retenidos", "apt-mark showhold", CommandRisk.Green, "Paquetes bloqueados"),
        new("packages.update", "packages", "Paquetes APT", "Actualizar índices", "apt-get update", CommandRisk.Yellow, "Descarga índices APT"),
        new("packages.upgrade", "packages", "Paquetes APT", "Actualizar paquetes", "apt-get upgrade -y", CommandRisk.Red, "Cambia paquetes instalados sin confirmación interactiva"),
        new("packages.autoremove", "packages", "Paquetes APT", "Eliminar dependencias huérfanas", "apt-get autoremove -y", CommandRisk.Red, "Desinstala paquetes sobrantes"),
        new("packages.install", "packages", "Paquetes APT", "Instalar herramienta jq", "apt-get install -y jq", CommandRisk.Red, "Instala paquetes del sistema"),
        new("packages.remove", "packages", "Paquetes APT", "Quitar herramienta jq", "apt-get remove -y jq", CommandRisk.Red, "Desinstala jq"),
        new("disks.df", "disks", "Discos y RAID", "Uso de disco", "df -hT", CommandRisk.Green, "Montajes y utilización"),
        new("disks.lsblk", "disks", "Discos y RAID", "Bloques y particiones", "lsblk -o NAME,SIZE,FSTYPE,MOUNTPOINTS", CommandRisk.Green, "Mapa de discos"),
        new("disks.mounts", "disks", "Discos y RAID", "Montajes", "findmnt", CommandRisk.Green, "Puntos montados"),
        new("disks.raid", "disks", "Discos y RAID", "Estado RAID", "cat /proc/mdstat", CommandRisk.Green, "Arreglos MD RAID"),
        new("disks.mdadm", "disks", "Discos y RAID", "Detalle MD RAID", "mdadm --detail --scan", CommandRisk.Green, "Resumen de arreglos RAID"),
        new("disks.lvm", "disks", "Discos y RAID", "Volúmenes LVM", "lvs -a", CommandRisk.Green, "Volúmenes lógicos"),
        new("disks.vgs", "disks", "Discos y RAID", "Grupos LVM", "vgs", CommandRisk.Green, "Grupos de volúmenes"),
        new("disks.pvs", "disks", "Discos y RAID", "Discos LVM", "pvs", CommandRisk.Green, "Volúmenes físicos"),
        new("disks.fdisk", "disks", "Discos y RAID", "Tablas de particiones", "fdisk -l", CommandRisk.Green, "Inventario de particiones"),
        new("disks.fstab", "disks", "Discos y RAID", "Configuración de montaje", "cat /etc/fstab", CommandRisk.Green, "Montajes configurados"),
        new("disks.fsck", "disks", "Discos y RAID", "Revisar sistema ext4", "fsck -n /dev/sdb1", CommandRisk.Yellow, "Chequeo no destructivo en dispositivo de ejemplo; no usar sobre FS montado"),
        new("disks.md-stop", "disks", "Discos y RAID", "Detener RAID de ejemplo", "mdadm --stop /dev/md0", CommandRisk.Red, "Detiene un arreglo RAID; riesgo de disponibilidad"),
        new("disks.mkfs", "disks", "Discos y RAID", "Formatear partición de ejemplo", "mkfs.ext4 /dev/sdb1", CommandRisk.Red, "DESTRUCTIVO: elimina estructura previa de datos"),
        new("disks.lvremove", "disks", "Discos y RAID", "Eliminar volumen de ejemplo", "lvremove -y /dev/vg0/lv0", CommandRisk.Red, "DESTRUCTIVO: elimina volumen LVM"),
        new("firewall.ufw", "firewall", "Firewall", "Estado UFW", "ufw status verbose", CommandRisk.Green, "Reglas del firewall UFW"),
        new("firewall.nft", "firewall", "Firewall", "Reglas nftables", "nft list ruleset", CommandRisk.Green, "Tabla de reglas nftables"),
        new("firewall.iptables", "firewall", "Firewall", "Reglas iptables", "iptables -S", CommandRisk.Green, "Configuración iptables"),
        new("firewall.enable", "firewall", "Firewall", "Activar firewall", "ufw --force enable", CommandRisk.Red, "Puede bloquear SSH y cortarte el acceso"),
        new("firewall.disable", "firewall", "Firewall", "Desactivar firewall", "ufw disable", CommandRisk.Red, "Expone servicios previamente protegidos"),
        new("firewall.allow", "firewall", "Firewall", "Permitir SSH", "ufw allow 22/tcp", CommandRisk.Yellow, "Abre puerto SSH TCP"),
        new("firewall.deny", "firewall", "Firewall", "Bloquear SSH", "ufw deny 22/tcp", CommandRisk.Red, "Puede cortar el acceso remoto"),
        new("firewall.reload", "firewall", "Firewall", "Recargar firewall", "ufw reload", CommandRisk.Yellow, "Reaplica reglas activas"),
        new("security.id", "security", "Usuarios y seguridad", "Identidad actual", "id", CommandRisk.Green, "UID, grupos y privilegios"),
        new("security.groups", "security", "Usuarios y seguridad", "Grupos del usuario", "groups", CommandRisk.Green, "Membresías"),
        new("security.accounts", "security", "Usuarios y seguridad", "Usuarios locales", "getent passwd", CommandRisk.Green, "Lista cuentas locales"),
        new("security.sudo", "security", "Usuarios y seguridad", "Permisos sudo", "sudo -n -l", CommandRisk.Yellow, "Consulta privilegios sin solicitar contraseña"),
        new("security.ssh-fingerprint", "security", "Usuarios y seguridad", "Huella del host", "ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256", CommandRisk.Green, "Fingerprint SSH del servidor"),
        new("security.adduser", "security", "Usuarios y seguridad", "Crear usuario de ejemplo", "useradd -m anticli_test", CommandRisk.Red, "Crea una cuenta Linux"),
        new("security.passwd", "security", "Usuarios y seguridad", "Bloquear usuario de ejemplo", "passwd -l anticli_test", CommandRisk.Red, "Bloquea inicio de sesión de la cuenta"),
        new("security.deleteuser", "security", "Usuarios y seguridad", "Eliminar usuario de ejemplo", "userdel -r anticli_test", CommandRisk.Red, "Elimina cuenta y directorio personal"),
        new("docker.version", "docker", "Docker y Compose", "Versión de Docker", "docker version", CommandRisk.Green, "Versiones de motor/cliente"),
        new("docker.containers", "docker", "Docker y Compose", "Contenedores", "docker ps -a", CommandRisk.Green, "Lista todos los contenedores"),
        new("docker.images", "docker", "Docker y Compose", "Imágenes", "docker image ls", CommandRisk.Green, "Lista imágenes locales"),
        new("docker.networks", "docker", "Docker y Compose", "Redes Docker", "docker network ls", CommandRisk.Green, "Lista redes Docker"),
        new("docker.volumes", "docker", "Docker y Compose", "Volúmenes Docker", "docker volume ls", CommandRisk.Green, "Lista volúmenes Docker"),
        new("docker.stats", "docker", "Docker y Compose", "Recursos de contenedores", "docker stats --no-stream", CommandRisk.Green, "Consumo instantáneo"),
        new("docker.compose", "docker", "Docker y Compose", "Servicios Compose", "docker compose ps", CommandRisk.Green, "Lista servicios Compose"),
        new("docker.prune", "docker", "Docker y Compose", "Limpiar imágenes sin uso", "docker image prune -f", CommandRisk.Red, "Elimina imágenes Docker no usadas"),
        new("docker.restart", "docker", "Docker y Compose", "Reiniciar contenedor de ejemplo", "docker restart anticli-test", CommandRisk.Red, "Interrumpe el contenedor indicado"),
        new("docker.compose-up", "docker", "Docker y Compose", "Crear o actualizar Compose", "docker compose up -d", CommandRisk.Red, "Puede recrear contenedores y modificar infraestructura"),
        new("docker.compose-down", "docker", "Docker y Compose", "Detener Compose", "docker compose down", CommandRisk.Red, "Detiene y elimina recursos Compose"),
        new("salt.version", "salt", "SaltStack", "Versión Salt", "salt-call --version", CommandRisk.Green, "Versión del minion Salt"),
        new("salt.grains", "salt", "SaltStack", "Grains locales", "salt-call --local grains.items --out=json", CommandRisk.Green, "Datos del minion"),
        new("salt.pillar", "salt", "SaltStack", "Pillar local", "salt-call --local pillar.items --out=json", CommandRisk.Yellow, "PUEDE revelar secretos; no compartir salida"),
        new("salt.highstate", "salt", "SaltStack", "Simular highstate", "salt-call --local state.apply test=True --out=json", CommandRisk.Yellow, "Simula estados Salt; requiere permisos"),
        new("salt.states", "salt", "SaltStack", "Aplicar estados", "salt-call --local state.apply --out=json", CommandRisk.Red, "Modifica sistema según definiciones Salt"),
        new("salt.ping", "salt", "SaltStack", "Estado minion", "salt-call test.ping --out=json", CommandRisk.Green, "Comprueba Salt minion"),
        new("salt.keys", "salt", "SaltStack", "Claves Salt", "salt-key -L", CommandRisk.Yellow, "Muestra claves del master, si instalado"),
        new("salt.accept", "salt", "SaltStack", "Aceptar claves pendientes", "salt-key -A -y", CommandRisk.Red, "Confía en claves Salt pendientes"),
        new("monitor.icinga", "monitor", "Monitorización", "Estado Icinga2", "systemctl status icinga2 --no-pager", CommandRisk.Green, "Estado servicio Icinga"),
        new("monitor.icinga-config", "monitor", "Monitorización", "Validar configuración Icinga2", "icinga2 daemon -C", CommandRisk.Green, "Valida config sin activarla"),
        new("monitor.icinga-reload", "monitor", "Monitorización", "Recargar Icinga2", "systemctl reload icinga2", CommandRisk.Yellow, "Actualiza configuración en ejecución"),
        new("monitor.icinga-restart", "monitor", "Monitorización", "Reiniciar Icinga2", "systemctl restart icinga2", CommandRisk.Red, "Reinicia servicio monitorización"),
        new("monitor.journal", "monitor", "Monitorización", "Logs críticos", "journalctl -p err -n 50 --no-pager", CommandRisk.Green, "Últimos errores del journal"),
        new("monitor.tail-icinga", "monitor", "Monitorización", "Logs de Icinga2", "journalctl -u icinga2 -n 60 --no-pager", CommandRisk.Green, "Eventos recientes del servicio"),
        new("monitor.sensors", "monitor", "Monitorización", "Temperaturas y sensores", "sensors", CommandRisk.Green, "Requiere lm-sensors"),
        new("cloud.azversion", "cloud", "Nube Azure", "Azure CLI", "az version", CommandRisk.Green, "Versiones Azure CLI"),
        new("cloud.azaccount", "cloud", "Nube Azure", "Cuenta activa", "az account show", CommandRisk.Yellow, "Muestra suscripción y tenant de la sesión"),
        new("cloud.azlist", "cloud", "Nube Azure", "Suscripciones", "az account list --output table", CommandRisk.Yellow, "Enumera metadatos de suscripciones"),
        new("cloud.azgroups", "cloud", "Nube Azure", "Grupos de recursos", "az group list --output table", CommandRisk.Green, "Recursos del tenant autorizado"),
        new("cloud.azvms", "cloud", "Nube Azure", "Máquinas virtuales", "az vm list --output table", CommandRisk.Green, "Lista máquinas virtuales"),
        new("cloud.azlogout", "cloud", "Nube Azure", "Cerrar sesión de Azure", "az logout", CommandRisk.Yellow, "Cierra la sesión de Azure CLI"),
        new("cloud.azlogin", "cloud", "Nube Azure", "Autenticar Azure", "az login --use-device-code", CommandRisk.Yellow, "Inicia login interactivo; puede no funcionar en SSH sin TTY"),
        new("logs.journal", "logs", "Logs y auditoría", "Logs recientes", "journalctl -n 100 --no-pager", CommandRisk.Green, "Últimos eventos del sistema"),
        new("logs.boots", "logs", "Logs y auditoría", "Inicios del sistema", "journalctl --list-boots --no-pager", CommandRisk.Green, "Registro de reinicios"),
        new("logs.ssh", "logs", "Logs y auditoría", "Fallos SSH", "journalctl -u ssh -n 100 --no-pager", CommandRisk.Green, "Historial SSH"),
        new("logs.syslog", "logs", "Logs y auditoría", "Syslog", "tail -n 100 /var/log/syslog", CommandRisk.Green, "Puede revelar datos operativos"),
        new("logs.audit", "logs", "Logs y auditoría", "Eventos de autenticación", "tail -n 80 /var/log/auth.log", CommandRisk.Yellow, "Logs con información sensible"),
        new("logs.rotate", "logs", "Logs y auditoría", "Forzar rotación de logs", "logrotate -f /etc/logrotate.conf", CommandRisk.Red, "Fuerza rotación del sistema"),
        new("git.version", "git", "Git", "Versión de Git", "git --version", CommandRisk.Green, "Comprueba instalación"),
        new("git.status", "git", "Git", "Estado del repositorio", "git status --short", CommandRisk.Green, "Estado en el directorio remoto"),
        new("git.log", "git", "Git", "Historial reciente", "git log -5 --oneline", CommandRisk.Green, "Últimos commits"),
        new("git.branches", "git", "Git", "Ramas locales", "git branch --list", CommandRisk.Green, "Ramas del repositorio"),
        new("git.fetch", "git", "Git", "Descargar refs remotas", "git fetch --all --prune", CommandRisk.Yellow, "Actualiza refs locales de Git"),
        new("git.pull", "git", "Git", "Actualizar rama", "git pull --ff-only", CommandRisk.Red, "Modifica árbol de trabajo remoto"),
        new("git.reset", "git", "Git", "Descartar cambios locales", "git reset --hard HEAD", CommandRisk.Red, "DESTRUCTIVO: descarta cambios sin confirmar"),
        new("backup.rsyncdry", "backup", "Copias y compresión", "Simulación copia local", "rsync -avn /etc/ /tmp/anticli-etc-backup/", CommandRisk.Green, "Simula sincronización"),
        new("backup.tarlist", "backup", "Copias y compresión", "Listar tar de ejemplo", "tar -tf /tmp/backup.tar", CommandRisk.Green, "Lista archivo, si existe"),
        new("backup.rsync", "backup", "Copias y compresión", "Copiar configuración", "rsync -av /etc/ /tmp/anticli-etc-backup/", CommandRisk.Yellow, "Copia archivos en /tmp"),
        new("backup.tar", "backup", "Copias y compresión", "Comprimir configuración", "tar -czf /tmp/anticli-etc-backup.tar.gz /etc", CommandRisk.Yellow, "Escribe archivo comprimido; puede contener secretos"),
        new("backup.sync", "backup", "Copias y compresión", "Sincronizar /tmp", "rsync -av --delete /tmp/a/ /tmp/b/", CommandRisk.Red, "DESTRUCTIVO en destino: --delete elimina archivos"),
    ];

    public static IReadOnlyList<AdminCommand> For(TargetPlatform platform) =>
        platform == TargetPlatform.Windows ? [] : Linux;

    public static AdminCommand? Find(TargetPlatform platform, string command) =>
        For(platform).FirstOrDefault(x => string.Equals(x.Command, command, StringComparison.Ordinal));

    /// <summary>
    /// A conservative advisory classifier. Unknown or composite commands are red.
    /// It does not provide shell sandboxing, command isolation or RBAC.
    /// </summary>
    public static CommandRisk Classify(TargetPlatform platform, string? command)
    {
        var text = (command ?? "").Trim();
        if (text.Length == 0) return CommandRisk.Green;
        var known = Find(platform, text);
        if (known is not null) return known.Risk;

        // The conservative default avoids presenting arbitrary shell expressions as safe.
        return CommandRisk.Red;
    }

    public static string Dot(CommandRisk risk) => risk switch
    {
        CommandRisk.Green => "🟢",
        CommandRisk.Yellow => "🟡",
        _ => "🔴"
    };

    public static string RiskName(CommandRisk risk) => risk switch
    {
        CommandRisk.Green => "Bajo / lectura",
        CommandRisk.Yellow => "Precaución / cambios limitados o salida sensible",
        _ => "Alto / cambios importantes o comando no catalogado"
    };
}
