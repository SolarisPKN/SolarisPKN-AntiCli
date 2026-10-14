using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

public static class PlatformReadOnlyActions
{
    public static IReadOnlyList<ReadOnlyAction> For(TargetPlatform platform) =>
        platform switch
        {
            TargetPlatform.Debian or TargetPlatform.Ubuntu => DebianReadOnlyActions.All,
            TargetPlatform.Windows =>
            [
                new("os", "Versión de Windows", "cmd /c ver"),
                new("user", "Usuario conectado", "whoami"),
                new("host", "Nombre del equipo", "hostname"),
                new("network", "Configuración de red", "ipconfig /all"),
                new("process", "Procesos activos", "tasklist"),
                new("services", "Servicios instalados", "sc.exe query"),
                new("ports", "Conexiones de red", "netstat -ano")
            ],
            TargetPlatform.OtherLinux =>
            [
                new("kernel", "Kernel y arquitectura", "uname -a"),
                new("uptime", "Tiempo de actividad", "uptime"),
                new("memory", "Uso de memoria", "free -m"),
                new("disks", "Ocupación de discos", "df -h"),
                new("network", "Interfaces de red", "ip -brief address")
            ],
            _ => []
        };
}
