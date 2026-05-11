using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailTableCsv
{
    private const string WidthsDirective = "#tc-widths";

    public static CompositionDetailTableSnapshot Parse(string csv)
    {
        var records = ParseRecords(csv);
        if (records.Count == 0)
        {
            return new CompositionDetailTableSnapshot();
        }

        var columnWidths = Array.Empty<double>();
        if (IsWidthsDirective(records[0]))
        {
            columnWidths = records[0]
                .Skip(1)
                .Select(ParseWidth)
                .ToArray();
            records = records.Skip(1).ToArray();
        }

        if (records.Count == 0)
        {
            return new CompositionDetailTableSnapshot(ColumnWidths: columnWidths);
        }

        return new CompositionDetailTableSnapshot(
            records[0],
            records.Skip(1).Select(row => (IReadOnlyList<string>)row).ToArray(),
            columnWidths);
    }

    public static string Format(CompositionDetailTableSnapshot table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var records = new List<IReadOnlyList<string>>();
        if (table.ColumnWidths.Any(width => width > 0))
        {
            records.Add(
                [WidthsDirective, .. table.ColumnWidths.Select(width => width.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))]);
        }

        records.Add(table.Columns);
        records.AddRange(table.Rows);
        return string.Join(Environment.NewLine, records.Select(FormatRecord));
    }

    public static IReadOnlyList<IReadOnlyList<string>> ParseRecords(string? csv)
    {
        return ReadRecords(csv ?? string.Empty);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadRecords(string csv)
    {
        if (csv.Length == 0)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        var records = new List<IReadOnlyList<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    fieldStarted = true;
                    inQuotes = true;
                    break;
                case ',':
                    AddField(record, field);
                    fieldStarted = false;
                    break;
                case '\r':
                    AddRecord(records, record, field);
                    fieldStarted = false;
                    if (index + 1 < csv.Length && csv[index + 1] == '\n')
                    {
                        index++;
                    }
                    break;
                case '\n':
                    AddRecord(records, record, field);
                    fieldStarted = false;
                    break;
                default:
                    fieldStarted = true;
                    field.Append(character);
                    break;
            }
        }

        if (fieldStarted || field.Length > 0 || record.Count > 0 || csv.EndsWith(",", StringComparison.Ordinal))
        {
            AddRecord(records, record, field);
        }

        return records;
    }

    private static string FormatRecord(IReadOnlyList<string> fields)
    {
        return string.Join(",", fields.Select(FormatField));
    }

    private static string FormatField(string? field)
    {
        var value = field ?? string.Empty;
        var mustQuote = value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n');
        return mustQuote ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static void AddField(ICollection<string> record, StringBuilder field)
    {
        record.Add(field.ToString());
        field.Clear();
    }

    private static void AddRecord(
        ICollection<IReadOnlyList<string>> records,
        List<string> record,
        StringBuilder field)
    {
        AddField(record, field);
        records.Add(record.ToArray());
        record.Clear();
    }

    private static bool IsWidthsDirective(IReadOnlyList<string> record)
    {
        return record.Count > 0 &&
            string.Equals(record[0], WidthsDirective, StringComparison.OrdinalIgnoreCase);
    }

    private static double ParseWidth(string value)
    {
        return double.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var width) && width > 0
            ? width
            : 0;
    }
}
