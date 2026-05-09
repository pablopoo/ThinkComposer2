# PDF Fidelity Design

Date: 2026-05-09

## Goal

Improve native PDF output quality while keeping the exporter lightweight, native, and independent from WinUI/WPF.

This is fidelity work for the current SkiaSharp PDF path, not a return to XPS or pixel-perfect WPF rendering.

## Scope

### View PDF

- Render node labels with wrapping inside node bounds.
- Preserve node fill, stroke, text color, stroke thickness, and rounded shape proportions more consistently.
- Render relationships with routed lines, endpoint arrowheads, connector labels, and style-aware stroke thickness.
- Include current-view complements where supported by the WinUI canvas model:
  - group/region blocks
  - legend/info/quote cards
  - readable fallback for unknown complement types
- Improve fit-to-page bounds so labels, arrowheads, thick strokes, and complements are not clipped.

### Document Report PDF

- Make the report PDF closer to the HTML report structure:
  - cover page
  - one diagram page per view
  - concept summary
  - relationship summary
  - table-definition/base-record summary where present
  - details, markers, and link-role metadata where available
- Add page headers, footers, and page numbers.
- Use a denser but readable layout suitable for printed review.

## Non-Goals

- Pixel-perfect parity with legacy WPF/XPS.
- PDF/A, tagging/accessibility metadata, signatures, or print-driver integration.
- New UI controls beyond existing export/report commands.
- Replacing SkiaSharp.

## Architecture

All rendering stays in `ThinkComposer.Core.Rendering`.

Primary files:

- `CompositionSnapshotPdfExporter`
- `CompositionDocumentReportPdfExporter`
- New shared PDF helpers only if they reduce duplication clearly.

The WinUI app continues to call core exporters and write returned bytes through existing file-picker handlers.

## Rendering Strategy

Use deterministic SkiaSharp drawing primitives:

- Rounded rectangles and strokes for concepts.
- Lines plus simple arrowhead polygons for relationships.
- Text wrapping by measuring words against available width.
- Shared color parsing with stable fallbacks.
- Bounds calculation that includes nodes and complement geometry.
- Report pages with predictable margins, typography, and section spacing.

When exact legacy styling is unknown, prefer a readable approximation and keep the output deterministic.

## Testing

Core tests should cover:

- View PDF exports non-trivial bytes and starts with `%PDF-`.
- View PDF includes enough PDF structure for page detection.
- Report PDF with details/tables/complements is larger than the basic fixture.
- Report generation writes PDF files in the workflow output path.
- The exporters remain usable from both `net48` and `net10.0-windows` builds.

Manual smoke:

- Open a migrated legacy document.
- Export view PDF.
- Export report PDF.
- Confirm diagrams are readable, labels are not clipped, and report summaries include core document data.

## Acceptance Criteria

- PDF diagrams are noticeably closer to the WinUI canvas: readable labels, arrows, styles, and complements.
- Report PDF includes document data that currently appears only in HTML report or preview.
- No WPF/XPS dependency is introduced.
- Existing export commands and tests remain green.
