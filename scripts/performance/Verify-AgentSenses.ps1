<#
.SYNOPSIS
End-to-end check of the agent senses (audio output probe, UI tree and UI invoke) against a running Sussudio app.
.DESCRIPTION
Drives ssctl through audio listen, ui tree and ui invoke, saves every raw response under the output directory, and
writes verify-result.json with one entry per check. Exit code is 0 only when every check passed. Re-run it against
any build to compare.

Preconditions (not changed by this script): the app is running, ideally with live preview and preview audio on. The
script toggles the Stats overlay twice and moves the preview volume slider one step and back, so both end where they
started; it changes no persisted capture, recording or Flashback setting. With a silent source the probe reports
no-audio-rendered, which is a pass here (the checks are on the plumbing); the result says which path ran, so a run
with audible content can be told apart from one without.
.EXAMPLE
./scripts/performance/Verify-AgentSenses.ps1 -OutputDirectory artifacts/perf-overhaul/senses
#>
[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $SsctlPath,
    [string] $PipeName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ([string]::IsNullOrWhiteSpace($SsctlPath)) {
    $SsctlPath = Join-Path $repoRoot 'tools/ssctl/bin/Debug/net8.0/ssctl.exe'
}
$SsctlPath = (Resolve-Path -LiteralPath $SsctlPath).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts/perf-overhaul/senses'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$startedUtc = (Get-Date).ToUniversalTime().ToString('o')

$checks = New-Object System.Collections.Generic.List[object]
$observations = [ordered]@{}

function Add-Check {
    param([string] $Name, [bool] $Passed, [string] $Detail)
    $checks.Add([pscustomobject]@{ name = $Name; passed = $Passed; detail = $Detail })
    $mark = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1} - {2}" -f $mark, $Name, $Detail)
}

function Get-SsctlArgs([string[]] $CommandArgs) {
    $argList = @('--json')
    if (-not [string]::IsNullOrWhiteSpace($PipeName)) { $argList += @('--pipe', $PipeName) }
    return $argList + $CommandArgs
}

