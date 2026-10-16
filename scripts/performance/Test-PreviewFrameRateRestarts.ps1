<#
.SYNOPSIS
Live check that every frame-rate restart renders preview on the capture-shared device.
.DESCRIPTION
Launches the built app on a private automation pipe, waits for the first preview
visual, then alternates `ssctl set fps` between two rates. Each change reinitializes
the capture pipeline and creates a new D3D11 preview renderer, whose render thread
races the capture service's device hand-over. A step passes only when its renderer
logs D3D11_PREVIEW_DEVICE_CREATED shared=True last and reaches
PREVIEW_D3D11_FIRST_FRAME with no PREVIEW_START_TIMEOUT.

Writes result.json and the app log under artifacts/preview-shared-device-race/.
Refuses to start while any Sussudio instance runs: the app is single instance and
that instance owns the capture device. Needs a capture source that supports both
rates. -Environment passes extra variables to the app process only (for example a
fault-injection build's delay variables).
.EXAMPLE
./scripts/performance/Test-PreviewFrameRateRestarts.ps1 -Label fix -RoundTrips 5
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'round-trips',

    [ValidateRange(1, 50)]
    [int] $RoundTrips = 5,

    [ValidateCount(2, 2)]
    [int[]] $Rates = @(60, 120),

    [ValidateRange(5, 120)]
    [int] $StepTimeoutSeconds = 20,

    [ValidateRange(0, 30)]
    [int] $SettleSeconds = 2,

    [hashtable] $Environment = @{},

    [string] $AppPath,
    [string] $SsctlPath,
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ([string]::IsNullOrWhiteSpace($AppPath)) {
    $AppPath = Join-Path $repoRoot 'Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.exe'
}
if ([string]::IsNullOrWhiteSpace($SsctlPath)) {
    $SsctlPath = Join-Path $repoRoot 'tools/ssctl/bin/Debug/net8.0/ssctl.exe'
}
$AppPath = (Resolve-Path -LiteralPath $AppPath).Path
$SsctlPath = (Resolve-Path -LiteralPath $SsctlPath).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $runName = '{0}-{1}' -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), $Label
    $OutputDirectory = Join-Path $repoRoot "artifacts/preview-shared-device-race/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}

$running = @(Get-Process -Name Sussudio -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw "Sussudio is already running (PID $($running.Id -join ', ')); it owns the capture device. Close it first."
}

$pipeName = "SussudioAutomation.restarts-$PID"
$logPath = Join-Path $repoRoot 'temp/logs/Sussudio_Debug.log'
$markerPattern = 'D3D11_PREVIEW_RENDERER_START|D3D11_PREVIEW_DEVICE_CREATED|D3D11_PREVIEW_SHARED_DEVICE_HANDOVER_DURING_INIT|PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT|FAULT_INJECT_'

function Invoke-Ssctl {
    param([string[]] $Arguments, [int] $TimeoutMs = 10000)
    # Windows PowerShell turns redirected native stderr into a terminating error under 'Stop'.
    $ErrorActionPreference = 'Continue'
    $output = & $SsctlPath --pipe $pipeName --timeout $TimeoutMs @Arguments 2>&1
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output | Out-String).Trim() }
}

# The app writes its log with sharing enabled; read it the same way.
function Read-LogLines {
    if (!(Test-Path -LiteralPath $logPath)) { return @() }
    $stream = [IO.File]::Open($logPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        $reader = New-Object IO.StreamReader($stream)
        $text = $reader.ReadToEnd()
    }
    finally {
        $stream.Dispose()
    }
    return @($text -split "`r?`n")
}

function Get-SelectedFrameRate {
    $state = Invoke-Ssctl -Arguments @('--json', 'state')
    if ($state.ExitCode -ne 0) { throw "ssctl state failed: $($state.Output)" }
    $json = $state.Output | ConvertFrom-Json
    return [double]$json.Snapshot.SelectedFrameRate
}

# Waits for the first visual (or a start timeout) after line $StartLine and
# summarizes the renderer markers logged in that window.
function Wait-PreviewRestart {
    param([int] $StartLine)
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $window = @(Read-LogLines | Select-Object -Skip $StartLine)
        $done = @($window | Where-Object { $_ -match 'PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT attempt=' }).Count -gt 0
    } while (!$done -and [DateTime]::UtcNow -lt $deadline)

    $deviceLines = @($window | Where-Object { $_ -match 'D3D11_PREVIEW_DEVICE_CREATED' })
    $sharedValues = @($deviceLines | ForEach-Object { if ($_ -match 'shared=(True|False)') { $Matches[1] } })
    $lastShared = $null
    if ($sharedValues.Count -gt 0) { $lastShared = $sharedValues[$sharedValues.Count - 1] -eq 'True' }
    $firstFrame = @($window | Where-Object { $_ -match 'PREVIEW_D3D11_FIRST_FRAME' }) | Select-Object -First 1
    $timeout = @($window | Where-Object { $_ -match 'PREVIEW_START_TIMEOUT attempt=' }) | Select-Object -First 1
    [pscustomobject]@{
        rendererStarts = @($window | Where-Object { $_ -match 'D3D11_PREVIEW_RENDERER_START' }).Count
        deviceCreatedShared = $sharedValues
        lastDeviceShared = $lastShared
        handOversCaughtDuringInit = @($window | Where-Object { $_ -match 'D3D11_PREVIEW_SHARED_DEVICE_HANDOVER_DURING_INIT' }).Count
        inactiveSubmissionFailures = @($window | Where-Object { $_ -match 'Shared D3D11 device is not active' }).Count
        firstVisual = $null -ne $firstFrame
        startTimeout = $null -ne $timeout
        markers = @($window | Where-Object { $_ -match $markerPattern })
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$appliedEnvironment = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName }
foreach ($key in $Environment.Keys) { $appliedEnvironment[$key] = [string]$Environment[$key] }
$savedEnvironment = @{}
foreach ($key in $appliedEnvironment.Keys) {
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
    [Environment]::SetEnvironmentVariable($key, $appliedEnvironment[$key], 'Process')
}

