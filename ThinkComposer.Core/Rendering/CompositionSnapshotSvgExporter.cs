using System.Globalization;
using System.Xml.Linq;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotSvgExporter
{
    public static string Export(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var bounds = GetBounds(snapshot);
        const double margin = 48;
        var offsetX = margin - bounds.Left;
        var offsetY = margin - bounds.Top;
        var width = Math.Max(320, bounds.Right - bounds.Left + margin * 2);
        var height = Math.Max(240, bounds.Bottom - bounds.Top + margin * 2);
        var nodesById = snapshot.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        XNamespace svg = "http://www.w3.org/2000/svg";
        var root = new XElement(
            svg + "svg",
            new XAttribute("xmlns", svg.NamespaceName),
            new XAttribute("width", Format(width)),
            new XAttribute("height", Format(height)),
            new XAttribute("viewBox", $"0 0 {Format(width)} {Format(height)}"),
            new XElement(svg + "rect",
                new XAttribute("width", "100%"),
                new XAttribute("height", "100%"),
                new XAttribute("fill", "#ffffff")));

        foreach (var connector in snapshot.Connectors)
        {
            if (!nodesById.TryGetValue(connector.SourceId, out var source) ||
                !nodesById.TryGetValue(connector.TargetId, out var target))
            {
                continue;
            }

            var route = CompositionConnectorRouter.Route(source, target);
            root.Add(new XElement(svg + "line",
                new XAttribute("x1", Format(route.Source.X + offsetX)),
                new XAttribute("y1", Format(route.Source.Y + offsetY)),
                new XAttribute("x2", Format(route.Target.X + offsetX)),
                new XAttribute("y2", Format(route.Target.Y + offsetY)),
                new XAttribute("stroke", "#2b78c6"),
                new XAttribute("stroke-width", "1.4")));
        }

        foreach (var node in snapshot.Nodes)
        {
            var x = node.Position.X + offsetX;
            var y = node.Position.Y + offsetY;
            var nodeWidth = Math.Max(20, node.Size.Width);
            var nodeHeight = Math.Max(20, node.Size.Height);
            root.Add(new XElement(svg + "rect",
                new XAttribute("x", Format(x)),
                new XAttribute("y", Format(y)),
                new XAttribute("width", Format(nodeWidth)),
                new XAttribute("height", Format(nodeHeight)),
                new XAttribute("rx", "7"),
                new XAttribute("fill", "#ffffff"),
                new XAttribute("stroke", "#8a9ba8"),
                new XAttribute("stroke-width", "1")));
            root.Add(new XElement(svg + "text",
                new XAttribute("x", Format(x + 10)),
                new XAttribute("y", Format(y + Math.Min(nodeHeight / 2 + 5, 22))),
                new XAttribute("font-family", "Segoe UI, Arial, sans-serif"),
                new XAttribute("font-size", "12"),
                new XAttribute("font-weight", "600"),
                new XAttribute("fill", "#1f1f1f"),
                node.Text));
        }

        return new XDocument(root).ToString(SaveOptions.DisableFormatting);
    }

    private static SnapshotBounds GetBounds(CompositionViewSnapshot snapshot)
    {
        if (snapshot.Nodes.Count == 0)
        {
            return new SnapshotBounds(0, 0, 320, 240);
        }

        return new SnapshotBounds(
            snapshot.Nodes.Min(node => node.Position.X),
            snapshot.Nodes.Min(node => node.Position.Y),
            snapshot.Nodes.Max(node => node.Position.X + Math.Max(1, node.Size.Width)),
            snapshot.Nodes.Max(node => node.Position.Y + Math.Max(1, node.Size.Height)));
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private readonly record struct SnapshotBounds(double Left, double Top, double Right, double Bottom);
}
