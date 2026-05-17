using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange()
    {
        var captureServiceText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingStartState.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.AudioInputs.cs")
            + "\n" + ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
            + "\n" + ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.PreviewPipeline.cs")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings();
        var captureServiceRawText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.AudioInputs.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewPipeline.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource();
        var captureServiceRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleText = ReadCaptureServicePreviewLifecycleSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelCaptureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var startVideoPreview = ExtractTextBetween(
            captureServiceText,
            "public Task StartVideoPreviewAsync",
            "private bool CanReuseVideoCaptureForPreview");
        var retainedPreviewFastPath = ExtractTextBetween(
            startVideoPreview,
            "(_isRecording || _flashbackEnabled)",
            "ThrowIfPendingLibAvDrainTaskBlocksReentry()");
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
        AssertContains(startVideoPreview, "var previousSettings = _flashbackBackendSettings ?? _currentSettings;");
        AssertContains(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)");
        AssertOccursBefore(startVideoPreview, "var previousSettings = _flashbackBackendSettings ?? _currentSettings;", "_currentSettings = settings;");
        AssertOccursBefore(startVideoPreview, "CanReuseFlashbackBackend(previousSettings, settings)", "_currentSettings = settings;");
        AssertContains(startVideoPreview, "CanReuseVideoCaptureForPreview(_unifiedVideoCapture, settings)");
        AssertRegex(
            startVideoPreview,
            @"if\s*\(\s*_unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!CanReuseVideoCaptureForPreview\(_unifiedVideoCapture,\s*settings\)\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*true\)",
            "preview settings-change recycle branch");
        AssertRegex(
            startVideoPreview,
            @"if\s*\(\s*_unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*!_flashbackEnabled\s*\)\s*\{[^{}]*DisposePreviewPipelineAsync\(transitionToken,\s*purgeFlashbackSegments:\s*false\)",
            "preview flashback-disabled recycle branch");
        AssertRegex(
            startVideoPreview,
            @"if\s*\(\s*_unifiedVideoCapture\s*!=\s*null\s*&&\s*!_isRecording\s*&&\s*_flashbackSink\s*!=\s*null\s*&&\s*flashbackBackendSettingsChanged\s*\)\s*\{[^{}]*DisposeFlashbackPreviewBackendAsync\(transitionToken,\s*purgeSegments:\s*true\)",
            "preview flashback-backend recycle branch");

        AssertContains(retainedPreviewFastPath, "_unifiedVideoCapture.SetPreviewSink(_previewFrameSink)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, settings, transitionToken)");
        AssertContains(retainedPreviewFastPath, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertOccursBefore(
            retainedPreviewFastPath,
            "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, settings, transitionToken)",
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
        var previewAudioGraphRaw = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs")
            .Replace("\r\n", "\n");
        var previewMicMonitorStart = ExtractTextBetween(
            previewAudioGraphRaw,
            "private async Task StartPreviewMicrophoneMonitorAsync",
            "private async Task RollbackPreviewAudioCaptureStartupAsync");
        AssertContains(previewMicMonitorStart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(previewMicMonitorStart, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(previewMicMonitorStart, "MIC_MONITOR_PREVIEW_START_DISPOSE_WARN");
        AssertContains(previewMicMonitorStart, "_microphoneCapture = micCapture;");
        AssertContains(previewMicMonitorStart, "micCapture = null;");
        AssertContains(previewMicMonitorStart, "_microphoneCapture = micCapture;\n            micCapture = null;");

        AssertContains(ensureFlashbackAudio, "if (settings.AudioEnabled && _wasapiAudioCapture == null)");
        AssertContains(ensureFlashbackAudio, "AttachFlashbackAudioIfSupported(_wasapiAudioCapture, reason)");
        AssertContains(ensureFlashbackAudio, "if (_micMonitorEnabled && _microphoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))");
        AssertContains(ensureFlashbackAudio, "_microphoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples))");

        AssertContains(startAudioPreview, "AttachFlashbackAudioIfSupported(_wasapiAudioCapture,");
        AssertOccursBefore(
            startAudioPreview,
            "AttachFlashbackAudioIfSupported(_wasapiAudioCapture,",
            "await StartWasapiPlaybackAsync(transitionToken)");
        AssertContains(startAudioPreview, "var createdCaptureForAudioPreview = false;");
        AssertContains(startAudioPreview, "createdCaptureForAudioPreview = true;");
        AssertContains(startAudioPreview, "_isAudioPreviewActive = false;");
        AssertContains(startAudioPreview, "DetachWasapiAudioCapture(capture);");
        AssertOccursBefore(
            startAudioPreview,
            "_isAudioPreviewActive = true;",
            "await StartWasapiPlaybackAsync(transitionToken)");
        var startAudioPreviewRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task StartAudioPreviewAsync",
            "public Task StopAudioPreviewAsync");
        AssertContains(startAudioPreviewRaw, "AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN");
        var updateAudioInput = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateAudioInputAsync",
            "        }, cancellationToken);\n}");
        AssertContains(updateAudioInput, "var committedSwitchToken = CancellationToken.None;");
        AssertContains(updateAudioInput, "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)");
        AssertContains(updateAudioInput, "await StartWasapiPlaybackAsync(committedSwitchToken)");
        AssertOccursBefore(
            updateAudioInput,
            "await newCapture.InitializeAsync(resolvedId, committedSwitchToken)",
            "DetachWasapiAudioCapture(oldCapture);");
        AssertContains(updateAudioInput, "_audioDeviceId = previousDeviceId;");
        AssertContains(updateAudioInput, "_audioDeviceName = previousDeviceName;");
        AssertContains(updateAudioInput, "activeSink != null && !ReferenceEquals(activeSink, _flashbackSink)");
        AssertOccursBefore(
            updateAudioInput,
            "newCapture.AttachRecordingSink(activeSink);",
            "await StartWasapiPlaybackAsync(committedSwitchToken)");
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
            "        }, cancellationToken);\n}");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");

        AssertContains(captureServiceText, "_flashbackBackendSettings = CloneCaptureSettings(settings)");
        AssertContains(captureServiceText, "_flashbackBackendSettings = CloneCaptureSettings(_currentSettings)");
        AssertContains(captureServiceText, "_flashbackBackend.ClearSinkAndSettings();");
        AssertContains(captureServiceText, "_flashbackBackend.Clear();");
        AssertContains(captureServiceText, "FlashbackPlaybackController? playbackController = null;");
        AssertContains(captureServiceText, "controller is { IsDisposed: false, IsInitialized: false }");
        AssertContains(captureServiceText, "(playbackController ?? _flashbackPlaybackController)?.Dispose();");
        AssertContains(coordinatorText, "controller == null || controller.IsDisposed");
        AssertContains(coordinatorText, "controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled }");
        AssertContains(coordinatorText, "? \"disposed\"");
        AssertContains(captureServiceText, "!CanReuseFlashbackBackend(_flashbackBackendSettings, settings)");
        AssertContains(captureServiceText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,");
        AssertContains(startVideoPreview, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startVideoPreview, "await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken)");
        var stopVideoPreviewCore = ExtractTextBetween(
            captureServiceText,
            "private Task StopVideoPreviewCoreAsync",
            "private bool CanReuseVideoCaptureForPreview");
        AssertContains(stopVideoPreviewCore, "var commitStoppedState = false;");
        AssertContains(stopVideoPreviewCore, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(stopVideoPreviewCore, "commitStoppedState = true;");
        AssertContains(stopVideoPreviewCore, "if (commitStoppedState)\n                {\n                    _isVideoPreviewActive = false;");
        AssertContains(stopVideoPreviewCore, "await StopTelemetryPollAsync().ConfigureAwait(false);");
        AssertContains(stopVideoPreviewCore, "catch (Exception ex) when (stopFailure != null)");
        AssertDoesNotContain(stopVideoPreviewCore, "!keepPipelineAlive) StopTelemetryPoll()");
        var stopPreviewBlock = ExtractTextBetween(
            viewModelCaptureText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreviewBlock, "var commitStoppedState = false;");
        AssertContains(stopPreviewBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(stopPreviewBlock, "if (commitStoppedState)\n            {\n                IsPreviewing = false;\n            }");
        AssertOccursBefore(
            ExtractTextBetween(
                captureServiceText,
                "if (_flashbackEnabled && _flashbackSink != null)",
                "_recordingSink = activeFlashbackSink"),
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken,",
            "activeFlashbackSink.BeginRecording");
        AssertContains(ensureFlashbackAudio, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(ensureFlashbackAudio, "await micCapture.DisposeAsync()");

        return Task.CompletedTask;
    }
}
