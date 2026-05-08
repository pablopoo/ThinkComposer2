namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentPersistenceAdvisor
{
    private const string ProjectedDomainId = "domain.default";
    private const string ProjectedDomainName = "Default Domain";
    private const string ProjectedDomainSummary = "Projected from a WinUI view snapshot.";
    private const string ProjectedConceptDefinitionId = "concept.default";
    private const string ProjectedRelationshipDefinitionId = "relationship.default";

    public static bool RequiresModernDocument(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return HasRichDomainData(document.Domain) ||
            HasRichIdeaData(document.Ideas) ||
            HasRichRelationshipData(document.Relationships) ||
            HasRichViewData(document.Views) ||
            HasRichExtensions(document.Extensions, allowSourceContract: true);
    }

    private static bool HasRichDomainData(CompositionDomainSnapshot domain)
    {
        return !string.Equals(domain.Id, ProjectedDomainId, StringComparison.Ordinal) ||
            !string.Equals(domain.Name, ProjectedDomainName, StringComparison.Ordinal) ||
            !string.Equals(domain.Summary, ProjectedDomainSummary, StringComparison.Ordinal) ||
            HasNonProjectedDefinitions(domain.ConceptDefinitions, ProjectedConceptDefinitionId, "Concept", "Concept") ||
            HasNonProjectedDefinitions(domain.RelationshipDefinitions, ProjectedRelationshipDefinitionId, "Relationship", "Relationship") ||
            domain.LinkRoleDefinitions.Count > 0 ||
            domain.MarkerDefinitions.Count > 0 ||
            domain.TableDefinitions.Count > 0 ||
            domain.ExternalLanguages.Count > 0 ||
            domain.Templates.Count > 0 ||
            HasRichExtensions(domain.Extensions, allowSourceContract: false);
    }

    private static bool HasNonProjectedDefinitions(
        IReadOnlyList<CompositionDefinitionSnapshot> definitions,
        string projectedId,
        string projectedName,
        string projectedKind)
    {
        if (definitions.Count != 1)
        {
            return definitions.Count > 0;
        }

        var definition = definitions[0];
        return !string.Equals(definition.Id, projectedId, StringComparison.Ordinal) ||
            !string.Equals(definition.Name, projectedName, StringComparison.Ordinal) ||
            !string.Equals(definition.Kind, projectedKind, StringComparison.Ordinal) ||
            !string.IsNullOrWhiteSpace(definition.Summary) ||
            definition.Details.Count > 0 ||
            HasRichExtensions(definition.Extensions, allowSourceContract: false) ||
            HasRichStyle(definition.Style);
    }

    private static bool HasRichIdeaData(IReadOnlyList<CompositionIdeaSnapshot> ideas)
    {
        return ideas.Any(idea =>
            !string.IsNullOrWhiteSpace(idea.Summary) ||
            idea.Details.Count > 0 ||
            idea.Markers.Count > 0 ||
            HasRichExtensions(idea.Extensions, allowSourceContract: false));
    }

    private static bool HasRichRelationshipData(IReadOnlyList<CompositionRelationshipSnapshot> relationships)
    {
        return relationships.Any(relationship =>
            relationship.Details.Count > 0 ||
            relationship.Markers.Count > 0 ||
            HasRichExtensions(relationship.Extensions, allowSourceContract: false));
    }

    private static bool HasRichViewData(IReadOnlyList<CompositionViewLayerSnapshot> views)
    {
        return views.Any(view =>
            view.Complements.Count > 0 ||
            HasRichExtensions(view.Extensions, allowSourceContract: false) ||
            HasRichStyle(view.Style));
    }

    private static bool HasRichExtensions(
        IReadOnlyList<CompositionExtensionSnapshot> extensions,
        bool allowSourceContract)
    {
        return extensions.Any(extension =>
            !allowSourceContract ||
            !string.Equals(extension.Key, "source.contract", StringComparison.Ordinal));
    }

    private static bool HasRichStyle(CompositionStyleSnapshot style)
    {
        return !string.IsNullOrWhiteSpace(style.Fill) ||
            !string.IsNullOrWhiteSpace(style.Stroke) ||
            !string.IsNullOrWhiteSpace(style.Text) ||
            style.StrokeThickness != 0 ||
            !string.IsNullOrWhiteSpace(style.StrokeDash) ||
            style.Properties.Count > 0;
    }
}
