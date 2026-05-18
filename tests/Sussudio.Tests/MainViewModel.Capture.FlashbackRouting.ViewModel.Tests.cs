using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        foreach (var methodName in new[]
        {
            "SetFlashbackEnabledAsync",
            "RestartFlashbackAsync",
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
            "UpdateFlashbackSettingsAsync",
            "ExportFlashbackRangeAsync",
            "ExportFlashbackLastNSecondsAsync",
            "GetFlashbackSegments",
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetOutPoint",
            "FlashbackClearInOutPoints"
        })
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        var viewModelFiles = ReadMainViewModelCodeFiles();
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs")
            .Replace("\r\n", "\n");
        var viewModelText = string.Join("\n", viewModelFiles.Values) + "\n" + recordingSettingsAutomationControllerText;
        var viewModelSharedStateText = viewModelFiles["MainViewModel.State.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.PreviewState.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.CaptureState.cs"];
        var viewModelAudioStateText = viewModelFiles["MainViewModel.AudioState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackSettings.cs"];
        var automationRecordingSettingsText = viewModelFiles["MainViewModel.AutomationRecordingSettings.cs"];
        var automationRecordingFormatText = recordingSettingsAutomationControllerText;
        var automationRecordingQualityText = recordingSettingsAutomationControllerText;
        var automationSplitEncodeModeText = recordingSettingsAutomationControllerText;
        var automationCustomBitrateText = recordingSettingsAutomationControllerText;
        var automationEncoderPresetText = recordingSettingsAutomationControllerText;
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackExport.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackExportOperation.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackExportAutomation.cs"];
        var flashbackPlaybackText = viewModelFiles["MainViewModel.FlashbackPlayback.cs"];
        var flashbackPlaybackCommandsText = viewModelFiles["MainViewModel.FlashbackPlaybackCommands.cs"];
        var flashbackAutomationText = flashbackSettingsText
            + "\n" + flashbackExportText
            + "\n" + flashbackExportOperationText
            + "\n" + flashbackExportAutomationText
            + "\n" + flashbackPlaybackText
            + "\n" + flashbackPlaybackCommandsText;
        var disposalText = viewModelFiles["MainViewModel.Disposal.cs"];
        var audioPropertyChangesText = viewModelFiles["MainViewModel.AudioPropertyChanges.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAudioPropertyChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioPropertyChanges.cs")
            .Replace("\r\n", "\n");
        var rawDisposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportOperation.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportAutomation.cs")
            .Replace("\r\n", "\n");
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackEncoderSettings.cs"];
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");
        var rawFlashbackEncoderSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackEncoderSettings.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackSettings.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource();

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackBeginScrub", "_sessionCoordinator.FlashbackBeginScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSeek", "_sessionCoordinator.FlashbackSeek(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackUpdateScrub", "return _sessionCoordinator.FlashbackUpdateScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrub", "_sessionCoordinator.FlashbackEndScrub()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrubAt", "_sessionCoordinator.FlashbackEndScrubAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPlay", "_sessionCoordinator.FlashbackPlay()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPause", "_sessionCoordinator.FlashbackPause()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackGoLive", "_sessionCoordinator.FlashbackGoLive()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackNudge", "_sessionCoordinator.FlashbackNudge(delta)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPoint", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPointAt", "_sessionCoordinator.FlashbackSetInPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPoint", "_sessionCoordinator.FlashbackSetOutPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPointAt", "_sessionCoordinator.FlashbackSetOutPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackClearInOutPoints", "=> _sessionCoordinator.FlashbackClearInOutPoints()");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        var updateFlashbackBufferStatus = ExtractMemberCode(flashbackPlaybackText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackPlaybackText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "ReferenceEquals(sender, _wasapiAudioCapture)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, _microphoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "Volatile.Write(ref _wasapiAudioCaptureFaultMessage, $\"{source}: {ex.Message}\");");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.InPointFilePts");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.OutPointFilePts");
        AssertContains(coordinatorText, "TimeSpan? InPointFilePts,");
        AssertContains(coordinatorText, "TimeSpan? OutPointFilePts,");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawFlashbackExportText, "Flashback export unavailable: flashback is not active.");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "case ExportFlashbackOutcome.Stale:");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "case ExportFlashbackOutcome.Stale:");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertContains(disposalText, "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(disposalText, "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(disposalText, "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawDisposalText, "_runtimeLifecycleController.StopForDispose();");
        AssertOccursBefore(rawDisposalText, "_runtimeLifecycleController.StopForDispose();", "var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(");
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertDoesNotContain(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "var reinitializeGeneration = Interlocked.Increment(ref _viewModel._previewReinitializeGeneration);");
        AssertContains(rawPreviewLifecycleControllerText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewLifecycleControllerText, "Volatile.Read(ref _viewModel._previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(rawPreviewLifecycleControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewLifecycleControllerText, "await AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewLifecycleControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewLifecycleControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewLifecycleControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewLifecycleControllerText, "if (ReferenceEquals(_viewModel._pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)\n                {\n                    _viewModel._pendingFlashbackCycleTask = null;\n                }");
        AssertContains(flashbackExportOperationText, "private abstract record ExportFlashbackOutcome");
        AssertContains(flashbackExportOperationText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
        AssertContains(flashbackExportOperationText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(flashbackExportOperationText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(flashbackExportOperationText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(flashbackExportOperationText, "_exportCts = null;");
        AssertContains(flashbackExportOperationText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(flashbackExportOperationText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(flashbackExportOperationText, "catch (ObjectDisposedException)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawFlashbackExportAutomationText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(flashbackExportOperationText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertDoesNotContain(
            flashbackExportText + "\n" + flashbackExportOperationText + "\n" + flashbackExportAutomationText,
            "exportCts.Dispose();");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "_suppressFlashbackFormatCycle is false");
        AssertContains(rawFlashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertContains(viewModelFlashbackStateText, "private bool _suppressFlashbackFormatCycle;");
        AssertMemberContains(automationRecordingSettingsText, "SetRecordingFormatAsync", "_recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken)");
        AssertMemberContains(automationRecordingFormatText, "SetRecordingFormatAsync", "_suppressFlashbackFormatCycle = true;");
        AssertMemberContains(automationRecordingFormatText, "SetRecordingFormatAsync", "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertMemberContains(automationRecordingFormatText, "SetRecordingFormatAsync", "await _viewModel._sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(automationRecordingQualityText, "SetQualityAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationRecordingQualityText, "SetQualityAsync", "_viewModel.SelectedQuality = matched;");
        AssertMemberContains(automationRecordingQualityText, "SetQualityAsync", "quality: settings.Quality");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "SplitEncodeMode: _viewModel.SelectedSplitEncodeMode");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "splitEncodeMode: settings.SplitEncodeMode");
        AssertMemberContains(automationCustomBitrateText, "SetCustomBitrateAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationCustomBitrateText, "SetCustomBitrateAsync", "_viewModel.CustomBitrateMbps = RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps);");
        AssertMemberContains(automationCustomBitrateText, "SetCustomBitrateAsync", "customBitrateMbps: settings.Bitrate");
        AssertMemberContains(automationEncoderPresetText, "SetPresetAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationEncoderPresetText, "SetPresetAsync", "_viewModel.SelectedPreset = matched;");
        AssertMemberContains(automationEncoderPresetText, "SetPresetAsync", "nvencPreset: settings.Preset");
        AssertMemberContains(flashbackEncoderSettingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackGpuDecodeChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "Interlocked.Increment(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "RestartFlashbackAfterSettingsUpdateAsync(updateTask, restartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "restartGeneration != Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "InvokeOnUiThreadAsync(");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "IsPreviewing && !IsRecording && _isLoadingSettings is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "shouldRestart is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "await RestartFlashbackAsync().ConfigureAwait(false)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "catch (OperationCanceledException ex)");
        AssertContains(rawFlashbackSettingsText, "RestartFlashbackAfterSettingsUpdate canceled");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedSplitEncodeModeChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "splitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "SplitEncodeMode: _viewModel.SelectedSplitEncodeMode");
        AssertMemberContains(automationSplitEncodeModeText, "SetSplitEncodeModeAsync", "splitEncodeMode: settings.SplitEncodeMode");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) canceled");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "var settings = BuildCaptureSettings();");
        AssertMemberContains(rawAudioPropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(\n                        true,\n                        \"audio_capture_enable\",");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings)");
        AssertMemberContains(rawAudioPropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, \"audio_capture_disable\", teardownCapture: true)");
        AssertContains(viewModelAudioStateText, "private int _audioEnabledChangeGeneration;");
        AssertContains(viewModelAudioStateText, "private bool _suppressAudioPreviewEnabledChangeOperation;");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "_suppressAudioPreviewEnabledChangeOperation = true;");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled");
        AssertMemberContains(audioPropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled");
        AssertMemberContains(rawAudioPropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=enable");
        AssertMemberContains(rawAudioPropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=disable");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackSettingsRestartGeneration;");

        foreach (var memberName in new[]
        {
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackEndScrubAt",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetInPointAt",
            "FlashbackSetOutPoint",
            "FlashbackSetOutPointAt",
            "FlashbackClearInOutPoints",
            "UpdateFlashbackBufferStatus",
            "UpdateFlashbackBitrate",
            "ExportFlashbackAsync",
            "SaveFlashbackLast5mAsync",
            "ExportFlashbackAutomationAsync",
            "GetFlashbackSegments",
            "SetFlashbackEnabledAsync",
            "RestartFlashbackAsync"
        })
        {
            AssertMemberDoesNotContain(flashbackAutomationText, memberName, "_captureService");
        }

        foreach (var memberName in new[]
        {
            "OnSelectedRecordingFormatChanged",
            "OnCustomBitrateMbpsChanged",
            "OnFlashbackBufferMinutesChanged",
            "OnFlashbackGpuDecodeChanged",
            "OnSelectedQualityChanged",
            "OnSelectedPresetChanged",
            "OnSelectedSplitEncodeModeChanged"
        })
        {
            var sourceText = memberName is "OnFlashbackBufferMinutesChanged" or "OnFlashbackGpuDecodeChanged"
                ? flashbackSettingsText
                : flashbackEncoderSettingsText;
            AssertMemberDoesNotContain(sourceText, memberName, "_captureService");
        }

        AssertNoRegex(
            viewModelText,
            @"\b_captureService\s*\.\s*(SetFlashbackEnabled|RestartFlashbackAsync|UpdateRecordingFormatAsync|CycleFlashbackEncoderSettingsAsync|UpdateFlashbackSettings|ExportFlashback|GetFlashbackSegments|FlashbackPlaybackController|FlashbackBufferManager|FlashbackDiskBytes|FlashbackTotalBytesWritten)\b",
            "MainViewModel flashback mutating/backend capture-service access");
        AssertNoRegex(
            viewModelText,
            @"\b(?:var|CaptureService)\s+\w+\s*=\s*_captureService\s*;",
            "MainViewModel local capture-service aliases");

        return Task.CompletedTask;
    }
}
