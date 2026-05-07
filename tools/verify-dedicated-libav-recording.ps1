<# End-to-end dedicated libav recording smoke: configures capture, records for a
short window, waits for verification, and checks runtime/ffprobe counters. #>
param(
    [string]$PipeName = "SussudioAutomation",
    [string]$AuthToken = "",
    [int]$DurationSeconds = 15,
    [int]$WaitTimeoutMs = 120000,
    [int]$WaitPollMs = 250,
    [int]$ConnectTimeoutMs = 5000,
    [int]$ResponseTimeoutBufferMs = 5000,
    [string]$OutputDirectory = "",
    [int]$MaxRuntimeQueueDrops = 0,
    [int]$MaxRuntimeCaptureDrops = 0,
    [int]$MaxVerificationEstimatedDrops = 0,
    [int]$MaxVerificationSevereGaps = 0,
    [int]$ExpectedWidth = 3840,
    [int]$ExpectedHeight = 2160,
    [double]$ExpectedFrameRate = 120.0,
    [double]$FrameRateTolerance = 1.0,
    [double]$MinExpectedFrameFraction = 0.80,
    [int]$MaxEncoderLastWriteAgeMs = 2000,
    [switch]$NoRestoreFlashback,
    [switch]$AllowPreviewRunningAtEnd
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($DurationSeconds -lt 1) {
    throw "DurationSeconds must be at least 1."
}

if ($WaitTimeoutMs -lt 1000) {
    throw "WaitTimeoutMs must be at least 1000."
}

if ($WaitPollMs -lt 50) {
    throw "WaitPollMs must be at least 50."
}

if ($ResponseTimeoutBufferMs -lt 0) {
    throw "ResponseTimeoutBufferMs cannot be negative."
}

if ($ExpectedWidth -le 0 -or $ExpectedHeight -le 0 -or $ExpectedFrameRate -le 0) {
    throw "ExpectedWidth, ExpectedHeight, and ExpectedFrameRate must be positive."
}

if ($FrameRateTolerance -lt 0) {
    throw "FrameRateTolerance cannot be negative."
}

if ($MinExpectedFrameFraction -le 0 -or $MinExpectedFrameFraction -gt 1) {
    throw "MinExpectedFrameFraction must be greater than 0 and less than or equal to 1."
}

if ($MaxEncoderLastWriteAgeMs -lt 100) {
    throw "MaxEncoderLastWriteAgeMs must be at least 100."
}

$script:SendCommandPath = Join-Path $PSScriptRoot "send-automation-command.ps1"
if (!(Test-Path $script:SendCommandPath)) {
    throw "Missing automation sender script: $script:SendCommandPath"
}

function Get-PropertyValue {
    param(
        [AllowNull()]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [AllowNull()]
        [object]$Fallback = $null
    )

    if ($null -eq $Object) {
        return $Fallback
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Fallback
    }

    return $property.Value
}

function Convert-ToBool {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $false
    }

    if ($Value -is [bool]) {
        return [bool]$Value
    }

    return [bool]::Parse([string]$Value)
}

function Convert-ToInt64 {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return 0L
    }

    return [long]$Value
}

function Convert-ToDouble {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return 0.0
    }

    return [double]$Value
}

function Convert-ToNullableDouble {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $null
    }

    $text = ([string]$Value).Trim()
    if ([string]::IsNullOrWhiteSpace($text) -or $text -eq "N/A") {
        return $null
    }

    $parsed = 0.0
    if ([double]::TryParse(
            $text,
            [Globalization.NumberStyles]::Float,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref]$parsed)) {
        return $parsed
    }

    return $null
}

function Convert-ToNullableInt64 {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $null
    }

    $text = ([string]$Value).Trim()
    if ([string]::IsNullOrWhiteSpace($text) -or $text -eq "N/A") {
        return $null
    }

    $parsed = 0L
    if ([long]::TryParse(
            $text,
            [Globalization.NumberStyles]::Integer,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref]$parsed)) {
        return $parsed
    }

    return $null
}

