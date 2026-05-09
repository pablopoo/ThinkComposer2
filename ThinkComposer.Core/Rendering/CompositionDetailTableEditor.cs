namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailTableEditor
{
    public static CompositionDetailTableSnapshot AppendPastedRows(
        CompositionDetailTableSnapshot table,
        string? text)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var pastedRows = CompositionDetailTablePaste.ParseRows(text);
        if (pastedRows.Count == 0)
        {
            return table;
        }

        var columnCount = Math.Max(
            Math.Max(table.Columns.Count, pastedRows.Max(row => row.Count)),
            1);
        var columns = EnsureColumns(table.Columns, columnCount);
        var rows = table.Rows
            .Select(row => NormalizeRow(row, columnCount))
            .Concat(pastedRows.Select(row => NormalizeRow(row, columnCount)))
            .ToArray();

        return new CompositionDetailTableSnapshot(columns, rows);
    }

    public static CompositionDetailTableSnapshot SortRows(
        CompositionDetailTableSnapshot table,
        int columnIndex,
        CompositionDetailTableSortDirection direction)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var orderedRows = table.Rows
            .Select((row, index) => new TableRowSortEntry(row, index, GetCell(row, columnIndex)))
            .ToArray();

        orderedRows = direction == CompositionDetailTableSortDirection.Descending
            ? orderedRows
                .OrderByDescending(entry => entry.SortValue, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Index)
                .ToArray()
            : orderedRows
                .OrderBy(entry => entry.SortValue, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Index)
                .ToArray();

        return new CompositionDetailTableSnapshot(
            table.Columns,
            orderedRows.Select(entry => entry.Row.ToArray()).ToArray());
    }

    public static CompositionDetailTableSnapshot DuplicateRow(
        CompositionDetailTableSnapshot table,
        int rowIndex)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (!IsValidRowIndex(table, rowIndex))
        {
            return table;
        }

        var rows = table.Rows.Select(row => row.ToArray()).ToList();
        rows.Insert(rowIndex + 1, table.Rows[rowIndex].ToArray());

        return new CompositionDetailTableSnapshot(table.Columns, rows);
    }

    public static CompositionDetailTableSnapshot MoveRow(
        CompositionDetailTableSnapshot table,
        int rowIndex,
        int offset)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var targetIndex = rowIndex + offset;
        if (!IsValidRowIndex(table, rowIndex) ||
            targetIndex < 0 ||
            targetIndex >= table.Rows.Count ||
            offset == 0)
        {
            return table;
        }

        var rows = table.Rows.Select(row => row.ToArray()).ToList();
        var row = rows[rowIndex];
        rows.RemoveAt(rowIndex);
        rows.Insert(targetIndex, row);

        return new CompositionDetailTableSnapshot(table.Columns, rows);
    }

    public static CompositionDetailTableSnapshot ClearRow(
        CompositionDetailTableSnapshot table,
        int rowIndex)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (!IsValidRowIndex(table, rowIndex))
        {
            return table;
        }

        var columnCount = Math.Max(table.Columns.Count, table.Rows[rowIndex].Count);
        var rows = table.Rows.Select(row => row.ToArray()).ToList();
        rows[rowIndex] = Enumerable.Repeat(string.Empty, columnCount).ToArray();

        return new CompositionDetailTableSnapshot(table.Columns, rows);
    }

    private static IReadOnlyList<string> EnsureColumns(
        IReadOnlyList<string> columns,
        int columnCount)
    {
        if (columns.Count >= columnCount)
        {
            return columns.ToArray();
        }

        var result = columns.ToList();
        for (var index = result.Count; index < columnCount; index++)
        {
            result.Add($"Column {index + 1}");
        }

        return result.ToArray();
    }

    private static string[] NormalizeRow(IReadOnlyList<string> row, int columnCount)
    {
        var result = new string[columnCount];
        for (var index = 0; index < columnCount; index++)
        {
            result[index] = index < row.Count ? row[index] ?? string.Empty : string.Empty;
        }

        return result;
    }

    private static bool IsValidRowIndex(
        CompositionDetailTableSnapshot table,
        int rowIndex)
    {
        return rowIndex >= 0 && rowIndex < table.Rows.Count;
    }

    private static string GetCell(IReadOnlyList<string> row, int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < row.Count
            ? row[columnIndex] ?? string.Empty
            : string.Empty;
    }

    private sealed record TableRowSortEntry(
        IReadOnlyList<string> Row,
        int Index,
        string SortValue);
}
