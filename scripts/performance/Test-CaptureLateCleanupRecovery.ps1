<#
.SYNOPSIS
Live check that preview start initializes capture after a half-done teardown is finished late.
.DESCRIPTION
Launches the built app on a private automation pipe with
SUSSUDIO_CAPTURE_FAULT_PRE_CLEANUP_FAILURES=1, so the first capture teardown that
reaches the renderer handoff fails, as a renderer stop timeout does. Capture keeps
the half-done cleanup. Only a later preview stop finishes it, and that is the full
capture cleanup, which uninitializes capture. The view model must then say
initialized=False so the next start initializes instead of failing with
"Capture not initialized".

Steps, each judged from app log markers and `ssctl --json state`:
  faulted-stop        `flashback off` (so a preview stop tears the pipeline down), then
                      `preview stop`: the injected fault fails the stop, preview reports
                      stopped, and capture stays initialized.
  late-cleanup        `preview start` is refused ("Capture cleanup must complete"). The
                      renderer attach that follows fails too, and its startup-failure stop
                      finishes the cleanup (CAPTURE_RETAINED_CLEANUP_COMPLETED). Afterwards
                      capture is Uninitialized and the view model must agree.
  recovery-start      `preview start` initializes capture and reaches Rendering.
  plain-cycle-flashback-off / plain-cycle-flashback-on
                      ordinary stop + start cycles, before and after `flashback on`, that
                      must render without clearing the flag or re-initializing.
  close               window close exits the process.

-PreCleanupFailures 0 skips the fault and runs only the plain cycles (a control).
Writes result.json and the app log under artifacts/capture-late-cleanup/. Refuses to
start while any Sussudio instance runs: the app is single instance.
.EXAMPLE
./scripts/performance/Test-CaptureLateCleanupRecovery.ps1 -Label fix
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'late-cleanup',

    [ValidateSet(0, 1)]
    [int] $PreCleanupFailures = 1,

    [ValidateRange(5, 120)]
    [int] $StepTimeoutSeconds = 30,

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
    $OutputDirectory = Join-Path $repoRoot "artifacts/capture-late-cleanup/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}

$running = @(Get-Process -Name Sussudio -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw "Sussudio is already running (PID $($running.Id -join ', ')); it owns the capture device. Close it first."
}

$pipeName = "SussudioAutomation.late-cleanup-$PID"
$logPath = Join-Path $repoRoot 'temp/logs/Sussudio_Debug.log'
$faultVariable = 'SUSSUDIO_CAPTURE_FAULT_PRE_CLEANUP_FAILURES'
$markerPattern = 'CAPTURE_FAULT_PRE_CLEANUP_FAILED|CAPTURE_RETAINED_CLEANUP_|PREVIEW_STOP_CAPTURE_UNINITIALIZED|CAPTURE_INIT|PREVIEW_START requested|PREVIEW_START_FAILURE_STOP|PREVIEW_RENDERER_ATTACH_FAILED|PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT|CAP-COORD|Capture not initialized|Capture cleanup must complete'

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

function Wait-PreviewStopped {
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        $snapshot = Get-Snapshot
        if ($null -ne $snapshot -and !$snapshot.IsPreviewing) { return $snapshot }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $snapshot
}

function Wait-StartupOutcome {
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        $snapshot = Get-Snapshot
        if ($null -ne $snapshot -and $snapshot.PreviewStartupState -in @('Rendering', 'Failed')) { return $snapshot }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $snapshot
}

