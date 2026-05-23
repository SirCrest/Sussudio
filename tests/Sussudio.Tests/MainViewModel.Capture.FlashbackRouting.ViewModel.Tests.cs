using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator()
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
        var viewModelAudioStateText = viewModelFiles["MainViewModel.AudioState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackSettings.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackExport.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackExportOperation.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackExportAutomation.cs"];
        var flashbackBufferStatusText = viewModelFlashbackStateText;
        var flashbackPlaybackCommandsText = viewModelFiles["MainViewModel.FlashbackPlaybackCommands.cs"];
        var flashbackPlaybackText = flashbackPlaybackCommandsText;
        var flashbackAutomationText = flashbackSettingsText
            + "\n" + flashbackExportText
            + "\n" + flashbackExportOperationText
            + "\n" + flashbackExportAutomationText
            + "\n" + flashbackBufferStatusText
            + "\n" + flashbackPlaybackCommandsText;
        var audioCapturePropertyChangesText = viewModelFiles["MainViewModel.AudioCapturePropertyChanges.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAudioCapturePropertyChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioCapturePropertyChanges.cs")
            .Replace("\r\n", "\n");
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackEncoderSettings.cs"];
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");
        var rawFlashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackSettings.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource();

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "StatusText = message;");
        AssertDoesNotContain(flashbackPlaybackText, "UpdateFlashbackBufferStatus");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackActionAsync", "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackBeginScrub(position ?? TimeSpan.Zero)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackSetInPoint().HasValue");
        AssertMemberDoesNotContain(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "_sessionCoordinator.FlashbackSetInPoint()");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackMarkers.cs")),
            "MainViewModel.FlashbackMarkers.cs folded into MainViewModel.FlashbackPlaybackCommands.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlaybackAutomation.cs")),
            "MainViewModel.FlashbackPlaybackAutomation.cs folded into MainViewModel.FlashbackPlaybackCommands.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlayback.cs")),
            "MainViewModel.FlashbackPlayback.cs folded into MainViewModel.FlashbackPlaybackCommands.cs");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");
        var updateFlashbackBufferStatus = ExtractMemberCode(flashbackBufferStatusText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "ClassifyCaptureFailureSource(object? sender)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, ProgramCapture)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, MicrophoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "_previewAudioGraph.RecordCaptureFault(source, ex);");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
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
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var settings = BuildCaptureSettings();");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(\n                        true,\n                        \"audio_capture_enable\",");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings)");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, \"audio_capture_disable\", teardownCapture: true)");
        AssertContains(viewModelAudioStateText, "private int _audioEnabledChangeGeneration;");
        AssertContains(viewModelAudioStateText, "private bool _suppressAudioPreviewEnabledChangeOperation;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "_suppressAudioPreviewEnabledChangeOperation = true;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=enable");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=disable");
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
