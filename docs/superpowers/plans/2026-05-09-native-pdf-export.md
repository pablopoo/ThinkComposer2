# Native PDF Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add native PDF export to the WinUI migration and remove XPS as a new-app output target.

**Architecture:** PDF generation lives in `ThinkComposer.Core.Rendering` and accepts existing snapshot DTOs. WinUI only handles file pickers and writes returned PDF bytes to disk.

**Tech Stack:** C#/.NET 10 + .NET Framework 4.8 multi-targeted core library, SkiaSharp PDF backend, existing console test project.

---

## File Structure

- Modify: `ThinkComposer.Core/ThinkComposer.Core.csproj`
  - Add `SkiaSharp` package reference.
- Create: `ThinkComposer.Core/Rendering/CompositionPdfExportOptions.cs`
  - Page size, margins, and default fonts for PDF exporters.
- Create: `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`
  - Render one `CompositionViewSnapshot` to a one-page PDF.
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs`
  - Render `CompositionDocumentSnapshot` to a multi-page PDF report.
- Modify: `ThinkComposer.Core.Tests/Program.cs`
  - Add red tests for view PDF, report PDF, and workflow file output.
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandIds.cs`
  - Add `ExportPdf` and `ReportPdf`.
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`
  - Add palette/top-bar metadata for PDF commands.
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
  - Update export/report button tooltips to include PDF.
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`
  - Add `.pdf` picker choices, call core PDF exporters, write bytes.
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`
  - Mark native PDF as implemented and XPS intentionally dropped from WinUI.
- Modify: `README.md`
  - Mention native PDF export instead of printable HTML as the primary PDF path.

---

## Task 1: Red Tests For Core PDF Export

**Files:**
- Modify: `ThinkComposer.Core.Tests/Program.cs`

- [ ] **Step 1: Add failing PDF assertions after the existing HTML/SVG export assertions**

```csharp
var exportedViewPdf = CompositionSnapshotPdfExporter.Export(snapshot);
AssertTrue(exportedViewPdf.Length > 1024, "view pdf non-trivial size");
AssertTrue(StartsWithPdfHeader(exportedViewPdf), "view pdf header");
AssertTrue(ContainsAscii(exportedViewPdf, "/Type /Page"), "view pdf page marker");
```

- [ ] **Step 2: Add failing report assertions after the existing modern report HTML assertions**

```csharp
var modernReportPdf = CompositionDocumentReportPdfExporter.Export(modernDocument);
AssertTrue(modernReportPdf.Length > 2048, "modern report pdf non-trivial size");
AssertTrue(StartsWithPdfHeader(modernReportPdf), "modern report pdf header");
AssertTrue(ContainsAscii(modernReportPdf, "/Type /Page"), "modern report pdf page marker");
```

- [ ] **Step 3: Add failing workflow file assertion inside `workflowOutputPath`**

```csharp
var reportPdfOutputPath = Path.Combine(workflowOutputPath, "report.pdf");
File.WriteAllBytes(reportPdfOutputPath, CompositionDocumentReportPdfExporter.Export(generationDocument));
AssertTrue(File.Exists(reportPdfOutputPath), "workflow pdf report output file");
AssertTrue(new FileInfo(reportPdfOutputPath).Length > 2048, "workflow pdf report output content");
```

- [ ] **Step 4: Add test helpers near existing helper methods**

```csharp
static bool StartsWithPdfHeader(byte[] content)
{
    return content.Length >= 5 &&
        content[0] == (byte)'%' &&
        content[1] == (byte)'P' &&
        content[2] == (byte)'D' &&
        content[3] == (byte)'F' &&
        content[4] == (byte)'-';
}

