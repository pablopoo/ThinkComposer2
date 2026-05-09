# Navigation Search P1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement parent navigation and conservative find/replace for editable document text.

**Architecture:** Core owns immutable document traversal, search, and replace. WinUI exposes replace controls in the existing Search panel and applies returned document snapshots.

**Tech Stack:** C#/.NET 10 WinUI 3, existing `ThinkComposer.Core.Rendering` DTOs, console tests in `ThinkComposer.Core.Tests`.

---

## File Structure

- Create: `ThinkComposer.Core/Rendering/CompositionDocumentNavigationTarget.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentNavigator.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextSearchResult.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextSearch.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextReplacer.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`
- Modify: `ThinkComposer.Core.Tests/Program.cs`
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

---

## Task 1: Red Tests

**Files:**
- Modify: `ThinkComposer.Core.Tests/Program.cs`

- [ ] Add tests after `documentCommands` assertions:

```csharp
var childParentTarget = CompositionDocumentNavigator.FindParentTarget(modernDocument, "view-child");
AssertTrue(childParentTarget is not null, "parent navigation target");
AssertEqual("view-1", childParentTarget!.ViewId, "parent navigation view");
AssertEqual("idea-1", childParentTarget.SelectedIdeaId, "parent navigation selected idea");
AssertTrue(CompositionDocumentNavigator.FindParentTarget(modernDocument, "view-1") is null, "root has no parent");

var textResults = CompositionDocumentTextSearch.Search(generationDocument, "Customer").ToArray();
AssertTrue(textResults.Any(result => result.Kind == CompositionDocumentTextSearchResultKind.Idea && result.TargetId == "idea-1"), "search idea");
AssertTrue(textResults.Any(result => result.Kind == CompositionDocumentTextSearchResultKind.Relationship && result.TargetId == "rel-1"), "search relationship");
AssertTrue(textResults.Any(result => result.Kind == CompositionDocumentTextSearchResultKind.View && result.TargetId == "view-child"), "search view");
AssertTrue(textResults.Any(result => result.Kind == CompositionDocumentTextSearchResultKind.Detail && result.TargetId == "detail-1"), "search detail");
AssertTrue(textResults.Any(result => result.Kind == CompositionDocumentTextSearchResultKind.Definition && result.TargetId == "concept-def"), "search definition");
AssertTrue(textResults.All(result => result.IsReplaceable), "document search results replaceable");

var renamedIdeaDocument = CompositionDocumentTextReplacer.ReplaceSelected(generationDocument, textResults.First(result => result.TargetId == "idea-1"), "Client");
AssertEqual("Client Need", renamedIdeaDocument.Ideas.Single(idea => idea.Id == "idea-1").Name, "replace selected idea");

var replaceAllResult = CompositionDocumentTextReplacer.ReplaceAll(generationDocument, "Customer", "Client");
AssertTrue(replaceAllResult.ReplacementCount >= 3, "replace all count");
AssertEqual("Client Need", replaceAllResult.Document.Ideas.Single(idea => idea.Id == "idea-1").Name, "replace all idea");
AssertEqual("Client Need Detail", replaceAllResult.Document.Views.Single(view => view.Id == "view-child").Name, "replace all view");
AssertTrue(replaceAllResult.Document.Ideas[0].Details[0].Value.Contains("spec.pdf", StringComparison.Ordinal), "replace all preserves unrelated detail");
```

- [ ] Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: compile fails because new core types do not exist.

---

## Task 2: Core Navigator

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentNavigationTarget.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentNavigator.cs`

- [ ] Create target record:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionDocumentNavigationTarget(string ViewId, string? SelectedIdeaId);
```

- [ ] Create navigator:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentNavigator
{
    public static CompositionDocumentNavigationTarget? FindParentTarget(CompositionDocumentSnapshot document, string? currentViewId)
    {
        if (document is null || string.IsNullOrWhiteSpace(currentViewId))
        {
            return null;
        }

        var currentView = document.Views.FirstOrDefault(view => string.Equals(view.Id, currentViewId, StringComparison.Ordinal));
        if (currentView is null || string.IsNullOrWhiteSpace(currentView.ContainerIdeaId))
        {
            return null;
        }

        var containerIdea = document.Ideas.FirstOrDefault(idea => string.Equals(idea.Id, currentView.ContainerIdeaId, StringComparison.Ordinal));
        if (containerIdea is null)
        {
            return null;
        }

        var parentView = document.Views.FirstOrDefault(view =>
            view.Nodes.Any(node => string.Equals(node.Id, containerIdea.Id, StringComparison.Ordinal)));
        return parentView is null ? null : new CompositionDocumentNavigationTarget(parentView.Id, containerIdea.Id);
    }
}
```

- [ ] Run core tests. Expected: search/replacer types still missing.

---

## Task 3: Core Search

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextSearchResult.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextSearch.cs`

