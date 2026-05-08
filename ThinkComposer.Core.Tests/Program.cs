using Instrumind.ThinkComposer.Core.Rendering;

var snapshot = DemoCompositionViewSource.CreateSnapshot();

AssertEqual("Composition 1", snapshot.Title, "snapshot title");
AssertEqual(3, snapshot.Nodes.Count, "node count");
AssertEqual(2, snapshot.Connectors.Count, "connector count");

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
