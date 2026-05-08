namespace Instrumind.ThinkComposer.Core.Rendering;

public enum CompositionDocumentValidationSeverity
{
    Warning,
    Error
}

public sealed record CompositionDocumentValidationIssue(
    string Code,
    CompositionDocumentValidationSeverity Severity,
    string Message,
    string TargetId = "");

public sealed record CompositionDocumentValidationResult(IReadOnlyList<CompositionDocumentValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(issue => issue.Severity == CompositionDocumentValidationSeverity.Error);
}

public static class CompositionDocumentValidationCodes
{
    public const string DuplicateIdeaId = "duplicate.idea.id";
    public const string DuplicateRelationshipId = "duplicate.relationship.id";
    public const string DuplicateTemplateKey = "duplicate.template.key";
    public const string MissingIdeaDefinition = "missing.idea.definition";
    public const string MissingRelationshipDefinition = "missing.relationship.definition";
    public const string MissingRelationshipEndpoint = "missing.relationship.endpoint";
    public const string MissingMarkerDefinition = "missing.marker.definition";
    public const string MissingViewNodeIdea = "missing.view.node.idea";
    public const string MissingViewConnectorRelationship = "missing.view.connector.relationship";
    public const string MissingViewConnectorEndpoint = "missing.view.connector.endpoint";
}