function Get-FirstPropertyValue {
    param(
        [AllowNull()]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string[]]$Names,
        [AllowNull()]
        [object]$Fallback = $null
    )

    foreach ($name in $Names) {
        $value = Get-PropertyValue $Object $name $null
        if ($null -ne $value) {
            return $value
        }
    }

    return $Fallback
}

function Invoke-Automation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [hashtable]$Payload = @{},
        [int]$ResponseTimeoutMs = 0,
        [switch]$AllowFailure
    )

    $payloadJson = "{}"
    if ($Payload.Count -gt 0) {
        $payloadJson = $Payload | ConvertTo-Json -Depth 20 -Compress
    }

    $raw = & $script:SendCommandPath `
        -Command $Command `
        -PipeName $PipeName `
        -AuthToken $AuthToken `
        -ConnectTimeoutMs $ConnectTimeoutMs `
        -ResponseTimeoutMs $ResponseTimeoutMs `
        -PayloadJson $payloadJson

    $response = $null
    $rawText = ($raw | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($rawText)) {
        try {
            $response = $rawText | ConvertFrom-Json
        }
        catch {
            if ($LASTEXITCODE -eq 0) {
                throw
            }
        }
    }

    if ($LASTEXITCODE -ne 0) {
        if ($AllowFailure -and $null -ne $response) {
            return $response
        }

        if ($null -ne $response) {
            $message = Get-PropertyValue $response "Message" "unknown error"
            $errorCode = Get-PropertyValue $response "ErrorCode" ""
            throw "Automation command '$Command' failed: $message [ErrorCode=$errorCode ExitCode=$LASTEXITCODE]"
        }

        throw "Automation command '$Command' transport failed with exit code $LASTEXITCODE."
    }

    if ($null -eq $response) {
        throw "Automation command '$Command' returned no JSON response."
    }

    if (-not $AllowFailure -and -not (Convert-ToBool (Get-PropertyValue $response "Success" $false))) {
        $message = Get-PropertyValue $response "Message" "unknown error"
        $errorCode = Get-PropertyValue $response "ErrorCode" ""
        throw "Automation command '$Command' failed: $message [ErrorCode=$errorCode]"
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
        throw "Dedicated LibAv verification failed: $Message"
    }
}

function Assert-AutomationResponseSucceeded {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Response,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    if (-not (Convert-ToBool (Get-PropertyValue $Response "Success" $false))) {
        $message = Get-PropertyValue $Response "Message" "unknown error"
        $errorCode = Get-PropertyValue $Response "ErrorCode" ""
        throw "$Context failed: $message [ErrorCode=$errorCode]"
    }
}

function Resolve-FfprobePath {
    $ffprobeCommand = Get-Command ffprobe.exe -ErrorAction SilentlyContinue
    if ($ffprobeCommand -and -not [string]::IsNullOrWhiteSpace($ffprobeCommand.Source)) {
        return $ffprobeCommand.Source
    }

    $repoRoot = Split-Path -Parent $PSScriptRoot
    foreach ($candidate in @(
            (Join-Path $repoRoot "latest-build\ffmpeg\ffprobe.exe"),
            (Join-Path $repoRoot "Sussudio\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\ffmpeg\ffprobe.exe"))) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "ffprobe.exe not found on PATH or known repo locations."
}

function Invoke-FfprobeJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FfprobePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $allOutput = & $FfprobePath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $joined = [string]::Join([Environment]::NewLine, @($allOutput))
        throw "ffprobe failed ($LASTEXITCODE): $joined"
    }

    $jsonText = [string]::Join([Environment]::NewLine, @($allOutput))
    if ([string]::IsNullOrWhiteSpace($jsonText)) {
        throw "ffprobe returned empty output."
    }

    return $jsonText | ConvertFrom-Json
}

