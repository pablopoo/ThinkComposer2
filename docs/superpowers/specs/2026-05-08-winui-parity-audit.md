# WinUI Parity Audit

Date: 2026-05-08

## Summary

The WinUI app now covers the main editor loop and a substantial part of the rich-document workflow: `.tcdoc` modern documents, legacy import/merge, canvas editing, domain definitions, link-role variants, multi-view navigation metadata, composite views, shortcut authoring, details, table-detail/base-table row editing, markers, visual complements, styles, templates, reports, generation, validation, clipboard/multi-select, migration checks, and reproducible WinUI publish output.

It is still not a complete legacy replacement only where the remaining work depends on product/release decisions: native PDF/XPS generation, real signing certificate provisioning, installer/update hosting, and licensing policy.

## Current WinUI Coverage

- Document: new, open `.tcdoc/.tcview`, import `.tdom/.tcom`, merge external documents, save `.tcdoc`, export view-only `.tcview`, recent files, dirty indicator.
- Data contract: versioned `CompositionDocumentSnapshot`, XML roundtrip, schema validation, migration-safe `.tcview` warning.
- Legacy bridge: exports legacy packages to `.tcdoc` or `.tcview`; WinUI consumes `.tcdoc` through the external bridge tool without referencing WPF.
- Canvas: render concepts/relationships, pan, zoom, select, multi-select, move, precise keyboard move, create concept, Tab/Enter mind-map creation, create relationship, delete, undo/redo.
- Navigation: browse/switch modern document views from Explorer and command search; create/open composite views from selected concepts; create shortcut objects in the current view; preserve composite/shortcut metadata in `.tcdoc`.
- Inspector: edit concept/relationship names, layout, definitions, link-role variants, details, markers, styles, current-view complements, and generation templates.
- Canvas complements: render group regions plus legend/info/quote cards from current-view complements.
- Domain: browse/edit concept, relationship, link-role, marker, table, and external-language definitions.
- Details: custom fields, links, attachments, CSV table details with per-cell row editing.
- Base tables: table definitions persist and edit per-cell row records, with records included in preview/report output.
- Output: SVG/HTML export, full document HTML report, HTML presentation deck, template-based file generation, full-document printable HTML preview with browser print/save-to-PDF handoff.
- Panels: Explorer, Inspector, Messages, Search, Preview, Diagnostics.
- Settings: light/dark theme and panel visibility.
- Hardening: full migration check script verifies WinUI/Core WPF boundaries, smoke-script/release-script coverage, workflow output tests, mind-map accelerators, and solution build; launch/UI smoke scripts cover startup, main window, toolbar commands, theme toggle, domain/content explorer tabs, and concept creation; publish script emits self-contained WinUI output plus release manifest/zip; release script emits multi-runtime artifacts plus `release-index.json` with SHA-256 hashes, update channel metadata, and optional Authenticode signing.

## Remaining Gaps

### P0 - Legacy Semantic Depth

- Idea-definition clusters, definition flags, relationship directionality, marker clusters, complement definitions, and unknown legacy fields are preserved through first-class fields or extension buckets where mapped.
- Remaining risk is limited to unobserved legacy model fields outside the current bridge fixtures.

### P1 - Tables And Structured Editors

- Table details and base-table records persist as CSV-style rows and can be edited through per-cell controls.
- Remaining gap versus a spreadsheet is convenience behavior such as column resizing, sorting, and bulk paste.

### P1 - Composite Navigation And Complements

- Modern documents preserve composite/shortcut metadata and support view switching, composite-view creation, and shortcut-object creation.
- Complements render as group regions and cards, and explicit complement geometry is respected where present.
- Remaining gap is complete parity for every legacy complement-specific styling option.

### P1 - Output Parity

- HTML report, HTML presentation, template generation, metadata-directive stripping, and file-output workflow coverage are implemented.
- Native PDF/XPS report export is not ported; the supported replacement path is the full-document printable HTML preview plus browser print/save-to-PDF.
- Legacy native presentation output is replaced by an HTML presentation deck generated from document views.

### P2 - Product/Release Hardening

- Self-contained publish output, release index, hash verification, update metadata, and optional signing hooks are scripted; installer format, signing certificate, and external update hosting remain to be supplied.
- Automated UI smoke covers launch, optional startup documents, main-window creation, theme switching, explorer tab switching, toolbar presence, and concept creation; core workflow tests cover report, presentation, and generated-file outputs without relying on native file picker dialogs.
- Product options/licensing checks remain a product decision.

## Recommended Backlog

1. Add spreadsheet convenience behavior only if per-cell row editing is not sufficient.
2. Add native PDF/XPS only if printable HTML plus browser save-to-PDF is not sufficient.
3. Extend UI automation around native open/save dialogs once stable automation for file pickers is needed.
4. Choose installer format, provision signing certificate, and configure update-channel hosting.
