namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionCommandCatalog
{
    private const string MissingDocumentReason = "Open or create a document first.";
    private const string MissingSnapshotReason = "Open or create a composition view first.";
    private const string MissingSelectionReason = "Select a concept or relationship first.";
    private const string MissingSingleConceptReason = "Select one concept first.";
    private const string MissingMultipleConceptsReason = "Select two or more concepts first.";
    private const string MissingClipboardReason = "Clipboard is empty.";
    private const string MissingUndoReason = "Nothing to undo.";
    private const string MissingRedoReason = "Nothing to redo.";

    public static IReadOnlyList<CompositionCommandEntry> ForSnapshot(CompositionViewSnapshot? snapshot)
    {
        return ForSnapshot(snapshot, CompositionCommandContext.Empty);
    }

    public static IReadOnlyList<CompositionCommandEntry> ForSnapshot(
        CompositionViewSnapshot? snapshot,
        CompositionCommandContext context)
    {
        var entries = new List<CompositionCommandEntry>(BuildBaseCommands(context));
        if (snapshot is null)
        {
            return entries;
        }

        entries.AddRange(snapshot.Nodes.Select(node =>
            new CompositionCommandEntry(
                $"node.{node.Id}",
                string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text,
                CompositionCommandEntryKind.Node,
                node.Id,
                "Concept",
                CompositionCommandCategory.Canvas,
                CompositionCommandSurface.CommandPalette)));

        entries.AddRange(snapshot.Connectors.Select(connector =>
            new CompositionCommandEntry(
                $"connector.{connector.Id}",
                string.IsNullOrWhiteSpace(connector.Text) ? "Relationship" : connector.Text,
                CompositionCommandEntryKind.Connector,
                connector.Id,
                "Relationship",
                CompositionCommandCategory.Canvas,
                CompositionCommandSurface.CommandPalette)));

        return entries;
    }

    public static IReadOnlyList<CompositionCommandEntry> ForDocument(CompositionDocumentSnapshot? document)
    {
        return ForDocument(document, CompositionCommandContext.Empty);
    }

    public static IReadOnlyList<CompositionCommandEntry> ForDocument(
        CompositionDocumentSnapshot? document,
        CompositionCommandContext context)
    {
        var entries = new List<CompositionCommandEntry>(BuildBaseCommands(context));
        if (document is null)
        {
            return entries;
        }

        entries.AddRange(document.Ideas.Select(idea =>
            new CompositionCommandEntry(
                $"node.{idea.Id}",
                string.IsNullOrWhiteSpace(idea.Name) ? idea.Id : idea.Name,
                CompositionCommandEntryKind.Node,
                idea.Id,
                "Concept",
                CompositionCommandCategory.Canvas,
                CompositionCommandSurface.CommandPalette)));

        entries.AddRange(document.Relationships.Select(relationship =>
            new CompositionCommandEntry(
                $"connector.{relationship.Id}",
                string.IsNullOrWhiteSpace(relationship.Name) ? "Relationship" : relationship.Name,
                CompositionCommandEntryKind.Connector,
                relationship.Id,
                "Relationship",
                CompositionCommandCategory.Canvas,
                CompositionCommandSurface.CommandPalette)));

        entries.AddRange(document.Views.Select(view =>
            new CompositionCommandEntry(
                $"view.{view.Id}",
                string.IsNullOrWhiteSpace(view.Name) ? view.Id : view.Name,
                CompositionCommandEntryKind.View,
                view.Id,
                string.IsNullOrWhiteSpace(view.ContainerIdeaId) ? "View" : $"View of {view.ContainerIdeaId}",
                CompositionCommandCategory.View,
                CompositionCommandSurface.CommandPalette)));

        AddDefinitions(entries, CompositionDefinitionGroup.Concept, "Concept definition", document.Domain.ConceptDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Relationship, "Relationship definition", document.Domain.RelationshipDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.LinkRole, "Link-role definition", document.Domain.LinkRoleDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Marker, "Marker definition", document.Domain.MarkerDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Table, "Table definition", document.Domain.TableDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.ExternalLanguage, "External language", document.Domain.ExternalLanguages);

        entries.AddRange(document.Domain.Templates.Select(template =>
            new CompositionCommandEntry(
                $"template.{template.Key}",
                template.Key,
                CompositionCommandEntryKind.Template,
                template.Key,
                "Generation template",
                CompositionCommandCategory.Generation,
                CompositionCommandSurface.CommandPalette | CompositionCommandSurface.Inspector)));

        entries.AddRange(document.Views.SelectMany(view => view.Complements.Select(complement =>
            new CompositionCommandEntry(
                $"complement.{view.Id}.{complement.Key}",
                string.IsNullOrWhiteSpace(complement.Value) ? complement.Key : complement.Value,
                CompositionCommandEntryKind.Complement,
                complement.Key,
                "View complement",
                CompositionCommandCategory.View,
                CompositionCommandSurface.CommandPalette | CompositionCommandSurface.Inspector))));

        return entries;
    }

    public static IReadOnlyList<CompositionCommandEntry> Search(
        IEnumerable<CompositionCommandEntry> entries,
        string query,
        int limit = 12)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return entries.Take(limit).ToArray();
        }

        var normalizedQuery = query.Trim();
        return entries
            .Where(entry =>
                Contains(entry.Title, normalizedQuery) ||
                Contains(entry.Subtitle, normalizedQuery) ||
                Contains(entry.Id, normalizedQuery) ||
                Contains(entry.Accelerator, normalizedQuery) ||
                Contains(entry.DisabledReason, normalizedQuery))
            .Take(limit)
            .ToArray();
    }

    private static IReadOnlyList<CompositionCommandEntry> BuildBaseCommands(CompositionCommandContext context)
    {
        context ??= CompositionCommandContext.Empty;
        var hasDocument = context.HasDocument;
        var hasSnapshot = context.HasSnapshot;
        var hasSelection = context.HasSelection;
        var hasSingleNode = context.HasSingleNodeSelection;
        var hasMultiNode = context.HasMultiNodeSelection;
        var hasConnector = context.HasSelectedConnector;
        var hasClipboard = context.HasClipboard;

        return
        [
            Command(CompositionCommandIds.NewDocument, "New document", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "Ctrl+N"),
            Command(CompositionCommandIds.Open, "Open", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "Ctrl+O"),
            Command(CompositionCommandIds.Save, "Save", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "Ctrl+S", hasDocument, MissingDocumentReason),
            Command(CompositionCommandIds.SaveAs, "Save As", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "Ctrl+Shift+S", hasDocument, MissingDocumentReason),
            Command(CompositionCommandIds.MergeDocument, "Merge document", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasDocument, MissingDocumentReason),
            Command(CompositionCommandIds.ExportHtml, "Export HTML/SVG", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ReportHtml, "Report HTML", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),
            Command(CompositionCommandIds.PresentationHtml, "Presentation HTML", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),
            Command(CompositionCommandIds.GenerateFiles, "Generate files", CompositionCommandCategory.Generation, CompositionCommandSurface.TopBar | CompositionCommandSurface.Inspector, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),
            Command(CompositionCommandIds.PrintPreview, "Print preview", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),
            Command(CompositionCommandIds.EditDocumentProperties, "Document properties", CompositionCommandCategory.Document, CompositionCommandSurface.Inspector, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),

            Command(CompositionCommandIds.NewConcept, "New concept", CompositionCommandCategory.Canvas, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu, "Canvas", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.NewRelationship, "New relationship", CompositionCommandCategory.Canvas, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Canvas", "", hasSingleNode, MissingSingleConceptReason),
            Command(CompositionCommandIds.EditName, "Edit name", CompositionCommandCategory.Edit, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Edit", "F2", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.ConvertType, "Convert type", CompositionCommandCategory.Edit, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Edit", "", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.OpenCompositeView, "Open composite view", CompositionCommandCategory.View, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "View", "", hasSingleNode && hasDocument, hasDocument ? MissingSingleConceptReason : MissingDocumentReason),
            Command(CompositionCommandIds.GoParent, "Go to parent", CompositionCommandCategory.View, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.CommandPalette, "View", "", hasDocument, MissingDocumentReason),

            Command(CompositionCommandIds.Undo, "Undo", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar, "Edit", "Ctrl+Z", context.CanUndo, MissingUndoReason),
            Command(CompositionCommandIds.Redo, "Redo", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar, "Edit", "Ctrl+Y", context.CanRedo, MissingRedoReason),
            Command(CompositionCommandIds.Delete, "Delete selection", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Edit", "Delete", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.Cut, "Cut", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu, "Edit", "Ctrl+X", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.Copy, "Copy", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu, "Edit", "Ctrl+C", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.Paste, "Paste", CompositionCommandCategory.Edit, CompositionCommandSurface.TopBar | CompositionCommandSurface.CanvasContextMenu, "Edit", "Ctrl+V", hasClipboard, MissingClipboardReason),
            Command(CompositionCommandIds.SelectAll, "Select all", CompositionCommandCategory.Edit, CompositionCommandSurface.CanvasContextMenu, "Edit", "Ctrl+A", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.PasteShortcut, "Paste shortcut", CompositionCommandCategory.Edit, CompositionCommandSurface.CanvasContextMenu, "Edit", "", hasDocument && hasSnapshot && hasClipboard, hasClipboard ? MissingDocumentReason : MissingClipboardReason),

            Command(CompositionCommandIds.CommandPalette, "Command palette", CompositionCommandCategory.App, CompositionCommandSurface.TopBar, "App", "Ctrl+Shift+P"),
            Command(CompositionCommandIds.FocusCanvas, "Focus canvas", CompositionCommandCategory.View, CompositionCommandSurface.TopBar, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleTheme, "Toggle theme", CompositionCommandCategory.App, CompositionCommandSurface.TopBar, "App"),

            Command(CompositionCommandIds.ActualSize, "Actual size", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ZoomIn, "Zoom in", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ZoomOut, "Zoom out", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.FitToView, "Fit to view", CompositionCommandCategory.View, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.ViewMenu, "View", "F8", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.PresentationMode, "Presentation mode", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.FullScreen, "Full-screen mode", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleGrid, "Show grid", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleSnapToGrid, "Snap to grid", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleGridPoints, "Grid points", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleIndicators, "Show indicators", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleMarkers, "Show markers", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleMarkerTitles, "Show marker titles", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleConceptDefinitionLabels, "Show concept definition labels", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleRelationshipDefinitionLabels, "Show relationship definition labels", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleLinkRoleDescriptorLabels, "Show link-role descriptor labels", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleLinkRoleDefinitorLabels, "Show link-role definitor labels", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleLinkRoleVariantLabels, "Show link-role variant labels", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),
            Command(CompositionCommandIds.ToggleAutoSizeByText, "Auto-size by entered text", CompositionCommandCategory.View, CompositionCommandSurface.ViewMenu, "View", "", hasSnapshot, MissingSnapshotReason),

            Command(CompositionCommandIds.GetFormat, "Get format", CompositionCommandCategory.Format, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Format", "", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.ApplyFormat, "Apply format", CompositionCommandCategory.Format, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Format", "", hasSelection, MissingSelectionReason),
            Command(CompositionCommandIds.AlignTop, "Align top", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.AlignLeft, "Align left", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.AlignRight, "Align right", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.AlignBottom, "Align bottom", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.AlignCenter, "Align center", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.AlignMiddle, "Align middle", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.SameWidth, "Same width", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.SameHeight, "Same height", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.SameSize, "Same size", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.DistributeHorizontally, "Distribute horizontally", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.DistributeVertically, "Distribute vertically", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", hasMultiNode, MissingMultipleConceptsReason),
            Command(CompositionCommandIds.BringToFront, "Bring to front", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", context.SelectedNodeCount > 0, MissingSelectionReason),
            Command(CompositionCommandIds.SendToBack, "Send to back", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", context.SelectedNodeCount > 0, MissingSelectionReason),
            Command(CompositionCommandIds.BringForward, "Bring forward", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", context.SelectedNodeCount > 0, MissingSelectionReason),
            Command(CompositionCommandIds.SendBackward, "Send backward", CompositionCommandCategory.Layout, CompositionCommandSurface.CanvasContextMenu, "Layout", "", context.SelectedNodeCount > 0, MissingSelectionReason),

            Command(CompositionCommandIds.ChangeRelationshipDefinition, "Change relationship definition", CompositionCommandCategory.Domain, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Domain", "", hasConnector, "Select a relationship first."),
            Command(CompositionCommandIds.ChangeLinkRole, "Change link-role variant", CompositionCommandCategory.Domain, CompositionCommandSurface.CanvasContextMenu | CompositionCommandSurface.Inspector, "Domain", "", hasConnector, "Select a relationship first."),
            Command(CompositionCommandIds.GenerationPreview, "Generation preview", CompositionCommandCategory.Generation, CompositionCommandSurface.Inspector, "Generation", "", hasDocument || hasSnapshot, MissingDocumentReason),
            Command(CompositionCommandIds.DomainStudio, "Domain Studio", CompositionCommandCategory.Domain, CompositionCommandSurface.DomainStudio | CompositionCommandSurface.Inspector, "Domain", "", hasDocument || hasSnapshot, MissingDocumentReason)
        ];
    }

    private static CompositionCommandEntry Command(
        string id,
        string title,
        CompositionCommandCategory category,
        CompositionCommandSurface surfaces,
        string subtitle,
        string accelerator = "",
        bool isEnabled = true,
        string disabledReason = "")
    {
        var allSurfaces = surfaces | CompositionCommandSurface.CommandPalette;
        return new CompositionCommandEntry(
            id,
            title,
            CompositionCommandEntryKind.Command,
            Subtitle: subtitle,
            Category: category,
            Surfaces: allSurfaces,
            Accelerator: accelerator,
            IsEnabled: isEnabled,
            DisabledReason: isEnabled ? string.Empty : disabledReason);
    }

    private static bool Contains(string value, string query)
    {
        return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void AddDefinitions(
        ICollection<CompositionCommandEntry> entries,
        CompositionDefinitionGroup group,
        string subtitle,
        IReadOnlyList<CompositionDefinitionSnapshot> definitions)
    {
        foreach (var definition in definitions)
        {
            entries.Add(new CompositionCommandEntry(
                $"definition.{group}.{definition.Id}",
                string.IsNullOrWhiteSpace(definition.Name) ? definition.Id : definition.Name,
                CompositionCommandEntryKind.Definition,
                definition.Id,
                subtitle,
                CompositionCommandCategory.Domain,
                CompositionCommandSurface.CommandPalette | CompositionCommandSurface.DomainStudio));
        }
    }
}
