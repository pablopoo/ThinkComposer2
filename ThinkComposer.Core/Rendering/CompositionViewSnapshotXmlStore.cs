using System.Globalization;
using System.Xml.Linq;
using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionViewSnapshotXmlStore
{
    public static CompositionViewSnapshot Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Snapshot path is required.", nameof(filePath));
        }

        var document = XDocument.Load(filePath);
        var root = document.Root ?? throw new InvalidOperationException("Snapshot XML has no root element.");

        var nodes = ReadElements(root, "Nodes", "Node")
            .Select(node => new CompositionNodeView(
                ReadAttribute(node, "id"),
                ReadAttribute(node, "text"),
                new TcPoint(ReadDouble(node, "x"), ReadDouble(node, "y")),
                new TcSize(ReadDouble(node, "width"), ReadDouble(node, "height"))))
            .ToList();

        var connectors = ReadElements(root, "Connectors", "Connector")
            .Select(connector => new CompositionConnectorView(
                ReadAttribute(connector, "id"),
                ReadAttribute(connector, "sourceId"),
                ReadAttribute(connector, "targetId"),
                ReadOptionalAttribute(connector, "text") ?? "Relationship"))
            .ToList();

        return new CompositionViewSnapshot(
            ReadAttribute(root, "id"),
            ReadAttribute(root, "title"),
            nodes,
            connectors);
    }

    public static void Save(CompositionViewSnapshot snapshot, string filePath)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Snapshot path is required.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new XDocument(
            new XElement(
                "CompositionViewSnapshot",
                new XAttribute("id", snapshot.Id),
                new XAttribute("title", snapshot.Title),
                new XElement(
                    "Nodes",
                    snapshot.Nodes.Select(node =>
                        new XElement(
                            "Node",
                            new XAttribute("id", node.Id),
                            new XAttribute("text", node.Text),
                            new XAttribute("x", FormatDouble(node.Position.X)),
                            new XAttribute("y", FormatDouble(node.Position.Y)),
                            new XAttribute("width", FormatDouble(node.Size.Width)),
                            new XAttribute("height", FormatDouble(node.Size.Height))))),
                new XElement(
                    "Connectors",
                    snapshot.Connectors.Select(connector =>
                        new XElement(
                            "Connector",
                            new XAttribute("id", connector.Id),
                            new XAttribute("text", connector.Text),
                            new XAttribute("sourceId", connector.SourceId),
                            new XAttribute("targetId", connector.TargetId))))));

        document.Save(filePath);
    }

    private static IEnumerable<XElement> ReadElements(XElement root, string containerName, string elementName)
    {
        return root.Element(containerName)?.Elements(elementName) ?? Enumerable.Empty<XElement>();
    }

    private static string ReadAttribute(XElement element, string name)
    {
        return element.Attribute(name)?.Value
            ?? throw new InvalidOperationException($"Snapshot XML is missing required '{name}' attribute.");
    }

    private static string? ReadOptionalAttribute(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }

    private static double ReadDouble(XElement element, string name)
    {
        return double.Parse(ReadAttribute(element, name), CultureInfo.InvariantCulture);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
