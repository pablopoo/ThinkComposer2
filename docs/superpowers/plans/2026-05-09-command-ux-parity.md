# Command UX Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement modern WinUI access to the original ThinkComposer command power without recreating the legacy toolbar layout.

**Architecture:** Use one shared command catalog in `ThinkComposer.Core` for command metadata, availability, keyboard hints, and surface placement. Keep domain and snapshot mutations in core editors, while `ThinkComposer.WinUI` only builds modern surfaces: compact command bar, canvas context menus, command palette, view menu, inspector sections, and Domain Studio.

**Tech Stack:** .NET 10, C# records, WinUI 3, XAML, Win2D, existing console test projects, PowerShell smoke scripts.

---

## References

- Spec: `docs/superpowers/specs/2026-05-09-command-ux-parity-design.md`
- Parity audit: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`
- Current WinUI shell: `ThinkComposer.WinUI/MainPage.xaml`
- Current WinUI logic: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Current canvas: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Current command catalog: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`
- Current tests: `ThinkComposer.Core.Tests/Program.cs`

## Scope

Included:

- Command metadata and disabled reasons.
- All P1 toolbar parity commands from the command UX spec.
- Canvas context menus.
- View menu and persisted view options.
- Alignment, distribution, same-size, and z-order commands.
- Inspector sections for shape, text, generation, document properties.
- Domain Studio for domain definitions and templates.
- Unit and smoke coverage.

Excluded from this plan:

- Send by email.
- Multi-document tab model, Save All, and Close Document.
- Replacing printable HTML with a native print pipeline.

## File Structure

Core command model:

- Modify: `ThinkComposer.Core/Rendering/CompositionCommandEntry.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandIds.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandCategory.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandSurface.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandContext.cs`

Core editing:

- Modify: `ThinkComposer.Core/Rendering/CompositionSnapshotSelectionEditor.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionAlignment.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionDistribution.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionSizeMode.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionZOrder.cs`

View options:

- Create: `ThinkComposer.Core/Rendering/CompositionViewOptionsSnapshot.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionViewLayerSnapshot.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotEditor.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotXmlStore.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentPersistenceAdvisor.cs`

WinUI:

- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Create: `ThinkComposer.WinUI/Domain/DomainStudioView.xaml`
- Create: `ThinkComposer.WinUI/Domain/DomainStudioView.xaml.cs`

Tests and docs:

- Modify: `ThinkComposer.Core.Tests/Program.cs`
- Modify: `scripts/smoke-winui-ui.ps1`
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

## Task 1: Enrich Command Catalog

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandEntry.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandIds.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandCategory.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandSurface.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionCommandContext.cs`
- Test: `ThinkComposer.Core.Tests/Program.cs`

- [ ] **Step 1: Add failing command metadata tests**

Append assertions near the existing command catalog tests in `ThinkComposer.Core.Tests/Program.cs`:

```csharp
var emptyCommandContext = new CompositionCommandContext(
    HasDocument: false,
    HasSnapshot: false,
    SelectedNodeCount: 0,
    HasSelectedConnector: false,
    HasClipboard: false,
    CanUndo: false,
    CanRedo: false);
var disabledCommands = CompositionCommandCatalog.ForSnapshot(null, emptyCommandContext);
var disabledSave = disabledCommands.Single(entry => entry.Id == CompositionCommandIds.Save);
AssertEqual(false, disabledSave.IsEnabled, "save disabled without document");
AssertEqual("Open or create a document first.", disabledSave.DisabledReason, "save disabled reason");

var selectedCommandContext = emptyCommandContext with
{
    HasDocument = true,
    HasSnapshot = true,
    SelectedNodeCount = 2,
    HasClipboard = true,
    CanUndo = true,
    CanRedo = true
};
var contextualCommands = CompositionCommandCatalog.ForSnapshot(snapshot, selectedCommandContext);
AssertTrue(contextualCommands.Any(entry =>
    entry.Id == CompositionCommandIds.AlignLeft &&
    entry.Surfaces.HasFlag(CompositionCommandSurface.CanvasContextMenu) &&
    entry.IsEnabled), "align left command enabled for multi-selection");
AssertTrue(contextualCommands.Any(entry =>
    entry.Id == CompositionCommandIds.CommandPalette &&
    entry.Accelerator == "Ctrl+Shift+P"), "command palette accelerator");
