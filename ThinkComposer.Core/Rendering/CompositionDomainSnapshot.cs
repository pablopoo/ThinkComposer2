namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDomainSnapshot(
    string Id,
    string Name,
    string Summary = "",
    IReadOnlyList<CompositionDefinitionSnapshot>? ConceptDefinitions = null,
    IReadOnlyList<CompositionDefinitionSnapshot>? RelationshipDefinitions = null,
    IReadOnlyList<CompositionDefinitionSnapshot>? MarkerDefinitions = null,
    IReadOnlyList<CompositionDefinitionSnapshot>? TableDefinitions = null,
    IReadOnlyList<CompositionDefinitionSnapshot>? ExternalLanguages = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Templates = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null,
    IReadOnlyList<CompositionDefinitionSnapshot>? LinkRoleDefinitions = null)
{
    public IReadOnlyList<CompositionDefinitionSnapshot> ConceptDefinitions { get; init; } =
        ConceptDefinitions ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionDefinitionSnapshot> RelationshipDefinitions { get; init; } =
        RelationshipDefinitions ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionDefinitionSnapshot> MarkerDefinitions { get; init; } =
        MarkerDefinitions ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionDefinitionSnapshot> TableDefinitions { get; init; } =
        TableDefinitions ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionDefinitionSnapshot> ExternalLanguages { get; init; } =
        ExternalLanguages ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionDefinitionSnapshot> LinkRoleDefinitions { get; init; } =
        LinkRoleDefinitions ?? Array.Empty<CompositionDefinitionSnapshot>();

    public IReadOnlyList<CompositionExtensionSnapshot> Templates { get; init; } =
        Templates ?? Array.Empty<CompositionExtensionSnapshot>();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();
}
