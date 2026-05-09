# ThinkComposer Parity Inventory

Date: 2026-05-08

Detailed audit: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`.

Status: WinUI is the primary app and covers the main editor, document, domain, output, and inspection workflows. Remaining non-P2 work is concentrated in optional spreadsheet conveniences and any legacy fields not yet seen in bridge fixtures.

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
- Tab/Enter mind-map creation for child and sibling concepts.
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
- Details: custom fields, links, attachments, CSV table details with per-cell row editing.
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
- HTML presentation deck.
- Template-based file generation.
- Generated output directory writing with path-escape checks.
- Printable HTML preview.

## Cutover

- `ThinkComposer.WinUI` is the solution app.
- WinUI/Core contain no WPF references.
- WPF remains only behind `ThinkComposer.LegacyBridge` for legacy import compatibility.
- `scripts/check-winui-migration.ps1` validates the WinUI/Core boundary and builds the solution.
