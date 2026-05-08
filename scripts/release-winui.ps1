param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x86", "win-x64", "win-arm64")]
    [string[]]$RuntimeIdentifiers = @("win-x86", "win-x64"),

    [string]$OutputRoot = "artifacts\winui",

    [string]$Version = "",

    [string]$UpdateChannel = "stable",

    [string]$UpdateBaseUrl = "",

    [string]$CertificatePath = "",

    [string]$CertificatePasswordEnvVar = "THINKCOMPOSER_SIGNING_PASSWORD",

    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [string]$SignToolPath = "",

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

function Get-ReleaseVersion {
    param([string]$ExecutablePath)

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        return $Version
    }

    $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath)
    if (-not [string]::IsNullOrWhiteSpace($fileVersion.ProductVersion)) {
        return $fileVersion.ProductVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($fileVersion.FileVersion)) {
        return $fileVersion.FileVersion
    }

    return "0.0.0"
}

function Resolve-SignToolPath {
    if (-not [string]::IsNullOrWhiteSpace($SignToolPath)) {
        if (-not (Test-Path -LiteralPath $SignToolPath)) {
            throw "SignTool not found: $SignToolPath"
        }

        return [System.IO.Path]::GetFullPath($SignToolPath)
    }

    $command = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($command -ne $null) {
        return $command.Source
    }

    $kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (Test-Path -LiteralPath $kitsRoot) {
        $candidate = Get-ChildItem -LiteralPath $kitsRoot -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate -ne $null) {
            return $candidate.FullName
        }
    }

    throw "SignTool was not found. Install Windows SDK or pass -SignToolPath."
}

function Invoke-CodeSigning {
    param([string]$ExecutablePath)

    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        return $false
    }

    $resolvedCertificatePath = [System.IO.Path]::GetFullPath($CertificatePath)
    if (-not (Test-Path -LiteralPath $resolvedCertificatePath)) {
        throw "Signing certificate not found: $resolvedCertificatePath"
    }

    $resolvedSignToolPath = Resolve-SignToolPath
    $signArgs = @(
        "sign",
        "/fd", "SHA256",
        "/f", $resolvedCertificatePath,
        "/tr", $TimestampUrl,
        "/td", "SHA256"
    )

    $certificatePassword = if ([string]::IsNullOrWhiteSpace($CertificatePasswordEnvVar)) {
        ""
    }
    else {
        [Environment]::GetEnvironmentVariable($CertificatePasswordEnvVar)
    }

    if (-not [string]::IsNullOrWhiteSpace($certificatePassword)) {
        $signArgs += @("/p", $certificatePassword)
    }

    $signArgs += $ExecutablePath
    & $resolvedSignToolPath @signArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Code signing failed for $ExecutablePath."
    }

    return $true
}

function Get-DownloadUrl {
    param([string]$ArtifactPath)

    if ([string]::IsNullOrWhiteSpace($UpdateBaseUrl)) {
        return ""
    }

    return "$($UpdateBaseUrl.TrimEnd('/'))/$([System.IO.Path]::GetFileName($ArtifactPath))"
}

$signedArtifacts = New-Object System.Collections.Generic.List[string]
$releaseVersion = ""

foreach ($runtimeIdentifier in $RuntimeIdentifiers) {
    $publishArgs = @{
        Configuration = $Configuration
        RuntimeIdentifier = $runtimeIdentifier
        OutputRoot = $OutputRoot
        NoZip = $true
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

    if ([string]::IsNullOrWhiteSpace($releaseVersion)) {
        $releaseVersion = Get-ReleaseVersion $exePath
    }

    if (Invoke-CodeSigning $exePath) {
        $signedArtifacts.Add((Get-RelativePath $exePath)) | Out-Null
    }

    $zipPath = Join-Path $outputRootPath "ThinkComposer.WinUI-$Configuration-$runtimeIdentifier.zip"
    if (-not $NoZip) {
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $artifactPath "*") -DestinationPath $zipPath -Force
        Write-Host "Created $zipPath"
    }

    $primaryArtifactPath = if (-not $NoZip) { $zipPath } else { $exePath }
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $primaryArtifactPath
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

    $artifacts.Add([ordered]@{
        runtimeIdentifier = $runtimeIdentifier
        platform = $manifest.platform
        manifest = Get-RelativePath $manifestPath
        directory = Get-RelativePath $artifactPath
        primaryArtifact = Get-RelativePath $primaryArtifactPath
        downloadUrl = Get-DownloadUrl $primaryArtifactPath
        sha256 = $hash.Hash
    }) | Out-Null
}

$index = [ordered]@{
    product = "ThinkComposer.WinUI"
    version = $releaseVersion
    configuration = $Configuration
    updateChannel = $UpdateChannel
    updateBaseUrl = $UpdateBaseUrl
    commit = Get-GitCommit
    createdUtc = [DateTime]::UtcNow.ToString("o")
    signing = [ordered]@{
        enabled = -not [string]::IsNullOrWhiteSpace($CertificatePath)
        certificate = if ([string]::IsNullOrWhiteSpace($CertificatePath)) { "" } else { [System.IO.Path]::GetFileName($CertificatePath) }
        timestampUrl = if ([string]::IsNullOrWhiteSpace($CertificatePath)) { "" } else { $TimestampUrl }
        signedArtifacts = $signedArtifacts
    }
    artifacts = $artifacts
}

if (-not (Test-Path -LiteralPath $outputRootPath)) {
    New-Item -ItemType Directory -Path $outputRootPath -Force | Out-Null
}

$indexPath = Join-Path $outputRootPath "release-index.json"
$index | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $indexPath -Encoding UTF8
Write-Host "Created $(Get-RelativePath $indexPath)" -ForegroundColor Green
