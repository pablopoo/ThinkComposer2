namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentValidator
{
    public static CompositionDocumentValidationResult Validate(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var issues = new List<CompositionDocumentValidationIssue>();
        var ideaIds = document.Ideas.Select(idea => idea.Id).ToArray();
        var relationshipIds = document.Relationships.Select(relationship => relationship.Id).ToArray();
        var conceptDefinitionIds = document.Domain.ConceptDefinitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var relationshipDefinitionIds = document.Domain.RelationshipDefinitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var markerDefinitionIds = document.Domain.MarkerDefinitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var ideaIdSet = ideaIds.ToHashSet(StringComparer.Ordinal);
        var relationshipIdSet = relationshipIds.ToHashSet(StringComparer.Ordinal);

        AddDuplicates(issues, ideaIds, CompositionDocumentValidationCodes.DuplicateIdeaId, "Duplicate concept id");
        AddDuplicates(issues, relationshipIds, CompositionDocumentValidationCodes.DuplicateRelationshipId, "Duplicate relationship id");
        AddDuplicates(issues, document.Domain.Templates.Select(template => template.Key), CompositionDocumentValidationCodes.DuplicateTemplateKey, "Duplicate template key");
        ValidateIdeas(document.Ideas, conceptDefinitionIds, markerDefinitionIds, issues);
        ValidateRelationships(document.Relationships, relationshipDefinitionIds, markerDefinitionIds, ideaIdSet, issues);
        ValidateViews(document.Views, ideaIdSet, relationshipIdSet, issues);

        return new CompositionDocumentValidationResult(issues);
    }

    private static void ValidateIdeas(
        IReadOnlyList<CompositionIdeaSnapshot> ideas,
        ISet<string> conceptDefinitionIds,
        ISet<string> markerDefinitionIds,
        ICollection<CompositionDocumentValidationIssue> issues)
    {
        foreach (var idea in ideas)
        {
            if (!string.IsNullOrWhiteSpace(idea.DefinitionId) && !conceptDefinitionIds.Contains(idea.DefinitionId))
            {
                issues.Add(Error(
                    CompositionDocumentValidationCodes.MissingIdeaDefinition,
                    $"Concept '{Display(idea.Name, idea.Id)}' references missing definition '{idea.DefinitionId}'.",
                    idea.Id));
            }

            ValidateMarkers(idea.Markers, markerDefinitionIds, idea.Id, issues);
        }
    }

    private static void ValidateRelationships(
        IReadOnlyList<CompositionRelationshipSnapshot> relationships,
        ISet<string> relationshipDefinitionIds,
        ISet<string> markerDefinitionIds,
        ISet<string> ideaIds,
        ICollection<CompositionDocumentValidationIssue> issues)
    {
        foreach (var relationship in relationships)
        {
            if (!string.IsNullOrWhiteSpace(relationship.DefinitionId) &&
                !relationshipDefinitionIds.Contains(relationship.DefinitionId))
            {
                issues.Add(Error(
                    CompositionDocumentValidationCodes.MissingRelationshipDefinition,
                    $"Relationship '{Display(relationship.Name, relationship.Id)}' references missing definition '{relationship.DefinitionId}'.",
                    relationship.Id));
            }

            if (!ideaIds.Contains(relationship.SourceIdeaId) || !ideaIds.Contains(relationship.TargetIdeaId))
            {
                issues.Add(Error(
                    CompositionDocumentValidationCodes.MissingRelationshipEndpoint,
                    $"Relationship '{Display(relationship.Name, relationship.Id)}' references a missing endpoint.",
                    relationship.Id));
            }

            ValidateMarkers(relationship.Markers, markerDefinitionIds, relationship.Id, issues);
        }
    }

    private static void ValidateViews(
        IReadOnlyList<CompositionViewLayerSnapshot> views,
        ISet<string> ideaIds,
        ISet<string> relationshipIds,
        ICollection<CompositionDocumentValidationIssue> issues)
    {
        foreach (var view in views)
        {
            var viewNodeIds = view.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var node in view.Nodes.Where(node => !ideaIds.Contains(node.Id)))
            {
                issues.Add(Error(
                    CompositionDocumentValidationCodes.MissingViewNodeIdea,
                    $"View '{view.Name}' contains node '{node.Id}' without a matching concept.",
                    node.Id));
            }

            foreach (var connector in view.Connectors)
            {
                if (!relationshipIds.Contains(connector.Id))
                {
                    issues.Add(Error(
                        CompositionDocumentValidationCodes.MissingViewConnectorRelationship,
                        $"View '{view.Name}' contains connector '{connector.Id}' without a matching relationship.",
                        connector.Id));
                }

                if (!viewNodeIds.Contains(connector.SourceId) || !viewNodeIds.Contains(connector.TargetId))
                {
                    issues.Add(Error(
                        CompositionDocumentValidationCodes.MissingViewConnectorEndpoint,
                        $"View '{view.Name}' contains connector '{connector.Id}' with a missing visual endpoint.",
                        connector.Id));
                }
            }
        }
    }

    private static void ValidateMarkers(
        IReadOnlyList<string> markers,
        ISet<string> markerDefinitionIds,
        string targetId,
        ICollection<CompositionDocumentValidationIssue> issues)
    {
        foreach (var marker in markers.Where(marker => !markerDefinitionIds.Contains(marker)))
        {
            issues.Add(Error(
                CompositionDocumentValidationCodes.MissingMarkerDefinition,
                $"Object '{targetId}' references missing marker '{marker}'.",
                targetId));
        }
    }

    private static void AddDuplicates(
        ICollection<CompositionDocumentValidationIssue> issues,
        IEnumerable<string> values,
        string code,
        string message)
    {
        foreach (var duplicate in values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            issues.Add(Error(code, $"{message}: '{duplicate}'.", duplicate));
        }
    }

    private static CompositionDocumentValidationIssue Error(string code, string message, string targetId)
    {
        return new CompositionDocumentValidationIssue(code, CompositionDocumentValidationSeverity.Error, message, targetId);
    }

    private static string Display(string name, string fallback)
    {
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }
}
