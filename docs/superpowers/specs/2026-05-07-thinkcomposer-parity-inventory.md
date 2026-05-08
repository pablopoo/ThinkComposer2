# ThinkComposer Parity Inventory

Date: 2026-05-07

Detailed audit: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`.

Status: basic WinUI editor parity is implemented. Full legacy product parity is not complete; the major blocker is the simplified `.tcview` model, which does not yet preserve rich domain/composition data.

## Document

- Open: `.tcview` native open plus `.tdom`/`.tcom` legacy import to modern snapshot.
- New: WinUI creates empty compositions.
- Save: WinUI saves native `.tcview`; imported legacy packages route through Save As.
- Save As: WinUI writes modern `.tcview`.
- Recent files: persisted in WinUI settings and available from toolbar/search.

## Composition Canvas

- Create concept
- Select concept
- Move concept
- Edit concept text
- Create relationship
- Select relationship
- Delete selected object
- Undo/redo
- Zoom
- Pan

## Panels

- Explorer navigation: WinUI tree selects canvas concepts and relationships.
- Inspector properties: WinUI inspector edits concept text/layout, relationship text, source/target display, and contextual actions.
- Messages: WinUI bottom tab implemented.
- Search: WinUI bottom tab searches commands, concepts, and relationships.
- Preview: WinUI bottom tab shows a composition summary.

## Output

- Export: WinUI HTML/SVG export implemented.
- Print: WinUI printable HTML preview implemented as replacement path.

## Settings

- Theme: persisted in WinUI.
- Workspace preferences: panel visibility persisted in WinUI.

## Cutover

- Primary app: `ThinkComposer.WinUI` is the solution app; legacy WPF shell is no longer a top-level solution project.
- WPF dependency: WinUI/Core contain no WPF references. WPF remains only behind `ThinkComposer.LegacyBridge` for legacy import compatibility.
