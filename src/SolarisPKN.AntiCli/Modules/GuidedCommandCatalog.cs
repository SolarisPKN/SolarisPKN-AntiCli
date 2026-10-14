using System.Globalization;
using System.Text.RegularExpressions;

namespace SolarisPKN.AntiCli.Modules;

public enum GuidedValueKind { None, Text, UserGroup, AbsolutePath, PositiveId }

public sealed record GuidedFlag(
    string Token, string LabelEs, string DescriptionEs,
    GuidedValueKind ValueKind = GuidedValueKind.None,
    string Placeholder = "", bool SelectedByDefault = false,
    string InitialValue = "");

public sealed record GuidedCommand(
    string Id, string Executable, string TitleEs, string ExplanationEs,
    string SearchTerms, CommandRisk Risk, bool SupportsBatch,
    IReadOnlyList<GuidedFlag> Flags);

public sealed record GuidedFlagChoice(string Token, string Value);

public sealed record GuidedPlan(string Command, CommandRisk Risk, int Operations, string Note);

/// <summary>
/// An extensible command schema: an executable, ordered flags (with types),
/// named operands and optional batch expansion. Values are validated and shell
/// quoted before being inserted into a reviewed template.
/// </summary>
public static class GuidedCommandCatalog
{
    private static readonly Regex AccountName = new(
        @"^[a-z_][a-z0-9_-]{0,27}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex GroupName = new(
        @"^[a-z_][a-z0-9_-]{0,31}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<GuidedCommand> All { get; } =
    [
        new("uname", "uname", "Información del sistema",
            "Mostrá la información del kernel y el equipo. Seleccioná las opciones que quieras; GNU uname ordena sus campos internamente aunque cambies el orden de las flags.",
            "sistema kernel arquitectura version equipo hostname nombre hardware uname ver sistema información linux",
            CommandRisk.Green, false,
            [
                new("-a", "Toda la información", "Muestra todos los campos disponibles, excepto los desconocidos."),
                new("-s", "Nombre del kernel", "Muestra el nombre del núcleo del sistema."),
                new("-n", "Nombre del equipo", "Muestra el nombre del servidor dentro de la red."),
                new("-r", "Versión de lanzamiento del kernel", "Muestra el release del kernel."),
                new("-v", "Compilación del kernel", "Muestra la versión completa de compilación."),
                new("-m", "Arquitectura de la máquina", "Por ejemplo x86_64 o aarch64."),
                new("-p", "Tipo de procesador", "Puede no estar disponible en todas las plataformas."),
                new("-i", "Plataforma de hardware", "Puede no estar disponible en todas las plataformas."),
                new("-o", "Sistema operativo", "Muestra el identificador del sistema operativo."),
                new("--help", "Mostrar ayuda", "Imprime el manual breve y finaliza."),
                new("--version", "Mostrar versión", "Imprime la versión de uname y finaliza.")
            ]),
        new("adduser", "adduser", "Crear usuario",
            "Crea una cuenta en Debian/Ubuntu con adduser. Para usarla sin terminal interactiva, elegí deshabilitar contraseña o inicio de sesión. No se establece contraseña ni se generan credenciales automáticamente.",
            "crear usuario alta cuenta alumnos alumnos lote masivo batch adduser useradd registrar cuentas",
            CommandRisk.Red, true,
            [
                new("--disabled-password", "Sin contraseña inicial (acceso por clave SSH)", "No solicita contraseña. La cuenta puede usar otros métodos autorizados, como claves SSH.", SelectedByDefault: true),
                new("--disabled-login", "Desactivar inicio de sesión", "Crea una cuenta sin permitir iniciar sesión hasta que se configure.", SelectedByDefault: false),
                new("--gecos", "Nombre completo o comentario", "Información de la cuenta. Con un valor vacío evita las preguntas interactivas.", GuidedValueKind.Text, "Ej.: Alumno de informática", true, ""),
                new("--shell", "Consola de inicio", "Shell que usará la cuenta, p. ej. /bin/bash.", GuidedValueKind.AbsolutePath, "/bin/bash"),
                new("--home", "Directorio personal (solo individual)", "Ruta absoluta para el directorio personal; en lotes cada cuenta necesita uno distinto.", GuidedValueKind.AbsolutePath, "/home/alumno"),
                new("--ingroup", "Grupo principal", "Grupo que ya debe existir en el servidor.", GuidedValueKind.UserGroup, "estudiantes"),
                new("--uid", "UID personalizado (solo individual)", "Identificador numérico único de la cuenta.", GuidedValueKind.PositiveId, "1050"),
                new("--no-create-home", "No crear directorio personal", "Evita crear el directorio del usuario."),
                new("--system", "Cuenta de sistema", "Crea un usuario de sistema en lugar de una cuenta humana."),
                new("--add_extra_groups", "Grupos adicionales de adduser.conf", "Añade los grupos predeterminados definidos en adduser.conf, no una lista arbitraria."),
                new("--quiet", "Reducir mensajes", "Oculta mensajes informativos y deja advertencias y errores.")
            ])
    ];

    public static GuidedCommand? Find(string id) =>
        All.FirstOrDefault(x => x.Id == id);

    public static IReadOnlyList<GuidedCommand> Search(string? text)
    {
        var terms = (text ?? "").Trim().ToLowerInvariant();
        if (terms.Length == 0) return All;
        var words = terms.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return All.Where(x =>
        {
            var corpus = (x.TitleEs + " " + x.SearchTerms + " " + x.Executable +
                " " + x.ExplanationEs).ToLowerInvariant();
            return words.All(corpus.Contains);
        }).ToArray();
    }

    public static GuidedPlan Build(
        GuidedCommand definition, IReadOnlyList<GuidedFlagChoice> orderedChoices,
        string? accountName = null, int quantity = 1,
        int firstNumber = 1, int minimumDigits = 2)
    {
        if (quantity is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Permitimos entre 1 y 100 cuentas por lote.");
        if (firstNumber is < 0 or > 999999 || firstNumber + quantity > 1000000)
            throw new ArgumentOutOfRangeException(nameof(firstNumber), "El número inicial es inválido.");
        if (minimumDigits is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(minimumDigits), "Los dígitos deben estar entre 1 y 8.");
        if (orderedChoices.Select(x => x.Token).Distinct(StringComparer.Ordinal).Count() != orderedChoices.Count)
            throw new ArgumentException("No se puede seleccionar dos veces la misma flag.");

        var options = new List<string>();
        foreach (var choice in orderedChoices)
        {
            var flag = definition.Flags.FirstOrDefault(f => f.Token == choice.Token)
                ?? throw new ArgumentException("Flag no documentada: " + choice.Token);
            if (flag.ValueKind == GuidedValueKind.None)
                options.Add(flag.Token);
            else
            {
                ValidateValue(flag.ValueKind, choice.Value);
                options.Add(flag.Token + " " + ShellQuote(choice.Value));
            }
        }

        if (definition.Id == "uname")
        {
            if (quantity != 1)
                throw new ArgumentException("uname no admite generación de usuarios por lotes.");
            if ((orderedChoices.Any(x => x.Token is "--help" or "--version")) && orderedChoices.Count > 1)
                throw new ArgumentException("--help y --version deben ejecutarse sin otras flags.");
            return new GuidedPlan(
                "uname" + (options.Count > 0 ? " " + string.Join(" ", options) : ""),
                CommandRisk.Green, 1,
                "GNU uname muestra los campos en un orden fijo aunque se reordenen las flags.");
        }

        if (definition.Id != "adduser")
            throw new NotSupportedException("Constructor guiado todavía no disponible para " + definition.Id);
        var name = (accountName ?? "").Trim();
        if (!AccountName.IsMatch(name) || name.StartsWith('-'))
            throw new ArgumentException("Usá un nombre de usuario Linux simple: letras minúsculas, números, guion o guion bajo (máximo 28 caracteres).");
        if (!orderedChoices.Any(x => x.Token is "--disabled-password" or "--disabled-login"))
            throw new ArgumentException("Para evitar un prompt interactivo, elegí «Sin contraseña inicial» o «Desactivar inicio de sesión».");
        if (orderedChoices.Any(x => x.Token == "--disabled-password") &&
            orderedChoices.Any(x => x.Token == "--disabled-login"))
            throw new ArgumentException("Elegí solamente una de las dos opciones de inicio de sesión.");
        // Prevent GECOS interactive prompts even if the operator did not explicitly select it.
        if (!orderedChoices.Any(x => x.Token == "--gecos"))
            options.Add("--gecos ''");
        if (quantity > 1 && orderedChoices.Any(x => x.Token is "--uid" or "--home"))
            throw new ArgumentException("El UID y el directorio personal personalizados solo están disponibles para una cuenta individual.");
        if (quantity == 1)
        {
            return new GuidedPlan("adduser " + string.Join(" ", options) + " -- " + ShellQuote(name),
                CommandRisk.Red, 1,
                "Se creará una cuenta. La contraseña no se genera ni queda configurada automáticamente.");
        }

        var suffixes = Enumerable.Range(firstNumber, quantity)
            .Select(n => n.ToString(CultureInfo.InvariantCulture)
                .PadLeft(minimumDigits, '0'))
            .ToArray();
        if (suffixes.Any(s => name.Length + s.Length > 32))
            throw new ArgumentException("El prefijo y los sufijos superan el largo permitido para el nombre de usuario.");
        // Prefix is strict ASCII and suffix values are generated locally, not supplied as shell.
        // One remote invocation keeps the workflow fast; break stops the batch at the first failure.
        var script = "for suffix in " + string.Join(" ", suffixes) +
            "; do adduser " + string.Join(" ", options) +
            " -- \"" + name + "${suffix}\" || break; done";
        return new GuidedPlan(script, CommandRisk.Red, quantity,
            "Lote de " + quantity + " cuentas: " + name + suffixes[0] + " … " +
            name + suffixes[^1] + ". Se detiene ante el primer error.");
    }

    private static void ValidateValue(GuidedValueKind kind, string? value)
    {
        if (value is null || value.Length > 160 || value.Any(char.IsControl))
            throw new ArgumentException("Ingresá un valor de opción válido (máximo 160 caracteres).");
        if (kind == GuidedValueKind.Text) return; // Empty GECOS is allowed.
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("La flag elegida necesita un valor.");
        if (kind == GuidedValueKind.UserGroup && !GroupName.IsMatch(value))
            throw new ArgumentException("Nombre de grupo Linux inválido.");
        if (kind == GuidedValueKind.AbsolutePath &&
            (!value.StartsWith('/', StringComparison.Ordinal) || value.Contains("..", StringComparison.Ordinal)))
            throw new ArgumentException("La ruta debe ser absoluta y no contener segmentos '..'.");
        if (kind == GuidedValueKind.PositiveId &&
            (!int.TryParse(value, CultureInfo.InvariantCulture, out var uid) || uid is < 1 or > 60000))
            throw new ArgumentException("El UID debe ser un número entre 1 y 60000.");
    }

    private static string ShellQuote(string text) =>
        "'" + text.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
}
