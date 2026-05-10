# PDF Fidelity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve SkiaSharp PDF output so exported views and reports include wrapped labels, relationship arrows, complements, richer report data, and basic page furniture.

**Architecture:** Keep all rendering in `ThinkComposer.Core.Rendering`. Add small PDF drawing helpers for wrapped text, arrows, page chrome, and complement rendering; WinUI keeps using the existing export calls.

**Tech Stack:** C# multi-targeted `net48;net10.0-windows`, SkiaSharp PDF, existing console test harness.

---

## File Map

- Modify `ThinkComposer.Core.Tests/Program.cs`: add RED assertions for complement view export and richer report export.
- Modify `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`: add complement-aware overload, wrapped node text, arrowheads, and complement drawing.
- Modify `ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs`: pass view complements into diagram pages, add page headers/footers, and add details/table report sections.
- Modify `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`: update output gap wording after implementation.

## Task 1: RED Tests For PDF Fidelity

**Files:**
- Modify: `ThinkComposer.Core.Tests/Program.cs`

- [ ] **Step 1: Add view-complement and rich-report assertions after existing PDF assertions**

Add this after `AssertTrue(ContainsAscii(exportedViewPdf, "/Type /Page"), "view pdf page marker");`:

```csharp
var complementedViewPdf = CompositionSnapshotPdfExporter.Export(
    snapshot,
    [
        new CompositionExtensionSnapshot(
            "legend.status",
            "Legend\nCritical flow",
            new Dictionary<string, string>
            {
                ["kind"] = "legend",
                ["title"] = "Status Legend",
                ["x"] = "420",
                ["y"] = "30",
                ["width"] = "220",
                ["height"] = "110"
            })
    ]);
AssertTrue(complementedViewPdf.Length > exportedViewPdf.Length + 256, "view pdf complement content");
```

Add this after `AssertTrue(ContainsAscii(modernReportPdf, "/Type /Page"), "modern report pdf page marker");`:

```csharp
var richReportDocument = modernDocument with
{
    Domain = modernDocument.Domain with
    {
        TableDefinitions =
        [
            modernDocument.Domain.TableDefinitions[0] with
            {
                TableRecords = new CompositionDetailTableSnapshot(
                    ["Task", "Owner", "Status"],
                    [
                        ["Review PDF output", "Pablo", "Open"],
                        ["Validate table rendering", "Team", "Ready"]
                    ])
            }
        ]
    },
    Ideas =
    [
        modernDocument.Ideas[0] with
        {
            Summary = "Long summary used to verify wrapped report text in generated PDF output.",
            Details =
            [
                modernDocument.Ideas[0].Details[0],
                CompositionDetailFactory.CreateTable(
                    "Checklist",
                    ["Task", "State"],
                    [
                        ["Review diagram labels", "Done"],
                        ["Review complement rendering", "Open"]
                    ])
            ]
        }
    ],
    Views =
    [
        modernDocument.Views[0] with
        {
            Complements =
            [
                new CompositionExtensionSnapshot(
                    "quote.review",
                    "PDF report should include complement cards and readable summaries.",
                    new Dictionary<string, string>
                    {
                        ["kind"] = "quote",
                        ["title"] = "Review note",
                        ["x"] = "220",
                        ["y"] = "130",
                        ["width"] = "260",
                        ["height"] = "96"
                    })
            ]
        },
        modernDocument.Views[1]
    ]
};
var richReportPdf = CompositionDocumentReportPdfExporter.Export(richReportDocument);
AssertTrue(richReportPdf.Length > modernReportPdf.Length + 2048, "rich report pdf includes extra sections");
AssertTrue(CountAscii(richReportPdf, "/Type /Page") >= CountAscii(modernReportPdf, "/Type /Page") + 1, "rich report pdf extra page");
```

Add helper near `ContainsAscii`:

```csharp
static int CountAscii(byte[] content, string text)
{
    var ascii = System.Text.Encoding.ASCII.GetString(content);
    var count = 0;
    var index = 0;
    while ((index = ascii.IndexOf(text, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += text.Length;
    }

    return count;
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: compile failure because the complement-aware `CompositionSnapshotPdfExporter.Export` overload does not exist, or assertion failure on richer report size/pages before implementation.

## Task 2: View PDF Fidelity

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`

- [ ] **Step 1: Add complement-aware overload**

Keep the existing API and add:

```csharp
public static byte[] Export(
    CompositionViewSnapshot snapshot,
    IReadOnlyList<CompositionExtensionSnapshot> complements,
    CompositionPdfExportOptions? options = null)
{
    return ExportInternal(snapshot, complements, options);
}
```

Make existing `Export(snapshot, options)` delegate to `ExportInternal(snapshot, Array.Empty<CompositionExtensionSnapshot>(), options)`.

- [ ] **Step 2: Include complements in bounds and rendering**

Use `CompositionViewComplementLayout.Build(complements, snapshot.Nodes)`; include item rectangles in `GetBounds`, draw group regions before connectors/nodes, and draw cards after nodes.

- [ ] **Step 3: Improve diagram readability**

Add wrapped text drawing inside nodes and complement cards. Add arrowhead polygons at relationship endpoints. Keep color parsing and style fallbacks deterministic.

- [ ] **Step 4: Run tests**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: view complement assertion passes; report assertion may still fail until Task 3.

## Task 3: Report PDF Fidelity

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs`

- [ ] **Step 1: Pass view complements into diagram rendering**

In `DrawViewPages`, render each view with:

```csharp
CompositionSnapshotPdfExporter.RenderSnapshot(canvas, snapshot, viewOptions, view.Complements, clearBackground: false);
```

- [ ] **Step 2: Add page headers and footers**

Track an incrementing page number in `Export`. Draw document title in the header, section name in the header, and `Page N` in the footer for cover, view, and data pages.

- [ ] **Step 3: Add rich data pages**

Add report pages for:

- idea summaries with definition, markers, details, and table detail names
- relationship summaries with source, target, definition, link-role, markers, and details
- table definitions with base table columns and rows

- [ ] **Step 4: Run tests**

Run: `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`

Expected: all core PDF tests pass, including richer report size/page assertions.

## Task 4: Docs, Compatibility, And Verification

**Files:**
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

- [ ] **Step 1: Update audit wording**

Change Output Parity to mention improved PDF diagram/report fidelity and complements in the native PDF path.

- [ ] **Step 2: Verify full regression**

Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
dotnet run --project ThinkComposer.LegacyBridge.Tests\ThinkComposer.LegacyBridge.Tests.csproj
dotnet build ThinkComposer.WinUI\ThinkComposer.WinUI.csproj -p:Platform=x64
powershell -ExecutionPolicy Bypass -File scripts\check-winui-migration.ps1
```

Expected: all commands exit 0.

- [ ] **Step 3: Commit and push**

```powershell
git add ThinkComposer.Core.Tests\Program.cs ThinkComposer.Core\Rendering\CompositionSnapshotPdfExporter.cs ThinkComposer.Core\Rendering\CompositionDocumentReportPdfExporter.cs docs\superpowers\specs\2026-05-08-winui-parity-audit.md docs\superpowers\plans\2026-05-10-pdf-fidelity.md
git commit -m "Improve native PDF fidelity"
git push origin thinkcomposer2
```

Expected: branch is clean and `origin/thinkcomposer2` receives the spec, plan, and implementation commits.
