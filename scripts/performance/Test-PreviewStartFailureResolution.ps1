<#
.SYNOPSIS
Live check that a preview start which fails before its capture session runs resolves the startup attempt to Failed.
.DESCRIPTION
Launches the built app on a private automation pipe with
SUSSUDIO_CAPTURE_FAULT_PREVIEW_START_FAILURES=2, so the first two video-preview starts
fail inside the capture start transition, before any start work, the way a start on an
uninitialized capture does. No renderer ever attaches for those starts, so only the start's
own outcome can resolve its startup attempt. Before the fix the attempt stayed in
StartingSession indefinitely and the loading overlay kept spinning.

Steps, each judged from app log markers and `ssctl --json state`:
  launch-failed-start   the launch auto-start (device scan) hits fault 1 and must reach
                        Failed with a capture-start-failed reason, preview stopped and the
                        unavailable placeholder shown, within one step timeout. The state and
                        placeholder are read again after the fades settle, and a window
                        screenshot is saved next to result.json.
  manual-failed-start   `preview start` hits fault 2: the command fails with the injected
                        error and the snapshot must show a new attempt in Failed, within one
                        step timeout of issuing the command.
  recovery-start        `preview start` must reach Rendering and present frames.
  close                 window close exits the process.

-PreviewStartFailures 0 skips the fault: launch must render and a plain stop + start cycle
must render (a control). Writes result.json and the app log under artifacts/preview-start-failure/.
Refuses to start while any Sussudio instance runs: the app is single instance.
.EXAMPLE
./scripts/performance/Test-PreviewStartFailureResolution.ps1 -Label fix
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'preview-start-failure',

    [ValidateSet(0, 2)]
    [int] $PreviewStartFailures = 2,

    [ValidateRange(5, 120)]
    [int] $StepTimeoutSeconds = 15,

    [ValidateRange(1, 60)]
    [int] $DwellSeconds = 5,

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
    $OutputDirectory = Join-Path $repoRoot "artifacts/preview-start-failure/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}

$running = @(Get-Process -Name Sussudio -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw "Sussudio is already running (PID $($running.Id -join ', ')); it owns the capture device. Close it first."
}

$pipeName = "SussudioAutomation.preview-start-failure-$PID"
# A per-run log root keeps earlier runs out of the log window and works for a build outside the repository.
$logRoot = Join-Path $OutputDirectory 'logs'
$logPath = Join-Path $logRoot 'Sussudio_Debug.log'
$faultVariable = 'SUSSUDIO_CAPTURE_FAULT_PREVIEW_START_FAILURES'
$faultMarker = 'CAPTURE_FAULT_PREVIEW_START_FAILED'
$markerPattern = 'CAPTURE_FAULT_PREVIEW_START_FAILED|PREVIEW_START requested|PREVIEW_START_ABORTED|PREVIEW_START_STATE|PREVIEW_START_REQUESTED|PREVIEW_START_FAILURE_IGNORED|PREVIEW_START_CANCELED|PREVIEW_SESSION_STARTED|PREVIEW_RENDERER_ATTACH_FAILED|PREVIEW_START_FAILURE_STOP|PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT|CAP-COORD-FAIL'

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

function Get-Snapshot {
    $state = Invoke-Ssctl -Arguments @('--json', 'state') -TimeoutMs 5000
    if ($state.ExitCode -ne 0) { return $null }
    return ($state.Output | ConvertFrom-Json).Snapshot
}

function Get-LogWindow {
    param([int] $StartLine)
    return @(Read-LogLines | Select-Object -Skip $StartLine)
}

function Test-LogMarker {
    param([string[]] $Window, [string] $Pattern)
    return @($Window | Where-Object { $_ -match $Pattern }).Count -gt 0
}

function Get-FaultCount {
    return @(Read-LogLines | Where-Object { $_ -match $faultMarker }).Count
}