function Assert-FileLevelIntegrityFallback {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Verification
    )

    $outputPath = [string](Get-PropertyValue $Verification "OutputPath" "")
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($outputPath)) "Verification output path is empty."

    $ffprobePath = Resolve-FfprobePath
    $probe = Invoke-FfprobeJson -FfprobePath $ffprobePath -Arguments @(
        "-v", "error",
        "-select_streams", "v:0",
        "-show_entries", "stream=nb_frames,duration",
        "-of", "json",
        $outputPath)

    Assert-Condition ($null -ne $probe.streams -and @($probe.streams).Count -gt 0) "Fallback ffprobe returned no video stream."
    $stream = @($probe.streams)[0]
    $durationSeconds = Convert-ToNullableDouble (Get-PropertyValue $stream "duration" $null)
    $frameCount = Convert-ToNullableInt64 (Get-PropertyValue $stream "nb_frames" $null)
    $minimumDurationSeconds = $DurationSeconds * $MinExpectedFrameFraction
    $minimumExpectedFrames = [long][Math]::Floor($ExpectedFrameRate * $DurationSeconds * $MinExpectedFrameFraction)

    Assert-Condition ($null -ne $durationSeconds) "Fallback ffprobe missing duration."
    Assert-Condition ($durationSeconds -ge $minimumDurationSeconds) "Fallback ffprobe duration $durationSeconds below minimum expected $minimumDurationSeconds."

    if ($null -ne $frameCount) {
        Assert-Condition ($frameCount -ge $minimumExpectedFrames) "Fallback ffprobe frame count $frameCount below minimum expected $minimumExpectedFrames."
        return
    }

    $detectedFrameRate = Convert-ToNullableDouble (Get-PropertyValue $Verification "DetectedFrameRate" $null)
    Assert-Condition ($null -ne $detectedFrameRate) "Fallback ffprobe missing nb_frames and verification frame rate."
    $estimatedFrames = [long][Math]::Floor($durationSeconds * $detectedFrameRate)
    Assert-Condition ($estimatedFrames -ge $minimumExpectedFrames) "Fallback ffprobe estimated frame count $estimatedFrames below minimum expected $minimumExpectedFrames."
}

function Get-Snapshot {
    $response = Invoke-Automation -Command "GetSnapshot"
    $snapshot = Get-PropertyValue $response "Snapshot" $null
    if ($null -eq $snapshot) {
        throw "GetSnapshot returned no snapshot payload."
    }

    return $snapshot
}

function Wait-Condition {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConditionName,
        [int]$TimeoutMs = $WaitTimeoutMs
    )

    $responseTimeoutMs = [Math]::Max($TimeoutMs + $ResponseTimeoutBufferMs, 60000)
    $response = Invoke-Automation -Command "WaitForCondition" -Payload @{
        condition = $ConditionName
        timeoutMs = $TimeoutMs
        pollMs = $WaitPollMs
    } -ResponseTimeoutMs $responseTimeoutMs -AllowFailure

    if (-not (Convert-ToBool (Get-PropertyValue $response "Success" $false))) {
        $message = Get-PropertyValue $response "Message" "timeout"
        throw "Timed out waiting for condition '$ConditionName': $message"
    }
}

function Wait-FlashbackInactive {
    $started = Get-Date
    while (((Get-Date) - $started).TotalMilliseconds -lt $WaitTimeoutMs) {
        $snapshot = Get-Snapshot
        $flashbackActive = Convert-ToBool (Get-PropertyValue $snapshot "FlashbackActive" $false)
        $backend = [string](Get-PropertyValue $snapshot "RecordingBackend" "None")
        if (-not $flashbackActive -and $backend -ne "Flashback") {
            return $snapshot
        }

        Start-Sleep -Milliseconds $WaitPollMs
    }

    throw "Timed out waiting for Flashback to become inactive."
}

