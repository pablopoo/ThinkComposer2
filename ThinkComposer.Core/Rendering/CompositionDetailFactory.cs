namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDetailFactory
{
    public static CompositionDetailSnapshot CreateCustomField(string name, string value, string? id = null)
    {
        return Create(CompositionDetailKinds.CustomField, name, value, id);
    }

    public static CompositionDetailSnapshot CreateLink(string name, string url, string? id = null)
    {
        return Create(CompositionDetailKinds.Link, name, url, id);
    }

    public static CompositionDetailSnapshot CreateAttachment(string filePath, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Attachment path is required.", nameof(filePath));
        }

        return Create(CompositionDetailKinds.Attachment, Path.GetFileName(filePath), filePath, id);
    }

    public static CompositionDetailSnapshot CreateTable(string name, string value = "", string? id = null)
    {
        return Create(CompositionDetailKinds.Table, name, value, id);
    }

    public static CompositionDetailSnapshot CreateTable(
        string name,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string? id = null)
    {
        return CreateTable(name, CompositionDetailTableCsv.Format(new CompositionDetailTableSnapshot(columns, rows)), id);
    }

    public static CompositionDetailSnapshot Create(string kind, string name, string value, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Detail name is required.", nameof(name));
        }

        var normalizedKind = string.IsNullOrWhiteSpace(kind) ? CompositionDetailKinds.CustomField : kind.Trim();
        var normalizedName = name.Trim();
        var normalizedId = string.IsNullOrWhiteSpace(id) ? CreateDetailId(normalizedName) : id!.Trim();
        return new CompositionDetailSnapshot(normalizedId, normalizedKind, normalizedName, value ?? string.Empty);
    }

    private static string CreateDetailId(string name)
    {
        var normalized = new string(name.Trim()
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray())
            .Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? $"detail-{Guid.NewGuid():N}" : normalized;
    }
}
