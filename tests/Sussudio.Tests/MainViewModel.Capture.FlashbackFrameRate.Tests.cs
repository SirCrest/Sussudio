using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task CaptureService_FlashbackFrameRateParts_PreserveOnlyDeliveredCadenceRational()
    {
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = captureServiceType.GetMethod(
            "ResolveFlashbackSessionFrameRateParts",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackSessionFrameRateParts not found.");

        var integerResult = method.Invoke(null, new[] { BuildFrameRateSettings(120u, 1u), 120.0 })!;
        AssertFlashbackFrameRateParts(integerResult, 120, 1, 120.0, "integer 120 delivered cadence");

        var ntscDelivery = 120000d / 1001d;
        var ntscResult = method.Invoke(null, new[] { BuildFrameRateSettings(120000u, 1001u), ntscDelivery })!;
        AssertFlashbackFrameRateParts(ntscResult, 120000, 1001, ntscDelivery, "matching NTSC delivered cadence");

        var mismatchedResult = method.Invoke(null, new[] { BuildFrameRateSettings(120000u, 1001u), 120.0 })!;
        AssertFlashbackFrameRateParts(mismatchedResult, 120, 1, 120.0, "source NTSC rejected then inferred from integer USB cadence");

        var missingResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120.0 })!;
        AssertFlashbackFrameRateParts(missingResult, 120, 1, 120.0, "missing rational infers integer delivered cadence");

        var measuredIntegerResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120.00048 })!;
        AssertFlashbackFrameRateParts(measuredIntegerResult, 120, 1, 120.0, "measured integer delivered cadence infers exact rational");

        var measuredNtscResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120000d / 1001d })!;
        AssertFlashbackFrameRateParts(measuredNtscResult, 120000, 1001, 120000d / 1001d, "missing rational infers NTSC delivered cadence");

        return Task.CompletedTask;
    }

    private static object BuildFrameRateSettings(uint? numerator, uint? denominator)
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", numerator);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", denominator);
        return settings;
    }

    private static void AssertFlashbackFrameRateParts(
        object result,
        int? expectedNumerator,
        int? expectedDenominator,
        double expectedFrameRate,
        string fieldName)
    {
        var resultType = result.GetType();
        var numerator = resultType.GetField("Item1")?.GetValue(result);
        var denominator = resultType.GetField("Item2")?.GetValue(result);
        var effectiveFrameRate = resultType.GetField("Item3")?.GetValue(result);

        AssertEqual(expectedNumerator, numerator == null ? null : Convert.ToInt32(numerator), $"{fieldName} numerator");
        AssertEqual(expectedDenominator, denominator == null ? null : Convert.ToInt32(denominator), $"{fieldName} denominator");
        AssertNearlyEqual(expectedFrameRate, Convert.ToDouble(effectiveFrameRate), 0.000001, $"{fieldName} effective frame rate");
    }

    private static Task CaptureService_FlashbackEnableDisable_PreservesPreviewState()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n");
        var setFlashbackEnabled = ExtractTextBetween(
            captureServiceText,
            "public Task SetFlashbackEnabledAsync",
            "/// <summary>\n    /// Updates flashback-specific fields");
        var stopAndDisposeRecordingBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private async Task DisposeTransientRecordingBackendAsync");

        AssertContains(setFlashbackEnabled, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(setFlashbackEnabled, "if (_flashbackEnabled == enabled)");
        AssertContains(setFlashbackEnabled, "if (enabled && (_flashbackSink != null || _isRecording))");
        AssertContains(setFlashbackEnabled, "if (!enabled && !_flashbackBackend.HasAnyResource)");
        AssertContains(
            setFlashbackEnabled,
            "if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)\n                {\n                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);");
        AssertContains(setFlashbackEnabled, "if (_isRecording)\n            {\n                _pendingFlashbackEnableAfterRecording = true;");
        AssertContains(setFlashbackEnabled, "FLASHBACK_ENABLE_DEFERRED");
        var recordingActiveEnableBranch = ExtractTextBetween(
            setFlashbackEnabled,
            "if (_isRecording)\n            {",
            "\n            _pendingFlashbackEnableAfterRecording = false;");
        AssertContains(recordingActiveEnableBranch, "return;");
        AssertDoesNotContain(recordingActiveEnableBranch, "EnsureFlashbackPreviewBackendAsync");
        var immediateEnableBranch = ExtractTextBetween(
            setFlashbackEnabled,
            "_pendingFlashbackEnableAfterRecording = false;\n            if (_unifiedVideoCapture != null && _currentSettings != null)",
            "\n        }, cancellationToken);");
        AssertContains(immediateEnableBranch, "try");
        AssertContains(immediateEnableBranch, "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, transitionToken)");
        AssertContains(immediateEnableBranch, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(immediateEnableBranch, "FLASHBACK_ENABLE_IMMEDIATE_CANCELLED");
        AssertContains(immediateEnableBranch, "catch");
        AssertContains(immediateEnableBranch, "_flashbackEnabled = false;");
        AssertContains(immediateEnableBranch, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(immediateEnableBranch, "await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true)");
        AssertContains(immediateEnableBranch, "FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(immediateEnableBranch, "throw;");

        AssertContains(stopAndDisposeRecordingBackend, "if (_pendingFlashbackEnableAfterRecording)");
        AssertContains(stopAndDisposeRecordingBackend, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(
            stopAndDisposeRecordingBackend,
            "if (_flashbackEnabled && _isVideoPreviewActive && _unifiedVideoCapture != null && _currentSettings != null)");
        AssertContains(
            stopAndDisposeRecordingBackend,
            "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken)");
        AssertContains(
            stopAndDisposeRecordingBackend,
            "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(
            stopAndDisposeRecordingBackend,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n                {\n                    cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(stopAndDisposeRecordingBackend, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        var deferredEnableFailureBranch = ExtractTextBetween(
            stopAndDisposeRecordingBackend,
            "catch (Exception ex)\n                {",
            "Logger.Log($\"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL");
        AssertContains(deferredEnableFailureBranch, "_flashbackEnabled = false;");
        AssertContains(deferredEnableFailureBranch, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(deferredEnableFailureBranch, "await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true)");

        return Task.CompletedTask;
    }

}
