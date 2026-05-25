using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelOutputPathSelection_LivesInFocusedPartial()
    {
        var mainViewModelFiles = ReadMainViewModelCodeFiles();
        var mainViewModelText = string.Join("\n", mainViewModelFiles.Values);
        var outputPathSelectionPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "ViewModels",
            "MainViewModel.OutputPathSelection.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertEqual(false, File.Exists(outputPathSelectionPath), "MainViewModel output picker partial retired");
        AssertDoesNotContain(mainViewModelText, "BrowseOutputPathAsync");
        AssertDoesNotContain(mainViewModelText, "FolderPicker");
        AssertDoesNotContain(mainViewModelText, "FileTypeFilter");
        AssertContains(agentMapText, "`Sussudio/Controllers/Recording/Output/OutputPathController.cs` owns recording output-");
        AssertContains(agentMapText, "`MainViewModel.RecordingState.cs` owns the stable recording facade:");
        AssertContains(agentMapText, "bridge, recording option selections, output path, counters, and observable");
        AssertContains(cleanupPlanText, "Recording output-path textbox, tooltip, resize-event updates, browse, and");
        AssertDoesNotContain(agentMapText, "`MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.OutputPathSelection.cs`");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty()
    {
        var builderType = RequireType("Sussudio.ViewModels.OutputDriveSpacePresentationBuilder");
        var buildMethod = builderType.GetMethod(
            "Build",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OutputDriveSpacePresentationBuilder.Build was not found.");

        AssertEqual(
            "",
            buildMethod.Invoke(null, new object?[] { "\0" }),
            "Output drive space invalid path fallback");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_LivesInFocusedHelper()
    {
        var bridgeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(bridgeText, "private void UpdateDiskSpace()");
        AssertContains(bridgeText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertDoesNotContain(bridgeText, "new DriveInfo(");
        AssertDoesNotContain(bridgeText, "Path.GetPathRoot(");
        AssertDoesNotContain(bridgeText, "Trace.TraceWarning(");
        AssertDoesNotContain(bridgeText, "Free: {freeGb:F1} GB");

        AssertContains(builderText, "internal static class OutputDriveSpacePresentationBuilder");
        AssertContains(builderText, "internal static string Build(string outputPath)");
        AssertContains(builderText, "new DriveInfo(Path.GetPathRoot(outputPath) ?? \"C:\");");
        AssertContains(builderText, "return $\"Free: {freeGb:F1} GB\";");
        AssertContains(builderText, "Trace.TraceWarning($\"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}\");");
        AssertContains(builderText, "return \"\";");
        AssertDoesNotContain(builderText, "DiskSpaceInfo =");

        AssertContains(agentMapText, "`MainViewModel.RecordingState.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge");
        AssertContains(agentMapText, "`Sussudio/ViewModels/ViewModelBuilders.cs` owns output drive probing");
        AssertContains(cleanupPlanText, "`ViewModelBuilders.cs`");

        return Task.CompletedTask;
    }
}