# The view model's initialized flag next to capture's own session state.
function Get-InitState {
    param($Snapshot)
    if ($null -eq $Snapshot) { return $null }
    [ordered]@{
        viewModelInitialized = [bool]$Snapshot.IsInitialized
        captureSessionState = [string]$Snapshot.SessionState
        isPreviewing = [bool]$Snapshot.IsPreviewing
        previewStartupState = $Snapshot.PreviewStartupState
    }
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
        startupState = $(if ($null -ne $outcome) { $outcome.PreviewStartupState } else { $null })
        firstFrameLogged = $firstFrame
        renderedDuringDwell = $renderedDuringDwell
        dwellSeconds = $DwellSeconds
        captureInitialized = Test-LogMarker -Window $window -Pattern 'CAPTURE_INIT_READY'
        flagClearedByStop = Test-LogMarker -Window $window -Pattern 'PREVIEW_STOP_CAPTURE_UNINITIALIZED'
        stateAfter = Get-InitState $after
        passed = $start.ExitCode -eq 0 -and $null -ne $outcome -and $outcome.PreviewStartupState -eq 'Rendering' -and
            $firstFrame -and $null -ne $renderedDuringDwell -and $renderedDuringDwell -gt 30
        markers = @($window | Where-Object { $_ -match $markerPattern })
    }
}

# An ordinary stop + start must render, and must neither clear the flag nor re-initialize capture.
function Invoke-PlainCycle {
    param([string] $Name)
    $startLine = @(Read-LogLines).Count
    $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
    $stopped = Wait-PreviewStopped
    $step = Invoke-RenderingStart -Name $Name -StartLine $startLine
    $step.stopExitCode = $stop.ExitCode
    $step.stateAfterStop = Get-InitState $stopped
    $step.passed = $step.passed -and $stop.ExitCode -eq 0 -and $null -ne $stopped -and !$stopped.IsPreviewing -and
        !$step.captureInitialized -and !$step.flagClearedByStop
    $steps.Add([pscustomobject]$step)
    Write-Host "${Name}: state=$($step.startupState) rendered=$($step.renderedDuringDwell) reinit=$($step.captureInitialized) passed=$($step.passed)"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$appliedEnvironment = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName }
