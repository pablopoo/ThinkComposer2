using System.Numerics;
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
    private readonly List<CompositionConnectorView> _connectors = [];

    private Vector2 _pan = new(0, 0);
    private double _zoom = 1.0;
    private CanvasNode? _selectedNode;
    private bool _isPanning;
    private Point _panStartScreen;
    private Vector2 _panStart;

    public CompositionCanvas()
    {
        InitializeComponent();
        LoadSnapshot(DemoCompositionViewSource.CreateSnapshot());
        ActualThemeChanged += (_, _) => DrawingSurface.Invalidate();
    }

    public void LoadSnapshot(CompositionViewSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _nodes.Clear();
        _nodes.AddRange(snapshot.Nodes.Select(CanvasNode.FromView));

        _connectors.Clear();
        _connectors.AddRange(snapshot.Connectors);

        _selectedNode = _nodes.FirstOrDefault();
        DrawingSurface?.Invalidate();
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
            var sourceNode = _nodes.FirstOrDefault(node => node.Id == connector.SourceId);
            var targetNode = _nodes.FirstOrDefault(node => node.Id == connector.TargetId);

            if (sourceNode is null || targetNode is null)
            {
                continue;
            }

            var source = sourceNode.Center;
            var target = targetNode.Center;
            session.DrawLine((float)source.X, (float)source.Y, (float)target.X, (float)target.Y, palette.Connector, 2);
        }
    }

    private void DrawNodes(CanvasDrawingSession session, CanvasPalette palette)
    {
        foreach (var node in _nodes)
        {
            var bounds = node.Bounds;
            var stroke = node == _selectedNode ? palette.SelectedStroke : palette.NodeStroke;
            var strokeWidth = node == _selectedNode ? 3 : 1;

            session.FillRoundedRectangle(bounds, 8, 8, palette.NodeFill);
            session.DrawRoundedRectangle(bounds, 8, 8, stroke, strokeWidth);
            session.DrawText(node.Title, new Rect(bounds.X + 12, bounds.Y + 10, bounds.Width - 24, 24), palette.Text, TitleFormat);
            session.DrawText(node.Subtitle, new Rect(bounds.X + 12, bounds.Y + 38, bounds.Width - 24, 36), palette.MutedText, BodyFormat);
        }
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
        _selectedNode = hitNode ?? _selectedNode;

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
        DrawingSurface.ReleasePointerCapture(e.Pointer);
    }

    private void DrawingSurface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(DrawingSurface);
        var beforeZoom = ToWorld(point.Position);
        var delta = point.Properties.MouseWheelDelta > 0 ? 1.1 : 0.9;
        _zoom = Math.Clamp(_zoom * delta, 0.35, 2.4);
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

    private static CanvasTextFormat TitleFormat { get; } = new()
    {
        FontFamily = "Segoe UI",
        FontSize = 14,
        FontWeight = FontWeights.SemiBold
    };

    private static CanvasTextFormat BodyFormat { get; } = new()
    {
        FontFamily = "Segoe UI",
        FontSize = 12,
        WordWrapping = CanvasWordWrapping.Wrap
    };

    private sealed class CanvasNode(string id, string title, string subtitle, Rect bounds)
    {
        public string Id { get; } = id;
        public string Title { get; } = title;
        public string Subtitle { get; } = subtitle;
        public Rect Bounds { get; set; } = bounds;
        public Point Center => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);

        public static CanvasNode FromView(CompositionNodeView node)
        {
            return new CanvasNode(
                node.Id,
                node.Text,
                "Read-only snapshot",
                new Rect(node.Position.X, node.Position.Y, node.Size.Width, node.Size.Height));
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
            Rgb(43, 120, 198),
            Rgb(255, 255, 255),
            Rgb(138, 155, 168),
            Rgb(0, 122, 204),
            Rgb(31, 31, 31),
            Rgb(96, 96, 96));

        private static CanvasPalette Dark { get; } = new(
            Rgb(30, 30, 30),
            Rgb(45, 45, 48),
            Rgb(55, 148, 255),
            Rgb(37, 37, 38),
            Rgb(80, 80, 80),
            Rgb(55, 148, 255),
            Rgb(220, 220, 220),
            Rgb(166, 166, 166));

        private static Windows.UI.Color Rgb(byte red, byte green, byte blue)
        {
            return Windows.UI.Color.FromArgb(255, red, green, blue);
        }
    }
}
