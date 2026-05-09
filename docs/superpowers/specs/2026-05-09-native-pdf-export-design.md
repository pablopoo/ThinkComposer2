# Native PDF Export Design

## Decision

ThinkComposer2 will remove XPS as an output target in the WinUI app and add direct PDF export as the native document output path.

The implementation will use SkiaSharp's PDF backend. SkiaSharp is open source, uses an MIT license, works in-process from .NET, and can generate vector PDF pages without WebView, Chromium, printer drivers, or the old WPF/XPS pipeline.

## Why Not XPS

XPS was useful in the original WPF app because WPF could render `FixedDocument` content and then convert it to PDF. That path is now legacy-heavy, Windows-printing dependent, and not a good fit for the WinUI rewrite.

The WinUI app should expose PDF as the user-facing document format. XPS references should be removed from new UI labels, command text, and pending parity docs unless they describe legacy behavior.

## Alternatives Considered

### SkiaSharp PDF

This is the selected option. It keeps the exporter native, lightweight, testable, and compatible with canvas-style rendering. It can draw text, shapes, paths, images, and multiple pages directly to a PDF surface.

### QuestPDF

QuestPDF is strong for structured reports, but its license has commercial thresholds. It is not the default dependency for this fork.

### PDFsharp

PDFsharp is a possible fallback and has precedent in the legacy app. It is less natural for reusing the same canvas/vector rendering path that the WinUI app needs.

### Browser Print To PDF

HTML print remains useful as a preview/fallback, but it should not be the native export implementation. It depends on user interaction and browser behavior.

### Microsoft Print To PDF

This is not acceptable as the main exporter because it is interactive, environment-dependent, and difficult to test automatically.

## User Experience

The top export menu will separate output intent:

- `Export View as SVG`
- `Export View as HTML`
- `Export View as PDF`
- `Export Document Report as HTML`
- `Export Document Report as PDF`
- `Presentation HTML`
- `Print Preview`

XPS will not appear in the WinUI UI.

PDF export should use normal save pickers, remember the last export folder through the existing app settings pattern where practical, and report success/failure in the status area.

## Export Scope

### View PDF

Exports the active composition view as one PDF page.

The exporter will compute the snapshot bounds, add a small page margin, scale to fit a standard page size by default, and preserve the visual structure of concepts, relationships, complements, marker glyph placeholders, colors, and labels.

### Document Report PDF

Exports a multi-page PDF report from the modern document snapshot:

- cover/title page
- one section per view
- view diagram page
- concept and relationship summaries where available

The first implementation can be visually simpler than the HTML report, but it must be complete enough to replace the old PDF/XPS report command as a native output path.

## Architecture

Add a PDF rendering layer in `ThinkComposer.Core.Rendering`:

- `CompositionSnapshotPdfExporter`
- `CompositionDocumentReportPdfExporter`
- small shared layout helpers only if duplication appears between view/report export

The exporters should accept existing `CompositionViewSnapshot` and `CompositionDocumentSnapshot` objects. They should return `byte[]` or write to a `Stream`; UI code will handle file picker integration.

The code should not depend on WinUI types. This keeps PDF generation testable from console/unit test projects and usable by future CLI/release tooling.

## Rendering Rules

Use SkiaSharp primitives:

- filled rounded rectangles for concept bodies
- stroked paths/lines for relationships
- text labels with sane fallback fonts
- document background matching the exported theme defaults
- deterministic margins, page size, scale, and metadata

If a visual detail cannot yet be rendered exactly, prefer a readable approximation over blocking the exporter. Track those as follow-up rendering fidelity gaps.

## Error Handling

Exporter failures should throw clear exceptions from core code.

WinUI command handlers should catch exceptions, show a concise failure dialog, and update status text. Cancelled save pickers should be no-ops.

## Testing

Add automated tests that:

- generate a PDF for a sample view snapshot
- generate a PDF report for a sample document snapshot
- verify output starts with `%PDF-`
- verify output is non-trivial in size
- verify generated files can be written by the WinUI release/smoke path if practical

Manual smoke test:

- open `PredefinedContent\Business_Model.tdom`
- export active view as PDF
- export document report as PDF
- open both files with the default PDF viewer
- confirm there is no XPS option in the WinUI export UI

## Follow-Up Boundaries

This design does not include:

- a full print subsystem rewrite
- pixel-perfect parity with the old WPF/XPS report renderer
- PDF/A compliance
- digital signatures
- accessibility tagging inside PDF

Those can be handled later if the app needs formal publishing workflows.
