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

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Test-PathInsideDirectory {
    param(
        [string]$DirectoryPath,
        [string]$ChildPath
    )

    $root = [System.IO.Path]::GetFullPath($DirectoryPath).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $child = [System.IO.Path]::GetFullPath($ChildPath)
    return $child.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-DirectoryHashManifest {
    param(
        [string]$ManifestPath,
        [string]$DirectoryPath
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Hash manifest not found: $ManifestPath"
    }

    $hashManifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    foreach ($file in @($hashManifest.files)) {
        $relativePath = [string]$file.path
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            throw "Hash manifest contains an empty path."
        }

        $filePath = Join-Path $DirectoryPath $relativePath
        if (-not (Test-PathInsideDirectory $DirectoryPath $filePath)) {
            throw "Hash manifest path escapes artifact directory: $relativePath"
        }
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            throw "Hash manifest file not found: $filePath"
        }

        $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $filePath
        if ($hash.Hash -ne $file.sha256) {
            throw "SHA256 mismatch for $relativePath."
        }
    }
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

$artifacts = @($index.artifacts)
if ($artifacts.Count -eq 0) {
    throw "Release index has no artifacts."
}
if ($RequireSigned -and -not $index.signing.enabled) {
    throw "Release index is not signed."
}

foreach ($artifact in $artifacts) {
    $artifactPath = Resolve-RepoPath $artifact.primaryArtifact

    if (-not (Test-Path -LiteralPath $artifactPath)) {
        throw "Release artifact not found: $artifactPath"
    }

    $primaryArtifactType = [string]$artifact.primaryArtifactType
    if ([string]::IsNullOrWhiteSpace($primaryArtifactType)) {
        $primaryArtifactType = if (Test-Path -LiteralPath $artifactPath -PathType Container) { "directory" } else { "file" }
    }

    if ($primaryArtifactType -eq "directory") {
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Container)) {
            throw "Directory artifact not found: $artifactPath"
        }
        if ([string]::IsNullOrWhiteSpace($artifact.hashManifest)) {
            throw "Directory artifact is missing hashManifest: $($artifact.primaryArtifact)"
        }

        Assert-DirectoryHashManifest (Resolve-RepoPath $artifact.hashManifest) $artifactPath
        continue
    }

    if ([string]::IsNullOrWhiteSpace($artifact.sha256)) {
        throw "Release artifact is missing sha256: $($artifact.primaryArtifact)"
    }

    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $artifactPath
    if ($hash.Hash -ne $artifact.sha256) {
        throw "SHA256 mismatch for $($artifact.primaryArtifact)."
    }

    if (-not [string]::IsNullOrWhiteSpace($artifact.hashManifest) -and -not [string]::IsNullOrWhiteSpace($artifact.directory)) {
        $directoryPath = Resolve-RepoPath $artifact.directory
        if (Test-Path -LiteralPath $directoryPath -PathType Container) {
            Assert-DirectoryHashManifest (Resolve-RepoPath $artifact.hashManifest) $directoryPath
        }
    }
}

if ($index.signing.enabled) {
    $signedArtifacts = @($index.signing.signedArtifacts)
    if ($signedArtifacts.Count -eq 0) {
        throw "Release index marks signing enabled but has no signed artifacts."
    }

    foreach ($signedArtifact in $signedArtifacts) {
        $signedArtifactPath = Resolve-RepoPath $signedArtifact

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
