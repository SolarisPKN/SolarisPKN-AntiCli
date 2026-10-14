using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarisPKN.AntiCli.Models;

/// <summary>
/// A complete editable project, not just the linear automation step list.
/// Version-1 workflow-only JSON files remain supported when loading.
/// </summary>
public sealed record AutomationProject(
    int ProjectVersion,
    AutomationWorkflow Workflow,
    AutomationGraph? Diagram = null);

public static class AutomationProjectFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(AutomationProject project)
    {
        Validate(project);
        return JsonSerializer.Serialize(project, Options);
    }

    public static AutomationProject Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 700_000)
            throw new InvalidDataException("Proyecto JSON vacío o demasiado grande.");

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("El proyecto debe ser un objeto JSON.");

        // Legacy projects contain AutomationWorkflow properties directly.
        // Do not mistake an invalid new envelope for a legacy workflow.
        if (!document.RootElement.TryGetProperty(nameof(AutomationProject.ProjectVersion), out _))
        {
            var workflow = AutomationWorkflowFile.Parse(json);
            return new AutomationProject(1, workflow);
        }

        var project = JsonSerializer.Deserialize<AutomationProject>(json, Options)
            ?? throw new InvalidDataException("Proyecto JSON vacío.");
        Validate(project);
        return project;
    }

    public static void Validate(AutomationProject project)
    {
        if (project.ProjectVersion != 1 || project.Workflow is null)
            throw new InvalidDataException("Se requiere ProjectVersion 1 y un flujo válido.");
        AutomationWorkflowFile.Validate(project.Workflow);
        if (project.Diagram is not null)
            AutomationGraphFile.Validate(project.Diagram);
    }
}