```

- [ ] **Step 2: Run red test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: compile fails because `CompositionCommandContext`, `CompositionCommandSurface`, and new command ids do not exist.

- [ ] **Step 3: Add metadata types**

Create `ThinkComposer.Core/Rendering/CompositionCommandCategory.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public enum CompositionCommandCategory
{
    Document,
    Edit,
    Canvas,
    View,
    Format,
    Layout,
    Generation,
    Domain,
    App
}
```

Create `ThinkComposer.Core/Rendering/CompositionCommandSurface.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

[Flags]
public enum CompositionCommandSurface
{
    None = 0,
    TopBar = 1,
    CanvasContextMenu = 2,
    Inspector = 4,
    ViewMenu = 8,
    CommandPalette = 16,
    DomainStudio = 32
}
```

Create `ThinkComposer.Core/Rendering/CompositionCommandContext.cs`:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionCommandContext(
    bool HasDocument,
    bool HasSnapshot,
    int SelectedNodeCount,
    bool HasSelectedConnector,
    bool HasClipboard,
    bool CanUndo,
    bool CanRedo)
{
    public static CompositionCommandContext Empty { get; } = new(false, false, 0, false, false, false, false);
}
```

- [ ] **Step 4: Extend command entry record**

Change `CompositionCommandEntry` so old call sites still compile:

```csharp
public sealed record CompositionCommandEntry(
    string Id,
    string Title,
    CompositionCommandEntryKind Kind,
    string? TargetId = null,
    string Subtitle = "",
    CompositionCommandCategory Category = CompositionCommandCategory.App,
    CompositionCommandSurface Surfaces = CompositionCommandSurface.CommandPalette,
    string Accelerator = "",
    bool IsEnabled = true,
    string DisabledReason = "")
{
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} - {Subtitle}";
    }
}
```

- [ ] **Step 5: Add command ids**

Add these constants to `CompositionCommandIds`:

```csharp
public const string CommandPalette = "app.commandPalette";
public const string SelectAll = "edit.selectAll";
public const string PasteShortcut = "edit.pasteShortcut";
public const string GoParent = "view.goParent";
public const string ActualSize = "view.actualSize";
public const string ZoomIn = "view.zoomIn";
public const string ZoomOut = "view.zoomOut";
public const string FitToView = "view.fitToView";
public const string PresentationMode = "view.presentationMode";
public const string FullScreen = "view.fullScreen";
public const string ToggleGrid = "view.toggleGrid";
public const string ToggleSnapToGrid = "view.toggleSnapToGrid";
public const string ToggleGridPoints = "view.toggleGridPoints";
public const string ToggleIndicators = "view.toggleIndicators";
public const string ToggleMarkers = "view.toggleMarkers";
public const string ToggleMarkerTitles = "view.toggleMarkerTitles";
public const string ToggleConceptDefinitionLabels = "view.toggleConceptDefinitionLabels";
public const string ToggleRelationshipDefinitionLabels = "view.toggleRelationshipDefinitionLabels";
public const string ToggleLinkRoleDescriptorLabels = "view.toggleLinkRoleDescriptorLabels";
public const string ToggleLinkRoleDefinitorLabels = "view.toggleLinkRoleDefinitorLabels";
public const string ToggleLinkRoleVariantLabels = "view.toggleLinkRoleVariantLabels";
public const string ToggleAutoSizeByText = "view.toggleAutoSizeByText";
public const string GetFormat = "format.get";
public const string ApplyFormat = "format.apply";
public const string AlignTop = "layout.alignTop";
public const string AlignLeft = "layout.alignLeft";
public const string AlignRight = "layout.alignRight";
public const string AlignBottom = "layout.alignBottom";
public const string AlignCenter = "layout.alignCenter";
public const string AlignMiddle = "layout.alignMiddle";
public const string SameWidth = "layout.sameWidth";
public const string SameHeight = "layout.sameHeight";
public const string SameSize = "layout.sameSize";
public const string DistributeHorizontally = "layout.distributeHorizontally";
public const string DistributeVertically = "layout.distributeVertically";
public const string BringToFront = "layout.bringToFront";
public const string SendToBack = "layout.sendToBack";
public const string BringForward = "layout.bringForward";
public const string SendBackward = "layout.sendBackward";
public const string GenerationPreview = "generation.preview";
public const string DomainStudio = "domain.studio";
public const string EditDocumentProperties = "document.properties";
```

- [ ] **Step 6: Rebuild base commands from metadata**

Replace `BaseCommands` in `CompositionCommandCatalog` with entries that set category, surface, accelerator, and availability. Keep `ForSnapshot(CompositionViewSnapshot? snapshot)` and `ForDocument(CompositionDocumentSnapshot? document)` as compatibility overloads that call the new overload with `CompositionCommandContext.Empty`.

