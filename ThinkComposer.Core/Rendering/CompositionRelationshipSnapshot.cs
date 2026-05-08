namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionRelationshipSnapshot(
    string Id,
    string Name,
    string SourceIdeaId,
    string TargetIdeaId,
    string DefinitionId = "",
    IReadOnlyList<CompositionDetailSnapshot>? Details = null,
    IReadOnlyList<string>? Markers = null,
    CompositionStyleSnapshot? Style = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null)
{
    public IReadOnlyList<CompositionDetailSnapshot> Details { get; init; } =
        Details ?? Array.Empty<CompositionDetailSnapshot>();

    public IReadOnlyList<string> Markers { get; init; } = Markers ?? Array.Empty<string>();

    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();
}
