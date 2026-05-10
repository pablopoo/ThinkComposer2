using SkiaSharp;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotPdfExporter
{
    private const float MinimumContentWidth = 320;
    private const float MinimumContentHeight = 240;

    public static byte[] Export(CompositionViewSnapshot snapshot, CompositionPdfExportOptions? options = null)
    {
        return ExportInternal(snapshot, Array.Empty<CompositionExtensionSnapshot>(), options);
    }

    public static byte[] Export(
        CompositionViewSnapshot snapshot,
        IReadOnlyList<CompositionExtensionSnapshot> complements,
        CompositionPdfExportOptions? options = null)
    {
        return ExportInternal(snapshot, complements, options);
    }

    private static byte[] ExportInternal(
        CompositionViewSnapshot snapshot,
        IReadOnlyList<CompositionExtensionSnapshot> complements,
        CompositionPdfExportOptions? options)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        options ??= new CompositionPdfExportOptions();
        complements ??= Array.Empty<CompositionExtensionSnapshot>();
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        using var canvas = document.BeginPage(options.PageWidth, options.PageHeight);

        RenderSnapshot(canvas, snapshot, options, complements);

        document.EndPage();
        document.Close();
        return stream.ToArray();
    }

    internal static void RenderSnapshot(
        SKCanvas canvas,
        CompositionViewSnapshot snapshot,
        CompositionPdfExportOptions options,
        IReadOnlyList<CompositionExtensionSnapshot>? complements = null,
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

        var complementItems = CompositionViewComplementLayout.Build(
            complements ?? Array.Empty<CompositionExtensionSnapshot>(),
            snapshot.Nodes);
        var bounds = GetBounds(snapshot, complementItems);
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

        DrawComplementRegions(canvas, complementItems, Project, nodeFillPaint, nodeStrokePaint, textPaint, connectorFont);

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
            DrawArrowHead(canvas, from, to, connectorPaint);

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
            DrawWrappedText(
                canvas,
                TrimForPdf(node.Text),
                new SKRect(topLeft.X + 10 * scale, topLeft.Y + 10 * scale, topLeft.X + width - 10 * scale, topLeft.Y + height - 8 * scale),
                titleFont,
                textPaint,
                maxLines: Math.Max(1, (int)(height / Math.Max(1, titleFont.Size + 2))));
        }

        DrawComplementCards(canvas, complementItems, Project, nodeFillPaint, nodeStrokePaint, textPaint, titleFont, connectorFont);
    }

    private static SnapshotBounds GetBounds(
        CompositionViewSnapshot snapshot,
        IReadOnlyList<CompositionComplementRenderItem> complementItems)
    {
        if (snapshot.Nodes.Count == 0 && complementItems.Count == 0)
        {
            return new SnapshotBounds(0, 0, MinimumContentWidth, MinimumContentHeight);
        }

        var left = snapshot.Nodes.Count == 0 ? 0 : snapshot.Nodes.Min(node => node.Position.X);
        var top = snapshot.Nodes.Count == 0 ? 0 : snapshot.Nodes.Min(node => node.Position.Y);
        var right = snapshot.Nodes.Count == 0 ? MinimumContentWidth : snapshot.Nodes.Max(node => node.Position.X + Math.Max(1, node.Size.Width));
        var bottom = snapshot.Nodes.Count == 0 ? MinimumContentHeight : snapshot.Nodes.Max(node => node.Position.Y + Math.Max(1, node.Size.Height));

        foreach (var item in complementItems)
        {
            left = Math.Min(left, item.Position.X);
            top = Math.Min(top, item.Position.Y);
            right = Math.Max(right, item.Position.X + Math.Max(1, item.Size.Width));
            bottom = Math.Max(bottom, item.Position.Y + Math.Max(1, item.Size.Height));
        }

        return new SnapshotBounds(
            left - 12,
            top - 12,
            right + 12,
            bottom + 12);
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

    private static void DrawComplementRegions(
        SKCanvas canvas,
        IReadOnlyList<CompositionComplementRenderItem> items,
        Func<double, double, SKPoint> project,
        SKPaint fillPaint,
        SKPaint strokePaint,
        SKPaint textPaint,
        SKFont font)
    {
        foreach (var item in items.Where(item => item.Kind == "Group"))
        {
            var rect = ProjectRect(item, project);
            using var roundRect = new SKRoundRect(rect, 8, 8);
            fillPaint.Color = WithOpacity(ParseColor(item.Style.Fill, WithAlpha(new SKColor(43, 120, 198), 28)), item.Style.Opacity);
            strokePaint.Color = WithOpacity(ParseColor(item.Style.Stroke, WithAlpha(new SKColor(43, 120, 198), 135)), item.Style.Opacity);
            strokePaint.StrokeWidth = Math.Max(0.5f, (float)PositiveOrDefault(item.Style.StrokeThickness, 1.2));
            canvas.DrawRoundRect(roundRect, fillPaint);
            canvas.DrawRoundRect(roundRect, strokePaint);

            textPaint.Color = WithOpacity(ParseColor(item.Style.Text, ParseColor(item.Style.Stroke, new SKColor(43, 120, 198))), item.Style.Opacity);
            using var styledFont = CreateComplementFont(item.Style, font);
            DrawWrappedText(
                canvas,
                PrefixIcon(item),
                new SKRect(rect.Left + 10, rect.Top + 8, rect.Right - 10, rect.Top + 28),
                styledFont,
                textPaint,
                maxLines: 1);
        }
    }

    private static void DrawComplementCards(
        SKCanvas canvas,
        IReadOnlyList<CompositionComplementRenderItem> items,
        Func<double, double, SKPoint> project,
        SKPaint fillPaint,
        SKPaint strokePaint,
        SKPaint textPaint,
        SKFont titleFont,
        SKFont bodyFont)
    {
        foreach (var item in items.Where(item => item.Kind != "Group"))
        {
            var rect = ProjectRect(item, project);
            using var roundRect = new SKRoundRect(rect, 7, 7);
            var defaultStroke = item.Kind == "Quote"
                ? new SKColor(147, 51, 234)
                : new SKColor(43, 120, 198);
            fillPaint.Color = WithOpacity(ParseColor(item.Style.Fill, new SKColor(248, 250, 252)), item.Style.Opacity);
            strokePaint.Color = WithOpacity(ParseColor(item.Style.Stroke, defaultStroke), item.Style.Opacity);
            strokePaint.StrokeWidth = Math.Max(0.5f, (float)PositiveOrDefault(item.Style.StrokeThickness, 1.1));
            canvas.DrawRoundRect(roundRect, fillPaint);
            canvas.DrawRoundRect(roundRect, strokePaint);

            textPaint.Color = WithOpacity(ParseColor(item.Style.Text, new SKColor(31, 35, 40)), item.Style.Opacity);
            using var styledTitleFont = CreateComplementFont(item.Style, titleFont);
            DrawWrappedText(
                canvas,
                PrefixIcon(item),
                new SKRect(rect.Left + 10, rect.Top + 8, rect.Right - 10, rect.Top + 28),
                styledTitleFont,
                textPaint,
                maxLines: 1);

            textPaint.Color = WithOpacity(ParseColor(item.Style.Text, new SKColor(95, 107, 122)), item.Style.Opacity);
            using var styledBodyFont = CreateComplementFont(item.Style, bodyFont);
            DrawWrappedText(
                canvas,
                string.IsNullOrWhiteSpace(item.Body) ? item.Key : item.Body,
                new SKRect(rect.Left + 10, rect.Top + 32, rect.Right - 10, rect.Bottom - 8),
                styledBodyFont,
                textPaint,
                maxLines: Math.Max(1, (int)((rect.Height - 40) / Math.Max(1, styledBodyFont.Size + 2))));
        }
    }

    private static SKRect ProjectRect(
        CompositionComplementRenderItem item,
        Func<double, double, SKPoint> project)
    {
        var topLeft = project(item.Position.X, item.Position.Y);
        var bottomRight = project(item.Position.X + item.Size.Width, item.Position.Y + item.Size.Height);
        return new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private static void DrawArrowHead(SKCanvas canvas, SKPoint from, SKPoint to, SKPaint paint)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1)
        {
            return;
        }

        var ux = (float)(dx / length);
        var uy = (float)(dy / length);
        var size = Math.Max(6, paint.StrokeWidth * 3.5f);
        var wing = size * 0.55f;
        var baseX = to.X - ux * size;
        var baseY = to.Y - uy * size;
        var left = new SKPoint(baseX - uy * wing, baseY + ux * wing);
        var right = new SKPoint(baseX + uy * wing, baseY - ux * wing);

        using var path = new SKPath();
        path.MoveTo(to);
        path.LineTo(left);
        path.LineTo(right);
        path.Close();

        using var fill = new SKPaint
        {
            IsAntialias = paint.IsAntialias,
            Color = paint.Color,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPath(path, fill);
    }

    private static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKRect rect,
        SKFont font,
        SKPaint paint,
        int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLines <= 0 || rect.Width <= 1)
        {
            return;
        }

        var words = text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        var y = rect.Top + font.Size;
        var lines = 0;

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (font.MeasureText(candidate) <= rect.Width)
            {
                line = candidate;
                continue;
            }

            if (line.Length == 0)
            {
                line = TrimWordToWidth(word, rect.Width, font);
            }

            lines++;
            var isLastLine = lines >= maxLines || y > rect.Bottom;
            canvas.DrawText(isLastLine ? Ellipsize(line, rect.Width, font) : line, rect.Left, y, font, paint);
            if (isLastLine)
            {
                return;
            }

            y += font.Size + 2;
            line = word;
        }

        if (line.Length > 0 && lines < maxLines && y <= rect.Bottom)
        {
            canvas.DrawText(lines + 1 >= maxLines ? Ellipsize(line, rect.Width, font) : line, rect.Left, y, font, paint);
        }
    }

    private static string TrimWordToWidth(string word, float maxWidth, SKFont font)
    {
        var value = word;
        while (value.Length > 1 && font.MeasureText(value) > maxWidth)
        {
            value = value.Substring(0, value.Length - 1);
        }

        return value;
    }

    private static string Ellipsize(string text, float maxWidth, SKFont font)
    {
        if (font.MeasureText(text) <= maxWidth)
        {
            return text;
        }

        var suffix = "...";
        var value = text;
        while (value.Length > 1 && font.MeasureText(value + suffix) > maxWidth)
        {
            value = value.Substring(0, value.Length - 1);
        }

        return value + suffix;
    }

    private static SKColor WithAlpha(SKColor color, byte alpha)
    {
        return new SKColor(color.Red, color.Green, color.Blue, alpha);
    }

    private static SKColor WithOpacity(SKColor color, double opacity)
    {
        var factor = double.IsNaN(opacity) ? 1 : Math.Max(0, Math.Min(1, opacity));
        return new SKColor(color.Red, color.Green, color.Blue, (byte)Math.Round(color.Alpha * factor));
    }

    private static SKFont CreateComplementFont(CompositionComplementStyle style, SKFont fallback)
    {
        var typeface = string.IsNullOrWhiteSpace(style.FontFamily)
            ? fallback.Typeface
            : SKTypeface.FromFamilyName(style.FontFamily);
        return new SKFont(typeface, style.FontSize > 0 ? (float)style.FontSize : fallback.Size)
        {
            Edging = SKFontEdging.Antialias
        };
    }

    private static string PrefixIcon(CompositionComplementRenderItem item)
    {
        return string.IsNullOrWhiteSpace(item.Style.Icon)
            ? item.Title
            : $"{item.Style.Icon.Trim()}  {item.Title}";
    }

    private readonly record struct SnapshotBounds(double Left, double Top, double Right, double Bottom);
}
