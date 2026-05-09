namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentTextSearch
{
    public static IReadOnlyList<CompositionDocumentTextSearchResult> Search(
        CompositionDocumentSnapshot document,
        string? query,
        int limit = 100)
    {
        if (document is null || string.IsNullOrWhiteSpace(query) || limit <= 0)
        {
            return Array.Empty<CompositionDocumentTextSearchResult>();
        }

        var searchText = query is null ? string.Empty : query.Trim();
        var results = new List<CompositionDocumentTextSearchResult>();

        foreach (var idea in document.Ideas)
        {
            AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Idea, idea.Id, "idea.name", idea.Name, idea.Name);
            foreach (var detail in idea.Details)
            {
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Detail, detail.Id, $"idea:{idea.Id}:detail.name", detail.Name, $"{idea.Name} / {detail.Name}");
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Detail, detail.Id, $"idea:{idea.Id}:detail.value", detail.Value, $"{idea.Name} / {detail.Name}: {detail.Value}");
            }
        }

        foreach (var relationship in document.Relationships)
        {
            AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Relationship, relationship.Id, "relationship.name", relationship.Name, relationship.Name);
            foreach (var detail in relationship.Details)
            {
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Detail, detail.Id, $"relationship:{relationship.Id}:detail.name", detail.Name, $"{relationship.Name} / {detail.Name}");
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Detail, detail.Id, $"relationship:{relationship.Id}:detail.value", detail.Value, $"{relationship.Name} / {detail.Name}: {detail.Value}");
            }
        }

        foreach (var view in document.Views)
        {
            AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.View, view.Id, "view.name", view.Name, view.Name);
            foreach (var complement in view.Complements)
            {
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Complement, complement.Key, $"view:{view.Id}:complement.key", complement.Key, $"{view.Name} / {complement.Key}");
                AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Complement, complement.Key, $"view:{view.Id}:complement.value", complement.Value, $"{view.Name} / {complement.Key}: {complement.Value}");
            }
        }

        AddDefinitions(results, searchText, limit, document.Domain.ConceptDefinitions);
        AddDefinitions(results, searchText, limit, document.Domain.RelationshipDefinitions);
        AddDefinitions(results, searchText, limit, document.Domain.LinkRoleDefinitions);
        AddDefinitions(results, searchText, limit, document.Domain.MarkerDefinitions);
        AddDefinitions(results, searchText, limit, document.Domain.TableDefinitions);
        AddDefinitions(results, searchText, limit, document.Domain.ExternalLanguages);

        foreach (var template in document.Domain.Templates)
        {
            AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Template, template.Key, "template.key", template.Key, template.Key);
            AddIfMatch(results, searchText, limit, CompositionDocumentTextSearchResultKind.Template, template.Key, "template.value", template.Value, $"{template.Key}: {template.Value}");
        }

        return results.Take(limit).ToArray();
    }

    private static void AddDefinitions(
        List<CompositionDocumentTextSearchResult> results,
        string query,
        int limit,
        IEnumerable<CompositionDefinitionSnapshot> definitions)
    {
        foreach (var definition in definitions)
        {
            AddIfMatch(results, query, limit, CompositionDocumentTextSearchResultKind.Definition, definition.Id, "definition.name", definition.Name, definition.Name);
        }
    }

    private static void AddIfMatch(
        List<CompositionDocumentTextSearchResult> results,
        string query,
        int limit,
        CompositionDocumentTextSearchResultKind kind,
        string targetId,
        string fieldPath,
        string value,
        string preview)
    {
        if (results.Count >= limit || string.IsNullOrEmpty(value) ||
            value.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        results.Add(new CompositionDocumentTextSearchResult(
            kind,
            targetId,
            fieldPath,
            TitleFor(kind, value),
            preview));
    }

    private static string TitleFor(CompositionDocumentTextSearchResultKind kind, string value)
    {
        return $"{kind}: {value}";
    }
}
