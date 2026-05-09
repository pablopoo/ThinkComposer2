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

    public static CompositionViewSnapshot Move(
        CompositionViewSnapshot snapshot,
        IReadOnlyCollection<string> nodeIds,
        TcPoint delta)
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
            Nodes = snapshot.Nodes.Select(node => selectedNodeIds.Contains(node.Id)
                ? node with { Position = new TcPoint(node.Position.X + delta.X, node.Position.Y + delta.Y) }
                : node).ToArray()
        };
    }

    public static CompositionViewSnapshot Align(
        CompositionViewSnapshot snapshot,
        IReadOnlyCollection<string> nodeIds,
        CompositionSelectionAlignment alignment)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var selectedNodes = GetSelectedNodes(snapshot, nodeIds);
        if (selectedNodes.Count < 2)
        {
            return snapshot;
        }

        var left = selectedNodes.Min(node => node.Position.X);
        var top = selectedNodes.Min(node => node.Position.Y);
        var right = selectedNodes.Max(node => node.Position.X + node.Size.Width);
        var bottom = selectedNodes.Max(node => node.Position.Y + node.Size.Height);
        var center = left + (right - left) / 2;
        var middle = top + (bottom - top) / 2;
        var selectedNodeIds = selectedNodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);

        return snapshot with
        {
            Nodes = snapshot.Nodes.Select(node =>
            {
                if (!selectedNodeIds.Contains(node.Id))
                {
                    return node;
                }

                var position = alignment switch
                {
                    CompositionSelectionAlignment.Top => new TcPoint(node.Position.X, top),
                    CompositionSelectionAlignment.Left => new TcPoint(left, node.Position.Y),
                    CompositionSelectionAlignment.Right => new TcPoint(right - node.Size.Width, node.Position.Y),
                    CompositionSelectionAlignment.Bottom => new TcPoint(node.Position.X, bottom - node.Size.Height),
                    CompositionSelectionAlignment.Center => new TcPoint(center - node.Size.Width / 2, node.Position.Y),
                    CompositionSelectionAlignment.Middle => new TcPoint(node.Position.X, middle - node.Size.Height / 2),
                    _ => node.Position
                };

                return node with { Position = position };
            }).ToArray()
        };
    }

    public static CompositionViewSnapshot ResizeToMatch(
        CompositionViewSnapshot snapshot,
        IReadOnlyList<string> nodeIds,
        CompositionSelectionSizeMode mode)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (nodeIds is null)
        {
            throw new ArgumentNullException(nameof(nodeIds));
        }

        var sourceNode = nodeIds
            .Select(id => snapshot.Nodes.FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal)))
            .FirstOrDefault(node => node is not null);
        if (sourceNode is null)
        {
            return snapshot;
        }

        var selectedNodeIds = nodeIds.ToHashSet(StringComparer.Ordinal);
        return snapshot with
        {
            Nodes = snapshot.Nodes.Select(node =>
            {
                if (!selectedNodeIds.Contains(node.Id))
                {
                    return node;
                }

                var size = mode switch
                {
                    CompositionSelectionSizeMode.SameWidth => node.Size with { Width = sourceNode.Size.Width },
                    CompositionSelectionSizeMode.SameHeight => node.Size with { Height = sourceNode.Size.Height },
                    CompositionSelectionSizeMode.SameSize => sourceNode.Size,
                    _ => node.Size
                };
                return node with { Size = size };
            }).ToArray()
        };
    }

    public static CompositionViewSnapshot Distribute(
        CompositionViewSnapshot snapshot,
        IReadOnlyCollection<string> nodeIds,
        CompositionSelectionDistribution distribution)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var selectedNodes = GetSelectedNodes(snapshot, nodeIds);
        if (selectedNodes.Count < 3)
        {
            return snapshot;
        }

        var ordered = selectedNodes
            .OrderBy(node => distribution == CompositionSelectionDistribution.Horizontal ? node.Position.X : node.Position.Y)
            .ToArray();
        var firstValue = distribution == CompositionSelectionDistribution.Horizontal
            ? ordered.First().Position.X
            : ordered.First().Position.Y;
        var lastValue = distribution == CompositionSelectionDistribution.Horizontal
            ? ordered.Last().Position.X
            : ordered.Last().Position.Y;
        var step = (lastValue - firstValue) / (ordered.Length - 1);
        var positions = ordered
            .Select((node, index) => new { node.Id, Value = firstValue + step * index })
            .ToDictionary(item => item.Id, item => item.Value, StringComparer.Ordinal);

        return snapshot with
        {
            Nodes = snapshot.Nodes.Select(node =>
            {
                if (!positions.TryGetValue(node.Id, out var value))
                {
                    return node;
                }

                var position = distribution == CompositionSelectionDistribution.Horizontal
                    ? new TcPoint(value, node.Position.Y)
                    : new TcPoint(node.Position.X, value);
                return node with { Position = position };
            }).ToArray()
        };
    }

    public static CompositionViewSnapshot Reorder(
        CompositionViewSnapshot snapshot,
        IReadOnlyCollection<string> nodeIds,
        CompositionSelectionZOrder zOrder)
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
        if (selectedNodeIds.Count == 0)
        {
            return snapshot;
        }

        var nodes = snapshot.Nodes.ToList();
        var reordered = zOrder switch
        {
            CompositionSelectionZOrder.BringToFront => nodes
                .Where(node => !selectedNodeIds.Contains(node.Id))
                .Concat(nodes.Where(node => selectedNodeIds.Contains(node.Id)))
                .ToArray(),
            CompositionSelectionZOrder.SendToBack => nodes
                .Where(node => selectedNodeIds.Contains(node.Id))
                .Concat(nodes.Where(node => !selectedNodeIds.Contains(node.Id)))
                .ToArray(),
            CompositionSelectionZOrder.BringForward => MoveOneStep(nodes, selectedNodeIds, forward: true),
            CompositionSelectionZOrder.SendBackward => MoveOneStep(nodes, selectedNodeIds, forward: false),
            _ => nodes.ToArray()
        };

        return snapshot with { Nodes = reordered };
    }

    private static IReadOnlyList<CompositionNodeView> GetSelectedNodes(
        CompositionViewSnapshot snapshot,
        IReadOnlyCollection<string> nodeIds)
    {
        if (nodeIds is null)
        {
            throw new ArgumentNullException(nameof(nodeIds));
        }

        var selectedNodeIds = nodeIds.ToHashSet(StringComparer.Ordinal);
        return snapshot.Nodes
            .Where(node => selectedNodeIds.Contains(node.Id))
            .ToArray();
    }

    private static IReadOnlyList<CompositionNodeView> MoveOneStep(
        List<CompositionNodeView> nodes,
        ISet<string> selectedNodeIds,
        bool forward)
    {
        if (forward)
        {
            for (var index = nodes.Count - 2; index >= 0; index--)
            {
                if (selectedNodeIds.Contains(nodes[index].Id) && !selectedNodeIds.Contains(nodes[index + 1].Id))
                {
                    (nodes[index], nodes[index + 1]) = (nodes[index + 1], nodes[index]);
                }
            }
        }
        else
        {
            for (var index = 1; index < nodes.Count; index++)
            {
                if (selectedNodeIds.Contains(nodes[index].Id) && !selectedNodeIds.Contains(nodes[index - 1].Id))
                {
                    (nodes[index - 1], nodes[index]) = (nodes[index], nodes[index - 1]);
                }
            }
        }

        return nodes.ToArray();
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
