using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelPresentationControllers_UseDependencyCompositionContexts()
    {
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var controllerGraphPresentationText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Presentation.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.Context.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.Context.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphPresentationText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphPresentationText, "private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphPresentationText, "new MainViewModelPreviewLifecycleController(\n                new MainViewModelPreviewLifecycleControllerContext");
        AssertContains(controllerGraphPresentationText, "SessionCoordinator = viewModel._sessionCoordinator,");
        AssertContains(controllerGraphPresentationText, "BuildCaptureSettings = viewModel.BuildCaptureSettings,");
        AssertContains(controllerGraphPresentationText, "InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),");
        AssertContains(controllerGraphPresentationText, "RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,");
        AssertContains(controllerGraphPresentationText, "CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(");
        AssertContains(controllerGraphPresentationText, "new MainViewModelPreviewReinitializeControllerContext");
        AssertContains(controllerGraphPresentationText, "IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphPresentationText, "ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphPresentationText, "ClearPendingFlashbackCycleIfSameAndCompleted = task =>");
        AssertContains(controllerGraphPresentationText, "SelectedDevice = () => viewModel.SelectedDevice,");
        AssertContains(controllerGraphPresentationText, "SetSelectedDevice = device => viewModel.SelectedDevice = device,");
        AssertContains(controllerGraphPresentationText, "IsInitialized = () => viewModel.IsInitialized,");
        AssertContains(controllerGraphPresentationText, "SetIsInitialized = value => viewModel.IsInitialized = value,");
        AssertContains(controllerGraphPresentationText, "IsPreviewing = () => viewModel.IsPreviewing,");
        AssertContains(controllerGraphPresentationText, "SetIsPreviewing = value => viewModel.IsPreviewing = value,");
        AssertContains(controllerGraphPresentationText, "IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,");
        AssertContains(controllerGraphPresentationText, "IsRecording = () => viewModel.IsRecording,");
        AssertContains(controllerGraphPresentationText, "ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,");
        AssertContains(controllerGraphPresentationText, "IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,");
        AssertContains(controllerGraphPresentationText, "SetStatusText = value => viewModel.StatusText = value,");
        AssertContains(controllerGraphPresentationText, "RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphPresentationText, "RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphPresentationText, "ApplyLatestSourceTelemetryForPreviewStart = () =>");

        AssertContains(previewStateText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(previewStateText, "internal void CancelPendingPreviewRestart()");
        AssertContains(previewStateText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewStateText, "public Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public partial bool IsPreviewing");
        AssertContains(previewStateText, "public partial bool IsPreviewReinitializing");
        AssertContains(previewStateText, "public partial bool IsInitialized");
        AssertContains(previewStateText, "private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);");
        AssertContains(previewStateText, "private int _previewReinitializeGeneration;");
        AssertContains(previewStateText, "private bool _cancelPreviewRestartAfterReinitialize;");
        AssertContains(previewStateText, "public event EventHandler? PreviewStartRequested;");
        AssertContains(previewStateText, "public event EventHandler? PreviewStopRequested;");
        AssertContains(previewStateText, "public event Func<string, Task>? PreviewReinitRequested;");
        AssertContains(previewStateText, "public event Func<Task>? PreviewRendererStopRequested;");

        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerContextText, "private sealed class MainViewModelPreviewLifecycleControllerContext");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewLifecycleControllerContext _context;");
        AssertDoesNotContain(previewLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewLifecycleControllerText, "_viewModel.");
        AssertContains(previewLifecycleControllerContextText, "public required CaptureSessionCoordinator SessionCoordinator { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<CaptureSettings> BuildCaptureSettings { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<CaptureDevice?> SelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action<CaptureDevice> SetSelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> IsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action<bool> SetIsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> IsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action<bool> SetIsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> IsPreviewReinitializing { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> IsRecording { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> ShouldStartAudioPreview { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Func<bool> IsAudioPreviewActive { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action<string> SetStatusText { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action RaisePreviewStartRequested { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action RaisePreviewStopRequested { get; init; }");
        AssertContains(previewLifecycleControllerContextText, "public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }");
        AssertContains(previewLifecycleControllerText, "public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewLifecycleControllerText, "_previewReinitializeController = _context.CreateReinitializeController(this);");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");

        AssertContains(previewReinitializeControllerText, "private sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerContextText, "private sealed class MainViewModelPreviewReinitializeControllerContext");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");

        return Task.CompletedTask;
    }
}
