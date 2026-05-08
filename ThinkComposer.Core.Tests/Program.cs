using Instrumind.ThinkComposer.Core.Primitives;
using Instrumind.ThinkComposer.Core.Rendering;

var snapshot = DemoCompositionViewSource.CreateSnapshot();

AssertEqual("Composition 1", snapshot.Title, "snapshot title");
AssertEqual(3, snapshot.Nodes.Count, "node count");
AssertEqual(2, snapshot.Connectors.Count, "connector count");

var emptyDocument = CompositionDocumentFactory.CreateEmpty("Untitled");
AssertEqual("Untitled", emptyDocument.Title, "empty document title");
AssertEqual(0, emptyDocument.Nodes.Count, "empty document nodes");
AssertEqual(0, emptyDocument.Connectors.Count, "empty document connectors");
AssertTrue(Guid.TryParse(emptyDocument.Id, out _), "empty document id is guid");

var commandEntries = CompositionCommandCatalog.ForSnapshot(snapshot);
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.Open), "command catalog open");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Node && entry.TargetId == "customer"), "command catalog node");
AssertEqual("Customer Need", CompositionCommandCatalog.Search(commandEntries, "customer").First().Title, "command search node");
AssertEqual(0, CompositionCommandCatalog.Search(commandEntries, "zzzz-not-found").Count, "command search miss");

var nodeIds = snapshot.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
AssertTrue(nodeIds.SetEquals(["customer", "capability", "service"]), "node ids");

var customer = snapshot.Nodes.Single(node => node.Id == "customer");
AssertEqual("Customer Need", customer.Text, "customer text");
AssertEqual(120.0, customer.Position.X, "customer x");
AssertEqual(132.0, customer.Position.Y, "customer y");
AssertEqual(164.0, customer.Size.Width, "customer width");
AssertEqual(82.0, customer.Size.Height, "customer height");

foreach (var connector in snapshot.Connectors)
{
    AssertTrue(nodeIds.Contains(connector.SourceId), $"connector {connector.Id} source exists");
    AssertTrue(nodeIds.Contains(connector.TargetId), $"connector {connector.Id} target exists");
}

var sourceSymbol = new VisualSymbol(
    Guid.Parse("11111111-1111-1111-1111-111111111111"),
    new LegacyRect(10, 20, 160, 80),
    new VisualRepresentation(new Idea("Source Concept")));
var targetSymbol = new VisualSymbol(
    Guid.Parse("22222222-2222-2222-2222-222222222222"),
    new LegacyRect(310, 220, 180, 90),
    new VisualRepresentation(new Idea("Target Concept")));
var connectorSymbol = new VisualConnector(
    Guid.Parse("33333333-3333-3333-3333-333333333333"),
    sourceSymbol,
    targetSymbol);
var legacyComposition = new LegacyComposition(
    Guid.Parse("44444444-4444-4444-4444-444444444444"),
    "Legacy Composition",
    new LegacyView(
    [
        new LegacyViewChild(sourceSymbol),
        new LegacyViewChild(connectorSymbol),
        new LegacyViewChild(targetSymbol)
    ]));

var legacySnapshot = LegacyCompositionSnapshotMapper.FromComposition(legacyComposition);

AssertEqual("Legacy Composition", legacySnapshot.Title, "legacy snapshot title");
AssertEqual("44444444-4444-4444-4444-444444444444", legacySnapshot.Id, "legacy snapshot id");
AssertEqual(2, legacySnapshot.Nodes.Count, "legacy node count");
AssertEqual(1, legacySnapshot.Connectors.Count, "legacy connector count");

var sourceNode = legacySnapshot.Nodes.Single(node => node.Id == "11111111-1111-1111-1111-111111111111");
AssertEqual("Source Concept", sourceNode.Text, "legacy source text");
AssertEqual(10.0, sourceNode.Position.X, "legacy source x");
AssertEqual(20.0, sourceNode.Position.Y, "legacy source y");
AssertEqual(160.0, sourceNode.Size.Width, "legacy source width");
AssertEqual(80.0, sourceNode.Size.Height, "legacy source height");

var legacyConnector = legacySnapshot.Connectors.Single();
AssertEqual("33333333-3333-3333-3333-333333333333", legacyConnector.Id, "legacy connector id");
AssertEqual(sourceSymbol.GlobalId.ToString(), legacyConnector.SourceId, "legacy connector source");
AssertEqual(targetSymbol.GlobalId.ToString(), legacyConnector.TargetId, "legacy connector target");

var snapshotPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-{Guid.NewGuid():N}.tcview");
try
{
    CompositionViewSnapshotXmlStore.Save(legacySnapshot, snapshotPath);
    var roundTripSnapshot = CompositionViewSnapshotXmlStore.Load(snapshotPath);

    AssertEqual(legacySnapshot.Id, roundTripSnapshot.Id, "roundtrip id");
    AssertEqual(legacySnapshot.Title, roundTripSnapshot.Title, "roundtrip title");
    AssertEqual(legacySnapshot.Nodes.Count, roundTripSnapshot.Nodes.Count, "roundtrip node count");
    AssertEqual(legacySnapshot.Connectors.Count, roundTripSnapshot.Connectors.Count, "roundtrip connector count");

    var roundTripNode = roundTripSnapshot.Nodes.Single(node => node.Id == sourceNode.Id);
    AssertEqual(sourceNode.Text, roundTripNode.Text, "roundtrip node text");
    AssertEqual(sourceNode.Position.X, roundTripNode.Position.X, "roundtrip node x");
    AssertEqual(sourceNode.Size.Width, roundTripNode.Size.Width, "roundtrip node width");
}
finally
{
    if (File.Exists(snapshotPath))
    {
        File.Delete(snapshotPath);
    }
}

var fitNodes = new[]
{
    new CompositionNodeView("left", "Left", new TcPoint(100, 200), new TcSize(100, 50)),
    new CompositionNodeView("right", "Right", new TcPoint(300, 400), new TcSize(100, 50))
};

var fit = CompositionViewportFitter.FitNodes(fitNodes, 1000, 800, margin: 56, minZoom: 0.35, maxZoom: 2.4);
AssertEqual(2.4, Math.Round(fit.Zoom, 4), "fit zoom");
AssertEqual(-100.0, Math.Round(fit.PanX, 4), "fit pan x");
AssertEqual(-380.0, Math.Round(fit.PanY, 4), "fit pan y");

var initialFit = CompositionViewportFitter.FitNodes(
    fitNodes,
    1000,
    800,
    margin: 72,
    minZoom: CompositionCanvasRenderDefaults.InitialMinZoom,
    maxZoom: CompositionCanvasRenderDefaults.InitialMaxZoom);
AssertEqual(1.35, Math.Round(initialFit.Zoom, 4), "initial fit max zoom");

var emptyFit = CompositionViewportFitter.FitNodes(Array.Empty<CompositionNodeView>(), 1000, 800);
AssertEqual(1.0, emptyFit.Zoom, "empty fit zoom");
AssertEqual(0.0, emptyFit.PanX, "empty fit pan x");
AssertEqual(0.0, emptyFit.PanY, "empty fit pan y");

var horizontalRoute = CompositionConnectorRouter.Route(
    new CompositionNodeView("source", "Source", new TcPoint(0, 0), new TcSize(100, 50)),
    new CompositionNodeView("target", "Target", new TcPoint(200, 0), new TcSize(100, 50)));
AssertEqual(100.0, Math.Round(horizontalRoute.Source.X, 4), "horizontal route source x");
AssertEqual(25.0, Math.Round(horizontalRoute.Source.Y, 4), "horizontal route source y");
AssertEqual(200.0, Math.Round(horizontalRoute.Target.X, 4), "horizontal route target x");
AssertEqual(25.0, Math.Round(horizontalRoute.Target.Y, 4), "horizontal route target y");

var verticalRoute = CompositionConnectorRouter.Route(
    new CompositionNodeView("source", "Source", new TcPoint(0, 0), new TcSize(100, 50)),
    new CompositionNodeView("target", "Target", new TcPoint(0, 150), new TcSize(100, 50)));
AssertEqual(50.0, Math.Round(verticalRoute.Source.X, 4), "vertical route source x");
AssertEqual(50.0, Math.Round(verticalRoute.Source.Y, 4), "vertical route source y");
AssertEqual(50.0, Math.Round(verticalRoute.Target.X, 4), "vertical route target x");
AssertEqual(150.0, Math.Round(verticalRoute.Target.Y, 4), "vertical route target y");

