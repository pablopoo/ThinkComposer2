using System.Globalization;
using System.Text.RegularExpressions;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailTableFormulaEvaluator
{
    private static readonly Regex SumRegex = new(
        @"^SUM\((?<range>[A-Z]+(?:(?<all>:)[A-Z]+|[0-9]+:[A-Z]+[0-9]+)?)\)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex BinaryRegex = new(
        @"^(?<left>[A-Z]+[0-9]+|-?[0-9]+(?:\.[0-9]+)?)\s*(?<op>[+\-*/])\s*(?<right>[A-Z]+[0-9]+|-?[0-9]+(?:\.[0-9]+)?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex CellRegex = new(
        @"^(?<column>[A-Z]+)(?<row>[0-9]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static CompositionDetailTableSnapshot Evaluate(CompositionDetailTableSnapshot table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var rows = table.Rows.Select(row => row.ToArray()).ToArray();
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                rows[row][column] = EvaluateCell(table, row, column, []);
            }
        }

        return new CompositionDetailTableSnapshot(table.Columns, rows, table.ColumnWidths);
    }

    private static string EvaluateCell(
        CompositionDetailTableSnapshot table,
        int row,
        int column,
        HashSet<(int Row, int Column)> visiting)
    {
        var value = GetCell(table, row, column);
        if (!value.StartsWith("=", StringComparison.Ordinal))
        {
            return value;
        }

        if (!visiting.Add((row, column)))
        {
            return value;
        }

        var expression = value.Substring(1).Trim();
        var result = TryEvaluateSum(table, expression, visiting, out var sum) ||
            TryEvaluateBinary(table, expression, visiting, out sum)
                ? FormatNumber(sum)
                : value;
        visiting.Remove((row, column));
        return result;
    }

    private static bool TryEvaluateSum(
        CompositionDetailTableSnapshot table,
        string expression,
        HashSet<(int Row, int Column)> visiting,
        out double result)
    {
        result = 0;
        var match = SumRegex.Match(expression);
        if (!match.Success)
        {
            return false;
        }

        var range = match.Groups["range"].Value;
        var cells = ExpandRange(table, range);
        foreach (var cell in cells)
        {
            result += ReadNumber(table, cell.Row, cell.Column, visiting);
        }

        return true;
    }

    private static bool TryEvaluateBinary(
        CompositionDetailTableSnapshot table,
        string expression,
        HashSet<(int Row, int Column)> visiting,
        out double result)
    {
        result = 0;
        var match = BinaryRegex.Match(expression);
        if (!match.Success)
        {
            return false;
        }

        var left = ReadOperand(table, match.Groups["left"].Value, visiting);
        var right = ReadOperand(table, match.Groups["right"].Value, visiting);
        result = match.Groups["op"].Value switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" when Math.Abs(right) > double.Epsilon => left / right,
            "/" => 0,
            _ => 0
        };
        return true;
    }

    private static IEnumerable<(int Row, int Column)> ExpandRange(
        CompositionDetailTableSnapshot table,
        string range)
    {
        if (range.IndexOf(':') >= 0)
        {
            var parts = range.Split(new[] { ':' }, 2);
            if (TryReadColumn(parts[0], out var wholeColumn) &&
                TryReadColumn(parts[1], out var wholeColumnEnd) &&
                wholeColumn == wholeColumnEnd)
            {
                for (var row = 0; row < table.Rows.Count; row++)
                {
                    yield return (row, wholeColumn);
                }

                yield break;
            }

            if (TryReadCell(parts[0], out var start) &&
                TryReadCell(parts[1], out var end))
            {
                var firstRow = Math.Min(start.Row, end.Row);
                var lastRow = Math.Max(start.Row, end.Row);
                var firstColumn = Math.Min(start.Column, end.Column);
                var lastColumn = Math.Max(start.Column, end.Column);
                for (var row = firstRow; row <= lastRow; row++)
                {
                    for (var column = firstColumn; column <= lastColumn; column++)
                    {
                        yield return (row, column);
                    }
                }
            }

            yield break;
        }

        if (TryReadCell(range, out var singleCell))
        {
            yield return singleCell;
        }
    }

    private static double ReadOperand(
        CompositionDetailTableSnapshot table,
        string value,
        HashSet<(int Row, int Column)> visiting)
    {
        if (TryReadCell(value, out var cell))
        {
            return ReadNumber(table, cell.Row, cell.Column, visiting);
        }

        return ParseNumber(value);
    }

    private static double ReadNumber(
        CompositionDetailTableSnapshot table,
        int row,
        int column,
        HashSet<(int Row, int Column)> visiting)
    {
        return ParseNumber(EvaluateCell(table, row, column, visiting));
    }

    private static string GetCell(CompositionDetailTableSnapshot table, int row, int column)
    {
        return row >= 0 &&
            row < table.Rows.Count &&
            column >= 0 &&
            column < table.Rows[row].Count
            ? table.Rows[row][column] ?? string.Empty
            : string.Empty;
    }

    private static bool TryReadCell(string value, out (int Row, int Column) cell)
    {
        cell = default;
        var match = CellRegex.Match(value.Trim());
        if (!match.Success ||
            !int.TryParse(match.Groups["row"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var row) ||
            !TryReadColumn(match.Groups["column"].Value, out var column))
        {
            return false;
        }

        cell = (row - 1, column);
        return row > 0;
    }

    private static bool TryReadColumn(string value, out int column)
    {
        column = 0;
        var text = value.Trim().ToUpperInvariant();
        if (text.Length == 0 || text.Any(character => character < 'A' || character > 'Z'))
        {
            return false;
        }

        foreach (var character in text)
        {
            column = (column * 26) + (character - 'A' + 1);
        }

        column--;
        return true;
    }

    private static double ParseNumber(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static string FormatNumber(double value)
    {
        return Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
    }
}
