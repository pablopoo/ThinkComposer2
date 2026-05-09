using Windows.Foundation;

namespace Instrumind.ThinkComposer.WinUI.Canvas;

public sealed class CompositionCanvasContextRequestedEventArgs(
    Point screenPoint,
    Point worldPoint,
    string? nodeId,
    string? connectorId,
    bool hasSelection) : EventArgs
{
    public Point ScreenPoint { get; } = screenPoint;

    public Point WorldPoint { get; } = worldPoint;

    public string? NodeId { get; } = nodeId;

    public string? ConnectorId { get; } = connectorId;

    public bool HasSelection { get; } = hasSelection;
}
