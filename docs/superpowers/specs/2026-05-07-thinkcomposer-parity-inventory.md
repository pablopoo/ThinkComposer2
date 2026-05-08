# ThinkComposer Parity Inventory

Date: 2026-05-08

Detailed audit: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`.

Status: WinUI is the primary app and covers the main editor, document, domain, output, and inspection workflows. Remaining work is concentrated in deep legacy semantics, specialized editors, PDF/XPS/multi-sheet output, and release hardening.

## Document

- Open: `.tcdoc`, `.tcview`, and legacy `.tdom/.tcom` through bridge import.
- New: WinUI creates empty modern documents.
- Merge: WinUI merges `.tcdoc/.tcview/.tdom/.tcom` into the active document.
- Save: WinUI saves full `.tcdoc`.
- Save As: WinUI writes `.tcdoc`; `.tcview` export is allowed with a data-loss warning for rich documents.
- Recent files: persisted and searchable.
- Dirty state: shown in app title.

## Composition Canvas

- Create/select/move concepts.
- Multi-select, copy/cut/paste, select all.
- Precise keyboard movement.
- Edit concept text/layout/style.
- Create/select/edit relationships.
- Delete selected objects.
- Undo/redo.
- Zoom/pan.

## Domain And Inspector

- Domain explorer for concept, relationship, marker, table, and external-language definitions.
- Definition create/update/delete.
- Definition-driven concept/relationship creation.
- Details: custom fields, links, attachments, CSV table details.
- Markers: assign marker ids to concepts/relationships.
- Complements: edit view complement extensions.
- Templates: edit generation templates.
- Styles: edit fill/stroke/text/line thickness for concepts/relationships.

## Panels

- Explorer navigation.
- Inspector properties/actions.
- Messages.
- Search for commands, objects, definitions, templates, complements, and recent files.
- Preview.
- Diagnostics with validation errors.

## Output

- HTML/SVG export.
- Full HTML report.
- Template-based file generation.
- Printable HTML preview.

## Cutover

- `ThinkComposer.WinUI` is the solution app.
- WinUI/Core contain no WPF references.
- WPF remains only behind `ThinkComposer.LegacyBridge` for legacy import compatibility.
- `scripts/check-winui-migration.ps1` validates the WinUI/Core boundary and builds the solution.
