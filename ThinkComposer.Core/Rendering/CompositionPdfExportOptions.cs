namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionPdfExportOptions(
    float PageWidth = 842,
    float PageHeight = 595,
    float Margin = 36,
    string FontFamily = "Segoe UI");
