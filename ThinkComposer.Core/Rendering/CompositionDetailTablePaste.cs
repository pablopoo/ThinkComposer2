namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailTablePaste
{
    public static IReadOnlyList<IReadOnlyList<string>> ParseRows(string? text)
    {
        var value = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        if (value.Contains('\t'))
        {
            return SplitLines(value)
                .Select(line => (IReadOnlyList<string>)line.Split('\t'))
                .ToArray();
        }

        return CompositionDetailTableCsv.ParseRecords(value);
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimEnd('\n')
            .Split('\n');
    }
}
