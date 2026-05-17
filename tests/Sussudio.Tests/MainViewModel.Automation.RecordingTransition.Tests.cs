using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var automationText = recordingLifecycleText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationFlashback.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudioInputSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportOperation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportAutomation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackSegments.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlaybackCommands.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.ViewModelRuntimeSnapshot.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
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
