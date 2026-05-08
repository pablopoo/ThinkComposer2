# 100% WinUI Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the ThinkComposer WPF UI with a fully native WinUI 3 application while preserving existing app functionality and document compatibility.

**Architecture:** Use a strangler migration. Keep WPF as a temporary behavior reference, extract UI-independent core logic, build a parallel WinUI 3 app, move rendering to a native Win2D canvas, then cut over once the WinUI app reaches parity. The final executable must not require WPF for the shell, canvas, panels, or editor workflows.

**Tech Stack:** .NET 10, Windows App SDK / WinUI 3, C#, XAML, Win2D, neutral ThinkComposer core/domain assemblies.

---

## Final Direction

This is not a WPF reskin.

The final product is a 100% WinUI 3 app. WPF remains only as a temporary baseline while the new app catches up.

Reference design:

- `docs/superpowers/specs/2026-05-07-ui-modernization-design.md`
- `docs/ui-mockups/thinkcomposer-winui3-final-direction.html`

## Hard Rules

- `ThinkComposer.WinUI` must not reference WPF assemblies.
- No primary UI surface may depend on `System.Windows.Controls`, `Window`, `UserControl`, `Canvas`, `DrawingVisual`, `Adorner`, `DependencyObject`, or WPF resource dictionaries.
- Document/model logic moves behind UI-neutral contracts before WinUI consumes it.
- WPF app must keep building until WinUI reaches parity.
- WPF is removed or archived after cutover.

## Phase 0: Freeze WPF Baseline

**Files:**
- No product code changes.
- Update only verification notes in `MODERNIZATION_PLAN.md` if needed.

- [x] **Step 1: Build baseline**

Run:

```powershell
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

Expected:

```text
0 Advertencia(s)
0 Errores
```

- [x] **Step 2: Smoke-test baseline executable**

Run:

```powershell
.\ThinkComposer\bin\Debug\net10.0-windows\Instrumind.ThinkComposer.exe
```

Expected: the current app opens, creates a composition, and closes cleanly.

- [x] **Step 3: Record baseline workflows**

Create `docs/superpowers/specs/2026-05-07-thinkcomposer-parity-inventory.md` with this checklist:

```markdown
# ThinkComposer Parity Inventory

## Document
- Open
- New
- Save
- Save As
- Recent files

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
- Explorer navigation
- Inspector properties
- Messages
- Search
- Preview

## Output
- Export
- Print

## Settings
- Theme
- Workspace preferences
```

Expected: the migration has a visible parity target before WinUI work starts.

## Phase 1: Extract UI-Neutral Core

**Files:**
- Create: `ThinkComposer.Core/ThinkComposer.Core.csproj`
- Create: `ThinkComposer.Core/Primitives/TcPoint.cs`
- Create: `ThinkComposer.Core/Primitives/TcSize.cs`
- Create: `ThinkComposer.Core/Primitives/TcColor.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionNodeView.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionConnectorView.cs`
- Modify: `Instrumind_ThinkComposer.sln`

- [x] **Step 1: Create core project**

Run:

```powershell
dotnet new classlib -n ThinkComposer.Core -f net10.0
# Then set TargetFramework to net10.0-windows and add the project to the solution.
```

Expected: `ThinkComposer.Core` appears in the solution and builds without WPF references.

- [x] **Step 2: Add neutral primitives**

Create `ThinkComposer.Core/Primitives/TcPoint.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Primitives;

public readonly record struct TcPoint(double X, double Y);
```

Create `ThinkComposer.Core/Primitives/TcSize.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Primitives;

public readonly record struct TcSize(double Width, double Height);
```

Create `ThinkComposer.Core/Primitives/TcColor.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Primitives;

public readonly record struct TcColor(byte A, byte R, byte G, byte B);
```

- [x] **Step 3: Add render DTOs**

Create `ThinkComposer.Core/Rendering/CompositionNodeView.cs`:

```csharp
using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionNodeView(
    string Id,
    string Text,
    TcPoint Position,
    TcSize Size);
```

Create `ThinkComposer.Core/Rendering/CompositionConnectorView.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionConnectorView(
    string Id,
    string SourceId,
    string TargetId);
