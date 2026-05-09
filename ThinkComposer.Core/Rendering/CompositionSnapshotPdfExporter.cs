using SkiaSharp;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotPdfExporter
{
    private const float MinimumContentWidth = 320;
    private const float MinimumContentHeight = 240;

    public static byte[] Export(CompositionViewSnapshot snapshot, CompositionPdfExportOptions? options = null)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        options ??= new CompositionPdfExportOptions();
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        using var canvas = document.BeginPage(options.PageWidth, options.PageHeight);

        RenderSnapshot(canvas, snapshot, options);

        document.EndPage();
        document.Close();
        return stream.ToArray();
    }

    internal static void RenderSnapshot(
        SKCanvas canvas,
        CompositionViewSnapshot snapshot,
        CompositionPdfExportOptions options,
        bool clearBackground = true)
    {
        if (canvas is null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (clearBackground)
        {
            canvas.Clear(SKColors.White);
        }

        var bounds = GetBounds(snapshot);
        var contentWidth = Math.Max(1, options.PageWidth - options.Margin * 2);
        var contentHeight = Math.Max(1, options.PageHeight - options.Margin * 2);
        var snapshotWidth = Math.Max(MinimumContentWidth, (float)(bounds.Right - bounds.Left));
        var snapshotHeight = Math.Max(MinimumContentHeight, (float)(bounds.Bottom - bounds.Top));
        var scale = Math.Min(contentWidth / snapshotWidth, contentHeight / snapshotHeight);
        var extraX = (contentWidth - snapshotWidth * scale) / 2;
        var extraY = (contentHeight - snapshotHeight * scale) / 2;
        var nodesById = snapshot.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        SKPoint Project(double x, double y)
        {
            return new SKPoint(
                options.Margin + extraX + (float)((x - bounds.Left) * scale),
                options.Margin + extraY + (float)((y - bounds.Top) * scale));
        }

        using var connectorPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round
        };
        using var nodeFillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var nodeStrokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(31, 35, 40)
        };
        using var typeface = SKTypeface.FromFamilyName(options.FontFamily);
        using var titleFont = new SKFont(typeface, Math.Max(8, 12 * scale))
        {
            Edging = SKFontEdging.Antialias
        };
        using var connectorFont = new SKFont(typeface, Math.Max(7, 10 * scale))
        {
            Edging = SKFontEdging.Antialias
        };

        foreach (var connector in snapshot.Connectors)
        {
            if (!nodesById.TryGetValue(connector.SourceId, out var source) ||
                !nodesById.TryGetValue(connector.TargetId, out var target))
            {
                continue;
            }

            var route = CompositionConnectorRouter.Route(source, target);
            var from = Project(route.Source.X, route.Source.Y);
            var to = Project(route.Target.X, route.Target.Y);
            connectorPaint.Color = ParseColor(connector.Style.Stroke, new SKColor(43, 120, 198));
            connectorPaint.StrokeWidth = Math.Max(1, (float)PositiveOrDefault(connector.Style.StrokeThickness, 1.4) * scale);
            canvas.DrawLine(from, to, connectorPaint);

            if (!string.IsNullOrWhiteSpace(connector.Text))
            {
                var center = new SKPoint((from.X + to.X) / 2 + 4, (from.Y + to.Y) / 2 - 4);
                textPaint.Color = connectorPaint.Color;
                canvas.DrawText(TrimForPdf(connector.Text), center.X, center.Y, connectorFont, textPaint);
            }
        }

        foreach (var node in snapshot.Nodes)
        {
            var topLeft = Project(node.Position.X, node.Position.Y);
            var width = Math.Max(20, (float)node.Size.Width) * scale;
            var height = Math.Max(20, (float)node.Size.Height) * scale;
            var rect = new SKRoundRect(
                new SKRect(topLeft.X, topLeft.Y, topLeft.X + width, topLeft.Y + height),
                Math.Min(7 * scale, width / 4),
                Math.Min(7 * scale, height / 4));

            nodeFillPaint.Color = ParseColor(node.Style.Fill, SKColors.White);
            nodeStrokePaint.Color = ParseColor(node.Style.Stroke, new SKColor(138, 155, 168));
            nodeStrokePaint.StrokeWidth = Math.Max(0.75f, (float)PositiveOrDefault(node.Style.StrokeThickness, 1) * scale);
            canvas.DrawRoundRect(rect, nodeFillPaint);
            canvas.DrawRoundRect(rect, nodeStrokePaint);

            textPaint.Color = ParseColor(node.Style.Text, new SKColor(31, 35, 40));
            var baseline = topLeft.Y + Math.Min(height / 2 + titleFont.Size / 2 - 2, titleFont.Size + 12 * scale);
            canvas.DrawText(TrimForPdf(node.Text), topLeft.X + 10 * scale, baseline, titleFont, textPaint);
        }
    }

    private static SnapshotBounds GetBounds(CompositionViewSnapshot snapshot)
    {
        if (snapshot.Nodes.Count == 0)
        {
            return new SnapshotBounds(0, 0, MinimumContentWidth, MinimumContentHeight);
        }

        return new SnapshotBounds(
            snapshot.Nodes.Min(node => node.Position.X),
            snapshot.Nodes.Min(node => node.Position.Y),
            snapshot.Nodes.Max(node => node.Position.X + Math.Max(1, node.Size.Width)),
            snapshot.Nodes.Max(node => node.Position.Y + Math.Max(1, node.Size.Height)));
    }

    private static SKColor ParseColor(string value, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return SKColor.TryParse(value.Trim(), out var color) ? color : fallback;
    }

    private static double PositiveOrDefault(double value, double defaultValue)
    {
        return value > 0 ? value : defaultValue;
    }

    private static string TrimForPdf(string? value)
    {
        var trimmed = (value ?? string.Empty)
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return trimmed.Length <= 96 ? trimmed : $"{trimmed.Substring(0, 93)}...";
    }

    private readonly record struct SnapshotBounds(double Left, double Top, double Right, double Bottom);
}
