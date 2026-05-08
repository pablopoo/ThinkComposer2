using System.Globalization;
using System.Numerics;
using Instrumind.ThinkComposer.Core.Primitives;
using Instrumind.ThinkComposer.Core.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Instrumind.ThinkComposer.WinUI.Canvas;

public sealed partial class CompositionCanvas : UserControl
{
    private readonly List<CanvasNode> _nodes = [];
    private readonly Dictionary<string, CanvasNode> _nodesById = new(StringComparer.Ordinal);
    private readonly List<CompositionConnectorView> _connectors = [];
    private readonly List<CompositionExtensionSnapshot> _complements = [];
    private readonly HashSet<string> _selectedNodeIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Rect> _dragStartBoundsByNodeId = new(StringComparer.Ordinal);

    private Vector2 _pan = new(0, 0);
    private double _zoom = 1.0;
    private CanvasNode? _selectedNode;
    private CompositionConnectorView? _selectedConnector;
    private bool _needsInitialFit;
    private bool _isPanning;
    private Point _panStartScreen;
    private Vector2 _panStart;
    private CanvasNode? _draggedNode;
    private Point _dragStartWorld;

    public CompositionCanvas()
    {
        InitializeComponent();
        LoadSnapshot(DemoCompositionViewSource.CreateSnapshot());
        ActualThemeChanged += (_, _) => DrawingSurface.Invalidate();
        DrawingSurface.SizeChanged += (_, _) =>
        {
            if (_needsInitialFit)
            {
                FitSnapshotToViewport();
            }
        };
    }

    public event EventHandler<CompositionNodeView?>? SelectedNodeChanged;
    public event EventHandler<IReadOnlyList<CompositionNodeView>>? SelectedNodesChanged;
    public event EventHandler<CompositionConnectorView?>? SelectedConnectorChanged;
    public event EventHandler<CompositionNodeView>? NodeMoved;
    public event EventHandler<CompositionNodeView>? NodeMoveCompleted;

    public CompositionNodeView? SelectedNode => _selectedNode?.Source;
    public IReadOnlyList<CompositionNodeView> SelectedNodes => _nodes
        .Where(node => _selectedNodeIds.Contains(node.Id))
        .Select(node => node.Source)
        .ToArray();
    public CompositionConnectorView? SelectedConnector => _selectedConnector;

    public void LoadSnapshot(
        CompositionViewSnapshot snapshot,
        string? selectedNodeId = null,
        bool fitToViewport = true,
        string? selectedConnectorId = null,
        IReadOnlyList<CompositionExtensionSnapshot>? complements = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _nodes.Clear();
        _nodes.AddRange(snapshot.Nodes.Select(CanvasNode.FromView));
        _nodesById.Clear();
        foreach (var node in _nodes)
        {
            _nodesById[node.Id] = node;
        }

        _connectors.Clear();
        _connectors.AddRange(snapshot.Connectors);
        _complements.Clear();
        if (complements is not null)
        {
            _complements.AddRange(complements);
        }

        _selectedNodeIds.Clear();

        SetSelectedNode(FindNode(selectedNodeId) ?? (string.IsNullOrWhiteSpace(selectedConnectorId) ? _nodes.FirstOrDefault() : null));
        SetSelectedConnector(FindConnector(selectedConnectorId));
        if (fitToViewport)
        {
            _needsInitialFit = true;
            FitSnapshotToViewport();
        }
        else
        {
            _needsInitialFit = false;
            DrawingSurface.Invalidate();
        }
    }

    public TcPoint GetViewportCenter()
    {
        var screenPoint = new Point(DrawingSurface.ActualWidth / 2, DrawingSurface.ActualHeight / 2);
        var worldPoint = ToWorld(screenPoint);
        return new TcPoint(worldPoint.X, worldPoint.Y);
    }

    public void SelectNode(string nodeId)
    {
        SetSelectedConnector(null);
        SetSelectedNode(FindNode(nodeId));
        DrawingSurface.Invalidate();
    }

