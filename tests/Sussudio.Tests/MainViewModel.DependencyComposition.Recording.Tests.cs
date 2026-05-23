using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelRecordingTransition_UsesDependencyCompositionContext()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphText, "private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(");
        AssertContains(controllerGraphText, "new MainViewModelRecordingTransitionController(\n                new MainViewModelRecordingTransitionControllerContext");
        AssertContains(controllerGraphText, "StartRecordingAsync = (settings, cancellationToken) =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),");
        AssertContains(controllerGraphText, "StopRecordingAsync = cancellationToken =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");

        AssertContains(recordingTransitionControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingTransitionControllerText, "internal sealed class MainViewModelRecordingTransitionController");
        AssertDoesNotContain(recordingTransitionControllerText, "partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerText, "internal sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertDoesNotContain(recordingTransitionControllerText, "await _viewModel.InitializeDeviceAsync(cancellationToken);");

        return Task.CompletedTask;
    }
}
