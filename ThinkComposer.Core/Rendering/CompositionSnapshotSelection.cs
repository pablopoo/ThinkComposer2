namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionSnapshotSelection(
    IReadOnlyList<CompositionNodeView>? Nodes = null,
    IReadOnlyList<CompositionConnectorView>? Connectors = null)
{
    public IReadOnlyList<CompositionNodeView> Nodes { get; init; } =
        Nodes ?? Array.Empty<CompositionNodeView>();
    public IReadOnlyList<CompositionConnectorView> Connectors { get; init; } =
        Connectors ?? Array.Empty<CompositionConnectorView>();
}
