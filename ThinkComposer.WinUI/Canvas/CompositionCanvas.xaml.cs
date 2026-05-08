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

    private Vector2 _pan = new(0, 0);
    private double _zoom = 1.0;
    private CanvasNode? _selectedNode;
    private bool _needsInitialFit;
    private bool _isPanning;
    private Point _panStartScreen;
    private Vector2 _panStart;
    private CanvasNode? _draggedNode;
    private Point _dragStartWorld;
    private Rect _dragStartBounds;

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
    public event EventHandler<CompositionNodeView>? NodeMoved;

    public CompositionNodeView? SelectedNode => _selectedNode?.Source;

    public void LoadSnapshot(CompositionViewSnapshot snapshot)
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

        SetSelectedNode(_nodes.FirstOrDefault());
        _needsInitialFit = true;
        FitSnapshotToViewport();
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
        DrawConnectors(session, palette);
        DrawNodes(session, palette);
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
            session.DrawLine(
                (float)route.Source.X,
                (float)route.Source.Y,
                (float)route.Target.X,
                (float)route.Target.Y,
                palette.Connector,
                CompositionCanvasRenderDefaults.ConnectorStrokeWidth);
        }
    }

    private void DrawNodes(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var node in _nodes)
        {
            var bounds = node.Bounds;
            var stroke = node == _selectedNode ? palette.SelectedStroke : palette.NodeStroke;
            var strokeWidth = node == _selectedNode ? CompositionCanvasRenderDefaults.SelectedNodeStrokeWidth : 1;

            session.FillRoundedRectangle(bounds, 7, 7, palette.NodeFill);
            session.DrawRoundedRectangle(bounds, 7, 7, stroke, strokeWidth);

            var label = CompositionNodeLabelPolicy.ForNode(node.Source);
            var padding = label.Padding;
            if (!label.ShowsSubtitle)
            {
                session.DrawText(
                    node.Title,
                    CreateCompactTextRect(bounds, padding),
                    palette.Text,
                    CompactTitleFormat);
                continue;
            }

            session.DrawText(
                node.Title,
                new Rect(bounds.X + padding, bounds.Y + 10, Math.Max(1, bounds.Width - padding * 2), 24),
                palette.Text,
                TitleFormat);
            session.DrawText(
                node.Subtitle,
                new Rect(bounds.X + padding, bounds.Y + 38, Math.Max(1, bounds.Width - padding * 2), Math.Max(1, bounds.Height - 48)),
                palette.MutedText,
                BodyFormat);
        }
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
        if (hitNode is not null)
        {
            SetSelectedNode(hitNode);
            _draggedNode = hitNode;
            _dragStartWorld = worldPoint;
            _dragStartBounds = hitNode.Bounds;
        }

        if (hitNode is null)
        {
            _isPanning = true;
            _panStartScreen = screenPoint;
            _panStart = _pan;
        }

        DrawingSurface.CapturePointer(e.Pointer);
        DrawingSurface.Invalidate();
    }

    private void DrawingSurface_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var screenPoint = e.GetCurrentPoint(DrawingSurface).Position;

        if (_draggedNode is not null)
        {
            var worldPoint = ToWorld(screenPoint);
            _draggedNode.MoveTo(
                _dragStartBounds.X + worldPoint.X - _dragStartWorld.X,
                _dragStartBounds.Y + worldPoint.Y - _dragStartWorld.Y);

            NodeMoved?.Invoke(this, _draggedNode.Source);
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
        _isPanning = false;
        _draggedNode = null;
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

    private void SetSelectedNode(CanvasNode? node)
    {
        if (ReferenceEquals(_selectedNode, node))
        {
            return;
        }

        _selectedNode = node;
        SelectedNodeChanged?.Invoke(this, node?.Source);
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
