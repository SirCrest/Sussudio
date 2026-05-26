using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotText = automationFacadeText;
        var viewModelRuntimeSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
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
        AssertContains(automationFacadeText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync");
        AssertDoesNotContain(automationFacadeText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(automationFacadeText, "public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();");
        AssertContains(automationFacadeText, "public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();");
        AssertContains(automationFacadeText, "public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationFacadeText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(agentMapText, "`MainViewModel.AutomationCommands.cs` owns automation-facing view-model runtime snapshot UI-thread capture.");
        AssertContains(agentMapText, "`ViewModelBuilders.cs` owns pure view-model runtime snapshot DTO construction.");
        AssertContains(agentMapText, "also owns automation-facing source/preview probes and preview frame capture.");
        AssertContains(cleanupPlanText, "`MainViewModel.AutomationCommands.cs`; pure view-model runtime snapshot DTO");
        AssertContains(cleanupPlanText, "construction lives in `ViewModelBuilders.cs`");
        AssertContains(cleanupPlanText, "probes, and preview frame capture now live in\n   `MainViewModel.AutomationCommands.cs`");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs");
        var automationOptionsText = automationSnapshotText;
        var automationOptionsBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertDoesNotContain(automationSnapshotText, "BuildStringOptions(");
        AssertContains(automationOptionsText, "GetAutomationOptionsSnapshotAsync");
        AssertContains(automationOptionsText, "InvokeOnUiThreadAsync(() =>");
        AssertContains(automationOptionsText, "AvailableFrameRates");
        AssertContains(automationOptionsText, "FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)");
        AssertContains(automationOptionsText, "AutomationOptionsSnapshotBuilder.Build(input)");
        AssertNoRegex(
            automationOptionsText,
            @"new\s+AutomationOptionsSnapshot\s*\{",
            "MainViewModel automation options DTO construction");
        AssertContains(automationOptionsBuilderText, "internal static class AutomationOptionsSnapshotBuilder");
        AssertContains(automationOptionsBuilderText, "internal sealed class AutomationOptionsSnapshotInput");
        AssertContains(automationOptionsBuilderText, "BuildStringOptions(input.RecordingFormats, input.SelectedRecordingFormat)");
        AssertContains(automationOptionsBuilderText, "MjpegDecoderCounts = Enumerable.Range(1, 8)");
        AssertContains(automationOptionsBuilderText, "DisableReason = option.DisableReason ?? string.Empty");
        AssertContains(automationOptionsBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationOptionsSnapshot.cs")),
            "MainViewModel.AutomationOptionsSnapshot.cs folded into MainViewModel.AutomationCommands.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationSnapshots.cs")),
            "MainViewModel.AutomationSnapshots.cs folded into MainViewModel.AutomationCommands.cs");

        return Task.CompletedTask;
    }
}
