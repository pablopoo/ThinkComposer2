namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDocumentTextReplaceAllResult(
    CompositionDocumentSnapshot Document,
    int ReplacementCount);

public static class CompositionDocumentTextReplacer
{
    public static CompositionDocumentSnapshot ReplaceSelected(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string replacementText)
    {
        return ReplaceSelected(document, result, SearchTextFromResult(result), replacementText);
    }

    public static CompositionDocumentSnapshot ReplaceSelected(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (result is null || string.IsNullOrEmpty(searchText) || !result.IsReplaceable)
        {
            return document;
        }

        return ApplyReplacement(document, result, searchText, replacementText).Document;
    }

    public static CompositionDocumentTextReplaceAllResult ReplaceAll(
        CompositionDocumentSnapshot document,
        string searchText,
        string replacementText)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(searchText))
        {
            return new CompositionDocumentTextReplaceAllResult(document, 0);
        }

        var current = document;
        var count = 0;
        foreach (var result in CompositionDocumentTextSearch.Search(document, searchText, int.MaxValue).Where(result => result.IsReplaceable))
        {
            var replaced = ApplyReplacement(current, result, searchText, replacementText);
            current = replaced.Document;
            count += replaced.ReplacementCount;
        }

        return new CompositionDocumentTextReplaceAllResult(current, count);
    }

    private static CompositionDocumentTextReplaceAllResult ApplyReplacement(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        return result.Kind switch
        {
            CompositionDocumentTextSearchResultKind.Idea => ReplaceIdea(document, result.TargetId, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.Relationship => ReplaceRelationship(document, result.TargetId, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.Detail => ReplaceDetail(document, result, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.View => ReplaceView(document, result.TargetId, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.Definition => ReplaceDefinition(document, result.TargetId, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.Template => ReplaceTemplate(document, result, searchText, replacementText),
            CompositionDocumentTextSearchResultKind.Complement => ReplaceComplement(document, result, searchText, replacementText),
            _ => new CompositionDocumentTextReplaceAllResult(document, 0)
        };
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceIdea(
        CompositionDocumentSnapshot document,
        string ideaId,
        string searchText,
        string replacementText)
    {
        var count = 0;
        var ideas = document.Ideas.Select(idea =>
        {
            if (!string.Equals(idea.Id, ideaId, StringComparison.Ordinal))
            {
                return idea;
            }

            var replacement = ReplaceText(idea.Name, searchText, replacementText, out var replacements);
            count += replacements;
            return idea with { Name = replacement };
        }).ToArray();

        var views = document.Views.Select(view =>
        {
            var nodes = view.Nodes.Select(node =>
            {
                if (!string.Equals(node.Id, ideaId, StringComparison.Ordinal))
                {
                    return node;
                }

                var replacement = ReplaceText(node.Text, searchText, replacementText, out var replacements);
                count += replacements;
                return node with { Text = replacement };
            }).ToArray();
            return view with { Nodes = nodes };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Ideas = ideas, Views = views }, count);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceRelationship(
        CompositionDocumentSnapshot document,
        string relationshipId,
        string searchText,
        string replacementText)
    {
        var count = 0;
        var relationships = document.Relationships.Select(relationship =>
        {
            if (!string.Equals(relationship.Id, relationshipId, StringComparison.Ordinal))
            {
                return relationship;
            }

            var replacement = ReplaceText(relationship.Name, searchText, replacementText, out var replacements);
            count += replacements;
            return relationship with { Name = replacement };
        }).ToArray();

        var views = document.Views.Select(view =>
        {
            var connectors = view.Connectors.Select(connector =>
            {
                if (!string.Equals(connector.Id, relationshipId, StringComparison.Ordinal))
                {
                    return connector;
                }

                var replacement = ReplaceText(connector.Text, searchText, replacementText, out var replacements);
                count += replacements;
                return connector with { Text = replacement };
            }).ToArray();
            return view with { Connectors = connectors };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Relationships = relationships, Views = views }, count);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceDetail(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        if (result.FieldPath.StartsWith("idea:", StringComparison.Ordinal))
        {
            return ReplaceIdeaDetail(document, result, searchText, replacementText);
        }

        if (result.FieldPath.StartsWith("relationship:", StringComparison.Ordinal))
        {
            return ReplaceRelationshipDetail(document, result, searchText, replacementText);
        }

        return new CompositionDocumentTextReplaceAllResult(document, 0);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceIdeaDetail(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        var ownerId = Segment(result.FieldPath, 1);
        var count = 0;
        var ideas = document.Ideas.Select(idea =>
        {
            if (!string.Equals(idea.Id, ownerId, StringComparison.Ordinal))
            {
                return idea;
            }

            var details = idea.Details.Select(detail => ReplaceDetailField(detail, result, searchText, replacementText, ref count)).ToArray();
            return idea with { Details = details };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Ideas = ideas }, count);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceRelationshipDetail(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        var ownerId = Segment(result.FieldPath, 1);
        var count = 0;
        var relationships = document.Relationships.Select(relationship =>
        {
            if (!string.Equals(relationship.Id, ownerId, StringComparison.Ordinal))
            {
                return relationship;
            }

            var details = relationship.Details.Select(detail => ReplaceDetailField(detail, result, searchText, replacementText, ref count)).ToArray();
            return relationship with { Details = details };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Relationships = relationships }, count);
    }

    private static CompositionDetailSnapshot ReplaceDetailField(
        CompositionDetailSnapshot detail,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText,
        ref int count)
    {
        if (!string.Equals(detail.Id, result.TargetId, StringComparison.Ordinal))
        {
            return detail;
        }

        if (result.FieldPath.EndsWith("detail.name", StringComparison.Ordinal))
        {
            var replacement = ReplaceText(detail.Name, searchText, replacementText, out var replacements);
            count += replacements;
            return detail with { Name = replacement };
        }

        if (result.FieldPath.EndsWith("detail.value", StringComparison.Ordinal))
        {
            var replacement = ReplaceText(detail.Value, searchText, replacementText, out var replacements);
            count += replacements;
            return detail with { Value = replacement };
        }

        return detail;
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceView(
        CompositionDocumentSnapshot document,
        string viewId,
        string searchText,
        string replacementText)
    {
        var count = 0;
        var views = document.Views.Select(view =>
        {
            if (!string.Equals(view.Id, viewId, StringComparison.Ordinal))
            {
                return view;
            }

            var replacement = ReplaceText(view.Name, searchText, replacementText, out var replacements);
            count += replacements;
            return view with { Name = replacement };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Views = views }, count);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceDefinition(
        CompositionDocumentSnapshot document,
        string definitionId,
        string searchText,
        string replacementText)
    {
        var count = 0;
        var domain = document.Domain;
        domain = domain with { ConceptDefinitions = ReplaceDefinitions(domain.ConceptDefinitions, definitionId, searchText, replacementText, ref count) };
        domain = domain with { RelationshipDefinitions = ReplaceDefinitions(domain.RelationshipDefinitions, definitionId, searchText, replacementText, ref count) };
        domain = domain with { LinkRoleDefinitions = ReplaceDefinitions(domain.LinkRoleDefinitions, definitionId, searchText, replacementText, ref count) };
        domain = domain with { MarkerDefinitions = ReplaceDefinitions(domain.MarkerDefinitions, definitionId, searchText, replacementText, ref count) };
        domain = domain with { TableDefinitions = ReplaceDefinitions(domain.TableDefinitions, definitionId, searchText, replacementText, ref count) };
        domain = domain with { ExternalLanguages = ReplaceDefinitions(domain.ExternalLanguages, definitionId, searchText, replacementText, ref count) };
        return new CompositionDocumentTextReplaceAllResult(document with { Domain = domain }, count);
    }

    private static IReadOnlyList<CompositionDefinitionSnapshot> ReplaceDefinitions(
        IReadOnlyList<CompositionDefinitionSnapshot> definitions,
        string definitionId,
        string searchText,
        string replacementText,
        ref int count)
    {
        var result = new List<CompositionDefinitionSnapshot>(definitions.Count);
        foreach (var definition in definitions)
        {
            if (!string.Equals(definition.Id, definitionId, StringComparison.Ordinal))
            {
                result.Add(definition);
                continue;
            }

            var replacement = ReplaceText(definition.Name, searchText, replacementText, out var replacements);
            count += replacements;
            result.Add(definition with { Name = replacement });
        }

        return result;
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceTemplate(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        var count = 0;
        var templates = document.Domain.Templates.Select(template =>
        {
            if (!string.Equals(template.Key, result.TargetId, StringComparison.Ordinal))
            {
                return template;
            }

            var key = template.Key;
            var value = template.Value;
            if (result.FieldPath == "template.key")
            {
                key = ReplaceText(key, searchText, replacementText, out var replacements);
                count += replacements;
            }
            else
            {
                value = ReplaceText(value, searchText, replacementText, out var replacements);
                count += replacements;
            }

            return new CompositionExtensionSnapshot(key, value);
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Domain = document.Domain with { Templates = templates } }, count);
    }

    private static CompositionDocumentTextReplaceAllResult ReplaceComplement(
        CompositionDocumentSnapshot document,
        CompositionDocumentTextSearchResult result,
        string searchText,
        string replacementText)
    {
        var viewId = Segment(result.FieldPath, 1);
        var count = 0;
        var views = document.Views.Select(view =>
        {
            if (!string.Equals(view.Id, viewId, StringComparison.Ordinal))
            {
                return view;
            }

            var complements = view.Complements.Select(complement =>
            {
                if (!string.Equals(complement.Key, result.TargetId, StringComparison.Ordinal))
                {
                    return complement;
                }

                if (result.FieldPath.EndsWith("complement.key", StringComparison.Ordinal))
                {
                    var replacement = ReplaceText(complement.Key, searchText, replacementText, out var replacements);
                    count += replacements;
                    return new CompositionExtensionSnapshot(replacement, complement.Value);
                }

                var value = ReplaceText(complement.Value, searchText, replacementText, out var valueReplacements);
                count += valueReplacements;
                return new CompositionExtensionSnapshot(complement.Key, value);
            }).ToArray();
            return view with { Complements = complements };
        }).ToArray();

        return new CompositionDocumentTextReplaceAllResult(document with { Views = views }, count);
    }

    private static string ReplaceText(string value, string searchText, string replacementText, out int count)
    {
        count = 0;
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(searchText))
        {
            return value;
        }

        var result = value;
        var index = result.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            result = result.Remove(index, searchText.Length).Insert(index, replacementText);
            count++;
            index = result.IndexOf(searchText, index + replacementText.Length, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string SearchTextFromResult(CompositionDocumentTextSearchResult result)
    {
        var marker = result.Title.IndexOf(": ", StringComparison.Ordinal);
        return marker >= 0 ? result.Title.Substring(marker + 2) : result.Title;
    }

    private static string Segment(string fieldPath, int index)
    {
        var segments = fieldPath.Split(':');
        return index >= 0 && index < segments.Length ? segments[index] : string.Empty;
    }
}
