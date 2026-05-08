using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentSnapshotAdapter
{
    private const string DefaultConceptDefinitionId = "concept.default";
    private const string DefaultRelationshipDefinitionId = "relationship.default";
    private const string DefaultViewId = "view.default";

    public static CompositionDocumentSnapshot FromViewSnapshot(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var domain = new CompositionDomainSnapshot(
            Id: "domain.default",
            Name: "Default Domain",
            Summary: "Projected from a WinUI view snapshot.",
            ConceptDefinitions:
            [
                new CompositionDefinitionSnapshot(DefaultConceptDefinitionId, "Concept", "Concept")
            ],
            RelationshipDefinitions:
            [
                new CompositionDefinitionSnapshot(DefaultRelationshipDefinitionId, "Relationship", "Relationship")
            ]);

        var ideas = snapshot.Nodes
            .Select(node => new CompositionIdeaSnapshot(
                Id: node.Id,
                Name: node.Text,
                DefinitionId: DefaultConceptDefinitionId,
                Style: node.Style))
            .ToArray();

        var relationships = snapshot.Connectors
            .Select(connector => new CompositionRelationshipSnapshot(
                Id: connector.Id,
                Name: connector.Text,
                SourceIdeaId: connector.SourceId,
                TargetIdeaId: connector.TargetId,
                DefinitionId: DefaultRelationshipDefinitionId,
                Style: connector.Style))
            .ToArray();

        return new CompositionDocumentSnapshot(
            Id: snapshot.Id,
            Title: snapshot.Title,
            Domain: domain,
            Ideas: ideas,
            Relationships: relationships,
            Views:
            [
                new CompositionViewLayerSnapshot(DefaultViewId, snapshot.Title, snapshot.Nodes, snapshot.Connectors)
            ],
            Extensions:
            [
                new CompositionExtensionSnapshot("source.contract", nameof(CompositionViewSnapshot))
            ]);
    }

    public static CompositionViewSnapshot ToViewSnapshot(CompositionDocumentSnapshot document, string? viewId = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var view = SelectView(document, viewId);
        if (view is not null)
        {
            return string.IsNullOrWhiteSpace(viewId)
                ? new CompositionViewSnapshot(document.Id, document.Title, view.Nodes, view.Connectors)
                : new CompositionViewSnapshot(view.Id, view.Name, view.Nodes, view.Connectors);
        }

        var nodes = document.Ideas
            .Select((idea, index) => new CompositionNodeView(
                idea.Id,
                idea.Name,
                new TcPoint(120 + (index % 4 * 240), 120 + (index / 4 * 160)),
                new TcSize(164, 82),
                idea.Style))
            .ToArray();

        var connectors = document.Relationships
            .Select(relationship => new CompositionConnectorView(
                relationship.Id,
                relationship.SourceIdeaId,
                relationship.TargetIdeaId,
                relationship.Name,
                relationship.Style))
            .ToArray();

        return new CompositionViewSnapshot(document.Id, document.Title, nodes, connectors);
    }

    private static CompositionViewLayerSnapshot? SelectView(CompositionDocumentSnapshot document, string? viewId)
    {
        if (!string.IsNullOrWhiteSpace(viewId))
        {
            return document.Views.FirstOrDefault(view => string.Equals(view.Id, viewId, StringComparison.Ordinal));
        }

        return document.Views.FirstOrDefault();
    }
}
