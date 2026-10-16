<#
.SYNOPSIS
Live check that a start after a failed capture teardown finishes that teardown and starts preview.
.DESCRIPTION
Launches the built app on a private automation pipe with
SUSSUDIO_CAPTURE_FAULT_PRE_CLEANUP_FAILURES=2, so the first two capture renderer handoffs
fail, as a renderer stop timeout does. The first fails a teardown, which leaves the
preview pipeline retained (capture keeps _cleanupRequired set). The second fails the
first attempt to finish it. The third handoff passes.

A retained teardown must be finished as a teardown: capture stays initialized and the
Flashback session is retired (segments kept on disk with the retired-session marker,
reclaimed later by bounded startup cleanup), never escalated into a full cleanup that
uninitializes capture and purges the segments. A start or an initialize may finish it and
then run in the same transition. Nothing may attach a renderer while it is retained.

Scenarios, each its own app launch, judged from app log markers, `ssctl --json state` and
the Flashback session directory on disk:

  StopThenStart (Flashback off, so a preview stop tears the pipeline down; audio preview
  on for the scenario and restored afterwards, so the audio monitor runs into the stop)
    faulted-stop            `preview stop` fails on fault 1. Preview reports stopped, the
                            audio monitor stops with it, the view model stays initialized,
                            capture is Faulted, and no preview session starts (the old
                            spurious attach).
    failed-recovery-start   `preview start` finishes the retained teardown first and hits
                            fault 2. The start fails with that error, not "Capture cleanup
                            must complete". No new capture is created or initialized, no
                            preview session or audio monitor starts, no renderer attach
                            fails, and no startup-failure stop runs.
    recovery-start          `preview start` finishes the teardown (scope=PreviewPipeline,
                            capture still initialized), starts a fresh pipeline without
                            re-initializing capture, restarts the audio monitor, and renders.
    plain-cycle-flashback-off / plain-cycle-flashback-on
                            ordinary stop + start cycles, before and after `flashback on`.

  ReinitThenStart (Flashback on, so DVR segments are live at the failure)
    failed-reinit           `set fps <another rate>` reinitializes. Its teardown hits fault 1
                            and its failure-cleanup stop hits fault 2 while finishing it.
                            Preview stops, the view model is uninitialized, the selection is
                            rolled back to the starting rate, and the Flashback session
                            directory still holds its segments with no retired marker.
    recovery-start          `preview start` initializes; that transition finishes the
                            teardown first. The Flashback session is retired (purge=False),
                            its segment files are still on disk, preview renders, and the
                            starting rate is still selected.

  StartRollback (also SUSSUDIO_CAPTURE_FAULT_PREVIEW_INSTALL_FAILURES=1: the launch auto-start
  fails after its capture is running and before the pipeline installs it, so its rollback
  starts from a capture nothing else owns)
    failed-launch-start     The rollback's renderer handoff hits fault 1 after the install
                            fault. The start fails, preview stays stopped, the view model
                            stays initialized, capture is Faulted, and no preview session
                            starts. The half-started capture is retained, not orphaned.
    failed-recovery-start   As in StopThenStart: the start's finish hits fault 2, and no new
                            capture is created (no second open of the device).
    recovery-start          The finish stops and releases the half-started capture, then a
                            fresh capture opens the same device and renders, without a
                            capture re-initialize.
    plain-cycle-flashback-off / plain-cycle-flashback-on

  StartRollbackReleased (install fault 1, no handoff fault)
    failed-launch-start     The launch auto-start fails on the install fault; the rollback's
                            handoff passes and the rollback releases the capture itself.
    recovery-start          Nothing is retained, so nothing is finished; a fresh capture
                            opens the device and renders.
    plain-cycle-flashback-off

  AudioGraphRollback (SUSSUDIO_CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILURES=2, no handoff fault: the
  first two fresh starts fail where they would start the Flashback backend, after program audio
  and the microphone monitor started). WASAPI capture workers are counted two ways: the live
  "WASAPI Capture" threads in the app process, and the log's started/stopped balance.
    failed-launch-start     The launch auto-start (microphone as persisted) fails on fault 1
                            after its audio graph ran; the rollback leaves no capture worker.
    enable-mic              `set mic on` while preview is stopped.
    failed-mic-start        `preview start` starts the microphone monitor, then fails on fault 2.
                            The rollback stops the monitor (MIC_MONITOR_STOP after the fault),
                            nothing is retained, and no capture worker is left.
    recovery-start          A fresh start renders with exactly one microphone monitor: one new
                            MIC_MONITOR_START, a launch-wide mic balance of 1, and as many live
                            capture threads as the log balance says (no leaked worker).
    plain-cycle-flashback-off
                            A teardown stop and start still leave one microphone monitor.
    restore-mic             `set mic` back to the persisted value read before enable-mic.

  Control (no fault): the two plain cycles only.

-Scenario All runs StopThenStart, ReinitThenStart, StartRollback, StartRollbackReleased and
AudioGraphRollback.
Writes result.json and one app log per launch under artifacts/capture-late-cleanup/. Refuses
to start while any Sussudio instance runs: the app is single instance and owns the capture device.
.EXAMPLE
./scripts/performance/Test-CaptureLateCleanupRecovery.ps1 -Label fix
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'late-cleanup',

    [ValidateSet('All', 'StopThenStart', 'ReinitThenStart', 'StartRollback', 'StartRollbackReleased', 'AudioGraphRollback', 'Control')]
    [string] $Scenario = 'All',

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