function Assert-ExpectedRuntimeMode {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Snapshot,
        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    $width = Convert-ToInt64 (Get-FirstPropertyValue $Snapshot @("NegotiatedWidth", "ActualWidth") 0)
    $height = Convert-ToInt64 (Get-FirstPropertyValue $Snapshot @("NegotiatedHeight", "ActualHeight") 0)
    $frameRate = Convert-ToDouble (Get-FirstPropertyValue $Snapshot @("NegotiatedFrameRate", "ActualFrameRate") 0)
    $frameRateDelta = [Math]::Abs($frameRate - $ExpectedFrameRate)

    Assert-Condition ($width -eq $ExpectedWidth) "$Context expected negotiated width $ExpectedWidth but saw $width."
    Assert-Condition ($height -eq $ExpectedHeight) "$Context expected negotiated height $ExpectedHeight but saw $height."
    Assert-Condition ($frameRateDelta -le $FrameRateTolerance) "$Context expected negotiated frame rate $ExpectedFrameRate +/- $FrameRateTolerance but saw $frameRate."
}

function Assert-LibAvRuntimeSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Snapshot
    )

    Assert-Condition (Convert-ToBool (Get-PropertyValue $Snapshot "IsRecording" $false)) "Expected IsRecording=true during runtime check."
    Assert-Condition ([string](Get-PropertyValue $Snapshot "RecordingBackend" "") -eq "LibAv") "Expected dedicated RecordingBackend=LibAv."
    Assert-Condition (-not (Convert-ToBool (Get-PropertyValue $Snapshot "FlashbackActive" $false))) "Expected FlashbackActive=false during dedicated LibAv recording."
    Assert-ExpectedRuntimeMode -Snapshot $Snapshot -Context "Runtime"
    $recordingFailureMessage = Get-PropertyValue $Snapshot "RecordingEncodingFailureMessage" ""
    Assert-Condition (-not (Convert-ToBool (Get-PropertyValue $Snapshot "RecordingEncodingFailed" $false))) "RecordingEncodingFailed became true: $recordingFailureMessage"
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "EncoderLastWriteAgeMs" 0)) -le $MaxEncoderLastWriteAgeMs) "EncoderLastWriteAgeMs exceeded $MaxEncoderLastWriteAgeMs."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "VideoDropsQueueSaturated" 0)) -le $MaxRuntimeQueueDrops) "VideoDropsQueueSaturated exceeded $MaxRuntimeQueueDrops."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "VideoDropsBacklogEviction" 0)) -le $MaxRuntimeQueueDrops) "VideoDropsBacklogEviction exceeded $MaxRuntimeQueueDrops."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "RecordingGpuFramesDropped" 0)) -le $MaxRuntimeQueueDrops) "RecordingGpuFramesDropped exceeded $MaxRuntimeQueueDrops."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "RecordingCudaFramesDropped" 0)) -le $MaxRuntimeQueueDrops) "RecordingCudaFramesDropped exceeded $MaxRuntimeQueueDrops."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Snapshot "CaptureCadenceEstimatedDroppedFrames" 0)) -le $MaxRuntimeCaptureDrops) "CaptureCadenceEstimatedDroppedFrames exceeded $MaxRuntimeCaptureDrops."
}

