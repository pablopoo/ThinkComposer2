namespace Instrumind.ThinkComposer.Core.Rendering;

public readonly record struct CompositionNodeLabelLayout(
    bool ShowsSubtitle,
    bool AllowsWrapping,
    double TitleFontSize,
    double BodyFontSize,
    double Padding);
