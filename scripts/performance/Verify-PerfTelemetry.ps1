<#
.SYNOPSIS
End-to-end check of the performance telemetry commands against a running Sussudio app.
.DESCRIPTION
Drives ssctl through perf report, perf window, perf mark, perf trace, knobs, knob set/reset, the performance
timeline's cadence under a burst of state reads and log tail, saves every raw response under the output directory, and writes verify-result.json with one entry per check.
Exit code is 0 only when every check passed. Re-run it against any build to compare.

Preconditions (not changed by this script): the app is running, ideally with live preview, so the
histograms and counters have something to observe. The script starts no preview or recording. It sets and
then resets one tuning knob (SUSSUDIO_DIAGNOSTICS_POLL_MS) and runs one window and one trace, then stops both.
.EXAMPLE
./scripts/performance/Verify-PerfTelemetry.ps1 -OutputDirectory artifacts/perf-overhaul/slice1
#>
[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $SsctlPath,
    [string] $PipeName,
    [ValidateRange(3, 120)]
    [int] $WindowSeconds = 10,
    [ValidateRange(2, 60)]
    [int] $TraceSeconds = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ([string]::IsNullOrWhiteSpace($SsctlPath)) {
    $SsctlPath = Join-Path $repoRoot 'tools/ssctl/bin/Debug/net8.0/ssctl.exe'
}
$SsctlPath = (Resolve-Path -LiteralPath $SsctlPath).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts/perf-overhaul/slice1'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
    param([string] $Name, [bool] $Passed, [string] $Detail)
    $checks.Add([pscustomobject]@{ name = $Name; passed = $Passed; detail = $Detail })
    $mark = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1} - {2}" -f $mark, $Name, $Detail)
}

# Runs ssctl with --json, saves the raw output, and returns the parsed response plus the exit code.
function Invoke-Ssctl {
    param([string] $Label, [string[]] $CommandArgs)
    $argList = @('--json')
    if (-not [string]::IsNullOrWhiteSpace($PipeName)) { $argList += @('--pipe', $PipeName) }
    $argList += $CommandArgs
    # Windows PowerShell 5.1 turns native stderr into terminating errors under 'Stop'; ssctl writes its answer to stdout.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $text = (& $SsctlPath @argList 2>&1 | Out-String)
        $exit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
    Set-Content -LiteralPath (Join-Path $OutputDirectory "$Label.json") -Value $text -Encoding UTF8
    $parsed = $null
    try { $parsed = $text | ConvertFrom-Json } catch { $parsed = $null }
    return [pscustomobject]@{ Exit = $exit; Raw = $text; Response = $parsed }
}

function Get-Data($result) {
    if ($null -ne $result.Response -and $result.Response.PSObject.Properties.Name -contains 'Data') { return $result.Response.Data }
    return $null
}

# Performance timeline entry timestamps newer than $After, oldest first, or $null when the response has no data.
function Get-TimelineTimestamps {
    param([string] $Label, [DateTimeOffset] $After)
    $timeline = Invoke-Ssctl $Label @('timeline', '--max', '60')
    # Timestamps are read from the raw text (PowerShell 7 would turn them into culture-formatted DateTime values),
    # only after the "Data" property: the response envelope has its own TimestampUtc, and JSON escapes '+'.
    $raw = $timeline.Raw
    $dataIndex = $raw.IndexOf('"Data"', [StringComparison]::Ordinal)
    if ($dataIndex -lt 0) { return $null }
    return , @([regex]::Matches($raw.Substring($dataIndex), '"TimestampUtc"\s*:\s*"([^"]+)"') | ForEach-Object {
            [DateTimeOffset]::Parse([regex]::Unescape($_.Groups[1].Value), [Globalization.CultureInfo]::InvariantCulture)
        } | Where-Object { $_ -ge $After })
}

# Spacing of the diagnostics hub ticks, from the performance timeline. Only entries newer than $After count, so a
# knob change is measured on ticks that ran under it. The snapshot refresh has occasional latency spikes of several
# hundred milliseconds that dominate the median, so the lower quartile is the figure that follows the poll interval.
function Get-HubIntervalStats {
    param([string] $Label, [DateTimeOffset] $After)
    $times = Get-TimelineTimestamps $Label $After
    if ($null -eq $times -or $times.Count -lt 6) { return $null }
    $allDeltas = @(for ($i = 1; $i -lt $times.Count; $i++) { ($times[$i] - $times[$i - 1]).TotalMilliseconds })
    $deltas = @($allDeltas | Where-Object { $_ -gt 0 } | Sort-Object)
    if ($deltas.Count -lt 5) { return $null }
    return [pscustomobject]@{
        Intervals = $deltas.Count
        MedianMs  = [double]$deltas[[int][Math]::Floor($deltas.Count / 2)]
        P25Ms     = [double]$deltas[[int][Math]::Floor($deltas.Count / 4)]
    }
}