function Assert-Verification {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Verification
    )

    $verificationMessage = Get-PropertyValue $Verification "Message" ""
    Assert-Condition (Convert-ToBool (Get-PropertyValue $Verification "Succeeded" $false)) "VerifyLastRecording did not succeed: $verificationMessage"
    Assert-Condition (Convert-ToBool (Get-PropertyValue $Verification "FileExists" $false)) "Verification output file does not exist."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Verification "FileSizeBytes" 0)) -gt 0) "Verification output file is empty."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Verification "DetectedWidth" 0)) -eq $ExpectedWidth) "Verification detected width did not match $ExpectedWidth."
    Assert-Condition ((Convert-ToInt64 (Get-PropertyValue $Verification "DetectedHeight" 0)) -eq $ExpectedHeight) "Verification detected height did not match $ExpectedHeight."

    $detectedFrameRate = Convert-ToDouble (Get-PropertyValue $Verification "DetectedFrameRate" 0)
    $detectedFrameRateDelta = [Math]::Abs($detectedFrameRate - $ExpectedFrameRate)
    Assert-Condition ($detectedFrameRateDelta -le $FrameRateTolerance) "Verification detected frame rate $detectedFrameRate outside expected $ExpectedFrameRate +/- $FrameRateTolerance."

    # RecordingVerifier treats ffprobe cadence metrics as optional because some
    # codec/container combinations do not expose usable per-frame timestamps.
    $cadenceSampleCount = Get-PropertyValue $Verification "CadenceSampleCount" $null
    if ($null -ne $cadenceSampleCount) {
        Assert-Condition ((Convert-ToInt64 $cadenceSampleCount) -gt 0) "Verification CadenceSampleCount must be greater than zero."
    }

    $estimatedDrops = Get-PropertyValue $Verification "CadenceEstimatedDroppedFrames" $null
    if ($null -ne $estimatedDrops) {
        Assert-Condition ((Convert-ToInt64 $estimatedDrops) -le $MaxVerificationEstimatedDrops) "CadenceEstimatedDroppedFrames exceeded $MaxVerificationEstimatedDrops."
    }

    $severeGaps = Get-PropertyValue $Verification "CadenceSevereGapCount" $null
    if ($null -ne $severeGaps) {
        Assert-Condition ((Convert-ToInt64 $severeGaps) -le $MaxVerificationSevereGaps) "CadenceSevereGapCount exceeded $MaxVerificationSevereGaps."
    }

    if ($null -eq $cadenceSampleCount) {
        Assert-FileLevelIntegrityFallback -Verification $Verification
    }
}

Write-Host "Dedicated LibAv recording verification: starting"

$startedPreview = $false
$initialSnapshot = Get-Snapshot
$initialFlashbackActive = Convert-ToBool (Get-PropertyValue $initialSnapshot "FlashbackActive" $false)
$initialOutputPath = [string](Get-PropertyValue $initialSnapshot "OutputPath" "")
$restoreFlashbackAfterDisable = $initialFlashbackActive
$outputPathChanged = -not [string]::IsNullOrWhiteSpace($OutputDirectory)
$verificationCompleted = $false
$script:CleanupFailures = [System.Collections.Generic.List[string]]::new()
$summary = $null

Assert-Condition (-not (Convert-ToBool (Get-PropertyValue $initialSnapshot "IsRecording" $false))) "App is already recording."

