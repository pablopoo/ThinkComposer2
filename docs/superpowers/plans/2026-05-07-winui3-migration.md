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

- [ ] **Step 1: Build baseline**

Run:

```powershell
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

Expected:

```text
0 Advertencia(s)
0 Errores
```

- [ ] **Step 2: Smoke-test baseline executable**

Run:

```powershell
.\ThinkComposer\bin\Debug\net10.0-windows\Instrumind.ThinkComposer.exe
```

Expected: the current app opens, creates a composition, and closes cleanly.

- [ ] **Step 3: Record baseline workflows**

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

- [ ] **Step 1: Create core project**

Run:

```powershell
dotnet new classlib -n ThinkComposer.Core -f net10.0-windows
dotnet sln Instrumind_ThinkComposer.sln add ThinkComposer.Core\ThinkComposer.Core.csproj
```

Expected: `ThinkComposer.Core` appears in the solution and builds without WPF references.

- [ ] **Step 2: Add neutral primitives**

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

- [ ] **Step 3: Add render DTOs**

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

- [ ] **Step 4: Verify no WPF references**

Run:

```powershell
dotnet build ThinkComposer.Core\ThinkComposer.Core.csproj
Select-String -Path ThinkComposer.Core\**\*.cs -Pattern "System.Windows|PresentationFramework|PresentationCore|WindowsBase"
```

Expected: build passes and search returns no matches.

- [ ] **Step 5: Commit**

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

- [ ] **Step 1: Create WinUI 3 project**

Use Visual Studio's WinUI 3 desktop template if CLI templates are not installed. Target .NET 10 and Windows App SDK.

Expected: `ThinkComposer.WinUI` opens a blank native WinUI window.

- [ ] **Step 2: Add shell layout**

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

- [ ] **Step 3: Add collapsible panels**

Implement state for:

```text
Explorer collapsed
Inspector collapsed
Bottom panel collapsed
Focus mode
```

Expected: panel toggles behave like VS Code and content state is preserved.

- [ ] **Step 4: Add light/dark theme**

Use WinUI theme resources and runtime theme switching.

Expected: app changes theme without restart.

- [ ] **Step 5: Verify no WPF references**

Run:

```powershell
Select-String -Path ThinkComposer.WinUI\**\*.* -Pattern "System.Windows|PresentationFramework|PresentationCore|WindowsBase|Windows.Controls"
```

Expected: no WPF references.

## Phase 3: Build Native Win2D Canvas Spike

**Files:**
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvasRenderer.cs`
- Modify: `ThinkComposer.WinUI/ThinkComposer.WinUI.csproj`

- [ ] **Step 1: Add Win2D package**

Add `Microsoft.Graphics.Win2D`.

Expected: WinUI project builds with Win2D available.

- [ ] **Step 2: Render fake composition**

Draw:

```text
3 nodes
2 connectors
selected node outline
canvas grid
```

Expected: drawing is native and not WPF-hosted.

- [ ] **Step 3: Add interaction**

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

- [ ] **Step 1: Map real model to DTOs**

Expose composition data as:

```csharp
IReadOnlyList<CompositionNodeView>
IReadOnlyList<CompositionConnectorView>
```

Expected: WinUI renderer consumes DTOs only.

- [ ] **Step 2: Render real composition read-only**

Load an existing or generated composition.

Expected: WinUI shows real data without WPF controls.

## Phase 5: Port Editing Workflows

**Files:**
- Modify WinUI canvas, command handlers, inspector, and core application services as needed.

- [ ] **Step 1: Concept editing**

Implement:

```text
create concept
select concept
move concept
edit concept text
delete concept
```

- [ ] **Step 2: Relationship editing**

Implement:

```text
create relationship
select relationship
delete relationship
edit relationship properties
```

- [ ] **Step 3: Undo/redo**

Connect edits to the existing command/undo model or create a neutral adapter if the current one is WPF-bound.

Expected: edit history works in WinUI.

## Phase 6: Port Panels And Commands

**Files:**
- Modify or create WinUI explorer, inspector, messages, search, preview, command palette, and settings views.

- [ ] **Step 1: Explorer**

Port composition/domain navigation.

- [ ] **Step 2: Inspector**

Port selected object properties and actions.

- [ ] **Step 3: Messages/search/preview**

Port bottom panel workflows.

- [ ] **Step 4: Command search**

Add command/object/view search.

## Phase 7: Port Output And App Services

**Files:**
- Modify WinUI app services and core adapters as needed.

- [ ] **Step 1: Open/save parity**

Existing documents open and save correctly from WinUI.

- [ ] **Step 2: Export/print parity**

Port export and print workflows or define a supported replacement if current code is WPF-only.

- [ ] **Step 3: Settings parity**

Port workspace preferences and theme settings.

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
