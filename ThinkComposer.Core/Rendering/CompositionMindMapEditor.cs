using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionMindMapCreationResult(
    CompositionViewSnapshot Snapshot,
    string NodeId,
    string? ConnectorId = null);

public static class CompositionMindMapEditor
{
    private const double SiblingVerticalGap = 70;
    private const double ChildHorizontalGap = 120;

    public static CompositionMindMapCreationResult CreateSibling(
        CompositionViewSnapshot snapshot,
        string selectedNodeId,
        string nodeId,
        string text = "New Concept")
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }
        var selectedNode = FindNode(snapshot, selectedNodeId);
        var node = CreateNode(
            selectedNode,
            nodeId,
            text,
            new TcPoint(selectedNode.Position.X, selectedNode.Position.Y + selectedNode.Size.Height + SiblingVerticalGap));

        return new CompositionMindMapCreationResult(
            CompositionSnapshotEditor.CreateNode(snapshot, node),
            node.Id);
    }

    public static CompositionMindMapCreationResult CreateChild(
        CompositionViewSnapshot snapshot,
        string selectedNodeId,
        string nodeId,
        string connectorId,
        string text = "New Concept",
        string relationshipText = "Relationship")
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }
        var selectedNode = FindNode(snapshot, selectedNodeId);
        var node = CreateNode(
            selectedNode,
            nodeId,
            text,
            new TcPoint(selectedNode.Position.X + selectedNode.Size.Width + ChildHorizontalGap, selectedNode.Position.Y));
        var connector = new CompositionConnectorView(connectorId, selectedNode.Id, node.Id, relationshipText);
        var withNode = CompositionSnapshotEditor.CreateNode(snapshot, node);

        return new CompositionMindMapCreationResult(
            CompositionSnapshotEditor.CreateConnector(withNode, connector),
            node.Id,
            connector.Id);
    }

    private static CompositionNodeView CreateNode(
        CompositionNodeView source,
        string nodeId,
        string text,
        TcPoint position)
    {
        return new CompositionNodeView(
            nodeId,
            string.IsNullOrWhiteSpace(text) ? "New Concept" : text.Trim(),
            position,
            source.Size,
            source.Style);
    }

    private static CompositionNodeView FindNode(CompositionViewSnapshot snapshot, string selectedNodeId)
    {
        if (string.IsNullOrWhiteSpace(selectedNodeId))
        {
            throw new ArgumentException("Selected node id is required.", nameof(selectedNodeId));
        }

        return snapshot.Nodes.FirstOrDefault(node => string.Equals(node.Id, selectedNodeId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Selected node '{selectedNodeId}' was not found.");
    }
}
