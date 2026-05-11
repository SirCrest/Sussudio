<# Smoke-tests the automation pipe through preview, optional recording, and
snapshot assertions without requiring manual UI interaction. #>
param(
    [string]$PipeName = "SussudioAutomation",
    [string]$AuthToken = "",
    [int]$WaitTimeoutMs = 45000,
    [int]$WaitPollMs = 250,
    [switch]$SkipRecording,
    [switch]$AllowPreviewRunningAtEnd
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:Root = Split-Path -Parent $PSScriptRoot
$script:SendCommandPath = Join-Path $PSScriptRoot "send-automation-command.ps1"
if (!(Test-Path $script:SendCommandPath)) {
    throw "Missing automation sender script: $script:SendCommandPath"
}

function Invoke-Automation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [hashtable]$Payload = @{},
        [switch]$AllowFailure
    )

    $payloadJson = $Payload | ConvertTo-Json -Depth 12 -Compress
    $raw = & $script:SendCommandPath `
        -Command $Command `
        -PipeName $PipeName `
        -AuthToken $AuthToken `
        -PayloadJson $payloadJson
    $response = $raw | ConvertFrom-Json

    if (-not $AllowFailure -and -not $response.Success) {
        throw "Automation command '$Command' failed: $($response.Message) [ErrorCode=$($response.ErrorCode)]"
    }

    return $response
}

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw "Smoke assertion failed: $Message"
    }
}

function Get-Snapshot {
    $response = Invoke-Automation -Command "GetSnapshot"
    if ($null -eq $response.Snapshot) {
        throw "GetSnapshot returned no snapshot payload."
    }

    return $response.Snapshot
}

function Wait-Condition {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConditionName
    )

    $response = Invoke-Automation -Command "WaitForCondition" -Payload @{
        condition = $ConditionName
        timeoutMs = $WaitTimeoutMs
        pollMs = $WaitPollMs
    } -AllowFailure

    if (-not $response.Success) {
        throw "Timed out waiting for condition '$ConditionName': $($response.Message)"
    }
}

Write-Host "Automation snapshot smoke: starting"

# Track whether the smoke run actually toggled preview/recording on so the
# finally block can roll the app back to a known-safe state even on Ctrl-C
# (PowerShell PipelineStoppedException) or unexpected exception. Without this
# wrapper, an operator Ctrl-C between SetRecordingEnabled=true and the
# matching SetRecordingEnabled=false leaves the app recording indefinitely.
$script:SmokePreviewEnabled = $false
$script:SmokeRecordingEnabled = $false

function Restore-SmokeAppState {
    if ($script:SmokeRecordingEnabled) {
        try {
            Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } -AllowFailure | Out-Null
        }
        catch {
            Write-Warning "Smoke cleanup: SetRecordingEnabled=false failed: $($_.Exception.Message)"
        }
    }

    if ($script:SmokePreviewEnabled) {
        try {
            Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $false } -AllowFailure | Out-Null
        }
        catch {
            Write-Warning "Smoke cleanup: SetPreviewEnabled=false failed: $($_.Exception.Message)"
        }
    }
}

try {

$idle = Get-Snapshot
Assert-Condition ($null -ne $idle.SessionState -and $idle.SessionState.Length -gt 0) "Idle snapshot missing SessionState."
Assert-Condition ($null -ne $idle.TelemetryAlignmentStatus -and $idle.TelemetryAlignmentStatus.Length -gt 0) "Idle snapshot missing TelemetryAlignmentStatus."
Assert-Condition ($null -ne $idle.RequestedPipelineMode -and $idle.RequestedPipelineMode.Length -gt 0) "Idle snapshot missing RequestedPipelineMode."
Assert-Condition ($null -ne $idle.ActivePipelineMode -and $idle.ActivePipelineMode.Length -gt 0) "Idle snapshot missing ActivePipelineMode."
Assert-Condition ([bool]$idle.PipelineModeMatched) "Idle snapshot expected PipelineModeMatched=true."

Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $true } | Out-Null
$script:SmokePreviewEnabled = $true
Wait-Condition -ConditionName "PreviewRendererHealthy"

$preview = Get-Snapshot
Assert-Condition ([bool]$preview.IsPreviewing) "Preview snapshot expected IsPreviewing=true."
Assert-Condition ($preview.PreviewRendererMode -ne $null -and $preview.PreviewRendererMode.Length -gt 0) "Preview snapshot missing renderer mode."
Assert-Condition ([bool]$preview.PipelineModeMatched) "Preview snapshot expected PipelineModeMatched=true."