Core rule:

```csharp
public static IReadOnlyList<CompositionCommandEntry> ForSnapshot(
    CompositionViewSnapshot? snapshot,
    CompositionCommandContext context)
{
    var entries = BuildBaseCommands(context).ToList();
    if (snapshot is null)
    {
        return entries;
    }

    entries.AddRange(snapshot.Nodes.Select(node => new CompositionCommandEntry(
        $"node.{node.Id}",
        string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text,
        CompositionCommandEntryKind.Node,
        node.Id,
        "Concept",
        CompositionCommandCategory.Canvas,
        CompositionCommandSurface.CommandPalette)));

    entries.AddRange(snapshot.Connectors.Select(connector => new CompositionCommandEntry(
        $"connector.{connector.Id}",
        string.IsNullOrWhiteSpace(connector.Text) ? "Relationship" : connector.Text,
        CompositionCommandEntryKind.Connector,
        connector.Id,
        "Relationship",
        CompositionCommandCategory.Canvas,
        CompositionCommandSurface.CommandPalette)));

    return entries;
}
```

- [ ] **Step 7: Run green test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: all core tests pass.

- [ ] **Step 8: Commit**

```powershell
git add ThinkComposer.Core\Rendering ThinkComposer.Core.Tests\Program.cs
git commit -m "Add command metadata catalog"
```

## Task 2: Add Selection Layout Editing

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionSnapshotSelectionEditor.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionAlignment.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionDistribution.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionSizeMode.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSelectionZOrder.cs`
- Test: `ThinkComposer.Core.Tests/Program.cs`

- [ ] **Step 1: Add failing layout tests**

Add a four-node snapshot fixture and assert:

```csharp
var layoutSnapshot = indexedSnapshot with
{
    Nodes =
    [
        new CompositionNodeView("a", "A", new TcPoint(100, 200), new TcSize(80, 40)),
        new CompositionNodeView("b", "B", new TcPoint(240, 260), new TcSize(120, 60)),
        new CompositionNodeView("c", "C", new TcPoint(420, 220), new TcSize(60, 80))
    ]
};
var alignedLeft = CompositionSnapshotSelectionEditor.Align(
    layoutSnapshot,
    ["a", "b", "c"],
    CompositionSelectionAlignment.Left);
AssertEqual(100.0, alignedLeft.Nodes.Single(node => node.Id == "b").Position.X, "align left b");
AssertEqual(100.0, alignedLeft.Nodes.Single(node => node.Id == "c").Position.X, "align left c");

var sameSize = CompositionSnapshotSelectionEditor.ResizeToMatch(
    layoutSnapshot,
    ["a", "b", "c"],
    CompositionSelectionSizeMode.SameSize);
AssertEqual(80.0, sameSize.Nodes.Single(node => node.Id == "b").Size.Width, "same size width");
AssertEqual(40.0, sameSize.Nodes.Single(node => node.Id == "c").Size.Height, "same size height");

var broughtFront = CompositionSnapshotSelectionEditor.Reorder(
    layoutSnapshot,
    ["a"],
    CompositionSelectionZOrder.BringToFront);
AssertEqual("a", broughtFront.Nodes.Last().Id, "bring front last node");
```

- [ ] **Step 2: Run red test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: compile fails because selection layout enums and methods do not exist.

- [ ] **Step 3: Add selection enums**

Create enums with these values:

```csharp
public enum CompositionSelectionAlignment { Top, Left, Right, Bottom, Center, Middle }
public enum CompositionSelectionDistribution { Horizontal, Vertical }
public enum CompositionSelectionSizeMode { SameWidth, SameHeight, SameSize }
public enum CompositionSelectionZOrder { BringToFront, SendToBack, BringForward, SendBackward }
```

- [ ] **Step 4: Implement layout methods**

Add methods to `CompositionSnapshotSelectionEditor`:

```csharp
public static CompositionViewSnapshot Align(
    CompositionViewSnapshot snapshot,
    IReadOnlyCollection<string> nodeIds,
    CompositionSelectionAlignment alignment)
```

Use selected node bounds only. For `Left` and `Top`, use the minimum selected coordinate. For `Right` and `Bottom`, align outer edges. For `Center` and `Middle`, align centers.

Add:

```csharp
public static CompositionViewSnapshot ResizeToMatch(
    CompositionViewSnapshot snapshot,
    IReadOnlyList<string> nodeIds,
    CompositionSelectionSizeMode mode)
