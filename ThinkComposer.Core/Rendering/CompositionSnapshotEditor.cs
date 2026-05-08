using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotEditor
{
    public static CompositionViewSnapshot MoveNode(
        CompositionViewSnapshot snapshot,
        string nodeId,
        TcPoint position)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node id is required.", nameof(nodeId));
        }

        var found = false;
        var nodes = snapshot.Nodes
            .Select(node =>
            {
                if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }

                found = true;
                return node with { Position = position };
            })
            .ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"Snapshot node '{nodeId}' was not found.");
        }

        return snapshot with { Nodes = nodes };
    }
}
