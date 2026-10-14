namespace SolarisPKN.AntiCli.Modules;

public sealed record ReadOnlyAction(string Id, string Label, string Command)
{
    public override string ToString() => Label;
}

public static class DebianReadOnlyActions
{
    // Fixed, reviewed commands. No unsanitized user input is interpolated.
    public static IReadOnlyList<ReadOnlyAction> All { get; } =
    [
        new("os", "Detectar distribución Linux", "cat /etc/os-release"),
        new("kernel", "Kernel y arquitectura", "uname -a"),
        new("uptime", "Tiempo encendido", "uptime"),
        new("memory", "Memoria", "free -m"),
        new("disks", "Uso de discos", "df -h"),
        new("services", "Servicios systemd", "systemctl list-units --type=service --all --no-pager"),
        new("addresses", "Direcciones de red", "ip -brief address"),
        new("routes", "Rutas IPv4", "ip -4 route show"),
        new("apt", "Paquetes instalados (muestra)", "dpkg-query -W")
    ];
}
