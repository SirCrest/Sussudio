<#
.SYNOPSIS
Live check that the "capture device stays in use" hint follows whether Flashback is buffering.
.DESCRIPTION
The unavailable-preview placeholder carries FlashbackKeepAliveHintText ("Flashback is still
buffering, so the capture device stays in use"). It must show only while the Flashback backend
buffers, which holds capture open after preview stops, and follow that state when it changes,
whether or not the placeholder is being revealed.

Launches the built app on a private automation pipe with
SUSSUDIO_CAPTURE_FAULT_PREVIEW_START_FAILURES=2 and Flashback enabled (its default). Each step
samples the hint through `ssctl ui tree` for -SampleWindowMs after its command returns:
  launch-failed-start    the launch auto-start fails before Flashback ever buffers: hidden.
  manual-failed-start    `preview start` fails the same way: hidden.
  recovery-start         `preview start` renders and Flashback starts buffering.
  user-stop              `preview stop` keeps capture open for Flashback: visible.
  disable-while-stopped  `flashback disable` releases capture with the placeholder up: hidden,
                         with no placeholder reveal to refresh it.
  enable-without-capture `flashback enable` with nothing open buffers nothing: hidden.
  fresh-start-stop       a start that begins Flashback buffering anew, stopped as soon as the
                         snapshot reports it buffering: visible from the first sample.
A hidden hint is judged from the element's own Visibility (its own Collapsed reason), so a
placeholder mid-fade is not mistaken for a hidden hint.
Writes result.json and the app log under artifacts/flashback-keepalive-hint/. Refuses to start
while any Sussudio instance runs: the app is single instance and the capture device is shared.
.EXAMPLE
./scripts/performance/Test-FlashbackKeepAliveHint.ps1 -Label fix
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'flashback-keepalive-hint',

    [ValidateRange(5, 120)]
    [int] $StepTimeoutSeconds = 15,

    [ValidateRange(1, 60)]
    [int] $DwellSeconds = 3,

    [ValidateRange(500, 10000)]
    [int] $SampleWindowMs = 1500,

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
    $OutputDirectory = Join-Path $repoRoot "artifacts/flashback-keepalive-hint/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}

$running = @(Get-Process -Name Sussudio -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw "Sussudio is already running (PID $($running.Id -join ', ')); it owns the capture device. Close it first."
}

$pipeName = "SussudioAutomation.flashback-keepalive-hint-$PID"
$logRoot = Join-Path $OutputDirectory 'logs'
$logPath = Join-Path $logRoot 'Sussudio_Debug.log'
$faultVariable = 'SUSSUDIO_CAPTURE_FAULT_PREVIEW_START_FAILURES'
$faultMarker = 'CAPTURE_FAULT_PREVIEW_START_FAILED'
$hintId = 'FlashbackKeepAliveHintText'
$ownCollapsedReason = 'the element has Visibility Collapsed'
$markerPattern = 'CAPTURE_FAULT_PREVIEW_START_FAILED|PREVIEW_START_STATE state=(Failed|Rendering)|PREVIEW_STOP keep_pipeline_alive|FLASHBACK_BUFFER_ACTIVE_SEGMENT_START|FLASHBACK_PREVIEW_DISPOSE_OK|FLASHBACK_ENABLE'

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

function Get-LogWindow {
    param([int] $StartLine)
    return @(Read-LogLines | Select-Object -Skip $StartLine)
}

function Test-LogMarker {
    param([string[]] $Window, [string] $Pattern)
    return @($Window | Where-Object { $_ -match $Pattern }).Count -gt 0
}

function Wait-LogMarker {
    param([int] $StartLine, [string] $Pattern, [int] $TimeoutSeconds = $StepTimeoutSeconds)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (Test-LogMarker -Window (Get-LogWindow -StartLine $StartLine) -Pattern $Pattern) { return $true }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

function Get-Snapshot {
    $state = Invoke-Ssctl -Arguments @('--json', 'state') -TimeoutMs 5000
    if ($state.ExitCode -ne 0) { return $null }
    return ($state.Output | ConvertFrom-Json).Snapshot
}

function Get-FlashbackEnabledSetting {
    $options = Invoke-Ssctl -Arguments @('--json', 'options') -TimeoutMs 5000
    if ($options.ExitCode -ne 0) { return $null }
    return [bool](($options.Output | ConvertFrom-Json).Data.IsFlashbackEnabled)
}

# One read of the hint element: its effective visibility and, when hidden, which layer hides it.
function Get-HintSample {
    param([Diagnostics.Stopwatch] $Clock)
    $tree = Invoke-Ssctl -Arguments @('--json', 'ui', 'tree', '--root', $hintId, '--all') -TimeoutMs 5000
    $atMs = [Math]::Round($Clock.Elapsed.TotalMilliseconds)
    if ($tree.ExitCode -ne 0) {
        return [pscustomobject]@{ atMs = $atMs; read = $false; visible = $null; ownCollapsed = $null; hiddenReason = $tree.Output }
    }
    $node = @(($tree.Output | ConvertFrom-Json).Data.Roots)[0]
    $reason = $null
    if ($node.PSObject.Properties.Name -contains 'HiddenReason') { $reason = [string]$node.HiddenReason }
    [pscustomobject]@{
        atMs = $atMs
        read = $true
        visible = [bool]$node.IsVisible
        ownCollapsed = $reason -eq $ownCollapsedReason
        hiddenReason = $reason
    }
}

# Samples the hint from now until -SampleWindowMs has passed on $Clock's current reading.
function Get-HintSamples {
    param([Diagnostics.Stopwatch] $Clock)
    $until = $Clock.Elapsed.TotalMilliseconds + $SampleWindowMs
    $samples = New-Object System.Collections.Generic.List[object]
    do {
        $samples.Add((Get-HintSample -Clock $Clock))
    } while ($Clock.Elapsed.TotalMilliseconds -lt $until)
    return $samples.ToArray()
}

# Hidden: no sample shows the hint, and the last one finds the element itself collapsed rather than
# only an ancestor (the placeholder mid-fade) hiding it.
function Test-HintHidden {
    param([object[]] $Samples)
    $unread = @($Samples | Where-Object { !$_.read }).Count
    $shown = @($Samples | Where-Object { $_.read -and $_.visible }).Count
    return $Samples.Count -gt 0 -and $unread -eq 0 -and $shown -eq 0 -and $Samples[-1].ownCollapsed
}

# Visible: the element itself is never collapsed, and the last sample shows it.
function Test-HintVisible {
    param([object[]] $Samples)
    $unread = @($Samples | Where-Object { !$_.read }).Count
    $collapsed = @($Samples | Where-Object { $_.read -and $_.ownCollapsed }).Count
    return $Samples.Count -gt 0 -and $unread -eq 0 -and $collapsed -eq 0 -and $Samples[-1].visible
}

function Get-StepState {
    $snapshot = Get-Snapshot
    if ($null -eq $snapshot) { return $null }
    [ordered]@{
        previewStartupState = $snapshot.PreviewStartupState
        attemptId = $snapshot.PreviewAttemptId
        lastFailureReason = $snapshot.PreviewLastFailureReason
        isPreviewing = [bool]$snapshot.IsPreviewing
        placeholderVisible = [bool]$snapshot.PreviewPlaceholderVisible
        flashbackActive = [bool]$snapshot.FlashbackActive
        flashbackEnabledSetting = Get-FlashbackEnabledSetting
        statusText = $snapshot.StatusText
    }
}

function Wait-Snapshot {
    param([scriptblock] $Until)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        $snapshot = Get-Snapshot
        if ($null -ne $snapshot -and (& $Until $snapshot)) { break }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    [pscustomobject]@{ Snapshot = $snapshot; WaitedMs = [Math]::Round($watch.Elapsed.TotalMilliseconds) }
}

function New-Step {
    param([string] $Name, [string] $Expect, [object[]] $Samples, $State, [string[]] $Window)
    [ordered]@{
        name = $Name
        expectHint = $Expect
        hintSamples = $Samples
        hintJudged = $(if ($Expect -eq 'hidden') { Test-HintHidden $Samples } else { Test-HintVisible $Samples })
        state = $State
        markers = @($Window | Where-Object { $_ -match $markerPattern })
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$appliedEnvironment = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName; SUSSUDIO_LOG_ROOT = $logRoot; $faultVariable = '2' }
$savedEnvironment = @{}
foreach ($key in @($appliedEnvironment.Keys)) {
    $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
    [Environment]::SetEnvironmentVariable($key, $appliedEnvironment[$key], 'Process')
}

$steps = New-Object System.Collections.Generic.List[object]
$app = $null
$close = $null
$allPassed = $false
$expectedSteps = 7
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
    if (!$ready) { throw "The app did not answer on its automation pipe within 90 s. Last ssctl reply (exit $($probe.ExitCode)): $($probe.Output)" }

    # launch-failed-start: the device-scan auto-start hits fault 1 before Flashback ever buffers.
    $faultFired = Wait-LogMarker -StartLine 0 -Pattern "$faultMarker count=1 " -TimeoutSeconds 60
    $outcome = Wait-Snapshot -Until { param($s) $s.PreviewStartupState -eq 'Failed' }
    Start-Sleep -Milliseconds 800
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $window = Read-LogLines
    $step = New-Step -Name 'launch-failed-start' -Expect 'hidden' -Samples $samples -State (Get-StepState) -Window $window
    $step.faultFired = $faultFired
    $step.waitedForFailedMs = $outcome.WaitedMs
    $step.bufferedBefore = Test-LogMarker -Window $window -Pattern 'FLASHBACK_BUFFER_ACTIVE_SEGMENT_START'
    $step.passed = $faultFired -and $step.hintJudged -and $null -ne $step.state -and
        $step.state.previewStartupState -eq 'Failed' -and !$step.state.isPreviewing -and $step.state.placeholderVisible -and
        !$step.state.flashbackActive -and $step.state.flashbackEnabledSetting -eq $true -and !$step.bufferedBefore
    $steps.Add([pscustomobject]$step)
    Write-Host "launch-failed-start: hintHidden=$($step.hintJudged) flashbackActive=$($step.state.flashbackActive) passed=$($step.passed)"
    $launchAttempt = $(if ($null -ne $step.state) { $step.state.attemptId } else { $null })

    # manual-failed-start: an automation start hits fault 2, sampled from the moment the command returns.
    $startLine = @(Read-LogLines).Count
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $outcome = Wait-Snapshot -Until { param($s) $s.PreviewStartupState -eq 'Failed' -and $s.PreviewAttemptId -ne $launchAttempt }
    $window = Get-LogWindow -StartLine $startLine
    $step = New-Step -Name 'manual-failed-start' -Expect 'hidden' -Samples $samples -State (Get-StepState) -Window $window
    $step.startExitCode = $start.ExitCode
    $step.startOutput = $start.Output
    $step.faultFired = Test-LogMarker -Window $window -Pattern "$faultMarker count=2 "
    $step.passed = $start.ExitCode -ne 0 -and $start.Output -match 'Injected fault' -and $step.faultFired -and $step.hintJudged -and
        $null -ne $step.state -and $step.state.previewStartupState -eq 'Failed' -and $step.state.attemptId -ne $launchAttempt -and
        $step.state.placeholderVisible -and !$step.state.flashbackActive -and $step.state.flashbackEnabledSetting -eq $true
    $steps.Add([pscustomobject]$step)
    Write-Host "manual-failed-start: exit=$($start.ExitCode) hintHidden=$($step.hintJudged) passed=$($step.passed)"

    # recovery-start: the fault budget is spent; preview renders and Flashback starts buffering.
    $startLine = @(Read-LogLines).Count
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $outcome = Wait-Snapshot -Until { param($s) $s.PreviewStartupState -eq 'Rendering' -and $s.FlashbackActive }
    $segmentStarted = Wait-LogMarker -StartLine $startLine -Pattern 'FLASHBACK_BUFFER_ACTIVE_SEGMENT_START' -TimeoutSeconds 10
    Start-Sleep -Seconds $DwellSeconds
    $window = Get-LogWindow -StartLine $startLine
    $state = Get-StepState
    $step = [ordered]@{
        name = 'recovery-start'
        startExitCode = $start.ExitCode
        waitedForBufferingMs = $outcome.WaitedMs
        segmentStarted = $segmentStarted
        state = $state
        markers = @($window | Where-Object { $_ -match $markerPattern })
    }
    $step.passed = $start.ExitCode -eq 0 -and $segmentStarted -and $null -ne $state -and
        $state.previewStartupState -eq 'Rendering' -and $state.isPreviewing -and $state.flashbackActive -and
        !(Test-LogMarker -Window $window -Pattern $faultMarker)
    $steps.Add([pscustomobject]$step)
    Write-Host "recovery-start: state=$($state.previewStartupState) flashbackActive=$($state.flashbackActive) passed=$($step.passed)"

    # user-stop: a plain stop keeps capture open for Flashback, so the revealed placeholder carries the hint.
    $startLine = @(Read-LogLines).Count
    $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $window = Get-LogWindow -StartLine $startLine
    $step = New-Step -Name 'user-stop' -Expect 'visible' -Samples $samples -State (Get-StepState) -Window $window
    $step.stopExitCode = $stop.ExitCode
    $step.keptCaptureForFlashback = Test-LogMarker -Window $window -Pattern 'PREVIEW_STOP keep_pipeline_alive=1 .*flashback_alive=True'
    $step.passed = $stop.ExitCode -eq 0 -and $step.keptCaptureForFlashback -and $step.hintJudged -and $null -ne $step.state -and
        !$step.state.isPreviewing -and $step.state.placeholderVisible -and $step.state.flashbackActive
    $steps.Add([pscustomobject]$step)
    Write-Host "user-stop: hintVisible=$($step.hintJudged) flashbackActive=$($step.state.flashbackActive) passed=$($step.passed)"

    # disable-while-stopped: the placeholder stays up and Flashback releases capture; nothing reveals
    # the placeholder again, so only the buffering signal can hide the hint.
    $startLine = @(Read-LogLines).Count
    $disable = Invoke-Ssctl -Arguments @('flashback', 'disable') -TimeoutMs 30000
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $window = Get-LogWindow -StartLine $startLine
    $step = New-Step -Name 'disable-while-stopped' -Expect 'hidden' -Samples $samples -State (Get-StepState) -Window $window
    $step.disableExitCode = $disable.ExitCode
    $step.passed = $disable.ExitCode -eq 0 -and $step.hintJudged -and $null -ne $step.state -and
        !$step.state.isPreviewing -and $step.state.placeholderVisible -and !$step.state.flashbackActive -and
        $step.state.flashbackEnabledSetting -eq $false
    $steps.Add([pscustomobject]$step)
    Write-Host "disable-while-stopped: hintHidden=$($step.hintJudged) flashbackActive=$($step.state.flashbackActive) passed=$($step.passed)"

    # enable-without-capture: the setting turns on, but with preview off nothing opens capture.
    $startLine = @(Read-LogLines).Count
    $enable = Invoke-Ssctl -Arguments @('flashback', 'enable') -TimeoutMs 30000
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $window = Get-LogWindow -StartLine $startLine
    $step = New-Step -Name 'enable-without-capture' -Expect 'hidden' -Samples $samples -State (Get-StepState) -Window $window
    $step.enableExitCode = $enable.ExitCode
    $step.passed = $enable.ExitCode -eq 0 -and $step.hintJudged -and $null -ne $step.state -and
        !$step.state.isPreviewing -and $step.state.placeholderVisible -and !$step.state.flashbackActive -and
        $step.state.flashbackEnabledSetting -eq $true
    $steps.Add([pscustomobject]$step)
    Write-Host "enable-without-capture: hintHidden=$($step.hintJudged) flashbackActive=$($step.state.flashbackActive) passed=$($step.passed)"

    # fresh-start-stop: Flashback starts buffering anew with this start; stop as soon as the snapshot
    # reports it, so the hint cannot lean on an earlier buffering state.
    $startLine = @(Read-LogLines).Count
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $outcome = Wait-Snapshot -Until { param($s) $s.PreviewStartupState -eq 'Rendering' -and $s.FlashbackActive }
    $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $samples = Get-HintSamples -Clock $clock
    $window = Get-LogWindow -StartLine $startLine
    $step = New-Step -Name 'fresh-start-stop' -Expect 'visible' -Samples $samples -State (Get-StepState) -Window $window
    $step.startExitCode = $start.ExitCode
    $step.stopExitCode = $stop.ExitCode
    $step.waitedForBufferingMs = $outcome.WaitedMs
    $step.segmentStarted = Test-LogMarker -Window $window -Pattern 'FLASHBACK_BUFFER_ACTIVE_SEGMENT_START'
    $step.keptCaptureForFlashback = Test-LogMarker -Window $window -Pattern 'PREVIEW_STOP keep_pipeline_alive=1 .*flashback_alive=True'
    $step.passed = $start.ExitCode -eq 0 -and $stop.ExitCode -eq 0 -and $step.segmentStarted -and $step.keptCaptureForFlashback -and
        $step.hintJudged -and $null -ne $step.state -and !$step.state.isPreviewing -and $step.state.placeholderVisible -and
        $step.state.flashbackActive
    $steps.Add([pscustomobject]$step)
    Write-Host "fresh-start-stop: hintVisible=$($step.hintJudged) firstSample=$($samples[0].atMs)ms passed=$($step.passed)"
}
finally {
    if ($null -ne $app -and !$app.HasExited) {
        $closeLine = @(Read-LogLines).Count
        $closeExit = (Invoke-Ssctl -Arguments @('window', 'close')).ExitCode
        $exited = $app.WaitForExit(30000)
        if (!$exited) { Stop-Process -Id $app.Id -Force }
        $close = [ordered]@{
            # The app drops the pipe while closing, so ssctl reports a transport failure (exit 3);
            # the step is judged on the process exiting.
            windowCloseExitCode = $closeExit
            exitedWithin30s = $exited
        }
        Write-Host "close: exited=$exited"
    }
    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $closePassed = $null -ne $close -and $close.exitedWithin30s
    $stepsPassed = @($steps | Where-Object { $_.passed }).Count
    $allPassed = $steps.Count -eq $expectedSteps -and $stepsPassed -eq $expectedSteps -and $closePassed
    $report = [pscustomobject]@{
        schema = 'sussudio.flashback-keepalive-hint/v1'
        label = $Label
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        commit = $commit
        trackedChangesPresent = $dirty
        appPath = $AppPath
        appDllSha256 = (Get-FileHash (Join-Path (Split-Path -Parent $AppPath) 'Sussudio.dll')).Hash
        environment = $appliedEnvironment
        sampleWindowMs = $SampleWindowMs
        passed = $allPassed
        stepsPassed = $stepsPassed
        stepsRun = $steps.Count
        stepsExpected = $expectedSteps
        steps = $steps
        close = $close
        doesNotProve = 'Hint samples come from `ssctl ui tree`, one ssctl process per sample (roughly 100-300 ms apart), so a hint shown for less than one sample interval could be missed. The buffering state reaches the hint through MainViewModel.IsFlashbackBuffering, which the one-second lifecycle tick refreshes, so a change can show up to a second late; the steps sample after the state has settled and do not bound that lag. The start failures are an injected exception at the top of the capture video-preview start transition, not a real device failure. A Flashback backend failure or auto-restart while preview is stopped, a disable refused during recording, and a reinitialize whose restart fails are not exercised live. Visibility is read from the element tree, not from rendered pixels.'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resultPath -Encoding utf8
    Write-Host "result: $resultPath passed=$allPassed ($stepsPassed/$expectedSteps steps)"
}

if (!$allPassed) { exit 1 }
