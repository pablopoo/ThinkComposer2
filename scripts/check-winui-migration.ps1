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
Assert-Contains "ThinkComposer.WinUI\MainPage.xaml.cs" "Full document printable preview generated" "Print preview must use the full document report path."

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
