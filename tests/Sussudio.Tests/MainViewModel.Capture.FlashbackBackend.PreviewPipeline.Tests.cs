using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task CaptureService_DeviceSwitchTeardown_StopsVideoBeforeFlashbackDisposal()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStart.cs")
            .Replace("\r\n", "\n");
        var unifiedVideoCaptureText = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
                .Replace("\r\n", "\n");
        var disposePreviewPipeline = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposePreviewPipelineAsync",
            "\n}");
        var unifiedDisposeCore = ExtractTextBetween(
            unifiedVideoCaptureText,
            "private async ValueTask DisposeCoreAsync",
            "private void ThrowIfDisposed()");

        AssertContains(disposePreviewPipeline, "unifiedVideoCapture.SetPreviewSink(null);");
        AssertContains(disposePreviewPipeline, "unifiedVideoCapture.SetFlashbackSink(null);");
        AssertContains(disposePreviewPipeline, "PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE");
        AssertOccursBefore(
            disposePreviewPipeline,
            "await unifiedVideoCapture.StopAsync().ConfigureAwait(false);",
            "await DisposeFlashbackPreviewBackendAsync(");
        AssertOccursBefore(
            disposePreviewPipeline,
            "await DisposeFlashbackPreviewBackendAsync(",
            "await unifiedVideoCapture.DisposeForPreviewReinitAsync().ConfigureAwait(false);");
        AssertDoesNotContain(disposePreviewPipeline, "await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);");
        AssertContains(unifiedVideoCaptureText, "public async ValueTask DisposeForPreviewReinitAsync()");
        AssertContains(unifiedDisposeCore, "if (disposeSharedD3DDeviceManager)");
        AssertContains(unifiedDisposeCore, "UNIFIED_VIDEO_REINIT_RETIRE_SHARED_D3D_MANAGER");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange()
    {
        var captureServiceText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            + "\n" + ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings();
        var captureServiceRawText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource();
        var captureServiceRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleText = ReadCaptureServicePreviewLifecycleSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var flashbackPreviewBackendText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs");
        var flashbackBackendResourcesText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var viewModelPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var startVideoPreview = ExtractTextBetween(
            captureServiceText,
            "public Task StartVideoPreviewAsync",
            "private bool CanReuseVideoCaptureForPreview");
        var retainedPreviewFastPath = ExtractTextBetween(
            startVideoPreview,
            "private async Task<bool> TryStartPreviewFromRetainedPipelineAsync",
            "private async Task StartFreshPreviewPipelineAsync");
        var ensureFlashbackAudio = ExtractTextBetween(
            captureServiceText,
            "private async Task EnsureFlashbackAudioInputsAsync",
            "private async Task EnsureFlashbackPreviewBackendAsync");
        var startAudioPreview = ExtractTextBetween(
            captureServiceText,
            "public Task StartAudioPreviewAsync",
            "public Task StopAudioPreviewAsync");

        AssertDoesNotContain(captureServiceRootText, "public Task StartVideoPreviewAsync");
        AssertDoesNotContain(captureServiceRootText, "private async Task DisposePreviewPipelineAsync");
        AssertContains(previewLifecycleText, "public Task StartVideoPreviewAsync");
        AssertContains(previewLifecycleText, "private async Task DisposePreviewPipelineAsync");
        AssertContains(startVideoPreview, "var previousSettings = _flashbackBackend.SettingsSnapshot ?? _currentSettings;");
        AssertContains(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)");
        AssertOccursBefore(startVideoPreview, "var previousSettings = _flashbackBackend.SettingsSnapshot ?? _currentSettings;", "_currentSettings = settings;");
        AssertOccursBefore(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)", "_currentSettings = settings;");
        AssertContains(startVideoPreview, "CanReuseVideoCaptureForPreview(unifiedVideoCapture, settings)");
        AssertRegex(
            startVideoPreview,
            @"var\s+unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!CanReuseVideoCaptureForPreview\(unifiedVideoCapture,\s*settings\)\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*true\)",
            "preview settings-change recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!_flashbackEnabled\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*false\)",
            "preview flashback-disabled recycle branch");
        AssertRegex(
            startVideoPreview,
            @"unifiedVideoCapture\s*=\s*_videoPipeline\.Capture;\s*if\s*\(\s*unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*_flashbackBackend\.Sink\s*!=\s*null\s*&&\s*flashbackBackendSettingsChanged\s*\)\s*\{[^{}]*DisposeFlashbackPreviewBackendAsync\(transitionToken,\s*purgeSegments:\s*true\)",
            "preview flashback-backend recycle branch");

        AssertContains(retainedPreviewFastPath, "unifiedVideoCapture.SetPreviewSink(_videoPipeline.PreviewFrameSink)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertOccursBefore(
            retainedPreviewFastPath,
            "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken)",
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertOccursBefore(
            retainedPreviewFastPath,
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,",
            "_isVideoPreviewActive = true;");
        var startVideoPreviewRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task StartVideoPreviewAsync",
            "private bool CanReuseVideoCaptureForPreview");
        AssertOccursBefore(
            startVideoPreviewRaw,
            "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)",
            "// Start flashback AFTER");
        var previewAudioGraphRaw = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs")
            .Replace("\r\n", "\n");
        var previewMicMonitorStart = ExtractTextBetween(
            previewAudioGraphRaw,
            "private async Task StartPreviewMicrophoneMonitorAsync",
            "private async Task RollbackPreviewAudioCaptureStartupAsync");
        AssertContains(previewMicMonitorStart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(previewMicMonitorStart, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(previewMicMonitorStart, "MIC_MONITOR_PREVIEW_START_DISPOSE_WARN");
        AssertContains(previewMicMonitorStart, "_previewAudioGraph.MicrophoneCapture = micCapture;");
        AssertContains(previewMicMonitorStart, "micCapture = null;");
        AssertContains(previewMicMonitorStart, "_previewAudioGraph.MicrophoneCapture = micCapture;\n            micCapture = null;");

        AssertContains(ensureFlashbackAudio, "if (settings.AudioEnabled && _previewAudioGraph.ProgramCapture == null)");
        AssertContains(ensureFlashbackAudio, "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, reason)");
        AssertContains(ensureFlashbackAudio, "if (_micMonitorEnabled && _previewAudioGraph.MicrophoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))");
        AssertContains(ensureFlashbackAudio, "_previewAudioGraph.MicrophoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples))");

        AssertContains(startAudioPreview, "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture,");
        AssertOccursBefore(
            startAudioPreview,
            "AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture,",
            "await _previewAudioGraph.StartPlaybackAsync(");
        AssertContains(startAudioPreview, "var createdCaptureForAudioPreview = false;");
        AssertContains(startAudioPreview, "createdCaptureForAudioPreview = true;");
        AssertContains(startAudioPreview, "_isAudioPreviewActive = false;");
        AssertContains(startAudioPreview, "_previewAudioGraph.DetachCapture(");
        AssertOccursBefore(
            startAudioPreview,
            "_isAudioPreviewActive = true;",
            "await _previewAudioGraph.StartPlaybackAsync(");
        var startAudioPreviewRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task StartAudioPreviewAsync",
            "public Task StopAudioPreviewAsync");
        AssertContains(startAudioPreviewRaw, "AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN");
        var updateAudioInput = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateAudioInputAsync",
            "private void OnWasapiAudioLevelUpdated");
        AssertContains(updateAudioInput, "var committedSwitchToken = CancellationToken.None;");
        AssertContains(updateAudioInput, "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)");
        AssertContains(updateAudioInput, "await _previewAudioGraph.StartPlaybackAsync(");
        AssertOccursBefore(
            updateAudioInput,
            "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)",
            "_previewAudioGraph.DetachCapture(");
        AssertContains(updateAudioInput, "_audioDeviceId = previousDeviceId;");
        AssertContains(updateAudioInput, "_audioDeviceName = previousDeviceName;");
        AssertContains(updateAudioInput, "activeSink != null && !ReferenceEquals(activeSink, _flashbackBackend.Sink)");
        AssertOccursBefore(
            updateAudioInput,
            "newCapture.AttachRecordingSink(activeSink);",
            "await _previewAudioGraph.StartPlaybackAsync(");
        var updateMicrophoneMonitor = ExtractTextBetween(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs").Replace("\r\n", "\n"),
            "public Task UpdateMicrophoneMonitorAsync",
            "        }, cancellationToken);");
        AssertContains(updateMicrophoneMonitor, "if (_isRecording)");
        AssertContains(updateMicrophoneMonitor, "MIC_MONITOR_UPDATE_DEFERRED recording=true");
        AssertOccursBefore(
            updateMicrophoneMonitor,
            "MIC_MONITOR_UPDATE_DEFERRED recording=true",
            "await DisposeMicrophoneCaptureAsync()");
        var updateAudioInputRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task UpdateAudioInputAsync",
            "private void OnWasapiAudioLevelUpdated");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");

        AssertContains(captureServiceText, "await _flashbackBackend.StartPreviewBackendAsync(");
        AssertContains(captureServiceText, "new FlashbackPreviewBackendStartRequest(");
        AssertContains(captureServiceText, "CloneCaptureSettings(settings),");
        AssertContains(captureServiceText, "CloneCaptureSettings(currentSettings)");
        AssertContains(flashbackBackendResourcesText, "SettingsSnapshot = request.SettingsSnapshot;");
        AssertContains(flashbackBackendResourcesText, "ClearSinkAndSettings();");
        AssertContains(captureServiceText, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        AssertContains(flashbackBackendResourcesText, "Clear();");
        AssertContains(flashbackBackendResourcesText, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(flashbackBackendResourcesText, "var bufferManager = new FlashbackBufferManager(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "Install(");
        AssertContains(flashbackBackendResourcesText, "AttachProducers(");
        AssertContains(flashbackBackendResourcesText, "playbackController.Initialize(");
        AssertContains(flashbackBackendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(flashbackBackendResourcesText, "flashbackSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(flashbackBackendResourcesText, "request.ScheduleDeferredCleanup(");
        AssertDoesNotContain(captureServiceText, "var bufferManager = new FlashbackBufferManager(");
        AssertDoesNotContain(captureServiceText, "FlashbackPlaybackController? playbackController = null;");
        AssertDoesNotContain(captureServiceText, "flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);");
        AssertDoesNotContain(flashbackPreviewBackendText, "flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(captureServiceText, "controller is { IsDisposed: false, IsInitialized: false }");
        AssertContains(coordinatorText, "controller == null || controller.IsDisposed");
        AssertContains(coordinatorText, "controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled }");
        AssertContains(coordinatorText, "? \"disposed\"");
        AssertContains(captureServiceText, "!CanReuseFlashbackBackend(_flashbackBackend.SettingsSnapshot, settings)");
        AssertContains(captureServiceText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertContains(startVideoPreview, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startVideoPreview, "await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken)");
        var stopVideoPreviewCore = ExtractTextBetween(
            captureServiceText,
            "private Task StopVideoPreviewCoreAsync",
            "private async Task DisposePreviewPipelineAsync");
        AssertContains(stopVideoPreviewCore, "var commitStoppedState = false;");
        AssertContains(stopVideoPreviewCore, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(stopVideoPreviewCore, "commitStoppedState = true;");
        AssertContains(stopVideoPreviewCore, "if (commitStoppedState)\n                {\n                    _isVideoPreviewActive = false;");
        AssertContains(stopVideoPreviewCore, "await StopTelemetryPollAsync().ConfigureAwait(false);");
        AssertContains(stopVideoPreviewCore, "catch (Exception ex) when (stopFailure != null)");
        AssertDoesNotContain(stopVideoPreviewCore, "!keepPipelineAlive) StopTelemetryPoll()");
        var stopPreviewBlock = ExtractTextBetween(
            viewModelPreviewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreviewBlock, "var commitStoppedState = false;");
        AssertContains(stopPreviewBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(stopPreviewBlock, "if (commitStoppedState)\n            {\n                _context.SetIsPreviewing(false);\n            }");
        AssertOccursBefore(
            ExtractTextBetween(
                captureServiceText,
                "if (_flashbackEnabled && _flashbackBackend.Sink != null)",
                "_recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);"),
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,",
            "activeFlashbackSink.BeginRecording");
        AssertContains(ensureFlashbackAudio, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(ensureFlashbackAudio, "await micCapture.DisposeAsync()");

        return Task.CompletedTask;
    }
}
