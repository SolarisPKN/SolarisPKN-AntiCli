using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace SolarisPKN.AntiCli;

/// <summary>Reusable row editor for the data model's flags and operands.</summary>
public partial class SessionView
{
    private sealed record EditableRow(StackPanel Panel, TextBox[] Cells);
    private readonly List<EditableRow> _flagEditRows = [];
    private readonly List<EditableRow> _operandEditRows = [];
    private bool _loadingModuleRows;

    private void ModuleEditorAddFlagClicked(object? sender, RoutedEventArgs e) =>
        AddModuleEditRow(true, ["--nueva-opcion", "Nombre de opción", "Qué hace", "None", "", "false"]);

    private void ModuleEditorAddOperandClicked(object? sender, RoutedEventArgs e) =>
        AddModuleEditRow(false, ["argumento", "Nombre del campo", "Qué necesita", "Text", "true", ""]);

    private void ModuleEditorRebuildRowsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            RebuildModuleEditRows();
            ModuleEditorStatus.Text = "Editor visual sincronizado con el texto actual.";
        }
        catch (Exception ex) { ModuleEditorStatus.Text = "No se pudieron cargar las filas: " + ex.Message; }
    }

    private void RebuildModuleEditRows()
    {
        _loadingModuleRows = true;
        try
        {
            _flagEditRows.Clear();
            _operandEditRows.Clear();
            ModuleEditorFlagRows.Children.Clear();
            ModuleEditorOperandRows.Children.Clear();
            foreach (var row in ParseEditorRows(ModuleEditorFlags.Text, "flags"))
                AddModuleEditRow(true, row);
            foreach (var row in ParseEditorRows(ModuleEditorOperands.Text, "parámetros"))
                AddModuleEditRow(false, row);
        }
        finally { _loadingModuleRows = false; }
    }

    private void AddModuleEditRow(bool isFlag, string[] values)
    {
        var target = isFlag ? ModuleEditorFlagRows : ModuleEditorOperandRows;
        var list = isFlag ? _flagEditRows : _operandEditRows;
        var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        var captions = isFlag
            ? new[] { "Token", "Etiqueta", "Descripción", "Tipo", "Valor inicial", "Por defecto" }
            : new[] { "Nombre", "Etiqueta", "Descripción", "Tipo", "Requerido", "Valor inicial" };
        var widths = new double[] { 118, 168, 218, 117, 120, 92 };
        var fields = new TextBox[6];
        for (var i = 0; i < fields.Length; i++)
        {
            var input = new TextBox
            {
                Text = values[i],
                Watermark = captions[i],
                Width = widths[i]
            };
            ToolTip.SetTip(input, captions[i] +
                (i == 3 ? " · Text, None, AbsolutePath, Account, PackageList..." : ""));
            input.TextChanged += (_, _) => SyncModuleRowsToText();
            fields[i] = input;
            rowPanel.Children.Add(input);
        }
        var remove = new Button { Content = "✕", MinWidth = 30 };
        remove.Click += (_, _) =>
        {
            list.RemoveAll(x => ReferenceEquals(x.Panel, rowPanel));
            target.Children.Remove(rowPanel);
            SyncModuleRowsToText();
        };
        rowPanel.Children.Add(remove);
        list.Add(new EditableRow(rowPanel, fields));
        target.Children.Add(rowPanel);
        SyncModuleRowsToText();
    }

    private void SyncModuleRowsToText()
    {
        if (_loadingModuleRows) return;
        ModuleEditorFlags.Text = string.Join("\n", _flagEditRows.Select(row =>
            string.Join(" | ", row.Cells.Select(cell => (cell.Text ?? "").Replace("|", "/", StringComparison.Ordinal)))));
        ModuleEditorOperands.Text = string.Join("\n", _operandEditRows.Select(row =>
            string.Join(" | ", row.Cells.Select(cell => (cell.Text ?? "").Replace("|", "/", StringComparison.Ordinal)))));
    }
}