$startedUtc = (Get-Date).ToUniversalTime().ToString('o')

# 0. Connectivity and context.
$state = Invoke-Ssctl '00-state' @('state')
Add-Check 'app reachable over the pipe' ($state.Exit -eq 0) "ssctl state exit=$($state.Exit)"
if ($state.Exit -ne 0) {
    $checks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verify-result.json') -Encoding UTF8
    exit 1
}

# 1. Lifetime report.
$lifetime = Invoke-Ssctl '01-report-lifetime' @('perf', 'report')
$lifetimeData = Get-Data $lifetime
Add-Check 'perf report succeeds' ($lifetime.Exit -eq 0 -and $null -ne $lifetimeData) "exit=$($lifetime.Exit)"
if ($null -ne $lifetimeData) {
    Add-Check 'lifetime report is process-start' ($lifetimeData.Since -eq 'process-start') "Since=$($lifetimeData.Since)"
    Add-Check 'process sensors report memory and handles' ($lifetimeData.Process.PrivateBytes -gt 0 -and $lifetimeData.Process.Handles -gt 0 -and $lifetimeData.Process.Threads -gt 0) "private=$($lifetimeData.Process.PrivateBytes) handles=$($lifetimeData.Process.Handles) threads=$($lifetimeData.Process.Threads)"
    Add-Check 'DWM refresh rate is read' ($null -ne $lifetimeData.Dwm.RefreshHz -and $lifetimeData.Dwm.RefreshHz -gt 0) "RefreshHz=$($lifetimeData.Dwm.RefreshHz)"
}
$idleReport = Invoke-Ssctl '01b-report-lifetime-idle' @('perf', 'report', '--idle')
$idleData = Get-Data $idleReport
if ($null -ne $idleData -and $null -ne $lifetimeData) {
    Add-Check 'includeIdle lists at least as many metrics' (@($idleData.Metrics).Count -ge @($lifetimeData.Metrics).Count -and @($idleData.Metrics).Count -gt 0) "idle=$(@($idleData.Metrics).Count) default=$(@($lifetimeData.Metrics).Count)"
}

# 2. Window begin / list / end.
$begin = Invoke-Ssctl '02a-window-begin' @('perf', 'window', 'begin', 'w1')
Add-Check 'perf window begin' ($begin.Exit -eq 0) "exit=$($begin.Exit)"
Start-Sleep -Seconds ([Math]::Max(1, [int]($WindowSeconds / 2)))
$during = Invoke-Ssctl '02a2-report-during-window' @('perf', 'report')
$duringData = Get-Data $during
Add-Check 'sensor demand is active while a window is open' ($null -ne $duringData -and $duringData.Sensors.Active -eq $true -and $duringData.Sensors.Reason -like 'window:*w1*') "Sensors=$($duringData.Sensors.Reason)"
Start-Sleep -Seconds ([Math]::Max(1, $WindowSeconds - [int]($WindowSeconds / 2)))
$list = Invoke-Ssctl '02b-window-list' @('perf', 'window', 'list')
$listData = Get-Data $list
Add-Check 'window list shows w1' ($list.Exit -eq 0 -and @($listData.Windows | Where-Object { $_.Name -eq 'w1' }).Count -eq 1) "count=$($listData.Count)"
$endW = Invoke-Ssctl '02c-window-end' @('perf', 'window', 'end', 'w1')
$endData = Get-Data $endW
Add-Check 'perf window end returns a window report' ($endW.Exit -eq 0 -and $null -ne $endData -and $endData.Window -eq 'w1' -and $endData.Since -eq 'window') "exit=$($endW.Exit)"
if ($null -ne $endData) {
    $durationSeconds = [double]$endData.DurationMs / 1000
    Add-Check 'window duration matches the wait' ($durationSeconds -ge ($WindowSeconds - 0.5) -and $durationSeconds -le ($WindowSeconds + 5)) ("DurationMs={0:N0} expected about {1} s" -f $endData.DurationMs, $WindowSeconds)
}
$readAfterEnd = Invoke-Ssctl '02d-window-read-after-end' @('perf', 'window', 'read', 'w1')
Add-Check 'ended window is gone' ($readAfterEnd.Exit -ne 0) "exit=$($readAfterEnd.Exit)"

