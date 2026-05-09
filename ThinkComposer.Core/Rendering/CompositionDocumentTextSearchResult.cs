namespace Instrumind.ThinkComposer.Core.Rendering;

public enum CompositionDocumentTextSearchResultKind
{
    Idea,
    Relationship,
    Detail,
    View,
    Definition,
    Template,
    Complement
}

public sealed record CompositionDocumentTextSearchResult(
    CompositionDocumentTextSearchResultKind Kind,
    string TargetId,
    string FieldPath,
    string Title,
    string Preview,
    bool IsReplaceable = true);
