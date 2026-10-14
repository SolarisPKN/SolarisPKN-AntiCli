using System.Text;
using System.Text.RegularExpressions;
using SolarisPKN.AntiCli.Models;

namespace SolarisPKN.AntiCli.Modules;

/// <summary>
/// Exports a REVIEWABLE POSIX shell script from any approved data-only workflow.
/// Never opens SSH, installs packages, schedules tasks, or runs the script.
/// </summary>
public static class AutomationScriptExporter
{
    private static readonly Regex Token = new(
        @"\{\{(?<name>[A-Za-z_][A-Za-z0-9_]{0,39})\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Generate(AutomationWorkflow workflow)
    {
        AutomationWorkflowFile.Validate(workflow);
        var result = new StringBuilder("#!/bin/sh\n");
        result.AppendLine("# Exportado desde SolarisPKN AntiCLI. Revisar antes de ejecutar.");
        result.AppendLine("# Las plantillas pueden contener comandos con efectos sobre el servidor.");
        result.AppendLine(workflow.StopOnError ? "set -eu" : "set -u");
        var activeSteps = workflow.Steps.Where(x => x.Enabled).ToArray();
        if (activeSteps.Length == 0)
            throw new InvalidDataException("Activá al menos un paso del flujo.");

        void AppendStep(int rowIndex, int stepIndex)
        {
            var row = workflow.Rows[rowIndex];
            var step = activeSteps[stepIndex];
            var command = Token.Replace(step.CommandTemplate, m =>
            {
                var name = m.Groups["name"].Value;
                var value = row.TryGetValue(name, out var fromRow)
                    ? fromRow : workflow.Constants[name];
                return GuidedCommandCatalog.ShellQuote(value);
            });
            result.Append("# Fila ").Append(rowIndex + 1)
                .Append(", paso ").Append(stepIndex + 1).Append(": ")
                .AppendLine(step.Description.Replace("\n", " ", StringComparison.Ordinal));
            if (workflow.StopOnError)
                result.AppendLine(command);
            else
                result.AppendLine("{ " + command + "; } || printf '%s\\n' " +
                    GuidedCommandCatalog.ShellQuote(
                        "[AntiCLI] Error en paso " + (stepIndex + 1) +
                        ", fila " + (rowIndex + 1)) + " >&2");
        }

        if (workflow.Order == AutomationOrder.PerStep)
        {
            for (var step = 0; step < activeSteps.Length; step++)
                for (var row = 0; row < workflow.Rows.Count; row++)
                    AppendStep(row, step);
        }
        else
        {
            for (var row = 0; row < workflow.Rows.Count; row++)
                for (var step = 0; step < activeSteps.Length; step++)
                    AppendStep(row, step);
        }
        if (result.Length > 350000)
            throw new ArgumentException("El script supera 350 KB; dividí el flujo.");
        return result.ToString();
    }
}