- [ ] Create result records and enum:

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public enum CompositionDocumentTextSearchResultKind
{
    Idea,
    Relationship,
    Detail,
    View,
    Definition,
    Template,
    Complement
}

public sealed record CompositionDocumentTextSearchResult(
    CompositionDocumentTextSearchResultKind Kind,
    string TargetId,
    string FieldPath,
    string Title,
    string Preview,
    bool IsReplaceable = true);
```

- [ ] Create search service that scans ideas, relationships, details, view names, definitions, templates, and complements with case-insensitive substring matching.

- [ ] Run core tests. Expected: replacer types still missing.

---

## Task 4: Core Replacer

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentTextReplacer.cs`

- [ ] Add result record:

```csharp
public sealed record CompositionDocumentTextReplaceAllResult(CompositionDocumentSnapshot Document, int ReplacementCount);
```

- [ ] Implement:

```csharp
public static CompositionDocumentSnapshot ReplaceSelected(
    CompositionDocumentSnapshot document,
    CompositionDocumentTextSearchResult result,
    string searchText,
    string replacementText)
```

and overload:

```csharp
public static CompositionDocumentSnapshot ReplaceSelected(
    CompositionDocumentSnapshot document,
    CompositionDocumentTextSearchResult result,
    string replacementText)
```

The overload uses the matched preview/title value to replace the first occurrence represented by `FieldPath`.

- [ ] Implement `ReplaceAll(CompositionDocumentSnapshot document, string searchText, string replacementText)`.

- [ ] Run core tests. Expected: all pass.

---

## Task 5: Command Catalog Parent State

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`

- [ ] Disable `Go to Parent` unless context has a document and current view can have a parent. If context lacks view-id data, keep it enabled when a document exists and enforce no-op in UI.

- [ ] Run core tests.

Expected: all pass.

---

## Task 6: WinUI Search Panel

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] Add `ReplaceBox`, `ReplaceSelectedButton`, and `ReplaceAllButton` below `BottomSearchBox`.

- [ ] Replace `RefreshSearchResults` so it combines:

```csharp
CompositionCommandCatalog.Search(_commandEntries, BottomSearchBox.Text, limit: 25)
CompositionDocumentTextSearch.Search(BuildCurrentDocument(), BottomSearchBox.Text, limit: 50)
```

Mapping document results to `CompositionCommandEntry` should use `Id = $"search:{kind}:{targetId}:{fieldPath}"`, `Title`, `Subtitle = Preview`, `Kind`, `TargetId`, and store original search result in a private dictionary keyed by id.

- [ ] `SearchResultsList_SelectionChanged` executes command entries or document search entries.

- [ ] `Replace selected` updates the selected stored result.

- [ ] `Replace all` calls core replacer with current search/replace text.

- [ ] Apply document updates through existing snapshot/document refresh helpers and mark dirty.

---

## Task 7: WinUI Parent Navigation

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] Replace `GoParent` placeholder with:

```csharp
GoToParentView();
```

- [ ] Implement `GoToParentView()` using `CompositionDocumentNavigator.FindParentTarget(BuildCurrentDocument(), _currentViewId)`.

- [ ] If target exists, open target view and select `SelectedIdeaId`.

- [ ] If no target exists, set status `No parent view`.

---

## Task 8: Docs And Verification

**Files:**
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

- [ ] Update P1 navigation/search gap.
- [ ] Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
dotnet run --project ThinkComposer.LegacyBridge.Tests\ThinkComposer.LegacyBridge.Tests.csproj
dotnet build ThinkComposer.WinUI\ThinkComposer.WinUI.csproj -p:Platform=x64
powershell -ExecutionPolicy Bypass -File scripts\check-winui-migration.ps1
```

- [ ] Commit:

```powershell
git add ThinkComposer.Core ThinkComposer.Core.Tests ThinkComposer.WinUI docs/superpowers/plans/2026-05-09-navigation-search-p1.md docs/superpowers/specs/2026-05-08-winui-parity-audit.md
git commit -m "Add navigation and search replace P1"
```
