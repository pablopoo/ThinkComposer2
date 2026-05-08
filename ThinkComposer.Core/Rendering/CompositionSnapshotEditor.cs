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

    public static CompositionViewSnapshot CreateConnector(
        CompositionViewSnapshot snapshot,
        CompositionConnectorView connector)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (connector is null)
        {
            throw new ArgumentNullException(nameof(connector));
        }

        ValidateConnector(snapshot, connector);

        return snapshot with
        {
            Connectors = snapshot.Connectors.Append(NormalizeConnector(connector)).ToArray()
        };
    }

    public static CompositionViewSnapshot RenameConnector(
        CompositionViewSnapshot snapshot,
        string connectorId,
        string text)
    {
        return UpdateConnector(snapshot, connectorId, connector => connector with { Text = text?.Trim() ?? string.Empty });
    }

    public static CompositionViewSnapshot DeleteConnector(CompositionViewSnapshot snapshot, string connectorId)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ValidateNodeId(connectorId);

        if (!snapshot.Connectors.Any(connector => string.Equals(connector.Id, connectorId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot connector '{connectorId}' was not found.");
        }

        return snapshot with
        {
            Connectors = snapshot.Connectors
                .Where(connector => !string.Equals(connector.Id, connectorId, StringComparison.Ordinal))
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

    private static CompositionViewSnapshot UpdateConnector(
        CompositionViewSnapshot snapshot,
        string connectorId,
        Func<CompositionConnectorView, CompositionConnectorView> update)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ValidateNodeId(connectorId);

        var found = false;
        var connectors = snapshot.Connectors
            .Select(connector =>
            {
                if (!string.Equals(connector.Id, connectorId, StringComparison.Ordinal))
                {
                    return connector;
                }

                found = true;
                return NormalizeConnector(update(connector));
            })
            .ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"Snapshot connector '{connectorId}' was not found.");
        }

        return snapshot with { Connectors = connectors };
    }

    private static void ValidateConnector(CompositionViewSnapshot snapshot, CompositionConnectorView connector)
    {
        ValidateNodeId(connector.Id);
        ValidateNodeId(connector.SourceId);
        ValidateNodeId(connector.TargetId);

        if (snapshot.Connectors.Any(existing => string.Equals(existing.Id, connector.Id, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot connector '{connector.Id}' already exists.");
        }

        if (!snapshot.Nodes.Any(node => string.Equals(node.Id, connector.SourceId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot source node '{connector.SourceId}' was not found.");
        }

        if (!snapshot.Nodes.Any(node => string.Equals(node.Id, connector.TargetId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Snapshot target node '{connector.TargetId}' was not found.");
        }
    }

    private static CompositionConnectorView NormalizeConnector(CompositionConnectorView connector)
    {
        return connector with { Text = string.IsNullOrWhiteSpace(connector.Text) ? "Relationship" : connector.Text.Trim() };
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