```

- [x] **Step 4: Verify no WPF references**

Run:

```powershell
dotnet build ThinkComposer.Core\ThinkComposer.Core.csproj
Select-String -Path ThinkComposer.Core\**\*.cs -Pattern "System.Windows|PresentationFramework|PresentationCore|WindowsBase"
```

Expected: build passes and search returns no matches.

- [x] **Step 5: Commit**

Run:

```powershell
git add ThinkComposer.Core Instrumind_ThinkComposer.sln
git commit -m "Create UI-neutral ThinkComposer core project"
```

## Phase 2: Create Parallel WinUI Shell

**Files:**
- Create: `ThinkComposer.WinUI/ThinkComposer.WinUI.csproj`
- Create: `ThinkComposer.WinUI/App.xaml`
- Create: `ThinkComposer.WinUI/App.xaml.cs`
- Create: `ThinkComposer.WinUI/MainWindow.xaml`
- Create: `ThinkComposer.WinUI/MainWindow.xaml.cs`
- Modify: `Instrumind_ThinkComposer.sln`

- [x] **Step 1: Create WinUI 3 project**

Use Visual Studio's WinUI 3 desktop template if CLI templates are not installed. Target .NET 10 and Windows App SDK.

Implementation note: installed the official `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` CLI template and generated `ThinkComposer.WinUI`. The template selected Windows App SDK `2.0.1`; the project is set to `WindowsAppSDKSelfContained=true` because the machine has Windows App Runtime 1.5-1.8 installed, not 2.0.

Expected: `ThinkComposer.WinUI` opens a blank native WinUI window.

- [x] **Step 2: Add shell layout**

Implement this structure:

```text
TitleBar
CommandBar
Activity rail
Explorer panel
Composition canvas host
Inspector panel
Bottom messages/search/preview panel
Status bar
```

Expected: the app visually matches `docs/ui-mockups/thinkcomposer-winui3-final-direction.html` at shell level.

- [x] **Step 3: Add collapsible panels**

Implement state for:

```text
Explorer collapsed
Inspector collapsed
Bottom panel collapsed
Focus mode
```

Expected: panel toggles behave like VS Code and content state is preserved.

- [x] **Step 4: Add light/dark theme**

Use WinUI theme resources and runtime theme switching.

Expected: app changes theme without restart.

- [x] **Step 5: Verify no WPF references**

Run:

```powershell
Select-String -Path ThinkComposer.WinUI\**\*.* -Pattern "System.Windows|PresentationFramework|PresentationCore|WindowsBase|Windows.Controls"
```

Expected: no WPF references.

## Phase 3: Build Native Win2D Canvas Spike

**Files:**
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Modify: `ThinkComposer.WinUI/ThinkComposer.WinUI.csproj`

- [x] **Step 1: Add Win2D package**

Add `Microsoft.Graphics.Win2D`.

Expected: WinUI project builds with Win2D available.

- [x] **Step 2: Render fake composition**

Draw:

```text
3 nodes
2 connectors
selected node outline
canvas grid
```

Expected: drawing is native and not WPF-hosted.

- [x] **Step 3: Add interaction**

Implement:

```text
pan
zoom
select node
drag selected node
```

Expected: interaction feels fast and stable.

## Phase 4: Load Read-Only Real Composition

**Files:**
- Create or modify core adapters under `ThinkComposer.Core`
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvasRenderer.cs`

- [x] **Step 1: Map generated composition source to DTOs**

Expose composition data as:

```csharp
IReadOnlyList<CompositionNodeView>
IReadOnlyList<CompositionConnectorView>
```

Expected: WinUI renderer consumes DTOs only.

Implementation note: added `CompositionViewSnapshot` plus `DemoCompositionViewSource` in `ThinkComposer.Core`, with `ThinkComposer.Core.Tests` covering node/connector snapshot shape. The legacy `.tcom` model adapter remains the next deeper document-loading step and must target this same DTO contract.

- [x] **Step 2: Render generated composition read-only**

Load an existing or generated composition.

Expected: WinUI shows real data without WPF controls.

Implementation note: `ThinkComposer.WinUI` now renders `CompositionViewSnapshot` data from Core instead of private hardcoded canvas data. Canvas remains read-only for model data: select, pan, and zoom are allowed; model writeback is deferred to Phase 5.

- [x] **Step 3: Add legacy package bridge**

Load legacy `.tdom`/`.tcom` packages through an isolated .NET Framework bridge and map their model objects to `CompositionViewSnapshot`.

Implementation note: added `LegacyCompositionSnapshotMapper` in `ThinkComposer.Core` using reflection so Core stays WPF-free, plus `ThinkComposer.LegacyBridge` targeting `net48` for BinaryFormatter package compatibility. `ThinkComposer.WinUI` does not reference the bridge or legacy WPF project; the next step is deciding whether WinUI consumes exported snapshots or a migrated modern document format.

- [x] **Step 4: Consume exported legacy snapshot in WinUI**

Export a `.tdom`/`.tcom` package to a UI-neutral `.tcview` snapshot and load that file in WinUI.