# @() keeps a single scenario an array; $() would unroll it to a string with no Count under strict mode.
$scenarioNames = @(if ($Scenario -eq 'All') { 'StopThenStart', 'ReinitThenStart', 'StartRollback', 'StartRollbackReleased', 'AudioGraphRollback' } else { $Scenario })
$pipeName = "SussudioAutomation.late-cleanup-$PID"
# The app rotates its prior log at startup, so each launch starts a fresh file.
$logPath = Join-Path $repoRoot 'temp/logs/Sussudio_Debug.log'
$settingsPath = Join-Path $env:LOCALAPPDATA 'Sussudio/settings.json'
$faultVariable = 'SUSSUDIO_CAPTURE_FAULT_PRE_CLEANUP_FAILURES'
$installFaultVariable = 'SUSSUDIO_CAPTURE_FAULT_PREVIEW_INSTALL_FAILURES'
$flashbackFaultVariable = 'SUSSUDIO_CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILURES'
# Fault 1 fails the teardown (or the start rollback), fault 2 fails the first finish of it, the third handoff passes.
$faultFailures = 2
# Per scenario: renderer handoff faults, fresh-start install faults, fresh-start faults after the audio graph
# started, and whether the launch auto-start must fail.
$scenarioFaults = @{
    StopThenStart = @{ PreCleanup = $faultFailures; Install = 0; Flashback = 0; LaunchFails = $false }
    ReinitThenStart = @{ PreCleanup = $faultFailures; Install = 0; Flashback = 0; LaunchFails = $false }
    StartRollback = @{ PreCleanup = $faultFailures; Install = 1; Flashback = 0; LaunchFails = $true }
    StartRollbackReleased = @{ PreCleanup = 0; Install = 1; Flashback = 0; LaunchFails = $true }
    AudioGraphRollback = @{ PreCleanup = 0; Install = 0; Flashback = 2; LaunchFails = $true }
    Control = @{ PreCleanup = 0; Install = 0; Flashback = 0; LaunchFails = $false }
}
$retiredMarkerName = '.flashback-retired-session'
$micStartPattern = 'MIC_MONITOR_START device='
$micStopPattern = '\] MIC_MONITOR_STOP\s*$'
$markerPattern = 'CAPTURE_FAULT_PRE_CLEANUP_FAILED|CAPTURE_FAULT_PREVIEW_INSTALL_FAILED|CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILED|MIC_MONITOR_|Microphone capture dispose failed|WASAPI capture st(arted|opped)\.|WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT|Preview backend active|CAPTURE_RETAINED_CLEANUP_|PREVIEW_STOP_CAPTURE_UNINITIALIZED|CAPTURE_INIT|PREVIEW_START requested|PREVIEW_START phase=(create_uvc|started)|Unified preview start failed|Unified preview rollback dispose|PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE|UNIFIED_VIDEO_REINIT_RETIRE_SHARED_D3D_MANAGER|PREVIEW_SESSION_STARTED|PREVIEW_START_FAILURE_STOP|PREVIEW_RENDERER_ATTACH_FAILED|PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT|REINIT_FAILED_CLEANUP|FLASHBACK_BUFFER_INIT|FLASHBACK_BUFFER_CLEANUP_RETIRE|FLASHBACK_BUFFER_DISPOSE|FLASHBACK_PREVIEW_DISPOSE_OK|FLASHBACK_CACHE_BUDGET_CLEANUP|FLASHBACK_STALE_SESSION_CLEANUP|CAP-COORD|Capture not initialized|Capture cleanup must complete'
# A preview session that starts or a renderer that attaches while cleanup is retained.
$spuriousAttachPattern = 'PREVIEW_SESSION_STARTED|PREVIEW_RENDERER_ATTACH_FAILED|PREVIEW_START_FAILURE_STOP'

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

function Select-Markers {
    param([string[]] $Window)
    return @($Window | Where-Object { $_ -match $markerPattern })
}

# The index of the first line in $Window matching $Pattern, or -1; orders markers within a step.
function Get-MarkerIndex {
    param([string[]] $Window, [string] $Pattern)
    for ($i = 0; $i -lt $Window.Count; $i++) {
        if ($Window[$i] -match $Pattern) { return $i }
    }
    return -1
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

# A failed step leaves no state to poll for; give queued capture errors time to land on the UI.
function Get-SettledSnapshot {
    Start-Sleep -Milliseconds 1500
    return Get-Snapshot
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
        selectedFrameRate = $Snapshot.SelectedFrameRate
    }
}

# The session directory of the newest Flashback buffer the log has initialized.
function Get-FlashbackSessionDirectory {
    $match = @(Read-LogLines | Where-Object { $_ -match "FLASHBACK_BUFFER_INIT .*session_dir='([^']+)'" } | Select-Object -Last 1)
    if ($match.Count -eq 0) { return $null }
    if ($match[0] -match "session_dir='([^']+)'") { return $Matches[1] }
    return $null
}

# What is on disk for one Flashback session: segment files and the lifetime markers.
function Get-SegmentEvidence {
    param([string] $SessionDirectory)
    $exists = ![string]::IsNullOrWhiteSpace($SessionDirectory) -and (Test-Path -LiteralPath $SessionDirectory -PathType Container)
    [ordered]@{
        sessionDirectory = $SessionDirectory
        exists = $exists
        segmentFiles = $(if ($exists) { @(Get-ChildItem -LiteralPath $SessionDirectory -Filter 'fb_*' -File).Count } else { 0 })
        retiredMarker = $exists -and (Test-Path -LiteralPath (Join-Path $SessionDirectory $retiredMarkerName))
        capturedUtc = [DateTime]::UtcNow.ToString('o')
    }
}

