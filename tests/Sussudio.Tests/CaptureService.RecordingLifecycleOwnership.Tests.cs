using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvResources.cs"))
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        AssertContains(captureServiceText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(captureServiceText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(captureServiceText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(captureServiceText, "FoldLibAvAudioFaultIntoFinalizeResult(result, cancellationException);");
        AssertContains(captureServiceText, "PublishLibAvRecordingIntegrity(");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n            {\n                result = sinkResult;");
        AssertContains(captureServiceText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(captureServiceText, "_previewAudioGraph.DetachCapture(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var recordingBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureRecordingBackendResources.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");
        var libAvAudioInputsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.AudioInputs.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvResources.cs"))
            .Replace("\r\n", "\n");
        var flashbackFinalizeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingSessionContextText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.SessionContext.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(rootText, "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
        AssertContains(rootText, "get => _recordingBackend.SettingsSnapshot;");
        AssertContains(rootText, "set => _recordingBackend.SettingsSnapshot = value;");
        AssertContains(rootText, "get => _recordingBackend.LibAvSink;");
        AssertContains(rootText, "set => _recordingBackend.LibAvSink = value;");
        AssertContains(rootText, "get => _recordingBackend.Sink;");
        AssertContains(rootText, "set => _recordingBackend.Sink = value;");
        AssertContains(rootText, "get => _recordingBackend.Context;");
        AssertContains(rootText, "set => _recordingBackend.Context = value;");
        AssertDoesNotContain(rootText, "private CaptureSettings? _activeRecordingSettings;");
        AssertDoesNotContain(rootText, "private LibAvRecordingSink? _libavSink;");
        AssertDoesNotContain(rootText, "private IRecordingSink? _recordingSink;");
        AssertDoesNotContain(rootText, "private RecordingContext? _recordingContext;");
        AssertContains(recordingBackendText, "internal sealed class CaptureRecordingBackendResources");
        AssertContains(recordingBackendText, "public LibAvRecordingSink? LibAvSink { get; set; }");
        AssertContains(recordingBackendText, "public IRecordingSink? Sink { get; set; }");
        AssertContains(recordingBackendText, "public RecordingContext? Context { get; set; }");
        AssertContains(recordingBackendText, "public CaptureSettings? SettingsSnapshot { get; set; }");
        AssertContains(recordingBackendText, "public Task? PendingLibAvDrainTask { get; set; }");
        AssertContains(recordingBackendText, "public bool IsFlashbackBackend(FlashbackEncoderSink? flashbackSink)");
        AssertContains(recordingBackendText, "public void InstallLibAv(");
        AssertContains(recordingBackendText, "public void InstallFlashback(");
        AssertContains(recordingBackendText, "public ActiveRecordingBackend DetachLibAvBackend()");
        AssertContains(recordingBackendText, "public RecordingContext? DetachFlashbackBackend()");
        AssertContains(recordingBackendText, "public void ClearPendingLibAvDrainIfCompletedSuccessfully()");
        AssertContains(recordingBackendText, "public void ThrowIfPendingLibAvDrainBlocksReentry()");
        AssertContains(recordingBackendText, "Previous recording backend failed to finalize cleanly. Check the logs and retry.");
        AssertContains(recordingBackendText, "Previous recording backend cleanup was canceled. Check the logs and retry.");
        AssertContains(recordingBackendText, "Previous recording backend is still finalizing. Please wait a moment and try again.");
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "RunTransitionAsync(CaptureSessionState.Recording");
        AssertContains(lifecycleText, "_recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();");
        AssertContains(lifecycleText, "var rollback = new RecordingStartRollbackState();");
        AssertContains(lifecycleText, "await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken)");
        AssertContains(lifecycleText, "await StartFlashbackRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await StartLibAvRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);\n                throw;");
        AssertDoesNotContain(lifecycleText, "CAPTURE_RECORDING_START_FAIL");
        AssertDoesNotContain(lifecycleText, "FLASHBACK_RECORDING_START_ROLLBACK_WARN");
        AssertDoesNotContain(lifecycleText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertDoesNotContain(lifecycleText, "HDR_NEGOTIATION");
        AssertContains(lifecycleText, "private sealed class RecordingStartRollbackState");
        AssertContains(lifecycleText, "public RecordingContext? RecordingContext { get; set; }");
        AssertContains(lifecycleText, "public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }");
        AssertContains(flashbackStartText, "private async Task DisposeUnusableFlashbackRecordingBackendAsync(");
        AssertContains(flashbackStartText, "private async Task StartFlashbackRecordingAsync(");
        AssertContains(flashbackStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(flashbackStartText, "_recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);");
        AssertContains(flashbackStartText, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackStartText, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(flashbackStartText, "_unifiedVideoCapture?.BeginFlashbackRecordingAccounting();");
        AssertDoesNotContain(flashbackStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAsync(");
        AssertContains(libAvStartText, "_recordingBackend.InstallLibAv(");
        AssertContains(libAvStartText, "await RefreshSourceTelemetryAsync(transitionToken)");
        AssertContains(libAvStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)");
        AssertContains(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(");
        AssertOccursBefore(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)", "await StartLibAvRecordingAudioInputsAsync(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "_recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "await unifiedVideoCapture.StartRecordingAsync(");
        AssertContains(libAvAudioInputsText, "private async Task StartLibAvRecordingAudioInputsAsync(");
        AssertContains(libAvAudioInputsText, "rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();");
        AssertContains(libAvAudioInputsText, "_wasapiAudioCapture.AttachRecordingSink(recordingSink);");
        AssertContains(libAvAudioInputsText, "rollback.SinkAttachedForAudioOnly = true;");
        AssertContains(libAvAudioInputsText, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(libAvAudioInputsText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);");
        AssertContains(libAvAudioInputsText, "micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(libAvAudioInputsText, "MICROPHONE_CAPTURE_START");
        AssertDoesNotContain(libAvStartText, "rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();");
        AssertDoesNotContain(libAvStartText, "_wasapiAudioCapture.AttachRecordingSink(recordingSink);");
        AssertDoesNotContain(libAvStartText, "micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));");
        AssertDoesNotContain(libAvStartText, "MICROPHONE_CAPTURE_START");
        AssertDoesNotContain(libAvStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertDoesNotContain(lifecycleText, "public Task StopRecordingAsync(");
        AssertDoesNotContain(lifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(stopLifecycleText, "public Task StopRecordingAsync(");
        AssertContains(stopLifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(stopLifecycleText, "await StopAndDisposeRecordingBackendAsync(\"Stopped\", emergency, transitionToken)");
        AssertContains(libAvFinalizeText, "var detachedBackend = _recordingBackend.DetachLibAvBackend();");
        AssertContains(libAvFinalizeText, "private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvFinalizeText, "private async Task<LibAvFinalizeStepResult> DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvFinalizeStepResult(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvVideoBoundaryStopResult(");
        AssertContains(flashbackFinalizeText, "var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();");
        AssertContains(flashbackRecordingText, "_recordingBackend.IsFlashbackBackend(_flashbackSink)");
        AssertContains(flashbackRecordingSessionContextText, "private FlashbackSessionContext CreateFlashbackSessionContext(");
        AssertContains(flashbackRecordingSessionContextText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertDoesNotContain(flashbackRecordingText, "private FlashbackSessionContext CreateFlashbackSessionContext(");

        return Task.CompletedTask;
    }
}
