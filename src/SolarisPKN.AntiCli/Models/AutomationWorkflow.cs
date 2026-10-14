using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SolarisPKN.AntiCli.Models;

/// <summary>
/// Portable, data-only description of a multistep administrative workflow.
/// The workflow DOES NOT execute commands. Script generation and remote
/// execution live in separate, explicit user-triggered components.
/// </summary>
public sealed record AutomationStep(
    string Id, string Description, string CommandTemplate, bool Enabled = true);

public enum AutomationOrder { PerRow, PerStep }

public sealed record AutomationWorkflow(
    int SchemaVersion,
    string Name,
    IReadOnlyList<AutomationStep> Steps,
    IReadOnlyDictionary<string, string> Constants,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    AutomationOrder Order = AutomationOrder.PerRow,
    bool StopOnError = true);

public static class AutomationWorkflowFile
{
    private static readonly Regex VariableName = new(
        @"\A[A-Za-z_][A-Za-z0-9_]{0,39}\z", RegexOptions.Compiled);
    private static readonly Regex TemplateValue = new(
        @"\{\{(?<name>[A-Za-z_][A-Za-z0-9_]{0,39})\}\}", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AutomationWorkflow Parse(string json)
    {
        if (json.Length > 300_000)
            throw new ArgumentException("La definición es demasiado extensa.");
        var workflow = JsonSerializer.Deserialize<AutomationWorkflow>(json, Options)
            ?? throw new InvalidDataException("El flujo está vacío.");
        Validate(workflow);
        return workflow;
    }

    public static string Serialize(AutomationWorkflow workflow)
    {
        Validate(workflow);
        return JsonSerializer.Serialize(workflow, Options);
    }

    public static void Validate(AutomationWorkflow workflow)
    {
        if (workflow.SchemaVersion != 1 || string.IsNullOrWhiteSpace(workflow.Name) ||
            workflow.Name.Length > 100)
            throw new InvalidDataException("El flujo necesita SchemaVersion 1 y nombre válido.");
        if (workflow.Steps.Count is < 1 or > 30 || workflow.Rows.Count is < 1 or > 100 ||
            workflow.Steps.Count * workflow.Rows.Count > 2_000)
            throw new InvalidDataException("Límites: 30 pasos, 100 filas y 2.000 operaciones.");
        if (workflow.Steps.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != workflow.Steps.Count)
            throw new InvalidDataException("Los IDs de pasos deben ser únicos.");
        if (workflow.Constants.Count > 40)
            throw new InvalidDataException("Demasiadas variables constantes.");
        foreach (var variable in workflow.Constants.Keys) CheckName(variable);
        foreach (var row in workflow.Rows)
        {
            if (row.Count > 40) throw new InvalidDataException("Máximo 40 variables por fila.");
            foreach (var name in row.Keys) CheckName(name);
            foreach (var value in row.Values) CheckValue(value);
        }
        foreach (var value in workflow.Constants.Values) CheckValue(value);
        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id) || string.IsNullOrWhiteSpace(step.CommandTemplate) ||
                step.CommandTemplate.Length > 3000 || step.CommandTemplate.Any(c => c is '\r' or '\n' or '\0'))
                throw new InvalidDataException("Paso sin comando o con texto demasiado largo.");
            foreach (Match match in TemplateValue.Matches(step.CommandTemplate))
            {
                var name = match.Groups["name"].Value;
                if (!workflow.Constants.ContainsKey(name) &&
                    workflow.Rows.Any(row => !row.ContainsKey(name)))
                    throw new InvalidDataException("Variable sin valor: " + name);
            }
        }
    }

    private static void CheckName(string value)
    {
        if (!VariableName.IsMatch(value))
            throw new InvalidDataException("Nombre de variable inválido: " + value);
    }

    private static void CheckValue(string value)
    {
        if (value.Length > 2048 || value.Any(c => c is '\r' or '\n' or '\0'))
            throw new InvalidDataException("Valor de variable inválido o demasiado largo.");
    }
}
