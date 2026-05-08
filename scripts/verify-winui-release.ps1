param(
    [string]$IndexPath = "artifacts\winui\release-index.json",

    [switch]$RequireSigned
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedIndexPath = if ([System.IO.Path]::IsPathRooted($IndexPath)) {
    $IndexPath
}
else {
    Join-Path $repoRoot $IndexPath
}

if (-not (Test-Path -LiteralPath $resolvedIndexPath)) {
    throw "Release index not found: $resolvedIndexPath"
}

$index = Get-Content -LiteralPath $resolvedIndexPath -Raw | ConvertFrom-Json
if ($index.product -ne "ThinkComposer.WinUI") {
    throw "Unexpected product in release index: $($index.product)"
}
if ([string]::IsNullOrWhiteSpace($index.updateChannel)) {
    throw "Release index is missing updateChannel."
}
if ($index.artifacts.Count -eq 0) {
    throw "Release index has no artifacts."
}
if ($RequireSigned -and -not $index.signing.enabled) {
    throw "Release index is not signed."
}

foreach ($artifact in $index.artifacts) {
    $artifactPath = if ([System.IO.Path]::IsPathRooted($artifact.primaryArtifact)) {
        $artifact.primaryArtifact
    }
    else {
        Join-Path $repoRoot $artifact.primaryArtifact
    }

    if (-not (Test-Path -LiteralPath $artifactPath)) {
        throw "Release artifact not found: $artifactPath"
    }

    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $artifactPath
    if ($hash.Hash -ne $artifact.sha256) {
        throw "SHA256 mismatch for $($artifact.primaryArtifact)."
    }
}

if ($index.signing.enabled) {
    if ($index.signing.signedArtifacts.Count -eq 0) {
        throw "Release index marks signing enabled but has no signed artifacts."
    }

    foreach ($signedArtifact in $index.signing.signedArtifacts) {
        $signedArtifactPath = if ([System.IO.Path]::IsPathRooted($signedArtifact)) {
            $signedArtifact
        }
        else {
            Join-Path $repoRoot $signedArtifact
        }

        if (-not (Test-Path -LiteralPath $signedArtifactPath)) {
            throw "Signed artifact not found: $signedArtifactPath"
        }

        $signature = Get-AuthenticodeSignature -LiteralPath $signedArtifactPath
        if ($signature.Status -ne "Valid") {
            throw "Invalid signature for $signedArtifact ($($signature.Status))."
        }
    }
}

Write-Host "WinUI release verification passed: $resolvedIndexPath" -ForegroundColor Green
