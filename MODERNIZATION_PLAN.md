# ThinkComposer Modernization Plan

Date: 2026-05-07

## Goal

Modernize ThinkComposer in controlled phases: first make the current application build reliably, then migrate the project system and runtime, then update dependencies and source code, and finally modernize the WPF UI.

Target runtime: `.NET 10 LTS` with `net10.0-windows` and WPF. WPF remains Windows-only in modern .NET.

## Current Baseline

- Main app: `ThinkComposer/ThinkComposer.csproj`
- Solution: `Instrumind_ThinkComposer.sln`
- Current primary target: `.NET Framework 4.8` as the first modernization step
- Project style: active solution `.csproj` projects are SDK-style; legacy unused project variants remain under `DotLiquid` and `PdfSharpXps/PdfSharp`
- UI: WPF
- Build platform: `net48` keeps `x86`; `net10.0-windows` builds AnyCPU so it can run with the installed x64 .NET runtime.
- Local SDK installed: `.NET SDK 10.0.203`
- Current build state: Debug x86 solution builds with 0 warnings and 0 errors; net10 smoke-test starts the main app.
- Active solution projects build for `net48;net10.0-windows`; net48 keeps the legacy output paths.
- `.NET Framework 4.8` reference assemblies are restored via NuGet package `Microsoft.NETFramework.ReferenceAssemblies.net48`
- Local check: the global `.NET Framework 4.8` Developer Pack path was not found under `C:\Program Files (x86)\Reference Assemblies`

## Dependency Inventory

Vendored source dependencies:

- `ICSharpCode.AvalonEdit`
- `PdfSharpXps/PdfSharp`
- `PdfSharpXps/PdfSharp.Xps`
- `DotLiquid`

Framework/API risks found in the codebase:

- `BinaryFormatter`
- `System.Web`, removed from active projects
- `System.Drawing` and GDI+
- Office Interop references, removed from `Common` because they were unused
- Windows Registry access
- P/Invoke to Windows DLLs
- WPF theme reference to `PresentationFramework.Aero`, removed from the main app

## Strategy

Do not start with UI. The technical foundation must move first, because UI work on old project files and old dependencies would create rework.

Use `.NET Framework 4.8` instead of `4.6.1` as the intermediate framework. This avoids spending effort restoring an older target and gives the project a cleaner bridge to SDK-style projects and `net10.0-windows`.

Recommended path:

1. Stabilize the current build on `.NET Framework 4.8`.
2. Keep `.NET Framework 4.8` only as an intermediate step before `.NET 10`.
3. Convert projects to SDK-style while still targeting .NET Framework.
4. Replace vendored dependencies with modern NuGet packages where possible.
5. Port app and libraries to `net10.0-windows`.
6. Replace unsupported or risky APIs.
7. Add regression tests around serialization, document loading, PDF/XPS export, and core editing workflows.
8. Modernize the WPF theme and high-use screens.

## Phase 0: Build Baseline

Objective: get the existing application compiling before any migration.

Tasks:

- Restore `.NET Framework 4.8` reference assemblies.
- Build:

```powershell
dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86
```

- Record all compile errors and warnings in this file or a companion issue list.
- Do not refactor during this phase.

Exit criteria:

- Debug x86 build succeeds, or all blocking compile errors are documented.

## Phase 1: Project System Modernization

Objective: reduce project-file friction before changing runtime.

Tasks:

- Convert `Common/Common.csproj` to SDK-style first.
- Convert `ThinkComposer/ThinkComposer.csproj` after `Common` builds.
- Convert `AdminUtils/AdminUtils.csproj` separately.
- Keep `TargetFramework` on .NET Framework during conversion.
- Preserve assembly names and namespaces.
- Preserve WPF resources and generated settings.

Exit criteria:

- Solution builds after SDK-style conversion.
- No runtime target change yet.

## Phase 2: Dependency Modernization

Objective: stop carrying old third-party source where NuGet packages are viable.

Candidate replacements:

- `ICSharpCode.AvalonEdit` -> NuGet `AvalonEdit`
- `DotLiquid-2010` -> current `DotLiquid` package, or another maintained templating package if compatibility breaks
- `PdfSharp` / `PdfSharp.Xps` -> current `PDFsharp` / `MigraDoc` path, with explicit review of XPS support

Exit criteria:

- Vendored dependency removal plan exists per dependency.
- One dependency is replaced at a time.
- Build succeeds after each replacement.

## Phase 3: Runtime Port

Objective: move the main app to modern .NET.