# 3. Marker.
$mark = Invoke-Ssctl '03-mark' @('perf', 'mark', 'hello')
Add-Check 'perf mark' ($mark.Exit -eq 0) "exit=$($mark.Exit)"

# 4. Frame trace.
$tracePath = Join-Path $OutputDirectory 'frame-trace.json'
if (Test-Path -LiteralPath $tracePath) { Remove-Item -LiteralPath $tracePath -Force }
$traceStart = Invoke-Ssctl '04a-trace-start' @('perf', 'trace', 'start', '--capacity', '65536')
Add-Check 'perf trace start' ($traceStart.Exit -eq 0) "exit=$($traceStart.Exit)"
Start-Sleep -Seconds ([Math]::Max(1, [int]($TraceSeconds / 2)))
$null = Invoke-Ssctl '04b-trace-marker' @('perf', 'mark', 'trace-marker')
Start-Sleep -Seconds ([Math]::Max(1, [int]($TraceSeconds / 2)))
$traceStop = Invoke-Ssctl '04c-trace-stop' @('perf', 'trace', 'stop')
Add-Check 'perf trace stop' ($traceStop.Exit -eq 0) "exit=$($traceStop.Exit)"
$traceDump = Invoke-Ssctl '04d-trace-dump' @('perf', 'trace', 'dump', $tracePath)
Add-Check 'perf trace dump' ($traceDump.Exit -eq 0 -and (Test-Path -LiteralPath $tracePath)) "exit=$($traceDump.Exit) path=$tracePath"
if (Test-Path -LiteralPath $tracePath) {
    $trace = $null
    try { $trace = Get-Content -LiteralPath $tracePath -Raw -Encoding UTF8 | ConvertFrom-Json } catch { $trace = $null }
    Add-Check 'trace dump parses as JSON with traceEvents' ($null -ne $trace -and $null -ne $trace.traceEvents) "bytes=$((Get-Item -LiteralPath $tracePath).Length)"
    if ($null -ne $trace) {
        $events = @($trace.traceEvents)
        $markers = @($events | Where-Object { $_.ph -eq 'i' -and $_.name -eq 'marker' -and $_.args.text -eq 'trace-marker' })
        Add-Check 'trace contains the marker' ($markers.Count -ge 1) "markers=$($markers.Count)"
        $metadata = @($events | Where-Object { $_.ph -eq 'M' })
        $threadNames = @($metadata | Where-Object { $_.name -eq 'thread_name' })
        $gcEvents = @($events | Where-Object { $_.name -like '*Gc*' -or $_.name -like '*GC*' })
        Add-Check 'trace carries process metadata' ($metadata.Count -ge 1) "metadata events=$($metadata.Count) thread_name=$($threadNames.Count)"
        Write-Host ("INFO trace events={0} gcEvents={1} (thread names appear once dedicated threads register)" -f $events.Count, $gcEvents.Count)
    }
}
$traceStatus = Invoke-Ssctl '04e-trace-status' @('perf', 'trace', 'status')
$traceStatusData = Get-Data $traceStatus
Add-Check 'trace status reports the frozen buffer' ($traceStatus.Exit -eq 0 -and $traceStatusData.Status.Running -eq $false -and $traceStatusData.Status.HasBuffer -eq $true) "exit=$($traceStatus.Exit)"

# 5. Knobs.
$knobs = Invoke-Ssctl '05-knobs' @('knobs')
$knobData = Get-Data $knobs
Add-Check 'knobs lists the catalog' ($knobs.Exit -eq 0 -and $knobData.Count -ge 40) "count=$($knobData.Count)"
if ($null -ne $knobData) {
    Add-Check 'poll knob is cataloged' (@($knobData.Knobs | Where-Object { $_.Name -eq 'SUSSUDIO_DIAGNOSTICS_POLL_MS' }).Count -eq 1) 'SUSSUDIO_DIAGNOSTICS_POLL_MS'
    Add-Check 'automation token is never listed as a knob' (@($knobData.Knobs | Where-Object { $_.Name -eq 'SUSSUDIO_AUTOMATION_TOKEN' }).Count -eq 0) 'excluded by design'
}

