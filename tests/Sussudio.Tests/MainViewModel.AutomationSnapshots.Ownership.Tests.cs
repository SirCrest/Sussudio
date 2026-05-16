using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationSnapshotsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ViewModelRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelRuntimeSnapshotBuilder.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(viewModelRuntimeSnapshotText, "public partial class MainViewModel");
        AssertContains(viewModelRuntimeSnapshotText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(viewModelRuntimeSnapshotText, "var sessionSnapshot = _sessionCoordinator.Snapshot;");
        AssertContains(viewModelRuntimeSnapshotText, "return InvokeOnUiThreadAsync(() =>");
        AssertContains(viewModelRuntimeSnapshotText, "var input = new ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotText, "return ViewModelRuntimeSnapshotBuilder.Build(input);");
        AssertDoesNotContain(viewModelRuntimeSnapshotText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal static class ViewModelRuntimeSnapshotBuilder");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal sealed class ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(input.SourceTelemetryTimestampUtc, input.TimestampUtc),");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCommand = sessionSnapshot.LastCommand?.ToString() ?? \"None\",");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCorrelationId = sessionSnapshot.LastCorrelationId ?? string.Empty,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0,");
        AssertDoesNotContain(automationSnapshotsText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync");
        AssertDoesNotContain(automationSnapshotsText, "new ViewModelRuntimeSnapshot");
        AssertContains(agentMapText, "`MainViewModel.ViewModelRuntimeSnapshot.cs` owns automation-facing view-model runtime snapshot UI-thread capture.");
        AssertContains(agentMapText, "`ViewModelRuntimeSnapshotBuilder.cs` owns pure view-model runtime snapshot DTO construction.");
        AssertContains(cleanupPlanText, "`MainViewModel.ViewModelRuntimeSnapshot.cs`; pure view-model runtime snapshot DTO");
        AssertContains(cleanupPlanText, "construction lives in `ViewModelRuntimeSnapshotBuilder.cs`");

        return Task.CompletedTask;
    }
}
