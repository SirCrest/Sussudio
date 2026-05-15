using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationSnapshotsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ViewModelRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(viewModelRuntimeSnapshotText, "public partial class MainViewModel");
        AssertContains(viewModelRuntimeSnapshotText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(viewModelRuntimeSnapshotText, "var sessionSnapshot = _sessionCoordinator.Snapshot;");
        AssertContains(viewModelRuntimeSnapshotText, "return InvokeOnUiThreadAsync(() => new ViewModelRuntimeSnapshot");
        AssertContains(viewModelRuntimeSnapshotText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow),");
        AssertContains(viewModelRuntimeSnapshotText, "CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,");
        AssertDoesNotContain(automationSnapshotsText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync");
        AssertDoesNotContain(automationSnapshotsText, "new ViewModelRuntimeSnapshot");
        AssertContains(agentMapText, "`MainViewModel.ViewModelRuntimeSnapshot.cs` owns automation-facing view-model runtime snapshot projection.");
        AssertContains(cleanupPlanText, "`MainViewModel.ViewModelRuntimeSnapshot.cs`");

        return Task.CompletedTask;
    }
}
