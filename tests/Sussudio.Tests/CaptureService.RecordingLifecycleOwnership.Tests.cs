using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"))
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

    internal static Task CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var recordingBackendText = ReadRepoFile("Sussudio/Services/Capture/CapturePipelineResources.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"))
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
        AssertDoesNotContain(rootText, "private Task? _pendingLibAvDrainTask");
        AssertDoesNotContain(rootText, "private RecordingContext? _recordingContext;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.SettingsSnapshot;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.LibAvSink;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.Sink;");
        AssertDoesNotContain(rootText, "get => _recordingBackend.Context;");
        AssertContains(recordingBackendText, "internal sealed class CaptureRecordingBackendResources");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureRecordingBackendResources.cs")),
            "recording backend resources folded into CapturePipelineResources.cs");
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
        AssertContains(lifecycleText, "private static async Task<StorageFolder> OpenRecordingOutputFolderAsync(");
        AssertContains(lifecycleText, "Output folder is unavailable: {settings.OutputPath}");
        AssertContains(lifecycleText, "private async Task<RecordingContext> CreateLibAvRecordingContextAsync(");
        AssertContains(lifecycleText, "private async Task<RecordingContext> CreateFlashbackRecordingContextAsync(");
        AssertContains(lifecycleText, "new RecordingContextRequest");
        AssertContains(lifecycleText, "GpuHandles = new GpuPipelineHandles(");
        AssertContains(lifecycleText, "GpuHandles = GpuPipelineHandles.None");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartContext.cs")),
            "old recording start context partial removed");
        AssertContains(flashbackStartText, "private async Task DisposeUnusableFlashbackRecordingBackendAsync(");
        AssertContains(flashbackStartText, "private async Task StartFlashbackRecordingAsync(");
        AssertContains(flashbackStartText, "await OpenRecordingOutputFolderAsync(settings)");
        AssertContains(flashbackStartText, "await CreateFlashbackRecordingContextAsync(");
        AssertContains(flashbackStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(flashbackStartText, "_recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);");
        AssertContains(flashbackStartText, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackStartText, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(flashbackStartText, "videoCapture?.BeginFlashbackRecordingAccounting();");
        AssertDoesNotContain(flashbackStartText, "StorageFolder.GetFolderFromPathAsync");
        AssertDoesNotContain(flashbackStartText, "new RecordingContextRequest");
        AssertDoesNotContain(flashbackStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAsync(");
        AssertContains(libAvStartText, "_recordingBackend.InstallLibAv(");
        AssertContains(libAvStartText, "await OpenRecordingOutputFolderAsync(settings)");
        AssertContains(libAvStartText, "await CreateLibAvRecordingContextAsync(");
        AssertContains(libAvStartText, "await RefreshSourceTelemetryAsync(transitionToken)");
        AssertContains(libAvStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)");
        AssertContains(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(");
        AssertContains(libAvStartText, "await PrepareLibAvRecordingVideoCaptureAsync(");
        AssertOccursBefore(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)", "await StartLibAvRecordingAudioInputsAsync(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "_recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(");
        AssertOccursBefore(libAvStartText, "await StartLibAvRecordingAudioInputsAsync(", "await unifiedVideoCapture.StartRecordingAsync(");
        AssertContains(libAvStartText, "private async Task<UnifiedVideoCapture> PrepareLibAvRecordingVideoCaptureAsync(");
        AssertContains(libAvStartText, "rollback.OwnedUnifiedVideoCapture = new UnifiedVideoCapture();");
        AssertContains(libAvStartText, "AttachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);");
        AssertContains(libAvStartText, "_videoPipeline.InstallCapture(rollback.OwnedUnifiedVideoCapture);");
        AssertContains(libAvStartText, "TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _videoPipeline.PreviewFrameSink : null);");
        AssertContains(libAvStartText, "Recording requires {(requireP010 ? \"P010\" : \"NV12\")}, but the active source-reader session negotiated");
        AssertContains(libAvStartText, "Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr=");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAudioInputsAsync(");
        AssertContains(libAvStartText, "rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();");
        AssertContains(libAvStartText, "_previewAudioGraph.ProgramCapture.AttachRecordingSink(recordingSink);");
        AssertContains(libAvStartText, "rollback.SinkAttachedForAudioOnly = true;");
        AssertContains(libAvStartText, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(libAvStartText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);");
        AssertContains(libAvStartText, "micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(libAvStartText, "MICROPHONE_CAPTURE_START");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartLibAv.VideoCapture.cs")),
            "old LibAv recording video-capture startup partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingStartLibAv.AudioInputs.cs")),
            "old LibAv recording audio-input startup partial removed");
        AssertDoesNotContain(libAvStartText, "StorageFolder.GetFolderFromPathAsync");
        AssertDoesNotContain(libAvStartText, "new RecordingContextRequest");
        AssertDoesNotContain(libAvStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(lifecycleText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(lifecycleText, "await StopAndDisposeRecordingBackendAsync(\"Stopped\", emergency, transitionToken)");
        AssertContains(lifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertEqual(false, System.IO.File.Exists(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.RecordingStopLifecycle.cs")),
            "old recording stop lifecycle partial removed");
        AssertContains(libAvFinalizeText, "var detachedBackend = _recordingBackend.DetachLibAvBackend();");
        AssertContains(libAvFinalizeText, "private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvFinalizeText, "private async Task DetachLibAvRecordingAudioBeforeSinkStopAsync()");
        AssertContains(libAvFinalizeText, "private async Task<LibAvFinalizeStepResult> StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(libAvFinalizeText, "private async Task<LibAvFinalizeStepResult> DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvFinalizeStepResult(");
        AssertContains(libAvFinalizeText, "private readonly record struct LibAvVideoBoundaryStopResult(");
        AssertContains(libAvFinalizeText, "VIDEO_DIAG mf_source_reader ");
        AssertContains(libAvFinalizeText, "VIDEO_DIAG recording_pipeline ");
        AssertContains(libAvFinalizeText, "var libAvDrainTask = libAvSink.EncodingCompletionTask;");
        AssertContains(libAvFinalizeText, "reason: \"recording_stop_deferred_drain\"");
        AssertContains(libAvFinalizeText, "_previewAudioGraph.DetachCapture(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvResources.cs")),
            "old broad LibAv resource finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvVideoBoundary.cs")),
            "old LibAv video-boundary finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvSink.cs")),
            "old LibAv sink finalization partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvIdlePreview.cs")),
            "old LibAv idle-preview finalization partial removed");
        AssertContains(flashbackFinalizeText, "var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();");
        AssertContains(flashbackRecordingText, "_recordingBackend.IsFlashbackBackend(_flashbackBackend.Sink)");
        AssertContains(flashbackRecordingText, "private FlashbackSessionContext CreateFlashbackSessionContext(");
        AssertContains(flashbackRecordingText, "var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);");
        AssertContains(flashbackRecordingText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertContains(flashbackRecordingText, "private static readonly (int Numerator, int Denominator)[] CommonFlashbackFrameRateParts");
        AssertContains(flashbackRecordingText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(flashbackRecordingText, "private static string? ResolveFlashbackCodecDowngradeReason(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackRecording.SessionContext.cs")),
            "old Flashback recording session-context partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackRecording.FrameRate.cs")),
            "old Flashback recording frame-rate partial removed");

        return Task.CompletedTask;
    }
}
