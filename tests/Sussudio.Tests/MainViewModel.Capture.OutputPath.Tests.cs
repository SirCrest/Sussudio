using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelOutputPathSelection_LivesInFocusedPartial()
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
        AssertContains(agentMapText, "`Sussudio/Controllers/Recording/Output/OutputPathActionController.cs` owns recording output-");
        AssertContains(agentMapText, "`MainViewModel.RecordingState.cs` owns recording option selections, output");
        AssertContains(cleanupPlanText, "Recording output-path browse/open-recordings button workflows now live in");
        AssertDoesNotContain(agentMapText, "`MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.OutputPathSelection.cs`");

        return Task.CompletedTask;
    }
}
