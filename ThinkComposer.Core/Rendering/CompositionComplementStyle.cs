namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionComplementStyle(
    string Fill = "",
    string Stroke = "",
    string Text = "",
    double Opacity = 1,
    double StrokeThickness = 0,
    string StrokeDash = "",
    string FontFamily = "",
    double FontSize = 0,
    string Icon = "");