```

Use the first selected node as the size source.

Add:

```csharp
public static CompositionViewSnapshot Distribute(
    CompositionViewSnapshot snapshot,
    IReadOnlyCollection<string> nodeIds,
    CompositionSelectionDistribution distribution)
```

Sort by X for horizontal and Y for vertical. Keep first and last fixed and evenly distribute the remaining top-left positions.

Add:

```csharp
public static CompositionViewSnapshot Reorder(
    CompositionViewSnapshot snapshot,
    IReadOnlyCollection<string> nodeIds,
    CompositionSelectionZOrder zOrder)
```

Treat nodes appearing after other nodes in `snapshot.Nodes` as visually above them because the canvas draws nodes in list order.

- [ ] **Step 5: Run green test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: all core tests pass.

- [ ] **Step 6: Commit**

```powershell
git add ThinkComposer.Core\Rendering ThinkComposer.Core.Tests\Program.cs
git commit -m "Add selection layout editing"
```

## Task 3: Persist View Options

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionViewOptionsSnapshot.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionViewLayerSnapshot.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotEditor.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotXmlStore.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentPersistenceAdvisor.cs`
- Test: `ThinkComposer.Core.Tests/Program.cs`

- [ ] **Step 1: Add failing view-options roundtrip test**

Add this to the modern document roundtrip section:

```csharp
var viewOptions = new CompositionViewOptionsSnapshot(
    ShowGrid: true,
    SnapToGrid: true,
    ShowGridPoints: true,
    ShowIndicators: false,
    ShowMarkers: true,
    ShowMarkerTitles: true,
    ShowConceptDefinitionLabels: true,
    ShowRelationshipDefinitionLabels: true,
    ShowLinkRoleDescriptorLabels: true,
    ShowLinkRoleDefinitorLabels: false,
    ShowLinkRoleVariantLabels: true,
    AutoSizeByEnteredText: true);
var optionsDocument = modernDocument with
{
    Views =
    [
        modernDocument.Views[0] with { Options = viewOptions }
    ]
};
var optionsPath = Path.Combine(Path.GetTempPath(), $"thinkcomposer-options-{Guid.NewGuid():N}.tcdoc");
CompositionDocumentSnapshotXmlStore.Save(optionsDocument, optionsPath);
var reloadedOptionsDocument = CompositionDocumentSnapshotXmlStore.Load(optionsPath);
AssertEqual(true, reloadedOptionsDocument.Views[0].Options.ShowGrid, "view options show grid");
AssertEqual(true, reloadedOptionsDocument.Views[0].Options.AutoSizeByEnteredText, "view options auto size");
File.Delete(optionsPath);
```

- [ ] **Step 2: Run red test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: compile fails because `CompositionViewOptionsSnapshot` and `CompositionViewLayerSnapshot.Options` do not exist.

- [ ] **Step 3: Add view options record**

Create `CompositionViewOptionsSnapshot`:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionViewOptionsSnapshot(
    bool ShowGrid = false,
    bool SnapToGrid = false,
    bool ShowGridPoints = false,
    bool ShowIndicators = true,
    bool ShowMarkers = true,
    bool ShowMarkerTitles = false,
    bool ShowConceptDefinitionLabels = false,
    bool ShowRelationshipDefinitionLabels = false,
    bool ShowLinkRoleDescriptorLabels = false,
    bool ShowLinkRoleDefinitorLabels = false,
    bool ShowLinkRoleVariantLabels = false,
    bool AutoSizeByEnteredText = false);
```

- [ ] **Step 4: Wire options into view layer and XML**

Add `CompositionViewOptionsSnapshot? Options = null` before `Extensions` in `CompositionViewLayerSnapshot`, with:

```csharp
public CompositionViewOptionsSnapshot Options { get; init; } = Options ?? new CompositionViewOptionsSnapshot();
```

In `CompositionDocumentSnapshotXmlStore.WriteView`, write an `Options` element with all boolean attributes. In `ReadViews`, pass `ReadViewOptions(element.Element("Options"))`.

- [ ] **Step 5: Preserve options when applying render snapshots**

In `CompositionDocumentSnapshotEditor.ApplyView`, keep `targetView.Options` when replacing nodes and connectors:

```csharp
views[targetIndex] = targetView with
{
    Name = viewSnapshot.Title,
    Nodes = viewSnapshot.Nodes,
    Connectors = viewSnapshot.Connectors,
    Options = targetView.Options
};
```

- [ ] **Step 6: Run green test**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: all core tests pass.

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.Core\Rendering ThinkComposer.Core.Tests\Program.cs
git commit -m "Persist composition view options"
```

