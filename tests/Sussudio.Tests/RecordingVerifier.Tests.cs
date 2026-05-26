using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static object BuildRuntimeSnapshotForVerification(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        return snapshot;
    }

    // ── RecordingVerifier: VerifyAsync early-exit paths ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { "/nonexistent/file.mp4", snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result")!;
        var result = resultProp.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual(false, GetBoolProperty(result, "FileExists"), "FileExists");
        AssertContains(GetStringProperty(result, "Message"), "does not exist");
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileIsEmpty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_test_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, Array.Empty<byte>());
        try
        {
            var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
            var verifier = Activator.CreateInstance(verifierType)!;
            var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

            var snapshot = BuildRuntimeSnapshotForVerification();
            var task = verifyAsync.Invoke(verifier, new object?[] { tempFile, snapshot, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "output-empty");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { null, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
    }

    // ── RecordingVerifier: contract surface ──

    internal static Task RecordingVerifier_ImplementsIRecordingVerifier()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var interfaceType = RequireType("Sussudio.Services.Contracts.IRecordingVerifier");

        AssertEqual(true, interfaceType.IsAssignableFrom(verifierType), "RecordingVerifier implements IRecordingVerifier");

        // VerifyAsync takes (string?, CaptureRuntimeSnapshot, CancellationToken)
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(verifyAsync, "RecordingVerifier.VerifyAsync");

        var parameters = verifyAsync!.GetParameters();
        AssertEqual(3, parameters.Length, "VerifyAsync parameter count");

        // Return type is Task<RecordingVerificationResult>
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        AssertEqual(true, verifyAsync.ReturnType.IsGenericType, "VerifyAsync returns generic Task");
        AssertEqual(resultType, verifyAsync.ReturnType.GetGenericArguments()[0], "VerifyAsync returns Task<RecordingVerificationResult>");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_CadenceAnalysisLivesWithVerifier()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public sealed class RecordingVerifier : IRecordingVerifier");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static CadenceMetrics ComputeCadenceMetrics(");
        AssertContains(rootText, "private static double? TryGetFrameTimestampSeconds(JsonElement frame)");
        AssertContains(rootText, "private static double? TryGetJsonDouble(JsonElement element, string propertyName)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Cadence.cs")),
            "RecordingVerifier cadence ffprobe pass folded into verifier owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe helper partial folded into verifier owner");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_ProbeValidationAndResultShapingOwnership()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public async Task<RecordingVerificationResult> VerifyAsync(");
        AssertContains(rootText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static Dictionary<string, string> ParseKeyValueOutput(string output)");
        AssertContains(rootText, "private static double? TryParseRational(string? value)");
        AssertContains(rootText, "private ProcessSpec CreateFfprobeProcessSpec(");
        AssertContains(rootText, "private static void ValidateContainer(");
        AssertContains(rootText, "private static void ValidateCodec(");
        AssertContains(rootText, "private static void ValidateDimensions(");
        AssertContains(rootText, "private static double? ResolveExpectedFrameRate(");
        AssertContains(rootText, "private static void ValidateCadence(");
        AssertContains(rootText, "private readonly record struct HdrValidationResult(");
        AssertContains(rootText, "private static HdrValidationResult ValidateHdrMetadata(");
        AssertContains(rootText, "private static string ResolveExpectedFormat(");
        AssertContains(rootText, "private static bool IsFlashbackRecording(");
        AssertContains(rootText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertContains(rootText, "private static HdrParityResult BuildHdrParityResult(");
        AssertContains(rootText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");
        AssertContains(rootText, "private static string? TryGetMismatchPart(");
        AssertContains(rootText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Results.cs")),
            "RecordingVerifier.Results.cs folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Validation.cs")),
            "RecordingVerifier validation policy folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe probe/process helpers folded into RecordingVerifier.cs");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerificationResult_HasExpectedProperties()
    {
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");

        var expectedProps = new[]
        {
            "TimestampUtc", "Succeeded", "Message", "OutputPath", "FileExists", "FileSizeBytes",
            "VerificationMode", "DetectedContainer", "DetectedVideoCodec", "DetectedPixelFormat",
            "DetectedColorPrimaries", "DetectedColorTransfer", "DetectedColorSpace",
            "DetectedHdrSideDataTypes", "HdrMetadataPresent", "HdrColorimetryValid",
            "HdrMasteringMetadataPresent", "HdrVerificationLevel",
            "DetectedWidth", "DetectedHeight", "DetectedFrameRate",
            "CadenceSampleCount", "CadenceObservedFps", "CadenceExpectedIntervalMs",
            "CadenceAverageIntervalMs", "CadenceP95IntervalMs", "CadenceMaxIntervalMs",
            "CadenceJitterStdDevMs", "CadenceSevereGapCount", "CadenceSevereGapPercent",
            "CadenceEstimatedDroppedFrames", "CadenceEstimatedDropPercent",
            "PrimaryMismatchCode", "PrimaryMismatchExpected", "PrimaryMismatchActual",
            "Mismatches", "HdrParity"
        };

        foreach (var prop in expectedProps)
        {
            AssertNotNull(
                resultType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance),
                $"RecordingVerificationResult.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification()
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

