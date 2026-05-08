namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentSnapshotEditor
{
    public static CompositionDocumentSnapshot ApplyViewSnapshot(
        CompositionDocumentSnapshot document,
        CompositionViewSnapshot viewSnapshot,
        string? viewId = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (viewSnapshot is null)
        {
            throw new ArgumentNullException(nameof(viewSnapshot));
        }

        var ideasById = document.Ideas.ToDictionary(idea => idea.Id, StringComparer.Ordinal);
        var relationshipsById = document.Relationships.ToDictionary(relationship => relationship.Id, StringComparer.Ordinal);
        var defaultConceptDefinitionId = document.Domain.ConceptDefinitions.FirstOrDefault()?.Id ?? string.Empty;
        var defaultRelationshipDefinitionId = document.Domain.RelationshipDefinitions.FirstOrDefault()?.Id ?? string.Empty;

        var ideas = viewSnapshot.Nodes
            .Select(node => ideasById.TryGetValue(node.Id, out var existing)
                ? existing with { Name = node.Text }
                : new CompositionIdeaSnapshot(node.Id, node.Text, defaultConceptDefinitionId))
            .ToArray();

        var relationships = viewSnapshot.Connectors
            .Select(connector => relationshipsById.TryGetValue(connector.Id, out var existing)
                ? existing with
                {
                    Name = connector.Text,
                    SourceIdeaId = connector.SourceId,
                    TargetIdeaId = connector.TargetId
                }
                : new CompositionRelationshipSnapshot(
                    connector.Id,
                    connector.Text,
                    connector.SourceId,
                    connector.TargetId,
                    defaultRelationshipDefinitionId))
            .ToArray();

        var views = ApplyView(document, viewSnapshot, viewId);

        return document with
        {
            Title = viewSnapshot.Title,
            Ideas = ideas,
            Relationships = relationships,
            Views = views
        };
    }

    private static IReadOnlyList<CompositionViewLayerSnapshot> ApplyView(
        CompositionDocumentSnapshot document,
        CompositionViewSnapshot viewSnapshot,
        string? viewId)
    {
        if (document.Views.Count == 0)
        {
            return
            [
                new CompositionViewLayerSnapshot("view.default", viewSnapshot.Title, viewSnapshot.Nodes, viewSnapshot.Connectors)
            ];
        }

        var targetIndex = string.IsNullOrWhiteSpace(viewId)
            ? 0
            : document.Views
                .Select((view, index) => new { view, index })
                .FirstOrDefault(item => string.Equals(item.view.Id, viewId, StringComparison.Ordinal))
                ?.index ?? 0;

        var views = document.Views.ToArray();
        var targetView = views[targetIndex];
        views[targetIndex] = targetView with
        {
            Name = viewSnapshot.Title,
            Nodes = viewSnapshot.Nodes,
            Connectors = viewSnapshot.Connectors
        };
        return views;
    }
}
