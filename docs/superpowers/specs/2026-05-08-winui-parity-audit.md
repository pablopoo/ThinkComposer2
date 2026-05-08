# WinUI Parity Audit

Date: 2026-05-08

## Summary

The WinUI app now covers the main editor loop and a substantial part of the rich-document workflow: `.tcdoc` modern documents, legacy import/merge, canvas editing, domain definitions, link-role variants, multi-view navigation metadata, composite views, shortcut authoring, details, table-detail/base-table row editing, markers, visual complements, styles, templates, reports, generation, validation, clipboard/multi-select, migration checks, and reproducible WinUI publish output.

It is still not a complete legacy replacement. Remaining gaps are mostly deep legacy features: presentation/multi-sheet print, PDF/XPS output, installer/signing, and broader automated UI smoke coverage.

## Current WinUI Coverage

- Document: new, open `.tcdoc/.tcview`, import `.tdom/.tcom`, merge external documents, save `.tcdoc`, export view-only `.tcview`, recent files, dirty indicator.
- Data contract: versioned `CompositionDocumentSnapshot`, XML roundtrip, schema validation, migration-safe `.tcview` warning.
- Legacy bridge: exports legacy packages to `.tcdoc` or `.tcview`; WinUI consumes `.tcdoc` through the external bridge tool without referencing WPF.
- Canvas: render concepts/relationships, pan, zoom, select, multi-select, move, precise keyboard move, create concept, create relationship, delete, undo/redo.
- Navigation: browse/switch modern document views from Explorer and command search; create/open composite views from selected concepts; create shortcut objects in the current view; preserve composite/shortcut metadata in `.tcdoc`.
- Inspector: edit concept/relationship names, layout, definitions, link-role variants, details, markers, styles, current-view complements, and generation templates.
- Canvas complements: render group regions plus legend/info/quote cards from current-view complements.
- Domain: browse/edit concept, relationship, link-role, marker, table, and external-language definitions.
- Details: custom fields, links, attachments, CSV table details with a structured row editor.
- Base tables: table definitions persist and edit row records, with records included in preview/report output.
- Output: SVG/HTML export, full document HTML report, template-based file generation, printable HTML preview.
- Panels: Explorer, Inspector, Messages, Search, Preview, Diagnostics.
- Settings: light/dark theme and panel visibility.
- Hardening: full migration check script verifies WinUI/Core WPF boundaries and solution build; publish script emits self-contained WinUI output plus release manifest/zip.

## Remaining Gaps

### P0 - Legacy Semantic Depth

- Idea-definition clusters and deeper domain semantics are only partially represented.
- Legacy unknown data is preserved through extensions where mapped, but the bridge still needs broader legacy-field coverage.

### P1 - Tables And Structured Editors

- Table details and base-table records persist as CSV-style rows and can be edited through a structured row editor, but there is no full spreadsheet-like cell grid.

### P1 - Composite Navigation And Complements

- Modern documents preserve composite/shortcut metadata and support view switching, composite-view creation, and shortcut-object creation.
- Complements render as basic group regions and cards, but do not yet support all legacy complement-specific styling and geometry.

### P1 - Output Parity

- HTML report and generation are implemented.
- PDF/XPS report export and multi-sheet print preview are not ported.
- Presentation command is not ported.

### P2 - Product/Release Hardening

- Self-contained publish output is scripted; installer, signing, and update channel remain to be defined.
- Automated UI smoke tests should be added around launch, theme switching, open/save, create/edit, and report/generation.
- Product options/licensing checks remain a product decision.

## Recommended Backlog

1. Build a spreadsheet-like grid editor for table details and base-table records.
2. Add PDF/XPS or an explicit supported replacement path.
3. Add UI automation smoke tests around launch, theme switching, open/save, create/edit, and report/generation.
4. Define installer, signing, and update-channel packaging.
