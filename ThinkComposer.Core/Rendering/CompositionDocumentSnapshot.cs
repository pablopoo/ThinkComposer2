namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDocumentSnapshot(
    string Id,
    string Title,
    CompositionDomainSnapshot Domain,
    IReadOnlyList<CompositionIdeaSnapshot>? Ideas = null,
    IReadOnlyList<CompositionRelationshipSnapshot>? Relationships = null,
    IReadOnlyList<CompositionViewLayerSnapshot>? Views = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null,
    int SchemaVersion = CompositionDocumentSnapshot.CurrentSchemaVersion)
{
    public const int CurrentSchemaVersion = 1;

    public IReadOnlyList<CompositionIdeaSnapshot> Ideas { get; init; } =
        Ideas ?? Array.Empty<CompositionIdeaSnapshot>();

    public IReadOnlyList<CompositionRelationshipSnapshot> Relationships { get; init; } =
        Relationships ?? Array.Empty<CompositionRelationshipSnapshot>();

    public IReadOnlyList<CompositionViewLayerSnapshot> Views { get; init; } =
        Views ?? Array.Empty<CompositionViewLayerSnapshot>();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();
}