Implementation note: added `CompositionViewSnapshotXmlStore`, `ThinkComposer.LegacyBridge.Tool`, and a generated `docs/generated/All-Purpose.tcview` smoke fixture. WinUI now loads a `.tcview` path passed on the command line, or the generated default fixture if present, without referencing WPF or the legacy bridge.

## Phase 5: Port Editing Workflows

**Files:**
- Modify WinUI canvas, command handlers, inspector, and core application services as needed.

- [x] **Step 1: Concept editing**

Implement:

```text
create concept
select concept
move concept
edit concept text
delete concept
```

Implementation note: WinUI can create concepts, select them, drag/move them on the canvas, edit name/layout through the inspector, delete selected concepts, and save the edited `.tcview` snapshot. Core editing operations are covered by `ThinkComposer.Core.Tests`.

- [x] **Step 2: Relationship editing**

Implement:

```text
create relationship
select relationship
delete relationship
edit relationship properties
```

Implementation note: WinUI can start a relationship from the selected concept, complete it by selecting a target concept, select connector lines on the canvas, edit relationship text in the inspector, delete selected relationships, and save them in `.tcview`.

- [x] **Step 3: Undo/redo**

Connect edits to the existing command/undo model or create a neutral adapter if the current one is WPF-bound.

Expected: edit history works in WinUI.

Implementation note: a neutral `CompositionEditingSession` now provides undo/redo for snapshot edits made by the WinUI shell, including concept and relationship edits.

## Phase 6: Port Panels And Commands

**Files:**
- Modify or create WinUI explorer, inspector, messages, search, preview, command palette, and settings views.

- [x] **Step 1: Explorer**

Port composition/domain navigation.

Implementation note: Explorer now lists composition concepts and relationships as selectable tree entries; choosing one selects the matching canvas object.

- [x] **Step 2: Inspector**

Port selected object properties and actions.

Implementation note: Inspector now shows contextual concept/relationship state, editable names/layout for concepts, source/target for relationships, and contextual create/delete actions.

- [x] **Step 3: Messages/search/preview**

Port bottom panel workflows.

Implementation note: the WinUI bottom panel now has functional Messages, Search, Preview, and Diagnostics tabs. Search reuses the command/object catalog; Preview renders a text snapshot summary.

- [x] **Step 4: Command search**

Add command/object/view search.

Implementation note: the WinUI command box now searches base commands, concepts, and relationships. Submitting a command executes it; submitting a concept or relationship selects it on the canvas.

## Phase 7: Port Output And App Services

**Files:**
- Modify WinUI app services and core adapters as needed.

- [ ] **Step 1: Open/save parity**

Existing documents open and save correctly from WinUI.

Implementation note: WinUI now supports New, Open `.tcview`, Save, and Save As `.tcview` through native Windows file pickers. WinUI can also open `.tdom`/`.tcom` by running the external legacy bridge tool and importing the package into a temporary `.tcview`; save-back to the original legacy package remains pending.

- [x] **Step 2: Export/print parity**

Port export and print workflows or define a supported replacement if current code is WPF-only.

Implementation note: WinUI now exports the active composition as printable HTML or SVG. The print workflow uses generated printable HTML launched through Windows as the supported WinUI replacement for the WPF print pipeline.

- [x] **Step 3: Settings parity**

Port workspace preferences and theme settings.

Implementation note: WinUI now persists theme plus explorer, inspector, and bottom panel visibility under local app data and restores them on startup.

## Phase 8: Cut Over To WinUI

**Files:**
- Modify solution/startup docs.
- Modify packaging/deployment files.

- [ ] **Step 1: Make WinUI primary executable**

Expected: normal launch path starts WinUI, not WPF.

- [ ] **Step 2: Run parity inventory**

Use `docs/superpowers/specs/2026-05-07-thinkcomposer-parity-inventory.md`.

Expected: all required parity items pass or have documented replacement behavior.

## Phase 9: Remove WPF UI Dependency

**Files:**
- Remove or archive WPF app project after WinUI parity.
- Keep reusable non-WPF libraries only.

- [ ] **Step 1: Remove WPF startup app**

Expected: final app does not require the old WPF shell.

- [ ] **Step 2: Verify final WinUI app**

Run:

```powershell
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

Expected: solution builds and WinUI is the primary app.

## First Concrete Milestone

Build this before extracting large areas:

```text
ThinkComposer.WinUI opens
native shell matches the mockup direction
light/dark themes work
explorer/inspector/bottom panels collapse
Win2D canvas renders a fake interactive composition
WinUI project has no WPF references
```

If this milestone fails, stop and reassess before moving deeper into the migration.
