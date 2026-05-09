# ThinkComposer2 Command UX Parity Design

Date: 2026-05-09

## Goal

ThinkComposer2 must preserve the functional power of the original ThinkComposer command toolbar without recreating the old toolbar layout.

The target UX is a modern native editor: compact top bar, contextual right-click menus, command palette, inspector-driven editing, and a dedicated domain-management surface.

## Design Decision

Do not port the original dynamic WPF toolbar one-to-one.

Use five command surfaces:

1. Compact top bar for frequent global actions.
2. Canvas context menu for selected-object actions.
3. Inspector for persistent object editing.
4. View menu near zoom/status for view-state toggles.
5. Domain Studio for domain/table/definition administration.

The command palette must expose all commands, even if they also appear elsewhere.

## Command Surfaces

### Top Bar

Keep the top bar small and always visible.

Commands:

- New document
- Open
- Recent
- Save
- Save As
- New concept
- Create relationship
- Undo
- Redo
- Export
- Report
- Presentation
- Generate files
- Print preview
- Command search
- Focus canvas
- Toggle theme

Avoid adding low-frequency formatting, alignment, and domain-administration commands here.

### Canvas Context Menu

Right-click should become the main home for selection-specific commands.

When nothing is selected:

- New concept here
- Paste
- Select all
- Fit to view
- View options

When one concept is selected:

- Edit name
- Convert type
- Create relationship from this
- Open composite view
- Create shortcut here
- Cut
- Copy
- Paste shortcut
- Delete
- Get format
- Apply format
- Bring to front
- Send to back

When multiple objects are selected:

- Cut
- Copy
- Delete
- Align top
- Align left
- Align right
- Align bottom
- Align center
- Align middle
- Same width
- Same height
- Same size
- Distribute horizontally
- Distribute vertically
- Bring to front
- Send to back
- Bring forward
- Send backward
- Apply format

When a relationship is selected:

- Edit name
- Change relationship definition
- Change link-role variant
- Delete
- Get format
- Apply format

### Inspector

The inspector is for persistent editing, not transient commands.

Sections:

- Object: name, type/definition, status/markers.
- Layout: x, y, width, height.
- Style: fill, stroke, text color, line thickness.
- Shape: geometry, multiple symbol, flip, tilt.
- Text: font family, size, weight, italic, alignment.
- Details: custom fields, links, attachments, table details.
- Relationships: incoming/outgoing summary and navigation.
- Link role: role variant selection for relationships.
- Complements: view-level cards, group regions, legends.
- Generation: templates and generation preview.
- Actions: create relationship, open composite view, create shortcut, delete.

This replaces many original toolbar formatting buttons with a more discoverable panel.

### View Menu

Add a compact view-options menu near the zoom/status affordance.

Commands:

- Actual size
- Zoom in
- Zoom out
- Fit to view
- Presentation mode
- Full-screen mode
- Show grid
- Snap to grid
- Grid lines / grid points
- Show indicators
- Show markers
- Show marker titles
- Show concept definition labels
- Show relationship definition labels
- Show link-role descriptor labels
- Show link-role definitor labels
- Show link-role variant labels
- Auto-size by entered text

These are view state and visibility commands, so they should not crowd object editing.

### Command Palette

The existing command search becomes the universal command palette.

It must index:

- Global commands.
- Contextual selection commands.
- View commands.
- Domain commands.
- Recent files.
- Concepts.
- Relationships.
- Views.
- Definitions.
- Generation templates.
- Complements.

Command palette behavior:

- Disabled commands remain searchable but show why they are unavailable.
- Selecting an unavailable command should show a short status explanation, not silently do nothing.
- Keyboard-first users should be able to complete all common workflows without touching the toolbar.

### Domain Studio

Domain commands should not live in the main toolbar.

Create a dedicated Domain Studio surface reachable from:

- Explorer > Domain.
- Command palette.
- A Domain button/action in the inspector when a document is open.

Domain Studio covers:

- Edit domain metadata.
- New domain.
- Open domain.
- Save domain as.
- Concept definitions.
- Relationship definitions.
- Link-role variant definitions.
- Marker definitions.
- Table-structure definitions.
- Base tables.
- External languages.
- Idea-definition clusters.

