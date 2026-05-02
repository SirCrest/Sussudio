using System.Threading.Tasks;

static partial class Program
{
    private static Task DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification()
    {
        var scriptText = ReadRepoFile("tools/verify-dedicated-libav-recording.ps1")
            .Replace("\r\n", "\n");

        AssertContains(scriptText, "SetFlashbackEnabled");
        AssertContains(scriptText, "SetRecordingEnabled");
        AssertContains(scriptText, "VerifyLastRecording");
        AssertContains(scriptText, "[int]$ResponseTimeoutBufferMs = 5000");
        AssertContains(scriptText, "-ResponseTimeoutMs $ResponseTimeoutMs");
        AssertContains(scriptText, "$responseTimeoutMs = [Math]::Max($TimeoutMs + $ResponseTimeoutBufferMs, 60000)");
        AssertContains(scriptText, "-ResponseTimeoutMs $responseTimeoutMs -AllowFailure");
        AssertContains(scriptText, "$rawText = ($raw | Out-String).Trim()");
        AssertContains(scriptText, "if ($AllowFailure -and $null -ne $response)");
        AssertContains(scriptText, "$initialOutputPath = [string](Get-PropertyValue $initialSnapshot \"OutputPath\" \"\")");
        AssertContains(scriptText, "$outputPathChanged = -not [string]::IsNullOrWhiteSpace($OutputDirectory)");
        AssertContains(scriptText, "function Assert-AutomationResponseSucceeded");
        AssertContains(scriptText, "Invoke-Automation -Command \"SetOutputPath\" -Payload @{ outputPath = $initialOutputPath } -AllowFailure");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $stopResponse -Context \"Cleanup recording stop\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $outputPathResponse -Context \"Output path restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $flashbackResponse -Context \"Flashback restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $previewResponse -Context \"Preview stop cleanup\"");
        AssertContains(scriptText, "$verificationCompleted = $true");
        AssertContains(scriptText, "Dedicated LibAv verification cleanup failed");
        AssertDoesNotContain(scriptText, "ConvertFrom-Json -Depth");
        AssertContains(scriptText, "[int]$ExpectedWidth = 3840");
        AssertContains(scriptText, "[int]$ExpectedHeight = 2160");
        AssertContains(scriptText, "[double]$ExpectedFrameRate = 120.0");
        AssertContains(scriptText, "[double]$FrameRateTolerance = 1.0");
        AssertContains(scriptText, "[double]$MinExpectedFrameFraction = 0.80");
        AssertContains(scriptText, "[int]$MaxEncoderLastWriteAgeMs = 2000");
        AssertContains(scriptText, "RecordingFileGrowing");
        AssertContains(scriptText, "RecordingStopped");
        AssertContains(scriptText, "VideoFramesFlowing");
        AssertContains(scriptText, "function Assert-ExpectedRuntimeMode");
        AssertContains(scriptText, "NegotiatedWidth");
        AssertContains(scriptText, "ActualWidth");
        AssertContains(scriptText, "NegotiatedFrameRate");
        AssertContains(scriptText, "ActualFrameRate");
        AssertContains(scriptText, "Get-PropertyValue $Snapshot \"RecordingBackend\" \"\") -eq \"LibAv\"");
        AssertContains(scriptText, "FlashbackActive=false");
        AssertContains(scriptText, "RecordingEncodingFailed");
        AssertContains(scriptText, "EncoderLastWriteAgeMs");
        AssertContains(scriptText, "VideoDropsQueueSaturated");
        AssertContains(scriptText, "VideoDropsBacklogEviction");
        AssertContains(scriptText, "RecordingGpuFramesDropped");
        AssertContains(scriptText, "RecordingCudaFramesDropped");
        AssertContains(scriptText, "CaptureCadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "DetectedWidth");
        AssertContains(scriptText, "DetectedHeight");
        AssertContains(scriptText, "DetectedFrameRate");
        AssertContains(scriptText, "RecordingVerifier treats ffprobe cadence metrics as optional");
        AssertContains(scriptText, "Verification CadenceSampleCount must be greater than zero.");
        AssertContains(scriptText, "if ($null -ne $estimatedDrops)");
        AssertContains(scriptText, "if ($null -ne $severeGaps)");
        AssertContains(scriptText, "function Assert-FileLevelIntegrityFallback");
        AssertContains(scriptText, "-show_entries\", \"stream=nb_frames,duration");
        AssertContains(scriptText, "Fallback ffprobe frame count");
        AssertContains(scriptText, "if ($null -eq $cadenceSampleCount)");
        AssertContains(scriptText, "CadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "CadenceSevereGapCount");
        AssertContains(scriptText, "VideoFramesEnqueued regressed");
        AssertContains(scriptText, "EncoderVideoFramesEncoded regressed");
        AssertContains(scriptText, "RecordingTotalBytes regressed");
        AssertContains(scriptText, "$minimumExpectedFrames = [long][Math]::Floor($ExpectedFrameRate * $DurationSeconds * $MinExpectedFrameFraction)");
        AssertContains(scriptText, "NoRestoreFlashback");
        AssertContains(scriptText, "initialFlashbackActive");
        AssertContains(scriptText, "restoreFlashbackAfterDisable");

        AssertOccursBefore(
            scriptText,
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $true }");
        AssertOccursBefore(
            scriptText,
            "Assert-ExpectedRuntimeMode -Snapshot $preDisable -Context \"Pre-record\"",
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-LibAvRuntimeSnapshot -Snapshot $recordingStart",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-Condition ($progressObservedCount -gt 0)",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Wait-Condition -ConditionName \"RecordingStopped\"",
            "$verifyResponse = Invoke-Automation -Command \"VerifyLastRecording\"");
        AssertOccursBefore(
            scriptText,
            "$verification = Get-PropertyValue",
            "Assert-Verification -Verification $verification");
        AssertOccursBefore(
            scriptText,
            "$verificationCompleted = $true",
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)");
        AssertOccursBefore(
            scriptText,
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)",
            "Write-Host \"Dedicated LibAv recording verification: PASS\"");

        return Task.CompletedTask;
    }
}
