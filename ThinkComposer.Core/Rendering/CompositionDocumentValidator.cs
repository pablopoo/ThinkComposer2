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

        if (string.IsNullOrWhiteSpace(document.Title))
        {
            issues.Add(Error(
                CompositionDocumentValidationCodes.EmptyDocumentTitle,
                "Document title is required.",
                document.Id));
        }

        AddDuplicates(issues, ideaIds, CompositionDocumentValidationCodes.DuplicateIdeaId, "Duplicate concept id");
        AddEmptyNames(
            issues,
            document.Ideas.Select(idea => (idea.Id, idea.Name)),
            CompositionDocumentValidationCodes.EmptyIdeaName,
            "Concept name is required");
        AddDuplicateNames(
            issues,
            document.Ideas.Select(idea => (idea.Id, idea.Name)),
            CompositionDocumentValidationCodes.DuplicateIdeaName,
            "Duplicate concept name");
        AddDuplicates(issues, relationshipIds, CompositionDocumentValidationCodes.DuplicateRelationshipId, "Duplicate relationship id");
        AddEmptyNames(
            issues,
            document.Relationships.Select(relationship => (relationship.Id, relationship.Name)),
            CompositionDocumentValidationCodes.EmptyRelationshipName,
            "Relationship name is required");
        AddDuplicateNames(
            issues,
            document.Relationships.Select(relationship => (relationship.Id, relationship.Name)),
            CompositionDocumentValidationCodes.DuplicateRelationshipName,
            "Duplicate relationship name");
        ValidateDefinitionNames(document.Domain.ConceptDefinitions, "Concept definition", issues);
        ValidateDefinitionNames(document.Domain.RelationshipDefinitions, "Relationship definition", issues);
        ValidateDefinitionNames(document.Domain.MarkerDefinitions, "Marker definition", issues);
        ValidateDefinitionNames(document.Domain.TableDefinitions, "Table definition", issues);
        ValidateDefinitionNames(document.Domain.ExternalLanguages, "External language", issues);
        AddEmptyValues(
            issues,
            document.Domain.Templates.Select(template => template.Key),
            CompositionDocumentValidationCodes.EmptyTemplateKey,
            "Template key is required");
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

    private static void ValidateDefinitionNames(
        IReadOnlyList<CompositionDefinitionSnapshot> definitions,
        string label,
        ICollection<CompositionDocumentValidationIssue> issues)
    {
        AddEmptyNames(
            issues,
            definitions.Select(definition => (definition.Id, definition.Name)),
            CompositionDocumentValidationCodes.EmptyDefinitionName,
            $"{label} name is required");
        AddDuplicateNames(
            issues,
            definitions.Select(definition => (definition.Id, definition.Name)),
            CompositionDocumentValidationCodes.DuplicateDefinitionName,
            $"Duplicate {label.ToLowerInvariant()} name");
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

    private static void AddEmptyNames(
        ICollection<CompositionDocumentValidationIssue> issues,
        IEnumerable<(string Id, string Name)> entries,
        string code,
        string message)
    {
        foreach (var entry in entries.Where(entry => string.IsNullOrWhiteSpace(entry.Name)))
        {
            issues.Add(Error(code, $"{message}.", entry.Id));
        }
    }

    private static void AddDuplicateNames(
        ICollection<CompositionDocumentValidationIssue> issues,
        IEnumerable<(string Id, string Name)> entries,
        string code,
        string message)
    {
        foreach (var duplicate in entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .GroupBy(entry => entry.Name.Trim(), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            issues.Add(Error(code, $"{message}: '{duplicate}'.", duplicate));
        }
    }

    private static void AddEmptyValues(
        ICollection<CompositionDocumentValidationIssue> issues,
        IEnumerable<string> values,
        string code,
        string message)
    {
        foreach (var value in values.Where(string.IsNullOrWhiteSpace))
        {
            issues.Add(Error(code, $"{message}.", value));
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