function Wait-SegmentFiles {
    param([string] $SessionDirectory, [int] $Minimum)
    $deadline = [DateTime]::UtcNow.AddSeconds($StepTimeoutSeconds)
    do {
        $evidence = Get-SegmentEvidence -SessionDirectory $SessionDirectory
        if ($evidence.segmentFiles -ge $Minimum) { return $evidence }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    return $evidence
}

# An enabled frame rate other than the selected one, for a reinit that keeps the resolution.
function Select-OtherFrameRate {
    param([double] $Current)
    $options = Invoke-Ssctl -Arguments @('--json', 'options') -TimeoutMs 5000
    if ($options.ExitCode -ne 0) { return $null }
    $data = Get-JsonProperty ($options.Output | ConvertFrom-Json) 'Data'
    foreach ($rate in @(Get-JsonProperty $data 'FrameRates' @())) {
        $friendly = [double](Get-JsonProperty $rate 'FriendlyValue' 0)
        if ((Get-JsonProperty $rate 'IsEnabled' $true) -and !(Get-JsonProperty $rate 'IsSelected' $false) -and
            [double](Get-JsonProperty $rate 'Value' 0) -gt 0 -and [Math]::Abs($friendly - $Current) -ge 1) {
            return $friendly
        }
    }
    return $null
}

# .NET publishes Thread.Name as the OS thread description, so a WASAPI capture worker (program audio or the
# microphone monitor) is visible from outside the app as a thread named "WASAPI Capture".
if (-not ('SussudioThreadDescriptionProbe' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class SussudioThreadDescriptionProbe
{
    private const uint ThreadQueryLimitedInformation = 0x0800;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

    [DllImport("kernel32.dll")]
    private static extern int GetThreadDescription(IntPtr thread, out IntPtr description);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    // Null when the thread has exited or cannot be opened.
    public static string Get(int threadId)
    {
        IntPtr handle = OpenThread(ThreadQueryLimitedInformation, false, (uint)threadId);
        if (handle == IntPtr.Zero) return null;
        try
        {
            IntPtr text;
            if (GetThreadDescription(handle, out text) < 0) return null;
            try { return Marshal.PtrToStringUni(text); }
            finally { LocalFree(text); }
        }
        finally
        {
            CloseHandle(handle);
        }
    }
}
'@
}

function Get-WasapiCaptureThreadCount {
    if ($null -eq $script:app -or $script:app.HasExited) { return $null }
    $process = Get-Process -Id $script:app.Id -ErrorAction SilentlyContinue
    if ($null -eq $process) { return $null }
    return @($process.Threads | Where-Object { [SussudioThreadDescriptionProbe]::Get($_.Id) -eq 'WASAPI Capture' }).Count
}

# The WASAPI capture workers alive now, counted from the process threads and from the launch's log: capture
# workers started minus stopped, and microphone monitors started minus stopped. A worker whose stop timed out
# logs no "stopped" line, so it stays counted.
function Get-WasapiWorkerEvidence {
    $lines = @(Read-LogLines)
    $started = @($lines | Where-Object { $_ -match '\] WASAPI capture started\.' }).Count
    $stopped = @($lines | Where-Object { $_ -match '\] WASAPI capture stopped\.' }).Count
    $micStarts = @($lines | Where-Object { $_ -match $micStartPattern }).Count
    $micStops = @($lines | Where-Object { $_ -match $micStopPattern }).Count
    [ordered]@{
        captureThreads = Get-WasapiCaptureThreadCount
        logStarted = $started
        logStopped = $stopped
        logLive = $started - $stopped
        micStarts = $micStarts
        micStops = $micStops
        micLive = $micStarts - $micStops
        capturedUtc = [DateTime]::UtcNow.ToString('o')
    }
}

# A stopped worker's thread can outlive its "stopped" line by a moment; wait briefly for the two counts to agree.
function Wait-WasapiWorkerEvidence {
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        $evidence = Get-WasapiWorkerEvidence
        if ($evidence.captureThreads -eq $evidence.logLive) { return $evidence }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $evidence
}

# The persisted microphone toggle the app loaded at launch; false when unset.
function Get-PersistedMicrophoneEnabled {
    if (!(Test-Path -LiteralPath $settingsPath)) { return $false }
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    return [bool](Get-JsonProperty $settings 'IsMicrophoneEnabled' $false)
}

# Strict mode throws on a missing property; serializers may omit defaults.
function Get-JsonProperty {
    param($Object, [string] $Name, $Default = $null)
    if ($null -eq $Object) { return $Default }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $Default }
    return $property.Value
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
        attachFailed = Test-LogMarker -Window $window -Pattern 'PREVIEW_RENDERER_ATTACH_FAILED|PREVIEW_START_FAILURE_STOP'
        stateAfter = Get-InitState $after
        passed = $start.ExitCode -eq 0 -and $null -ne $outcome -and $outcome.PreviewStartupState -eq 'Rendering' -and
            $firstFrame -and $null -ne $renderedDuringDwell -and $renderedDuringDwell -gt 30
        markers = Select-Markers $window
    }
}

# An ordinary stop + start must render, and must neither clear the flag nor re-initialize capture.
# -RequireOneMicrophone also requires exactly one microphone monitor and no leaked capture worker afterwards.
function Invoke-PlainCycle {
    param([string] $Name, [switch] $RequireOneMicrophone)
    $startLine = @(Read-LogLines).Count
    $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
    $stopped = Wait-PreviewStopped
    $step = Invoke-RenderingStart -Name $Name -StartLine $startLine
    $step.stopExitCode = $stop.ExitCode
    $step.stateAfterStop = Get-InitState $stopped
    $step.passed = $step.passed -and $stop.ExitCode -eq 0 -and $null -ne $stopped -and !$stopped.IsPreviewing -and
        !$step.captureInitialized -and !$step.flagClearedByStop -and !$step.attachFailed
    if ($RequireOneMicrophone) {
        $step.workers = Wait-WasapiWorkerEvidence
        $step.passed = $step.passed -and $step.workers.micLive -eq 1 -and $step.workers.captureThreads -eq $step.workers.logLive
    }
    $steps.Add([pscustomobject]$step)
    Write-Host "${Name}: state=$($step.startupState) rendered=$($step.renderedDuringDwell) reinit=$($step.captureInitialized) passed=$($step.passed)"
}

function Invoke-FlashbackToggle {
    param([ValidateSet('on', 'off')] [string] $State)
    $toggle = Invoke-Ssctl -Arguments @('flashback', $State) -TimeoutMs 30000
    if ($toggle.ExitCode -ne 0) { throw "flashback $State failed: $($toggle.Output)" }
}

function Set-AudioPreview {
    param([ValidateSet('on', 'off')] [string] $State)
    $set = Invoke-Ssctl -Arguments @('set', 'audio-preview', $State) -TimeoutMs 30000
    if ($set.ExitCode -ne 0) { throw "set audio-preview $State failed: $($set.Output)" }
}

