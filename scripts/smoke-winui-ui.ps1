param(
    [string]$ExecutablePath = "",
    [string]$DocumentPath = "",
    [string]$ExpectedTitle = "",
    [int]$StartupSeconds = 6
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$x64ExePath = Join-Path $repoRoot "ThinkComposer.WinUI\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\ThinkComposer.WinUI.exe"
$x86ExePath = Join-Path $repoRoot "ThinkComposer.WinUI\bin\x86\Debug\net10.0-windows10.0.26100.0\win-x86\ThinkComposer.WinUI.exe"
$defaultExePath = if (Test-Path -LiteralPath $x64ExePath) { $x64ExePath } else { $x86ExePath }
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

function Find-ElementByIdOrName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId,
        [string]$Name
    )

    $element = Find-ElementByAutomationId -Root $Root -AutomationId $AutomationId
    if ($element -ne $null) {
        return $element
    }

    return Find-ElementByName -Root $Root -Name $Name
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

    if (-not [string]::IsNullOrWhiteSpace($ExpectedTitle)) {
        $titleElement = Find-ElementByAutomationId -Root $root -AutomationId "CompositionTitleText"
        if ($titleElement -eq $null) {
            throw "Composition title element not found."
        }

        $actualTitle = $titleElement.Current.Name
        if ($actualTitle -notlike "*$ExpectedTitle*") {
            throw "Loaded document title '$actualTitle' does not contain '$ExpectedTitle'."
        }
    }

    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ThemeToggleButton") -Name "Theme toggle"
    Start-Sleep -Milliseconds 300
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ThemeToggleButton") -Name "Theme toggle"
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ExplorerDomainTabButton") -Name "Domain tab"
    Start-Sleep -Milliseconds 300
    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "ExplorerContentTabButton") -Name "Content tab"
    if ((Find-ElementByIdOrName -Root $root -AutomationId "ViewOptionsButton" -Name "View options") -eq $null) {
        throw "View options button not found."
    }

    Invoke-Element -Element (Find-ElementByName -Root $root -Name "New concept") -Name "New concept"
    Invoke-Element -Element (Find-ElementByIdOrName -Root $root -AutomationId "OpenDomainStudioExplorerButton" -Name "Domain Studio") -Name "Domain Studio"
    Start-Sleep -Milliseconds 300
    if ((Find-ElementByAutomationId -Root $root -AutomationId "DomainStudioTitle") -eq $null) {
        throw "Domain Studio did not open."
    }

    Invoke-Element -Element (Find-ElementByAutomationId -Root $root -AutomationId "CloseDomainStudioButton") -Name "Close Domain Studio"

    Write-Host "WinUI UI smoke passed. PID: $($process.Id)" -ForegroundColor Green
}
finally {
    if ($process -ne $null -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
    }
}
