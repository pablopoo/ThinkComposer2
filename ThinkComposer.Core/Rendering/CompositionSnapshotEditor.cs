using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotEditor
{
    private const double MinimumNodeWidth = 20;
    private const double MinimumNodeHeight = 20;

    public static CompositionViewSnapshot CreateNode(
        CompositionViewSnapshot snapshot,
        CompositionNodeView node)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (string.IsNullOrWhiteSpace(node.Id))
        {
            throw new ArgumentException("Node id is required.", nameof(node));
        }

        if (snapshot.Nodes.Any(existing => string.Equals(existing.Id, node.Id, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot node '{node.Id}' already exists.");
        }

        return snapshot with { Nodes = snapshot.Nodes.Append(NormalizeNode(node)).ToArray() };
    }

    public static CompositionViewSnapshot RenameNode(
        CompositionViewSnapshot snapshot,
        string nodeId,
        string text)
    {
        return UpdateNode(snapshot, nodeId, node => node with { Text = text?.Trim() ?? string.Empty });
    }

    public static CompositionViewSnapshot MoveNode(
        CompositionViewSnapshot snapshot,
        string nodeId,
        TcPoint position)
    {
        return UpdateNode(snapshot, nodeId, node => node with { Position = position });
    }

    public static CompositionViewSnapshot ResizeNode(
        CompositionViewSnapshot snapshot,
        string nodeId,
        TcSize size)
    {
        return UpdateNode(snapshot, nodeId, node => node with { Size = NormalizeSize(size) });
    }

    public static CompositionViewSnapshot DeleteNode(CompositionViewSnapshot snapshot, string nodeId)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ValidateNodeId(nodeId);

        if (!snapshot.Nodes.Any(node => string.Equals(node.Id, nodeId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot node '{nodeId}' was not found.");
        }

        return snapshot with
        {
            Nodes = snapshot.Nodes
                .Where(node => !string.Equals(node.Id, nodeId, StringComparison.Ordinal))
                .ToArray(),
            Connectors = snapshot.Connectors
                .Where(connector =>
                    !string.Equals(connector.SourceId, nodeId, StringComparison.Ordinal) &&
                    !string.Equals(connector.TargetId, nodeId, StringComparison.Ordinal))
                .ToArray()
        };
    }

    private static CompositionViewSnapshot UpdateNode(
        CompositionViewSnapshot snapshot,
        string nodeId,
        Func<CompositionNodeView, CompositionNodeView> update)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ValidateNodeId(nodeId);

        var found = false;
        var nodes = snapshot.Nodes
            .Select(node =>
            {
                if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }

                found = true;
                return NormalizeNode(update(node));
            })
            .ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"Snapshot node '{nodeId}' was not found.");
        }

        return snapshot with { Nodes = nodes };
    }

    private static void ValidateNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node id is required.", nameof(nodeId));
        }
    }

    private static CompositionNodeView NormalizeNode(CompositionNodeView node)
    {
        return node with { Size = NormalizeSize(node.Size) };
    }

    private static TcSize NormalizeSize(TcSize size)
    {
        return new TcSize(
            Math.Max(MinimumNodeWidth, size.Width),
            Math.Max(MinimumNodeHeight, size.Height));
    }
}