# Runs ssctl with --json, saves the raw output, and returns the parsed response plus the exit code.
function Invoke-Ssctl {
    param([string] $Label, [string[]] $CommandArgs)
    $argList = Get-SsctlArgs $CommandArgs
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

function Get-Field($object, [string] $Name) {
    if ($null -ne $object -and $object.PSObject.Properties.Name -contains $Name) { return $object.$Name }
    return $null
}

function Get-Data($result) { return Get-Field $result.Response 'Data' }
function Get-ErrorCode($result) { return Get-Field $result.Response 'ErrorCode' }

function Get-TreeNodes($treeData) {
    $nodes = New-Object System.Collections.Generic.List[object]
    $stack = New-Object System.Collections.Stack
    foreach ($root in @(Get-Field $treeData 'Roots')) { if ($null -ne $root) { $stack.Push($root) } }
    while ($stack.Count -gt 0) {
        $node = $stack.Pop()
        $nodes.Add($node)
        foreach ($child in @(Get-Field $node 'Children')) { if ($null -ne $child) { $stack.Push($child) } }
    }
    return $nodes
}

$state = Invoke-Ssctl 'state' @('state')
Add-Check 'app reachable over the pipe' ($state.Exit -eq 0) "ssctl state exit=$($state.Exit)"
if ($state.Exit -ne 0) {
    $checks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verify-result.json') -Encoding UTF8
    exit 1
}
$snapshot = Get-Data $state
if ($null -eq $snapshot) { $snapshot = Get-Field $state.Response 'Snapshot' }
$audioPreviewOn = [bool](Get-Field $snapshot 'IsAudioPreviewEnabled')
$observations.audioPreviewEnabled = $audioPreviewOn
$observations.previewing = [bool](Get-Field $snapshot 'IsPreviewing')

# ---- Ears ----------------------------------------------------------------------------------------------------------

$plain = Invoke-Ssctl 'audio-listen-2s' @('audio', 'listen', '--seconds', '2')
$plainData = Get-Data $plain
$plainStatus = Get-Field $plainData 'Status'
$observations.listenStatus = $plainStatus
Add-Check 'audio listen captures or reports silence, never an activation failure' ($plainStatus -in @('captured', 'no-audio-rendered')) "status=$plainStatus exit=$($plain.Exit)"
$loopbackPackets = Get-Field (Get-Field $plainData 'Diagnostics') 'LoopbackPackets'
# The process loopback stream delivers 10 ms packets whether or not anything is rendering (zero-filled when idle).
Add-Check 'loopback delivers packets for the whole capture' ($null -ne $loopbackPackets -and $loopbackPackets -ge 150) "loopbackPackets=$loopbackPackets for 2 s"
Add-Check 'a silent capture is a failure exit, a real one is not' (($plainStatus -eq 'captured' -and $plain.Exit -eq 0) -or ($plainStatus -eq 'no-audio-rendered' -and $plain.Exit -ne 0 -and (Get-ErrorCode $plain) -eq 'command-failed' -and $null -ne (Get-Field $plainData 'Analysis'))) "status=$plainStatus exit=$($plain.Exit) code=$(Get-ErrorCode $plain)"

$wavPath = Join-Path $OutputDirectory 'listen-latency.wav'
if (Test-Path -LiteralPath $wavPath) { Remove-Item -LiteralPath $wavPath -Force }
$latencyRun = Invoke-Ssctl 'audio-listen-latency' @('audio', 'listen', '--seconds', '3', '--latency', '--wav', $wavPath)
$latencyData = Get-Data $latencyRun
$latency = Get-Field $latencyData 'Latency'
$latencyStatus = Get-Field $latency 'Status'
$observations.latencyStatus = $latencyStatus
$observations.latencyMs = Get-Field $latency 'LatencyMs'
Add-Check 'latency result is a measurement or an explained refusal' (($latencyStatus -eq 'ok' -and (Get-Field $latency 'Confidence') -ge 0.6) -or ($latencyStatus -in @('insufficient-signal', 'unavailable') -and -not [string]::IsNullOrWhiteSpace((Get-Field $latency 'Reason')))) "status=$latencyStatus confidence=$(Get-Field $latency 'Confidence') reason=$(Get-Field $latency 'Reason')"
Add-Check 'latency states its scope' (-not [string]::IsNullOrWhiteSpace((Get-Field $latency 'Scope'))) "scope present=$(-not [string]::IsNullOrWhiteSpace((Get-Field $latency 'Scope')))"
$wavBytes = Get-Field $latencyData 'WavBytes'
$fileBytes = if (Test-Path -LiteralPath $wavPath) { (Get-Item -LiteralPath $wavPath).Length } else { -1 }
# 3 s of 48 kHz float32 stereo plus the header.
Add-Check 'the WAV is written and matches the reported size' ($fileBytes -gt 1100000 -and $fileBytes -eq $wavBytes) "file=$fileBytes reported=$wavBytes"
$diagnostics = Get-Field $latencyData 'Diagnostics'
if ($audioPreviewOn) {
    Add-Check 'the capture-delivery tap saw packets and rejected none' ((Get-Field $diagnostics 'TapPackets') -gt 0 -and (Get-Field $diagnostics 'TapRejectedPackets') -eq 0) "tapPackets=$(Get-Field $diagnostics 'TapPackets') rejected=$(Get-Field $diagnostics 'TapRejectedPackets')"
}
else {
    Write-Host 'INFO preview audio is off: the tap is offered only by a capture that feeds preview playback, so its count is not checked'
}

# A second latency measurement while the first holds the tap must be refused, not queued or merged.
$job = Start-Job -ScriptBlock { param($exe, $argList) (& $exe @argList 2>&1 | Out-String) } -ArgumentList $SsctlPath, (Get-SsctlArgs @('audio', 'listen', '--seconds', '4', '--latency'))
Start-Sleep -Milliseconds 1500
$second = Invoke-Ssctl 'audio-listen-overlap-second' @('audio', 'listen', '--seconds', '1', '--latency')
$firstText = Receive-Job -Job $job -Wait
Remove-Job -Job $job
Set-Content -LiteralPath (Join-Path $OutputDirectory 'audio-listen-overlap-first.json') -Value $firstText -Encoding UTF8
Add-Check 'an overlapping latency measurement is refused as tap-busy' ((Get-Field (Get-Data $second) 'Status') -eq 'tap-busy' -and (Get-ErrorCode $second) -eq 'command-failed') "status=$(Get-Field (Get-Data $second) 'Status') code=$(Get-ErrorCode $second)"

foreach ($case in @(@{ Label = 'too-short'; Args = @('audio', 'listen', '--seconds', '0.1') },
                    @{ Label = 'too-long'; Args = @('audio', 'listen', '--seconds', '31') },
                    @{ Label = 'not-wav'; Args = @('audio', 'listen', '--seconds', '1', '--wav', (Join-Path $OutputDirectory 'bad.txt')) })) {
    $rejected = Invoke-Ssctl "audio-listen-reject-$($case.Label)" $case.Args
    Add-Check "audio listen rejects $($case.Label) input, never clamps" ($rejected.Exit -ne 0 -and (Get-ErrorCode $rejected) -eq 'invalid-request') "code=$(Get-ErrorCode $rejected) message=$(Get-Field $rejected.Response 'Message')"
}

# ---- Fingers -------------------------------------------------------------------------------------------------------

$tree = Invoke-Ssctl 'ui-tree' @('ui', 'tree')
$treeData = Get-Data $tree
$nodes = Get-TreeNodes $treeData
$observations.treeEmitted = Get-Field $treeData 'EmittedNodes'
$observations.treeVisited = Get-Field $treeData 'VisitedElements'
Add-Check 'ui tree returns interactive nodes untruncated' ($tree.Exit -eq 0 -and $nodes.Count -gt 0 -and (Get-Field $treeData 'Truncated') -eq $false) "nodes=$($nodes.Count) visited=$(Get-Field $treeData 'VisitedElements') truncated=$(Get-Field $treeData 'Truncated')"
$statsNode = $nodes | Where-Object { (Get-Field $_ 'Name') -eq 'StatsToggle' } | Select-Object -First 1
Add-Check 'ui tree lists the Stats toggle with its Toggle pattern' ($null -ne $statsNode -and @(Get-Field $statsNode 'Patterns') -contains 'Toggle') "found=$($null -ne $statsNode)"

$shallow = Invoke-Ssctl 'ui-tree-shallow' @('ui', 'tree', '--all', '--depth', '2')
Add-Check 'a depth limit is reported as truncation' ((Get-Field (Get-Data $shallow) 'Truncated') -eq $true -and -not [string]::IsNullOrWhiteSpace((Get-Field (Get-Data $shallow) 'TruncationReason'))) "truncated=$(Get-Field (Get-Data $shallow) 'Truncated')"

$toggleBefore = Get-Field $statsNode 'ToggleState'
$flip = Invoke-Ssctl 'ui-invoke-stats-toggle' @('ui', 'invoke', 'StatsToggle', 'toggle')
$flipData = Get-Data $flip
$toggleAfter = Get-Field (Get-Field $flipData 'Element') 'ToggleState'
Add-Check 'toggle goes through the peer and flips the state' ((Get-Field $flipData 'Status') -eq 'invoked' -and (Get-Field $flipData 'Via') -eq 'peer:Toggle' -and $null -ne $toggleAfter -and $toggleAfter -ne $toggleBefore) "before=$toggleBefore after=$toggleAfter via=$(Get-Field $flipData 'Via')"
$back = Invoke-Ssctl 'ui-invoke-stats-toggle-back' @('ui', 'invoke', 'StatsToggle', 'toggle')
Add-Check 'a second toggle restores the original state' ((Get-Field (Get-Field (Get-Data $back) 'Element') 'ToggleState') -eq $toggleBefore) "restored=$(Get-Field (Get-Field (Get-Data $back) 'Element') 'ToggleState')"

$sliderNode = $nodes | Where-Object { (Get-Field $_ 'Name') -eq 'PreviewVolumeSlider' } | Select-Object -First 1
$range = Get-Field $sliderNode 'Range'
if ($null -ne $range) {
    $original = [double](Get-Field $range 'Value')
    $maximum = [double](Get-Field $range 'Maximum')
    $step = if ($original -ge 1) { $original - 1 } else { $original + 1 }
    $stepText = $step.ToString([Globalization.CultureInfo]::InvariantCulture)
    $set = Invoke-Ssctl 'ui-invoke-volume-step' @('ui', 'invoke', 'PreviewVolumeSlider', 'set-value', $stepText)
    $setValue = Get-Field (Get-Field (Get-Field (Get-Data $set) 'Element') 'Range') 'Value'
    Add-Check 'set-value moves the slider through its RangeValue peer' ((Get-Field (Get-Data $set) 'Via') -eq 'peer:RangeValue' -and $setValue -eq $step) "value=$setValue expected=$step"
    $restore = Invoke-Ssctl 'ui-invoke-volume-restore' @('ui', 'invoke', 'PreviewVolumeSlider', 'set-value', $original.ToString([Globalization.CultureInfo]::InvariantCulture))
    Add-Check 'the slider is restored' ((Get-Field (Get-Field (Get-Field (Get-Data $restore) 'Element') 'Range') 'Value') -eq $original) "value=$(Get-Field (Get-Field (Get-Field (Get-Data $restore) 'Element') 'Range') 'Value') original=$original"
    $outOfRange = Invoke-Ssctl 'ui-invoke-volume-out-of-range' @('ui', 'invoke', 'PreviewVolumeSlider', 'set-value', ($maximum + 1).ToString([Globalization.CultureInfo]::InvariantCulture))
    Add-Check 'an out-of-range value is rejected, never clamped' ((Get-ErrorCode $outOfRange) -eq 'invalid-request' -and (Get-Field (Get-Data $outOfRange) 'Status') -eq 'invalid-value') "status=$(Get-Field (Get-Data $outOfRange) 'Status')"
    $notNumber = Invoke-Ssctl 'ui-invoke-volume-not-number' @('ui', 'invoke', 'PreviewVolumeSlider', 'set-value', 'abc')
    Add-Check 'a non-numeric value is rejected' ((Get-Field (Get-Data $notNumber) 'Status') -eq 'invalid-value') "status=$(Get-Field (Get-Data $notNumber) 'Status')"
}
else {
    Add-Check 'ui tree lists the preview volume slider with its range' $false 'PreviewVolumeSlider or its Range missing from the tree'
}

$missing = Invoke-Ssctl 'ui-invoke-not-found' @('ui', 'invoke', 'NoSuchElementAnywhere')
Add-Check 'an unknown target is not-found, never a close guess' ((Get-ErrorCode $missing) -eq 'invalid-request' -and (Get-Field (Get-Data $missing) 'Status') -eq 'not-found') "status=$(Get-Field (Get-Data $missing) 'Status')"
$accelerator = Invoke-Ssctl 'ui-invoke-accelerator' @('ui', 'invoke', 'RecordButton', 'accelerator')
Add-Check 'raising a keyboard accelerator is refused as unsupported' ((Get-Field (Get-Data $accelerator) 'Status') -eq 'invalid-action') "status=$(Get-Field (Get-Data $accelerator) 'Status')"

$allTree = Invoke-Ssctl 'ui-tree-all' @('ui', 'tree', '--all', '--depth', '24')
$hiddenNode = Get-TreeNodes (Get-Data $allTree) |
    Where-Object { (Get-Field $_ 'Interactive') -eq $true -and (Get-Field $_ 'IsVisible') -eq $false -and -not [string]::IsNullOrWhiteSpace((Get-Field $_ 'Path')) } |
    Select-Object -First 1
if ($null -ne $hiddenNode) {
    $hidden = Invoke-Ssctl 'ui-invoke-hidden' @('ui', 'invoke', (Get-Field $hiddenNode 'Path'))
    Add-Check 'a hidden element is refused with its reason' ((Get-ErrorCode $hidden) -eq 'command-failed' -and (Get-Field (Get-Data $hidden) 'Status') -eq 'hidden') "target=$(Get-Field $hiddenNode 'Path') status=$(Get-Field (Get-Data $hidden) 'Status')"
}
else {
    Write-Host 'INFO no hidden interactive element in the current layout; the hidden-target refusal was not exercised'
}

$failed = @($checks | Where-Object { -not $_.passed })
$result = [ordered]@{
    schemaVersion   = 1
    startedUtc      = $startedUtc
    finishedUtc     = (Get-Date).ToUniversalTime().ToString('o')
    ssctl           = $SsctlPath
    outputDirectory = $OutputDirectory
    passed          = ($failed.Count -eq 0)
    checkCount      = $checks.Count
    failedCount     = $failed.Count
    observations    = $observations
    checks          = $checks
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verify-result.json') -Encoding UTF8
Write-Host ("{0} of {1} checks passed" -f ($checks.Count - $failed.Count), $checks.Count)
if ($failed.Count -gt 0) { exit 1 }
exit 0