    public void SelectNodes(IEnumerable<string> nodeIds)
    {
        var nodes = nodeIds
            .Select(FindNode)
            .Where(node => node is not null)
            .Cast<CanvasNode>()
            .ToArray();
        SetSelectedConnector(null);
        SetSelectedNodes(nodes);
        DrawingSurface.Invalidate();
    }

    public void SelectConnector(string connectorId)
    {
        SetSelectedNode(null);
        SetSelectedConnector(FindConnector(connectorId));
        DrawingSurface.Invalidate();
    }

    public void FitSnapshotToViewport()
    {
        if (DrawingSurface.ActualWidth <= 0 || DrawingSurface.ActualHeight <= 0)
        {
            _needsInitialFit = true;
            return;
        }

        var fit = CompositionViewportFitter.FitNodes(
            _nodes.Select(node => node.Source),
            DrawingSurface.ActualWidth,
            DrawingSurface.ActualHeight,
            margin: 72,
            minZoom: CompositionCanvasRenderDefaults.InitialMinZoom,
            maxZoom: CompositionCanvasRenderDefaults.InitialMaxZoom);

        _zoom = fit.Zoom;
        _pan = new Vector2((float)fit.PanX, (float)fit.PanY);
        _needsInitialFit = false;
        DrawingSurface.Invalidate();
    }

    private void DrawingSurface_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var session = args.DrawingSession;
        var palette = CanvasPalette.ForTheme(ActualTheme);
        session.Clear(palette.Background);

        DrawGrid(session, sender.ActualWidth, sender.ActualHeight, palette);

        session.Transform = Matrix3x2.CreateScale((float)_zoom) * Matrix3x2.CreateTranslation(_pan);
        DrawComplementRegions(session, palette);
        DrawConnectors(session, palette);
        DrawNodes(session, palette);
        DrawComplementCards(session, palette);
        session.Transform = Matrix3x2.Identity;

