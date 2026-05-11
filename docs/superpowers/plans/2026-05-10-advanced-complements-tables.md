# Advanced Complements And Tables Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve complement parity and table editing with dash/offset/quadrant support, column-width metadata, and simple formulas.

**Architecture:** Keep behavior in Core where possible. WinUI owns editor controls and clipboard/UI actions. PDF/HTML exporters consume Core snapshots and evaluated tables.

**Tech Stack:** .NET 10/WinUI 3, SkiaSharp PDF, Win2D canvas, XML `.tcdoc` persistence.

---

### Task 1: Complement Layout And Style

**Files:**
- Modify: `ThinkComposer.Core.Tests/Program.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionComplementStyle.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionViewComplementLayout.cs`
- Modify: `ThinkComposer.WinUI/Canvas/CompositionCanvas.xaml.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionSnapshotPdfExporter.cs`

- [x] Add failing tests for `lineDash`, `lineThickness`, `offsetX`, `offsetY`, and `quadrant`.
- [x] Add `StrokeDash` to complement style.
- [x] Parse `lineThickness` and `lineDash` aliases.
- [x] Apply offset and quadrant placement in layout.
- [x] Render dashed complement borders in WinUI and PDF.

### Task 2: Table Widths And Formulas

**Files:**
- Modify: `ThinkComposer.Core.Tests/Program.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDetailTableSnapshot.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDetailTableCsv.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDetailTableEditor.cs`
- Create: `ThinkComposer.Core/Rendering/CompositionDetailTableFormulaEvaluator.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentSnapshotXmlStore.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentReportHtmlExporter.cs`
- Modify: `ThinkComposer.Core/Rendering/CompositionDocumentReportPdfExporter.cs`
- Modify: `ThinkComposer.WinUI/MainPage.xaml`
- Modify: `ThinkComposer.WinUI/MainPage.xaml.cs`

- [x] Add failing tests for column widths and formula evaluation.
- [x] Persist widths through XML and CSV.
- [x] Preserve widths through table editor operations.
- [x] Add simple formula evaluator for cell references, arithmetic, and `SUM`.
- [x] Add WinUI width field and formula-evaluation action.
- [x] Evaluate formulas in report output.

### Task 3: Verification

**Files:**
- Modify: `docs/superpowers/specs/2026-05-08-winui-parity-audit.md`

- [x] Update parity audit.
- [x] Run Core tests.
- [x] Run LegacyBridge tests.
- [x] Build WinUI x64.
- [x] Run migration check script.
