using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs");
        var automationOptionsText = automationSnapshotText;
        var automationOptionsBuilderText = ReadRepoFile("Sussudio/ViewModels/AutomationOptionsSnapshotBuilder.cs");

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
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationOptionsSnapshot.cs")),
            "MainViewModel.AutomationOptionsSnapshot.cs folded into MainViewModel.AutomationSnapshots.cs");

        return Task.CompletedTask;
    }
}
