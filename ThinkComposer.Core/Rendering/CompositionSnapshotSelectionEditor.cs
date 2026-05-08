using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotSelectionEditor
{
    public static CompositionSnapshotSelection Copy(CompositionViewSnapshot snapshot, IReadOnlyCollection<string> nodeIds)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (nodeIds is null)
        {
            throw new ArgumentNullException(nameof(nodeIds));
        }

        var selectedNodeIds = nodeIds.ToHashSet(StringComparer.Ordinal);
        var nodes = snapshot.Nodes
            .Where(node => selectedNodeIds.Contains(node.Id))
            .ToArray();
        var connectors = snapshot.Connectors
            .Where(connector => selectedNodeIds.Contains(connector.SourceId) && selectedNodeIds.Contains(connector.TargetId))
            .ToArray();

        return new CompositionSnapshotSelection(nodes, connectors);
    }

    public static CompositionViewSnapshot Paste(
        CompositionViewSnapshot snapshot,
        CompositionSnapshotSelection selection,
        TcPoint offset)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var usedIds = snapshot.Nodes.Select(node => node.Id)
            .Concat(snapshot.Connectors.Select(connector => connector.Id))
            .ToHashSet(StringComparer.Ordinal);
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);

        var pastedNodes = selection.Nodes
            .Select(node =>
            {
                var nextId = CreateCopyId(node.Id, usedIds);
                idMap[node.Id] = nextId;
                usedIds.Add(nextId);
                return node with
                {
                    Id = nextId,
                    Position = new TcPoint(node.Position.X + offset.X, node.Position.Y + offset.Y)
                };
            })
            .ToArray();

        var pastedConnectors = selection.Connectors
            .Where(connector => idMap.ContainsKey(connector.SourceId) && idMap.ContainsKey(connector.TargetId))
            .Select(connector =>
            {
                var nextId = CreateCopyId(connector.Id, usedIds);
                usedIds.Add(nextId);
                return connector with
                {
                    Id = nextId,
                    SourceId = idMap[connector.SourceId],
                    TargetId = idMap[connector.TargetId]
                };
            })
            .ToArray();

        return snapshot with
        {
            Nodes = snapshot.Nodes.Concat(pastedNodes).ToArray(),
            Connectors = snapshot.Connectors.Concat(pastedConnectors).ToArray()
        };
    }

    public static CompositionViewSnapshot Delete(CompositionViewSnapshot snapshot, IReadOnlyCollection<string> nodeIds)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (nodeIds is null)
        {
            throw new ArgumentNullException(nameof(nodeIds));
        }

        var selectedNodeIds = nodeIds.ToHashSet(StringComparer.Ordinal);
        return snapshot with
        {
            Nodes = snapshot.Nodes.Where(node => !selectedNodeIds.Contains(node.Id)).ToArray(),
            Connectors = snapshot.Connectors
                .Where(connector => !selectedNodeIds.Contains(connector.SourceId) && !selectedNodeIds.Contains(connector.TargetId))
                .ToArray()
        };
    }

    private static string CreateCopyId(string baseId, ISet<string> usedIds)
    {
        var candidate = $"{baseId}-copy";
        if (!usedIds.Contains(candidate))
        {
            return candidate;
        }

        for (var index = 2; ; index++)
        {
            candidate = $"{baseId}-copy-{index}";
            if (!usedIds.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
