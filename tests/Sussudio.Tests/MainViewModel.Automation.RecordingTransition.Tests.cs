using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var automationRecordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var automationText = automationRecordingLifecycleText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationFlashback.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceAudio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationMicrophone.cs")
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
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var recordingOperationsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingOperations.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingRuntime.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var runtimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertContains(automationRecordingLifecycleText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => SetRecordingDesiredStateAsync(!IsRecording);");
        AssertContains(recordingLifecycleText, "Recording transition already in progress.");
        AssertContains(recordingLifecycleText, "await inFlight;");
        AssertContains(recordingLifecycleText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(recordingOperationsText, "private async Task StartRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingOperationsText, "private async Task StopRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingOperationsText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingOperationsText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertContains(recordingRuntimeText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(recordingRuntimeText, "private void UpdateRecordingStats()");
        AssertContains(recordingRuntimeText, "private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)");
        AssertContains(recordingRuntimeText, "RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, \"0\");");
        AssertContains(recordingRuntimeText, "RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : \"--\";");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefresh = false;");
        AssertContains(recordingRuntimeText, "RebuildResolutionOptions();");
        AssertContains(runtimeText, "UpdateRecordingStats();");
        AssertDoesNotContain(runtimeText, "private void UpdateRecordingStats()");
        AssertDoesNotContain(runtimeText, "private static double? ComputeAverageBitrate(");
        AssertDoesNotContain(runtimeText, "partial void OnIsRecordingChanged(bool value)");
        AssertDoesNotContain(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertDoesNotContain(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "return SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }
}
