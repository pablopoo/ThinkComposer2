namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDetailTableSnapshot(
    IReadOnlyList<string>? Columns = null,
    IReadOnlyList<IReadOnlyList<string>>? Rows = null)
{
    public IReadOnlyList<string> Columns { get; init; } = Columns ?? Array.Empty<string>();

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } =
        Rows?.Select(row => (IReadOnlyList<string>)row.ToArray()).ToArray()
        ?? Array.Empty<IReadOnlyList<string>>();
}
