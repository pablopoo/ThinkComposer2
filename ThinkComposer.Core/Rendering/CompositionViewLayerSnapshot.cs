namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionViewLayerSnapshot(
    string Id,
    string Name,
    IReadOnlyList<CompositionNodeView>? Nodes = null,
    IReadOnlyList<CompositionConnectorView>? Connectors = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Complements = null,
    CompositionStyleSnapshot? Style = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null)
{
    public IReadOnlyList<CompositionNodeView> Nodes { get; init; } = Nodes ?? Array.Empty<CompositionNodeView>();

    public IReadOnlyList<CompositionConnectorView> Connectors { get; init; } =
        Connectors ?? Array.Empty<CompositionConnectorView>();

    public IReadOnlyList<CompositionExtensionSnapshot> Complements { get; init; } =
        Complements ?? Array.Empty<CompositionExtensionSnapshot>();

    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();
}
