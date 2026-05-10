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

        var pageNumber = 1;
        DrawCoverPage(pdf, document, options, pageNumber++, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
        DrawViewPages(pdf, document, options, ref pageNumber, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
        DrawSummaryPages(pdf, document, options, ref pageNumber, titleFont, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
        DrawTableDataPages(pdf, document, options, ref pageNumber, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);

        pdf.Close();
        return stream.ToArray();
    }

    private static void DrawCoverPage(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        int pageNumber,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        DrawPageChrome(canvas, document, "Overview", pageNumber, options, bodyFont, mutedPaint, dividerPaint);

        var y = options.Margin + 42;
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
        ref int pageNumber,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        if (document.Views.Count == 0)
        {
            DrawViewPage(
                pdf,
                document,
                options,
                pageNumber++,
                CompositionDocumentSnapshotAdapter.ToViewSnapshot(document),
                Array.Empty<CompositionExtensionSnapshot>(),
                headingFont,
                bodyFont,
                textPaint,
                mutedPaint,
                dividerPaint);
            return;
        }

        foreach (var viewLayer in document.Views)
        {
            DrawViewPage(
                pdf,
                document,
                options,
                pageNumber++,
                CompositionDocumentSnapshotAdapter.ToViewSnapshot(document, viewLayer.Id),
                viewLayer.Complements,
                headingFont,
                bodyFont,
                textPaint,
                mutedPaint,
                dividerPaint);
        }
    }

    private static void DrawViewPage(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        int pageNumber,
        CompositionViewSnapshot view,
        IReadOnlyList<CompositionExtensionSnapshot> complements,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        DrawPageChrome(canvas, document, Clean(view.Title, "View"), pageNumber, options, bodyFont, mutedPaint, dividerPaint);
        canvas.DrawText(Clean(view.Title, "View"), options.Margin, options.Margin + 22, headingFont, textPaint);
        canvas.DrawText(
            $"Concepts: {view.Nodes.Count}  Relationships: {view.Connectors.Count}  Complements: {complements.Count}",
            options.Margin,
            options.Margin + 42,
            bodyFont,
            mutedPaint);
        canvas.DrawLine(options.Margin, options.Margin + 52, options.PageWidth - options.Margin, options.Margin + 52, dividerPaint);

        var viewOptions = options with
        {
            Margin = options.Margin + 24,
            PageHeight = options.PageHeight - 88
        };
        canvas.Save();
        canvas.Translate(0, 70);
        CompositionSnapshotPdfExporter.RenderSnapshot(canvas, view, viewOptions, complements, clearBackground: false);
        canvas.Restore();
        pdf.EndPage();
    }

    private static void DrawSummaryPages(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        ref int pageNumber,
        SKFont titleFont,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        DrawPageChrome(canvas, document, "Summary", pageNumber++, options, bodyFont, mutedPaint, dividerPaint);

        var y = options.Margin + 24;
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
            DrawRelationshipSummaryPage(pdf, document, options, pageNumber++, headingFont, bodyFont, textPaint, mutedPaint, dividerPaint);
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
        int pageNumber,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        DrawPageChrome(canvas, document, "Relationships", pageNumber, options, bodyFont, mutedPaint, dividerPaint);
        var y = options.Margin + 24;
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

    private static void DrawTableDataPages(
        SKDocument pdf,
        CompositionDocumentSnapshot document,
        CompositionPdfExportOptions options,
        ref int pageNumber,
        SKFont headingFont,
        SKFont bodyFont,
        SKPaint textPaint,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        var tableLines = BuildTableDataLines(document).ToArray();
        if (tableLines.Length == 0)
        {
            return;
        }

        using var canvas = pdf.BeginPage(options.PageWidth, options.PageHeight);
        canvas.Clear(SKColors.White);
        DrawPageChrome(canvas, document, "Tables and Details", pageNumber++, options, bodyFont, mutedPaint, dividerPaint);

        var y = options.Margin + 24;
        canvas.DrawText("Tables and Details", options.Margin, y, headingFont, textPaint);
        y += 26;
        DrawSummaryLines(canvas, tableLines, options, y, bodyFont, textPaint);
        pdf.EndPage();
    }

    private static IEnumerable<string> BuildTableDataLines(CompositionDocumentSnapshot document)
    {
        foreach (var definition in document.Domain.TableDefinitions)
        {
            if (definition.TableRecords.Rows.Count == 0)
            {
                continue;
            }

            yield return $"Table definition: {definition.Name}";
            yield return $"Columns: {string.Join(", ", definition.TableRecords.Columns)}";
            foreach (var row in definition.TableRecords.Rows)
            {
                yield return $"Record: {string.Join(" | ", row)}";
            }
        }

        foreach (var idea in document.Ideas)
        {
            foreach (var detail in idea.Details.Where(detail => string.Equals(detail.Kind, CompositionDetailKinds.Table, StringComparison.Ordinal)))
            {
                var table = CompositionDetailTableCsv.Parse(detail.Value);
                yield return $"Concept table: {idea.Name} / {detail.Name}";
                yield return $"Columns: {string.Join(", ", table.Columns)}";
                foreach (var row in table.Rows)
                {
                    yield return $"Row: {string.Join(" | ", row)}";
                }
            }
        }

        foreach (var relationship in document.Relationships)
        {
            foreach (var detail in relationship.Details.Where(detail => string.Equals(detail.Kind, CompositionDetailKinds.Table, StringComparison.Ordinal)))
            {
                var table = CompositionDetailTableCsv.Parse(detail.Value);
                yield return $"Relationship table: {relationship.Name} / {detail.Name}";
                yield return $"Columns: {string.Join(", ", table.Columns)}";
                foreach (var row in table.Rows)
                {
                    yield return $"Row: {string.Join(" | ", row)}";
                }
            }
        }
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

    private static void DrawPageChrome(
        SKCanvas canvas,
        CompositionDocumentSnapshot document,
        string section,
        int pageNumber,
        CompositionPdfExportOptions options,
        SKFont font,
        SKPaint mutedPaint,
        SKPaint dividerPaint)
    {
        var title = Clean(document.Title, "Untitled document");
        canvas.DrawText(title, options.Margin, options.Margin - 12, font, mutedPaint);
        canvas.DrawText(section, options.PageWidth / 2, options.Margin - 12, font, mutedPaint);
        canvas.DrawLine(options.Margin, options.Margin - 4, options.PageWidth - options.Margin, options.Margin - 4, dividerPaint);
        canvas.DrawLine(options.Margin, options.PageHeight - options.Margin + 10, options.PageWidth - options.Margin, options.PageHeight - options.Margin + 10, dividerPaint);
        canvas.DrawText($"Page {pageNumber}", options.Margin, options.PageHeight - options.Margin + 26, font, mutedPaint);
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
