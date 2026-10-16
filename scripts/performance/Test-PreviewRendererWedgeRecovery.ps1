<#
.SYNOPSIS
Live check that preview recovers from a D3D11 preview renderer whose render thread is wedged.
.DESCRIPTION
Launches the built app on a private automation pipe with
SUSSUDIO_PREVIEW_FAULT_WEDGE_RENDERER_START=2, so the renderer created by the first
explicit `preview start` holds its render thread inside its first video-processor
creation (within the renderer's native-call fence) for 30 s. That stands in for the
D3D11 call that once stayed wedged after a stop/start, which left every later stop
timing out until the app exited.

Steps, each judged from app log markers and `ssctl --json state`:
  wedged-restart  preview stop + start: the held renderer misses its first visual,
                  the startup-failure stop abandons it (PREVIEW_RENDERER_ABANDONED),
                  and preview reports stopped.
  recovery-start  preview start: a fresh SwapChainPanel replaces the one the wedged
                  swap chain is bound to, and preview reaches Rendering while the
                  wedged thread is still held.
  late-release    (unless -CloseWhileWedged) the held call returns while the new
                  renderer is live: the abandoned thread exits with its cleanup
                  complete, no stray first-frame callback reaches the host, and the
                  live renderer keeps presenting.
  plain-cycle-N   -PlainCycles ordinary stop + start cycles that must reach Rendering.
  close           the app accepts window close and exits with code 0 (with
                  -CloseWhileWedged, while the render thread is still held).

-WedgeRendererStart 0 skips the fault and runs only the plain cycles (a control).
Writes result.json and the app log under artifacts/preview-renderer-wedge/. Refuses
to start while any Sussudio instance runs: the app is single instance.
.EXAMPLE
./scripts/performance/Test-PreviewRendererWedgeRecovery.ps1 -Label fix
.EXAMPLE
./scripts/performance/Test-PreviewRendererWedgeRecovery.ps1 -Label close-while-wedged -CloseWhileWedged
#>
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label = 'wedge-recovery',

    [ValidateSet(0, 2)]
    [int] $WedgeRendererStart = 2,

    [ValidateRange(0, 20)]
    [int] $PlainCycles = 2,

    [switch] $CloseWhileWedged,

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
    $OutputDirectory = Join-Path $repoRoot "artifacts/preview-renderer-wedge/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}

$running = @(Get-Process -Name Sussudio -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw "Sussudio is already running (PID $($running.Id -join ', ')); it owns the capture device. Close it first."
}

$pipeName = "SussudioAutomation.wedge-$PID"
$logPath = Join-Path $repoRoot 'temp/logs/Sussudio_Debug.log'
$markerPattern = 'D3D11_PREVIEW_RENDERER_START|D3D11_PREVIEW_FAULT_WEDGE_|D3D11_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT|D3D11_PREVIEW_RENDERER_STOP_|D3D11_PREVIEW_RENDER_THREAD_|PREVIEW_RENDERER_ABANDONED|PREVIEW_SWAPCHAIN_PANEL_REPLACED|PREVIEW_D3D11_FIRST_FRAME|PREVIEW_START_TIMEOUT|PREVIEW_START_FAILURE_STOP|PREVIEW_REINIT_RENDERER_STOP|CAP-COORD|TimeoutException'

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
    if ($text.Length -eq 0) { return @() }
    # Drop the empty element after the final newline, so a line count is the index of the next line written.
    return @($text.TrimEnd("`r`n".ToCharArray()) -split "`r?`n")
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

# Starts preview and requires Rendering with frames presented after a dwell.
function Invoke-RenderingStart {
    param([string] $Name, [int] $StartLine)
    $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
    $outcome = Wait-StartupOutcome
    $firstFrame = Wait-LogMarker -StartLine $StartLine -Pattern 'PREVIEW_D3D11_FIRST_FRAME'
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
        presentCallAvgMs = $(if ($null -ne $after) { $after.PreviewD3DPresentCallAvgMs } else { $null })
        panelReplacedForAbandonedRenderer = Test-LogMarker -Window $window -Pattern 'PREVIEW_SWAPCHAIN_PANEL_REPLACED reason=abandoned-renderer'
        passed = $start.ExitCode -eq 0 -and $null -ne $outcome -and $outcome.PreviewStartupState -eq 'Rendering' -and
            $firstFrame -and $null -ne $renderedDuringDwell -and $renderedDuringDwell -gt 30
        markers = @($window | Where-Object { $_ -match $markerPattern })
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$appliedEnvironment = @{ SUSSUDIO_AUTOMATION_PIPE = $pipeName }
if ($WedgeRendererStart -gt 0) {
    $appliedEnvironment['SUSSUDIO_PREVIEW_FAULT_WEDGE_RENDERER_START'] = [string]$WedgeRendererStart
}
$savedEnvironment = @{}
foreach ($key in @($appliedEnvironment.Keys) + 'SUSSUDIO_PREVIEW_FAULT_WEDGE_RENDERER_START') {
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
    # Windows PowerShell reports ExitCode only for a process whose handle was opened before it exited.
    $null = $app.Handle
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
    }
    Write-Host "launch: state=$($launch.startupState) firstFrame=$($launch.firstFrameLogged)"
    if ($launch.startupState -ne 'Rendering') { throw "Launch preview did not reach Rendering ($($launch.startupState))." }
    Start-Sleep -Seconds 2

    if ($WedgeRendererStart -gt 0) {
        # wedged-restart: renderer #2 holds its render thread; the startup-failure stop must abandon it.
        $startLine = @(Read-LogLines).Count
        $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
        $stoppedBefore = Wait-PreviewStopped
        $start = Invoke-Ssctl -Arguments @('preview', 'start') -TimeoutMs 30000
        $held = Wait-LogMarker -StartLine $startLine -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_HOLD'
        $abandoned = Wait-LogMarker -StartLine $startLine -Pattern 'PREVIEW_RENDERER_ABANDONED' -TimeoutSeconds ($StepTimeoutSeconds + 15)
        $stoppedAfter = Wait-PreviewStopped
        $window = Get-LogWindow -StartLine $startLine
        $step = [ordered]@{
            name = 'wedged-restart'
            stopExitCode = $stop.ExitCode
            stoppedBeforeStart = $null -ne $stoppedBefore -and !$stoppedBefore.IsPreviewing
            startExitCode = $start.ExitCode
            startOutput = $start.Output
            wedgeHeld = $held
            startTimedOut = Test-LogMarker -Window $window -Pattern 'PREVIEW_START_TIMEOUT'
            nativeFenceTimeouts = @($window | Where-Object { $_ -match 'D3D11_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT' }).Count
            rendererAbandoned = $abandoned
            previewStoppedAfterAbandon = $null -ne $stoppedAfter -and !$stoppedAfter.IsPreviewing
            wedgeReleasedDuringStep = Test-LogMarker -Window $window -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_RELEASED'
            markers = @($window | Where-Object { $_ -match $markerPattern })
        }
        $step.passed = $step.stoppedBeforeStart -and $held -and $abandoned -and $step.previewStoppedAfterAbandon -and
            !$step.wedgeReleasedDuringStep
        $steps.Add([pscustomobject]$step)
        Write-Host "wedged-restart: held=$held abandoned=$abandoned stopped=$($step.previewStoppedAfterAbandon) passed=$($step.passed)"

        # recovery-start: the next renderer gets a fresh panel and renders while the old thread is still held.
        # A hold that expires before the dwell ends would only show a restart after the wedge, so it fails the step.
        $startLine = @(Read-LogLines).Count
        $recovery = Invoke-RenderingStart -Name 'recovery-start' -StartLine $startLine
        $recoveryWindow = Get-LogWindow -StartLine $startLine
        $recovery.wedgeStillHeldWhenRendering = !(Test-LogMarker -Window $recoveryWindow -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_RELEASED')
        $recovery.passed = $recovery.passed -and $recovery.panelReplacedForAbandonedRenderer -and $recovery.wedgeStillHeldWhenRendering
        $steps.Add([pscustomobject]$recovery)
        Write-Host "recovery-start: state=$($recovery.startupState) rendered=$($recovery.renderedDuringDwell) freshPanel=$($recovery.panelReplacedForAbandonedRenderer) held=$($recovery.wedgeStillHeldWhenRendering) passed=$($recovery.passed)"

        if (!$CloseWhileWedged) {
            # late-release: the held call returns into an abandoned renderer while the new one is live, and
            # its thread then releases its own swap chain and device. The window starts where recovery's ended.
            $releaseLine = $startLine + $recoveryWindow.Count
            $before = Get-Snapshot
            $released = Wait-LogMarker -StartLine $releaseLine -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_RELEASED' -TimeoutSeconds 45
            if ($released) {
                $null = Wait-LogMarker -StartLine $releaseLine -Pattern 'D3D11_PREVIEW_RENDER_THREAD_(EXITED|CLEANUP_RETAINED)' -TimeoutSeconds 10
            }
            Start-Sleep -Seconds 3
            $after = Get-Snapshot
            $window = Get-LogWindow -StartLine $releaseLine
            $afterRelease = @()
            $releaseIndex = -1
            for ($i = 0; $i -lt $window.Count; $i++) {
                if ($window[$i] -match 'D3D11_PREVIEW_FAULT_WEDGE_RELEASED') { $releaseIndex = $i; break }
            }
            if ($releaseIndex -ge 0) { $afterRelease = @($window | Select-Object -Skip $releaseIndex) }
            $renderedAcrossRelease = $null
            if ($null -ne $before -and $null -ne $after) {
                $renderedAcrossRelease = [long]$after.PreviewD3DFramesRendered - [long]$before.PreviewD3DFramesRendered
            }
            $step = [ordered]@{
                name = 'late-release'
                released = $released
                strayHostFirstFrameAfterRelease = Test-LogMarker -Window $afterRelease -Pattern 'PREVIEW_D3D11_FIRST_FRAME'
                abandonedThreadExited = Test-LogMarker -Window $afterRelease -Pattern 'D3D11_PREVIEW_RENDER_THREAD_EXITED cleanup=complete'
                abandonedCleanupRetained = Test-LogMarker -Window $afterRelease -Pattern 'D3D11_PREVIEW_RENDER_THREAD_CLEANUP_RETAINED'
                stillRendering = $null -ne $after -and $after.PreviewStartupState -eq 'Rendering'
                renderedAcrossRelease = $renderedAcrossRelease
                markers = @($window | Where-Object { $_ -match $markerPattern })
            }
            $step.passed = $released -and $step.abandonedThreadExited -and !$step.abandonedCleanupRetained -and
                !$step.strayHostFirstFrameAfterRelease -and $step.stillRendering -and
                $null -ne $renderedAcrossRelease -and $renderedAcrossRelease -gt 30
            $steps.Add([pscustomobject]$step)
            Write-Host "late-release: released=$released exited=$($step.abandonedThreadExited) retained=$($step.abandonedCleanupRetained) stray=$($step.strayHostFirstFrameAfterRelease) rendered=$renderedAcrossRelease passed=$($step.passed)"
        }
    }

    $plainCount = $PlainCycles
    if ($CloseWhileWedged) { $plainCount = 0 }
    for ($cycle = 1; $cycle -le $plainCount; $cycle++) {
        $startLine = @(Read-LogLines).Count
        $stop = Invoke-Ssctl -Arguments @('preview', 'stop') -TimeoutMs 30000
        $stopped = Wait-PreviewStopped
        $step = Invoke-RenderingStart -Name "plain-cycle-$cycle" -StartLine $startLine
        $step.stopExitCode = $stop.ExitCode
        $step.passed = $step.passed -and $stop.ExitCode -eq 0 -and $null -ne $stopped -and !$stopped.IsPreviewing
        $steps.Add([pscustomobject]$step)
        Write-Host "plain-cycle-${cycle}: state=$($step.startupState) rendered=$($step.renderedDuringDwell) passed=$($step.passed)"
    }
}
finally {
    if ($null -ne $app -and !$app.HasExited) {
        $closeLine = @(Read-LogLines).Count
        $wedgeHeldAtClose = $false
        if ($WedgeRendererStart -gt 0) {
            $all = Read-LogLines
            $wedgeHeldAtClose = (Test-LogMarker -Window $all -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_HOLD') -and
                !(Test-LogMarker -Window $all -Pattern 'D3D11_PREVIEW_FAULT_WEDGE_RELEASED')
        }
        $closeCommand = Invoke-Ssctl -Arguments @('window', 'close')
        $exited = $app.WaitForExit(30000)
        if (!$exited) {
            Stop-Process -Id $app.Id -Force
            $null = $app.WaitForExit(5000)
        }
        $appExitCode = $null
        if ($app.HasExited) { $appExitCode = $app.ExitCode }
        $window = Get-LogWindow -StartLine $closeLine
        # ssctl exits 3 on a healthy close as well: the app tears its automation pipe down while answering
        # WindowAction Close. The app's own log line shows the close was accepted, and its exit code shows a
        # clean exit (FailFast and native crashes are nonzero).
        $close = [ordered]@{
            windowCloseExitCode = $closeCommand.ExitCode
            windowCloseOutput = $closeCommand.Output
            closeRequestAccepted = Test-LogMarker -Window $window -Pattern 'WINDOW_CLOSED_TRIGGER requested=1'
            wedgeHeldAtClose = $wedgeHeldAtClose
            exitedWithin30s = $exited
            appExitCode = $appExitCode
            markers = @($window | Where-Object { $_ -match $markerPattern })
        }
        Write-Host "close: accepted=$($close.closeRequestAccepted) exited=$exited appExitCode=$appExitCode wedgeHeldAtClose=$wedgeHeldAtClose"
    }
    if (Test-Path -LiteralPath $logPath) {
        Copy-Item -LiteralPath $logPath -Destination (Join-Path $OutputDirectory 'Sussudio_Debug.log')
    }

    $ErrorActionPreference = 'Continue'
    $commit = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $dirty = @(& git -C $repoRoot status --porcelain --untracked-files=no 2>$null).Count -gt 0
    $expectedSteps = $PlainCycles
    if ($WedgeRendererStart -gt 0) { $expectedSteps = $(if ($CloseWhileWedged) { 2 } else { 3 + $PlainCycles }) }
    $closePassed = $null -ne $close -and $close.closeRequestAccepted -and $close.exitedWithin30s -and $close.appExitCode -eq 0
    if ($CloseWhileWedged) { $closePassed = $closePassed -and $close.wedgeHeldAtClose }
    $allPassed = $steps.Count -eq $expectedSteps -and @($steps | Where-Object { !$_.passed }).Count -eq 0 -and $closePassed
    $report = [pscustomobject]@{
        schema = 'sussudio.preview-renderer-wedge-recovery/v1'
        label = $Label
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        commit = $commit
        trackedChangesPresent = $dirty
        appPath = $AppPath
        appDllSha256 = (Get-FileHash (Join-Path (Split-Path -Parent $AppPath) 'Sussudio.dll')).Hash
        environment = $appliedEnvironment
        closeWhileWedged = [bool]$CloseWhileWedged
        passed = $allPassed
        stepsPassed = @($steps | Where-Object { $_.passed }).Count
        stepsRun = $steps.Count
        stepsExpected = $expectedSteps
        launch = $launch
        steps = $steps
        close = $close
        doesNotProve = 'The wedge is a managed 30 s hold inside the renderer native-call fence. It does not reproduce the display/compositor condition that wedged a real D3D11 call, and cannot show whether such a call holds the shared device lock against the next renderer. Judged from log markers and state counters only; not colour, HDR, audio, or recording.'
    }
    $resultPath = Join-Path $OutputDirectory 'result.json'
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resultPath -Encoding utf8
    Write-Host "result: $resultPath passed=$allPassed ($(@($steps | Where-Object { $_.passed }).Count)/$expectedSteps steps)"
}

if (!$allPassed) { exit 1 }
