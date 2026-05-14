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

    private static Task MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost()
    {
        var flashbackWindowText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackScrubText = ReadRepoFile("Sussudio/MainWindow.FlashbackScrub.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var fullScreenWindowText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerText = (
            ReadRepoFile("Sussudio/Controllers/FullScreenController.cs")
            + "\n" + ReadRepoFile("Sussudio/Controllers/FullScreenController.Transitions.cs"))
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(xamlText, "PointerReleased=\"FlashbackScrubArea_PointerReleased\"");
        AssertContains(xamlText, "PointerCanceled=\"FlashbackScrubArea_PointerCanceled\"");
        AssertContains(xamlText, "PointerCaptureLost=\"FlashbackScrubArea_PointerCaptureLost\"");
        AssertContains(flashbackScrubText, "Flashback pointer scrub interaction");
        AssertContains(flashbackScrubText, "private bool _isFlashbackScrubbing;");
        AssertContains(flashbackScrubText, "private TimeSpan? _lastScrubPointerPosition;");
        AssertContains(flashbackScrubText, "private long _lastScrubUpdateTick;");
        AssertContains(flashbackScrubText, "private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)");
        AssertContains(flashbackScrubText, "if (!ViewModel.FlashbackBeginScrub(targetPosition))\n        {\n            _lastScrubPointerPosition = null;\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub begin\", \"FLASHBACK_UI_SCRUB_BEGIN_REJECTED\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "if (!ViewModel.FlashbackUpdateScrub(targetPosition))\n        {\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub update\", \"FLASHBACK_UI_SCRUB_UPDATE_REJECTED\");\n            EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"update_rejected\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)");
        AssertContains(flashbackScrubText, "TimeSpan? releasePosition = null;\n        if (_isFlashbackScrubbing)");
        AssertContains(flashbackScrubText, "var targetPosition = ComputeFlashbackScrubPosition(e);\n            releasePosition = targetPosition;\n            _lastScrubPointerPosition = targetPosition;\n            if (!ViewModel.FlashbackUpdateScrub(targetPosition))");
        AssertContains(flashbackScrubText, "ViewModel.ReportFlashbackPlaybackRejection(\"scrub release update\", \"FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED\");");
        AssertContains(flashbackScrubText, "EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"released\", releasePosition);");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"set in point\", \"FLASHBACK_UI_SET_IN_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"set out point\", \"FLASHBACK_UI_SET_OUT_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"clear in/out\", \"FLASHBACK_UI_CLEAR_INOUT_REJECTED\")");
        AssertContains(flashbackWindowText, "Logger.Log($\"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackWindowText, "Logger.Log($\"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_CLEAR_INOUT\");");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"pause\", \"FLASHBACK_UI_PAUSE_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"play\", \"FLASHBACK_UI_PLAY_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"go live\", \"FLASHBACK_UI_GOLIVE_REJECTED\")");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_PAUSE\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_PLAY\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_GOLIVE\");");
        AssertContains(flashbackScrubText, "_isFlashbackScrubbing = true;\n        _lastScrubPointerPosition = targetPosition;\n        _lastScrubUpdateTick = 0;\n        (sender as UIElement)?.CapturePointer(e.Pointer);");
        AssertContains(flashbackScrubText, "var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;");
        AssertContains(flashbackScrubText, "var ended = releasePosition.HasValue\n            ? ViewModel.FlashbackEndScrubAt(releasePosition.Value)\n            : ViewModel.FlashbackEndScrub();\n        if (!ended)\n        {\n            ViewModel.ReportFlashbackPlaybackRejection($\"scrub end ({reason})\", $\"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}\");\n        }");
        AssertContains(flashbackScrubText, "_isFlashbackScrubbing = false;\n        _lastScrubUpdateTick = 0;\n        _lastScrubPointerPosition = null;\n        element?.ReleasePointerCapture(pointer);");
        AssertContains(flashbackScrubText, "FLASHBACK_UI_SCRUB_END");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCanceled");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCaptureLost");
        AssertContains(flashbackScrubText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return;");
        AssertContains(flashbackScrubText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return TimeSpan.Zero;");
        AssertContains(flashbackScrubText, "private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)");
        AssertContains(flashbackScrubText, "if (!IsUsableFlashbackTrackDimension(width) || !double.IsFinite(x))");
        AssertContains(flashbackScrubText, "private static bool IsUsableFlashbackTrackDimension(double value)\n        => double.IsFinite(value) && value > 0;");
        AssertContains(flashbackScrubText, "private static bool IsUsableFlashbackDuration(TimeSpan value)\n        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;");
        AssertContains(fullScreenWindowText, "if (ViewModel.IsFlashbackEnabled && FlashbackTimelinePanel.Visibility == Visibility.Visible)");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertContains(fullScreenControllerText, "var timelineVisibleAtExit = _context.ShouldShowFlashbackTimeline();");
        AssertContains(fullScreenWindowText, "private bool ShouldShowFlashbackTimeline()");
        AssertContains(fullScreenWindowText, "return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;");
        AssertContains(fullScreenWindowText, "var carriedPosition = _lastScrubPointerPosition;\n        Logger.Log($\"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}\");");
        AssertContains(fullScreenWindowText, "var ended = carriedPosition.HasValue\n            ? ViewModel?.FlashbackEndScrubAt(carriedPosition.Value) ?? false\n            : ViewModel?.FlashbackEndScrub() ?? false;\n        if (!ended)");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"scrub end (fullscreen_enter)\", \"FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter\")");
        AssertDoesNotContain(fullScreenWindowText, "var carriedPosition = ViewModel?.FlashbackPlaybackPosition;");
        AssertDoesNotContain(flashbackScrubText, "var carriedPosition = _isFlashbackScrubbing ? ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");
        AssertDoesNotContain(flashbackWindowText, "private void FlashbackScrubArea_PointerPressed(");
        AssertDoesNotContain(mainWindowText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(mainWindowText, "private TimeSpan? _lastScrubPointerPosition;");

        return Task.CompletedTask;
    }

    private static Task MainWindowFlashbackToggle_RollsBackUiStateOnFailure()
    {
        var flashbackWindowText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineText = ReadRepoFile("Sussudio/MainWindow.FlashbackTimeline.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineControllerText = ReadRepoFile("Sussudio/Controllers/FlashbackTimelineController.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs")
            .Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
        AssertContains(mainWindowText, "InitializeFlashbackTimelineController();");
        AssertContains(viewModelText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(viewModelText, "IsFlashbackTimelineVisible = false;");
        AssertContains(bindingsText, "FlashbackEnabledToggle.IsOn = ViewModel.IsFlashbackEnabled;");
        AssertContains(bindingsText, "ApplyFlashbackTimelineLockout();");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsFlashbackEnabled):\n                HandleFlashbackEnabledChanged();");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):\n                HandleFlashbackTimelineVisibleChanged();");
        AssertContains(flashbackPropertyChangedText, "ApplyFlashbackTimelineLockout();");
        AssertContains(flashbackPropertyChangedText, "ApplyFlashbackTimelineVisibility(ViewModel.IsFlashbackTimelineVisible);");
        AssertContains(flashbackTimelineText, "private FlashbackTimelineController _flashbackTimelineController = null!;");
        AssertContains(flashbackTimelineText, "FlashbackToggle = FlashbackToggle,");
        AssertContains(flashbackTimelineText, "FlashbackTimelinePanel = FlashbackTimelinePanel,");
        AssertContains(flashbackTimelineText, "SnapPlayheadOnNextOpen = () => _snapFlashbackPlayheadOnNextUpdate = true,");
        AssertContains(flashbackTimelineText, "ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.OnToggleChecked();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ApplyLockout();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ResetAnimationForFullScreen();");
        AssertContains(flashbackTimelineText, "_isFlashbackScrubbing = false;");
        AssertContains(flashbackTimelineControllerText, "internal sealed class FlashbackTimelineController");
        AssertContains(flashbackTimelineControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(flashbackTimelineControllerText, "private bool _suppressToggle;");
        AssertContains(flashbackTimelineControllerText, "if (!_context.ViewModel.IsFlashbackEnabled)\n        {\n            ApplyLockout();\n            return;\n        }");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = true;");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = false;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackToggle.IsEnabled = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "SyncToggle(isVisible: false);");
        AssertContains(flashbackTimelineControllerText, "_context.ClearScrubInteraction();");
        AssertContains(flashbackTimelineControllerText, "CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "_timelineStoryboard?.Stop();");
        AssertContains(flashbackWindowText, "if (_suppressFlashbackEnabledToggle)");
        AssertContains(flashbackWindowText, "var requestedEnabled = FlashbackEnabledToggle.IsOn;");
        AssertContains(flashbackWindowText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");
        AssertContains(flashbackWindowText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(flashbackWindowText, "var previousEnabled = ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackWindowText, "ViewModel.IsFlashbackEnabled = requestedEnabled;");
        AssertContains(flashbackWindowText, "ViewModel.IsFlashbackEnabled = previousEnabled;");
        AssertContains(flashbackWindowText, "_suppressFlashbackEnabledToggle = true;");
        AssertContains(flashbackWindowText, "FlashbackEnabledToggle.IsOn = previousEnabled;");
        AssertContains(flashbackWindowText, "_suppressFlashbackEnabledToggle = false;");

        return Task.CompletedTask;
    }

}