if ($PreCleanupFailures -gt 0) {
    $appliedEnvironment[$faultVariable] = [string]$PreCleanupFailures
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
$launch = $null
$close = $null
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

    $launchOutcome = Wait-StartupOutcome
    $launch = [ordered]@{
        startupState = $(if ($null -ne $launchOutcome) { $launchOutcome.PreviewStartupState } else { $null })
        firstFrameLogged = Wait-LogMarker -StartLine 0 -Pattern 'PREVIEW_D3D11_FIRST_FRAME'
        faultFiredBeforeSteps = Test-LogMarker -Window (Read-LogLines) -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED'
        state = Get-InitState $launchOutcome
    }
    Write-Host "launch: state=$($launch.startupState) firstFrame=$($launch.firstFrameLogged)"
    if ($launch.startupState -ne 'Rendering') { throw "Launch preview did not reach Rendering ($($launch.startupState))." }
    if ($launch.faultFiredBeforeSteps) { throw 'The injected fault fired during launch, before the faulted stop.' }
    Start-Sleep -Seconds 2

    $flashbackOff = Invoke-Ssctl -Arguments @('flashback', 'off') -TimeoutMs 30000
    if ($flashbackOff.ExitCode -ne 0) { throw "flashback off failed: $($flashbackOff.Output)" }

    if ($PreCleanupFailures -gt 0) {
        # faulted-stop: the stop tears the pipeline down, and the fault fails its renderer handoff.
        $startLine = @(Read-LogLines).Count
        $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
        $faultFired = Wait-LogMarker -StartLine $startLine -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED'
        $stopped = Wait-PreviewStopped
        $window = Get-LogWindow -StartLine $startLine
        $state = Get-InitState $stopped
        $step = [ordered]@{
            name = 'faulted-stop'
            stopExitCode = $stop.ExitCode
            stopOutput = $stop.Output
            faultFired = $faultFired
            stopFailed = Test-LogMarker -Window $window -Pattern 'CAP-COORD-FAIL\] StopVideoPreview'
            stateAfter = $state
            markers = @($window | Where-Object { $_ -match $markerPattern })
        }
        $step.passed = $stop.ExitCode -ne 0 -and $faultFired -and $step.stopFailed -and $null -ne $state -and
            !$state.isPreviewing -and $state.viewModelInitialized -and $state.captureSessionState -ne 'Uninitialized'
        $steps.Add([pscustomobject]$step)
        Write-Host "faulted-stop: fault=$faultFired stopFailed=$($step.stopFailed) passed=$($step.passed)"

        # late-cleanup: the refused start's failure stop finishes the full cleanup; the flag must follow.
        $startLine = @(Read-LogLines).Count
        $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
        $cleanupCompleted = Wait-LogMarker -StartLine $startLine -Pattern 'CAPTURE_RETAINED_CLEANUP_COMPLETED'
        $failureStopCompleted = Wait-LogMarker -StartLine $startLine -Pattern 'PREVIEW_START_FAILURE_STOP completed'
        $settled = Wait-PreviewStopped
        Start-Sleep -Milliseconds 500
        $settled = Get-Snapshot
        $window = Get-LogWindow -StartLine $startLine
        $state = Get-InitState $settled
        $step = [ordered]@{
            name = 'late-cleanup'
            startExitCode = $start.ExitCode
            startOutput = $start.Output
            startRefusedForCleanup = $start.Output -match 'Capture cleanup must complete'
            retainedCleanupCompleted = $cleanupCompleted
            failureStopCompleted = $failureStopCompleted
            flagClearedByStop = Test-LogMarker -Window $window -Pattern 'PREVIEW_STOP_CAPTURE_UNINITIALIZED'
            stateAfter = $state
            markers = @($window | Where-Object { $_ -match $markerPattern })
        }
        $step.passed = $start.ExitCode -ne 0 -and $step.startRefusedForCleanup -and $cleanupCompleted -and
            $failureStopCompleted -and $null -ne $state -and !$state.isPreviewing -and
            $state.captureSessionState -eq 'Uninitialized' -and !$state.viewModelInitialized -and $step.flagClearedByStop
        $steps.Add([pscustomobject]$step)
        Write-Host "late-cleanup: cleanup=$cleanupCompleted capture=$($state.captureSessionState) vmInitialized=$($state.viewModelInitialized) passed=$($step.passed)"

        # recovery-start: the next start must initialize capture and render.
        $startLine = @(Read-LogLines).Count
        $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
        $recovery.passed = $recovery.passed -and $recovery.captureInitialized
        $steps.Add([pscustomobject]$recovery)
        Write-Host "recovery-start: state=$($recovery.startupState) init=$($recovery.captureInitialized) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"
    }

    Invoke-PlainCycle -Name 'plain-cycle-flashback-off'
    $flashbackOn = Invoke-Ssctl -Arguments @('flashback', 'on') -TimeoutMs 30000
    if ($flashbackOn.ExitCode -ne 0) { throw "flashback on failed: $($flashbackOn.Output)" }
    Start-Sleep -Seconds 2
    Invoke-PlainCycle -Name 'plain-cycle-flashback-on'
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
    if (Test-Path -LiteralPath $logPath) {
        Copy-Item -LiteralPath $logPath -Destination (Join-Path $OutputDirectory 'Sussudio_Debug.log')
    }

    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $expectedSteps = $(if ($PreCleanupFailures -gt 0) { 5 } else { 2 })
    $closePassed = $null -ne $close -and $close.exitedWithin30s
    $allPassed = $steps.Count -eq $expectedSteps -and @($steps | Where-Object { !$_.passed }).Count -eq 0 -and $closePassed
    $report = [pscustomobject]@{
        schema = 'sussudio.capture-late-cleanup-recovery/v1'
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
        launch = $launch
        steps = $steps
        close = $close
        doesNotProve = 'The teardown failure is an injected exception at the capture pre-cleanup renderer handoff, not a real renderer stop timeout. Only the preview-stop path that finishes a retained cleanup is exercised; recording, reinit, and fatal capture cleanup are not. Judged from log markers and state counters only; not colour, HDR, audio, or recording.'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resultPath -Encoding utf8
    Write-Host "result: $resultPath passed=$allPassed ($(@($steps | Where-Object { $_.passed }).Count)/$expectedSteps steps)"
}

if (!$allPassed) { exit 1 }
