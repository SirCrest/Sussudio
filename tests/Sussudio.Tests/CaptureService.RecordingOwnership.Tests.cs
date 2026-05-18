using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n                {\n                    result = sinkResult;");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingLifecycleLivesInFocusedPartial()
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
        var libAvFinalizeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs")
            .Replace("\r\n", "\n");
        var flashbackFinalizeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(rootText, "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
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
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "RunTransitionAsync(CaptureSessionState.Recording");
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
        AssertContains(flashbackFinalizeText, "var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();");
        AssertContains(flashbackRecordingText, "_recordingBackend.IsFlashbackBackend(_flashbackSink)");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs")
            }).Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var rollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(lifecycleText, "Recording start rollback cleanup failed");
        AssertContains(rollbackText, "private async Task RollbackRecordingStartAsync(");
        AssertContains(rollbackText, "CAPTURE_RECORDING_START_FAIL");
        AssertContains(rollbackText, "RecordLastRecordingFailure(ex);");
        AssertContains(rollbackText, "CancelRecordingStartRollback(\"start_recording_failed\")");
        AssertContains(rollbackText, "FLASHBACK_RECORDING_START_ROLLBACK_WARN");
        AssertContains(rollbackText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertContains(rollbackText, "Recording start rollback cleanup failed");
        AssertContains(rollbackText, "Transient recording backend cleanup failed during start rollback");
        AssertContains(rollbackText, "_recordingStopwatch.Reset();");
        AssertDoesNotContain(finalizationCallSiteText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");
        AssertOccursBefore(rollbackText, "CAPTURE_RECORDING_START_FAIL", "RecordLastRecordingFailure(ex);");
        AssertOccursBefore(rollbackText, "RecordLastRecordingFailure(ex);", "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertOccursBefore(rollbackText, "rollback.FlashbackRecordingBackendLeaseHeld = false;", "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertOccursBefore(rollbackText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);", "await DisposeTransientRecordingBackendAsync(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingOutcomeStateLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs")
            }).Replace("\r\n", "\n");
        var routerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
            .Replace("\r\n", "\n");
        var outcomeStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private string? _lastOutputPath;");
        AssertDoesNotContain(rootText, "private string _lastFinalizeStatus = \"None\";");
        AssertDoesNotContain(rootText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertDoesNotContain(rootText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(outcomeStateText, "private string? _lastOutputPath;");
        AssertContains(outcomeStateText, "private string _lastFinalizeStatus = \"None\";");
        AssertContains(outcomeStateText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertContains(outcomeStateText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "_lastOutputPath = finalOutputPath;");
        AssertContains(outcomeStateText, "_lastFinalizeStatus = \"Recording\";");
        AssertContains(outcomeStateText, "_lastFinalizeUtc = null;");
        AssertContains(outcomeStateText, "_lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(outcomeStateText, "if (updateOutputPath)");
        AssertContains(outcomeStateText, "_lastOutputPath = result.OutputPath;");
        AssertContains(outcomeStateText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertContains(outcomeStateText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertContains(outcomeStateText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");

        AssertContains(flashbackStartText, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(libAvStartText, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = fbRecordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = recordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastFinalizeStatus = \"Recording\";");
        AssertDoesNotContain(lifecycleText, "_lastFinalizeUtc = null;");
        AssertDoesNotContain(lifecycleText, "_lastPreservedArtifacts = Array.Empty<string>();");

        AssertContains(finalizationCallSiteText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(finalizationCallSiteText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(routerText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertDoesNotContain(routerText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(finalizationCallSiteText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeStatus = fbResult.StatusMessage;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastPreservedArtifacts = fbResult.PreservedArtifacts;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        return Task.CompletedTask;
    }

}
