using System.Globalization;
using System.Xml.Linq;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentSnapshotXmlStore
{
    public static CompositionDocumentSnapshot Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Document path is required.", nameof(filePath));
        }

        var root = XDocument.Load(filePath).Root ?? throw new InvalidDataException("Document root is missing.");
        return new CompositionDocumentSnapshot(
            ReadString(root, "id"),
            ReadString(root, "title"),
            ReadDomain(root.Element("Domain")),
            ReadIdeas(root.Element("Ideas")),
            ReadRelationships(root.Element("Relationships")),
            ReadViews(root.Element("Views")),
            ReadExtensions(root.Element("Extensions")),
            ReadInt(root, "schemaVersion", CompositionDocumentSnapshot.CurrentSchemaVersion));
    }

    public static void Save(CompositionDocumentSnapshot document, string filePath)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Document path is required.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        new XDocument(WriteDocument(document)).Save(filePath);
    }

    private static XElement WriteDocument(CompositionDocumentSnapshot document)
    {
        return new XElement(
            "CompositionDocument",
            new XAttribute("schemaVersion", document.SchemaVersion),
            new XAttribute("id", document.Id),
            new XAttribute("title", document.Title),
            WriteDomain(document.Domain),
            new XElement("Ideas", document.Ideas.Select(WriteIdea)),
            new XElement("Relationships", document.Relationships.Select(WriteRelationship)),
            new XElement("Views", document.Views.Select(WriteView)),
            WriteExtensions("Extensions", document.Extensions));
    }

    private static XElement WriteDomain(CompositionDomainSnapshot domain)
    {
        return new XElement(
            "Domain",
            new XAttribute("id", domain.Id),
            new XAttribute("name", domain.Name),
            new XAttribute("summary", domain.Summary),
            new XElement("ConceptDefinitions", domain.ConceptDefinitions.Select(WriteDefinition)),
            new XElement("RelationshipDefinitions", domain.RelationshipDefinitions.Select(WriteDefinition)),
            new XElement("LinkRoleDefinitions", domain.LinkRoleDefinitions.Select(WriteDefinition)),
            new XElement("MarkerDefinitions", domain.MarkerDefinitions.Select(WriteDefinition)),
            new XElement("TableDefinitions", domain.TableDefinitions.Select(WriteDefinition)),
            new XElement("ExternalLanguages", domain.ExternalLanguages.Select(WriteDefinition)),
            WriteExtensions("Templates", domain.Templates),
            WriteExtensions("Extensions", domain.Extensions));
    }

    private static XElement WriteDefinition(CompositionDefinitionSnapshot definition)
    {
        return new XElement(
            "Definition",
            new XAttribute("id", definition.Id),
            new XAttribute("name", definition.Name),
            new XAttribute("kind", definition.Kind),
            new XAttribute("summary", definition.Summary),
            WriteStyle(definition.Style),
            WriteDetails(definition.Details),
            WriteExtensions("Extensions", definition.Extensions));
    }

    private static XElement WriteIdea(CompositionIdeaSnapshot idea)
    {
        return new XElement(
            "Idea",
            new XAttribute("id", idea.Id),
            new XAttribute("name", idea.Name),
            new XAttribute("definitionId", idea.DefinitionId),
            new XAttribute("summary", idea.Summary),
            new XElement("Markers", idea.Markers.Select(marker => new XElement("Marker", new XAttribute("id", marker)))),
            WriteStyle(idea.Style),
            WriteDetails(idea.Details),
            WriteExtensions("Extensions", idea.Extensions));
    }

    private static XElement WriteRelationship(CompositionRelationshipSnapshot relationship)
    {
        return new XElement(
            "Relationship",
            new XAttribute("id", relationship.Id),
            new XAttribute("name", relationship.Name),
            new XAttribute("sourceIdeaId", relationship.SourceIdeaId),
            new XAttribute("targetIdeaId", relationship.TargetIdeaId),
            new XAttribute("definitionId", relationship.DefinitionId),
            new XAttribute("linkRoleId", relationship.LinkRoleId),
            new XElement("Markers", relationship.Markers.Select(marker => new XElement("Marker", new XAttribute("id", marker)))),
            WriteStyle(relationship.Style),
            WriteDetails(relationship.Details),
            WriteExtensions("Extensions", relationship.Extensions));
    }

    private static XElement WriteView(CompositionViewLayerSnapshot view)
    {
        return new XElement(
            "View",
            new XAttribute("id", view.Id),
            new XAttribute("name", view.Name),
            new XElement("Nodes", view.Nodes.Select(WriteNode)),
            new XElement("Connectors", view.Connectors.Select(WriteConnector)),
            WriteExtensions("Complements", view.Complements),
            WriteStyle(view.Style),
            WriteExtensions("Extensions", view.Extensions));
    }

    private static XElement WriteNode(CompositionNodeView node)
    {
        return new XElement(
            "Node",
            new XAttribute("id", node.Id),
            new XAttribute("text", node.Text),
            new XAttribute("x", Format(node.Position.X)),
            new XAttribute("y", Format(node.Position.Y)),
            new XAttribute("width", Format(node.Size.Width)),
            new XAttribute("height", Format(node.Size.Height)));
    }

    private static XElement WriteConnector(CompositionConnectorView connector)
    {
        return new XElement(
            "Connector",
            new XAttribute("id", connector.Id),
            new XAttribute("sourceId", connector.SourceId),
            new XAttribute("targetId", connector.TargetId),
            new XAttribute("text", connector.Text));
    }

    private static XElement WriteDetails(IReadOnlyList<CompositionDetailSnapshot> details)
    {
        return new XElement("Details", details.Select(detail =>
            new XElement(
                "Detail",
                new XAttribute("id", detail.Id),
                new XAttribute("kind", detail.Kind),
                new XAttribute("name", detail.Name),
                new XAttribute("value", detail.Value),
                WriteExtensions("Extensions", detail.Extensions))));
    }

    private static XElement WriteStyle(CompositionStyleSnapshot style)
    {
        return new XElement(
            "Style",
            new XAttribute("fill", style.Fill),
            new XAttribute("stroke", style.Stroke),
            new XAttribute("text", style.Text),
            new XAttribute("strokeThickness", Format(style.StrokeThickness)),
            new XAttribute("strokeDash", style.StrokeDash),
            new XElement("Properties", style.Properties.Select(property =>
                new XElement("Property", new XAttribute("key", property.Key), new XAttribute("value", property.Value)))));
    }

    private static XElement WriteExtensions(string elementName, IReadOnlyList<CompositionExtensionSnapshot> extensions)
    {
        return new XElement(elementName, extensions.Select(extension =>
            new XElement(
                "Extension",
                new XAttribute("key", extension.Key),
                new XAttribute("value", extension.Value),
                new XElement("Properties", extension.Properties.Select(property =>
                    new XElement("Property", new XAttribute("key", property.Key), new XAttribute("value", property.Value)))))));
    }

    private static CompositionDomainSnapshot ReadDomain(XElement? element)
    {
        if (element is null)
        {
            return new CompositionDomainSnapshot("", "");
        }

        return new CompositionDomainSnapshot(
            ReadString(element, "id"),
            ReadString(element, "name"),
            ReadString(element, "summary"),
            ReadDefinitions(element.Element("ConceptDefinitions")),
            ReadDefinitions(element.Element("RelationshipDefinitions")),
            ReadDefinitions(element.Element("MarkerDefinitions")),
            ReadDefinitions(element.Element("TableDefinitions")),
            ReadDefinitions(element.Element("ExternalLanguages")),
            ReadExtensions(element.Element("Templates")),
            ReadExtensions(element.Element("Extensions")),
            ReadDefinitions(element.Element("LinkRoleDefinitions")));
    }

    private static IReadOnlyList<CompositionDefinitionSnapshot> ReadDefinitions(XElement? container)
    {
        return container?.Elements("Definition").Select(element =>
            new CompositionDefinitionSnapshot(
                ReadString(element, "id"),
                ReadString(element, "name"),
                ReadString(element, "kind"),
                ReadString(element, "summary"),
                ReadStyle(element.Element("Style")),
                ReadDetails(element.Element("Details")),
                ReadExtensions(element.Element("Extensions")))).ToArray()
            ?? Array.Empty<CompositionDefinitionSnapshot>();
    }

    private static IReadOnlyList<CompositionIdeaSnapshot> ReadIdeas(XElement? container)
    {
        return container?.Elements("Idea").Select(element =>
            new CompositionIdeaSnapshot(
                ReadString(element, "id"),
                ReadString(element, "name"),
                ReadString(element, "definitionId"),
                ReadString(element, "summary"),
                ReadDetails(element.Element("Details")),
                ReadMarkers(element.Element("Markers")),
                ReadStyle(element.Element("Style")),
                ReadExtensions(element.Element("Extensions")))).ToArray()
            ?? Array.Empty<CompositionIdeaSnapshot>();
    }

    private static IReadOnlyList<CompositionRelationshipSnapshot> ReadRelationships(XElement? container)
    {
        return container?.Elements("Relationship").Select(element =>
            new CompositionRelationshipSnapshot(
                ReadString(element, "id"),
                ReadString(element, "name"),
                ReadString(element, "sourceIdeaId"),
                ReadString(element, "targetIdeaId"),
                ReadString(element, "definitionId"),
                ReadDetails(element.Element("Details")),
                ReadMarkers(element.Element("Markers")),
                ReadStyle(element.Element("Style")),
                ReadExtensions(element.Element("Extensions")),
                ReadString(element, "linkRoleId"))).ToArray()
            ?? Array.Empty<CompositionRelationshipSnapshot>();
    }

    private static IReadOnlyList<CompositionViewLayerSnapshot> ReadViews(XElement? container)
    {
        return container?.Elements("View").Select(element =>
            new CompositionViewLayerSnapshot(
                ReadString(element, "id"),
                ReadString(element, "name"),
                ReadNodes(element.Element("Nodes")),
                ReadConnectors(element.Element("Connectors")),
                ReadExtensions(element.Element("Complements")),
                ReadStyle(element.Element("Style")),
                ReadExtensions(element.Element("Extensions")))).ToArray()
            ?? Array.Empty<CompositionViewLayerSnapshot>();
    }

    private static IReadOnlyList<CompositionNodeView> ReadNodes(XElement? container)
    {
        return container?.Elements("Node").Select(element =>
            new CompositionNodeView(
                ReadString(element, "id"),
                ReadString(element, "text"),
                new Primitives.TcPoint(ReadDouble(element, "x"), ReadDouble(element, "y")),
                new Primitives.TcSize(ReadDouble(element, "width"), ReadDouble(element, "height")))).ToArray()
            ?? Array.Empty<CompositionNodeView>();
    }

    private static IReadOnlyList<CompositionConnectorView> ReadConnectors(XElement? container)
    {
        return container?.Elements("Connector").Select(element =>
            new CompositionConnectorView(
                ReadString(element, "id"),
                ReadString(element, "sourceId"),
                ReadString(element, "targetId"),
                ReadString(element, "text"))).ToArray()
            ?? Array.Empty<CompositionConnectorView>();
    }

    private static IReadOnlyList<CompositionDetailSnapshot> ReadDetails(XElement? container)
    {
        return container?.Elements("Detail").Select(element =>
            new CompositionDetailSnapshot(
                ReadString(element, "id"),
                ReadString(element, "kind"),
                ReadString(element, "name"),
                ReadString(element, "value"),
                ReadExtensions(element.Element("Extensions")))).ToArray()
            ?? Array.Empty<CompositionDetailSnapshot>();
    }

    private static IReadOnlyList<string> ReadMarkers(XElement? container)
    {
        return container?.Elements("Marker").Select(element => ReadString(element, "id")).ToArray()
            ?? Array.Empty<string>();
    }

    private static CompositionStyleSnapshot ReadStyle(XElement? element)
    {
        if (element is null)
        {
            return new CompositionStyleSnapshot();
        }

        return new CompositionStyleSnapshot(
            ReadString(element, "fill"),
            ReadString(element, "stroke"),
            ReadString(element, "text"),
            ReadDouble(element, "strokeThickness"),
            ReadString(element, "strokeDash"),
            ReadProperties(element.Element("Properties")));
    }

    private static IReadOnlyList<CompositionExtensionSnapshot> ReadExtensions(XElement? container)
    {
        return container?.Elements("Extension").Select(element =>
            new CompositionExtensionSnapshot(
                ReadString(element, "key"),
                ReadString(element, "value"),
                ReadProperties(element.Element("Properties")))).ToArray()
            ?? Array.Empty<CompositionExtensionSnapshot>();
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(XElement? container)
    {
        return container?.Elements("Property")
                .GroupBy(element => ReadString(element, "key"), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => ReadString(group.Last(), "value"), StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static string ReadString(XElement element, string attributeName)
    {
        return element.Attribute(attributeName)?.Value ?? string.Empty;
    }

    private static int ReadInt(XElement element, string attributeName, int defaultValue)
    {
        return int.TryParse(element.Attribute(attributeName)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private static double ReadDouble(XElement element, string attributeName)
    {
        return double.TryParse(element.Attribute(attributeName)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
