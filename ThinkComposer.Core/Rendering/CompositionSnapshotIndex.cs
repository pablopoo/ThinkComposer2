namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed class CompositionSnapshotIndex
{
    private readonly IReadOnlyDictionary<string, CompositionNodeView> _nodesById;
    private readonly IReadOnlyDictionary<string, int> _incomingCounts;
    private readonly IReadOnlyDictionary<string, int> _outgoingCounts;

    private CompositionSnapshotIndex(
        IReadOnlyList<CompositionNodeView> nodes,
        IReadOnlyList<CompositionConnectorView> connectors,
        IReadOnlyDictionary<string, CompositionNodeView> nodesById,
        IReadOnlyDictionary<string, int> incomingCounts,
        IReadOnlyDictionary<string, int> outgoingCounts)
    {
        Nodes = nodes;
        Connectors = connectors;
        _nodesById = nodesById;
        _incomingCounts = incomingCounts;
        _outgoingCounts = outgoingCounts;
    }

    public IReadOnlyList<CompositionNodeView> Nodes { get; }
    public IReadOnlyList<CompositionConnectorView> Connectors { get; }
    public CompositionNodeView? FirstNode => Nodes.FirstOrDefault();

    public static CompositionSnapshotIndex FromSnapshot(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var nodesById = snapshot.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var incomingCounts = CountBy(snapshot.Connectors, connector => connector.TargetId);
        var outgoingCounts = CountBy(snapshot.Connectors, connector => connector.SourceId);

        return new CompositionSnapshotIndex(
            snapshot.Nodes,
            snapshot.Connectors,
            nodesById,
            incomingCounts,
            outgoingCounts);
    }

    public CompositionNodeView? FindNode(string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }
        return _nodesById.TryGetValue(id, out var node) ? node : null;
    }

    public int CountIncoming(string nodeId)
    {
        if (nodeId is null)
        {
            throw new ArgumentNullException(nameof(nodeId));
        }
        return _incomingCounts.TryGetValue(nodeId, out var count) ? count : 0;
    }

    public int CountOutgoing(string nodeId)
    {
        if (nodeId is null)
        {
            throw new ArgumentNullException(nameof(nodeId));
        }
        return _outgoingCounts.TryGetValue(nodeId, out var count) ? count : 0;
    }

    private static IReadOnlyDictionary<string, int> CountBy(
        IEnumerable<CompositionConnectorView> connectors,
        Func<CompositionConnectorView, string> selector)
    {
        return connectors
            .GroupBy(selector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }
}
