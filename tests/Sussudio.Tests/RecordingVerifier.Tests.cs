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

    internal static Task RecordingVerifier_CadenceAnalysisLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");
        var cadenceText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public sealed partial class RecordingVerifier : IRecordingVerifier");
        AssertContains(cadenceText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(cadenceText, "private static CadenceMetrics ComputeCadenceMetrics(");
        AssertContains(cadenceText, "private static double? TryGetFrameTimestampSeconds(JsonElement frame)");
        AssertContains(cadenceText, "private static double? TryGetJsonDouble(JsonElement element, string propertyName)");
        AssertDoesNotContain(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertDoesNotContain(rootText, "private static CadenceMetrics ComputeCadenceMetrics(");
        AssertDoesNotContain(rootText, "private static double? TryGetFrameTimestampSeconds(JsonElement frame)");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_ProbeValidationAndResultsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");
        var ffprobeText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs")
            .Replace("\r\n", "\n");
        var resultsText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public async Task<RecordingVerificationResult> VerifyAsync(");
        AssertContains(ffprobeText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertContains(ffprobeText, "private static Dictionary<string, string> ParseKeyValueOutput(string output)");
        AssertContains(ffprobeText, "private static double? TryParseRational(string? value)");
        AssertContains(ffprobeText, "private ProcessSpec CreateFfprobeProcessSpec(");
        AssertContains(validationText, "private static void ValidateContainer(");
        AssertContains(validationText, "private static void ValidateCodec(");
        AssertContains(validationText, "private static void ValidateDimensions(");
        AssertContains(validationText, "private static double? ResolveExpectedFrameRate(");
        AssertContains(validationText, "private static void ValidateCadence(");
        AssertContains(validationText, "private readonly record struct HdrValidationResult(");
        AssertContains(validationText, "private static HdrValidationResult ValidateHdrMetadata(");
        AssertContains(validationText, "private static string ResolveExpectedFormat(");
        AssertContains(validationText, "private static bool IsFlashbackRecording(");
        AssertContains(resultsText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertContains(resultsText, "private static HdrParityResult BuildHdrParityResult(");
        AssertContains(resultsText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");
        AssertContains(resultsText, "private static string? TryGetMismatchPart(");
        AssertContains(resultsText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertDoesNotContain(rootText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertDoesNotContain(rootText, "private static void ValidateContainer(");
        AssertDoesNotContain(rootText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertDoesNotContain(rootText, "private static HdrParityResult BuildHdrParityResult(");
        AssertDoesNotContain(rootText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");
        AssertDoesNotContain(rootText, "private static string? TryGetMismatchPart(");
        AssertDoesNotContain(rootText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertDoesNotContain(validationText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertDoesNotContain(validationText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");

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
}

