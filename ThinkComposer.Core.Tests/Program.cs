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
AssertEqual(CompositionDocumentFileKind.Snapshot, CompositionDocumentFileKindDetector.FromPath("sample.tcview"), "snapshot extension");
AssertEqual(CompositionDocumentFileKind.ModernDocument, CompositionDocumentFileKindDetector.FromPath("sample.tcdoc"), "modern document extension");
AssertEqual(CompositionDocumentFileKind.LegacyPackage, CompositionDocumentFileKindDetector.FromPath("sample.tdom"), "tdom extension");
AssertEqual(CompositionDocumentFileKind.LegacyPackage, CompositionDocumentFileKindDetector.FromPath("sample.tcom"), "tcom extension");
AssertEqual(CompositionDocumentFileKind.Unknown, CompositionDocumentFileKindDetector.FromPath("sample.txt"), "unknown extension");

var exportedSvg = CompositionSnapshotSvgExporter.Export(snapshot);
AssertTrue(exportedSvg.Contains("<svg", StringComparison.Ordinal), "svg root");
AssertTrue(exportedSvg.Contains("Customer Need", StringComparison.Ordinal), "svg node text");
AssertTrue(exportedSvg.Contains("<line", StringComparison.Ordinal), "svg line");

var exportedHtml = CompositionSnapshotHtmlExporter.Export(snapshot);
AssertTrue(exportedHtml.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "html doctype");
AssertTrue(exportedHtml.Contains("Composition 1", StringComparison.Ordinal), "html title");
AssertTrue(exportedHtml.Contains("<svg", StringComparison.Ordinal), "html svg");

var previewText = CompositionSnapshotPreviewTextBuilder.Build(snapshot);
AssertTrue(previewText.Contains("Composition 1", StringComparison.Ordinal), "preview title");
AssertTrue(previewText.Contains("Concepts (3)", StringComparison.Ordinal), "preview concepts");
AssertTrue(previewText.Contains("Customer Need", StringComparison.Ordinal), "preview node");
AssertTrue(previewText.Contains("Relationships (2)", StringComparison.Ordinal), "preview relationships");

var modernDocument = new CompositionDocumentSnapshot(
    Id: "doc-1",
    Title: "Modern Document",
    Domain: new CompositionDomainSnapshot(
        Id: "domain-1",
        Name: "All Purpose",
        Summary: "General diagramming",
        ConceptDefinitions:
        [
            new CompositionDefinitionSnapshot(
                Id: "concept-def",
                Name: "Concept",
                Kind: "Concept",
                Style: new CompositionStyleSnapshot(Fill: "#ffffff", Stroke: "#8a9ba8"))
        ],
        RelationshipDefinitions:
        [
            new CompositionDefinitionSnapshot(
                Id: "relationship-def",
                Name: "Relationship",
                Kind: "Relationship",
                Style: new CompositionStyleSnapshot(Stroke: "#2b78c6"))
        ],
        MarkerDefinitions:
        [
            new CompositionDefinitionSnapshot(Id: "marker-def", Name: "Risk", Kind: "Marker")
        ],
        TableDefinitions:
        [
            new CompositionDefinitionSnapshot(Id: "table-def", Name: "Checklist", Kind: "Table")
        ],
        ExternalLanguages:
        [
            new CompositionDefinitionSnapshot(Id: "sql", Name: "SQL", Kind: "ExternalLanguage")
        ],
        Templates:
        [
            new CompositionExtensionSnapshot("composition.template.sql", "select * from ideas")
        ],
        Extensions:
        [
            new CompositionExtensionSnapshot("legacy.domain.raw", "<domain />")
        ]),
    Ideas:
    [
        new CompositionIdeaSnapshot(
            Id: "idea-1",
            Name: "Customer Need",
            DefinitionId: "concept-def",
            Summary: "A need",
            Details:
            [
                new CompositionDetailSnapshot("detail-1", "Attachment", "Spec", "spec.pdf")
            ],
            Markers:
            [
                "marker-def"
            ],
            Style: new CompositionStyleSnapshot(Fill: "#f8f8f8"),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.idea.raw", "<idea />")
            ])
    ],
    Relationships:
    [
        new CompositionRelationshipSnapshot(
            Id: "rel-1",
            Name: "Addresses",
            SourceIdeaId: "idea-1",
            TargetIdeaId: "idea-1",
            DefinitionId: "relationship-def",
            Details:
            [
                new CompositionDetailSnapshot("detail-2", "CustomField", "Priority", "High")
            ],
            Style: new CompositionStyleSnapshot(Stroke: "#2b78c6"),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.relationship.raw", "<relationship />")
            ])
    ],
    Views:
    [
        new CompositionViewLayerSnapshot(
            Id: "view-1",
            Name: "Main",
            Nodes:
            [
                new CompositionNodeView("idea-1", "Customer Need", new TcPoint(10, 20), new TcSize(160, 80))
            ],
            Connectors:
            [
                new CompositionConnectorView("rel-1", "idea-1", "idea-1", "Addresses")
            ],
            Complements:
            [
                new CompositionExtensionSnapshot("legend", "Legend")
            ],
            Style: new CompositionStyleSnapshot(Fill: "#ffffff"),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.view.raw", "<view />")
            ])
    ],
    Extensions:
    [
        new CompositionExtensionSnapshot("legacy.package.raw", "<package />")
    ]);
