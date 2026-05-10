using System.Globalization;
using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionViewComplementLayout
{
    public static IReadOnlyList<CompositionComplementRenderItem> Build(
        IReadOnlyList<CompositionExtensionSnapshot> complements,
        IReadOnlyList<CompositionNodeView> nodes)
    {
        if (complements.Count == 0)
        {
            return Array.Empty<CompositionComplementRenderItem>();
        }

        var bounds = GetNodeBounds(nodes);
        var cardIndex = 0;
        var items = new List<CompositionComplementRenderItem>();
        foreach (var complement in complements)
        {
            var kind = Classify(complement);
            var title = TitleFromComplement(complement);
            var style = ReadStyle(complement);
            if (TryReadExplicitLayout(complement, out var position, out var size))
            {
                items.Add(new CompositionComplementRenderItem(
                    complement.Key,
                    title,
                    complement.Value,
                    kind,
                    position,
                    size,
                    style));
                continue;
            }

            if (kind == "Group")
            {
                items.Add(new CompositionComplementRenderItem(
                    complement.Key,
                    title,
                    complement.Value,
                    kind,
                    new TcPoint(bounds.Left - 28, bounds.Top - 28),
                    new TcSize(Math.Max(180, bounds.Width + 56), Math.Max(120, bounds.Height + 56)),
                    style));
                continue;
            }

            items.Add(new CompositionComplementRenderItem(
                complement.Key,
                title,
                complement.Value,
                kind,
                new TcPoint(bounds.Right + 32, bounds.Top + (cardIndex * 92)),
                new TcSize(220, kind == "Legend" ? 104 : 84),
                style));
            cardIndex++;
        }

        return items;
    }

    private static (double Left, double Top, double Right, double Bottom, double Width, double Height) GetNodeBounds(
        IReadOnlyList<CompositionNodeView> nodes)
    {
        if (nodes.Count == 0)
        {
            return (80, 80, 360, 240, 280, 160);
        }

        var left = nodes.Min(node => node.Position.X);
        var top = nodes.Min(node => node.Position.Y);
        var right = nodes.Max(node => node.Position.X + node.Size.Width);
        var bottom = nodes.Max(node => node.Position.Y + node.Size.Height);
        return (left, top, right, bottom, right - left, bottom - top);
    }

    private static string Classify(CompositionExtensionSnapshot complement)
    {
        var kind = TryGetProperty(complement, "kind") ?? complement.Key;
        if (kind.StartsWith("group.", StringComparison.OrdinalIgnoreCase) ||
            kind.IndexOf("group", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Group";
        }

        if (kind.IndexOf("legend", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Legend";
        }

        if (kind.IndexOf("quote", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Quote";
        }

        return "Info";
    }

    private static string TitleFromComplement(CompositionExtensionSnapshot complement)
    {
        var title = TryGetProperty(complement, "title") ?? complement.Key;
        var dot = title.LastIndexOf('.');
        if (dot >= 0 && dot + 1 < title.Length)
        {
            title = title.Substring(dot + 1);
        }

        title = title.Replace('-', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(title)
            ? "Complement"
            : char.ToUpperInvariant(title[0]) + title.Substring(1);
    }

    private static bool TryReadExplicitLayout(
        CompositionExtensionSnapshot complement,
        out TcPoint position,
        out TcSize size)
    {
        position = default;
        size = default;

        if (!TryReadDouble(complement, "x", out var x) ||
            !TryReadDouble(complement, "y", out var y) ||
            !TryReadDouble(complement, "width", out var width) ||
            !TryReadDouble(complement, "height", out var height))
        {
            return false;
        }

        position = new TcPoint(x, y);
        size = new TcSize(width, height);
        return true;
    }

    private static bool TryReadDouble(
        CompositionExtensionSnapshot complement,
        string key,
        out double value)
    {
        value = 0;
        var rawValue = TryGetProperty(complement, key);
        return rawValue is not null &&
            double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static CompositionComplementStyle ReadStyle(CompositionExtensionSnapshot complement)
    {
        return new CompositionComplementStyle(
            Fill: FirstProperty(complement, "fill", "background", "backgroundColor"),
            Stroke: FirstProperty(complement, "stroke", "border", "borderColor"),
            Text: FirstProperty(complement, "text", "foreground", "foregroundColor", "textColor"),
            Opacity: ReadClampedDouble(complement, 1, "opacity", "alpha"),
            StrokeThickness: ReadPositiveDouble(complement, 0, "strokeThickness", "borderThickness", "borderWidth"),
            FontFamily: FirstProperty(complement, "font", "fontFamily"),
            FontSize: ReadPositiveDouble(complement, 0, "fontSize"),
            Icon: FirstProperty(complement, "icon"));
    }

    private static string FirstProperty(CompositionExtensionSnapshot complement, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = TryGetProperty(complement, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value!.Trim();
            }
        }

        return string.Empty;
    }

    private static double ReadClampedDouble(
        CompositionExtensionSnapshot complement,
        double fallback,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (TryReadDouble(complement, key, out var value) && value >= 0 && value <= 1)
            {
                return value;
            }
        }

        return fallback;
    }

    private static double ReadPositiveDouble(
        CompositionExtensionSnapshot complement,
        double fallback,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (TryReadDouble(complement, key, out var value) && value > 0)
            {
                return value;
            }
        }

        return fallback;
    }

    private static string? TryGetProperty(CompositionExtensionSnapshot complement, string key)
    {
        if (complement.Properties.TryGetValue(key, out var value))
        {
            return value;
        }

        foreach (var property in complement.Properties)
        {
            if (string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }
}
