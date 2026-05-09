param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message) | Out-Null
}

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Needle,
        [string]$Message
    )

    $fullPath = Join-Path $repoRoot $Path
    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -notmatch [regex]::Escape($Needle)) {
        Add-Failure $Message
    }
}

function Assert-NotContains {
    param(
        [string]$Path,
        [string]$Needle,
        [string]$Message
    )

    $fullPath = Join-Path $repoRoot $Path
    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -match [regex]::Escape($Needle)) {
        Add-Failure $Message
    }
}

function Test-ForbiddenTokens {
    param(
        [string[]]$Roots,
        [string[]]$Patterns
    )

    $extensions = @(".cs", ".csproj", ".xaml")
    foreach ($root in $Roots) {
        $fullRoot = Join-Path $repoRoot $root
        if (-not (Test-Path -LiteralPath $fullRoot)) {
            Add-Failure "Missing checked root: $root"
            continue
        }

        $files = Get-ChildItem -LiteralPath $fullRoot -Recurse -File |
            Where-Object { $extensions -contains $_.Extension }

        foreach ($file in $files) {
            $relative = $file.FullName.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
            $content = Get-Content -LiteralPath $file.FullName -Raw
            foreach ($pattern in $Patterns) {
                if ($content -match $pattern) {
                    Add-Failure "Forbidden WPF reference '$pattern' in $relative"
                }
            }
        }
    }
}

Assert-Contains "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj" "<UseWinUI>true</UseWinUI>" "WinUI app must keep UseWinUI enabled."
Assert-NotContains "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj" "<UseWPF>true</UseWPF>" "WinUI app must not enable WPF."
Assert-NotContains "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj" "..\ThinkComposer\ThinkComposer.csproj" "WinUI app must not reference the legacy WPF application."
Assert-NotContains "ThinkComposer.Core\ThinkComposer.Core.csproj" "<UseWPF>true</UseWPF>" "Core project must stay UI-framework neutral."
Assert-Contains "scripts\smoke-winui-launch.ps1" "DocumentPath" "WinUI smoke must support startup document coverage."
Assert-Contains "scripts\smoke-winui-launch.ps1" "RequireWindow" "WinUI smoke must verify the main window when requested."
Assert-Contains "scripts\smoke-winui-ui.ps1" "UIAutomationClient" "WinUI UI smoke must use UI Automation."
Assert-Contains "scripts\smoke-winui-ui.ps1" "ExpectedTitle" "WinUI UI smoke must verify startup document identity."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "CompositionDocumentFileKind.LegacyPackage" "WinUI startup document detection must accept legacy packages."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml" "ThemeToggleButton" "Theme toggle must be automation-addressable."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml" "TableCellsPanel" "WinUI table editor must expose per-cell editing controls."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "RootPage_KeyDown" "WinUI must expose mind-map Tab/Enter keyboard creation."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "Full document printable preview generated" "Print preview must use the full document report path."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "ReadTableCellValues" "WinUI table editor must save rows from per-cell controls."
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "CompositionMindMapEditor.CreateChild" "WinUI must route Tab mind-map creation through Core."
Assert-Contains "ThinkComposer.Core\Rendering\CompositionFileGenerationResult.cs" "EnsureTrailingSeparator" "Generated file output must reject sibling-prefix path escapes."
Assert-Contains "ThinkComposer.Core.Tests\Program.cs" "workflow report output file" "Core tests must cover report file workflow output."
Assert-Contains "ThinkComposer.Core.Tests\Program.cs" "modern generation strips metadata directives" "Core tests must cover output-template metadata stripping."
Assert-Contains "scripts\release-winui.ps1" "release-index.json" "WinUI release script must emit a release index."
Assert-Contains "scripts\release-winui.ps1" "CertificatePath" "WinUI release script must support optional code signing."
Assert-Contains "scripts\release-winui.ps1" "UpdateChannel" "WinUI release script must include update channel metadata."
Assert-Contains "scripts\release-winui.ps1" "primaryArtifactType" "WinUI release index must distinguish zip and directory artifacts."
Assert-Contains "scripts\release-winui.ps1" "artifact-hashes.json" "WinUI NoZip release must emit directory hash manifests."
Assert-Contains "scripts\verify-winui-release.ps1" "SHA256" "WinUI release verification must validate artifact hashes."
Assert-Contains "scripts\verify-winui-release.ps1" "hashManifest" "WinUI release verification must validate directory hash manifests."

$forbiddenPatterns = @(
    "\bSystem\.Windows\b",
    "\bPresentationFramework\b",
    "\bPresentationCore\b",
    "\bWindowsBase\b",
    "\bSystem\.Windows\.Controls\b",
    "\bSystem\.Windows\.Media\b",
    "\bSystem\.Windows\.Input\b"
)

Test-ForbiddenTokens `
    -Roots @("ThinkComposer.WinUI", "ThinkComposer.Core", "ThinkComposer.Core.Tests") `
    -Patterns $forbiddenPatterns

if (-not $SkipBuild) {
    Push-Location $repoRoot
    try {
        dotnet build "Instrumind_ThinkComposer.sln" -p:Configuration=Debug -p:Platform=x86
        if ($LASTEXITCODE -ne 0) {
            Add-Failure "Solution build failed."
        }
    }
    finally {
        Pop-Location
    }
}

if ($failures.Count -gt 0) {
    Write-Host "WinUI migration checks failed:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host "WinUI migration checks passed." -ForegroundColor Green
