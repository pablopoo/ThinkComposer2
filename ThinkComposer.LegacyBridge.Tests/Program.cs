using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Instrumind.ThinkComposer.Core.Rendering;
using Instrumind.ThinkComposer.LegacyBridge;

var domainPath = Path.GetFullPath(Path.Combine(
    Directory.GetCurrentDirectory(),
    "PredefinedContent",
    "All-Purpose.tdom"));

var snapshot = LegacyCompositionSnapshotLoader.LoadFromFile(domainPath);

AssertEqual("All-Purpose", snapshot.Title, "legacy domain title");
AssertTrue(!string.IsNullOrWhiteSpace(snapshot.Id), "legacy snapshot id");

var nodeIds = snapshot.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
foreach (var connector in snapshot.Connectors)
{
    AssertTrue(nodeIds.Contains(connector.SourceId), $"connector {connector.Id} source exists");
    AssertTrue(nodeIds.Contains(connector.TargetId), $"connector {connector.Id} target exists");
}

var exportPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-export-{Guid.NewGuid():N}.tcview");
try
{
    LegacyCompositionSnapshotExporter.ExportToFile(domainPath, exportPath);
    var exported = CompositionViewSnapshotXmlStore.Load(exportPath);

    AssertEqual(snapshot.Id, exported.Id, "exported id");
    AssertEqual(snapshot.Title, exported.Title, "exported title");
    AssertEqual(snapshot.Nodes.Count, exported.Nodes.Count, "exported node count");
    AssertEqual(snapshot.Connectors.Count, exported.Connectors.Count, "exported connector count");
}
finally
{
    if (File.Exists(exportPath))
    {
        File.Delete(exportPath);
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
