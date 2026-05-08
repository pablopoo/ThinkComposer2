param(
    [string]$ExecutablePath = "",
    [string]$DocumentPath = "",
    [int]$StartupSeconds = 6
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$defaultExePath = Join-Path $repoRoot "ThinkComposer.WinUI\bin\x86\Debug\net10.0-windows10.0.26100.0\win-x86\ThinkComposer.WinUI.exe"
$exePath = if ([string]::IsNullOrWhiteSpace($ExecutablePath)) { $defaultExePath } else { $ExecutablePath }
$process = $null

function Find-ElementByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-ElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Invoke-Element {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Name
    )

    if ($Element -eq $null) {
        throw "UI element not found: $Name"
    }

    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "WinUI executable not found: $exePath"
}

$argumentList = @()
if (-not [string]::IsNullOrWhiteSpace($DocumentPath)) {
    $resolvedDocumentPath = [System.IO.Path]::GetFullPath($DocumentPath)
    if (-not (Test-Path -LiteralPath $resolvedDocumentPath)) {
        throw "Startup document not found: $resolvedDocumentPath"
    }

    $argumentList += "`"$resolvedDocumentPath`""
}

Add-Type -AssemblyName UIAutomationClient

try {
    $startOptions = @{
        FilePath = $exePath
        PassThru = $true
    }
    if ($argumentList.Count -gt 0) {
        $startOptions.ArgumentList = $argumentList
    }

    $process = Start-Process @startOptions
    Start-Sleep -Seconds $StartupSeconds
    $process.Refresh()

    if ($process.HasExited) {
        throw "WinUI process exited during startup with code $($process.ExitCode)."
    }

    if ($process.MainWindowHandle -eq 0) {
        throw "WinUI main window was not created."
    }

    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    foreach ($name in @("New concept", "Save", "Export", "Report", "Present", "Print")) {
        if ((Find-ElementByName -Root $root -Name $name) -eq $null) {
            throw "Required toolbar command not found: $name"
        }
    }

    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ThemeToggleButton") -Name "Theme toggle"
    Start-Sleep -Milliseconds 300
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ThemeToggleButton") -Name "Theme toggle"
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ExplorerDomainTabButton") -Name "Domain tab"
    Start-Sleep -Milliseconds 300
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ExplorerContentTabButton") -Name "Content tab"
    Invoke-Element -Element (Find-ElementByName -Root $root -Name "New concept") -Name "New concept"

    Write-Host "WinUI UI smoke passed. PID: $($process.Id)" -ForegroundColor Green
}
finally {
    if ($process -ne $null -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }
}
