namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionExtensionSnapshot(
    string Key,
    string Value,
    IReadOnlyDictionary<string, string>? Properties = null)
{
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        Properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
