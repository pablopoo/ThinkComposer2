# Modern Document Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a versioned modern ThinkComposer document contract that can preserve full legacy data and still feed the current WinUI canvas.

**Architecture:** Add full-fidelity DTO records and XML persistence to `ThinkComposer.Core`. Keep current `CompositionViewSnapshot` as the render projection used by WinUI. Use extension buckets to preserve legacy data that does not have first-class UI yet.

**Tech Stack:** C# records, `System.Xml.Linq`, existing `ThinkComposer.Core.Tests` console test harness, `dotnet run`, `dotnet build`.

---

## Task 1: Core DTO Contract

**Files:**
- Test: `ThinkComposer.Core.Tests/Program.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDomainSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDefinitionSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionIdeaSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionRelationshipSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionViewLayerSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDetailSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionStyleSnapshot.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionExtensionSnapshot.cs`

- [x] **Step 1: Write failing DTO usage test**

Add a test that constructs a document with domain metadata, definitions, details, style, markers, complements, templates, and extensions.

- [x] **Step 2: Run red test**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: compile fails because the new DTO types do not exist.

- [x] **Step 3: Add DTO records**

Implement records with immutable list properties and default empty lists.

- [x] **Step 4: Run green test**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: pass.

- [x] **Step 5: Commit**

Commit message: `Add modern document DTO contract`.

## Task 2: XML Roundtrip

**Files:**
- Test: `ThinkComposer.Core.Tests/Program.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotXmlStore.cs`

- [x] **Step 1: Write failing roundtrip test**

Save and load a full document and assert schema version, domain definitions, details, styles, templates, complements, and extensions survive.

- [x] **Step 2: Run red test**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: compile fails because `CompositionDocumentSnapshotXmlStore` does not exist.

- [x] **Step 3: Implement XML store**

Use `XDocument` and explicit element names. Do not use binary serialization.

- [x] **Step 4: Run green test**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: pass.

- [ ] **Step 5: Commit**

Commit message: `Add modern document XML store`.

## Task 3: View Snapshot Projection

**Files:**
- Test: `ThinkComposer.Core.Tests/Program.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotAdapter.cs`

- [ ] **Step 1: Write failing projection tests**

Convert `CompositionViewSnapshot` to `CompositionDocumentSnapshot` and project back to `CompositionViewSnapshot`.

- [ ] **Step 2: Run red test**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: compile fails because `CompositionDocumentSnapshotAdapter` does not exist.

- [ ] **Step 3: Implement adapter**

Map nodes to ideas, connectors to relationships, and visual nodes/connectors to the default view layer.

- [ ] **Step 4: Run green test and solution build**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

Expected: pass.

- [ ] **Step 5: Commit**

Commit message: `Project modern documents to WinUI snapshots`.