AssertEqual(CompositionDocumentSnapshot.CurrentSchemaVersion, modernDocument.SchemaVersion, "modern schema");
AssertEqual("All Purpose", modernDocument.Domain.Name, "modern domain");
AssertEqual("Concept", modernDocument.Domain.ConceptDefinitions[0].Name, "modern concept definition");
AssertEqual("Spec", modernDocument.Ideas[0].Details[0].Name, "modern idea detail");
AssertEqual("Legend", modernDocument.Views[0].Complements[0].Value, "modern view complement");
AssertEqual("legacy.package.raw", modernDocument.Extensions[0].Key, "modern extension");

var modernReportHtml = CompositionDocumentReportHtmlExporter.Export(modernDocument);
AssertTrue(modernReportHtml.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "modern report doctype");
AssertTrue(modernReportHtml.Contains("Modern Document", StringComparison.Ordinal), "modern report title");
AssertTrue(modernReportHtml.Contains("Domain: All Purpose", StringComparison.Ordinal), "modern report domain");
AssertTrue(modernReportHtml.Contains("Customer Need", StringComparison.Ordinal), "modern report idea");
AssertTrue(modernReportHtml.Contains("Spec", StringComparison.Ordinal), "modern report idea detail");
AssertTrue(modernReportHtml.Contains("Addresses", StringComparison.Ordinal), "modern report relationship");
AssertTrue(modernReportHtml.Contains("Source: Customer Need", StringComparison.Ordinal), "modern report source");
AssertTrue(modernReportHtml.Contains("<svg", StringComparison.Ordinal), "modern report view svg");
AssertTrue(modernReportHtml.Contains("Checklist", StringComparison.Ordinal), "modern report table definition");