# Polls the log after $StartLine until $Pattern appears or the step deadline passes.
function Wait-LogMarker {
    param([int] $StartLine, [string] $Pattern, [int] $TimeoutSeconds = $StepTimeoutSeconds)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $window = Get-LogWindow -StartLine $StartLine
        if (Test-LogMarker -Window $window -Pattern $Pattern) { return $true }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

# Polls until the startup attempt settles (Rendering or Failed), optionally on an attempt other
# than $NotAttemptId. Returns the last snapshot and how long the wait took.
function Wait-StartupOutcome {
    param([string] $NotAttemptId)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        $snapshot = Get-Snapshot
        if ($null -ne $snapshot -and $snapshot.PreviewStartupState -in @('Rendering', 'Failed') -and
            ([string]::IsNullOrEmpty($NotAttemptId) -or $snapshot.PreviewAttemptId -ne $NotAttemptId)) {
            break
        }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    [pscustomobject]@{ Snapshot = $snapshot; WaitedMs = [Math]::Round($watch.Elapsed.TotalMilliseconds) }
}

function Get-StartupState {
    param($Snapshot)
    if ($null -eq $Snapshot) { return $null }
    [ordered]@{
        previewStartupState = $Snapshot.PreviewStartupState
        attemptId = $Snapshot.PreviewAttemptId
        lastFailureReason = $Snapshot.PreviewLastFailureReason
        isPreviewing = [bool]$Snapshot.IsPreviewing
        viewModelInitialized = [bool]$Snapshot.IsInitialized
        captureSessionState = [string]$Snapshot.SessionState
        placeholderVisible = [bool]$Snapshot.PreviewPlaceholderVisible
        statusText = $Snapshot.StatusText
    }
}

# Milliseconds between the first log line matching $FromPattern and the first later line matching
# $ToPattern, from the log's own [HH:mm:ss.fff] stamps.
function Measure-LogInterval {
    param([string[]] $Window, [string] $FromPattern, [string] $ToPattern)
    $from = $null
    foreach ($line in $Window) {
        if ($line -notmatch '^\[(\d\d:\d\d:\d\d\.\d\d\d)\]') { continue }
        $stamp = [TimeSpan]::ParseExact($Matches[1], 'hh\:mm\:ss\.fff', [Globalization.CultureInfo]::InvariantCulture)
        if ($null -eq $from) {
            if ($line -match $FromPattern) { $from = $stamp }
            continue
        }
        if ($line -match $ToPattern) {
            $elapsed = ($stamp - $from).TotalMilliseconds
            if ($elapsed -lt 0) { $elapsed += [TimeSpan]::FromDays(1).TotalMilliseconds }
            return [Math]::Round($elapsed)
        }
    }
    return $null
}

# Judges a failed start: the attempt must be Failed with a capture start reason, preview stopped,
# placeholder shown, and the outcome reported once without an ignored outcome for this request.
# The placeholder is judged after the 200 ms fades settle: a start that fails inside the placeholder
# fade-out used to have that fade-out collapse the placeholder again after the reveal.
function Measure-FailedStart {
    param([string] $Name, $Outcome, [string[]] $Window, [int] $ExpectedFaultCount)
    $resolved = Get-StartupState $Outcome.Snapshot
    Start-Sleep -Milliseconds 800
    $state = Get-StartupState (Get-Snapshot)
    $screenshotPath = Join-Path $OutputDirectory "$Name.png"
    $screenshot = Invoke-Ssctl -Arguments @('screenshot', $screenshotPath) -TimeoutMs 10000
    $requestMatch = @($Window | Where-Object { $_ -match 'PREVIEW_START_ABORTED request=(\d+)' } | Select-Object -First 1)
    $requestId = $null
    if ($requestMatch.Count -gt 0 -and $requestMatch[0] -match 'PREVIEW_START_ABORTED request=(\d+)') { $requestId = $Matches[1] }
    $step = [ordered]@{
        name = $Name
        faultFired = Test-LogMarker -Window $Window -Pattern "$faultMarker count=$ExpectedFaultCount "
        startRequestId = $requestId
        startAbortedLogged = $null -ne $requestId
        failedStateLogged = Test-LogMarker -Window $Window -Pattern 'PREVIEW_START_STATE state=Failed .*reason=capture-start-failed:Injected fault'
        ignoredForRequest = $(if ($null -ne $requestId) { Test-LogMarker -Window $Window -Pattern "PREVIEW_START_FAILURE_IGNORED request=$requestId " } else { $false })
        rendererAttachAttempted = Test-LogMarker -Window $Window -Pattern 'PREVIEW_SESSION_STARTED'
        requestToFailedMs = Measure-LogInterval -Window $Window -FromPattern 'PREVIEW_START requested' -ToPattern 'PREVIEW_START_STATE state=Failed'
        waitedMs = $Outcome.WaitedMs
        stepTimeoutSeconds = $StepTimeoutSeconds
        stateWhenResolved = $resolved
        stateAfter = $state
        screenshot = $(if ($screenshot.ExitCode -eq 0) { Split-Path -Leaf $screenshotPath } else { "failed (exit $($screenshot.ExitCode)): $($screenshot.Output)" })
    }
    $step.passed = $null -ne $resolved -and $resolved.previewStartupState -eq 'Failed' -and
        $null -ne $state -and $state.previewStartupState -eq 'Failed' -and $state.attemptId -eq $resolved.attemptId -and
        [string]$state.lastFailureReason -like 'capture-start-failed:Injected fault*' -and
        !$state.isPreviewing -and $state.placeholderVisible -and
        $step.faultFired -and $step.startAbortedLogged -and $step.failedStateLogged -and !$step.ignoredForRequest -and
        !$step.rendererAttachAttempted -and $Outcome.WaitedMs -le ($StepTimeoutSeconds * 1000)
    return $step
}

# Starts preview and requires Rendering with frames presented after a dwell.
function Invoke-RenderingStart {
    param([string] $Name, [int] $StartLine)
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $outcome = Wait-StartupOutcome
    $firstFrame = Wait-LogMarker -StartLine $StartLine -Pattern 'PREVIEW_D3D11_FIRST_FRAME' -TimeoutSeconds 5
    $before = Get-Snapshot
    Start-Sleep -Seconds $DwellSeconds
    $after = Get-Snapshot
    $renderedDuringDwell = $null
    if ($null -ne $before -and $null -ne $after) {
        $renderedDuringDwell = [long]$after.PreviewD3DFramesRendered - [long]$before.PreviewD3DFramesRendered
    }
    $window = Get-LogWindow -StartLine $StartLine
    [ordered]@{
        name = $Name
        startExitCode = $start.ExitCode
        startOutput = $start.Output
        startupState = $(if ($null -ne $outcome.Snapshot) { $outcome.Snapshot.PreviewStartupState } else { $null })
        firstFrameLogged = $firstFrame
        renderedDuringDwell = $renderedDuringDwell
        dwellSeconds = $DwellSeconds
        faultFired = Test-LogMarker -Window $window -Pattern $faultMarker
        stateAfter = Get-StartupState $after
        passed = $start.ExitCode -eq 0 -and $null -ne $outcome.Snapshot -and $outcome.Snapshot.PreviewStartupState -eq 'Rendering' -and
            $firstFrame -and $null -ne $renderedDuringDwell -and $renderedDuringDwell -gt 30 -and
            !(Test-LogMarker -Window $window -Pattern $faultMarker)
        markers = @($window | Where-Object { $_ -match $markerPattern })
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$appliedEnvironment = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName; SUSSUDIO_LOG_ROOT = $logRoot }
if ($PreviewStartFailures -gt 0) {
    $appliedEnvironment[$faultVariable] = [string]$PreviewStartFailures
}
$savedEnvironment = @{}
foreach ($key in @($appliedEnvironment.Keys) + $faultVariable) {
    if ($savedEnvironment.ContainsKey($key)) { continue }
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
    $value = $null
    if ($appliedEnvironment.ContainsKey($key)) { $value = $appliedEnvironment[$key] }
    [Environment]::SetEnvironmentVariable($key, $value, 'Process')
}

$steps = New-Object System.Collections.Generic.List[object]
$app = $null
$close = $null
$allPassed = $false
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
        $probe = Invoke-Ssctl -Arguments @('state') -TimeoutMs 2000
        $ready = $probe.ExitCode -eq 0
    } while (!$ready -and !$app.HasExited -and [DateTime]::UtcNow -lt $deadline)
    # A stale ssctl is refused with a manifest mismatch; the last reply says so.
    if (!$ready) { throw "The app did not answer on its automation pipe within 90 s. Last ssctl reply (exit $($probe.ExitCode)): $($probe.Output)" }

    if ($PreviewStartFailures -gt 0) {
        # launch-failed-start: the device-scan auto-start hits fault 1 and no renderer attaches.
        $launchFault = Wait-LogMarker -StartLine 0 -Pattern $faultMarker -TimeoutSeconds 60
        $outcome = Wait-StartupOutcome
        $launchWindow = Read-LogLines
        $step = Measure-FailedStart -Name 'launch-failed-start' -Outcome $outcome -Window $launchWindow -ExpectedFaultCount 1
        $step.faultFiredWithin60s = $launchFault
        $step.faultsFiredSoFar = Get-FaultCount
        $step.passed = $step.passed -and $step.faultsFiredSoFar -eq 1
        $step.markers = @($launchWindow | Where-Object { $_ -match $markerPattern })
        $steps.Add([pscustomobject]$step)
        $launchAttempt = $(if ($null -ne $step.stateAfter) { $step.stateAfter.attemptId } else { $null })
        Write-Host "launch-failed-start: state=$($step.stateAfter.previewStartupState) reason=$($step.stateAfter.lastFailureReason) requestToFailedMs=$($step.requestToFailedMs) passed=$($step.passed)"
        Start-Sleep -Seconds 1

        # manual-failed-start: an automation start hits fault 2; the command fails and a new attempt fails with it.
        $startLine = @(Read-LogLines).Count
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
        $commandMs = [Math]::Round($watch.Elapsed.TotalMilliseconds)
        $atReturn = Get-StartupState (Get-Snapshot)
        $outcome = Wait-StartupOutcome -NotAttemptId $launchAttempt
        $window = Get-LogWindow -StartLine $startLine
        $step = Measure-FailedStart -Name 'manual-failed-start' -Outcome $outcome -Window $window -ExpectedFaultCount 2
        $step.startExitCode = $start.ExitCode
        $step.startOutput = $start.Output
        $step.commandMs = $commandMs
        $step.stateWhenCommandReturned = $atReturn
        $step.newAttempt = $null -ne $step.stateAfter -and $step.stateAfter.attemptId -ne $launchAttempt
        $step.passed = $step.passed -and $start.ExitCode -ne 0 -and $start.Output -match 'Injected fault' -and $step.newAttempt
        $step.markers = @($window | Where-Object { $_ -match $markerPattern })
        $steps.Add([pscustomobject]$step)
        Write-Host "manual-failed-start: exit=$($start.ExitCode) state=$($step.stateAfter.previewStartupState) atReturn=$($atReturn.previewStartupState) newAttempt=$($step.newAttempt) passed=$($step.passed)"

        # recovery-start: the fault budget is spent; the next start must render.
        $startLine = @(Read-LogLines).Count
        $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
        $steps.Add([pscustomobject]$recovery)
        Write-Host "recovery-start: state=$($recovery.startupState) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"
    }
    else {
        $outcome = Wait-StartupOutcome
        $launchWindow = Read-LogLines
        $step = [ordered]@{
            name = 'launch-renders'
            startupState = $(if ($null -ne $outcome.Snapshot) { $outcome.Snapshot.PreviewStartupState } else { $null })
            faultFired = Test-LogMarker -Window $launchWindow -Pattern $faultMarker
            stateAfter = Get-StartupState $outcome.Snapshot
        }
        $step.passed = $step.startupState -eq 'Rendering' -and !$step.faultFired
        $steps.Add([pscustomobject]$step)
        Write-Host "launch-renders: state=$($step.startupState) passed=$($step.passed)"
        Start-Sleep -Seconds 2

        $startLine = @(Read-LogLines).Count
        $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
        Start-Sleep -Seconds 1
        $cycle = Invoke-RenderingStart -Name 'plain-cycle' -StartLine $startLine
        $cycle.stopExitCode = $stop.ExitCode
        $cycle.passed = $cycle.passed -and $stop.ExitCode -eq 0
        $steps.Add([pscustomobject]$cycle)
        Write-Host "plain-cycle: state=$($cycle.startupState) rendered=$($cycle.renderedDuringDwell) passed=$($cycle.passed)"
    }
}
finally {
    if ($null -ne $app -and !$app.HasExited) {
        $closeLine = @(Read-LogLines).Count
        $closeExit = (Invoke-Ssctl -Arguments @('window', 'close')).ExitCode
        $exited = $app.WaitForExit(30000)
        if (!$exited) { Stop-Process -Id $app.Id -Force }
        $window = Get-LogWindow -StartLine $closeLine
        $close = [ordered]@{
            windowCloseExitCode = $closeExit
            exitedWithin30s = $exited
            markers = @($window | Where-Object { $_ -match $markerPattern })
        }
        Write-Host "close: exited=$exited"
    }
    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $expectedSteps = $(if ($PreviewStartFailures -gt 0) { 3 } else { 2 })
    $closePassed = $null -ne $close -and $close.exitedWithin30s
    $allPassed = $steps.Count -eq $expectedSteps -and @($steps | Where-Object { !$_.passed }).Count -eq 0 -and $closePassed
    $report = [pscustomobject]@{
        schema = 'sussudio.preview-start-failure-resolution/v1'
        label = $Label
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        commit = $commit
        trackedChangesPresent = $dirty
        appPath = $AppPath
        appDllSha256 = (Get-FileHash (Join-Path (Split-Path -Parent $AppPath) 'Sussudio.dll')).Hash
        environment = $appliedEnvironment
        passed = $allPassed
        stepsPassed = @($steps | Where-Object { $_.passed }).Count
        stepsRun = $steps.Count
        stepsExpected = $expectedSteps
        steps = $steps
        close = $close
        doesNotProve = 'The start failure is an injected exception at the top of the capture video-preview start transition, not a real device failure. Initialize failures, a cancelled start, a start superseded by a newer request, and the reinitialize device-busy retry are not exercised live (the xUnit suite covers them in isolation). Judged from log markers and snapshot state only; the placeholder check reads its Visibility, not its opacity or the rendered pixels (the saved window screenshots are for a human to look at, not judged).'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resultPath -Encoding utf8
    Write-Host "result: $resultPath passed=$allPassed ($(@($steps | Where-Object { $_.passed }).Count)/$expectedSteps steps)"
}

if (!$allPassed) { exit 1 }
