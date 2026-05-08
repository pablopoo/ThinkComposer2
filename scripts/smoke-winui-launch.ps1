param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$NoPublish,
    [int]$StartupSeconds = 5
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$rid = "win-$($Platform.ToLowerInvariant())"
$project = Join-Path $repoRoot "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj"
$publishDir = Join-Path $repoRoot "ThinkComposer.WinUI\bin\$Configuration\net10.0-windows10.0.26100.0\$rid\publish"
$exePath = Join-Path $publishDir "ThinkComposer.WinUI.exe"
$process = $null

if (-not $NoPublish) {
    Push-Location $repoRoot
    try {
        dotnet publish $project -p:Configuration=$Configuration -p:Platform=$Platform -p:PublishProfile=$rid --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "WinUI publish failed."
        }
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Published executable not found: $exePath"
}

try {
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds $StartupSeconds

    if ($process.HasExited) {
        throw "WinUI process exited during startup with code $($process.ExitCode)."
    }

    Write-Host "WinUI launch smoke passed. PID: $($process.Id)" -ForegroundColor Green
}
finally {
    if ($process -ne $null -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }
}
