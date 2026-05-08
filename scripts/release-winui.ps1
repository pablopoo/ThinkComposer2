param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x86", "win-x64", "win-arm64")]
    [string[]]$RuntimeIdentifiers = @("win-x86", "win-x64"),

    [string]$OutputRoot = "artifacts\winui",

    [switch]$NoZip,

    [switch]$SkipSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRootPath = Join-Path $repoRoot $OutputRoot
$artifacts = New-Object System.Collections.Generic.List[object]

function Get-RelativePath {
    param([string]$Path)

    $root = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $target = [System.IO.Path]::GetFullPath($Path)
    if ($target.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $target.Substring($root.Length)
    }

    return $target
}

function Get-GitCommit {
    Push-Location $repoRoot
    try {
        $commit = git rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($commit)) {
            return $commit.Trim()
        }
    }
    finally {
        Pop-Location
    }

    return "unknown"
}

foreach ($runtimeIdentifier in $RuntimeIdentifiers) {
    $publishArgs = @{
        Configuration = $Configuration
        RuntimeIdentifier = $runtimeIdentifier
        OutputRoot = $OutputRoot
    }
    if ($NoZip) {
        $publishArgs.NoZip = $true
    }
    if ($SkipSmoke) {
        $publishArgs.SkipSmoke = $true
    }

    & (Join-Path $PSScriptRoot "publish-winui.ps1") @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Publishing failed for $runtimeIdentifier."
    }

    $artifactPath = Join-Path $outputRootPath "$Configuration-$runtimeIdentifier"
    $manifestPath = Join-Path $artifactPath "release-manifest.json"
    $exePath = Join-Path $artifactPath "ThinkComposer.WinUI.exe"
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "Release manifest missing for $runtimeIdentifier."
    }
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Executable missing for $runtimeIdentifier."
    }

    $zipPath = Join-Path $outputRootPath "ThinkComposer.WinUI-$Configuration-$runtimeIdentifier.zip"
    $primaryArtifactPath = if ((-not $NoZip) -and (Test-Path -LiteralPath $zipPath)) { $zipPath } else { $exePath }
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $primaryArtifactPath
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

    $artifacts.Add([ordered]@{
        runtimeIdentifier = $runtimeIdentifier
        platform = $manifest.platform
        manifest = Get-RelativePath $manifestPath
        directory = Get-RelativePath $artifactPath
        primaryArtifact = Get-RelativePath $primaryArtifactPath
        sha256 = $hash.Hash
    }) | Out-Null
}

$index = [ordered]@{
    product = "ThinkComposer.WinUI"
    configuration = $Configuration
    commit = Get-GitCommit
    createdUtc = [DateTime]::UtcNow.ToString("o")
    artifacts = $artifacts
}

if (-not (Test-Path -LiteralPath $outputRootPath)) {
    New-Item -ItemType Directory -Path $outputRootPath -Force | Out-Null
}

$indexPath = Join-Path $outputRootPath "release-index.json"
$index | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $indexPath -Encoding UTF8
Write-Host "Created $(Get-RelativePath $indexPath)" -ForegroundColor Green