This keeps normal diagram editing clean while preserving advanced authoring.

## Original Command Parity Mapping

### Covered Today

- New, Open, Recent, Save, Save As.
- Merge document.
- Export HTML/SVG.
- HTML report.
- HTML presentation.
- Generate files.
- Print preview through printable HTML.
- Undo, Redo, Delete, Cut, Copy, Paste.
- New concept, new relationship.
- Command/object/view search.
- Light/dark theme.
- Focus canvas.

### Needs UX Placement And Implementation

- Save All: only needed if multi-document tabs are restored.
- Edit composition properties: inspector/document settings panel.
- Close document: document tab/context action once multi-document tabs exist.
- Select All: context menu and command palette.
- Find & Replace: bottom search panel upgrade.
- Paste Shortcut: context menu and command palette.
- Go to Parent: context menu, explorer, command palette.
- Export image/PDF/XPS: export menu, with HTML/PDF strategy decided separately.
- Native print: replace or defer behind printable HTML strategy.
- Format commands: inspector plus context menu for get/apply format.
- Alignment/distribution/z-order: multi-select context menu.
- Shape/flip/tilt/multiple symbol: inspector shape section plus context menu.
- View toggles: view menu and command palette.
- Generation Preview: generation inspector section and command palette.
- Domain commands: Domain Studio.
- About/update: app/settings menu, not toolbar.
- Send by email: not P1; can be removed or implemented later as share/export workflow.

## Keyboard Model

Required accelerators:

- Ctrl+N: New document.
- Ctrl+O: Open.
- Ctrl+S: Save.
- Ctrl+Shift+S: Save As.
- Ctrl+Z / Ctrl+Y: Undo / Redo.
- Ctrl+X / Ctrl+C / Ctrl+V: Cut / Copy / Paste.
- Ctrl+A: Select all.
- Delete: Delete selection.
- Ctrl+Shift+P: Command palette.
- F8: Fit to view.

Optional later:

- Ctrl+G: Go to parent or find next, pending final search behavior.
- Ctrl+Shift+F: Find & Replace.

## Implementation Phases

### Phase 1: Command Model

Add command metadata for:

- required selection count
- command category
- surface placement
- enabled/disabled reason
- keyboard accelerator
- execution handler

The command palette, top bar, context menu, and view menu should read from the same command catalog.

### Phase 2: Context Menus

Add right-click menus for:

- empty canvas
- single concept
- relationship
- multi-selection

This phase should implement Select All, Paste Shortcut, Go to Parent, and common selection commands first.

### Phase 3: View Menu

Add view menu and wire:

- zoom commands
- fit/actual size
- grid toggles
- label/indicator toggles

Persist view settings in `.tcdoc` when they affect document/view state.

### Phase 4: Selection Layout Commands

Implement:

- align
- same size
- distribute spacing
- z-order

All must support undo/redo as a single command operation.

### Phase 5: Inspector Expansion

Add missing inspector sections:

- Shape
- Text format
- Generation preview
- Document/composition properties

### Phase 6: Domain Studio

Create the Domain Studio view after command model and context menu work stabilizes.

Start with definition lists and simple edit forms. Advanced domain authoring can remain incremental.

## Testing

Automated coverage:

- Command catalog unit tests for command visibility, labels, and enabled states.
- Snapshot editing tests for align, distribute, z-order, paste shortcut, and view toggles.
- UI smoke tests for context menu opening, command palette execution, and view menu toggles.

Manual QA:

- Compare original WPF toolbar command list against command palette search results.
- Open `Business_Model.tdom`.
- Verify commands work from at least one modern surface.
- Verify disabled commands explain why they are disabled.

## Acceptance Criteria

- Users can access every P1 original toolbar command from a modern surface.
- Frequent commands stay visible in the top bar.
- Selection commands are available from right-click.
- View toggles live in a view menu and command palette.
- Domain administration lives in Domain Studio, not the main toolbar.
- Command palette can find every command.
- Commands that are not available explain the missing precondition.
- New commands are covered by smoke or unit tests before being considered done.