## Task 4: Centralize WinUI Command Execution

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Test: manual and smoke.

- [ ] **Step 1: Build command context from UI state**

Add to `MainPage.xaml.cs`:

```csharp
private CompositionCommandContext BuildCommandContext()
{
    return new CompositionCommandContext(
        HasDocument: _currentDocument is not null || _currentSnapshot is not null,
        HasSnapshot: _currentSnapshot is not null,
        SelectedNodeCount: GetSelectedNodeIds().Count,
        HasSelectedConnector: CanvasView.SelectedConnector is not null,
        HasClipboard: _clipboardSelection is { Nodes.Count: > 0 },
        CanUndo: _editingSession?.CanUndo == true,
        CanRedo: _editingSession?.CanRedo == true);
}
```

- [ ] **Step 2: Pass context into catalog**

Change `RefreshCommandCatalog` to call:

```csharp
var context = BuildCommandContext();
var sourceEntries = _currentDocument is not null && _currentSnapshot is not null
    ? CompositionCommandCatalog.ForDocument(BuildCurrentDocument(), context)
    : CompositionCommandCatalog.ForSnapshot(_currentSnapshot, context);
```

- [ ] **Step 3: Prevent silent disabled commands**

At the start of `ExecuteCommandEntry`:

```csharp
if (!entry.IsEnabled)
{
    StatusContextText.Text = string.IsNullOrWhiteSpace(entry.DisabledReason)
        ? $"{entry.Title} is not available."
        : entry.DisabledReason;
    return;
}
```

- [ ] **Step 4: Add missing accelerators**

In `MainPage.xaml`, add:

```xml
<KeyboardAccelerator Key="N" Modifiers="Control" Invoked="NewDocumentKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="O" Modifiers="Control" Invoked="OpenKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="S" Modifiers="Control" Invoked="SaveKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="S" Modifiers="Control,Shift" Invoked="SaveAsKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="P" Modifiers="Control,Shift" Invoked="CommandPaletteKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="F8" Invoked="FitToViewKeyboardAccelerator_Invoked" />
<KeyboardAccelerator Key="Delete" Invoked="DeleteKeyboardAccelerator_Invoked" />
```

Each handler calls `ExecuteCommand` with the matching `CompositionCommandIds` constant and sets `args.Handled = true`.

- [ ] **Step 5: Route command ids to existing handlers**

Add cases in `ExecuteCommand` for every new P1 command id. For commands completed by Tasks 5-9, show a specific status message that names the missing precondition or surface until the owning task replaces it with behavior. Do not leave commands silently ignored.

- [ ] **Step 6: Smoke test command palette disabled reason**

