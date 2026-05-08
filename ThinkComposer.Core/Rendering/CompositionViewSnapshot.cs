namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionViewSnapshot(
    string Id,
    string Title,
    IReadOnlyList<CompositionNodeView> Nodes,
    IReadOnlyList<CompositionConnectorView> Connectors);
