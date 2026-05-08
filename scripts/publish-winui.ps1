param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x86", "win-x64", "win-arm64")]
    [string]$RuntimeIdentifier = "win-x64",

    [string]$OutputRoot = "artifacts\winui",

    [switch]$NoZip,

    [switch]$SkipSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj"
$outputRootPath = Join-Path $repoRoot $OutputRoot
$outputPath = Join-Path $outputRootPath "$Configuration-$RuntimeIdentifier"
$platform = switch ($RuntimeIdentifier) {
    "win-x86" { "x86" }
    "win-x64" { "x64" }
    "win-arm64" { "ARM64" }
}

function Assert-InRepo {
    param([string]$Path)

    $resolvedRoot = [System.IO.Path]::GetFullPath($repoRoot)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside repository: $resolvedPath"
    }
}

Assert-InRepo $outputPath

if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

Push-Location $repoRoot
try {
    dotnet publish $project `
        -c $Configuration `
        -r $RuntimeIdentifier `
        --self-contained true `
        -p:Platform=$platform `
        -p:WindowsAppSDKSelfContained=true `
        -p:PublishSingleFile=false `
        -o $outputPath

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    $exePath = Join-Path $outputPath "ThinkComposer.WinUI.exe"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Published executable was not found: $exePath"
    }

    $commit = "unknown"
    $gitCommit = git rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitCommit)) {
        $commit = $gitCommit.Trim()
    }

    $manifest = [ordered]@{
        product = "ThinkComposer.WinUI"
        configuration = $Configuration
        runtimeIdentifier = $RuntimeIdentifier
        platform = $platform
        commit = $commit
        createdUtc = [DateTime]::UtcNow.ToString("o")
        executable = "ThinkComposer.WinUI.exe"
    }

    $manifestPath = Join-Path $outputPath "release-manifest.json"
    $manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    if (-not $NoZip) {
        $zipPath = Join-Path $outputRootPath "ThinkComposer.WinUI-$Configuration-$RuntimeIdentifier.zip"
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $zipPath -Force
        Write-Host "Created $zipPath"
    }

    if (-not $SkipSmoke) {
        & (Join-Path $PSScriptRoot "smoke-winui-launch.ps1") -ExecutablePath $exePath
    }
}
finally {
    Pop-Location
}

Write-Host "Published ThinkComposer.WinUI to $outputPath" -ForegroundColor Green