Run the app, search for `Save` before opening a document, execute it, and verify status text says `Open or create a document first.`

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.WinUI\MainPage.xaml ThinkComposer.WinUI\MainPage.xaml.cs
git commit -m "Centralize WinUI command execution"
```

## Task 5: Add Canvas Context Menus

**Files:**
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] **Step 1: Expose right-click hit context from canvas**

Create a public event in `CompositionCanvas.xaml.cs`:

```csharp
public event EventHandler<CompositionCanvasContextRequestedEventArgs>? ContextRequested;
```

Create `ThinkComposer.WinUI/Canvas/CompositionCanvasContextRequestedEventArgs.cs` or place the sealed class at the bottom of `CompositionCanvas.xaml.cs`:

```csharp
public sealed class CompositionCanvasContextRequestedEventArgs(
    Point screenPoint,
    Point worldPoint,
    string? nodeId,
    string? connectorId,
    bool hasSelection) : EventArgs
{
    public Point ScreenPoint { get; } = screenPoint;
    public Point WorldPoint { get; } = worldPoint;
    public string? NodeId { get; } = nodeId;
    public string? ConnectorId { get; } = connectorId;
    public bool HasSelection { get; } = hasSelection;
}
```

In `DrawingSurface_PointerPressed`, if right button is pressed, select the hit node or connector and raise `ContextRequested` without starting drag or pan.

- [ ] **Step 2: Subscribe from MainPage**

In `MainPage` constructor:

```csharp
CanvasView.ContextRequested += CanvasView_ContextRequested;
```

- [ ] **Step 3: Build menu from command catalog**

Add:

```csharp
private void CanvasView_ContextRequested(object? sender, CompositionCanvasContextRequestedEventArgs e)
{
    RefreshCommandCatalog();
    var commands = SelectCanvasContextCommands(e);
    var flyout = new MenuFlyout();
    foreach (var command in commands)
    {
        var item = new MenuFlyoutItem
        {
            Text = command.Title,
            Tag = command,
            IsEnabled = command.IsEnabled
        };
        item.Click += CanvasCommandMenuItem_Click;
        flyout.Items.Add(item);
    }

    flyout.ShowAt(CanvasView, new FlyoutShowOptions { Position = e.ScreenPoint });
}
```

Add `SelectCanvasContextCommands` with these command groups:

- Empty canvas: new concept, paste, select all, fit to view, view options.
- Single concept: edit name, convert type, create relationship, open composite view, create shortcut, cut, copy, paste shortcut, delete, get format, apply format, bring to front, send to back.
- Relationship: edit name, change relationship definition, change link role, delete, get format, apply format.
- Multi-selection: cut, copy, delete, align, same size, distribute, z-order, apply format.

- [ ] **Step 4: Implement context command click**

Add:

```csharp
private void CanvasCommandMenuItem_Click(object sender, RoutedEventArgs e)
{
    if (sender is MenuFlyoutItem { Tag: CompositionCommandEntry entry })
    {
        ExecuteCommandEntry(entry);
    }
}
```

- [ ] **Step 5: Implement Select All and Paste Shortcut**

Route `CompositionCommandIds.SelectAll` to existing select-all logic.

Route `CompositionCommandIds.PasteShortcut` to create a shortcut for the first clipboard node if `_currentDocument`, `_currentViewId`, and `_clipboardSelection` are present. If any are missing, set `StatusContextText.Text` to a precise disabled reason.

- [ ] **Step 6: Manual smoke test**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\smoke-winui-ui.ps1 -DocumentPath PredefinedContent\Business_Model.tdom -ExpectedTitle "Business Overview" -StartupSeconds 8
```

Expected: app opens, smoke script captures the WinUI shell, and manual right-click shows modern context menus.

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.WinUI\Canvas ThinkComposer.WinUI\MainPage.xaml.cs
git commit -m "Add canvas context menus"
```

## Task 6: Add View Menu and Canvas View Options

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`

- [ ] **Step 1: Add canvas option properties**

Add properties to `CompositionCanvas`:

```csharp
public CompositionViewOptionsSnapshot ViewOptions { get; private set; } = new();

public void ApplyViewOptions(CompositionViewOptionsSnapshot options)
{
    ViewOptions = options ?? new CompositionViewOptionsSnapshot();
    DrawingSurface.Invalidate();
}
```

Use `ViewOptions.ShowGrid` and `ViewOptions.ShowGridPoints` during rendering. Use `ViewOptions.SnapToGrid` when moving nodes by rounding final positions to the nearest 10 world units.

- [ ] **Step 2: Add view menu button**

In `MainPage.xaml`, add a compact button in the status bar near zoom/status:

```xml
<Button
    x:Name="ViewOptionsButton"
    Grid.Column="3"
    Click="ViewOptionsButton_Click"
    Style="{StaticResource ChromeButtonStyle}"
    ToolTipService.ToolTip="View options">
    <SymbolIcon Symbol="View" />
</Button>
```

Adjust status grid columns so the button has an explicit column.

- [ ] **Step 3: Build view menu flyout**

In `ViewOptionsButton_Click`, create a `MenuFlyout` with:

- Actual size
- Zoom in
- Zoom out
- Fit to view
- Presentation mode
- Full-screen mode
- Show grid
- Snap to grid
- Grid points
- Show indicators
- Show markers
- Show marker titles
- Show concept definition labels
- Show relationship definition labels
- Show link-role descriptor labels
- Show link-role definitor labels
- Show link-role variant labels
- Auto-size by entered text

Toggle items use `ToggleMenuFlyoutItem` and update the current view options.

- [ ] **Step 4: Persist option toggles**

Add:

```csharp
private void UpdateCurrentViewOptions(Func<CompositionViewOptionsSnapshot, CompositionViewOptionsSnapshot> update)
```

This method finds the current view in `_currentDocument.Views`, replaces its `Options`, calls `CanvasView.ApplyViewOptions(nextOptions)`, marks the document dirty, refreshes commands, and updates status.

- [ ] **Step 5: Route view command ids**

In `ExecuteCommand`, wire:

- `ActualSize`, `ZoomIn`, `ZoomOut`, `FitToView`
- all view option toggles
- `PresentationMode`, `FullScreen`

