using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailTableCsv
{
    public static CompositionDetailTableSnapshot Parse(string csv)
    {
        var records = ReadRecords(csv ?? string.Empty);
        if (records.Count == 0)
        {
            return new CompositionDetailTableSnapshot();
        }

        return new CompositionDetailTableSnapshot(
            records[0],
            records.Skip(1).Select(row => (IReadOnlyList<string>)row).ToArray());
    }

    public static string Format(CompositionDetailTableSnapshot table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        var records = new List<IReadOnlyList<string>> { table.Columns };
        records.AddRange(table.Rows);
        return string.Join(Environment.NewLine, records.Select(FormatRecord));
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
}