try {
    if ($outputPathChanged) {
        Invoke-Automation -Command "SetOutputPath" -Payload @{ outputPath = $OutputDirectory } | Out-Null
    }

    if (-not (Convert-ToBool (Get-PropertyValue $initialSnapshot "IsPreviewing" $false))) {
        Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $true } | Out-Null
        $startedPreview = $true
    }

    Wait-Condition -ConditionName "VideoFramesFlowing"
    $preDisable = Get-Snapshot
    Assert-ExpectedRuntimeMode -Snapshot $preDisable -Context "Pre-record"
    $restoreFlashbackAfterDisable = $restoreFlashbackAfterDisable -or (Convert-ToBool (Get-PropertyValue $preDisable "FlashbackActive" $false))

    Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $false } | Out-Null
    $preRecord = Wait-FlashbackInactive
    Assert-Condition (-not (Convert-ToBool (Get-PropertyValue $preRecord "FlashbackActive" $false))) "Flashback did not become inactive before recording."

    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "RecordingFileGrowing"

    $recordingStart = Get-Snapshot
    Assert-LibAvRuntimeSnapshot -Snapshot $recordingStart

    $startedAt = Get-Date
    $lastRuntimeSnapshot = $recordingStart
    $previousFramesEnqueued = Convert-ToInt64 (Get-PropertyValue $recordingStart "VideoFramesEnqueued" 0)
    $previousEncodedFrames = Convert-ToInt64 (Get-PropertyValue $recordingStart "EncoderVideoFramesEncoded" 0)
    $previousRecordingBytes = Convert-ToInt64 (Get-PropertyValue $recordingStart "RecordingTotalBytes" 0)
    $progressObservedCount = 0
    while (((Get-Date) - $startedAt).TotalSeconds -lt $DurationSeconds) {
        Start-Sleep -Milliseconds $WaitPollMs
        $lastRuntimeSnapshot = Get-Snapshot
        Assert-LibAvRuntimeSnapshot -Snapshot $lastRuntimeSnapshot

        $framesEnqueued = Convert-ToInt64 (Get-PropertyValue $lastRuntimeSnapshot "VideoFramesEnqueued" 0)
        $encodedFrames = Convert-ToInt64 (Get-PropertyValue $lastRuntimeSnapshot "EncoderVideoFramesEncoded" 0)
        $recordingBytes = Convert-ToInt64 (Get-PropertyValue $lastRuntimeSnapshot "RecordingTotalBytes" 0)

        Assert-Condition ($framesEnqueued -ge $previousFramesEnqueued) "VideoFramesEnqueued regressed from $previousFramesEnqueued to $framesEnqueued."
        Assert-Condition ($encodedFrames -ge $previousEncodedFrames) "EncoderVideoFramesEncoded regressed from $previousEncodedFrames to $encodedFrames."
        Assert-Condition ($recordingBytes -ge $previousRecordingBytes) "RecordingTotalBytes regressed from $previousRecordingBytes to $recordingBytes."

        if ($framesEnqueued -gt $previousFramesEnqueued -or $encodedFrames -gt $previousEncodedFrames -or $recordingBytes -gt $previousRecordingBytes) {
            $progressObservedCount++
        }

        $previousFramesEnqueued = $framesEnqueued
        $previousEncodedFrames = $encodedFrames
        $previousRecordingBytes = $recordingBytes
    }

    Assert-Condition ($progressObservedCount -gt 0) "Recording counters did not make progress during the duration loop."

    $minimumExpectedFrames = [long][Math]::Floor($ExpectedFrameRate * $DurationSeconds * $MinExpectedFrameFraction)
    $finalFramesEnqueued = Convert-ToInt64 (Get-PropertyValue $lastRuntimeSnapshot "VideoFramesEnqueued" 0)
    $finalEncodedFrames = Convert-ToInt64 (Get-PropertyValue $lastRuntimeSnapshot "EncoderVideoFramesEncoded" 0)
    Assert-Condition ($finalFramesEnqueued -ge $minimumExpectedFrames) "VideoFramesEnqueued $finalFramesEnqueued below minimum expected $minimumExpectedFrames."
    Assert-Condition ($finalEncodedFrames -ge $minimumExpectedFrames) "EncoderVideoFramesEncoded $finalEncodedFrames below minimum expected $minimumExpectedFrames."

    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } | Out-Null
    Wait-Condition -ConditionName "RecordingStopped" -TimeoutMs ([Math]::Max($WaitTimeoutMs, 150000))

    $verifyResponse = Invoke-Automation -Command "VerifyLastRecording"
    $verification = Get-PropertyValue (Get-PropertyValue $verifyResponse "Data" $null) "Verification" $null
    if ($null -eq $verification) {
        $verification = Get-PropertyValue (Get-PropertyValue $verifyResponse "Snapshot" $null) "LastVerification" $null
    }

    if ($null -eq $verification) {
        throw "VerifyLastRecording returned no verification payload."
    }

    Assert-Verification -Verification $verification

    $finalSnapshot = Get-Snapshot
    $summary = [ordered]@{
        Result = "PASS"
        DurationSeconds = $DurationSeconds
        RecordingBackendDuringRun = Get-PropertyValue $lastRuntimeSnapshot "RecordingBackend" ""
        FlashbackActiveDuringRun = Get-PropertyValue $lastRuntimeSnapshot "FlashbackActive" $null
        RuntimeFramesArrived = Get-PropertyValue $lastRuntimeSnapshot "IngestVideoFramesArrived" 0
        RuntimeFramesEnqueued = Get-PropertyValue $lastRuntimeSnapshot "VideoFramesEnqueued" 0
        RuntimeEncodedFrames = Get-PropertyValue $lastRuntimeSnapshot "EncoderVideoFramesEncoded" 0
        RuntimeQueueDrops = Get-PropertyValue $lastRuntimeSnapshot "VideoDropsQueueSaturated" 0
        RuntimeGpuDrops = Get-PropertyValue $lastRuntimeSnapshot "RecordingGpuFramesDropped" 0
        RuntimeCudaDrops = Get-PropertyValue $lastRuntimeSnapshot "RecordingCudaFramesDropped" 0
        RuntimeCaptureEstimatedDrops = Get-PropertyValue $lastRuntimeSnapshot "CaptureCadenceEstimatedDroppedFrames" 0
        ExpectedWidth = $ExpectedWidth
        ExpectedHeight = $ExpectedHeight
        ExpectedFrameRate = $ExpectedFrameRate
        OutputPath = Get-PropertyValue $verification "OutputPath" ""
        FileSizeBytes = Get-PropertyValue $verification "FileSizeBytes" 0
        DetectedCodec = Get-PropertyValue $verification "DetectedVideoCodec" ""
        DetectedWidth = Get-PropertyValue $verification "DetectedWidth" $null
        DetectedHeight = Get-PropertyValue $verification "DetectedHeight" $null
        DetectedFrameRate = Get-PropertyValue $verification "DetectedFrameRate" $null
        CadenceEstimatedDroppedFrames = Get-PropertyValue $verification "CadenceEstimatedDroppedFrames" $null
        CadenceSevereGapCount = Get-PropertyValue $verification "CadenceSevereGapCount" $null
        FinalizeStatus = Get-PropertyValue $finalSnapshot "LastFinalizeStatus" ""
    }

    $verificationCompleted = $true
}
finally {
    try {
        $cleanupSnapshot = Get-Snapshot
        if (Convert-ToBool (Get-PropertyValue $cleanupSnapshot "IsRecording" $false)) {
            $stopResponse = Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } -AllowFailure
            Assert-AutomationResponseSucceeded -Response $stopResponse -Context "Cleanup recording stop"
            Wait-Condition -ConditionName "RecordingStopped" -TimeoutMs ([Math]::Max($WaitTimeoutMs, 150000))
        }
    }
    catch {
        $message = "Cleanup recording stop failed: $($_.Exception.Message)"
        $script:CleanupFailures.Add($message)
        Write-Warning $message
    }

    if ($restoreFlashbackAfterDisable -and -not $NoRestoreFlashback) {
        try {
            $flashbackResponse = Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $true } -AllowFailure
            Assert-AutomationResponseSucceeded -Response $flashbackResponse -Context "Flashback restore"
        }
        catch {
            $message = "Flashback restore failed: $($_.Exception.Message)"
            $script:CleanupFailures.Add($message)
            Write-Warning $message
        }
    }

    if ($outputPathChanged) {
        try {
            if ([string]::IsNullOrWhiteSpace($initialOutputPath)) {
                throw "Initial OutputPath was empty, cannot restore after temporary OutputDirectory '$OutputDirectory'."
            }

            $outputPathResponse = Invoke-Automation -Command "SetOutputPath" -Payload @{ outputPath = $initialOutputPath } -AllowFailure
            Assert-AutomationResponseSucceeded -Response $outputPathResponse -Context "Output path restore"
        }
        catch {
            $message = "Output path restore failed: $($_.Exception.Message)"
            $script:CleanupFailures.Add($message)
            Write-Warning $message
        }
    }

    if ($startedPreview -and -not $AllowPreviewRunningAtEnd) {
        try {
            $previewResponse = Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $false } -AllowFailure
            Assert-AutomationResponseSucceeded -Response $previewResponse -Context "Preview stop cleanup"
        }
        catch {
            $message = "Preview stop cleanup failed: $($_.Exception.Message)"
            $script:CleanupFailures.Add($message)
            Write-Warning $message
        }
    }

    if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0) {
        throw "Dedicated LibAv verification cleanup failed: $($script:CleanupFailures -join '; ')"
    }
}

Write-Host "Dedicated LibAv recording verification: PASS"
$summary | ConvertTo-Json -Depth 20
