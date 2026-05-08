using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentPreviewTextBuilder
{
    public static string Build(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var definitions = BuildDefinitionsIndex(document.Domain);
        var ideas = document.Ideas.ToDictionary(idea => idea.Id, StringComparer.Ordinal);
        var generatedFiles = CompositionDocumentFileGenerator.Generate(document).Files;
        var builder = new StringBuilder();

        builder.AppendLine(document.Title);
        builder.AppendLine($"Domain: {document.Domain.Name}");
        builder.AppendLine($"Concepts: {document.Ideas.Count}");
        builder.AppendLine($"Relationships: {document.Relationships.Count}");
        builder.AppendLine($"Views: {document.Views.Count}");
        builder.AppendLine();

        AppendDomain(builder, document.Domain);
        AppendIdeas(builder, document.Ideas, definitions);
        AppendRelationships(builder, document.Relationships, definitions, ideas);
        AppendGeneratedFiles(builder, generatedFiles);

        return builder.ToString();
    }

    private static void AppendDomain(StringBuilder builder, CompositionDomainSnapshot domain)
    {
        builder.AppendLine("Domain");
        AppendDefinitionCount(builder, "Concept definitions", domain.ConceptDefinitions);
        AppendDefinitionCount(builder, "Relationship definitions", domain.RelationshipDefinitions);
        AppendDefinitionCount(builder, "Link-role variants", domain.LinkRoleDefinitions);
        AppendDefinitionCount(builder, "Markers", domain.MarkerDefinitions);
        AppendDefinitionCount(builder, "Tables", domain.TableDefinitions);
        AppendDefinitionCount(builder, "External languages", domain.ExternalLanguages);
        builder.AppendLine($"Generation templates: {domain.Templates.Count}");
        builder.AppendLine();
    }

    private static void AppendDefinitionCount(
        StringBuilder builder,
        string label,
        IReadOnlyList<CompositionDefinitionSnapshot> definitions)
    {
        builder.AppendLine($"- {label}: {definitions.Count}");
        foreach (var definition in definitions.OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {definition.Name}");
        }
    }

    private static void AppendIdeas(
        StringBuilder builder,
        IReadOnlyList<CompositionIdeaSnapshot> ideas,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions)
    {
        builder.AppendLine($"Concepts ({ideas.Count})");
        foreach (var idea in ideas.OrderBy(idea => idea.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- Concept: {idea.Name}");
            builder.AppendLine($"  Definition: {FindDefinitionName(definitions, idea.DefinitionId)}");
            if (idea.IsComposite || !string.IsNullOrWhiteSpace(idea.ActiveViewId))
            {
                builder.AppendLine($"  Composite view: {FindViewName(idea.ActiveViewId)}");
            }
            if (!string.IsNullOrWhiteSpace(idea.ShortcutTargetId))
            {
                builder.AppendLine($"  Shortcut target: {idea.ShortcutTargetId}");
            }
            AppendMarkers(builder, idea.Markers, definitions);
            AppendDetails(builder, idea.Details);
        }

        builder.AppendLine();
    }

    private static void AppendRelationships(
        StringBuilder builder,
        IReadOnlyList<CompositionRelationshipSnapshot> relationships,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions,
        IReadOnlyDictionary<string, CompositionIdeaSnapshot> ideas)
    {
        builder.AppendLine($"Relationships ({relationships.Count})");
        foreach (var relationship in relationships.OrderBy(relationship => relationship.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- Relationship: {relationship.Name}");
            builder.AppendLine($"  Definition: {FindDefinitionName(definitions, relationship.DefinitionId)}");
            if (!string.IsNullOrWhiteSpace(relationship.LinkRoleId))
            {
                builder.AppendLine($"  Link role: {FindDefinitionName(definitions, relationship.LinkRoleId)}");
            }
            builder.AppendLine($"  Source: {FindIdeaName(ideas, relationship.SourceIdeaId)}");
            builder.AppendLine($"  Target: {FindIdeaName(ideas, relationship.TargetIdeaId)}");
            AppendMarkers(builder, relationship.Markers, definitions);
            AppendDetails(builder, relationship.Details);
        }

        builder.AppendLine();
    }

    private static void AppendMarkers(
        StringBuilder builder,
        IReadOnlyList<string> markers,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions)
    {
        foreach (var marker in markers)
        {
            builder.AppendLine($"  Marker: {FindDefinitionName(definitions, marker)}");
        }
    }

    private static void AppendDetails(StringBuilder builder, IReadOnlyList<CompositionDetailSnapshot> details)
    {
        foreach (var detail in details.Where(detail => !string.IsNullOrWhiteSpace(detail.Name)))
        {
            var value = string.IsNullOrWhiteSpace(detail.Value) ? detail.Kind : detail.Value;
            builder.AppendLine($"  Detail: {detail.Name} = {value}");
        }
    }

    private static void AppendGeneratedFiles(
        StringBuilder builder,
        IReadOnlyList<CompositionGeneratedFile> generatedFiles)
    {
        builder.AppendLine($"Generated files ({generatedFiles.Count})");
        foreach (var file in generatedFiles.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {file.RelativePath}");
        }
    }

    private static IReadOnlyDictionary<string, CompositionDefinitionSnapshot> BuildDefinitionsIndex(CompositionDomainSnapshot domain)
    {
        return domain.ConceptDefinitions
            .Concat(domain.RelationshipDefinitions)
            .Concat(domain.LinkRoleDefinitions)
            .Concat(domain.MarkerDefinitions)
            .Concat(domain.TableDefinitions)
            .Concat(domain.ExternalLanguages)
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    private static string FindDefinitionName(
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions,
        string definitionId)
    {
        return definitions.TryGetValue(definitionId, out var definition)
            ? definition.Name
            : definitionId;
    }

    private static string FindIdeaName(IReadOnlyDictionary<string, CompositionIdeaSnapshot> ideas, string ideaId)
    {
        return ideas.TryGetValue(ideaId, out var idea) ? idea.Name : ideaId;
    }

    private static string FindViewName(string viewId)
    {
        return string.IsNullOrWhiteSpace(viewId) ? "(none)" : viewId;
    }
}