$steps = New-Object System.Collections.Generic.List[object]
$startup = $null
$app = $null
$closeExitCode = $null
try {
    $app = Start-Process -FilePath $AppPath -WorkingDirectory (Split-Path -Parent $AppPath) -PassThru
}
finally {
    foreach ($key in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process')
    }
}

try {
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    do {
        Start-Sleep -Milliseconds 500
        $ready = (Invoke-Ssctl -Arguments @('state') -TimeoutMs 2000).ExitCode -eq 0
    } while (!$ready -and !$app.HasExited -and [DateTime]::UtcNow -lt $deadline)
    if (!$ready) { throw 'The app did not answer on its automation pipe within 90 s.' }

    $startup = Wait-PreviewRestart -StartLine 0
    Start-Sleep -Seconds $SettleSeconds

    # Start with the rate that differs from the current one so every step restarts preview.
    $current = Get-SelectedFrameRate
    $order = @($Rates)
    if ([Math]::Abs($current - $Rates[1]) -lt 1) { $order = @($Rates[0], $Rates[1]) }
    elseif ([Math]::Abs($current - $Rates[0]) -lt 1) { $order = @($Rates[1], $Rates[0]) }

    for ($trip = 1; $trip -le $RoundTrips; $trip++) {
        foreach ($rate in $order) {
            $startLine = @(Read-LogLines).Count
            $started = [DateTime]::UtcNow
            $set = Invoke-Ssctl -Arguments @('set', 'fps', [string]$rate)
            $result = Wait-PreviewRestart -StartLine $startLine
            $selected = Get-SelectedFrameRate
            $passed = $set.ExitCode -eq 0 -and $result.rendererStarts -ge 1 -and $result.lastDeviceShared -eq $true -and
                $result.firstVisual -and !$result.startTimeout -and [Math]::Abs($selected - $rate) -lt 1
            $steps.Add([pscustomobject]@{
                roundTrip = $trip
                requestedFps = $rate
                selectedFps = $selected
                ssctlExitCode = $set.ExitCode
                ssctlOutput = $set.Output
                elapsedMs = [int]([DateTime]::UtcNow - $started).TotalMilliseconds
                passed = $passed
                rendererStarts = $result.rendererStarts
                deviceCreatedShared = $result.deviceCreatedShared
                handOversCaughtDuringInit = $result.handOversCaughtDuringInit
                inactiveSubmissionFailures = $result.inactiveSubmissionFailures
                firstVisual = $result.firstVisual
                startTimeout = $result.startTimeout
                markers = $result.markers
            })
            Write-Host ("trip {0} fps {1}: passed={2} shared={3} firstVisual={4} caught={5}" -f $trip, $rate, $passed, ($result.deviceCreatedShared -join '/'), $result.firstVisual, $result.handOversCaughtDuringInit)
            Start-Sleep -Seconds $SettleSeconds
        }
    }
}
finally {
    if ($null -ne $app -and !$app.HasExited) {
        $closeExitCode = (Invoke-Ssctl -Arguments @('window', 'close')).ExitCode
        if (!$app.WaitForExit(30000)) {
            Stop-Process -Id $app.Id -Force
        }
    }
    if (Test-Path -LiteralPath $logPath) {
        Copy-Item -LiteralPath $logPath -Destination (Join-Path $OutputDirectory 'Sussudio_Debug.log')
    }

    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $allPassed = $steps.Count -eq ($RoundTrips * 2) -and @($steps | Where-Object { !$_.passed }).Count -eq 0
    $report = [pscustomobject]@{
        schema = 'sussudio.preview-frame-rate-restarts/v1'
        label = $Label
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        commit = $commit
        trackedChangesPresent = $dirty
        appPath = $AppPath
        environment = $appliedEnvironment
        rates = $Rates
        roundTrips = $RoundTrips
        passed = $allPassed
        stepsPassed = @($steps | Where-Object { $_.passed }).Count
        stepsRun = $steps.Count
        startup = $startup
        steps = $steps
        windowCloseExitCode = $closeExitCode
        doesNotProve = 'Checks renderer device selection and first visual from app log markers only; not frame pacing, color, HDR, audio, or recording.'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resultPath -Encoding utf8
    Write-Host "result: $resultPath passed=$allPassed ($(@($steps | Where-Object { $_.passed }).Count)/$($steps.Count) steps)"
}

if (!$allPassed) { exit 1 }
