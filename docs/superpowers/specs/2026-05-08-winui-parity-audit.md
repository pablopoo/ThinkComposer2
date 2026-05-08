# WinUI Parity Audit

Date: 2026-05-08

## Summary

The WinUI app now covers the basic editor loop: open/import, create, select, move, rename, connect, delete, undo/redo, save `.tcview`, export, preview, theme, panels, recent files, and command search.

It is not yet at full legacy product parity. The current WinUI data model is a simplified composition view snapshot. It does not preserve or expose many legacy ThinkComposer concepts: domain definitions, rich details, markers, complements, styles, templates, generation, and full reports.

## Evidence Inspected

- `ThinkComposer.WinUI`: current migrated shell, canvas, inspector, command search, export, settings.
- `ThinkComposer.Core`: current neutral snapshot model and XML store.
- `ThinkComposer/Composer/CompositionsManager.cs`: legacy command surface for export, print, report, clipboard, style, view, generation.
- `ThinkComposer/Composer/CompositionsManager.EditingCommands.cs`: legacy edit commands for clipboard, details, formatting.
- `ThinkComposer/Definitor/DomainsManager.cs`: legacy domain command surface.
- `ThinkComposer/Definitor/DomainServices.cs`: domain editors, definitions, markers, base tables, external languages, templates.
- `ThinkComposer/Composer/Reporting/ReportingManager.cs`: HTML and PDF/XPS report workflows.
- `ThinkComposer/ThinkComposer.csproj`: legacy feature inventory through included editors/generators.

## Current WinUI Coverage

- Document: new, open `.tcview`, import `.tdom/.tcom`, save `.tcview`, save as `.tcview`, recent files.
- Canvas: render concepts/relationships, pan, zoom, select, move, create concept, create relationship, delete.
- Inspector: rename concept/relationship, edit concept position/size, source/target display, contextual actions.
- Panels: Explorer, Inspector, Messages, Search, Preview, Diagnostics.
- Output: HTML/SVG export and printable HTML preview.
- Settings: theme and panel visibility.
- Cutover: WinUI is the primary solution app; WPF shell is no longer top-level.

## Major Gaps

### P0 - Full-Fidelity Document Model

Current `.tcview` is a simplified view snapshot. It preserves only enough data for visual nodes/connectors.

Missing:
- Domain model and domain metadata.
- Concept and relationship definitions.
- Idea identifiers beyond rendered nodes.
- Rich relationship/link role semantics.
- Markers, complements, shortcuts, nested/composite content.
- Details: attachments, links, tables, custom fields.
- Visual styles and per-object formatting.
- Output templates and external language declarations.

Impact: importing a rich legacy `.tdom/.tcom` into WinUI can drop product data if the user saves only `.tcview`. This is the most important blocker for claiming true 100% functionality.

Recommended next slice:
- Introduce a versioned modern document contract, not just a view snapshot.
- Extend the legacy bridge to export the complete domain/composition graph into that contract.
- Add round-trip tests proving that rich legacy documents preserve domain definitions, details, markers, complements, styles, and templates.

### P0 - Domain And Definition Workflows

Legacy has explicit domain operations and editors:
- New/Open/Edit/Save Domain.
- Concept definitions.
- Relationship definitions.
- Link-role variant definitions.
- Marker definitions.
- Table-structure definitions.
- Base tables.
- External languages.
- Idea-definition clusters.
- Output templates.

Current WinUI only displays a basic `Domain` tab placeholder and does not expose definition editing.

Recommended next slice:
- Build a WinUI Domain workspace with read/write lists for concept/relationship definitions.
- Start with read-only domain browsing from imported documents, then add editing and save.

### P1 - Details, Tables, Attachments, Links

Legacy includes dedicated UI for:
- `LocalDetailsEditor`
- `DetailsEditorMaintainer`
- `DetailAttachmentEditor`
- `DetailTableEditor`
- `DetailLinkEditor`
- `SingleTableRecordEditor`
- table import/export

Current WinUI Inspector does not expose details.

Recommended next slice:
- Extend core DTOs with idea details.
- Add Inspector tabs for Attachments, Links, Tables, and Custom Fields.
- Add table import/export tests before UI work.

### P1 - Formatting, Styles, Palettes

Legacy supports:
- Fill brush.
- Line brush/thickness/dash.
- Connector format.
- Text format.
- Get/apply format.
- Graphic styles.
- Definition-driven palettes.
- Grid/snap/indicators/label visibility toggles.

Current WinUI has fixed visual styling with limited theme support.

Recommended next slice:
- Add style fields to the core visual model.
- Add a compact style inspector with fill, stroke, connector, and text controls.
- Add palette-driven creation once domain definitions are available.

### P1 - Clipboard And Selection Commands

Legacy supports cut/copy/paste, paste shortcut, select all, parent navigation, and multi-selection-oriented commands.

Current WinUI supports single selection and delete, but not full clipboard/multi-select parity.

Recommended next slice:
- Add multi-select to the canvas.
- Add copy/cut/paste in `.tcview` JSON/XML clipboard format.
- Add select all and keyboard shortcuts.

### P1 - Reports, Generation, Print

Legacy supports:
- HTML reports.
- PDF/XPS reports.
- report configuration editor.
- file/code generation from output templates.
- generation preview.
- richer print/export workflows.

Current WinUI export/print is a useful replacement for basic output, but not report/generation parity.

Recommended next slice:
- Rebuild report generation over the modern document contract.
- Keep HTML report first; defer PDF until the HTML path is complete.
- Rebuild generation templates after domain external languages/templates are in core.

### P2 - Complements, Markers, Shortcuts, Composite Navigation

Legacy has commands and renderers for:
- markers and marker assignment.
- visual complements such as info cards, legends, group lines, quotes.
- shortcuts.
- parent/composite navigation.

Current WinUI does not implement these object types.

Recommended next slice:
- Add them only after the full document contract exists.
- Start with read-only rendering, then editing.

### P2 - Product/Options/Packaging

Still to harden:
- application options.
- licensing/edition checks if still desired.
- release packaging and installer replacement.
- automated UI smoke tests.
- migration warning when opening legacy documents.

## Recommended Backlog

1. **P0: Modern document contract**
   Create `CompositionDocumentSnapshot` or equivalent full-fidelity model with schema versioning, validation, and round-trip tests.

2. **P0: Full legacy bridge export**
   Expand the bridge beyond nodes/connectors so legacy documents import without data loss.

3. **P0: Migration-safe save path**
   When a user imports `.tdom/.tcom`, warn that saving as `.tcview` is the modern migration path and verify no rich data is being discarded.

4. **P1: Domain browser/editor**
   Port concept/relationship definitions and palettes first, then markers/tables/templates.

5. **P1: Details editor**
   Add attachments, links, tables, and custom fields in the Inspector.

6. **P1: Style system**
   Persist and edit fill, stroke, connector, text, and graphic styles.

7. **P1: Clipboard and multi-selection**
   Implement cut/copy/paste, paste shortcut, select all, and keyboard shortcuts.

8. **P1: Reports and generation**
   Port HTML report and generation preview after templates and full document data exist.

9. **P2: Complements and markers**
   Add visual complements and marker assignment/rendering.

10. **P2: Release hardening**
    Add UI automation smoke tests, packaging, installer/release docs, and migration docs.

## Next Best Task

Start with **P0: Modern document contract**.

Reason: most remaining UI parity depends on data that the current `.tcview` does not carry. Adding panels before preserving that data would create UI around missing state and increase rework.
