using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentMerger
{
    private static readonly TcPoint ImportedViewOffset = new(48, 48);

    public static CompositionDocumentSnapshot Merge(
        CompositionDocumentSnapshot target,
        CompositionDocumentSnapshot incoming)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (incoming is null)
        {
            throw new ArgumentNullException(nameof(incoming));
        }

        var usedIds = target.Ideas.Select(idea => idea.Id)
            .Concat(target.Relationships.Select(relationship => relationship.Id))
            .Concat(target.Views.Select(view => view.Id))
            .ToHashSet(StringComparer.Ordinal);
        var ideaIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var relationshipIdMap = new Dictionary<string, string>(StringComparer.Ordinal);

        var incomingIdeas = incoming.Ideas.Select(idea =>
        {
            var nextId = CreateUniqueId(idea.Id, usedIds);
            usedIds.Add(nextId);
            ideaIdMap[idea.Id] = nextId;
            return idea with { Id = nextId };
        }).ToArray();

        var incomingRelationships = incoming.Relationships.Select(relationship =>
        {
            var nextId = CreateUniqueId(relationship.Id, usedIds);
            usedIds.Add(nextId);
            relationshipIdMap[relationship.Id] = nextId;
            return relationship with
            {
                Id = nextId,
                SourceIdeaId = Remap(ideaIdMap, relationship.SourceIdeaId),
                TargetIdeaId = Remap(ideaIdMap, relationship.TargetIdeaId)
            };
        }).ToArray();

        return target with
        {
            Domain = MergeDomains(target.Domain, incoming.Domain),
            Ideas = target.Ideas.Concat(incomingIdeas).ToArray(),
            Relationships = target.Relationships.Concat(incomingRelationships).ToArray(),
            Views = MergeViews(target, incoming, ideaIdMap, relationshipIdMap, usedIds),
            Extensions = MergeExtensions(target.Extensions, incoming.Extensions)
        };
    }

    private static CompositionDomainSnapshot MergeDomains(
        CompositionDomainSnapshot target,
        CompositionDomainSnapshot incoming)
    {
        return target with
        {
            ConceptDefinitions = MergeDefinitions(target.ConceptDefinitions, incoming.ConceptDefinitions),
            RelationshipDefinitions = MergeDefinitions(target.RelationshipDefinitions, incoming.RelationshipDefinitions),
            MarkerDefinitions = MergeDefinitions(target.MarkerDefinitions, incoming.MarkerDefinitions),
            TableDefinitions = MergeDefinitions(target.TableDefinitions, incoming.TableDefinitions),
            ExternalLanguages = MergeDefinitions(target.ExternalLanguages, incoming.ExternalLanguages),
            Templates = MergeExtensions(target.Templates, incoming.Templates),
            Extensions = MergeExtensions(target.Extensions, incoming.Extensions)
        };
    }

    private static IReadOnlyList<CompositionViewLayerSnapshot> MergeViews(
        CompositionDocumentSnapshot target,
        CompositionDocumentSnapshot incoming,
        IReadOnlyDictionary<string, string> ideaIdMap,
        IReadOnlyDictionary<string, string> relationshipIdMap,
        ISet<string> usedIds)
    {
        var targetViews = target.Views.Count == 0
            ? [ViewFromSnapshot(CompositionDocumentSnapshotAdapter.ToViewSnapshot(target))]
            : target.Views.ToArray();

        if (incoming.Views.Count == 0)
        {
            return targetViews;
        }

        var result = targetViews.ToArray();
        var incomingPrimaryView = RemapView(incoming.Views[0], incoming.Views[0].Id, ideaIdMap, relationshipIdMap, ImportedViewOffset);
        result[0] = result[0] with
        {
            Nodes = result[0].Nodes.Concat(incomingPrimaryView.Nodes).ToArray(),
            Connectors = result[0].Connectors.Concat(incomingPrimaryView.Connectors).ToArray()
        };

        var extraViews = incoming.Views.Skip(1)
            .Select(view =>
            {
                var nextViewId = CreateUniqueId(view.Id, usedIds);
                usedIds.Add(nextViewId);
                return RemapView(view, nextViewId, ideaIdMap, relationshipIdMap, ImportedViewOffset);
            });

        return result.Concat(extraViews).ToArray();
    }

    private static CompositionViewLayerSnapshot RemapView(
        CompositionViewLayerSnapshot view,
        string viewId,
        IReadOnlyDictionary<string, string> ideaIdMap,
        IReadOnlyDictionary<string, string> relationshipIdMap,
        TcPoint offset)
    {
        return view with
        {
            Id = viewId,
            Nodes = view.Nodes.Select(node => node with
            {
                Id = Remap(ideaIdMap, node.Id),
                Position = new TcPoint(node.Position.X + offset.X, node.Position.Y + offset.Y)
            }).ToArray(),
            Connectors = view.Connectors.Select(connector => connector with
            {
                Id = Remap(relationshipIdMap, connector.Id),
                SourceId = Remap(ideaIdMap, connector.SourceId),
                TargetId = Remap(ideaIdMap, connector.TargetId)
            }).ToArray()
        };
    }

    private static CompositionViewLayerSnapshot ViewFromSnapshot(CompositionViewSnapshot snapshot)
    {
        return new CompositionViewLayerSnapshot("view.default", snapshot.Title, snapshot.Nodes, snapshot.Connectors);
    }

    private static IReadOnlyList<CompositionDefinitionSnapshot> MergeDefinitions(
        IReadOnlyList<CompositionDefinitionSnapshot> target,
        IReadOnlyList<CompositionDefinitionSnapshot> incoming)
    {
        return target.Concat(incoming)
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static IReadOnlyList<CompositionExtensionSnapshot> MergeExtensions(
        IReadOnlyList<CompositionExtensionSnapshot> target,
        IReadOnlyList<CompositionExtensionSnapshot> incoming)
    {
        return target.Concat(incoming)
            .GroupBy(extension => extension.Key, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static string Remap(IReadOnlyDictionary<string, string> idMap, string id)
    {
        return idMap.TryGetValue(id, out var mappedId) ? mappedId : id;
    }

    private static string CreateUniqueId(string baseId, ISet<string> usedIds)
    {
        if (!usedIds.Contains(baseId))
        {
            return baseId;
        }

        var candidate = $"{baseId}-merged";
        if (!usedIds.Contains(candidate))
        {
            return candidate;
        }

        for (var index = 2; ; index++)
        {
            candidate = $"{baseId}-merged-{index}";
            if (!usedIds.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
