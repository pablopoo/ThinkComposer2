# ThinkComposer 100% WinUI Modernization Design

Date: 2026-05-07

## Decision

The final application target is 100% WinUI 3 on Windows App SDK.

The current WPF app is only a temporary control/baseline while the migration reaches feature parity. The final product must not depend on WPF for its main shell, canvas, panels, commands, or editor workflows.

Reference mockups:

- `docs/ui-mockups/thinkcomposer-winui3-final-direction.html`
- `docs/ui-mockups/thinkcomposer-b2-vscode-light-dark.html`

## Product Goal

Keep ThinkComposer's functionality, but allow the UI to change radically if that improves UX.

The app should feel like a modern native editor: fast startup, dense workspace, collapsible panels, command search, central canvas, modern inspector, clean light theme, and a VS Code-inspired dark theme.

## Non-Negotiables

- Final UI is native WinUI 3, not WPF.
- No Electron/WebView/web UI runtime as the primary app shell.
- Existing document compatibility must be preserved.
- Existing core workflows must survive: create/open/save compositions, edit concepts and relationships, inspect properties, navigate content, use messages/search/preview, export/print where supported today.
- WPF remains buildable only until the WinUI app reaches parity.

## Target UX

The final shell uses a Figma/VS Code-style workspace:

- Left activity rail for workspace areas.
- Collapsible explorer for composition/domain navigation.
- Central GPU-accelerated composition canvas.
- Right inspector for selected object properties and actions.
- Bottom panel for messages, search results, previews, diagnostics, and logs.
- Compact command bar and command/object search.
- Tabs for open compositions or views.
- Light and dark themes based on WinUI theme resources.

The canvas is the product center. Panels support the canvas; they should never dominate the workspace.

## Architecture

Use a staged replacement, not a direct XAML port.

### `ThinkComposer.Core`

UI-independent document/model/application logic.

This layer must not reference WPF or WinUI surface types. Where the old model leaks UI primitives, introduce neutral records such as:

```csharp
public readonly record struct TcPoint(double X, double Y);
public readonly record struct TcSize(double Width, double Height);
public readonly record struct TcColor(byte A, byte R, byte G, byte B);
```

### `ThinkComposer.Rendering`

Neutral canvas view models and render DTOs.

The renderer consumes composition-view data, not WPF controls:

```csharp
public sealed record CompositionNodeView(string Id, string Text, TcPoint Position, TcSize Size);
public sealed record CompositionConnectorView(string Id, string SourceId, string TargetId);
```

### `ThinkComposer.WinUI`

The new app shell, panels, commands, canvas host, dialogs, theme system, and native interaction layer.

The project must not reference `PresentationFramework`, `PresentationCore`, `WindowsBase`, `System.Xaml`, or WPF controls.

### Current WPF App

Temporary reference implementation only.

It stays usable during migration so behavior can be compared. It is removed or archived after WinUI reaches parity.

## Migration Stages

### Stage 0: Freeze Baseline

Keep the current WPF app buildable and smoke-testable. Capture current workflows before replacing them.

### Stage 1: Feature Inventory

Map the existing app into functional areas:

- document lifecycle
- composition canvas
- concept editing
- relationship editing
- inspector/properties
- explorer/navigation
- palettes/tools
- search/messages/preview
- export/print
- settings/theme

### Stage 2: Core Extraction

Move only UI-independent model/document contracts first. Do not drag WPF controls into the new core.

### Stage 3: WinUI Shell

Create the native WinUI shell with real layout, theme switching, collapsible panels, command bar, and placeholder canvas.

### Stage 4: Win2D Canvas Spike

Build a native canvas proof:

- draw nodes/connectors
- pan
- zoom
- select
- drag
- show selection affordances

This proves the app can feel fast without web UI.

### Stage 5: Read-Only Real Composition

Load an existing or generated composition into WinUI and render it read-only through neutral DTOs.

### Stage 6: Editing Workflows

Add editing in priority order:

1. create/select/move concepts
2. edit text/properties
3. create/select relationships
4. delete/undo/redo
5. palette/tool commands

### Stage 7: App Parity

Port dialogs, search, messages, preview, import/export, printing, and settings.

### Stage 8: Cutover

Make WinUI the primary executable once parity is acceptable.

### Stage 9: Remove WPF Dependency

Delete or archive the old WPF app after the WinUI executable replaces it. The final app must be WinUI-native.

## Acceptance Criteria

First milestone:

- `ThinkComposer.WinUI` opens.
- Shell is native WinUI.
- Light/dark themes work.
- Panels collapse like VS Code.
- Win2D canvas draws and interacts with a fake composition.
- WinUI project has no WPF references.

Final milestone:

- WinUI app preserves existing user workflows.
- Existing documents open/save correctly.
- Canvas editing is native and responsive.
- WPF app is no longer required for normal use.
- No primary UI surface remains WPF.

## Risks

- Existing model code may mix domain state with WPF geometry/brush/control types.
- Canvas fidelity may require custom text/layout work in Win2D.
- Printing/export may be more expensive than shell/canvas migration.
- Some legacy dialogs may need redesign instead of porting.

## Design Stance

Do not make a prettier WPF app. Build the replacement editor.

The UI can change completely, but the product capability must remain ThinkComposer.