var compactLabel = CompositionNodeLabelPolicy.ForNode(
    new CompositionNodeView("small", "Small", new TcPoint(0, 0), new TcSize(80, 24)));
AssertEqual(false, compactLabel.ShowsSubtitle, "compact label subtitle");
AssertEqual(11.0, compactLabel.TitleFontSize, "compact label font");
AssertEqual(false, compactLabel.AllowsWrapping, "compact label wrapping");

var expandedLabel = CompositionNodeLabelPolicy.ForNode(
    new CompositionNodeView("large", "Large", new TcPoint(0, 0), new TcSize(164, 82)));
AssertEqual(true, expandedLabel.ShowsSubtitle, "expanded label subtitle");
AssertEqual(14.0, expandedLabel.TitleFontSize, "expanded label font");
AssertEqual(true, expandedLabel.AllowsWrapping, "expanded label wrapping");

var indexedSnapshot = new CompositionViewSnapshot(
    "indexed",
    "Indexed",
    fitNodes,
    [
        new CompositionConnectorView("left-to-right", "left", "right"),
        new CompositionConnectorView("right-to-left", "right", "left")
    ]);
var index = CompositionSnapshotIndex.FromSnapshot(indexedSnapshot);
AssertEqual(1, index.CountOutgoing("left"), "left outgoing");
AssertEqual(1, index.CountIncoming("left"), "left incoming");
AssertEqual(0, index.CountIncoming("missing"), "missing incoming");

var movedSnapshot = CompositionSnapshotEditor.MoveNode(indexedSnapshot, "left", new TcPoint(42, 84));
var movedLeft = movedSnapshot.Nodes.Single(node => node.Id == "left");
var unmovedRight = movedSnapshot.Nodes.Single(node => node.Id == "right");
AssertEqual(42.0, movedLeft.Position.X, "moved left x");
AssertEqual(84.0, movedLeft.Position.Y, "moved left y");
AssertEqual(100.0, movedLeft.Size.Width, "moved left width");
AssertEqual(300.0, unmovedRight.Position.X, "unmoved right x");
AssertEqual(indexedSnapshot.Connectors.Count, movedSnapshot.Connectors.Count, "move preserves connectors");
AssertEqual(100.0, indexedSnapshot.Nodes.Single(node => node.Id == "left").Position.X, "move leaves original x");

var renamedSnapshot = CompositionSnapshotEditor.RenameNode(indexedSnapshot, "left", "Renamed Left");
AssertEqual("Renamed Left", renamedSnapshot.Nodes.Single(node => node.Id == "left").Text, "rename node");
AssertEqual("Left", indexedSnapshot.Nodes.Single(node => node.Id == "left").Text, "rename leaves original");

var resizedSnapshot = CompositionSnapshotEditor.ResizeNode(indexedSnapshot, "left", new TcSize(180, 90));
var resizedLeft = resizedSnapshot.Nodes.Single(node => node.Id == "left");
AssertEqual(180.0, resizedLeft.Size.Width, "resize width");
AssertEqual(90.0, resizedLeft.Size.Height, "resize height");

var createdSnapshot = CompositionSnapshotEditor.CreateNode(
    indexedSnapshot,
    new CompositionNodeView("created", "Created", new TcPoint(10, 20), new TcSize(120, 60)));
AssertEqual(3, createdSnapshot.Nodes.Count, "create node count");
AssertEqual("Created", createdSnapshot.Nodes.Single(node => node.Id == "created").Text, "create node text");
AssertThrows<InvalidOperationException>(
    () => CompositionSnapshotEditor.CreateNode(indexedSnapshot, fitNodes[0]),
    "create duplicate node");

var deletedSnapshot = CompositionSnapshotEditor.DeleteNode(indexedSnapshot, "left");
AssertEqual(1, deletedSnapshot.Nodes.Count, "delete node count");
AssertEqual(0, deletedSnapshot.Connectors.Count, "delete connected connectors");

var relationshipSnapshot = CompositionSnapshotEditor.CreateConnector(
    indexedSnapshot,
    new CompositionConnectorView("created-relationship", "left", "right", "Created Relationship"));
AssertEqual(3, relationshipSnapshot.Connectors.Count, "create relationship count");
AssertEqual(
    "Created Relationship",
    relationshipSnapshot.Connectors.Single(connector => connector.Id == "created-relationship").Text,
    "create relationship text");