function Start-App {
    param([int] $FaultFailures, [int] $InstallFaultFailures, [int] $FlashbackFaultFailures, [switch] $ExpectLaunchFailure)
    $applied = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName }
    if ($FaultFailures -gt 0) { $applied[$faultVariable] = [string]$FaultFailures }
    if ($InstallFaultFailures -gt 0) { $applied[$installFaultVariable] = [string]$InstallFaultFailures }
    if ($FlashbackFaultFailures -gt 0) { $applied[$flashbackFaultVariable] = [string]$FlashbackFaultFailures }
    $saved = @{}
    foreach ($key in @($applied.Keys) + $faultVariable + $installFaultVariable + $flashbackFaultVariable) {
        if ($saved.ContainsKey($key)) { continue }
        $saved[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        $value = $null
        if ($applied.ContainsKey($key)) { $value = $applied[$key] }
        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    }
    try {
        $script:app = Start-Process -FilePath $AppPath -WorkingDirectory (Split-Path -Parent $AppPath) -PassThru
    }
    finally {
        foreach ($key in $saved.Keys) {
            [Environment]::SetEnvironmentVariable($key, $saved[$key], 'Process')
        }
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    do {
        Start-Sleep -Milliseconds 500
        $probe = Invoke-Ssctl -Arguments @('state') -TimeoutMs 2000
        $ready = $probe.ExitCode -eq 0
    } while (!$ready -and !$script:app.HasExited -and [DateTime]::UtcNow -lt $deadline)
    # A stale ssctl is refused with a manifest mismatch; the last reply says so.
    if (!$ready) { throw "The app did not answer on its automation pipe within 90 s. Last ssctl reply (exit $($probe.ExitCode)): $($probe.Output)" }

    if ($ExpectLaunchFailure) {
        # The launch auto-start is the first fresh start, so the injected start fault fails it; the scenario judges it.
        $failed = Wait-LogMarker -StartLine 0 -Pattern 'CAP-COORD-FAIL\] StartVideoPreview'
        $script:launch = [ordered]@{
            environment = $applied
            startFailureLogged = $failed
            state = Get-InitState (Get-SettledSnapshot)
        }
        Write-Host "launch: expected start failure logged=$failed"
        if (!$failed) { throw 'The launch auto-start did not fail on the injected start fault.' }
        return
    }

    $launchOutcome = Wait-StartupOutcome
    $launch = [ordered]@{
        environment = $applied
        startupState = $(if ($null -ne $launchOutcome) { $launchOutcome.PreviewStartupState } else { $null })
        firstFrameLogged = Wait-LogMarker -StartLine 0 -Pattern 'PREVIEW_D3D11_FIRST_FRAME'
        faultFiredBeforeSteps = Test-LogMarker -Window (Read-LogLines) -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED'
        state = Get-InitState $launchOutcome
    }
    Write-Host "launch: state=$($launch.startupState) firstFrame=$($launch.firstFrameLogged)"
    $script:launch = $launch
    if ($launch.startupState -ne 'Rendering') { throw "Launch preview did not reach Rendering ($($launch.startupState))." }
    if ($launch.faultFiredBeforeSteps) { throw 'The injected fault fired during launch, before the first step.' }
    Start-Sleep -Seconds 2
}

function Stop-App {
    param([string] $LogName)
    $close = $null
    if ($null -ne $script:app -and !$script:app.HasExited) {
        $closeLine = @(Read-LogLines).Count
        $closeExit = (Invoke-Ssctl -Arguments @('window', 'close')).ExitCode
        $exited = $script:app.WaitForExit(30000)
        if (!$exited) { Stop-Process -Id $script:app.Id -Force }
        $close = [ordered]@{
            windowCloseExitCode = $closeExit
            exitedWithin30s = $exited
            markers = Select-Markers (Get-LogWindow -StartLine $closeLine)
        }
        Write-Host "close: exited=$exited"
    }
    if (Test-Path -LiteralPath $logPath) {
        Copy-Item -LiteralPath $logPath -Destination (Join-Path $OutputDirectory $LogName)
    }
    return $close
}

function Invoke-StopThenStart {
    Invoke-FlashbackToggle -State off
    # Audio monitoring runs into the faulted stop, so the stop must be seen to end it. The
    # user's setting is put back afterwards.
    $initialAudioPreview = [bool](Get-JsonProperty (Get-Snapshot) 'IsAudioPreviewEnabled' $false)
    Set-AudioPreview -State on
    try {
        Invoke-StopThenStartSteps
    }
    finally {
        if (!$initialAudioPreview) {
            try { Set-AudioPreview -State off }
            catch { Write-Warning "Could not restore audio preview off: $($_.Exception.Message)" }
        }
    }
}

function Invoke-StopThenStartSteps {
    $audioMonitorStarted = Wait-LogMarker -StartLine 0 -Pattern 'WASAPI audio playback started\.'

    # faulted-stop: the stop tears the pipeline down, and fault 1 fails its renderer handoff.
    $startLine = @(Read-LogLines).Count
    $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
    $faultFired = Wait-LogMarker -StartLine $startLine -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED count=1 '
    $stopped = Wait-PreviewStopped
    $settled = Get-SettledSnapshot
    $window = Get-LogWindow -StartLine $startLine
    $state = Get-InitState $settled
    $step = [ordered]@{
        name = 'faulted-stop'
        stopExitCode = $stop.ExitCode
        stopOutput = $stop.Output
        faultFired = $faultFired
        stopFailed = Test-LogMarker -Window $window -Pattern 'CAP-COORD-FAIL\] StopVideoPreview'
        spuriousAttach = Test-LogMarker -Window $window -Pattern $spuriousAttachPattern
        audioMonitorStarted = $audioMonitorStarted
        audioMonitorStopped = Test-LogMarker -Window $window -Pattern 'PREVIEW_STOP_FAILED_AUDIO_MONITOR_STOPPED'
        audioPlaybackDisposed = Test-LogMarker -Window $window -Pattern 'WASAPI audio playback disposed\.'
        stateAfter = $state
        markers = Select-Markers $window
    }
    $step.passed = $stop.ExitCode -ne 0 -and $faultFired -and $step.stopFailed -and !$step.spuriousAttach -and
        $audioMonitorStarted -and $step.audioMonitorStopped -and $step.audioPlaybackDisposed -and
        $null -ne $stopped -and $null -ne $state -and !$state.isPreviewing -and $state.viewModelInitialized -and
        $state.captureSessionState -eq 'Faulted'
    $steps.Add([pscustomobject]$step)
    Write-Host "faulted-stop: fault=$faultFired stopFailed=$($step.stopFailed) spuriousAttach=$($step.spuriousAttach) audioStopped=$($step.audioMonitorStopped)/$($step.audioPlaybackDisposed) passed=$($step.passed)"

    Invoke-FailedRecoveryStart

    # recovery-start: the third handoff passes; the teardown finishes and a fresh pipeline renders
    # on the capture session that is still initialized.
    $startLine = @(Read-LogLines).Count
    $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
    $window = Get-LogWindow -StartLine $startLine
    $recovery.finishCompleted = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_COMPLETED requested=Previewing scope=PreviewPipeline initialized=True'
    $recovery.audioMonitorRestarted = Test-LogMarker -Window $window -Pattern 'WASAPI audio playback started\.'
    $recovery.passed = $recovery.passed -and $recovery.finishCompleted -and !$recovery.captureInitialized -and
        !$recovery.flagClearedByStop -and !$recovery.attachFailed -and $recovery.audioMonitorRestarted
    $steps.Add([pscustomobject]$recovery)
    Write-Host "recovery-start: state=$($recovery.startupState) finished=$($recovery.finishCompleted) reinit=$($recovery.captureInitialized) audio=$($recovery.audioMonitorRestarted) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"

    Invoke-PlainCycle -Name 'plain-cycle-flashback-off'
    Invoke-FlashbackToggle -State on
    Start-Sleep -Seconds 2
    Invoke-PlainCycle -Name 'plain-cycle-flashback-on'
}

# failed-recovery-start: a start finishes the retained teardown first, and fault 2 fails that finish.
function Invoke-FailedRecoveryStart {
    $startLine = @(Read-LogLines).Count
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $faultFired = Wait-LogMarker -StartLine $startLine -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED count=2 '
    $settled = Get-SettledSnapshot
    $window = Get-LogWindow -StartLine $startLine
    $state = Get-InitState $settled
    $step = [ordered]@{
        name = 'failed-recovery-start'
        startExitCode = $start.ExitCode
        startOutput = $start.Output
        faultFired = $faultFired
        failedWithFinishError = $start.Output -match 'Injected fault'
        refusedForCleanup = $start.Output -match 'Capture cleanup must complete'
        finishBegan = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN requested=Previewing scope=PreviewPipeline'
        finishCompleted = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_COMPLETED'
        newCaptureCreated = Test-LogMarker -Window $window -Pattern 'PREVIEW_START phase=create_uvc'
        captureInitialized = Test-LogMarker -Window $window -Pattern '\] CAPTURE_INIT( |_READY)'
        spuriousAttach = Test-LogMarker -Window $window -Pattern $spuriousAttachPattern
        audioMonitorStarted = Test-LogMarker -Window $window -Pattern 'WASAPI audio playback started\.'
        stateAfter = $state
        markers = Select-Markers $window
    }
    $step.passed = $start.ExitCode -ne 0 -and $faultFired -and $step.failedWithFinishError -and !$step.refusedForCleanup -and
        $step.finishBegan -and !$step.finishCompleted -and !$step.newCaptureCreated -and !$step.captureInitialized -and
        !$step.spuriousAttach -and !$step.audioMonitorStarted -and
        $null -ne $state -and !$state.isPreviewing -and $state.viewModelInitialized
    $steps.Add([pscustomobject]$step)
    Write-Host "failed-recovery-start: fault=$faultFired finishBegan=$($step.finishBegan) completed=$($step.finishCompleted) spuriousAttach=$($step.spuriousAttach) passed=$($step.passed)"
}

# failed-launch-start: the launch auto-start fails on the injected start fault: by default the install fault,
# with its capture running and not yet installed. With -HandoffFaults the rollback's renderer handoff hits
# fault 1 after that. With -AfterAudioGraph it is the Flashback fault, after the audio graph ran, and the
# rollback must leave no WASAPI capture worker behind.
function Invoke-FailedLaunchStart {
    param([switch] $HandoffFaults, [switch] $AfterAudioGraph)
    $window = @(Read-LogLines)
    $state = $script:launch.state
    $startFault = $(if ($AfterAudioGraph) { 'CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILED' } else { 'CAPTURE_FAULT_PREVIEW_INSTALL_FAILED' })
    $startFaultIndex = Get-MarkerIndex -Window $window -Pattern "$startFault count=1 "
    $handoffIndex = Get-MarkerIndex -Window $window -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED count=1 '
    $step = [ordered]@{
        name = 'failed-launch-start'
        startFault = $startFault
        startFaultFired = $startFaultIndex -ge 0
        handoffFaultFired = $handoffIndex -ge 0
        handoffFaultedInRollback = $startFaultIndex -ge 0 -and $handoffIndex -gt $startFaultIndex
        rollbackRan = Test-LogMarker -Window $window -Pattern 'Unified preview start failed'
        rollbackDisposeWarned = Test-LogMarker -Window $window -Pattern 'Unified preview rollback dispose warning'
        startFailed = Test-LogMarker -Window $window -Pattern 'CAP-COORD-FAIL\] StartVideoPreview'
        capturesCreated = @($window | Where-Object { $_ -match 'PREVIEW_START phase=create_uvc' }).Count
        spuriousAttach = Test-LogMarker -Window $window -Pattern $spuriousAttachPattern
        stateAfter = $state
        markers = Select-Markers $window
    }
    $handoffAsExpected = $(if ($HandoffFaults) { $step.handoffFaultedInRollback } else { !$step.handoffFaultFired })
    $step.passed = $step.startFaultFired -and $handoffAsExpected -and $step.rollbackRan -and $step.startFailed -and
        $step.capturesCreated -eq 1 -and !$step.spuriousAttach -and $null -ne $state -and !$state.isPreviewing -and
        $state.viewModelInitialized -and $state.captureSessionState -eq 'Faulted'
    if ($AfterAudioGraph) {
        $audioGraphIndex = Get-MarkerIndex -Window $window -Pattern 'Preview backend active'
        $step.audioGraphRanBeforeFault = $audioGraphIndex -ge 0 -and $startFaultIndex -gt $audioGraphIndex
        $step.workers = Wait-WasapiWorkerEvidence
        $step.passed = $step.passed -and $step.audioGraphRanBeforeFault -and $step.workers.captureThreads -eq 0 -and
            $step.workers.logLive -eq 0 -and $step.workers.micLive -eq 0
    }
    $steps.Add([pscustomobject]$step)
    Write-Host "failed-launch-start: fault=$($step.startFaultFired) handoff=$($step.handoffFaultFired) rollback=$($step.rollbackRan) captures=$($step.capturesCreated) passed=$($step.passed)"
}

function Invoke-AudioGraphRollback {
    Invoke-FailedLaunchStart -AfterAudioGraph
    $micPersisted = Get-PersistedMicrophoneEnabled
    try {
        # enable-mic: preview is stopped, so this only records the microphone for the next start.
        $enable = Invoke-Ssctl -Arguments @('set', 'mic', 'on') -TimeoutMs 30000
        $steps.Add([pscustomobject][ordered]@{
            name = 'enable-mic'
            persistedBefore = $micPersisted
            exitCode = $enable.ExitCode
            output = $enable.Output
            passed = $enable.ExitCode -eq 0
        })
        Write-Host "enable-mic: exit=$($enable.ExitCode) persistedBefore=$micPersisted"

        # failed-mic-start: the start runs program audio and the microphone monitor, then fails on fault 2. The
        # rollback must stop the monitor itself: nothing is retained for a later transition to finish.
        $startLine = @(Read-LogLines).Count
        $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
        $faultFired = Wait-LogMarker -StartLine $startLine -Pattern 'CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILED count=2 '
        $settled = Get-SettledSnapshot
        $window = Get-LogWindow -StartLine $startLine
        $state = Get-InitState $settled
        $micStartIndex = Get-MarkerIndex -Window $window -Pattern $micStartPattern
        $faultIndex = Get-MarkerIndex -Window $window -Pattern 'CAPTURE_FAULT_PREVIEW_FLASHBACK_FAILED count=2 '
        $rollbackIndex = Get-MarkerIndex -Window $window -Pattern 'Unified preview start failed'
        $micStopIndex = Get-MarkerIndex -Window $window -Pattern $micStopPattern
        $step = [ordered]@{
            name = 'failed-mic-start'
            startExitCode = $start.ExitCode
            startOutput = $start.Output
            faultFired = $faultFired
            failedWithStartFault = $start.Output -match 'Injected fault'
            micStartedBeforeFault = $micStartIndex -ge 0 -and $faultIndex -gt $micStartIndex
            micStoppedByRollback = $faultIndex -ge 0 -and $rollbackIndex -gt $faultIndex -and $micStopIndex -gt $rollbackIndex
            micDisposeFailed = Test-LogMarker -Window $window -Pattern 'Microphone capture dispose failed|WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT'
            retainedCleanup = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN'
            spuriousAttach = Test-LogMarker -Window $window -Pattern $spuriousAttachPattern
            workers = Wait-WasapiWorkerEvidence
            stateAfter = $state
            markers = Select-Markers $window
        }
        $step.passed = $start.ExitCode -ne 0 -and $faultFired -and $step.failedWithStartFault -and $step.micStartedBeforeFault -and
            $step.micStoppedByRollback -and !$step.micDisposeFailed -and !$step.retainedCleanup -and !$step.spuriousAttach -and
            $step.workers.captureThreads -eq 0 -and $step.workers.logLive -eq 0 -and $step.workers.micLive -eq 0 -and
            $null -ne $state -and !$state.isPreviewing -and $state.viewModelInitialized -and $state.captureSessionState -eq 'Faulted'
        $steps.Add([pscustomobject]$step)
        Write-Host "failed-mic-start: fault=$faultFired micStarted=$($step.micStartedBeforeFault) micStopped=$($step.micStoppedByRollback) threads=$($step.workers.captureThreads) micLive=$($step.workers.micLive) passed=$($step.passed)"

        # recovery-start: a fresh pipeline with exactly one microphone monitor. A monitor leaked by the failed
        # start would still run beside the new one: a launch-wide mic balance of 2 and an extra capture thread.
        $startLine = @(Read-LogLines).Count
        $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
        $window = Get-LogWindow -StartLine $startLine
        $recovery.micStartsInStep = @($window | Where-Object { $_ -match $micStartPattern }).Count
        $recovery.finishBegan = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN'
        $recovery.freshCaptureCreated = Test-LogMarker -Window $window -Pattern 'PREVIEW_START phase=create_uvc'
        $recovery.workers = Wait-WasapiWorkerEvidence
        $recovery.passed = $recovery.passed -and $recovery.micStartsInStep -eq 1 -and !$recovery.finishBegan -and
            $recovery.freshCaptureCreated -and !$recovery.captureInitialized -and !$recovery.flagClearedByStop -and
            !$recovery.attachFailed -and $recovery.workers.micLive -eq 1 -and $recovery.workers.logLive -ge 1 -and
            $recovery.workers.captureThreads -eq $recovery.workers.logLive
        $steps.Add([pscustomobject]$recovery)
        Write-Host "recovery-start: state=$($recovery.startupState) micStarts=$($recovery.micStartsInStep) micLive=$($recovery.workers.micLive) threads=$($recovery.workers.captureThreads)/$($recovery.workers.logLive) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"

        # A teardown stop releases the monitor and the start runs a new one: still exactly one.
        Invoke-FlashbackToggle -State off
        Invoke-PlainCycle -Name 'plain-cycle-flashback-off' -RequireOneMicrophone
    }
    finally {
        # restore-mic: leave the persisted microphone toggle as this scenario found it.
        $restoreValue = $(if ($micPersisted) { 'on' } else { 'off' })
        $restore = Invoke-Ssctl -Arguments @('set', 'mic', $restoreValue) -TimeoutMs 30000
        $steps.Add([pscustomobject][ordered]@{
            name = 'restore-mic'
            value = $restoreValue
            exitCode = $restore.ExitCode
            output = $restore.Output
            persistedAfter = Get-PersistedMicrophoneEnabled
            passed = $restore.ExitCode -eq 0 -and (Get-PersistedMicrophoneEnabled) -eq $micPersisted
        })
        Write-Host "restore-mic: set mic $restoreValue exit=$($restore.ExitCode)"
    }
}

function Invoke-StartRollback {
    Invoke-FailedLaunchStart -HandoffFaults
    Invoke-FailedRecoveryStart

    # recovery-start: the third handoff passes. The finish stops and releases the capture the
    # launch rollback left behind, then a fresh capture opens the same device and renders.
    # Before the fix the pipeline never owned that capture, so the finish had no video to stop.
    $startLine = @(Read-LogLines).Count
    $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
    $window = Get-LogWindow -StartLine $startLine
    $beginIndex = Get-MarkerIndex -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN requested=Previewing scope=PreviewPipeline'
    $videoStopIndex = Get-MarkerIndex -Window $window -Pattern 'PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE'
    $completedIndex = Get-MarkerIndex -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_COMPLETED requested=Previewing scope=PreviewPipeline initialized=True'
    $recovery.finishCompleted = $completedIndex -ge 0
    $recovery.finishStoppedRetainedVideo = $beginIndex -ge 0 -and $videoStopIndex -gt $beginIndex -and $completedIndex -gt $videoStopIndex
    # Presence, not order: the [FATAL] breadcrumb is written synchronously and can land ahead of the queued
    # COMPLETED line. The transition runs its start only after the finish completes, under one lock hold.
    $recovery.freshCaptureCreated = Test-LogMarker -Window $window -Pattern 'PREVIEW_START phase=create_uvc'
    $recovery.passed = $recovery.passed -and $recovery.finishCompleted -and $recovery.finishStoppedRetainedVideo -and
        $recovery.freshCaptureCreated -and !$recovery.captureInitialized -and !$recovery.flagClearedByStop -and !$recovery.attachFailed
    $steps.Add([pscustomobject]$recovery)
    Write-Host "recovery-start: state=$($recovery.startupState) finished=$($recovery.finishCompleted) videoStopped=$($recovery.finishStoppedRetainedVideo) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"

    # Flashback can be toggled only once nothing is retained.
    Invoke-FlashbackToggle -State off
    Invoke-PlainCycle -Name 'plain-cycle-flashback-off'
    Invoke-FlashbackToggle -State on
    Start-Sleep -Seconds 2
    Invoke-PlainCycle -Name 'plain-cycle-flashback-on'
}

function Invoke-StartRollbackReleased {
    Invoke-FailedLaunchStart

    # recovery-start: the rollback released everything itself, so nothing is finished and a fresh
    # capture opens the same device and renders.
    $startLine = @(Read-LogLines).Count
    $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
    $window = Get-LogWindow -StartLine $startLine
    $recovery.finishBegan = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN'
    $recovery.freshCaptureCreated = Test-LogMarker -Window $window -Pattern 'PREVIEW_START phase=create_uvc'
    $recovery.passed = $recovery.passed -and !$recovery.finishBegan -and $recovery.freshCaptureCreated -and
        !$recovery.captureInitialized -and !$recovery.flagClearedByStop -and !$recovery.attachFailed
    $steps.Add([pscustomobject]$recovery)
    Write-Host "recovery-start: state=$($recovery.startupState) finishBegan=$($recovery.finishBegan) rendered=$($recovery.renderedDuringDwell) passed=$($recovery.passed)"

    Invoke-FlashbackToggle -State off
    Invoke-PlainCycle -Name 'plain-cycle-flashback-off'
}

function Invoke-ReinitThenStart {
    Invoke-FlashbackToggle -State on
    if (Test-LogMarker -Window (Read-LogLines) -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED') {
        throw 'The injected fault fired while enabling Flashback, before the reinit.'
    }

    $sessionDirectory = Get-FlashbackSessionDirectory
    if ($null -eq $sessionDirectory) { throw 'No Flashback buffer was initialized; the reinit scenario needs live segments.' }
    $segmentsBefore = Wait-SegmentFiles -SessionDirectory $sessionDirectory -Minimum 2
    $initial = Get-Snapshot
    $initialFrameRate = [double]$initial.SelectedFrameRate
    $otherFrameRate = Select-OtherFrameRate -Current $initialFrameRate
    if ($null -eq $otherFrameRate) { throw "No other enabled frame rate to reinitialize to (selected $initialFrameRate)." }

    # failed-reinit: the reinit teardown hits fault 1 and its failure-cleanup stop hits fault 2.
    $startLine = @(Read-LogLines).Count
    $set = Invoke-Ssctl -Arguments @('set', 'fps', ([string]$otherFrameRate)) -TimeoutMs 60000
    $cleanupFaulted = Wait-LogMarker -StartLine $startLine -Pattern 'REINIT_FAILED_CLEANUP_FAULT'
    $settled = Get-SettledSnapshot
    $window = Get-LogWindow -StartLine $startLine
    $state = Get-InitState $settled
    $segmentsAtFailure = Get-SegmentEvidence -SessionDirectory $sessionDirectory
    $step = [ordered]@{
        name = 'failed-reinit'
        requestedFrameRate = $otherFrameRate
        initialFrameRate = $initialFrameRate
        setExitCode = $set.ExitCode
        setOutput = $set.Output
        teardownFaulted = Test-LogMarker -Window $window -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED count=1 '
        finishFaulted = Test-LogMarker -Window $window -Pattern 'CAPTURE_FAULT_PRE_CLEANUP_FAILED count=2 '
        reinitCleanupFaulted = $cleanupFaulted
        finishBegan = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_BEGIN requested=Ready scope=PreviewPipeline'
        finishCompleted = Test-LogMarker -Window $window -Pattern 'CAPTURE_RETAINED_CLEANUP_COMPLETED'
        flashbackDisposed = Test-LogMarker -Window $window -Pattern 'FLASHBACK_PREVIEW_DISPOSE_OK'
        spuriousAttach = Test-LogMarker -Window $window -Pattern $spuriousAttachPattern
        segmentsBefore = $segmentsBefore
        segmentsAtFailure = $segmentsAtFailure
        stateAfter = $state
        # The failed reinit rolls the selection back to the rate it started from.
        selectedFrameRateRestored = $null -ne $state -and [Math]::Abs([double]$state.selectedFrameRate - $initialFrameRate) -lt 0.01
        markers = Select-Markers $window
    }
    $step.passed = $set.ExitCode -ne 0 -and $step.teardownFaulted -and $step.finishFaulted -and $cleanupFaulted -and
        $step.finishBegan -and !$step.finishCompleted -and !$step.flashbackDisposed -and !$step.spuriousAttach -and
        $segmentsBefore.segmentFiles -ge 1 -and $segmentsAtFailure.exists -and !$segmentsAtFailure.retiredMarker -and
        $segmentsAtFailure.segmentFiles -ge $segmentsBefore.segmentFiles -and
        $null -ne $state -and !$state.isPreviewing -and !$state.viewModelInitialized -and $step.selectedFrameRateRestored
    $steps.Add([pscustomobject]$step)
    Write-Host "failed-reinit: faults=$($step.teardownFaulted)/$($step.finishFaulted) segments=$($segmentsBefore.segmentFiles)->$($segmentsAtFailure.segmentFiles) vmInitialized=$($state.viewModelInitialized) passed=$($step.passed)"

    # recovery-start: the third handoff passes. The transition that finds the teardown retained
    # finishes it as a teardown, retiring the Flashback session, then preview renders.
    $startLine = @(Read-LogLines).Count
    $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
    $window = Get-LogWindow -StartLine $startLine
    $finishLine = @($window | Where-Object { $_ -match 'CAPTURE_RETAINED_CLEANUP_COMPLETED .*scope=PreviewPipeline initialized=True' } | Select-Object -First 1)
    $recovery.finishCompleted = $finishLine.Count -gt 0
    $recovery.finishedBy = $(if ($finishLine.Count -gt 0 -and $finishLine[0] -match 'requested=(\w+)') { $Matches[1] } else { $null })
    $recovery.segmentsRetired = Test-LogMarker -Window $window -Pattern 'FLASHBACK_PREVIEW_DISPOSE_OK purge=False'
    $recovery.segmentsPurged = Test-LogMarker -Window $window -Pattern 'FLASHBACK_PREVIEW_DISPOSE_OK purge=True|FLASHBACK_BUFFER_DISPOSE_PURGE'
    $recovery.segmentsAfter = Get-SegmentEvidence -SessionDirectory $sessionDirectory
    $after = Get-Snapshot
    $recovery.selectedFrameRateRestored = $null -ne $after -and [Math]::Abs([double]$after.SelectedFrameRate - $initialFrameRate) -lt 0.01
    $recovery.passed = $recovery.passed -and $recovery.finishCompleted -and $recovery.segmentsRetired -and
        !$recovery.segmentsPurged -and !$recovery.attachFailed -and $recovery.segmentsAfter.exists -and
        $recovery.segmentsAfter.retiredMarker -and $recovery.segmentsAfter.segmentFiles -ge $segmentsAtFailure.segmentFiles -and
        $recovery.selectedFrameRateRestored
    $steps.Add([pscustomobject]$recovery)
    Write-Host "recovery-start: state=$($recovery.startupState) finishedBy=$($recovery.finishedBy) retired=$($recovery.segmentsRetired) segments=$($recovery.segmentsAfter.segmentFiles) marker=$($recovery.segmentsAfter.retiredMarker) rateRestored=$($recovery.selectedFrameRateRestored) passed=$($recovery.passed)"
}

function Invoke-Control {
    Invoke-FlashbackToggle -State off
    Invoke-PlainCycle -Name 'plain-cycle-flashback-off'
    Invoke-FlashbackToggle -State on
    Start-Sleep -Seconds 2
    Invoke-PlainCycle -Name 'plain-cycle-flashback-on'
}

$expectedStepCounts = @{ StopThenStart = 5; ReinitThenStart = 2; StartRollback = 5; StartRollbackReleased = 3; AudioGraphRollback = 6; Control = 2 }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$scenarioResults = New-Object System.Collections.Generic.List[object]
# Stays false unless the report is computed, so an error while writing it still exits 1.
$allPassed = $false
try {
    foreach ($name in $scenarioNames) {
        $steps = New-Object System.Collections.Generic.List[object]
        $script:app = $null
        $script:launch = $null
        $close = $null
        $failure = $null
        Write-Host "== $name =="
        try {
            $faults = $scenarioFaults[$name]
            Start-App -FaultFailures $faults.PreCleanup -InstallFaultFailures $faults.Install -FlashbackFaultFailures $faults.Flashback -ExpectLaunchFailure:$faults.LaunchFails
            switch ($name) {
                'StopThenStart' { Invoke-StopThenStart }
                'ReinitThenStart' { Invoke-ReinitThenStart }
                'StartRollback' { Invoke-StartRollback }
                'StartRollbackReleased' { Invoke-StartRollbackReleased }
                'AudioGraphRollback' { Invoke-AudioGraphRollback }
                'Control' { Invoke-Control }
            }
        }
        catch {
            $failure = $_.Exception.Message
            Write-Host "${name} aborted: $failure"
        }
        finally {
            $close = Stop-App -LogName "$name.Sussudio_Debug.log"
        }

        $closePassed = $null -ne $close -and $close.exitedWithin30s
        $scenarioResults.Add([pscustomobject][ordered]@{
            name = $name
            passed = $null -eq $failure -and $steps.Count -eq $expectedStepCounts[$name] -and
                @($steps | Where-Object { !$_.passed }).Count -eq 0 -and $closePassed
            failure = $failure
            stepsPassed = @($steps | Where-Object { $_.passed }).Count
            stepsRun = $steps.Count
            stepsExpected = $expectedStepCounts[$name]
            faults = $scenarioFaults[$name]
            log = "$name.Sussudio_Debug.log"
            launch = $script:launch
            steps = $steps
            close = $close
        })
    }
}
finally {
    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $passed = $scenarioResults.Count -eq $scenarioNames.Count -and @($scenarioResults | Where-Object { !$_.passed }).Count -eq 0
    $report = [pscustomobject]@{
        schema = 'sussudio.capture-late-cleanup-recovery/v4'
        label = $Label
        scenario = $Scenario
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        commit = $commit
        trackedChangesPresent = $dirty
        appPath = $AppPath
        appDllSha256 = (Get-FileHash (Join-Path (Split-Path -Parent $AppPath) 'Sussudio.dll')).Hash
        passed = $passed
        scenarios = $scenarioResults
        doesNotProve = 'The teardown failures are injected exceptions at the capture pre-cleanup renderer handoff, not real renderer stop timeouts, and the start failures are injected exceptions between a running capture and its install, or after the audio graph started where the Flashback backend would start, not real start failures (a renderer attached mid-start, a Flashback encoder that fails to open). Exercised: a preview-stop teardown, a reinit teardown and a fresh-start rollback, each finished late by a start or an initialize, a fresh-start rollback that releases its capture itself, and one that releases program audio and the microphone monitor. Not exercised: a retained full capture cleanup (fatal capture error), recording, the post-recording idle-release handoff, a start rollback after the Flashback backend started, a microphone dispose that fails or times out, a rollback whose handoff fails after the audio graph started, and cancellation of a finish. Device release is inferred from the finish stopping the retained video and a fresh capture opening the same device and rendering, not from a handle count. WASAPI capture workers are counted from live process threads named by the app and from the log balance; that shows a worker thread ended, not that the audio endpoint was released. Judged from log markers, state counters, process threads and the Flashback session directory on disk; not colour, HDR, audible audio, or recording.'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resultPath -Encoding utf8 -ErrorAction Stop
    $allPassed = $passed
    Write-Host "result: $resultPath passed=$allPassed"
}

if ($allPassed -ne $true) { exit 1 }
