using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace SolarisPKN.AntiCli;

/// <summary>
/// Session-local SSH operation journal. Nothing gets uploaded or written to
/// disk unless the operator explicitly chooses Export. The editor permits
/// redacting secrets before sharing with teammates.
/// </summary>
public partial class SessionView
{
    private readonly List<SshCommandHistoryEntry> _commandHistory = [];

    private sealed record SshCommandHistoryEntry(
        DateTimeOffset StartedUtc, string Host, int Port, string Command,
        bool UsedSudo, int? ExitCode, string StdOut, string StdErr, string? Error)
    {
        public override string ToString()
        {
            var status = ExitCode.HasValue ? "exit=" + ExitCode.Value : "ERROR";
            return StartedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") +
                " · " + Host + ":" + Port + " · " + status + " · " +
                (Command.Length > 95 ? Command[..95] + "…" : Command);
        }
    }

    private void RecordCommand(
        DateTimeOffset started, string host, int port, string executedCommand,
        bool sudo, int? exitCode, string stdout, string stderr, string? error = null)
    {
        _commandHistory.Add(new SshCommandHistoryEntry(
            started, host, port, executedCommand, sudo,
            exitCode, stdout, stderr, error));
        RefreshHistory();
    }

    private static string FormatHistory(IEnumerable<SshCommandHistoryEntry> entries)
    {
        var result = new StringBuilder();
        result.AppendLine("# AntiCLI · Historial SSH");
        result.AppendLine("# Revisá las líneas antes de compartir; algunas salidas contienen datos sensibles.");
        result.AppendLine();
        foreach (var item in entries)
        {
            result.AppendLine("------------------------------------------------------------");
            result.AppendLine("Fecha UTC: " + item.StartedUtc.ToString("O"));
            result.AppendLine("Servidor: " + item.Host + ":" + item.Port);
            result.AppendLine("sudo: " + (item.UsedSudo ? "sí" : "no"));
            result.AppendLine("Comando: " + item.Command);
            result.AppendLine("Código de salida: " + (item.ExitCode?.ToString() ?? "sin código"));
            if (item.Error is not null) result.AppendLine("Error: " + item.Error);
            if (!string.IsNullOrWhiteSpace(item.StdOut))
                result.AppendLine("STDOUT:\n" + item.StdOut);
            if (!string.IsNullOrWhiteSpace(item.StdErr))
                result.AppendLine("STDERR:\n" + item.StdErr);
        }
        return result.ToString();
    }

    private void RefreshHistory()
    {
        // Preserve the operator's redactions until the next executed command.
        // Rebuilding the log is intentional and identified in the help text.
        CommandHistoryList.ItemsSource = null;
        CommandHistoryList.ItemsSource = _commandHistory.ToArray();
        CommandHistoryLog.Text = FormatHistory(_commandHistory);
    }

    private async void CopyCommandClicked(object? sender, RoutedEventArgs e) =>
        await CopyToClipboardAsync(AssistedCommand.Text ?? "", "comando");

    private async void PasteCommandClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return;
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                AssistedCommand.Text = text.TrimEnd('\r', '\n');
                AssistedCommand.CaretIndex = AssistedCommand.Text.Length;
                ExecutionStatus.Text = "Texto pegado. Revisá el comando y su riesgo antes de ejecutarlo.";
            }
        }
        catch (Exception ex) { ExecutionStatus.Text = "No se pudo pegar: " + ex.Message; }
    }

    private async void CopyOutputClicked(object? sender, RoutedEventArgs e) =>
        await CopyToClipboardAsync(Output.Text ?? "", "salida");

    private async void CopyHistoryCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (CommandHistoryList.SelectedItem is SshCommandHistoryEntry entry)
            await CopyToClipboardAsync(entry.Command, "comando del historial");
    }

    private void ReuseHistoryCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (CommandHistoryList.SelectedItem is not SshCommandHistoryEntry entry) return;
        AssistedCommand.Text = entry.Command;
        AssistedCommand.Focus();
        ExecutionStatus.Text = "Comando recuperado; revisá el riesgo antes de ejecutarlo.";
    }

    private void CommandHistorySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (CommandHistoryList.SelectedItem is not SshCommandHistoryEntry entry) return;
        ExecutionStatus.Text = entry.ToString();
    }

    private async void CopyLogClicked(object? sender, RoutedEventArgs e) =>
        await CopyToClipboardAsync(CommandHistoryLog.Text ?? "", "registro SSH");

    private async Task CopyToClipboardAsync(string text, string what)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
                throw new InvalidOperationException("El portapapeles no está disponible.");
            await clipboard.SetTextAsync(text);
            ExecutionStatus.Text = "Copiado al portapapeles: " + what + ".";
        }
        catch (Exception ex) { ExecutionStatus.Text = "No se pudo copiar: " + ex.Message; }
    }

    private async void ExportLogClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider
                ?? throw new InvalidOperationException("El selector de archivos no está disponible.");
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar historial SSH revisado",
                SuggestedFileName = "AntiCLI-ssh-log-" +
                    DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss") + ".txt",
                FileTypeChoices =
                [
                    new FilePickerFileType("Texto") { Patterns = ["*.txt"], MimeTypes = ["text/plain"] }
                ]
            });
            if (file is null) return;
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek) stream.SetLength(0);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(CommandHistoryLog.Text ?? "");
            await writer.FlushAsync();
            ExecutionStatus.Text = "Registro exportado. Verificá que no contenga secretos antes de compartir.";
        }
        catch (Exception ex) { ExecutionStatus.Text = "No se pudo exportar: " + ex.Message; }
    }

    private void ClearHistoryClicked(object? sender, RoutedEventArgs e)
    {
        _commandHistory.Clear();
        CommandHistoryList.ItemsSource = null;
        CommandHistoryLog.Text = "";
        ExecutionStatus.Text = "Historial local de esta sesión limpiado.";
    }
}
