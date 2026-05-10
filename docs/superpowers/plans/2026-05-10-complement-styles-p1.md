# Complement Styles P1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add style metadata parsing for visual complements and apply it consistently in WinUI canvas and native PDF export.

**Architecture:** Parse complement style once in `ThinkComposer.Core.Rendering` and expose it through `CompositionComplementRenderItem`. WinUI and PDF renderers consume that style with their own color/font primitives.

**Tech Stack:** C# multi-targeted core library, WinUI/Win2D canvas, SkiaSharp PDF, existing console tests.

---

## File Map

- Create `ThinkComposer.Core/Rendering/CompositionComplementStyle.cs`: immutable style values with safe defaults.
- Modify `ThinkComposer.Core/Rendering/CompositionComplementRenderItem.cs`: add `Style`.
- Modify `ThinkComposer.Core/Rendering/CompositionViewComplementLayout.cs`: parse aliases and numeric style values.
- Modify `ThinkComposer.Core.Tests/Program.cs`: add RED tests for complement style parsing.
- Modify `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`: render complement style in canvas.
- Modify `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`: render complement style in PDF.
- Modify `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`: update P1 complement gap.

## Task 1: RED Tests

- [ ] Add this test block after the existing `AssertEqual("Legend", modernDocument.Views[0].Complements[0].Value, "modern view complement");` assertion in `ThinkComposer.Core.Tests/Program.cs`:

```csharp
var styledComplementItems = CompositionViewComplementLayout.Build(
    [
        new CompositionExtensionSnapshot(
            "legend.status",
            "Styled body",
            new Dictionary<string, string>
            {
                ["kind"] = "legend",
                ["title"] = "Status",
                ["backgroundColor"] = "#fff4cc",
                ["borderColor"] = "#b45309",
                ["textColor"] = "#111827",
                ["alpha"] = "0.65",
                ["borderWidth"] = "2.5",
                ["fontFamily"] = "Consolas",
                ["fontSize"] = "13.5",
                ["icon"] = "warning"
            })
    ],
    modernDocument.Views[0].Nodes);
var styledComplement = styledComplementItems.Single();
AssertEqual("#fff4cc", styledComplement.Style.Fill, "complement style fill alias");
AssertEqual("#b45309", styledComplement.Style.Stroke, "complement style stroke alias");
AssertEqual("#111827", styledComplement.Style.Text, "complement style text alias");
AssertEqual(0.65, styledComplement.Style.Opacity, "complement style opacity alias");
AssertEqual(2.5, styledComplement.Style.StrokeThickness, "complement style stroke thickness alias");
AssertEqual("Consolas", styledComplement.Style.FontFamily, "complement style font family alias");
AssertEqual(13.5, styledComplement.Style.FontSize, "complement style font size");
AssertEqual("warning", styledComplement.Style.Icon, "complement style icon");

var fallbackComplement = CompositionViewComplementLayout.Build(
    [
        new CompositionExtensionSnapshot(
            "info.bad-style",
            "Bad style",
            new Dictionary<string, string>
            {
                ["opacity"] = "not-a-number",
                ["strokeThickness"] = "-4",
                ["fontSize"] = "0"
            })
    ],
    modernDocument.Views[0].Nodes).Single();
AssertEqual(1.0, fallbackComplement.Style.Opacity, "complement invalid opacity fallback");
AssertEqual(0.0, fallbackComplement.Style.StrokeThickness, "complement invalid stroke fallback");
AssertEqual(0.0, fallbackComplement.Style.FontSize, "complement invalid font fallback");
```

- [ ] Run `dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj`.

Expected: compile failure because `CompositionComplementRenderItem.Style` does not exist.

## Task 2: Core Style Parsing

- [ ] Create `CompositionComplementStyle` with properties `Fill`, `Stroke`, `Text`, `Opacity`, `StrokeThickness`, `FontFamily`, `FontSize`, and `Icon`.
- [ ] Add `CompositionComplementStyle Style` to `CompositionComplementRenderItem`.
- [ ] In `CompositionViewComplementLayout`, parse aliases with existing case-insensitive property lookup.
- [ ] Clamp opacity to `0..1`; accept positive stroke thickness and font size; use default values when missing or invalid.
- [ ] Run core tests and expect PASS.

## Task 3: WinUI Canvas Rendering

- [ ] In `CompositionCanvas.xaml.cs`, use `item.Style.Fill`, `Stroke`, `Text`, `Opacity`, and `StrokeThickness` for group/card rendering.
- [ ] Keep existing palette defaults when style values are empty or invalid.
- [ ] Render `item.Style.Icon` as a small prefix in card titles when present.
- [ ] Build WinUI with `dotnet build ThinkComposer.WinUI\ThinkComposer.WinUI.csproj -p:Platform=x64`.

Expected: build exits 0.

## Task 4: PDF Rendering

- [ ] In `CompositionSnapshotPdfExporter`, use `item.Style.Fill`, `Stroke`, `Text`, `Opacity`, `StrokeThickness`, `FontFamily`, `FontSize`, and `Icon`.
- [ ] Keep existing hardcoded defaults when style values are absent.
- [ ] Run core tests.

Expected: tests exit 0.

## Task 5: Docs, Full Verification, Commit

- [ ] Update `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`: complement styles should no longer be the P1 gap; remaining gap is only unknown legacy-specific complement variants.
- [ ] Run:

```powershell
dotnet run --project ThinkComposer.Core.Tests\ThinkComposer.Core.Tests.csproj
dotnet run --project ThinkComposer.LegacyBridge.Tests\ThinkComposer.LegacyBridge.Tests.csproj
dotnet build ThinkComposer.WinUI\ThinkComposer.WinUI.csproj -p:Platform=x64
powershell -ExecutionPolicy Bypass -File scripts\check-winui-migration.ps1
```

Expected: all exit 0.

- [ ] Commit and push:

```powershell
git add ThinkComposer.Core.Tests\Program.cs ThinkComposer.Core\Rendering\CompositionComplementStyle.cs ThinkComposer.Core\Rendering\CompositionComplementRenderItem.cs ThinkComposer.Core\Rendering\CompositionViewComplementLayout.cs ThinkComposer.Core\Rendering\CompositionSnapshotPdfExporter.cs ThinkComposer.WinUI\Canvas\CompositionCanvas.xaml.cs docs\superpowers\plans\2026-05-10-complement-styles-p1.md docs\superpowers\specs\2026-05-08-winui-parity-audit.md
git commit -m "Add complement style rendering"
git push origin thinkcomposer2
```