Target:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
```

Likely required package references:

- `Microsoft.Windows.Compatibility`
- `System.Configuration.ConfigurationManager`
- `System.Drawing.Common` only where unavoidable, and only for Windows-only code

Exit criteria:

- `Common` builds on `net10.0-windows`.
- `ThinkComposer` builds on `net10.0-windows`.
- App starts and opens the main window.

## Phase 4: Source Modernization

Objective: remove high-risk APIs and improve maintainability.

Priority order:

1. Replace `BinaryFormatter` serialization.
2. Isolate file format compatibility for existing documents.
3. Replace `System.Web` helpers with modern APIs.
4. Encapsulate Registry, Office Interop, and P/Invoke behind Windows service interfaces.
5. Add focused tests for model serialization and document opening.
6. Enable modern C# features gradually.
7. Enable nullable only after enough tests exist.

Exit criteria:

- New documents no longer depend on `BinaryFormatter`.
- Existing documents have a defined compatibility path.
- Core model tests exist.

## Phase 5: UI Modernization

Objective: refresh WPF look and usability without rewriting the product.

Scope:

- Replace old Aero dependency.
- Define a modern resource dictionary for colors, typography, spacing, controls, and icons.
- Modernize main window shell first.
- Modernize command bars, palettes, buttons, tabs, dialogs, and inspectors.
- Keep canvas/editor behavior stable.

Exit criteria:

- Main shell and common controls use the new theme.
- Existing composition editing workflows still work.
- UI is tested at multiple DPI/font scales.

## Immediate Next Actions

1. Continue replacing or isolating runtime-risk `System.Drawing` usage that remains in shared rendering/PDF code.
2. Decide whether to delete or archive legacy unused project variants (`DotLiquid-2008`, `DotLiquid`, `PdfSharp`, `PdfSharp-Hybrid`, `PdfSharp-ag`).
3. Begin UI modernization with a modern WPF resource dictionary and main shell refresh.

## Verification Log

- 2026-05-06: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` failed with `MSB3644` because `.NETFramework,Version=v4.6.1` reference assemblies were not installed. Affected first-wave projects: `AdminUtils`, `ThinkComposer`, `Common`.
- 2026-05-07: After retargeting the solution projects from `v4.6.1` to `v4.8`, the same build fails with `MSB3644` because `.NETFramework,Version=v4.8` reference assemblies are not installed. Next required local dependency: `.NET Framework 4.8 Developer Pack`.
- 2026-05-07: Added repo-local `.NET Framework 4.8` reference assemblies via `Microsoft.NETFramework.ReferenceAssemblies.net48`; `dotnet clean Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds.
- 2026-05-07: `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 34 remaining code warnings and 0 errors.
- 2026-05-07: Converted `Common/Common.csproj` to SDK-style while keeping `net48`, explicit WPF items, and legacy output paths. `dotnet clean` and `dotnet build` for `Instrumind_ThinkComposer.sln` both succeed with 34 warnings and 0 errors.
- 2026-05-07: Converted all active solution `.csproj` projects to SDK-style: `AdminUtils`, `Common`, `DotLiquid-2010`, `ICSharpCode.AvalonEdit`, `PdfSharp-WPF`, `PdfSharp.Xps`, and `ThinkComposer`. Updated solution x86 mappings so external dependencies build after a clean. `dotnet clean` and `dotnet build` for `Instrumind_ThinkComposer.sln` both succeed with 37 warnings and 0 errors.
- 2026-05-07: Cleaned active solution warnings and removed the missing `AllRules.ruleset` reference. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.
- 2026-05-07: Multi-targeted `ICSharpCode.AvalonEdit` to `net48;net10.0-windows`. `dotnet build ICSharpCode.AvalonEdit\ICSharpCode.AvalonEdit.csproj -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors. Full solution build remains at 0 warnings and 0 errors.
- 2026-05-07: Multi-targeted `PdfSharp-WPF` and `PdfSharp.Xps` to `net48;net10.0-windows`. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.
- 2026-05-07: Multi-targeted `Common`, `DotLiquid-2010`, `AdminUtils`, and `ThinkComposer` to `net48;net10.0-windows`. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.
- 2026-05-07: Set net10 builds to AnyCPU while preserving x86 for net48, removed `System.Web` from active projects, replaced AdminUtils `WebRequest` with `HttpClient`, removed hard-coded Aero theme loading, and smoke-tested `ThinkComposer\bin\Debug\net10.0-windows\Instrumind.ThinkComposer.exe`. The app starts and stays running with empty stdout/stderr; it is closed after the smoke-test.
- 2026-05-07: Replaced the remaining active `WebClient` download helper with `HttpClient`, preserving progress/cancel callbacks on the caller synchronization context. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.
- 2026-05-07: Isolated `BinaryFormatter` helper usage to `net48`; modern .NET now fails fast with explicit `PlatformNotSupportedException` instead of invoking formatter-based serialization. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.
- 2026-05-07: Removed unused active `System.Drawing` references from `AdminUtils` and `ThinkComposer`, and removed unnecessary WinForms targeting from `ThinkComposer`. `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86` succeeds with 0 warnings and 0 errors.

## References

- Microsoft porting guide: https://learn.microsoft.com/en-us/dotnet/core/porting/framework-overview
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
