# UI Modernization Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Build the first WPF implementation of the approved B2 shell: VS Code-inspired light/dark UI, central canvas, activity rail, collapsible explorer, inspector, and bottom panel.

**Architecture:** Keep existing shell content contracts intact by preserving the current container names. Replace only the top-level WPF layout and add code-behind helpers that change `ColumnDefinition` and `RowDefinition` sizes for collapse states. Theme values live in one app-level resource dictionary so the visual direction can evolve without touching editor logic.

**Tech Stack:** WPF XAML, C# code-behind, existing ThinkComposer shell interfaces, `dotnet build`, net10 smoke test.

---

### Task 1: Add Modern Shell Resources

**Files:**
- Create: `ThinkComposer/ApplicationShell/ModernShellResources.xaml`
- Modify: `ThinkComposer/App.xaml`
- Modify: `ThinkComposer/ThinkComposer.csproj`

- [x] **Step 1: Add resource dictionary**

Create `ModernShellResources.xaml` with shell brushes and reusable button styles:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="TcShellTitleBrush" Color="#FFF8F8F8" />
    <SolidColorBrush x:Key="TcShellCommandBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="TcShellRailBrush" Color="#FFF3F3F3" />
    <SolidColorBrush x:Key="TcShellPanelBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="TcShellCanvasBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="TcShellBottomBrush" Color="#FFFAFAFA" />
    <SolidColorBrush x:Key="TcShellBorderBrush" Color="#FFE5E5E5" />
    <SolidColorBrush x:Key="TcShellMutedTextBrush" Color="#FF57606A" />
    <SolidColorBrush x:Key="TcShellTextBrush" Color="#FF24292F" />
    <SolidColorBrush x:Key="TcShellAccentBrush" Color="#FF007ACC" />
    <SolidColorBrush x:Key="TcShellAccentTextBrush" Color="#FFFFFFFF" />
</ResourceDictionary>
```

- [x] **Step 2: Merge resources**

Add this merged dictionary in `App.xaml` after `Common/Themes/Generic.xaml`:

```xml
<ResourceDictionary Source="ApplicationShell/ModernShellResources.xaml"/>
```

- [x] **Step 3: Include page in project**

Add this near the other `ApplicationShell` pages in `ThinkComposer.csproj`:

```xml
<Page Include="ApplicationShell\ModernShellResources.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
```

- [x] **Step 4: Build**

Run: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86`

Expected: build succeeds.

### Task 2: Rework Header Into Title Bar + Command Bar

**Files:**
- Modify: `ThinkComposer/ApplicationShell/MainWindowHeader.xaml`

- [x] **Step 1: Replace header layout**

Replace the old header XAML with a two-row title/command shell. Preserve `PaletteSupraContainer` and `QuickToolPanel` names:

```xml
<UserControl ...>
  <Grid Background="{DynamicResource TcShellTitleBrush}">
    <Grid.RowDefinitions>
      <RowDefinition Height="35"/>
      <RowDefinition Height="42"/>
    </Grid.RowDefinitions>
    <!-- title row with logo, title, document text, QuickToolPanel -->
    <!-- command row with PaletteSupraContainer and search TextBox -->
  </Grid>
</UserControl>
```

- [x] **Step 2: Build**

Run: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86`

Expected: build succeeds.

### Task 3: Rework Main Shell Layout

**Files:**
- Modify: `ThinkComposer/ApplicationShell/MainWindow.xaml`

- [x] **Step 1: Replace workspace grid**

Create a row-based root with `WinHeader` on top and `WorkingAreaBorder` below. Inside `WorkingAreaBorder`, create columns for activity rail, explorer, canvas, and inspector, plus rows for main workspace, bottom panel, and status bar.

Preserve these container names exactly:

```xml
NavigationTopContainer
NavigationBottomContainer
DocumentContainer
MessagingContainer
EditingTopContainer
EditingMediumUpperContainer
EditingMediumLowerContainer
EditingBottomContainer
StatusContainer
```

- [x] **Step 2: Add collapse buttons**

Add rail buttons and local panel buttons wired to:

```xml
Click="ToggleExplorerPanel_Click"
Click="ToggleInspectorPanel_Click"
Click="ToggleBottomPanel_Click"
Click="ToggleFocusMode_Click"
```

- [x] **Step 3: Build**

Run: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86`

Expected: build fails only because click handlers do not exist yet.

### Task 4: Add Collapse Behavior

**Files:**
- Modify: `ThinkComposer/ApplicationShell/MainWindow.xaml.cs`

- [x] **Step 1: Add constants and fields**

Add preferred sizes and state fields:

```csharp
private const double ExplorerPanelWidth = 240.0;
private const double InspectorPanelWidth = 250.0;
private const double BottomPanelHeight = 180.0;

private bool IsExplorerPanelVisible = true;
private bool IsInspectorPanelVisible = true;
private bool IsBottomPanelVisible = true;
```

- [x] **Step 2: Add helper methods**

Add:

```csharp
private void ApplyShellPanelState()
{
    this.ExplorerColumn.Width = new GridLength(IsExplorerPanelVisible ? ExplorerPanelWidth : 0.0);
    this.InspectorColumn.Width = new GridLength(IsInspectorPanelVisible ? InspectorPanelWidth : 0.0);
    this.BottomPanelRow.Height = new GridLength(IsBottomPanelVisible ? BottomPanelHeight : 0.0);
}
```

- [x] **Step 3: Add click handlers**

Add handlers that flip booleans and call `ApplyShellPanelState()`. `ToggleFocusMode_Click` sets all three booleans to `false`.

- [x] **Step 4: Build**

Run: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86`

Expected: build succeeds.

### Task 5: Smoke Test

**Files:**
- No file changes.

- [x] **Step 1: Run net10 smoke test**

Launch `ThinkComposer\bin\Debug\net10.0-windows\Instrumind.ThinkComposer.exe`, invoke `Toggle Explorer`, `Toggle Inspector`, `Toggle Messages`, `Focus Canvas`, and `Toggle Theme` through UI Automation, then close with `WM_CLOSE`.

Expected: process exits with code 0.

- [x] **Step 2: Commit**

Run:

```bash
git add ThinkComposer/ApplicationShell/ModernShellResources.xaml ThinkComposer/App.xaml ThinkComposer/ThinkComposer.csproj ThinkComposer/ApplicationShell/MainWindow.xaml ThinkComposer/ApplicationShell/MainWindowHeader.xaml ThinkComposer/ApplicationShell/MainWindow.xaml.cs docs/superpowers/plans/2026-05-07-ui-modernization-shell.md
git commit -m "Implement modern collapsible WPF shell"
```
