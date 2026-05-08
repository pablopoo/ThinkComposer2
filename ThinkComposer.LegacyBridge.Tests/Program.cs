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

var documentExportPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-export-{Guid.NewGuid():N}.tcdoc");
try
{
    var document = LegacyCompositionDocumentExporter.ExportToFile(domainPath, documentExportPath);
    var reloadedDocument = CompositionDocumentSnapshotXmlStore.Load(documentExportPath);

    AssertEqual(document.Id, reloadedDocument.Id, "document exported id");
    AssertEqual("All-Purpose", reloadedDocument.Title, "document exported title");
    AssertEqual("All-Purpose", reloadedDocument.Domain.Name, "document exported domain");
    AssertTrue(reloadedDocument.Domain.ConceptDefinitions.Count > 0, "document concept definitions");
    AssertTrue(reloadedDocument.Domain.RelationshipDefinitions.Count > 0, "document relationship definitions");
    AssertTrue(reloadedDocument.Domain.MarkerDefinitions.Count > 0, "document marker definitions");
    AssertTrue(reloadedDocument.Domain.TableDefinitions.Count > 0, "document table definitions");
    AssertEqual(snapshot.Nodes.Count, reloadedDocument.Ideas.Count, "document idea count");
    AssertEqual(snapshot.Connectors.Count, reloadedDocument.Relationships.Count, "document relationship count");
    AssertEqual(1, reloadedDocument.Views.Count, "document view count");
    AssertTrue(reloadedDocument.Extensions.Any(extension => extension.Key == "legacy.package.base64"), "document preserves legacy package");
    AssertTrue(reloadedDocument.Extensions.Any(extension => extension.Key == "legacy.source.extension"), "document preserves legacy extension");
}
finally
{
    if (File.Exists(documentExportPath))
    {
        File.Delete(documentExportPath);
    }
}

var dispatchedDocumentPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-dispatch-{Guid.NewGuid():N}.tcdoc");
try
{
    var dispatchedKind = LegacyCompositionExportDispatcher.ExportToFile(domainPath, dispatchedDocumentPath);
    AssertEqual(CompositionDocumentFileKind.ModernDocument, dispatchedKind, "dispatch modern kind");
    AssertTrue(File.Exists(dispatchedDocumentPath), "dispatch modern file exists");
    AssertEqual("All-Purpose", CompositionDocumentSnapshotXmlStore.Load(dispatchedDocumentPath).Title, "dispatch modern title");
}
finally
{
    if (File.Exists(dispatchedDocumentPath))
    {
        File.Delete(dispatchedDocumentPath);
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