`FitToView` calls `CanvasView.FitSnapshotToViewport()`. Presentation and full-screen set status if the app cannot enter the mode yet.

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
```

Expected: all core tests pass.

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.WinUI\MainPage.xaml ThinkComposer.WinUI\MainPage.xaml.cs ThinkComposer.WinUI\Canvas\CompositionCanvas.xaml.cs
git commit -m "Add view options menu"
```

## Task 7: Wire Layout and Format Commands in WinUI

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Test: existing core tests plus manual context menu.

- [ ] **Step 1: Add copied format state**

Add field:

```csharp
private CompositionStyleSnapshot? _copiedFormat;
```

- [ ] **Step 2: Implement format commands**

`GetFormat` reads selected node or connector style into `_copiedFormat`.

`ApplyFormat` applies `_copiedFormat` to selected nodes and the selected connector. Use `CompositionDocumentSnapshotEditor.SetIdeaStyle` and `SetRelationshipStyle` when `_currentDocument` is present; otherwise update `_currentSnapshot` directly through `CompositionSnapshotEditor`.

- [ ] **Step 3: Implement layout command routing**

In `ExecuteCommand`, route:

```csharp
case CompositionCommandIds.AlignLeft:
    ApplySelectionAlignment(CompositionSelectionAlignment.Left);
    break;
case CompositionCommandIds.AlignTop:
    ApplySelectionAlignment(CompositionSelectionAlignment.Top);
    break;
case CompositionCommandIds.SameSize:
    ApplySelectionResize(CompositionSelectionSizeMode.SameSize);
    break;
case CompositionCommandIds.DistributeHorizontally:
    ApplySelectionDistribution(CompositionSelectionDistribution.Horizontal);
    break;
case CompositionCommandIds.BringToFront:
    ApplySelectionZOrder(CompositionSelectionZOrder.BringToFront);
    break;
```

Add cases for every layout id from Task 1.

- [ ] **Step 4: Add layout helper methods**

Each helper gets selected node ids, validates the minimum selection count, calls the matching `CompositionSnapshotSelectionEditor` method, calls `ApplyEditedSnapshot`, reselects nodes, and writes status text.

- [ ] **Step 5: Manual smoke test**

Open `PredefinedContent/Business_Model.tdom`, select multiple concepts, right-click, run align left, same size, distribute horizontally, bring to front, undo, redo.

Expected: layout changes apply as one undo operation per command.

- [ ] **Step 6: Commit**

```powershell
git add ThinkComposer.WinUI\MainPage.xaml.cs
git commit -m "Wire layout and format commands"
```

## Task 8: Expand Inspector Sections

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] **Step 1: Add Shape expander**

Add a `Shape` expander below `Style` with:

- Geometry combo: Rectangle, Rounded rectangle, Ellipse, Hexagon.
- Multiple symbol toggle.
- Flip horizontal toggle.
- Flip vertical toggle.
- Tilt number box.

Store values in `CompositionStyleSnapshot.Properties` keys:

```text
shape.geometry
shape.multipleSymbol
shape.flipHorizontal
shape.flipVertical
shape.tilt
```

- [ ] **Step 2: Add Text expander**

Add a `Text` expander with:

- Font family text box.
- Font size number box.
- Bold toggle.
- Italic toggle.
- Alignment combo.

Store values in `CompositionStyleSnapshot.Properties` keys:

```text
text.fontFamily
text.fontSize
text.bold
text.italic
text.alignment
```

- [ ] **Step 3: Add Generation preview expander**

Expose selected generation template key, template text, and a read-only preview for the selected concept or relationship. Use existing `CompositionDocumentFileGenerator` inputs and write status when no template is selected.

- [ ] **Step 4: Add Document properties expander**

Expose document title, domain name, and domain summary. Apply changes to `_currentDocument` and update `_currentSnapshot.Title` through `ApplyEditedSnapshot` when title changes.

- [ ] **Step 5: Apply inspector values**

Create helper:

```csharp
private CompositionStyleSnapshot WithStyleProperty(
    CompositionStyleSnapshot style,
    string key,
    string value)
```

It creates a new dictionary from `style.Properties`, sets or removes the key, and returns `style with { Properties = properties }`.

- [ ] **Step 6: Smoke test inspector**

