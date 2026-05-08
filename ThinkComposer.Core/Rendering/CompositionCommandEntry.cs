namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionCommandEntry(
    string Id,
    string Title,
    CompositionCommandEntryKind Kind,
    string? TargetId = null,
    string Subtitle = "")
{
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} - {Subtitle}";
    }
}