static bool ContainsAscii(byte[] content, string text)
{
    return System.Text.Encoding.ASCII.GetString(content).Contains(text, StringComparison.Ordinal);
}
```

- [ ] **Step 5: Run the core tests and verify RED**

Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: build fails because `CompositionSnapshotPdfExporter` and `CompositionDocumentReportPdfExporter` do not exist.

---

## Task 2: Add SkiaSharp Dependency

**Files:**
- Modify: `ThinkComposer.Core/ThinkComposer.Core.csproj`

- [ ] **Step 1: Add package reference**

Run: `dotnet add ThinkComposer.Core/ThinkComposer.Core.csproj package SkiaSharp`

Expected: package restore succeeds and `ThinkComposer.Core.csproj` contains a `PackageReference Include="SkiaSharp"`.

- [ ] **Step 2: Build core to verify dependency restore**

Run: `dotnet build ThinkComposer.Core/ThinkComposer.Core.csproj -p:TargetFramework=net10.0-windows`

Expected: build still fails only because PDF exporter classes are missing from tests, or succeeds if tests are not part of this build.

---

## Task 3: Implement View PDF Exporter

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionPdfExportOptions.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`

- [ ] **Step 1: Add export options**

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionPdfExportOptions(
    float PageWidth = 842,
    float PageHeight = 595,
    float Margin = 36,
    string FontFamily = "Segoe UI");
```

- [ ] **Step 2: Add `CompositionSnapshotPdfExporter.Export` API**

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotPdfExporter
{
    public static byte[] Export(CompositionViewSnapshot snapshot, CompositionPdfExportOptions? options = null)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        options ??= new CompositionPdfExportOptions();
        using var stream = new MemoryStream();
        using var document = SkiaSharp.SKDocument.CreatePdf(stream);
        using var canvas = document.BeginPage(options.PageWidth, options.PageHeight);

        RenderSnapshot(canvas, snapshot, options);

        document.EndPage();
        document.Close();
        return stream.ToArray();
    }
}
```

- [ ] **Step 3: Implement bounds, color parsing, routing, node drawing, connector drawing, and text drawing in the same file**

Use existing `CompositionConnectorRouter.Route(source, target)` so connector geometry matches SVG/canvas behavior. Use white fallback backgrounds and existing snapshot style values:

```csharp
private static SkiaSharp.SKColor ParseColor(string value, SkiaSharp.SKColor fallback)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return fallback;
    }

    return SkiaSharp.SKColor.TryParse(value.Trim(), out var color) ? color : fallback;
}
```

- [ ] **Step 4: Run the core tests and verify view PDF goes GREEN or report PDF remains RED**

Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: view PDF assertions pass; report PDF exporter still missing.

---

## Task 4: Implement Document Report PDF Exporter

**Files:**
- Create: `ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`

- [ ] **Step 1: Expose an internal render method from `CompositionSnapshotPdfExporter`**

```csharp
internal static void RenderSnapshot(
    SkiaSharp.SKCanvas canvas,
    CompositionViewSnapshot snapshot,
    CompositionPdfExportOptions options)
```

- [ ] **Step 2: Add report exporter API**

```csharp
namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentReportPdfExporter
{
    public static byte[] Export(CompositionDocumentSnapshot document, CompositionPdfExportOptions? options = null)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        options ??= new CompositionPdfExportOptions();
        using var stream = new MemoryStream();
        using var pdf = SkiaSharp.SKDocument.CreatePdf(stream);

        // cover page, domain summary page, one page per view, summary pages.

        pdf.Close();
        return stream.ToArray();
    }
}
```

- [ ] **Step 3: Render report pages**

The report must include:

- title and domain summary
- counts for concepts, relationships, and views
- one view diagram page per `document.Views`
- concept summary lines
- relationship summary lines

- [ ] **Step 4: Run the core tests and verify GREEN**

Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: all core tests pass.

---

## Task 5: Wire PDF Commands Into Command Catalog

