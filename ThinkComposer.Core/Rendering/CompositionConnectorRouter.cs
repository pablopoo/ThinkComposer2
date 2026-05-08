using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionConnectorRouter
{
    public static CompositionConnectorRoute Route(CompositionNodeView source, CompositionNodeView target)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var sourceCenter = CenterOf(source);
        var targetCenter = CenterOf(target);
        var dx = targetCenter.X - sourceCenter.X;
        var dy = targetCenter.Y - sourceCenter.Y;

        if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
        {
            return new CompositionConnectorRoute(sourceCenter, targetCenter);
        }

        return new CompositionConnectorRoute(
            EdgePoint(sourceCenter, source.Size, dx, dy),
            EdgePoint(targetCenter, target.Size, -dx, -dy));
    }

    private static TcPoint CenterOf(CompositionNodeView node)
    {
        return new TcPoint(
            node.Position.X + Math.Max(1.0, node.Size.Width) / 2,
            node.Position.Y + Math.Max(1.0, node.Size.Height) / 2);
    }

    private static TcPoint EdgePoint(TcPoint center, TcSize size, double dx, double dy)
    {
        var halfWidth = Math.Max(1.0, size.Width) / 2;
        var halfHeight = Math.Max(1.0, size.Height) / 2;
        var xScale = Math.Abs(dx) < double.Epsilon ? double.PositiveInfinity : halfWidth / Math.Abs(dx);
        var yScale = Math.Abs(dy) < double.Epsilon ? double.PositiveInfinity : halfHeight / Math.Abs(dy);
        var scale = Math.Min(xScale, yScale);

        return new TcPoint(
            center.X + dx * scale,
            center.Y + dy * scale);
    }
}
