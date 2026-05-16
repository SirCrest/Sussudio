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

    private static async Task RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist()
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

    private static async Task RecordingVerifier_ReturnsFailure_WhenFileIsEmpty()
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

    private static async Task RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull()
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

    private static Task RecordingVerifier_ImplementsIRecordingVerifier()
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

    private static Task RecordingVerifier_CadenceAnalysisLivesInFocusedPartial()
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

    private static Task RecordingVerifier_ProbeValidationAndResultsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");
        var ffprobeText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs")
            .Replace("\r\n", "\n");
        var parsingText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs")
            .Replace("\r\n", "\n");
        var resultsText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public async Task<RecordingVerificationResult> VerifyAsync(");
        AssertContains(ffprobeText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertContains(ffprobeText, "private ProcessSpec CreateFfprobeProcessSpec(");
        AssertContains(parsingText, "private static Dictionary<string, string> ParseKeyValueOutput(string output)");
        AssertContains(parsingText, "private static double? TryParseRational(string? value)");
        AssertContains(validationText, "private static void ValidateContainer(");
        AssertContains(validationText, "private static HdrValidationResult ValidateHdrMetadata(");
        AssertContains(resultsText, "private static HdrParityResult BuildHdrParityResult(");
        AssertContains(resultsText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertDoesNotContain(rootText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertDoesNotContain(rootText, "private static void ValidateContainer(");
        AssertDoesNotContain(rootText, "private static HdrParityResult BuildHdrParityResult(");
        AssertDoesNotContain(rootText, "private static RecordingVerificationResult CreateEarlyFailure(");

        return Task.CompletedTask;
    }

    private static Task RecordingVerificationResult_HasExpectedProperties()
    {
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");

        var expectedProps = new[]
        {
            "Succeeded", "Message", "OutputPath", "FileExists", "FileSizeBytes",
            "VerificationMode", "DetectedContainer", "DetectedVideoCodec",
            "DetectedPixelFormat", "DetectedWidth", "DetectedHeight", "DetectedFrameRate",
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

