using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelRecordingTransition_UsesDependencyCompositionContext()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var controllerGraphRecordingText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Recording.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Context.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerOperationsText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Operations.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphRecordingText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphRecordingText, "private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(");
        AssertContains(controllerGraphRecordingText, "new MainViewModelRecordingTransitionController(\n                new MainViewModelRecordingTransitionControllerContext");
        AssertContains(controllerGraphRecordingText, "StartRecordingAsync = (settings, cancellationToken) =>");
        AssertContains(controllerGraphRecordingText, "viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),");
        AssertContains(controllerGraphRecordingText, "StopRecordingAsync = cancellationToken =>");
        AssertContains(controllerGraphRecordingText, "viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");

        AssertContains(recordingTransitionControllerText, "private sealed partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerContextText, "private sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertDoesNotContain(recordingTransitionControllerOperationsText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerOperationsText, "await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertDoesNotContain(recordingTransitionControllerOperationsText, "await _viewModel.InitializeDeviceAsync(cancellationToken);");

        return Task.CompletedTask;
    }
}
