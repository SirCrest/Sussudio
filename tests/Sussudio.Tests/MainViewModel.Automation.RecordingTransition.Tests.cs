using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingTransitionControllerText, "Logger.LogException(ex);");
        AssertContains(recordingTransitionControllerText, "_viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException ex)");
        AssertContains(recordingTransitionControllerText, "transitionError = ex;");
        AssertContains(recordingTransitionControllerText, "Logger.Log($\"Recording transition wait canceled: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))");
        AssertContains(recordingTransitionControllerText, "throw transitionCanceled;");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(recordingTransitionControllerText, "_viewModel.StatusText = \"Recording start canceled\";");
        AssertContains(recordingTransitionControllerText, "_viewModel.StatusText = \"Stop recording canceled\";");
        AssertContains(recordingTransitionControllerText, "_viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;");
        AssertContains(recordingTransitionControllerText, "_viewModel.StatusText = $\"Recording failed: {ex.Message}\";");
        AssertContains(recordingTransitionControllerText, "_viewModel.StatusText = $\"Stop recording failed: {ex.Message}\";");
        AssertContains(recordingTransitionControllerText, "throw;");

        return Task.CompletedTask;
    }

    private static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingLifecycleText, "internal Task StopRecordingForEmergencyAsync");
        // Fix #12: emergency stop now routes through the coordinator's emergency-flagged path
        // so LibAvRecordingSink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s).
        AssertContains(recordingLifecycleText, "=> _recordingTransitionController.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs"), "=> _viewModel._sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingLifecycleText, "if (!IsRecording)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var automationText = recordingLifecycleText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportOperation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportAutomation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlaybackCommands.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.ViewModelRuntimeSnapshot.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingRuntime.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "ViewModels",
                "MainViewModel.AutomationRecordingLifecycle.cs")),
            "MainViewModel automation recording lifecycle bridge partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        AssertContains(recordingLifecycleText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => _recordingTransitionController.ToggleRecordingAsync();");
        AssertContains(recordingLifecycleText, "=> _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(recordingTransitionControllerText, "private sealed class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerText, "Recording transition already in progress.");
        AssertContains(recordingTransitionControllerText, "await inFlight;");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerText, "private async Task StartRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private async Task StopRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "await _viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await _viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertDoesNotContain(recordingStateText, "_activeRecordingToggleTask");
        AssertDoesNotContain(recordingStateText, "_recordingToggleInProgress");
        AssertContains(recordingRuntimeText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(recordingRuntimeText, "private void UpdateRecordingStats()");
        AssertContains(recordingRuntimeText, "private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)");
        AssertContains(recordingRuntimeText, "RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, \"0\");");
        AssertContains(recordingRuntimeText, "RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : \"--\";");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefresh = false;");
        AssertContains(recordingRuntimeText, "RebuildResolutionOptions();");
        AssertContains(runtimeLifecycleControllerText, "_viewModel.UpdateRecordingStats();");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void UpdateRecordingStats()");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private static double? ComputeAverageBitrate(");
        AssertDoesNotContain(runtimeLifecycleControllerText, "partial void OnIsRecordingChanged(bool value)");
        AssertDoesNotContain(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertDoesNotContain(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }
}
