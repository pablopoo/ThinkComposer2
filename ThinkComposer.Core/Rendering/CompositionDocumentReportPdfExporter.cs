using SkiaSharp;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentReportPdfExporter
{
    private const float TitleSize = 22;
    private const float HeadingSize = 15;
    private const float BodySize = 10.5f;
    private const float LineGap = 5;

    public static byte[] Export(CompositionDocumentSnapshot document, CompositionPdfExportOptions? options = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        options ??= new CompositionPdfExportOptions();
        using var stream = new MemoryStream();
        using var pdf = SKDocument.CreatePdf(stream);
        using var typeface = SKTypeface.FromFamilyName(options.FontFamily);
        using var titleFont = new SKFont(typeface, TitleSize) { Edging = SKFontEdging.Antialias };
        using var headingFont = new SKFont(typeface, HeadingSize) { Edging = SKFontEdging.Antialias };
        using var bodyFont = new SKFont(typeface, BodySize) { Edging = SKFontEdging.Antialias };
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(31, 35, 40)
        };
        using var mutedPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(95, 107, 122)
        };
        using var dividerPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(216, 222, 232),
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        DrawCoverPage(pdf, document, options, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
        DrawViewPages(pdf, document, options, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
        DrawSummaryPages(pdf, document, options, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);

        pdf.Close();
        return stream.ToArray();
    }

    private static void DrawCoverPage(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);

        var y = options.Margin + 28;
        canvas.DrawText(Clean(document.Title, "Untitled document"), options.Margin, y, titleFont, textPaint);
        y += 34;
        canvas.DrawText($"Domain: {Clean(document.Domain.Name, "Untitled domain")}", options.Margin, y, headingFont, mutedPaint);
        y += 28;
        canvas.DrawLine(options.Margin, y, options.PageWidth - options.Margin, y, dividerPaint);
        y += 28;

        foreach (var line in new[]
        {
            $"Concepts: {document.Ideas.Count}",
            $"Relationships: {document.Relationships.Count}",
            $"Views: {document.Views.Count}",
            $"Concept definitions: {document.Domain.ConceptDefinitions.Count}",
            $"Relationship definitions: {document.Domain.RelationshipDefinitions.Count}",
            $"Marker definitions: {document.Domain.MarkerDefinitions.Count}",
            $"Table definitions: {document.Domain.TableDefinitions.Count}"
        })
        {
            canvas.DrawText(line, options.Margin, y, bodyFont, textPaint);
            y += BodySize + LineGap;
        }

        if (!string.IsNullOrWhiteSpace(document.Domain.Summary))
        {
            y += 12;
            DrawWrappedText(canvas, document.Domain.Summary, options.Margin, y, options.PageWidth - options.Margin * 2, bodyFont, mutedPaint);
        }

        pdf.EndPage();
    }

    private static void DrawViewPages(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        var views = document.Views.Count == 0
            ? [CompositionDocumentSnapshotAdapter.ToViewSnapshot(document)]
            : document.Views.Select(view => CompositionDocumentSnapshotAdapter.ToViewSnapshot(document, view.Id)).ToArray();

        foreach (var view in views)
        {
            using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
            canvas.Clear(SKColors.White);
            canvas.DrawText(Clean(view.Title, "View"), options.Margin, options.Margin, headingFont, textPaint);
            canvas.DrawText(
                $"Concepts: {view.Nodes.Count}  Relationships: {view.Connectors.Count}",
                options.Margin,
                options.Margin + 20,
                bodyFont,
                mutedPaint);
            canvas.DrawLine(options.Margin, options.Margin + 30, options.PageWidth - options.Margin, options.Margin + 30, dividerPaint);

            var viewOptions = options with
            {
                Margin = options.Margin + 24,
                PageHeight = options.PageHeight - 64
            };
            canvas.Save();
            canvas.Translate(0, 48);
            CompositionSnapshotPdfExporter.RenderSnapshot(canvas, view, viewOptions, clearBackground: false);
            canvas.Restore();
            pdf.EndPage();
        }
    }

    private static void DrawSummaryPages(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);

        var y = options.Margin;
        canvas.DrawText("Concepts", options.Margin, y, headingFont, textPaint);
        y += 24;
        y = DrawSummaryLines(
            canvas,
            document.Ideas.Select(idea => $"{idea.Name} - {FindDefinitionName(document.Domain, idea.DefinitionId)}{Suffix(idea.Summary)}"),
            options,
            y,
            bodyFont,
            textPaint);

        y += 22;
        if (y > options.PageHeight - options.Margin - 80)
        {
            pdf.EndPage();
            DrawRelationshipSummaryPage(pdf, document, options, headingFont, bodyFont, textPaint);
            return;
        }

        canvas.DrawText("Relationships", options.Margin, y, headingFont, textPaint);
        y += 24;
        DrawSummaryLines(
            canvas,
            document.Relationships.Select(relationship =>
                $"{relationship.Name} - {FindIdeaName(document, relationship.SourceIdeaId)} -> {FindIdeaName(document, relationship.TargetIdeaId)}"),
            options,
            y,
            bodyFont,
            textPaint);

        pdf.EndPage();
    }

    private static void DrawRelationshipSummaryPage(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        var y = options.Margin;
        canvas.DrawText("Relationships", options.Margin, y, headingFont, textPaint);
        y += 24;
        DrawSummaryLines(
            canvas,
            document.Relationships.Select(relationship =>
                $"{relationship.Name} - {FindIdeaName(document, relationship.SourceIdeaId)} -> {FindIdeaName(document, relationship.TargetIdeaId)}"),
            options,
            y,
            bodyFont,
            textPaint);
        pdf.EndPage();
    }

    private static float DrawSummaryLines(
        SKCanvas canvas,
        IEnumerable<string> lines,
        CompositionPdfExportOptions options,
        float y,
        SKFont font,
        SKPaint paint)
    {
        var availableWidth = options.PageWidth - options.Margin * 2;
        var maxY = options.PageHeight - options.Margin;
        var wroteAny = false;

        foreach (var line in lines)
        {
            if (y > maxY)
            {
                break;
            }

            var bullet = $"- {line}";
            y = DrawWrappedText(canvas, bullet, options.Margin, y, availableWidth, font, paint);
            y += 3;
            wroteAny = true;
        }

        if (!wroteAny)
        {
            canvas.DrawText("No records.", options.Margin, y, font, paint);
            y += font.Size + LineGap;
        }

        return y;
    }

    private static float DrawWrappedText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        float maxWidth,
        SKFont font,
        SKPaint paint)
    {
        var words = Clean(text, string.Empty).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return y;
        }

        var line = string.Empty;
        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (font.MeasureText(candidate) <= maxWidth)
            {
                line = candidate;
                continue;
            }

            canvas.DrawText(line, x, y, font, paint);
            y += font.Size + LineGap;
            line = word;
        }

        if (line.Length > 0)
        {
            canvas.DrawText(line, x, y, font, paint);
            y += font.Size + LineGap;
        }

        return y;
    }

    private static string FindDefinitionName(CompositionDomainSnapshot domain, string definitionId)
    {
        return domain.ConceptDefinitions
            .Concat(domain.RelationshipDefinitions)
            .Concat(domain.LinkRoleDefinitions)
            .Concat(domain.MarkerDefinitions)
            .Concat(domain.TableDefinitions)
            .Concat(domain.ExternalLanguages)
            .FirstOrDefault(definition => string.Equals(definition.Id, definitionId, StringComparison.Ordinal))
            ?.Name ?? definitionId;
    }

    private static string FindIdeaName(CompositionDocumentSnapshot document, string ideaId)
    {
        return document.Ideas.FirstOrDefault(idea => string.Equals(idea.Id, ideaId, StringComparison.Ordinal))?.Name ?? ideaId;
    }

    private static string Suffix(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $" - {Clean(value, string.Empty)}";
    }

    private static string Clean(string? value, string fallback)
    {
        var clean = (value ?? string.Empty)
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return clean.Length == 0 ? fallback : clean;
    }
}
