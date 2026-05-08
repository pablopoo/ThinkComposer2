namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionIdeaSnapshot(
    string Id,
    string Name,
    string DefinitionId = "",
    string Summary = "",
    IReadOnlyList<CompositionDetailSnapshot>? Details = null,
    IReadOnlyList<string>? Markers = null,
    CompositionStyleSnapshot? Style = null,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null,
    string ParentIdeaId = "",
    string ActiveViewId = "",
    bool IsComposite = false,
    string ShortcutTargetId = "")
{
    public IReadOnlyList<CompositionDetailSnapshot> Details { get; init; } =
        Details ?? Array.Empty<CompositionDetailSnapshot>();

    public IReadOnlyList<string> Markers { get; init; } = Markers ?? Array.Empty<string>();

    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();

    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();

    public string ParentIdeaId { get; init; } = ParentIdeaId;

    public string ActiveViewId { get; init; } = ActiveViewId;

    public bool IsComposite { get; init; } = IsComposite;

    public string ShortcutTargetId { get; init; } = ShortcutTargetId;
}
