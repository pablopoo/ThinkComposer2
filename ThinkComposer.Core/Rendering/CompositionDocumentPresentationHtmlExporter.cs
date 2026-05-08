using System.Net;
using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentPresentationHtmlExporter
{
    public static string Export(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine($"  <title>{Encode(document.Title)} - ThinkComposer Presentation</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    :root { color-scheme: light; --text: #1f2328; --muted: #59636e; --border: #d0d7de; --fill: #f6f8fa; --accent: #0969da; }");
        builder.AppendLine("    body { margin: 0; font-family: Segoe UI, Arial, sans-serif; color: var(--text); background: #fff; }");
        builder.AppendLine("    .slide { min-height: 100vh; box-sizing: border-box; padding: 42px 56px; display: flex; flex-direction: column; gap: 18px; border-bottom: 1px solid var(--border); }");
        builder.AppendLine("    .title { justify-content: center; }");
        builder.AppendLine("    h1 { margin: 0; font-size: 42px; font-weight: 600; }");
        builder.AppendLine("    h2 { margin: 0; font-size: 28px; font-weight: 600; }");
        builder.AppendLine("    .meta { color: var(--muted); font-size: 14px; }");
        builder.AppendLine("    .stats { display: flex; gap: 12px; flex-wrap: wrap; }");
        builder.AppendLine("    .stat { border: 1px solid var(--border); border-radius: 8px; padding: 10px 14px; background: var(--fill); }");
        builder.AppendLine("    .canvas svg { width: 100%; height: auto; max-height: 74vh; border: 1px solid var(--border); background: #fff; }");
        builder.AppendLine("    @media print { .slide { page-break-after: always; min-height: 100vh; } }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");

        AppendTitleSlide(builder, document);
        IReadOnlyList<CompositionViewSnapshot> views = document.Views.Count == 0
            ? [CompositionDocumentSnapshotAdapter.ToViewSnapshot(document)]
            : document.Views.Select(view => CompositionDocumentSnapshotAdapter.ToViewSnapshot(document, view.Id)).ToArray();
        foreach (var view in views)
        {
            AppendViewSlide(builder, view);
        }

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static void AppendTitleSlide(StringBuilder builder, CompositionDocumentSnapshot document)
    {
        builder.AppendLine("  <section class=\"slide title\">");
        builder.AppendLine($"    <h1>{Encode(document.Title)}</h1>");
        builder.AppendLine($"    <div class=\"meta\">Domain: {Encode(document.Domain.Name)}</div>");
        builder.AppendLine("    <div class=\"stats\">");
        builder.AppendLine($"      <div class=\"stat\">Concepts: {document.Ideas.Count}</div>");
        builder.AppendLine($"      <div class=\"stat\">Relationships: {document.Relationships.Count}</div>");
        builder.AppendLine($"      <div class=\"stat\">Views: {document.Views.Count}</div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("  </section>");
    }

    private static void AppendViewSlide(StringBuilder builder, CompositionViewSnapshot view)
    {
        builder.AppendLine("  <section class=\"slide\">");
        builder.AppendLine($"    <h2>{Encode(view.Title)}</h2>");
        builder.AppendLine($"    <div class=\"meta\">Concepts: {view.Nodes.Count} | Relationships: {view.Connectors.Count}</div>");
        builder.AppendLine("    <div class=\"canvas\">");
        builder.AppendLine(CompositionSnapshotSvgExporter.Export(view));
        builder.AppendLine("    </div>");
        builder.AppendLine("  </section>");
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
