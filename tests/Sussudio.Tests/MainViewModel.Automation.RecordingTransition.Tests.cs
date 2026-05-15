using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => SetRecordingDesiredStateAsync(!IsRecording);");
        AssertContains(recordingLifecycleText, "Recording transition already in progress.");
        AssertContains(recordingLifecycleText, "await inFlight;");
        AssertContains(recordingLifecycleText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertDoesNotContain(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertDoesNotContain(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "return SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }
}