Invoke-Automation -Command "SetHdrEnabled" -Payload @{ enabled = $false } | Out-Null
$sdr = Get-Snapshot
Assert-Condition ([bool]$sdr.PipelineModeMatched) "SDR snapshot expected PipelineModeMatched=true."
Assert-Condition ($sdr.RequestedPipelineMode -eq "SDR") "SDR snapshot expected RequestedPipelineMode=SDR."

$hdrToggleVerified = $false
if ([bool]$preview.IsHdrAvailable) {
    Invoke-Automation -Command "SetHdrEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "HdrModeApplied"
    $hdr = Get-Snapshot
    Assert-Condition ([bool]$hdr.IsHdrEnabled) "HDR snapshot expected IsHdrEnabled=true after toggle."
    Assert-Condition ($hdr.RequestedPipelineMode -eq "HDR10-PQ") "HDR snapshot expected RequestedPipelineMode=HDR10-PQ."
    Assert-Condition ([bool]$hdr.PipelineModeMatched) "HDR snapshot expected PipelineModeMatched=true."
    $hdrToggleVerified = $true

    Invoke-Automation -Command "SetHdrEnabled" -Payload @{ enabled = $false } | Out-Null
    $sdr = Get-Snapshot
    Assert-Condition ([bool]$sdr.PipelineModeMatched) "SDR snapshot expected PipelineModeMatched=true after HDR reset."
}

if (-not $SkipRecording) {
    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $true } | Out-Null
    $script:SmokeRecordingEnabled = $true
    Wait-Condition -ConditionName "RecordingFileGrowing"

    $recording = Get-Snapshot
    Assert-Condition ([bool]$recording.IsRecording) "Recording snapshot expected IsRecording=true."
    Assert-Condition ($recording.RecordingBackend -ne "None") "Recording snapshot expected active recording backend."
    Assert-Condition ($null -ne $recording.ObservedP010FrameCount) "Recording snapshot missing observed frame telemetry."
    Assert-Condition ($null -ne $recording.ObservedNv12FrameCount) "Recording snapshot missing observed NV12 telemetry."
    Assert-Condition ($recording.RequestedPipelineMode -ne $null -and $recording.RequestedPipelineMode.Length -gt 0) "Recording snapshot missing RequestedPipelineMode."
    Assert-Condition ($recording.ActivePipelineMode -ne $null -and $recording.ActivePipelineMode.Length -gt 0) "Recording snapshot missing ActivePipelineMode."
    Assert-Condition ([bool]$recording.PipelineModeMatched) "Recording snapshot expected PipelineModeMatched=true."
    Assert-Condition ($recording.PipelineModeStatus -eq "Active") "Recording snapshot expected PipelineModeStatus=Active."

    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } | Out-Null
    $script:SmokeRecordingEnabled = $false
    Wait-Condition -ConditionName "RecordingStopped"

    $stopped = Get-Snapshot
    Assert-Condition (-not [bool]$stopped.IsRecording) "Post-stop snapshot expected IsRecording=false."
    Assert-Condition ($null -ne $stopped.MuxResult -and $stopped.MuxResult.Length -gt 0) "Post-stop snapshot missing mux result."
}

Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $false } | Out-Null
$script:SmokePreviewEnabled = $false
Start-Sleep -Milliseconds 500

$final = Get-Snapshot
Assert-Condition (-not [bool]$final.IsRecording) "Final snapshot expected IsRecording=false."
if (-not $AllowPreviewRunningAtEnd) {
    Assert-Condition (-not [bool]$final.IsPreviewing) "Final snapshot expected IsPreviewing=false."
}
Assert-Condition ($null -ne $final.TelemetryAlignmentStatus -and $final.TelemetryAlignmentStatus.Length -gt 0) "Final snapshot missing TelemetryAlignmentStatus."
Assert-Condition ([bool]$final.PipelineModeMatched) "Final snapshot expected PipelineModeMatched=true."

$summary = [ordered]@{
    IdleState = $idle.SessionState
    PreviewRendererMode = $preview.PreviewRendererMode
    HdrToggleVerified = $hdrToggleVerified
    RequestedPipelineMode = $final.RequestedPipelineMode
    ActivePipelineMode = $final.ActivePipelineMode
    FinalState = $final.SessionState
    FinalTelemetryAlignmentStatus = $final.TelemetryAlignmentStatus
    FinalMuxResult = $final.MuxResult
}

Write-Host "Automation snapshot smoke: PASS"
$summary | ConvertTo-Json -Depth 8

}
finally {
    Restore-SmokeAppState
}
