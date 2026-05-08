namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentSnapshotEditor
{
    public static CompositionDocumentSnapshot UpsertIdeaDetail(
        CompositionDocumentSnapshot document,
        string ideaId,
        CompositionDetailSnapshot detail)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (detail is null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        return document with
        {
            Ideas = document.Ideas.Select(idea => string.Equals(idea.Id, ideaId, StringComparison.Ordinal)
                ? idea with { Details = UpsertDetail(idea.Details, detail) }
                : idea).ToArray()
        };
    }

    public static CompositionDocumentSnapshot DeleteIdeaDetail(
        CompositionDocumentSnapshot document,
        string ideaId,
        string detailId)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document with
        {
            Ideas = document.Ideas.Select(idea => string.Equals(idea.Id, ideaId, StringComparison.Ordinal)
                ? idea with { Details = DeleteDetail(idea.Details, detailId) }
                : idea).ToArray()
        };
    }

    public static CompositionDocumentSnapshot SetIdeaMarkers(
        CompositionDocumentSnapshot document,
        string ideaId,
        IReadOnlyList<string> markers)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (markers is null)
        {
            throw new ArgumentNullException(nameof(markers));
        }

        return document with
        {
            Ideas = document.Ideas.Select(idea => string.Equals(idea.Id, ideaId, StringComparison.Ordinal)
                ? idea with { Markers = NormalizeMarkers(markers) }
                : idea).ToArray()
        };
    }

    public static CompositionDocumentSnapshot UpsertRelationshipDetail(
        CompositionDocumentSnapshot document,
        string relationshipId,
        CompositionDetailSnapshot detail)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (detail is null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        return document with
        {
            Relationships = document.Relationships.Select(relationship =>
                string.Equals(relationship.Id, relationshipId, StringComparison.Ordinal)
                    ? relationship with { Details = UpsertDetail(relationship.Details, detail) }
                    : relationship).ToArray()
        };
    }

    public static CompositionDocumentSnapshot DeleteRelationshipDetail(
        CompositionDocumentSnapshot document,
        string relationshipId,
        string detailId)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document with
        {
            Relationships = document.Relationships.Select(relationship =>
                string.Equals(relationship.Id, relationshipId, StringComparison.Ordinal)
                    ? relationship with { Details = DeleteDetail(relationship.Details, detailId) }
                    : relationship).ToArray()
        };
    }

    public static CompositionDocumentSnapshot SetRelationshipMarkers(
        CompositionDocumentSnapshot document,
        string relationshipId,
        IReadOnlyList<string> markers)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (markers is null)
        {
            throw new ArgumentNullException(nameof(markers));
        }

        return document with
        {
            Relationships = document.Relationships.Select(relationship =>
                string.Equals(relationship.Id, relationshipId, StringComparison.Ordinal)
                    ? relationship with { Markers = NormalizeMarkers(markers) }
                    : relationship).ToArray()
        };
    }

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

    private static IReadOnlyList<CompositionDetailSnapshot> UpsertDetail(
        IReadOnlyList<CompositionDetailSnapshot> details,
        CompositionDetailSnapshot detail)
    {
        var detailId = string.IsNullOrWhiteSpace(detail.Id) ? detail.Name : detail.Id;
        var nextDetail = string.Equals(detail.Id, detailId, StringComparison.Ordinal)
            ? detail
            : detail with { Id = detailId };
        var replaced = false;
        var result = details.Select(existing =>
        {
            var matches = string.Equals(existing.Id, detailId, StringComparison.Ordinal) ||
                (!string.IsNullOrWhiteSpace(nextDetail.Name) &&
                    string.Equals(existing.Name, nextDetail.Name, StringComparison.Ordinal));
            if (!matches)
            {
                return existing;
            }

            replaced = true;
            return nextDetail;
        }).ToList();

        if (!replaced)
        {
            result.Add(nextDetail);
        }

        return result.ToArray();
    }

    private static IReadOnlyList<CompositionDetailSnapshot> DeleteDetail(
        IReadOnlyList<CompositionDetailSnapshot> details,
        string detailId)
    {
        return details
            .Where(detail => !string.Equals(detail.Id, detailId, StringComparison.Ordinal))
            .ToArray();
    }

    private static IReadOnlyList<string> NormalizeMarkers(IReadOnlyList<string> markers)
    {
        return markers
            .Where(marker => !string.IsNullOrWhiteSpace(marker))
            .Select(marker => marker.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
