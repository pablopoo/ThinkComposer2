param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$NoPublish,
    [string]$ExecutablePath = "",
    [string]$DocumentPath = "",
    [switch]$RequireWindow,
    [string]$WindowTitleContains = "ThinkComposer",
    [int]$StartupSeconds = 5
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$rid = "win-$($Platform.ToLowerInvariant())"
$project = Join-Path $repoRoot "ThinkComposer.WinUI\ThinkComposer.WinUI.csproj"
$publishDir = Join-Path $repoRoot "ThinkComposer.WinUI\bin\$Configuration\net10.0-windows10.0.26100.0\$rid\publish"
$exePath = if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    Join-Path $publishDir "ThinkComposer.WinUI.exe"
}
else {
    $ExecutablePath
}
$process = $null

if (-not $NoPublish -and [string]::IsNullOrWhiteSpace($ExecutablePath)) {
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

$argumentList = @()
if (-not [string]::IsNullOrWhiteSpace($DocumentPath)) {
    $resolvedDocumentPath = [System.IO.Path]::GetFullPath($DocumentPath)
    if (-not (Test-Path -LiteralPath $resolvedDocumentPath)) {
        throw "Startup document not found: $resolvedDocumentPath"
    }

    $argumentList += "`"$resolvedDocumentPath`""
}

try {
    $startOptions = @{
        FilePath = $exePath
        PassThru = $true
    }
    if ($argumentList.Count -gt 0) {
        $startOptions.ArgumentList = $argumentList
    }
    if (-not $RequireWindow) {
        $startOptions.WindowStyle = "Hidden"
    }

    $process = Start-Process @startOptions
    Start-Sleep -Seconds $StartupSeconds

    if ($process.HasExited) {
        throw "WinUI process exited during startup with code $($process.ExitCode)."
    }

    if ($RequireWindow) {
        $process.Refresh()
        if ($process.MainWindowHandle -eq 0) {
            throw "WinUI main window was not created."
        }

        if (-not [string]::IsNullOrWhiteSpace($WindowTitleContains) -and
            $process.MainWindowTitle -notlike "*$WindowTitleContains*") {
            throw "WinUI main window title '$($process.MainWindowTitle)' does not contain '$WindowTitleContains'."
        }
    }

    Write-Host "WinUI launch smoke passed. PID: $($process.Id)" -ForegroundColor Green
}
finally {
    if ($process -ne $null -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }
}
