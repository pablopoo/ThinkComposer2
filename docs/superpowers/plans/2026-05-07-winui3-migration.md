# WinUI 3 Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move ThinkComposer toward a modern native WinUI 3 app without adding a web UI runtime and without rewriting business/document logic first.

**Architecture:** Use a strangler migration. Keep the current WPF app as the working product, extract UI-independent core services, create a parallel WinUI 3 shell, then migrate the canvas through a native Win2D renderer. Avoid attempting a direct XAML port because the current app has thousands of WPF-specific references and WPF controls cannot be reused directly in WinUI 3.

**Tech Stack:** .NET 10, Windows App SDK / WinUI 3, C#, XAML, Win2D for GPU-accelerated 2D canvas rendering, existing ThinkComposer document/model code after extraction.

---

## Rationale

WinUI 3 is the native Windows option for a modern, fast UI. It avoids WebView/Electron overhead and gives Fluent controls, modern composition, and Windows App SDK integration.

The current codebase is deeply WPF-bound. A quick scan found more than 3,000 references to WPF surface types such as `System.Windows`, `Window`, `UserControl`, `Canvas`, `DrawingVisual`, `Adorner`, `DependencyObject`, and related APIs. A direct conversion would be high-risk and slow.

Therefore the migration must be staged.

## Phase 0: Freeze the WPF Baseline

**Files:**
- No code changes.

- [ ] **Step 1: Keep WPF app buildable**

Run:

```powershell
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

Expected:

```text
0 Advertencia(s)
0 Errores
```

- [ ] **Step 2: Keep WPF smoke test as control**

Run the current net10 WPF app:

```powershell
.\ThinkComposer\bin\Debug\net10.0-windows\Instrumind.ThinkComposer.exe
```

Expected: app opens, a new composition can be created, and the process exits cleanly.

## Phase 1: Split Core From WPF

**Files:**
- Create: `ThinkComposer.Core/ThinkComposer.Core.csproj`
- Move or duplicate first-pass core-only classes from `ThinkComposer/Model`, `ThinkComposer/MetaModel`, and non-visual document services.
- Do not move visual WPF classes in this phase.

- [ ] **Step 1: Create `ThinkComposer.Core`**

Create a new SDK-style class library targeting `net10.0-windows` first. The first version references only UI-independent libraries.

- [ ] **Step 2: Move document/model contracts**

Start with model contracts and serialization boundaries, not UI controls. Candidate areas:

```text
ThinkComposer/Model
ThinkComposer/MetaModel/InformationMetaModel
ThinkComposer/MetaModel/GraphMetaModel
```

Expected: no direct dependency on `PresentationFramework`, `PresentationCore`, or `WindowsBase` in the new project.

- [ ] **Step 3: Add compatibility adapters**

Where WPF types are currently part of model contracts, introduce neutral equivalents:

```csharp
public readonly record struct TcPoint(double X, double Y);
public readonly record struct TcSize(double Width, double Height);
public readonly record struct TcColor(byte A, byte R, byte G, byte B);
```

Expected: WinUI and WPF can each map these to their own UI types.

## Phase 2: Create Parallel WinUI Shell

**Files:**
- Create: `ThinkComposer.WinUI/ThinkComposer.WinUI.csproj`
- Create: `ThinkComposer.WinUI/App.xaml`
- Create: `ThinkComposer.WinUI/App.xaml.cs`
- Create: `ThinkComposer.WinUI/MainWindow.xaml`
- Create: `ThinkComposer.WinUI/MainWindow.xaml.cs`

- [ ] **Step 1: Add WinUI 3 project**

Create a Windows App SDK / WinUI 3 desktop project targeting `net10.0-windows10.0.19041.0` or the supported target required by the installed Windows App SDK.

- [ ] **Step 2: Implement shell only**

Build the shell layout:

```text
TitleBar
CommandBar
NavigationView or custom activity rail
TreeView explorer
central canvas host
InfoBar/messages panel
right inspector panel
```

Expected: WinUI app opens without loading existing documents.

- [ ] **Step 3: Add light/dark theme**

Use WinUI theme resources and `ActualTheme` support. Default to light. Add a theme toggle.

Expected: theme changes without restarting.

## Phase 3: Native Canvas Spike

**Files:**
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Create: `ThinkComposer.WinUI/Canvas/CompositionCanvasRenderer.cs`

- [ ] **Step 1: Add Win2D package**

Add `Microsoft.Graphics.Win2D` to the WinUI project.

- [ ] **Step 2: Render a small diagram**

Use `CanvasControl` or `CanvasVirtualControl` to draw:

```text
3 nodes
2 connectors
selection rectangle
zoom/pan transform
```

Expected: smooth native rendering with no web runtime.

- [ ] **Step 3: Test interaction**

Implement:

```text
pan
zoom
single selection
drag selected node
```

Expected: interaction feels at least as responsive as WPF.

## Phase 4: Bridge Real Data

**Files:**
- Modify: `ThinkComposer.WinUI/MainWindow.xaml.cs`
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvasRenderer.cs`
- Modify: core model adapters from Phase 1.

- [ ] **Step 1: Load a sample composition**

Load a real or generated composition through `ThinkComposer.Core`.

- [ ] **Step 2: Map composition elements to render DTOs**

Use DTOs that do not expose WPF or WinUI types:

```csharp
public sealed record CompositionNodeView(string Id, string Text, TcPoint Position, TcSize Size);
public sealed record CompositionConnectorView(string Id, string SourceId, string TargetId);
```

Expected: renderer consumes DTOs only.

## Phase 5: Decide Go / No-Go

Go only if all criteria pass:

- WinUI shell starts fast.
- Canvas pan/zoom/select feels native and smooth.
- Existing document/model logic can be consumed without dragging WPF dependencies.
- Packaging/deployment is acceptable.
- WPF app remains usable during migration.

No-go if:

- Core extraction explodes into broad rewrites before a canvas spike works.
- Win2D cannot support needed text/geometry fidelity.
- Deployment overhead is unacceptable.

## First Concrete Milestone

The first milestone is not a full app. It is:

```text
ThinkComposer.WinUI opens
light/dark shell works
Win2D canvas draws a small interactive fake composition
no WPF dependency in the WinUI project
```

This milestone proves whether the native WinUI path is worth continuing.
