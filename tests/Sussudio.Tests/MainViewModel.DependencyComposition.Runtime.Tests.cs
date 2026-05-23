using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelRuntimeControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var controllerGraphRuntimeText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Runtime.cs").Replace("\r\n", "\n");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs").Replace("\r\n", "\n");
        var sourceTelemetryControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.Context.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.Context.cs").Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs").Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDisposalController.cs").Replace("\r\n", "\n");
        var disposalControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDisposalController.Context.cs").Replace("\r\n", "\n");

        AssertContains(sourceTelemetryControllerText, "namespace Sussudio.Controllers;");
        AssertContains(sourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(sourceTelemetryControllerContextText, "internal sealed class MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "private readonly MainViewModelSourceTelemetryControllerContext _context;");
        AssertDoesNotContain(sourceTelemetryControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(sourceTelemetryControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerContextText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(sourceTelemetryControllerContextText, "public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }");
        AssertContains(sourceTelemetryControllerContextText, "public required Func<string?, bool> IsAutoResolutionValue { get; init; }");
        AssertContains(sourceTelemetryControllerContextText, "public required Action RebuildResolutionOptions { get; init; }");
        AssertContains(controllerGraphText, "SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,");
        AssertContains(controllerGraphText, "BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,");
        AssertContains(controllerGraphText, "IsAutoResolutionValue = MainViewModel.IsAutoResolutionValue,");
        AssertContains(controllerGraphText, "RebuildResolutionOptions = viewModel.RebuildResolutionOptions,");
        AssertContains(controllerGraphText, "UpdateTargetSummary = viewModel.UpdateTargetSummary,");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(controllerGraphRuntimeText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphRuntimeText, "private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(");
        AssertContains(controllerGraphRuntimeText, "new MainViewModelRuntimeLifecycleController(\n                new MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(controllerGraphRuntimeText, "CreateEventIngressController = () => CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphRuntimeText, "deviceFormatProbeController,");
        AssertContains(controllerGraphRuntimeText, "sourceTelemetryController),");
        AssertContains(controllerGraphRuntimeText, "ApplySourceTelemetrySnapshot = sourceTelemetryController.ApplySourceTelemetrySnapshot,");
        AssertContains(controllerGraphRuntimeText, "RefreshSourceTelemetrySummaryAge = sourceTelemetryController.RefreshSourceTelemetrySummaryAge,");
        AssertContains(controllerGraphRuntimeText, "GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(");

        AssertContains(runtimeLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeLifecycleControllerContextText, "internal sealed class MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeLifecycleControllerContext _context;");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeLifecycleControllerText, "_viewModel.");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController = _context.CreateEventIngressController();");
        AssertContains(runtimeLifecycleControllerText, "public void Start()");
        AssertContains(runtimeLifecycleControllerText, "=> _eventIngressController.Attach();");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController.Detach();");
        AssertContains(runtimeLifecycleControllerText, "public void InitializePresentation()");
        AssertContains(runtimeLifecycleControllerText, "var latestSourceTelemetry = _context.GetLatestSourceTelemetrySnapshot();");
        AssertContains(runtimeLifecycleControllerText, "_context.SetLatestSourceTelemetrySnapshot(latestSourceTelemetry);");
        AssertContains(runtimeLifecycleControllerText, "_context.ApplySourceTelemetrySnapshot(latestSourceTelemetry, false);");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateLiveCaptureInfo();");
        AssertContains(runtimeLifecycleControllerText, "SetupTimer();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateDiskSpace();");

        AssertContains(runtimeEventIngressControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressController");
        AssertDoesNotContain(runtimeEventIngressControllerText, "partial class MainViewModelRuntimeEventIngressController");
        AssertContains(runtimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "private readonly MainViewModelRuntimeEventIngressControllerContext _context;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(controllerGraphText, "private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(runtimeEventIngressControllerText, "public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"system resume\")");
        AssertEqual(
            true,
            runtimeEventIngressControllerText.Split('\n').Length >= 100,
            "runtime event ingress controller is a substantial ownership file");
        AssertContains(runtimeEventIngressControllerText, "public void Attach()");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCaptureErrorOccurred(OnCaptureError);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachFrameCaptured(OnFrameCaptured);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(runtimeEventIngressControllerText, "_context.AttachAudioDevicesChanged(_context.OnAudioDevicesChanged);");
        AssertContains(runtimeEventIngressControllerText, "public void Detach()");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(runtimeEventIngressControllerText, "_context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");

        AssertContains(controllerGraphText, "private static MainViewModelDisposalController CreateDisposalController(");
        AssertContains(controllerGraphText, "MainViewModelDeviceAudioRequestController deviceAudioRequestController,");
        AssertContains(controllerGraphText, "MainViewModelRuntimeLifecycleController runtimeLifecycleController)");
        AssertContains(controllerGraphText, "new MainViewModelDisposalController(\n                new MainViewModelDisposalControllerContext");
        AssertContains(controllerGraphText, "TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,");
        AssertContains(controllerGraphText, "CancelPendingAudioControlWork = deviceAudioRequestController.CancelPendingAudioControlWork,");
        AssertContains(controllerGraphText, "StopRuntimeForDispose = runtimeLifecycleController.StopForDispose,");
        AssertContains(controllerGraphText, "CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),");
        AssertContains(controllerGraphText, "AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,");
        AssertContains(controllerGraphText, "public MainViewModelDisposalController DisposalController { get; }");

        AssertContains(disposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalText, "=> _disposalController.Dispose();");
        AssertContains(disposalText, "=> await _disposalController.DisposeAsync().ConfigureAwait(false);");
        AssertContains(disposalControllerText, "namespace Sussudio.Controllers;");
        AssertContains(disposalControllerText, "internal sealed class MainViewModelDisposalController");
        AssertContains(disposalControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(disposalControllerContextText, "internal sealed class MainViewModelDisposalControllerContext");
        AssertContains(disposalControllerText, "private readonly MainViewModelDisposalControllerContext _context;");
        AssertContains(disposalControllerContextText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
        AssertDoesNotContain(disposalControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(disposalControllerText, "_viewModel.");
        AssertEqual(
            true,
            disposalControllerText.Split('\n').Length >= 100,
            "view-model disposal controller is a substantial ownership file");
        AssertContains(disposalControllerText, "private const int DefaultDisposeTimeoutMs = 30000;");
        AssertContains(disposalControllerText, "private async Task DisposeCoreAsync()");
        AssertContains(disposalControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.CancelPendingAudioControlWork();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS");
        AssertDoesNotContain(disposalText, "_captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertDoesNotContain(disposalText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");

        return Task.CompletedTask;
    }
}
