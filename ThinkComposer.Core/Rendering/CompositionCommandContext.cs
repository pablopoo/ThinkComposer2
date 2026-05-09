namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionCommandContext(
    bool HasDocument,
    bool HasSnapshot,
    int SelectedNodeCount,
    bool HasSelectedConnector,
    bool HasClipboard,
    bool CanUndo,
    bool CanRedo)
{
    public static CompositionCommandContext Empty { get; } = new(false, false, 0, false, false, false, false);

    public bool HasSelection => SelectedNodeCount > 0 || HasSelectedConnector;

    public bool HasSingleNodeSelection => SelectedNodeCount == 1 && !HasSelectedConnector;

    public bool HasMultiNodeSelection => SelectedNodeCount > 1 && !HasSelectedConnector;
}
