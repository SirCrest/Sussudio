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
        var viewModelText = string.Join("\n", viewModelFiles.Values);
        var viewModelSharedStateText = viewModelFiles["MainViewModel.State.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.CaptureState.cs"];
        var viewModelAudioStateText = viewModelFiles["MainViewModel.AudioState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var automationText = viewModelFiles["MainViewModel.Automation.cs"];
        var automationRecordingSettingsText = viewModelFiles["MainViewModel.AutomationRecordingSettings.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackExport.cs"];
        var flashbackPlaybackText = viewModelFiles["MainViewModel.FlashbackPlayback.cs"];
        var flashbackAutomationText = automationText + "\n" + flashbackExportText + "\n" + flashbackPlaybackText;
        var disposalText = viewModelFiles["MainViewModel.Disposal.cs"];
        var audioPropertyChangesText = viewModelFiles["MainViewModel.AudioPropertyChanges.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAudioPropertyChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioPropertyChanges.cs")
            .Replace("\r\n", "\n");
        var rawDisposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs")
            .Replace("\r\n", "\n");
        var rawAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var rawCaptureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var settingsText = viewModelFiles["MainViewModel.Settings.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackSettings.cs"];
        var rawSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Settings.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackSettings.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource();

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackBeginScrub", "_sessionCoordinator.FlashbackBeginScrub(position)");
        AssertMemberContains(flashbackPlaybackText, "FlashbackUpdateScrub", "return _sessionCoordinator.FlashbackUpdateScrub(position)");
        AssertMemberContains(flashbackPlaybackText, "FlashbackEndScrub", "_sessionCoordinator.FlashbackEndScrub()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackEndScrubAt", "_sessionCoordinator.FlashbackEndScrubAt(position)");
        AssertMemberContains(flashbackPlaybackText, "FlashbackPlay", "_sessionCoordinator.FlashbackPlay()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackPause", "_sessionCoordinator.FlashbackPause()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackGoLive", "_sessionCoordinator.FlashbackGoLive()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackNudge", "_sessionCoordinator.FlashbackNudge(delta)");
        AssertMemberContains(flashbackPlaybackText, "FlashbackSetInPoint", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackSetOutPoint", "_sessionCoordinator.FlashbackSetOutPoint()");
        AssertMemberContains(flashbackPlaybackText, "FlashbackClearInOutPoints", "=> _sessionCoordinator.FlashbackClearInOutPoints()");
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
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(rawCaptureText, "var reinitializeGeneration = Interlocked.Increment(ref _previewReinitializeGeneration);");
        AssertContains(rawCaptureText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawCaptureText, "Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(rawCaptureText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawCaptureText, "await AwaitWithTimeoutAsync(\n                    pendingCycle,\n                    FlashbackCycleBeforeReinitializeTimeoutMs,\n                    \"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawCaptureText, "catch (TimeoutException ex)\n            {\n                Logger.Log($\"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}\");");
        AssertContains(rawCaptureText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(viewModelFiles["MainViewModel.Capture.cs"], "if (ReferenceEquals(_pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)\n            {\n                _pendingFlashbackCycleTask = null;\n            }");
        AssertContains(flashbackExportText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
        AssertContains(flashbackExportText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(flashbackExportText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(flashbackExportText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(flashbackExportText, "_exportCts = null;");
        AssertContains(flashbackExportText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(flashbackExportText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(flashbackExportText, "catch (ObjectDisposedException)");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawFlashbackExportText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(flashbackExportText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawFlashbackExportText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawFlashbackExportText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawFlashbackExportText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawFlashbackExportText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertDoesNotContain(flashbackExportText, "exportCts.Dispose();");
        AssertMemberContains(flashbackExportText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(automationText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(automationText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(automationText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertMemberContains(flashbackSettingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertMemberContains(flashbackSettingsText, "OnSelectedRecordingFormatChanged", "_suppressFlashbackFormatCycle is false");
        AssertContains(rawFlashbackSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertContains(viewModelFlashbackStateText, "private bool _suppressFlashbackFormatCycle;");
        AssertMemberContains(automationRecordingSettingsText, "SetRecordingFormatAsync", "_suppressFlashbackFormatCycle = true;");
        AssertMemberContains(automationRecordingSettingsText, "SetRecordingFormatAsync", "RecordingFormat.Av1Mp4");
        AssertMemberContains(automationRecordingSettingsText, "SetRecordingFormatAsync", "await _sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertDoesNotContain(automationText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(flashbackSettingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
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
        AssertMemberContains(flashbackSettingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackSettingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackSettingsText, "OnSelectedSplitEncodeModeChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackSettingsText, "TrackFlashbackEncoderSettingsCycle", "quality: ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(flashbackSettingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(flashbackSettingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(flashbackSettingsText, "TrackFlashbackEncoderSettingsCycle", "splitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(flashbackSettingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(automationRecordingSettingsText, "SetSplitEncodeModeAsync", "_suppressFlashbackEncoderSettingsCycle = true;");
        AssertMemberContains(automationRecordingSettingsText, "SetSplitEncodeModeAsync", "SplitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(automationRecordingSettingsText, "SetSplitEncodeModeAsync", "splitEncodeMode: settings.SplitEncodeMode");
        AssertMemberContains(flashbackSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(flashbackSettingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(flashbackSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(flashbackSettingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(flashbackSettingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawFlashbackSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawFlashbackSettingsText, "CycleFlashbackEncoder({description}) canceled");
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
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackEndScrubAt",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetOutPoint",
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
            "OnSelectedPresetChanged"
        })
        {
            AssertMemberDoesNotContain(flashbackSettingsText, memberName, "_captureService");
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
