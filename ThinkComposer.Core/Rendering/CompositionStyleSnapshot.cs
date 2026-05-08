namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionStyleSnapshot(
    string Fill = "",
    string Stroke = "",
    string Text = "",
    double StrokeThickness = 0,
    string StrokeDash = "",
    IReadOnlyDictionary<string, string>? Properties = null)
{
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        Properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
