namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDefinitionSnapshot(
    string Id,
    string Name,
    string Kind,
    string Summary = "",
    CompositionStyleSnapshot? Style = null,
    IReadOnlyList<CompositionDetailSnapshot>? Details = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null,
    CompositionDetailTableSnapshot? TableRecords = null)
{
    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();

    public IReadOnlyList<CompositionDetailSnapshot> Details { get; init; } =
        Details ?? Array.Empty<CompositionDetailSnapshot>();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();

    public CompositionDetailTableSnapshot TableRecords { get; init; } =
        TableRecords ?? new CompositionDetailTableSnapshot();
}
