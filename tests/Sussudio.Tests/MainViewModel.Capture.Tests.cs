using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        // Coordinator must expose the audio monitoring API surface
        var setPreviewVolume = coordinatorType.GetMethod(
            "SetPreviewVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(setPreviewVolume, "CaptureSessionCoordinator.SetPreviewVolume");

        var updateAudioMonitoring = coordinatorType.GetMethod(
            "UpdateAudioMonitoringAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioMonitoring, "CaptureSessionCoordinator.UpdateAudioMonitoringAsync");

        var updateAudioInput = coordinatorType.GetMethod(
            "UpdateAudioInputAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioInput, "CaptureSessionCoordinator.UpdateAudioInputAsync");

        var startVideoPreview = coordinatorType.GetMethod(
            "StartVideoPreviewAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(startVideoPreview, "CaptureSessionCoordinator.StartVideoPreviewAsync");

        // The command kind enum must include UpdateAudioMonitoring
        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        AssertEqual(true,
            Enum.IsDefined(commandKindType, Enum.Parse(commandKindType, "SetAudioPreviewEnabled")),
            "AutomationCommandKind.SetAudioPreviewEnabled exists");

        return Task.CompletedTask;
    }

    private static Task CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        var rangeExport = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackRangeAsync",
            "    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertContains(rangeExport, "FlashbackExporter? flashbackExporter;");
        AssertContains(rangeExport, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(rangeExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(rangeExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(rangeExport, "snapshotExporter: flashbackExporter,");

        var lastNExport = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync",
            "    private void ReleaseFlashbackBackendLeaseIfHeld");
        AssertContains(lastNExport, "FlashbackExporter? flashbackExporter;");
        AssertContains(lastNExport, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(lastNExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(lastNExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(lastNExport, "snapshotExporter: flashbackExporter,");

        var exportCore = ExtractTextBetween(
            captureServiceText,
            "private async Task<FinalizeResult> ExportFlashbackCoreAsync",
            "    private static IReadOnlyList<FlashbackExportSegment>?");
        AssertContains(exportCore, "FlashbackExporter? snapshotExporter = null,");
        AssertOccursBefore(exportCore, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(exportCore, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackExporter ??= new FlashbackExporter();\n            }");
        AssertContains(exportCore, "var forceRotateFallbackUsed = false;");
        AssertContains(exportCore, "forceRotateFallbackUsed = true;");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (forceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "RecordLastFlashbackExportResult(exportId, result);\n            CompleteFlashbackExportDiagnostics(exportId, result);");

        return Task.CompletedTask;
    }

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
        var automationText = viewModelFiles["MainViewModel.Automation.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n");
        var rawCaptureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var settingsText = viewModelFiles["MainViewModel.Settings.cs"];
        var rawSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Settings.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(automationText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(automationText, "FlashbackBeginScrub", "_sessionCoordinator.FlashbackBeginScrub(position)");
        AssertMemberContains(automationText, "FlashbackUpdateScrub", "return _sessionCoordinator.FlashbackUpdateScrub(position)");
        AssertMemberContains(automationText, "FlashbackEndScrub", "_sessionCoordinator.FlashbackEndScrub()");
        AssertMemberContains(automationText, "FlashbackEndScrubAt", "_sessionCoordinator.FlashbackEndScrubAt(position)");
        AssertMemberContains(automationText, "FlashbackPlay", "_sessionCoordinator.FlashbackPlay()");
        AssertMemberContains(automationText, "FlashbackPause", "_sessionCoordinator.FlashbackPause()");
        AssertMemberContains(automationText, "FlashbackGoLive", "_sessionCoordinator.FlashbackGoLive()");
        AssertMemberContains(automationText, "FlashbackNudge", "_sessionCoordinator.FlashbackNudge(delta)");
        AssertMemberContains(automationText, "FlashbackSetInPoint", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(automationText, "FlashbackSetOutPoint", "_sessionCoordinator.FlashbackSetOutPoint()");
        AssertMemberContains(automationText, "FlashbackClearInOutPoints", "=> _sessionCoordinator.FlashbackClearInOutPoints()");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(automationText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        var updateFlashbackBufferStatus = ExtractMemberCode(automationText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(automationText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "ReferenceEquals(sender, _wasapiAudioCapture)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, _microphoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "Volatile.Write(ref _wasapiAudioCaptureFaultMessage, $\"{source}: {ex.Message}\");");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertMemberContains(automationText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(automationText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawAutomationText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawAutomationText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawAutomationText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawAutomationText, "Flashback export unavailable: flashback is not active.");
        AssertMemberContains(automationText, "ExportFlashbackAsync", "if (!isCurrent) return;");
        AssertMemberContains(automationText, "SaveFlashbackLast5mAsync", "if (!isCurrent) return;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private int _flashbackExportOperationId;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(viewModelFiles["MainViewModel.cs"], "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(viewModelFiles["MainViewModel.cs"], "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawViewModelText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private int _previewReinitializeGeneration;");
        AssertContains(rawCaptureText, "var reinitializeGeneration = Interlocked.Increment(ref _previewReinitializeGeneration);");
        AssertContains(rawCaptureText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawCaptureText, "Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(rawCaptureText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawCaptureText, "await AwaitWithTimeoutAsync(\n                    pendingCycle,\n                    FlashbackCycleBeforeReinitializeTimeoutMs,\n                    \"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawCaptureText, "catch (TimeoutException ex)\n            {\n                Logger.Log($\"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}\");");
        AssertContains(rawCaptureText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(viewModelFiles["MainViewModel.Capture.cs"], "if (ReferenceEquals(_pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)\n            {\n                _pendingFlashbackCycleTask = null;\n            }");
        AssertContains(automationText, "private async Task<(FinalizeResult? Result, string? ErrorMessage, bool IsCurrent)> ExportFlashbackCoreAsync");
        AssertContains(automationText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(automationText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(automationText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(automationText, "_exportCts = null;");
        AssertContains(automationText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(automationText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(automationText, "catch (ObjectDisposedException)");
        AssertContains(rawAutomationText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(ct)");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(automationText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawAutomationText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(automationText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawAutomationText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertDoesNotContain(automationText, "exportCts.Dispose();");
        AssertMemberContains(automationText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(automationText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(automationText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(automationText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertMemberContains(settingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertContains(rawSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertMemberContains(settingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(settingsText, "OnFlashbackBufferMinutesChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(settingsText, "OnFlashbackGpuDecodeChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(settingsText, "OnFlashbackBufferMinutesChanged", "Interlocked.Increment(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(settingsText, "OnFlashbackBufferMinutesChanged", "RestartFlashbackAfterSettingsUpdateAsync(updateTask, restartGeneration)");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "restartGeneration != Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "InvokeOnUiThreadAsync(");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "IsPreviewing && !IsRecording && _isLoadingSettings is false");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "shouldRestart is false");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "await RestartFlashbackAsync().ConfigureAwait(false)");
        AssertMemberContains(settingsText, "RestartFlashbackAfterSettingsUpdateAsync", "catch (OperationCanceledException ex)");
        AssertContains(rawSettingsText, "RestartFlashbackAfterSettingsUpdate canceled");
        AssertMemberContains(settingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(settingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(settingsText, "TrackFlashbackEncoderSettingsCycle", "quality: ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(settingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(settingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(settingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(settingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(settingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(settingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(settingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(settingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawSettingsText, "CycleFlashbackEncoder({description}) canceled");
        AssertMemberContains(viewModelFiles["MainViewModel.cs"], "OnIsAudioEnabledChanged", "_sessionCoordinator.RestartFlashbackAsync(BuildCaptureSettings())");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private int _audioEnabledChangeGeneration;");
        AssertMemberContains(viewModelFiles["MainViewModel.cs"], "OnIsAudioEnabledChanged", "var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);");
        AssertMemberContains(viewModelFiles["MainViewModel.cs"], "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled");
        AssertMemberContains(viewModelFiles["MainViewModel.cs"], "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled");
        AssertMemberContains(rawViewModelText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=enable");
        AssertMemberContains(rawViewModelText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=disable");
        AssertContains(viewModelFiles["MainViewModel.cs"], "private int _flashbackSettingsRestartGeneration;");

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
            AssertMemberDoesNotContain(automationText, memberName, "_captureService");
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
            AssertMemberDoesNotContain(settingsText, memberName, "_captureService");
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
        var fullScreenWindowText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(xamlText, "PointerReleased=\"FlashbackScrubArea_PointerReleased\"");
        AssertContains(xamlText, "PointerCanceled=\"FlashbackScrubArea_PointerCanceled\"");
        AssertContains(xamlText, "PointerCaptureLost=\"FlashbackScrubArea_PointerCaptureLost\"");
        AssertContains(flashbackWindowText, "private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)");
        AssertContains(flashbackWindowText, "if (!ViewModel.FlashbackBeginScrub(targetPosition))\n        {\n            _lastScrubPointerPosition = null;\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub begin\", \"FLASHBACK_UI_SCRUB_BEGIN_REJECTED\");\n            return;\n        }");
        AssertContains(flashbackWindowText, "if (!ViewModel.FlashbackUpdateScrub(targetPosition))\n        {\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub update\", \"FLASHBACK_UI_SCRUB_UPDATE_REJECTED\");\n            EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"update_rejected\");\n            return;\n        }");
        AssertContains(flashbackWindowText, "private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)");
        AssertContains(flashbackWindowText, "TimeSpan? releasePosition = null;\n        if (_isFlashbackScrubbing)");
        AssertContains(flashbackWindowText, "var targetPosition = ComputeFlashbackScrubPosition(e);\n            releasePosition = targetPosition;\n            _lastScrubPointerPosition = targetPosition;\n            if (!ViewModel.FlashbackUpdateScrub(targetPosition))");
        AssertContains(flashbackWindowText, "ViewModel.ReportFlashbackPlaybackRejection(\"scrub release update\", \"FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED\");");
        AssertContains(flashbackWindowText, "EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"released\", releasePosition);");
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
        AssertContains(flashbackWindowText, "_isFlashbackScrubbing = true;\n        _lastScrubPointerPosition = targetPosition;\n        _lastScrubUpdateTick = 0;\n        (sender as UIElement)?.CapturePointer(e.Pointer);");
        AssertContains(flashbackWindowText, "var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;");
        AssertContains(flashbackWindowText, "var ended = releasePosition.HasValue\n            ? ViewModel.FlashbackEndScrubAt(releasePosition.Value)\n            : ViewModel.FlashbackEndScrub();\n        if (!ended)\n        {\n            ViewModel.ReportFlashbackPlaybackRejection($\"scrub end ({reason})\", $\"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}\");\n        }");
        AssertContains(flashbackWindowText, "_isFlashbackScrubbing = false;\n        _lastScrubUpdateTick = 0;\n        _lastScrubPointerPosition = null;\n        element?.ReleasePointerCapture(pointer);");
        AssertContains(flashbackWindowText, "FLASHBACK_UI_SCRUB_END");
        AssertContains(flashbackWindowText, "FlashbackScrubArea_PointerCanceled");
        AssertContains(flashbackWindowText, "FlashbackScrubArea_PointerCaptureLost");
        AssertContains(flashbackWindowText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return;");
        AssertContains(flashbackWindowText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return TimeSpan.Zero;");
        AssertContains(flashbackWindowText, "private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)");
        AssertContains(flashbackWindowText, "if (!IsUsableFlashbackTrackDimension(width) || !double.IsFinite(x))");
        AssertContains(flashbackWindowText, "private static bool IsUsableFlashbackTrackDimension(double value)\n        => double.IsFinite(value) && value > 0;");
        AssertContains(flashbackWindowText, "private static bool IsUsableFlashbackDuration(TimeSpan value)\n        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertContains(fullScreenWindowText, "var carriedPosition = _lastScrubPointerPosition;\n            Logger.Log($\"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}\");");
        AssertContains(fullScreenWindowText, "var ended = carriedPosition.HasValue\n                ? ViewModel?.FlashbackEndScrubAt(carriedPosition.Value) ?? false\n                : ViewModel?.FlashbackEndScrub() ?? false;\n            if (!ended)");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"scrub end (fullscreen_enter)\", \"FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter\")");
        AssertDoesNotContain(fullScreenWindowText, "var carriedPosition = ViewModel?.FlashbackPlaybackPosition;");
        AssertDoesNotContain(flashbackWindowText, "var carriedPosition = _isFlashbackScrubbing ? ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");

        return Task.CompletedTask;
    }

    private static Task MainWindowFlashbackToggle_RollsBackUiStateOnFailure()
    {
        var flashbackWindowText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
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

    private static Task CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange()
    {
        var captureServiceText = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Services/Capture/CaptureService.cs");
        var captureServiceRawText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
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
        var previewMicMonitorStart = ExtractTextBetween(
            startVideoPreviewRaw,
            "// Start mic monitoring if enabled",
            "// Start flashback AFTER");
        AssertContains(previewMicMonitorStart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(previewMicMonitorStart, "catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)");
        AssertContains(previewMicMonitorStart, "MIC_MONITOR_PREVIEW_START_DISPOSE_WARN");
        AssertContains(previewMicMonitorStart, "_microphoneCapture = micCapture;\n                        micCapture = null;");

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
            "public Task CleanupAsync");
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
            captureServiceRawText,
            "public Task UpdateMicrophoneMonitorAsync",
            "private void OnWasapiCaptureFailed");
        AssertContains(updateMicrophoneMonitor, "if (_isRecording)");
        AssertContains(updateMicrophoneMonitor, "MIC_MONITOR_UPDATE_DEFERRED recording=true");
        AssertOccursBefore(
            updateMicrophoneMonitor,
            "MIC_MONITOR_UPDATE_DEFERRED recording=true",
            "await DisposeMicrophoneCaptureAsync()");
        var updateAudioInputRaw = ExtractTextBetween(
            captureServiceRawText,
            "public Task UpdateAudioInputAsync",
            "public Task CleanupAsync");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN");
        AssertContains(updateAudioInputRaw, "AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");

        AssertContains(captureServiceText, "_flashbackBackendSettings = CloneCaptureSettings(settings)");
        AssertContains(captureServiceText, "_flashbackBackendSettings = CloneCaptureSettings(_currentSettings)");
        AssertContains(captureServiceText, "_flashbackBackendSettings = null");
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
            "    public Task ToggleRecordingAsync()");
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

    private static Task CaptureService_FlashbackLifecycleLogs_UseOutcomeNames()
    {
        var flashbackTexts = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("FLASHBACK_", StringComparison.Ordinal))
            .Select(path => File.ReadAllText(path).Replace("\r\n", "\n"))
            .ToArray();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var flashbackText = string.Join("\n", flashbackTexts);

        AssertNoRegex(
            flashbackText,
            @"""FLASHBACK_[^""]*_(BEGIN|DONE|END)\b",
            "Flashback lifecycle scaffold log tokens");

        foreach (var expectedToken in new[]
        {
            "FLASHBACK_RESTART_OK",
            "FLASHBACK_FORMAT_CHANGE_OK",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_OK",
            "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK",
            "FLASHBACK_RECORDING_EXPORT_OK",
            "FLASHBACK_RECORDING_EXPORT_FAIL",
            "FLASHBACK_UNIFIED_RECORDING_STOP_OK",
            "FLASHBACK_UNIFIED_RECORDING_STOP_FAIL",
            "FLASHBACK_PREVIEW_INIT_OK",
            "FLASHBACK_PREVIEW_INIT_CANCELLED",
            "FLASHBACK_PREVIEW_DISPOSE_OK",
            "FLASHBACK_BUFFER_CYCLE_OK",
            "FLASHBACK_RECORDING_ACTIVE",
            "FLASHBACK_RECORDING_READY",
            "FLASHBACK_EXPORT_OK",
            "FLASHBACK_EXPORT_SEGMENT_OK",
            "FLASHBACK_EXPORT_SEGMENTS_OK",
            "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN",
            "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN",
            "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED",
            "FLASHBACK_PLAYBACK_DISPOSE_REQUEST"
        })
        {
            AssertContains(flashbackText, expectedToken);
        }

        var encoderSettingsChange = ExtractTextBetween(
            captureServiceText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public void SetPreviewVolume");
        AssertContains(encoderSettingsChange, "var cycleFailed = false;");
        AssertContains(encoderSettingsChange, "cycleFailed = true;");
        AssertContains(encoderSettingsChange, "if (!cycleFailed)");
        AssertContains(encoderSettingsChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(encoderSettingsChange, "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED");
        AssertContains(
            encoderSettingsChange,
            "FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_FAIL quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} type={ex.GetType().Name} error='{ex.Message}'");

        var formatChange = ExtractTextBetween(
            captureServiceText,
            "public Task UpdateRecordingFormatAsync",
            "/// <summary>\n    /// Cycles the flashback encoder");
        AssertContains(formatChange, "var cycleFailed = false;");
        AssertContains(formatChange, "cycleFailed = true;");
        AssertContains(formatChange, "if (!cycleFailed)");
        AssertContains(formatChange, "catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED");
        AssertContains(formatChange, "FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");

        var cycleBuffer = ExtractTextBetween(
            captureServiceText,
            "private async Task CycleFlashbackBufferAsync",
            "    private void OnFlashbackFrameEncoded");
        AssertContains(cycleBuffer, "var preservedInPoint = !effectivePurgeSegments ? oldPlaybackController?.InPoint : null;");
        AssertContains(cycleBuffer, "var preservedOutPoint = !effectivePurgeSegments ? oldPlaybackController?.OutPoint : null;");
        AssertDoesNotContain(cycleBuffer, "var preservedInPoint = oldPlaybackController?.InPoint;");
        AssertDoesNotContain(cycleBuffer, "var preservedOutPoint = oldPlaybackController?.OutPoint;");
        AssertContains(cycleBuffer, "playbackController.InPoint = preservedInPoint;");
        AssertContains(cycleBuffer, "playbackController.OutPoint = preservedOutPoint;");
        var ensureFlashbackPreviewBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task EnsureFlashbackPreviewBackendAsync",
            "private async Task DisposeFlashbackPreviewBackendAsync");
        var createFlashbackSessionContext = ExtractTextBetween(
            captureServiceText,
            "private FlashbackSessionContext CreateFlashbackSessionContext",
            "    private async Task EnsureFlashbackPreviewBackendAsync");
        AssertContains(createFlashbackSessionContext, "var forceTransportStreamFlashback = UseTransportStreamFlashbackCodec(unifiedVideoCapture, settings, frameRate)");
        AssertContains(createFlashbackSessionContext, "var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);");
        AssertContains(createFlashbackSessionContext, "frameRate = frameRateParts.EffectiveFrameRate;");
        AssertContains(createFlashbackSessionContext, "FrameRateNumerator = fpsNum");
        AssertContains(captureServiceText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT");
        AssertContains(captureServiceText, "FLASHBACK_FRAME_RATE_RATIONAL_REJECT");
        AssertContains(captureServiceText, "deltaFps > toleranceFps");
        AssertContains(createFlashbackSessionContext, "? \"hevc_nvenc\"");
        AssertContains(captureServiceText, "private static bool UseTransportStreamFlashbackCodec(");
        AssertContains(captureServiceText, "settings.Format == RecordingFormat.Av1Mp4");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackExportVerificationFormat(");
        AssertContains(captureServiceText, "forceRotateResult.Status == FlashbackForceRotateStatus.Failed");
        AssertContains(captureServiceText, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertDoesNotContain(
            ExtractTextBetween(
                captureServiceText,
                "if (segmentPaths.Count == 0)",
                "// Fallback: single-file export if no segments available"),
            "force_rotate_failed");
        AssertContains(captureServiceText, "? RecordingFormat.HevcMp4.ToString()");
        AssertContains(createFlashbackSessionContext, "var flashbackNvencPreset = unifiedVideoCapture.IsSoftwareMjpegPipelineActive && frameRate >= 100");
        AssertContains(createFlashbackSessionContext, "? \"Fast\"");
        AssertContains(createFlashbackSessionContext, "NvencPreset = flashbackNvencPreset");
        // Silent codec/preset substitutions are surfaced via a one-shot log line and the
        // automation snapshot. The log line must include enough context to reproduce.
        AssertContains(createFlashbackSessionContext, "var downgradeReason = ResolveFlashbackCodecDowngradeReason(settings, unifiedVideoCapture);");
        AssertContains(createFlashbackSessionContext, "FLASHBACK_CODEC_DOWNGRADE requested_format={settings.Format}");
        AssertContains(createFlashbackSessionContext, "FLASHBACK_CODEC_DOWNGRADE_CLEARED");
        AssertContains(captureServiceText, "private static string? ResolveFlashbackCodecDowngradeReason(");
        AssertContains(captureServiceText, "AV1->HEVC: software MJPEG pipeline at");
        AssertContains(captureServiceText, "NVENC preset '");
        // Snapshot field must be populated from the resolver so downstream consumers
        // (verifier, automation, UI) all observe the same downgrade state.
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotsText, "FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(requestedSettings, unifiedVideoCapture),");
        var contractsText = ReadRepoFile("Sussudio/Models/AutomationContracts.cs")
            .Replace("\r\n", "\n");
        AssertContains(contractsText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        AssertContains(ensureFlashbackPreviewBackend, "var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_CANCELLED");
        AssertContains(ensureFlashbackPreviewBackend, "FLASHBACK_PREVIEW_INIT_FAIL");
        AssertContains(cycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN");
        AssertContains(cycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN");
        AssertContains(cycleBuffer, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(cycleBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(cycleBuffer, "await oldSink.StopAsync(committedCycleToken)");
        AssertContains(cycleBuffer, "await newSink.StartAsync(\n                CreateFlashbackSessionContext(unifiedVideoCapture, _currentSettings),\n                committedCycleToken,");
        AssertContains(cycleBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertOccursBefore(
            cycleBuffer,
            "await oldSink.DisposeAsync().ConfigureAwait(false);",
            "_flashbackSink = null;");

        return Task.CompletedTask;
    }

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
        AssertFlashbackFrameRateParts(mismatchedResult, null, null, 120.0, "source NTSC rejected for integer USB cadence");

        var missingResult = method.Invoke(null, new[] { BuildFrameRateSettings(null, null), 120.0 })!;
        AssertFlashbackFrameRateParts(missingResult, null, null, 120.0, "missing rational falls back to delivered cadence");

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
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var setFlashbackEnabled = ExtractTextBetween(
            captureServiceText,
            "public Task SetFlashbackEnabledAsync",
            "/// <summary>\n    /// Updates flashback-specific fields");
        var stopAndDisposeRecordingBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private void TryApplySharedPreviewDevice");

        AssertContains(setFlashbackEnabled, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(setFlashbackEnabled, "if (_flashbackEnabled == enabled)");
        AssertContains(setFlashbackEnabled, "if (enabled && (_flashbackSink != null || _isRecording))");
        AssertContains(
            setFlashbackEnabled,
            "if (!enabled &&\n                    _flashbackSink == null &&\n                    _flashbackBufferManager == null &&\n                    _flashbackExporter == null &&\n                    _flashbackPlaybackController == null)");
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

    private static Dictionary<string, string> ReadMainViewModelCodeFiles()
    {
        return Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels"), "MainViewModel*.cs")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                path => Path.GetFileName(path),
                path => StripCSharpCommentsAndStringContents(File.ReadAllText(path).Replace("\r\n", "\n")),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadRepoCodeWithoutCommentsOrStrings(string relativePath)
        => StripCSharpCommentsAndStringContents(ReadRepoFile(relativePath).Replace("\r\n", "\n"));

    private static void AssertMemberContains(string source, string memberName, string token)
        => AssertContains(ExtractMemberCode(source, memberName), token);

    private static void AssertMemberDoesNotContain(string source, string memberName, string token)
        => AssertDoesNotContain(ExtractMemberCode(source, memberName), token);

    private static string ExtractMemberCode(string source, string memberName)
    {
        var match = Regex.Match(
            source,
            @"(?m)^\s*(?:(?:public|private|protected|internal|static|async|partial|override|virtual|sealed)\s+)*(?:[\w<>,\?\[\]\.]+\s+)+" +
            Regex.Escape(memberName) +
            @"\s*\(");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Member '{memberName}' was not found.");
        }

        var openBrace = source.IndexOf('{', match.Index);
        var arrow = source.IndexOf("=>", match.Index, StringComparison.Ordinal);
        var semicolon = source.IndexOf(';', match.Index);
        if (arrow >= 0 && semicolon >= 0 && (openBrace < 0 || arrow < openBrace))
        {
            return source.Substring(match.Index, semicolon - match.Index + 1);
        }

        if (openBrace < 0)
        {
            throw new InvalidOperationException($"Member '{memberName}' has no body.");
        }

        var closeBrace = FindMatchingBrace(source, openBrace);
        return source.Substring(match.Index, closeBrace - match.Index + 1);
    }

    private static string ExtractTextBetween(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Start token '{startToken}' was not found.");
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"End token '{endToken}' was not found after '{startToken}'.");
        }

        return source.Substring(start, end - start);
    }

    private static int FindMatchingBrace(string source, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        throw new InvalidOperationException("Matching brace was not found.");
    }

    private static void AssertRegex(string value, string pattern, string fieldName)
    {
        if (!Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: pattern '{pattern}' was not found.");
        }
    }

    private static void AssertNoRegex(string value, string pattern, string fieldName)
    {
        if (Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: forbidden pattern '{pattern}' was found.");
        }
    }

    private static void AssertOccursBefore(string value, string earlierToken, string laterToken)
    {
        var earlier = value.IndexOf(earlierToken, StringComparison.Ordinal);
        var later = value.IndexOf(laterToken, StringComparison.Ordinal);
        if (earlier < 0 || later < 0 || earlier >= later)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{earlierToken}' to occur before '{laterToken}'.");
        }
    }

    private static string StripCSharpCommentsAndStringContents(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (current == '/' && next == '/')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length && source[i] != '\n')
                {
                    builder.Append(' ');
                    i++;
                }
                if (i < source.Length)
                {
                    builder.Append('\n');
                }
                continue;
            }

            if (current == '/' && next == '*')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length)
                {
                    if (source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/')
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i++;
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '"')
            {
                var verbatim = i > 0 && source[i - 1] == '@';
                builder.Append('"');
                i++;
                while (i < source.Length)
                {
                    if (!verbatim && source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '"')
                    {
                        builder.Append('"');
                        if (verbatim && i + 1 < source.Length && source[i + 1] == '"')
                        {
                            builder.Append('"');
                            i += 2;
                            continue;
                        }
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '\'')
            {
                builder.Append('\'');
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '\'')
                    {
                        builder.Append('\'');
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
