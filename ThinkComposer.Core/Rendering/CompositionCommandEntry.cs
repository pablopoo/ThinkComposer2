namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionCommandEntry(
    string Id,
    string Title,
    CompositionCommandEntryKind Kind,
    string? TargetId = null,
    string Subtitle = "",
    CompositionCommandCategory Category = CompositionCommandCategory.App,
    CompositionCommandSurface Surfaces = CompositionCommandSurface.CommandPalette,
    string Accelerator = "",
    bool IsEnabled = true,
    string DisabledReason = "")
{
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} - {Subtitle}";
    }
}
