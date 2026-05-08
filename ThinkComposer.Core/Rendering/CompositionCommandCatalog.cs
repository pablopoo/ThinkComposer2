namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionCommandCatalog
{
    private static readonly CompositionCommandEntry[] BaseCommands =
    [
        new(CompositionCommandIds.NewDocument, "New document", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.Open, "Open", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.Save, "Save", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.SaveAs, "Save As", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.ExportHtml, "Export HTML/SVG", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.ReportHtml, "Report HTML", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.GenerateFiles, "Generate files", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.MergeDocument, "Merge document", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.PrintPreview, "Print preview", CompositionCommandEntryKind.Command, Subtitle: "Document"),
        new(CompositionCommandIds.NewConcept, "New concept", CompositionCommandEntryKind.Command, Subtitle: "Canvas"),
        new(CompositionCommandIds.NewRelationship, "New relationship", CompositionCommandEntryKind.Command, Subtitle: "Canvas"),
        new(CompositionCommandIds.Delete, "Delete selection", CompositionCommandEntryKind.Command, Subtitle: "Edit"),
        new(CompositionCommandIds.Undo, "Undo", CompositionCommandEntryKind.Command, Subtitle: "Edit"),
        new(CompositionCommandIds.Redo, "Redo", CompositionCommandEntryKind.Command, Subtitle: "Edit"),
        new(CompositionCommandIds.FocusCanvas, "Focus canvas", CompositionCommandEntryKind.Command, Subtitle: "View"),
        new(CompositionCommandIds.ToggleTheme, "Toggle theme", CompositionCommandEntryKind.Command, Subtitle: "View")
    ];

    public static IReadOnlyList<CompositionCommandEntry> ForSnapshot(CompositionViewSnapshot? snapshot)
    {
        var entries = new List<CompositionCommandEntry>(BaseCommands);
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
                "Concept")));

        entries.AddRange(snapshot.Connectors.Select(connector =>
            new CompositionCommandEntry(
                $"connector.{connector.Id}",
                string.IsNullOrWhiteSpace(connector.Text) ? "Relationship" : connector.Text,
                CompositionCommandEntryKind.Connector,
                connector.Id,
                "Relationship")));

        return entries;
    }

    public static IReadOnlyList<CompositionCommandEntry> ForDocument(CompositionDocumentSnapshot? document)
    {
        if (document is null)
        {
            return BaseCommands;
        }

        var entries = new List<CompositionCommandEntry>(BaseCommands);
        entries.AddRange(document.Ideas.Select(idea =>
            new CompositionCommandEntry(
                $"node.{idea.Id}",
                string.IsNullOrWhiteSpace(idea.Name) ? idea.Id : idea.Name,
                CompositionCommandEntryKind.Node,
                idea.Id,
                "Concept")));

        entries.AddRange(document.Relationships.Select(relationship =>
            new CompositionCommandEntry(
                $"connector.{relationship.Id}",
                string.IsNullOrWhiteSpace(relationship.Name) ? "Relationship" : relationship.Name,
                CompositionCommandEntryKind.Connector,
                relationship.Id,
                "Relationship")));

        AddDefinitions(entries, CompositionDefinitionGroup.Concept, "Concept definition", document.Domain.ConceptDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Relationship, "Relationship definition", document.Domain.RelationshipDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Marker, "Marker definition", document.Domain.MarkerDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.Table, "Table definition", document.Domain.TableDefinitions);
        AddDefinitions(entries, CompositionDefinitionGroup.ExternalLanguage, "External language", document.Domain.ExternalLanguages);

        entries.AddRange(document.Domain.Templates.Select(template =>
            new CompositionCommandEntry(
                $"template.{template.Key}",
                template.Key,
                CompositionCommandEntryKind.Template,
                template.Key,
                "Generation template")));

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
                Contains(entry.Id, normalizedQuery))
            .Take(limit)
            .ToArray();
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
                subtitle));
        }
    }
}
