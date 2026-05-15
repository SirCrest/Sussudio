using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelOutputPathSelection_LivesInFocusedPartial()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var outputPathSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.OutputPathSelection.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(outputPathSelectionText, "public partial class MainViewModel");
        AssertContains(outputPathSelectionText, "public async Task BrowseOutputPathAsync()");
        AssertContains(outputPathSelectionText, "var picker = new FolderPicker();");
        AssertContains(outputPathSelectionText, "picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;");
        AssertContains(outputPathSelectionText, "await picker.PickSingleFolderAsync();");
        AssertContains(outputPathSelectionText, "OutputPath = folder.Path;");
        AssertContains(outputPathSelectionText, "StatusText = $\"Error selecting folder: {ex.Message}\";");
        AssertDoesNotContain(captureText, "BrowseOutputPathAsync");
        AssertDoesNotContain(captureText, "FolderPicker");
        AssertContains(agentMapText, "`MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.");
        AssertContains(cleanupPlanText, "`MainViewModel.OutputPathSelection.cs`");

        return Task.CompletedTask;
    }
}
