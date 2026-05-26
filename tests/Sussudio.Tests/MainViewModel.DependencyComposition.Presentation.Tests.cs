using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelPresentationControllers_UseDependencyCompositionContexts()
    {
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerText = previewLifecycleControllerText;

        AssertContains(controllerGraphText, "private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphText, "new MainViewModelPreviewLifecycleController(\n                new MainViewModelPreviewLifecycleControllerContext");
        AssertContains(controllerGraphText, "SessionCoordinator = viewModel._sessionCoordinator,");
        AssertContains(controllerGraphText, "BuildCaptureSettings = viewModel.BuildCaptureSettings,");
        AssertContains(controllerGraphText, "InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),");
        AssertContains(controllerGraphText, "RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,");
        AssertContains(controllerGraphText, "CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(");
        AssertContains(controllerGraphText, "new MainViewModelPreviewReinitializeControllerContext");
        AssertContains(controllerGraphText, "IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "PreviewReinitializeDebounceMs = PreviewReinitializeDebounceMs,");
        AssertContains(controllerGraphText, "ClearPendingFlashbackCycleIfSameAndCompleted = task =>");
        AssertContains(controllerGraphText, "FlashbackCycleBeforeReinitializeTimeoutMs = FlashbackCycleBeforeReinitializeTimeoutMs,");
        AssertContains(controllerGraphText, "AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,");
        AssertContains(controllerGraphText, "SelectedDevice = () => viewModel.SelectedDevice,");
        AssertContains(controllerGraphText, "SetSelectedDevice = device => viewModel.SelectedDevice = device,");
        AssertContains(controllerGraphText, "IsInitialized = () => viewModel.IsInitialized,");
        AssertContains(controllerGraphText, "SetIsInitialized = value => viewModel.IsInitialized = value,");
        AssertContains(controllerGraphText, "IsPreviewing = () => viewModel.IsPreviewing,");
        AssertContains(controllerGraphText, "SetIsPreviewing = value => viewModel.IsPreviewing = value,");
        AssertContains(controllerGraphText, "IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,");
        AssertContains(controllerGraphText, "IsRecording = () => viewModel.IsRecording,");
        AssertContains(controllerGraphText, "ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,");
        AssertContains(controllerGraphText, "IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,");
        AssertContains(controllerGraphText, "SetStatusText = value => viewModel.StatusText = value,");
        AssertContains(controllerGraphText, "RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphText, "RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),");
        AssertContains(controllerGraphText, "ApplyLatestSourceTelemetryForPreviewStart = () =>");

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

        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleControllerContext");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewLifecycleControllerContext _context;");
        AssertDoesNotContain(previewLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewLifecycleControllerText, "_viewModel.");
        AssertContains(previewLifecycleControllerText, "public required CaptureSessionCoordinator SessionCoordinator { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<CaptureSettings> BuildCaptureSettings { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<CaptureDevice?> SelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<CaptureDevice> SetSelectedDevice { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<bool> SetIsInitialized { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<bool> SetIsPreviewing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsPreviewReinitializing { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsRecording { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> ShouldStartAudioPreview { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsAudioPreviewActive { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action<string> SetStatusText { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action RaisePreviewStartRequested { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action RaisePreviewStopRequested { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }");
        AssertContains(previewLifecycleControllerText, "public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewLifecycleControllerText, "_previewReinitializeController = _context.CreateReinitializeController(this);");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");

        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeControllerContext");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public required int PreviewReinitializeDebounceMs { get; init; }");
        AssertContains(previewReinitializeControllerText, "public required int FlashbackCycleBeforeReinitializeTimeoutMs { get; init; }");
        AssertContains(previewReinitializeControllerText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelPreviewReinitializeController.cs")),
            "preview reinitialize transaction controller lives with preview lifecycle owner");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(previewStateText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);");
        AssertContains(previewStateText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "=> _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(mainViewModelText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(mainViewModelText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "return _context.InvokeOnUiThreadAsync(async () =>");
        AssertContains(previewLifecycleControllerText, "CancelPendingPreviewRestart();");
        AssertContains(previewLifecycleControllerText, "if (enabled == _context.IsPreviewing())");
        AssertContains(previewLifecycleControllerText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(previewLifecycleControllerText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureServiceText, "private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertContains(captureServiceText, "await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Probes.cs")),
            "CaptureService probe partial folded into snapshots");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel preview automation partial");

        return Task.CompletedTask;
    }
}