AssertThrows<InvalidOperationException>(
    () => CompositionSnapshotEditor.CreateConnector(indexedSnapshot, new CompositionConnectorView("left-to-right", "left", "right")),
    "create duplicate relationship");
AssertThrows<InvalidOperationException>(
    () => CompositionSnapshotEditor.CreateConnector(indexedSnapshot, new CompositionConnectorView("missing-target", "left", "missing")),
    "create relationship missing target");

var renamedRelationshipSnapshot = CompositionSnapshotEditor.RenameConnector(
    relationshipSnapshot,
    "created-relationship",
    "Renamed Relationship");
AssertEqual(
    "Renamed Relationship",
    renamedRelationshipSnapshot.Connectors.Single(connector => connector.Id == "created-relationship").Text,
    "rename relationship");

var deletedRelationshipSnapshot = CompositionSnapshotEditor.DeleteConnector(relationshipSnapshot, "created-relationship");
AssertEqual(2, deletedRelationshipSnapshot.Connectors.Count, "delete relationship count");

var editSession = new CompositionEditingSession(indexedSnapshot);
editSession.Apply(renamedSnapshot);
AssertEqual(true, editSession.CanUndo, "session can undo");
AssertEqual(false, editSession.CanRedo, "session cannot redo after apply");
AssertEqual("Renamed Left", editSession.CurrentSnapshot.Nodes.Single(node => node.Id == "left").Text, "session apply");
AssertEqual("Left", editSession.Undo().Nodes.Single(node => node.Id == "left").Text, "session undo");
AssertEqual(true, editSession.CanRedo, "session can redo");
AssertEqual("Renamed Left", editSession.Redo().Nodes.Single(node => node.Id == "left").Text, "session redo");

AssertThrows<InvalidOperationException>(
    () => CompositionSnapshotEditor.MoveNode(indexedSnapshot, "missing", new TcPoint(0, 0)),
    "move missing node");

var movedSnapshotPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-moved-{Guid.NewGuid():N}.tcview");
try
{
    CompositionViewSnapshotXmlStore.Save(movedSnapshot, movedSnapshotPath);
    var reloadedMovedSnapshot = CompositionViewSnapshotXmlStore.Load(movedSnapshotPath);
    var reloadedMovedLeft = reloadedMovedSnapshot.Nodes.Single(node => node.Id == "left");

    AssertEqual(42.0, reloadedMovedLeft.Position.X, "moved roundtrip x");
    AssertEqual(84.0, reloadedMovedLeft.Position.Y, "moved roundtrip y");
}
finally
{
    if (File.Exists(movedSnapshotPath))
    {
        File.Delete(movedSnapshotPath);
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true");
    }
}

static void AssertThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"{name}: expected {typeof(TException).Name}");
}

public sealed class LegacyComposition(Guid globalId, string name, LegacyView activeView)
{
    public Guid GlobalId { get; } = globalId;
    public string Name { get; } = name;
    public LegacyView ActiveView { get; } = activeView;
    public LegacyView RootView { get; } = activeView;
}

public sealed class LegacyView(IReadOnlyList<LegacyViewChild> viewChildren)
{
    public IReadOnlyList<LegacyViewChild> ViewChildren { get; } = viewChildren;
}

public sealed class LegacyViewChild(object key)
{
    public object Key { get; } = key;
}

public sealed class VisualSymbol(Guid globalId, LegacyRect baseArea, VisualRepresentation ownerRepresentation)
{
    public Guid GlobalId { get; } = globalId;
    public LegacyRect BaseArea { get; } = baseArea;
    public VisualRepresentation OwnerRepresentation { get; } = ownerRepresentation;
}

public sealed class VisualConnector(Guid globalId, VisualSymbol originSymbol, VisualSymbol targetSymbol)
{
    public Guid GlobalId { get; } = globalId;
    public VisualSymbol OriginSymbol { get; } = originSymbol;
    public VisualSymbol TargetSymbol { get; } = targetSymbol;
}

public sealed class VisualRepresentation(Idea representedIdea)
{
    public Idea RepresentedIdea { get; } = representedIdea;
}

public sealed class Idea(string name)
{
    public string Name { get; } = name;
}

public sealed class LegacyRect(double x, double y, double width, double height)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Width { get; } = width;
    public double Height { get; } = height;
}
