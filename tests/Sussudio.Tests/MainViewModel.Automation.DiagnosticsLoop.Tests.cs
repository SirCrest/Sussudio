using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs");
        var automationOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationOptionsSnapshot.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertDoesNotContain(automationSnapshotText, "GetAutomationOptionsSnapshotAsync");
        AssertDoesNotContain(automationSnapshotText, "BuildStringOptions(");
        AssertContains(automationOptionsText, "GetAutomationOptionsSnapshotAsync");
        AssertContains(automationOptionsText, "BuildStringOptions(");
        AssertContains(automationOptionsText, "RecordingFormats = BuildStringOptions(AvailableRecordingFormats, SelectedRecordingFormat)");
        AssertContains(automationOptionsText, "MjpegDecoderCounts = Enumerable.Range(1, 8)");
        AssertContains(automationOptionsText, "PreviewVolumePercent = PreviewVolume * 100.0");

        return Task.CompletedTask;
    }
}
