namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDetailTableSnapshot(
    IReadOnlyList<string>? Columns = null,
    IReadOnlyList<IReadOnlyList<string>>? Rows = null,
    IReadOnlyList<double>? ColumnWidths = null)
{
    public IReadOnlyList<string> Columns { get; init; } = Columns ?? Array.Empty<string>();

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } =
        Rows?.Select(row => (IReadOnlyList<string>)row.ToArray()).ToArray()
        ?? Array.Empty<IReadOnlyList<string>>();

    public IReadOnlyList<double> ColumnWidths { get; init; } =
        ColumnWidths?.Select(width => width > 0 ? width : 0).ToArray()
        ?? Array.Empty<double>();
}
