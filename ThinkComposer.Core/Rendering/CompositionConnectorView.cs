namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionConnectorView(
    string Id,
    string SourceId,
    string TargetId,
    string Text = "Relationship",
    CompositionStyleSnapshot? Style = null)
{
    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();
}