# 6. Live knob: the diagnostics hub tick should follow SUSSUDIO_DIAGNOSTICS_POLL_MS.
Start-Sleep -Seconds 12
$baseline = Get-HubIntervalStats '06a-timeline-baseline' ([DateTimeOffset]::UtcNow.AddSeconds(-12))
$set = Invoke-Ssctl '06b-knob-set-250' @('knob', 'set', 'SUSSUDIO_DIAGNOSTICS_POLL_MS', '250')
$setAt = [DateTimeOffset]::UtcNow
Add-Check 'knob set accepts an in-range value' ($set.Exit -eq 0) "exit=$($set.Exit)"
Start-Sleep -Seconds 12
$fast = Get-HubIntervalStats '06c-timeline-250' $setAt.AddSeconds(1)
$reset = Invoke-Ssctl '06d-knob-reset' @('knob', 'reset', 'SUSSUDIO_DIAGNOSTICS_POLL_MS')
$resetAt = [DateTimeOffset]::UtcNow
Add-Check 'knob reset' ($reset.Exit -eq 0) "exit=$($reset.Exit)"
Start-Sleep -Seconds 12
$restored = Get-HubIntervalStats '06e-timeline-restored' $resetAt.AddSeconds(1)
if ($null -ne $baseline -and $null -ne $fast) {
    Add-Check 'hub ticks faster after set 250' ($fast.P25Ms -lt ($baseline.P25Ms * 0.75)) ("lower-quartile interval baseline={0:N0} ms, at 250={1:N0} ms (medians {2:N0} and {3:N0} ms, {4} and {5} intervals)" -f $baseline.P25Ms, $fast.P25Ms, $baseline.MedianMs, $fast.MedianMs, $baseline.Intervals, $fast.Intervals)
}
else {
    Add-Check 'hub ticks faster after set 250' $false 'timeline had too few entries to measure'
}
if ($null -ne $baseline -and $null -ne $restored) {
    Add-Check 'hub tick rate restored after reset' ([Math]::Abs($restored.P25Ms - $baseline.P25Ms) -lt ($baseline.P25Ms * 0.25)) ("lower-quartile interval baseline={0:N0} ms, restored={1:N0} ms ({2} intervals)" -f $baseline.P25Ms, $restored.P25Ms, $restored.Intervals)
}
else {
    Add-Check 'hub tick rate restored after reset' $false 'timeline had too few entries to measure'
}

# 6b. Cadence timeline: on-demand reads (each `ssctl state` refreshes the snapshot) add no timeline entries, so a
# burst of reads leaves one entry per poll interval. Before the fix every read appended its own entry.
$pollKnob = @((Get-Data (Invoke-Ssctl '06f-knobs-poll' @('knobs', 'POLL_MS'))).Knobs | Where-Object { $_.Name -eq 'SUSSUDIO_DIAGNOSTICS_POLL_MS' })
$pollMs = if ($pollKnob.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($pollKnob[0].Value)) { [double]$pollKnob[0].Value } elseif ($pollKnob.Count -eq 1) { [double]$pollKnob[0].Default } else { 500.0 }
$burstReads = 12
$burstStart = [DateTimeOffset]::UtcNow
for ($i = 0; $i -lt $burstReads; $i++) { $null = Invoke-Ssctl ('06g-burst-state-{0:00}' -f $i) @('state') }
$burstEnd = [DateTimeOffset]::UtcNow
Start-Sleep -Milliseconds ([int]($pollMs * 2))
$burstTimes = Get-TimelineTimestamps '06h-timeline-after-burst' $burstStart
if ($null -ne $burstTimes) {
    $inBurst = @($burstTimes | Where-Object { $_ -le $burstEnd })
    $burstMs = ($burstEnd - $burstStart).TotalMilliseconds
    $maxTicks = [int][Math]::Ceiling($burstMs / $pollMs) + 1
    $minSpacing = if ($inBurst.Count -ge 2) { (@(for ($i = 1; $i -lt $inBurst.Count; $i++) { ($inBurst[$i] - $inBurst[$i - 1]).TotalMilliseconds }) | Measure-Object -Minimum).Minimum } else { $null }
    Add-Check 'on-demand reads add no timeline entries' ($inBurst.Count -le $maxTicks -and ($null -eq $minSpacing -or $minSpacing -ge ($pollMs / 2))) ("{0} entries in a {1:N0} ms burst of {2} state reads (at most {3} poll ticks at {4:N0} ms), smallest spacing {5} ms" -f $inBurst.Count, $burstMs, $burstReads, $maxTicks, $pollMs, $(if ($null -eq $minSpacing) { 'n/a' } else { '{0:N0}' -f $minSpacing }))
}
else {
    Add-Check 'on-demand reads add no timeline entries' $false 'timeline response had no data'
}