Change fill/stroke/text plus one shape and one text property. Save as `.tcdoc`, reopen, and verify the inspector values persist.

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.WinUI\MainPage.xaml ThinkComposer.WinUI\MainPage.xaml.cs
git commit -m "Expand inspector editing sections"
```

## Task 9: Add Domain Studio

**Files:**
- Create: `ThinkComposer.WinUI/Domain/DomainStudioView.xaml`
- Create: `ThinkComposer.WinUI/Domain/DomainStudioView.xaml.cs`
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] **Step 1: Create Domain Studio user control**

Create a two-column WinUI control:

- Left list: Concept definitions, Relationship definitions, Link-role definitions, Marker definitions, Table definitions, External languages, Templates.
- Right editor: id, name, kind, summary, style fields, table records for table definitions, template value for templates.

- [ ] **Step 2: Define public API**

In `DomainStudioView.xaml.cs`, expose:

```csharp
public CompositionDocumentSnapshot? Document { get; private set; }
public event EventHandler<CompositionDocumentSnapshot>? DocumentChanged;

public void LoadDocument(CompositionDocumentSnapshot? document)
```

All save/delete operations must produce a new `CompositionDocumentSnapshot` through `CompositionDocumentSnapshotEditor` and raise `DocumentChanged`.

- [ ] **Step 3: Host Domain Studio in MainPage**

Add a center overlay or replaceable center panel in `MainPage.xaml` that can show `DomainStudioView` while the canvas is hidden. Add command routing for `CompositionCommandIds.DomainStudio`.

- [ ] **Step 4: Wire explorer and command palette entry points**

Explorer Domain tab gets a button named `Open Domain Studio`. Command palette finds `Domain Studio`. Inspector document/domain section gets a button to open the studio.

- [ ] **Step 5: Preserve existing Domain tab behavior**

Keep the current Explorer Domain tree. Domain Studio is a richer editing surface, not a replacement for navigation.

- [ ] **Step 6: Manual smoke test**

Open `PredefinedContent/Business_Model.tdom`, open Domain Studio, edit one marker definition name, save as `.tcdoc`, reopen, and verify the edited definition appears in command search and inspector.

- [ ] **Step 7: Commit**

```powershell
git add ThinkComposer.WinUI\Domain ThinkComposer.WinUI\MainPage.xaml ThinkComposer.WinUI\MainPage.xaml.cs
git commit -m "Add Domain Studio surface"
```

## Task 10: Update Smoke Coverage and Parity Docs

**Files:**
- Modify: `scripts/smoke-winui-ui.ps1`
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

- [ ] **Step 1: Extend smoke script**

Add optional checks for:

- Command palette visible after `Ctrl+Shift+P`.
- View menu button visible.
- Context menu opens on right-click over canvas.
- Domain Studio button or title visible after command execution.

- [ ] **Step 2: Run full verification**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
dotnet run --project ThinkComposer.LegacyBridge.Tests\ThinkComposer.LegacyBridge.Tests.csproj
powershell -ExecutionPolicy Bypass -File scripts\check-winui-migration.ps1
powershell -ExecutionPolicy Bypass -File scripts\smoke-winui-ui.ps1 -DocumentPath PredefinedContent\Business_Model.tdom -ExpectedTitle "Business Overview" -StartupSeconds 8
```

Expected: all commands exit with code 0.

- [ ] **Step 3: Update parity audit**

Mark P1 command surfaces as implemented only when:

- command palette can find the command,
- one modern UI surface can execute it,
- disabled state has a visible reason,
- undo/redo behavior is correct for mutating snapshot commands.

- [ ] **Step 4: Release build check**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\release-winui.ps1 -Configuration Release -RuntimeIdentifiers win-x64 -OutputRoot artifacts\winui-internal -UpdateChannel internal
```

Expected: release artifact is produced under `artifacts/winui-internal`.

- [ ] **Step 5: Commit**

```powershell
git add scripts\smoke-winui-ui.ps1 docs\superpowers\specs\2026-05-08-winui-parity-audit.md
git commit -m "Update command parity verification"
```

## Final Acceptance Checklist

- [ ] Every P1 command from `2026-05-09-command-ux-parity-design.md` appears in command palette search.
- [ ] Frequent global commands remain in the compact top bar.
- [ ] Empty canvas, single concept, relationship, and multi-selection context menus work.
- [ ] View toggles live in the view menu and command palette.
- [ ] Domain administration lives in Domain Studio.
- [ ] Disabled commands show a reason in status text.
- [ ] Selection layout commands support undo and redo as one operation each.
- [ ] View options persist in `.tcdoc`.
- [ ] Core tests pass.
- [ ] Legacy bridge tests pass.
- [ ] WinUI migration check passes.
- [ ] WinUI smoke test passes.
- [ ] Release build succeeds.