var generationDocument = modernDocument with
{
    Domain = modernDocument.Domain with
    {
        Templates =
        [
            new CompositionExtensionSnapshot(
                "legacy.template.concept.default",
                "%%:FILENAME={{Name}}.md\n# {{Name}}\nDefinition={{Definition.Name}}\nSpec={{Details.Spec}}"),
            new CompositionExtensionSnapshot(
                "legacy.template.relationship.default",
                "%%:FILENAME={{Name}}.rel.txt\n{{Source.Name}} -> {{Target.Name}}: {{Name}}")
        ]
    }
};
var generatedFiles = CompositionDocumentFileGenerator.Generate(generationDocument);
AssertEqual(2, generatedFiles.Files.Count, "modern generation file count");
var generatedConceptFile = generatedFiles.Files.Single(file => file.RelativePath == "Customer Need.md");
AssertTrue(generatedConceptFile.Content.Contains("# Customer Need", StringComparison.Ordinal), "modern generation concept name");
AssertTrue(generatedConceptFile.Content.Contains("Definition=Concept", StringComparison.Ordinal), "modern generation definition");
AssertTrue(generatedConceptFile.Content.Contains("Spec=spec.pdf", StringComparison.Ordinal), "modern generation detail");
var generatedRelationshipFile = generatedFiles.Files.Single(file => file.RelativePath == "Addresses.rel.txt");
AssertTrue(generatedRelationshipFile.Content.Contains("Customer Need -> Customer Need: Addresses", StringComparison.Ordinal), "modern generation relationship");
var documentPreviewText = CompositionDocumentPreviewTextBuilder.Build(generationDocument);
AssertTrue(documentPreviewText.Contains("Modern Document", StringComparison.Ordinal), "document preview title");
AssertTrue(documentPreviewText.Contains("Domain: All Purpose", StringComparison.Ordinal), "document preview domain");
AssertTrue(documentPreviewText.Contains("Concept: Customer Need", StringComparison.Ordinal), "document preview idea");
AssertTrue(documentPreviewText.Contains("Marker: Risk", StringComparison.Ordinal), "document preview marker");
AssertTrue(documentPreviewText.Contains("Detail: Spec = spec.pdf", StringComparison.Ordinal), "document preview detail");
AssertTrue(documentPreviewText.Contains("Generated files (2)", StringComparison.Ordinal), "document preview generated count");
AssertTrue(documentPreviewText.Contains("Customer Need.md", StringComparison.Ordinal), "document preview generated concept file");
AssertTrue(documentPreviewText.Contains("Addresses.rel.txt", StringComparison.Ordinal), "document preview generated relationship file");
var documentCommands = CompositionCommandCatalog.ForDocument(generationDocument);
AssertTrue(documentCommands.Any(entry => entry.Kind == CompositionCommandEntryKind.Definition && entry.TargetId == "marker-def"), "document command marker definition");
AssertTrue(documentCommands.Any(entry => entry.Kind == CompositionCommandEntryKind.Template && entry.TargetId == "legacy.template.concept.default"), "document command template");
var templateEditedDocument = CompositionDocumentSnapshotEditor.UpsertDomainTemplate(
    modernDocument,
    new CompositionExtensionSnapshot(
        "template.concept.note",
        "%%:FILENAME={{Name}}.note.md\n{{Document.Title}}/{{Name}}"));
AssertTrue(templateEditedDocument.Domain.Templates.Any(template => template.Key == "template.concept.note"), "template added");
AssertEqual(1, CompositionDocumentFileGenerator.Generate(templateEditedDocument).Files.Count, "template generation after add");
var templateUpdatedDocument = CompositionDocumentSnapshotEditor.UpsertDomainTemplate(
    templateEditedDocument,
    new CompositionExtensionSnapshot(
        "template.concept.note",
        "%%:FILENAME={{Name}}.updated.md\n{{Name}}"));
AssertEqual("{{Name}}", templateUpdatedDocument.Domain.Templates.Single(template => template.Key == "template.concept.note").Value.Split('\n').Last(), "template updated");
var templateDeletedDocument = CompositionDocumentSnapshotEditor.DeleteDomainTemplate(templateUpdatedDocument, "template.concept.note");
AssertTrue(!templateDeletedDocument.Domain.Templates.Any(template => template.Key == "template.concept.note"), "template deleted");

var modernDocumentPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-modern-{Guid.NewGuid():N}.tcdoc");
try
{
    CompositionDocumentSnapshotXmlStore.Save(modernDocument, modernDocumentPath);
    var reloadedModernDocument = CompositionDocumentSnapshotXmlStore.Load(modernDocumentPath);

    AssertEqual(modernDocument.SchemaVersion, reloadedModernDocument.SchemaVersion, "modern roundtrip schema");
    AssertEqual("Modern Document", reloadedModernDocument.Title, "modern roundtrip title");
    AssertEqual("All Purpose", reloadedModernDocument.Domain.Name, "modern roundtrip domain");
    AssertEqual("Relationship", reloadedModernDocument.Domain.RelationshipDefinitions[0].Name, "modern roundtrip relationship definition");
    AssertEqual("Risk", reloadedModernDocument.Domain.MarkerDefinitions[0].Name, "modern roundtrip marker definition");
    AssertEqual("Checklist", reloadedModernDocument.Domain.TableDefinitions[0].Name, "modern roundtrip table definition");
    AssertEqual("SQL", reloadedModernDocument.Domain.ExternalLanguages[0].Name, "modern roundtrip external language");
    AssertEqual("select * from ideas", reloadedModernDocument.Domain.Templates[0].Value, "modern roundtrip template");
    AssertEqual("Customer Need", reloadedModernDocument.Ideas[0].Name, "modern roundtrip idea");
    AssertEqual("Spec", reloadedModernDocument.Ideas[0].Details[0].Name, "modern roundtrip idea detail");
    AssertEqual("marker-def", reloadedModernDocument.Ideas[0].Markers[0], "modern roundtrip idea marker");
    AssertEqual("#f8f8f8", reloadedModernDocument.Ideas[0].Style.Fill, "modern roundtrip idea style");
    AssertEqual("High", reloadedModernDocument.Relationships[0].Details[0].Value, "modern roundtrip relationship detail");
    AssertEqual("Legend", reloadedModernDocument.Views[0].Complements[0].Value, "modern roundtrip complement");
    AssertEqual("legacy.package.raw", reloadedModernDocument.Extensions[0].Key, "modern roundtrip extension");
}
finally
{
    if (File.Exists(modernDocumentPath))
    {
        File.Delete(modernDocumentPath);
    }
}