        DrawViewportLabel(session, sender.ActualWidth, sender.ActualHeight, palette);
    }

    private void DrawGrid(CanvasDrawingSession session, double width, double height, CanvasPalette palette)
    {
        const float spacing = 24;
        for (var x = _pan.X % spacing; x < width; x += spacing)
        {
            session.DrawLine((float)x, 0, (float)x, (float)height, palette.GridLine, 0.5f);
        }

        for (var y = _pan.Y % spacing; y < height; y += spacing)
        {
            session.DrawLine(0, (float)y, (float)width, (float)y, palette.GridLine, 0.5f);
        }
    }

    private void DrawConnectors(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var connector in _connectors)
        {
            if (!_nodesById.TryGetValue(connector.SourceId, out var sourceNode) ||
                !_nodesById.TryGetValue(connector.TargetId, out var targetNode))
            {
                continue;
            }

            var route = CompositionConnectorRouter.Route(sourceNode.ToRoutingView(), targetNode.ToRoutingView());
            var isSelected = string.Equals(connector.Id, _selectedConnector?.Id, StringComparison.Ordinal);
            session.DrawLine(
                (float)route.Source.X,
                (float)route.Source.Y,
                (float)route.Target.X,
                (float)route.Target.Y,
                isSelected ? palette.SelectedStroke : ColorFromStyle(connector.Style.Stroke, palette.Connector),
                isSelected
                    ? CompositionCanvasRenderDefaults.SelectedNodeStrokeWidth
                    : StrokeWidthFromStyle(connector.Style.StrokeThickness, CompositionCanvasRenderDefaults.ConnectorStrokeWidth));
        }
    }

    private void DrawNodes(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var node in _nodes)
        {
            var bounds = node.Bounds;
            var isSelected = _selectedNodeIds.Contains(node.Id);
            var stroke = isSelected ? palette.SelectedStroke : ColorFromStyle(node.Source.Style.Stroke, palette.NodeStroke);
            var strokeWidth = isSelected
                ? CompositionCanvasRenderDefaults.SelectedNodeStrokeWidth
                : StrokeWidthFromStyle(node.Source.Style.StrokeThickness, 1);
            var fill = ColorFromStyle(node.Source.Style.Fill, palette.NodeFill);
            var text = ColorFromStyle(node.Source.Style.Text, palette.Text);

            session.FillRoundedRectangle(bounds, 7, 7, fill);
            session.DrawRoundedRectangle(bounds, 7, 7, stroke, strokeWidth);

            var label = CompositionNodeLabelPolicy.ForNode(node.Source);
            var padding = label.Padding;
            if (!label.ShowsSubtitle)
            {
                session.DrawText(
                    node.Title,
                    CreateCompactTextRect(bounds, padding),
                    text,
                    CompactTitleFormat);
                continue;
            }

            session.DrawText(
                node.Title,
                new Rect(bounds.X + padding, bounds.Y + 10, Math.Max(1, bounds.Width - padding * 2), 24),
                text,
                TitleFormat);
            session.DrawText(
                node.Subtitle,
                new Rect(bounds.X + padding, bounds.Y + 38, Math.Max(1, bounds.Width - padding * 2), Math.Max(1, bounds.Height - 48)),
                palette.MutedText,
                BodyFormat);
        }
    }

    private void DrawComplementRegions(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var item in BuildComplementItems().Where(item => item.Kind == "Group"))
        {
            var bounds = ToRect(item);
            session.FillRoundedRectangle(bounds, 8, 8, WithAlpha(palette.SelectedStroke, 24));
            session.DrawRoundedRectangle(bounds, 8, 8, WithAlpha(palette.SelectedStroke, 130), 1.2f);
            session.DrawText(
                item.Title,
                new Rect(bounds.X + 12, bounds.Y + 8, Math.Max(1, bounds.Width - 24), 22),
                palette.SelectedStroke,
                BodyFormat);
        }
    }

    private void DrawComplementCards(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var item in BuildComplementItems().Where(item => item.Kind != "Group"))
        {
            var bounds = ToRect(item);
            session.FillRoundedRectangle(bounds, 7, 7, palette.NodeFill);
            session.DrawRoundedRectangle(bounds, 7, 7, WithAlpha(palette.SelectedStroke, 150), 1.1f);
            session.DrawText(
                item.Title,
                new Rect(bounds.X + 12, bounds.Y + 10, Math.Max(1, bounds.Width - 24), 22),
                palette.Text,
                CompactTitleFormat);
            session.DrawText(
                string.IsNullOrWhiteSpace(item.Body) ? item.Key : item.Body,
                new Rect(bounds.X + 12, bounds.Y + 36, Math.Max(1, bounds.Width - 24), Math.Max(1, bounds.Height - 44)),
                palette.MutedText,
                BodyFormat);
        }
    }

    private IReadOnlyList<CompositionComplementRenderItem> BuildComplementItems()
    {
        return CompositionViewComplementLayout.Build(_complements, _nodes.Select(node => node.Source).ToArray());
    }

    private static Rect ToRect(CompositionComplementRenderItem item)
    {
        return new Rect(item.Position.X, item.Position.Y, item.Size.Width, item.Size.Height);
    }

    private static Windows.UI.Color WithAlpha(Windows.UI.Color color, byte alpha)
    {
        return Windows.UI.Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Rect CreateCompactTextRect(Rect bounds, double horizontalPadding)
    {
        const double lineHeight = 18;
        return new Rect(
            bounds.X + horizontalPadding,
            bounds.Y + Math.Max(3, (bounds.Height - lineHeight) / 2),
            Math.Max(1, bounds.Width - horizontalPadding * 2),
            lineHeight);
    }

    private static Windows.UI.Color ColorFromStyle(string value, Windows.UI.Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var hex = value.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length == 6 &&
            int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return Windows.UI.Color.FromArgb(
                255,
                (byte)((rgb >> 16) & 0xff),
                (byte)((rgb >> 8) & 0xff),
                (byte)(rgb & 0xff));
        }

        if (hex.Length == 8 &&
            uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            return Windows.UI.Color.FromArgb(
                (byte)((argb >> 24) & 0xff),
                (byte)((argb >> 16) & 0xff),
                (byte)((argb >> 8) & 0xff),
                (byte)(argb & 0xff));
        }

        return fallback;
    }

    private static float StrokeWidthFromStyle(double value, float fallback)
    {
        return value > 0 ? (float)value : fallback;
    }

    private void DrawViewportLabel(CanvasDrawingSession session, double width, double height, CanvasPalette palette)
    {
        var label = $"Win2D canvas  |  zoom {Math.Round(_zoom * 100)}%";
        session.FillRoundedRectangle(new Rect(width - 206, height - 42, 190, 30), 6, 6, palette.NodeFill);
        session.DrawRoundedRectangle(new Rect(width - 206, height - 42, 190, 30), 6, 6, palette.NodeStroke);
        session.DrawText(label, new Rect(width - 194, height - 36, 168, 22), palette.MutedText, BodyFormat);
    }

    private void DrawingSurface_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var screenPoint = e.GetCurrentPoint(DrawingSurface).Position;
        var worldPoint = ToWorld(screenPoint);
        var hitNode = HitTest(worldPoint);
        var extendsSelection = e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift);
        if (hitNode is not null)
        {
            SetSelectedConnector(null);
            if (extendsSelection)
            {
                ToggleSelectedNode(hitNode);
                if (!_selectedNodeIds.Contains(hitNode.Id))
                {
                    DrawingSurface.Invalidate();
                    return;
                }
            }
            else if (!_selectedNodeIds.Contains(hitNode.Id))
            {
                SetSelectedNode(hitNode);
            }

            BeginNodeDrag(hitNode, worldPoint);
        }

        if (hitNode is null)
        {
            var hitConnector = HitTestConnector(worldPoint);
            if (hitConnector is not null)
            {
                SetSelectedNode(null);
                SetSelectedConnector(hitConnector);
            }
            else
            {
                if (!extendsSelection)
                {
                    SetSelectedNode(null);
                    SetSelectedConnector(null);
                }

                _isPanning = true;
                _panStartScreen = screenPoint;
                _panStart = _pan;
            }
        }

        DrawingSurface.CapturePointer(e.Pointer);
        DrawingSurface.Invalidate();
    }

    private CompositionConnectorView? HitTestConnector(Point worldPoint)
    {
        var threshold = Math.Max(4, 8 / _zoom);
        for (var index = _connectors.Count - 1; index >= 0; index--)
        {
            var connector = _connectors[index];
            if (!_nodesById.TryGetValue(connector.SourceId, out var sourceNode) ||
                !_nodesById.TryGetValue(connector.TargetId, out var targetNode))
            {
                continue;
            }

            var route = CompositionConnectorRouter.Route(sourceNode.ToRoutingView(), targetNode.ToRoutingView());
            if (DistanceToSegment(worldPoint, route) <= threshold)
            {
                return connector;
            }
        }

        return null;
    }

    private static double DistanceToSegment(Point point, CompositionConnectorRoute route)
    {
        var x1 = route.Source.X;
        var y1 = route.Source.Y;
        var x2 = route.Target.X;
        var y2 = route.Target.Y;
        var dx = x2 - x1;
        var dy = y2 - y1;
        if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
        {
            return Math.Sqrt(Math.Pow(point.X - x1, 2) + Math.Pow(point.Y - y1, 2));
        }

        var t = Math.Max(0, Math.Min(1, ((point.X - x1) * dx + (point.Y - y1) * dy) / (dx * dx + dy * dy)));
        var projectionX = x1 + t * dx;
        var projectionY = y1 + t * dy;
        return Math.Sqrt(Math.Pow(point.X - projectionX, 2) + Math.Pow(point.Y - projectionY, 2));
    }

    private void DrawingSurface_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var screenPoint = e.GetCurrentPoint(DrawingSurface).Position;

        if (_draggedNode is not null)
        {
            var worldPoint = ToWorld(screenPoint);
            var deltaX = worldPoint.X - _dragStartWorld.X;
            var deltaY = worldPoint.Y - _dragStartWorld.Y;
            foreach (var node in _nodes.Where(node => _selectedNodeIds.Contains(node.Id)))
            {
                if (!_dragStartBoundsByNodeId.TryGetValue(node.Id, out var startBounds))
                {
                    continue;
                }

                node.MoveTo(startBounds.X + deltaX, startBounds.Y + deltaY);
                NodeMoved?.Invoke(this, node.Source);
            }

            DrawingSurface.Invalidate();
            return;
        }

        if (_isPanning)
        {
            _pan = new Vector2(
                _panStart.X + (float)(screenPoint.X - _panStartScreen.X),
                _panStart.Y + (float)(screenPoint.Y - _panStartScreen.Y));
            DrawingSurface.Invalidate();
        }
    }

    private void DrawingSurface_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var movedNodes = _draggedNode is null
            ? Array.Empty<CanvasNode>()
            : _nodes.Where(node => _selectedNodeIds.Contains(node.Id)).ToArray();
        _isPanning = false;
        _draggedNode = null;
        _dragStartBoundsByNodeId.Clear();
        foreach (var movedNode in movedNodes)
        {
            NodeMoveCompleted?.Invoke(this, movedNode.Source);
        }

        DrawingSurface.ReleasePointerCapture(e.Pointer);
    }

    private void DrawingSurface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(DrawingSurface);
        var beforeZoom = ToWorld(point.Position);
        var delta = point.Properties.MouseWheelDelta > 0 ? 1.1 : 0.9;
        _zoom = Math.Clamp(
            _zoom * delta,
            CompositionCanvasRenderDefaults.InteractionMinZoom,
            CompositionCanvasRenderDefaults.InteractionMaxZoom);
        _pan = new Vector2(
            (float)(point.Position.X - beforeZoom.X * _zoom),
            (float)(point.Position.Y - beforeZoom.Y * _zoom));
        DrawingSurface.Invalidate();
    }

    private Point ToWorld(Point screenPoint)
    {
        return new Point((screenPoint.X - _pan.X) / _zoom, (screenPoint.Y - _pan.Y) / _zoom);
    }

    private CanvasNode? HitTest(Point worldPoint)
    {
        return _nodes.LastOrDefault(node => node.Bounds.Contains(worldPoint));
    }

    private CanvasNode? FindNode(string? nodeId)
    {
        return string.IsNullOrWhiteSpace(nodeId) ? null : _nodesById.GetValueOrDefault(nodeId);
    }

    private CompositionConnectorView? FindConnector(string? connectorId)
    {
        return string.IsNullOrWhiteSpace(connectorId)
            ? null
            : _connectors.FirstOrDefault(connector => string.Equals(connector.Id, connectorId, StringComparison.Ordinal));
    }

    private void SetSelectedNode(CanvasNode? node)
    {
        if (ReferenceEquals(_selectedNode, node))
        {
            return;
        }

        _selectedNodeIds.Clear();
        if (node is not null)
        {
            _selectedNodeIds.Add(node.Id);
        }

        _selectedNode = node;
        SelectedNodeChanged?.Invoke(this, node?.Source);
        SelectedNodesChanged?.Invoke(this, SelectedNodes);
    }

    private void SetSelectedNodes(IReadOnlyList<CanvasNode> nodes)
    {
        _selectedNodeIds.Clear();
        foreach (var node in nodes)
        {
            _selectedNodeIds.Add(node.Id);
        }

        _selectedNode = nodes.Count == 1 ? nodes[0] : null;
        SelectedNodeChanged?.Invoke(this, _selectedNode?.Source);
        SelectedNodesChanged?.Invoke(this, SelectedNodes);
    }

    private void ToggleSelectedNode(CanvasNode node)
    {
        if (!_selectedNodeIds.Add(node.Id))
        {
            _selectedNodeIds.Remove(node.Id);
        }

        _selectedNode = _selectedNodeIds.Count == 1
            ? _nodes.FirstOrDefault(candidate => _selectedNodeIds.Contains(candidate.Id))
            : null;
        SelectedNodeChanged?.Invoke(this, _selectedNode?.Source);
        SelectedNodesChanged?.Invoke(this, SelectedNodes);
    }

    private void BeginNodeDrag(CanvasNode node, Point worldPoint)
    {
        _draggedNode = node;
        _dragStartWorld = worldPoint;
        _dragStartBoundsByNodeId.Clear();
        foreach (var selectedNode in _nodes.Where(candidate => _selectedNodeIds.Contains(candidate.Id)))
        {
            _dragStartBoundsByNodeId[selectedNode.Id] = selectedNode.Bounds;
        }
    }

    private void SetSelectedConnector(CompositionConnectorView? connector)
    {
        if (Equals(_selectedConnector, connector))
        {
            return;
        }

        _selectedConnector = connector;
        if (connector is not null)
        {
            _selectedNodeIds.Clear();
            _selectedNode = null;
            SelectedNodeChanged?.Invoke(this, null);
            SelectedNodesChanged?.Invoke(this, SelectedNodes);
        }

        SelectedConnectorChanged?.Invoke(this, connector);
    }

    private static CanvasTextFormat TitleFormat { get; } = new()
    {
        FontFamily = "Segoe UI",
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        WordWrapping = CanvasWordWrapping.NoWrap
    };

    private static CanvasTextFormat CompactTitleFormat { get; } = new()
    {
        FontFamily = "Segoe UI",
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        WordWrapping = CanvasWordWrapping.NoWrap,
        TrimmingGranularity = CanvasTextTrimmingGranularity.Character,
        TrimmingSign = CanvasTrimmingSign.Ellipsis
    };

    private static CanvasTextFormat BodyFormat { get; } = new()
    {
        FontFamily = "Segoe UI",
        FontSize = 12,
        WordWrapping = CanvasWordWrapping.Wrap
    };

    private sealed class CanvasNode(string id, string title, string subtitle, Rect bounds, CompositionNodeView source)
    {
        public string Id { get; } = id;
        public string Title { get; } = title;
        public string Subtitle { get; } = subtitle;
        public Rect Bounds { get; set; } = bounds;
        public CompositionNodeView Source { get; private set; } = source;
        public Point Center => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);

        public void MoveTo(double x, double y)
        {
            Bounds = new Rect(x, y, Bounds.Width, Bounds.Height);
            Source = Source with { Position = new TcPoint(x, y) };
        }

        public CompositionNodeView ToRoutingView()
        {
            return Source with
            {
                Position = new TcPoint(Bounds.X, Bounds.Y),
                Size = new TcSize(Bounds.Width, Bounds.Height)
            };
        }

        public static CanvasNode FromView(CompositionNodeView node)
        {
            return new CanvasNode(
                node.Id,
                node.Text,
                "Read-only snapshot",
                new Rect(node.Position.X, node.Position.Y, node.Size.Width, node.Size.Height),
                node);
        }
    }

    private sealed record CanvasPalette(
        Windows.UI.Color Background,
        Windows.UI.Color GridLine,
        Windows.UI.Color Connector,
        Windows.UI.Color NodeFill,
        Windows.UI.Color NodeStroke,
        Windows.UI.Color SelectedStroke,
        Windows.UI.Color Text,
        Windows.UI.Color MutedText)
    {
        public static CanvasPalette ForTheme(ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? Dark : Light;
        }

        private static CanvasPalette Light { get; } = new(
            Rgb(255, 255, 255),
            Rgb(235, 235, 235),
            Rgba(170, 43, 120, 198),
            Rgb(255, 255, 255),
            Rgb(138, 155, 168),
            Rgb(0, 122, 204),
            Rgb(31, 31, 31),
            Rgb(96, 96, 96));

        private static CanvasPalette Dark { get; } = new(
            Rgb(30, 30, 30),
            Rgb(45, 45, 48),
            Rgba(180, 55, 148, 255),
            Rgb(37, 37, 38),
            Rgb(80, 80, 80),
            Rgb(55, 148, 255),
            Rgb(220, 220, 220),
            Rgb(166, 166, 166));

        private static Windows.UI.Color Rgb(byte red, byte green, byte blue)
        {
            return Windows.UI.Color.FromArgb(255, red, green, blue);
        }

        private static Windows.UI.Color Rgba(byte alpha, byte red, byte green, byte blue)
        {
            return Windows.UI.Color.FromArgb(alpha, red, green, blue);
        }
    }
}