# 7. Rejections: out of range, unknown name, credential.
$tooLow = Invoke-Ssctl '07a-knob-set-out-of-range' @('knob', 'set', 'SUSSUDIO_DIAGNOSTICS_POLL_MS', '50')
Add-Check 'out-of-range value is rejected, not clamped' ($tooLow.Exit -ne 0 -and (Get-Data $tooLow).Failure -eq 'OutOfRange') "exit=$($tooLow.Exit) failure=$((Get-Data $tooLow).Failure)"
$wrongType = Invoke-Ssctl '07b-knob-set-wrong-type' @('knob', 'set', 'SUSSUDIO_DIAGNOSTICS_POLL_MS', 'fast')
Add-Check 'wrong-type value is rejected' ($wrongType.Exit -ne 0 -and (Get-Data $wrongType).Failure -eq 'WrongType') "exit=$($wrongType.Exit) failure=$((Get-Data $wrongType).Failure)"
# Reset, not set: the credential knob is excluded, so this must be rejected as unknown, and no value is ever sent.
$token = Invoke-Ssctl '07c-knob-reset-token' @('knob', 'reset', 'SUSSUDIO_AUTOMATION_TOKEN')
Add-Check 'automation token is not a tunable knob' ($token.Exit -ne 0 -and (Get-Data $token).Failure -eq 'UnknownKnob') "exit=$($token.Exit) failure=$((Get-Data $token).Failure)"
$afterRejects = Invoke-Ssctl '07d-knobs-after-rejects' @('knobs', 'POLL_MS')
$pollAfter = @((Get-Data $afterRejects).Knobs | Where-Object { $_.Name -eq 'SUSSUDIO_DIAGNOSTICS_POLL_MS' })
Add-Check 'rejected sets left the knob at its launch value' ($pollAfter.Count -eq 1 -and $pollAfter[0].Overridden -eq $false) "overridden=$(if ($pollAfter.Count -eq 1) { $pollAfter[0].Overridden } else { 'n/a' })"

# 8. Log tail.
$tail = Invoke-Ssctl '08a-log-tail' @('log', 'tail', '--max', '20')
$tailData = Get-Data $tail
Add-Check 'log tail returns lines' ($tail.Exit -eq 0 -and @($tailData.Lines).Count -ge 1 -and @($tailData.Lines).Count -le 20) "lines=$(@($tailData.Lines).Count) total=$($tailData.TotalLines)"
$knobLog = Invoke-Ssctl '08b-log-tail-knob-lines' @('log', 'tail', '--max', '20', '--contains', 'TUNING_KNOB_SET')
Add-Check 'knob changes are logged' ($knobLog.Exit -eq 0 -and @((Get-Data $knobLog).Lines).Count -ge 2) "lines=$(@((Get-Data $knobLog).Lines).Count)"

# 9. Final lifetime report with the marker and cleanup state.
$final = Invoke-Ssctl '09-report-final' @('perf', 'report')
$finalData = Get-Data $final
if ($null -ne $finalData) {
    Add-Check 'report lists the hello marker' (@($finalData.Markers | Where-Object { $_.Text -eq 'hello' }).Count -ge 1) "markers=$(@($finalData.Markers).Count)"
    Add-Check 'no window or trace left running' ($finalData.Trace.Running -eq $false -and $finalData.Sensors.Active -eq $false) "trace.running=$($finalData.Trace.Running) sensors=$($finalData.Sensors.Reason)"
}

$failed = @($checks | Where-Object { -not $_.passed })
$result = [ordered]@{
    schemaVersion = 1
    startedUtc    = $startedUtc
    finishedUtc   = (Get-Date).ToUniversalTime().ToString('o')
    ssctl         = $SsctlPath
    outputDirectory = $OutputDirectory
    passed        = ($failed.Count -eq 0)
    checkCount    = $checks.Count
    failedCount   = $failed.Count
    checks        = $checks
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verify-result.json') -Encoding UTF8
Write-Host ("{0} of {1} checks passed" -f ($checks.Count - $failed.Count), $checks.Count)
if ($failed.Count -gt 0) { exit 1 }
exit 0