var modernFromView = CompositionDocumentSnapshotAdapter.FromViewSnapshot(snapshot);
AssertEqual(snapshot.Id, modernFromView.Id, "adapter document id");
AssertEqual(snapshot.Title, modernFromView.Title, "adapter document title");
AssertEqual(snapshot.Nodes.Count, modernFromView.Ideas.Count, "adapter idea count");
AssertEqual(snapshot.Connectors.Count, modernFromView.Relationships.Count, "adapter relationship count");
AssertEqual("Default Domain", modernFromView.Domain.Name, "adapter default domain");
AssertEqual("Customer Need", modernFromView.Ideas.Single(idea => idea.Id == "customer").Name, "adapter idea name");
AssertEqual("capability", modernFromView.Relationships.Single(relationship => relationship.Id == "customer-capability").TargetIdeaId, "adapter relationship target");
AssertTrue(CompositionDocumentPersistenceAdvisor.RequiresModernDocument(modernDocument), "modern document requires modern persistence");
AssertTrue(CompositionDocumentPersistenceAdvisor.RequiresModernDocument(templateEditedDocument), "template document requires modern persistence");
AssertTrue(!CompositionDocumentPersistenceAdvisor.RequiresModernDocument(modernFromView), "view projection does not require modern persistence");

var projectedSnapshot = CompositionDocumentSnapshotAdapter.ToViewSnapshot(modernFromView);
AssertEqual(snapshot.Id, projectedSnapshot.Id, "adapter projected id");
AssertEqual(snapshot.Title, projectedSnapshot.Title, "adapter projected title");
AssertEqual(snapshot.Nodes.Count, projectedSnapshot.Nodes.Count, "adapter projected node count");
AssertEqual(snapshot.Connectors.Count, projectedSnapshot.Connectors.Count, "adapter projected connector count");
AssertEqual("Customer Need", projectedSnapshot.Nodes.Single(node => node.Id == "customer").Text, "adapter projected node text");
AssertEqual("capability", projectedSnapshot.Connectors.Single(connector => connector.Id == "customer-capability").TargetId, "adapter projected connector target");

var styledView = new CompositionViewSnapshot(
    "styled",
    "Styled View",
    [
        new CompositionNodeView(
            "styled-node",
            "Styled Node",
            new TcPoint(10, 20),
            new TcSize(140, 60),
            Style: new CompositionStyleSnapshot(Fill: "#fef3c7", Stroke: "#b45309", Text: "#111827", StrokeThickness: 2.5))
    ],
    [
        new CompositionConnectorView(
            "styled-link",
            "styled-node",
            "styled-node",
            "Styled Link",
            Style: new CompositionStyleSnapshot(Stroke: "#16a34a", StrokeThickness: 3.5))
    ]);
var styledDocument = CompositionDocumentSnapshotAdapter.FromViewSnapshot(styledView);
AssertEqual("#fef3c7", styledDocument.Ideas.Single().Style.Fill, "adapter stores node fill style");
AssertEqual("#16a34a", styledDocument.Relationships.Single().Style.Stroke, "adapter stores connector stroke style");
var styledProjected = CompositionDocumentSnapshotAdapter.ToViewSnapshot(styledDocument);
AssertEqual("#fef3c7", styledProjected.Nodes.Single().Style.Fill, "adapter projects node fill style");
AssertEqual("#16a34a", styledProjected.Connectors.Single().Style.Stroke, "adapter projects connector stroke style");
var styledSvg = CompositionSnapshotSvgExporter.Export(styledProjected);
AssertTrue(styledSvg.Contains("fill=\"#fef3c7\"", StringComparison.Ordinal), "svg node fill style");
AssertTrue(styledSvg.Contains("stroke=\"#16a34a\"", StringComparison.Ordinal), "svg connector stroke style");

var styledViewPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-styled-{Guid.NewGuid():N}.tcview");
try
{
    CompositionViewSnapshotXmlStore.Save(styledView, styledViewPath);
    var reloadedStyledView = CompositionViewSnapshotXmlStore.Load(styledViewPath);
    AssertEqual("#fef3c7", reloadedStyledView.Nodes.Single().Style.Fill, "view roundtrip node fill style");
    AssertEqual("#16a34a", reloadedStyledView.Connectors.Single().Style.Stroke, "view roundtrip connector stroke style");
}
finally
{
    if (File.Exists(styledViewPath))
    {
        File.Delete(styledViewPath);
    }
}

var renamedModernSnapshot = CompositionSnapshotEditor.RenameNode(projectedSnapshot, "customer", "Renamed Need");
var updatedModernDocument = CompositionDocumentSnapshotEditor.ApplyViewSnapshot(modernFromView, renamedModernSnapshot);
AssertEqual("Renamed Need", updatedModernDocument.Ideas.Single(idea => idea.Id == "customer").Name, "modern edit updates idea");
AssertEqual("Renamed Need", updatedModernDocument.Views[0].Nodes.Single(node => node.Id == "customer").Text, "modern edit updates view node");
AssertEqual(modernFromView.Relationships.Count, updatedModernDocument.Relationships.Count, "modern edit preserves relationships");

var detailedDocument = CompositionDocumentSnapshotEditor.UpsertIdeaDetail(
    modernDocument,
    "idea-1",
    new CompositionDetailSnapshot("detail-added", "CustomField", "Owner", "Pablo"));
AssertEqual("Pablo", detailedDocument.Ideas.Single(idea => idea.Id == "idea-1").Details.Single(detail => detail.Name == "Owner").Value, "idea detail added");
var updatedDetailDocument = CompositionDocumentSnapshotEditor.UpsertIdeaDetail(
    detailedDocument,
    "idea-1",
    new CompositionDetailSnapshot("detail-added", "CustomField", "Owner", "Team"));
AssertEqual("Team", updatedDetailDocument.Ideas.Single(idea => idea.Id == "idea-1").Details.Single(detail => detail.Name == "Owner").Value, "idea detail updated");
var markedDocument = CompositionDocumentSnapshotEditor.SetIdeaMarkers(updatedDetailDocument, "idea-1", ["marker-def", "risk-high"]);
AssertEqual(2, markedDocument.Ideas.Single(idea => idea.Id == "idea-1").Markers.Count, "idea markers updated");
var styledIdeaDocument = CompositionDocumentSnapshotEditor.SetIdeaStyle(
    markedDocument,
    "idea-1",
    new CompositionStyleSnapshot(Fill: "#dbeafe", Stroke: "#1d4ed8", Text: "#111827", StrokeThickness: 2));
AssertEqual("#dbeafe", styledIdeaDocument.Ideas.Single(idea => idea.Id == "idea-1").Style.Fill, "idea style fill updated");
var relationshipDetailDocument = CompositionDocumentSnapshotEditor.UpsertRelationshipDetail(
    styledIdeaDocument,
    "rel-1",
    new CompositionDetailSnapshot("rel-detail", "CustomField", "Latency", "Low"));
AssertEqual("Low", relationshipDetailDocument.Relationships.Single(relationship => relationship.Id == "rel-1").Details.Single(detail => detail.Name == "Latency").Value, "relationship detail added");
var styledRelationshipDocument = CompositionDocumentSnapshotEditor.SetRelationshipStyle(
    relationshipDetailDocument,
    "rel-1",
    new CompositionStyleSnapshot(Stroke: "#9333ea", StrokeThickness: 3));
AssertEqual("#9333ea", styledRelationshipDocument.Relationships.Single(relationship => relationship.Id == "rel-1").Style.Stroke, "relationship style stroke updated");
var typedDocument = CompositionDocumentSnapshotEditor.UpsertDefinition(
    styledRelationshipDocument,
    CompositionDefinitionGroup.Concept,
    new CompositionDefinitionSnapshot("concept-opportunity", "Opportunity", "Concept"));
