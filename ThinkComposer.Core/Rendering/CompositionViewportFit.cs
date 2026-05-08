namespace Instrumind.ThinkComposer.Core.Rendering;

public readonly record struct CompositionViewportFit(
    double Zoom,
    double PanX,
    double PanY)
{
    public static CompositionViewportFit Default { get; } = new(1.0, 0.0, 0.0);
}
