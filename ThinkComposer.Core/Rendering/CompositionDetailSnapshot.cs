namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDetailSnapshot(
    string Id,
    string Kind,
    string Name,
    string Value,
    IReadOnlyList<CompositionExtensionSnapshot>? Extensions = null)
{
    public IReadOnlyList<CompositionExtensionSnapshot> Extensions { get; init; } =
        Extensions ?? Array.Empty<CompositionExtensionSnapshot>();
}