typedDocument = CompositionDocumentSnapshotEditor.UpsertDefinition(
    typedDocument,
    CompositionDefinitionGroup.Relationship,
    new CompositionDefinitionSnapshot("relationship-blocks", "Blocks", "Relationship"));
var typedIdeaDocument = CompositionDocumentSnapshotEditor.SetIdeaDefinition(typedDocument, "idea-1", "concept-opportunity");
AssertEqual("concept-opportunity", typedIdeaDocument.Ideas.Single(idea => idea.Id == "idea-1").DefinitionId, "idea definition updated");
var typedRelationshipDocument = CompositionDocumentSnapshotEditor.SetRelationshipDefinition(typedIdeaDocument, "rel-1", "relationship-blocks");
AssertEqual("relationship-blocks", typedRelationshipDocument.Relationships.Single(relationship => relationship.Id == "rel-1").DefinitionId, "relationship definition updated");
var deletedDetailDocument = CompositionDocumentSnapshotEditor.DeleteIdeaDetail(styledRelationshipDocument, "idea-1", "detail-added");
AssertTrue(!deletedDetailDocument.Ideas.Single(idea => idea.Id == "idea-1").Details.Any(detail => detail.Id == "detail-added"), "idea detail deleted");

var definitionEditedDocument = CompositionDocumentSnapshotEditor.UpsertDefinition(
    modernDocument,
    CompositionDefinitionGroup.Marker,
    new CompositionDefinitionSnapshot("risk-high", "High Risk", "Marker", "Needs attention"));
AssertTrue(definitionEditedDocument.Domain.MarkerDefinitions.Any(definition => definition.Id == "risk-high"), "definition added");
var definitionUpdatedDocument = CompositionDocumentSnapshotEditor.UpsertDefinition(
    definitionEditedDocument,
    CompositionDefinitionGroup.Marker,
    new CompositionDefinitionSnapshot("risk-high", "Critical Risk", "Marker"));
AssertEqual("Critical Risk", definitionUpdatedDocument.Domain.MarkerDefinitions.Single(definition => definition.Id == "risk-high").Name, "definition updated");
var definitionDeletedDocument = CompositionDocumentSnapshotEditor.DeleteDefinition(
    definitionUpdatedDocument,
    CompositionDefinitionGroup.Marker,
    "risk-high");
AssertTrue(!definitionDeletedDocument.Domain.MarkerDefinitions.Any(definition => definition.Id == "risk-high"), "definition deleted");

var incomingDocument = new CompositionDocumentSnapshot(
    Id: "incoming-doc",
    Title: "Incoming",
    Domain: new CompositionDomainSnapshot(
        Id: "domain-incoming",
        Name: "Incoming Domain",
        ConceptDefinitions:
        [
            new CompositionDefinitionSnapshot("incoming-concept", "Incoming Concept", "Concept")
        ]),
    Ideas:
    [
        new CompositionIdeaSnapshot("customer", "Imported Customer", "incoming-concept")
    ],
    Relationships:
    [
        new CompositionRelationshipSnapshot("customer-capability", "Imported Link", "customer", "customer", "relationship.default")
    ],
    Views:
    [
        new CompositionViewLayerSnapshot(
            "incoming-view",
            "Incoming",
            [new CompositionNodeView("customer", "Imported Customer", new TcPoint(10, 20), new TcSize(120, 60))],
            [new CompositionConnectorView("customer-capability", "customer", "customer", "Imported Link")])
    ]);
var mergedDocument = CompositionDocumentMerger.Merge(modernFromView, incomingDocument);
AssertEqual(modernFromView.Ideas.Count + 1, mergedDocument.Ideas.Count, "merge idea count");
AssertTrue(mergedDocument.Domain.ConceptDefinitions.Any(definition => definition.Id == "incoming-concept"), "merge domain definition");
var mergedIncomingIdea = mergedDocument.Ideas.Single(idea => idea.Name == "Imported Customer");
AssertTrue(mergedIncomingIdea.Id != "customer", "merge remaps duplicate idea id");
var mergedIncomingRelationship = mergedDocument.Relationships.Single(relationship => relationship.Name == "Imported Link");
AssertEqual(mergedIncomingIdea.Id, mergedIncomingRelationship.SourceIdeaId, "merge remaps relationship source");
AssertEqual(mergedIncomingIdea.Id, mergedDocument.Views[0].Nodes.Single(node => node.Text == "Imported Customer").Id, "merge remaps view node");
AssertEqual(58.0, mergedDocument.Views[0].Nodes.Single(node => node.Text == "Imported Customer").Position.X, "merge offsets imported x");

var settingsPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-settings-{Guid.NewGuid():N}.xml");
try
{
    var settings = new CompositionWorkspaceSettings(
        Theme: CompositionWorkspaceTheme.Dark,
        IsExplorerVisible: false,
        IsInspectorVisible: true,
        IsBottomVisible: false,
        RecentFiles: ["C:\\Docs\\One.tcview", "C:\\Docs\\Two.tdom"]);
    CompositionWorkspaceSettingsXmlStore.Save(settings, settingsPath);
    var reloadedSettings = CompositionWorkspaceSettingsXmlStore.LoadOrDefault(settingsPath);
    AssertEqual(CompositionWorkspaceTheme.Dark, reloadedSettings.Theme, "settings theme");
    AssertEqual(false, reloadedSettings.IsExplorerVisible, "settings explorer");
    AssertEqual(true, reloadedSettings.IsInspectorVisible, "settings inspector");
    AssertEqual(false, reloadedSettings.IsBottomVisible, "settings bottom");
    AssertEqual(2, reloadedSettings.RecentFiles.Count, "settings recent count");
    AssertEqual("C:\\Docs\\One.tcview", reloadedSettings.RecentFiles[0], "settings recent first");
}
finally
{
    if (File.Exists(settingsPath))
    {
        File.Delete(settingsPath);
    }
}

var commandEntries = CompositionCommandCatalog.ForSnapshot(snapshot);
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.Open), "command catalog open");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.ExportHtml), "command catalog export");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.ReportHtml), "command catalog report");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.GenerateFiles), "command catalog generate files");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.MergeDocument), "command catalog merge");
AssertTrue(commandEntries.Any(entry => entry.Kind == CompositionCommandEntryKind.Command && entry.Id == CompositionCommandIds.PrintPreview), "command catalog print preview");
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

var copiedSelection = CompositionSnapshotSelectionEditor.Copy(indexedSnapshot, ["left", "right"]);
AssertEqual(2, copiedSelection.Nodes.Count, "copy selection node count");
AssertEqual(2, copiedSelection.Connectors.Count, "copy selection connector count");

var pastedSelectionSnapshot = CompositionSnapshotSelectionEditor.Paste(indexedSnapshot, copiedSelection, new TcPoint(25, 35));
AssertEqual(4, pastedSelectionSnapshot.Nodes.Count, "paste selection node count");
AssertEqual(4, pastedSelectionSnapshot.Connectors.Count, "paste selection connector count");
AssertEqual(125.0, pastedSelectionSnapshot.Nodes.Single(node => node.Id == "left-copy").Position.X, "paste selection x");
AssertEqual("right-copy", pastedSelectionSnapshot.Connectors.Single(connector => connector.Id == "left-to-right-copy").TargetId, "paste selection connector target");

var deletedSelectionSnapshot = CompositionSnapshotSelectionEditor.Delete(indexedSnapshot, ["left", "right"]);
AssertEqual(0, deletedSelectionSnapshot.Nodes.Count, "delete selection node count");
AssertEqual(0, deletedSelectionSnapshot.Connectors.Count, "delete selection connector count");

var movedSelectionSnapshot = CompositionSnapshotSelectionEditor.Move(indexedSnapshot, ["left", "right"], new TcPoint(5, -10));
AssertEqual(105.0, movedSelectionSnapshot.Nodes.Single(node => node.Id == "left").Position.X, "move selection left x");
AssertEqual(190.0, movedSelectionSnapshot.Nodes.Single(node => node.Id == "left").Position.Y, "move selection left y");
AssertEqual(305.0, movedSelectionSnapshot.Nodes.Single(node => node.Id == "right").Position.X, "move selection right x");
AssertEqual(indexedSnapshot.Connectors.Count, movedSelectionSnapshot.Connectors.Count, "move selection preserves connectors");

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
