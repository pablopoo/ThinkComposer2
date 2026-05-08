namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionViewportFitter
{
    public static CompositionViewportFit FitNodes(
        IEnumerable<CompositionNodeView> nodes,
        double viewportWidth,
        double viewportHeight,
        double margin = 56,
        double minZoom = 0.35,
        double maxZoom = 2.4)
    {
        if (nodes is null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return CompositionViewportFit.Default;
        }

        var nodeList = nodes as IReadOnlyCollection<CompositionNodeView> ?? nodes.ToArray();
        if (nodeList.Count == 0)
        {
            return CompositionViewportFit.Default;
        }

        var left = nodeList.Min(node => node.Position.X);
        var top = nodeList.Min(node => node.Position.Y);
        var right = nodeList.Max(node => node.Position.X + Math.Max(1.0, node.Size.Width));
        var bottom = nodeList.Max(node => node.Position.Y + Math.Max(1.0, node.Size.Height));

        var contentWidth = Math.Max(1.0, right - left);
        var contentHeight = Math.Max(1.0, bottom - top);
        var usableWidth = Math.Max(1.0, viewportWidth - margin * 2);
        var usableHeight = Math.Max(1.0, viewportHeight - margin * 2);

        var lowerZoom = Math.Min(minZoom, maxZoom);
        var upperZoom = Math.Max(minZoom, maxZoom);
        var zoom = Clamp(Math.Min(usableWidth / contentWidth, usableHeight / contentHeight), lowerZoom, upperZoom);

        var centerX = left + contentWidth / 2;
        var centerY = top + contentHeight / 2;
        return new CompositionViewportFit(
            zoom,
            viewportWidth / 2 - centerX * zoom,
            viewportHeight / 2 - centerY * zoom);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