**Files:**
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandIds.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs`

- [ ] **Step 1: Add command ids**

```csharp
public const string ExportPdf = "document.exportPdf";
public const string ReportPdf = "document.reportPdf";
```

- [ ] **Step 2: Add command catalog entries**

```csharp
Command(CompositionCommandIds.ExportPdf, "Export view PDF", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasSnapshot, MissingSnapshotReason),
Command(CompositionCommandIds.ReportPdf, "Report PDF", CompositionCommandCategory.Document, CompositionCommandSurface.TopBar, "Document", "", hasDocument || hasSnapshot, MissingDocumentReason),
```

- [ ] **Step 3: Run core tests**

Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: all core tests pass.

---

## Task 6: Wire PDF Export Into WinUI

**Files:**
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [ ] **Step 1: Update button tooltips**

Change export tooltip to `Export HTML, SVG, or PDF`.

Change report tooltip to `Export document report as HTML or PDF`.

- [ ] **Step 2: Add `.pdf` picker options**

In `ExportButton_Click`:

```csharp
picker.FileTypeChoices.Add("PDF document", [".pdf"]);
```

In `ReportButton_Click`:

```csharp
picker.FileTypeChoices.Add("Document report PDF", [".pdf"]);
```

- [ ] **Step 3: Write PDF bytes when `.pdf` is selected**

In `ExportButton_Click`:

```csharp
if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
{
    await FileIO.WriteBytesAsync(file, CompositionSnapshotPdfExporter.Export(_currentSnapshot));
}
else
{
    var content = string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase)
        ? CompositionSnapshotSvgExporter.Export(_currentSnapshot)
        : CompositionSnapshotHtmlExporter.Export(_currentSnapshot);
    await FileIO.WriteTextAsync(file, content);
}
```

In `ReportButton_Click`:

```csharp
if (string.Equals(Path.GetExtension(file.Path), ".pdf", StringComparison.OrdinalIgnoreCase))
{
    await FileIO.WriteBytesAsync(file, CompositionDocumentReportPdfExporter.Export(document));
}
else
{
    await FileIO.WriteTextAsync(file, CompositionDocumentReportHtmlExporter.Export(document));
}
```

- [ ] **Step 4: Add cases to `ExecuteCommand`**

```csharp
case CompositionCommandIds.ExportPdf:
    ExportButton_Click(this, new RoutedEventArgs());
    break;
case CompositionCommandIds.ReportPdf:
    ReportButton_Click(this, new RoutedEventArgs());
    break;
```

- [ ] **Step 5: Build WinUI**

Run: `dotnet build ThinkComposer.WinUI/ThinkComposer.WinUI.csproj -p:Platform=x64`

Expected: build succeeds.

---

## Task 7: Update Docs And Verify No New XPS UI

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

- [ ] **Step 1: Update README**

Change output line to mention native PDF:

```markdown
- Export HTML/SVG/PDF, generate PDF/HTML reports, presentations, and printable HTML previews.
```

- [ ] **Step 2: Update parity audit**

Replace the current native PDF/XPS gap with:

```markdown
- Native PDF export is implemented in the WinUI path. XPS is intentionally not carried forward as a user-facing output target.
```

- [ ] **Step 3: Search for unwanted new XPS claims**

Run: `rg -n "PDF/XPS|XPS document|Export Image.*PDF|native PDF/XPS" README.md docs/superpowers ThinkComposer.WinUI ThinkComposer.Core`

Expected: matches only describe legacy behavior or the intentional XPS drop.

---

## Task 8: Full Verification And Commit

**Files:**
- All touched files

- [ ] **Step 1: Run core tests**

Run: `dotnet run --project ThinkComposer.Core.Tests/ThinkComposer.Core.Tests.csproj`

Expected: all assertions pass.

- [ ] **Step 2: Run bridge tests**

Run: `dotnet run --project ThinkComposer.LegacyBridge.Tests/ThinkComposer.LegacyBridge.Tests.csproj`

Expected: all assertions pass.

- [ ] **Step 3: Build WinUI**

Run: `dotnet build ThinkComposer.WinUI/ThinkComposer.WinUI.csproj -p:Platform=x64`

Expected: build succeeds.

- [ ] **Step 4: Run migration check**

Run: `powershell -ExecutionPolicy Bypass -File scripts/check-winui-migration.ps1`

Expected: script succeeds.

- [ ] **Step 5: Commit**

```bash
git add ThinkComposer.Core/ThinkComposer.Core.csproj ThinkComposer.Core/Rendering/CompositionPdfExportOptions.cs ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs ThinkComposer.Core/Rendering/CompositionCommandIds.cs ThinkComposer.Core/Rendering/CompositionCommandCatalog.cs ThinkComposer.Core.Tests/Program.cs ThinkComposer.WinUI/MainPage.xaml ThinkComposer.WinUI/MainPage.xaml.cs README.md docs/superpowers/specs/2026-05-08-winui-parity-audit.md docs/superpowers/plans/2026-05-09-native-pdf-export.md
git commit -m "Add native PDF export"
```
