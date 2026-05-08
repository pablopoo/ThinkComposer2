# WinUI Parity Audit

Date: 2026-05-08

## Summary

The WinUI app now covers the main editor loop and a substantial part of the rich-document workflow: `.tcdoc` modern documents, legacy import/merge, canvas editing, domain definitions, link-role variants, details, table-detail row editing, markers, complements, styles, templates, reports, generation, validation, clipboard/multi-select, and migration checks.

It is still not a complete legacy replacement. Remaining gaps are mostly deep legacy features: base-table record workflows, composite/shortcut navigation, presentation/multi-sheet print, PDF/XPS output, release packaging, and automated UI smoke coverage.

## Current WinUI Coverage

- Document: new, open `.tcdoc/.tcview`, import `.tdom/.tcom`, merge external documents, save `.tcdoc`, export view-only `.tcview`, recent files, dirty indicator.
- Data contract: versioned `CompositionDocumentSnapshot`, XML roundtrip, schema validation, migration-safe `.tcview` warning.
- Legacy bridge: exports legacy packages to `.tcdoc` or `.tcview`; WinUI consumes `.tcdoc` through the external bridge tool without referencing WPF.
- Canvas: render concepts/relationships, pan, zoom, select, multi-select, move, precise keyboard move, create concept, create relationship, delete, undo/redo.
- Inspector: edit concept/relationship names, layout, definitions, link-role variants, details, markers, styles, view complements, and generation templates.
- Domain: browse/edit concept, relationship, link-role, marker, table, and external-language definitions.
- Details: custom fields, links, attachments, CSV table details with a structured row editor.
- Output: SVG/HTML export, full document HTML report, template-based file generation, printable HTML preview.
- Panels: Explorer, Inspector, Messages, Search, Preview, Diagnostics.
- Settings: light/dark theme and panel visibility.
- Hardening: full migration check script verifies WinUI/Core WPF boundaries and solution build.

## Remaining Gaps

### P0 - Legacy Semantic Depth

- Idea-definition clusters and deeper domain semantics are only partially represented.
- Legacy unknown data is preserved through extensions where mapped, but the bridge still needs broader legacy-field coverage.

### P1 - Tables And Structured Editors

- Table details persist as CSV and can be edited through a structured row editor, but there is no full spreadsheet-like cell grid.
- Base-table definitions are listed/editable as definitions, but base-table records are not a dedicated workflow.

### P1 - Composite Navigation And Shortcuts

- Shortcut objects and parent/composite navigation are not implemented as first-class WinUI workflows.
- Complements can be edited as view extensions, but specialized visual rendering for group regions, legends, quotes, and info cards is still basic.

### P1 - Output Parity

- HTML report and generation are implemented.
- PDF/XPS report export and multi-sheet print preview are not ported.
- Presentation command is not ported.

### P2 - Product/Release Hardening

- Installer/release packaging remains to be defined.
- Automated UI smoke tests should be added around launch, theme switching, open/save, create/edit, and report/generation.
- Product options/licensing checks remain a product decision.

## Recommended Backlog

1. Add shortcut/composite navigation to the modern contract and WinUI.
2. Build a spreadsheet-like grid editor and base-table record workflows.
3. Render complements visually on the canvas.
4. Add PDF/XPS or an explicit supported replacement path.
5. Add UI automation smoke tests and release packaging.
