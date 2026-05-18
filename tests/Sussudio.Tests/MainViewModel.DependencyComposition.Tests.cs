using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var stateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs").Replace("\r\n", "\n");
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs").Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs").Replace("\r\n", "\n");
        var dependenciesText = ReadRepoFile("Sussudio/ViewModels/MainViewModelDependencies.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "public MainViewModel()\n        : this(MainViewModelDependencies.CreateDefault())");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "_deviceService = dependencies.DeviceService;");
        AssertContains(rootText, "_captureService = dependencies.CaptureService;");
        AssertContains(rootText, "_sessionCoordinator = dependencies.SessionCoordinator;");
        AssertContains(rootText, "_deviceAudioControlService = dependencies.DeviceAudioControlService;");
        AssertContains(rootText, "_dispatcherQueue = dependencies.DispatcherQueue;");
        AssertContains(rootText, "_uiDispatchController = new MainViewModelUiDispatchController(");
        AssertContains(rootText, "DispatcherQueue = _dispatcherQueue,");
        AssertContains(rootText, "IsDisposing = () => Volatile.Read(ref _disposeState) != 0,");
        AssertContains(rootText, "_audioDeviceWatcher = dependencies.AudioDeviceWatcher;");
        AssertContains(rootText, "_recordingTransitionController = new MainViewModelRecordingTransitionController(this);");
        AssertContains(rootText, "_previewLifecycleController = new MainViewModelPreviewLifecycleController(this);");
        AssertContains(rootText, "_deviceAudioRequestController = new MainViewModelDeviceAudioRequestController(this);");
        AssertContains(rootText, "_captureSettingsAutomationController = new MainViewModelCaptureSettingsAutomationController(this);");
        AssertContains(rootText, "_recordingSettingsAutomationController = new MainViewModelRecordingSettingsAutomationController(this);");
        AssertContains(rootText, "_captureModeOptionRebuildController = new MainViewModelCaptureModeOptionRebuildController(this);");
        AssertContains(rootText, "_deviceFormatProbeController = new MainViewModelDeviceFormatProbeController(this);");
        AssertContains(rootText, "_runtimeLifecycleController = new MainViewModelRuntimeLifecycleController(this);");
        AssertContains(rootText, "_runtimeLifecycleController.Start();");
        AssertContains(rootText, "_runtimeLifecycleController.InitializePresentation();");
        AssertDoesNotContain(rootText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(rootText, "_captureService = new CaptureService();");
        AssertDoesNotContain(rootText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(rootText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(rootText, "_audioDeviceWatcher = new AudioDeviceWatcher();");
        AssertDoesNotContain(rootText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertDoesNotContain(rootText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(rootText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertDoesNotContain(rootText, "[ObservableProperty]");
        AssertContains(stateText, "public partial bool IsStatsVisible");
        AssertContains(stateText, "public partial bool IsSettingsVisible");
        AssertContains(stateText, "public partial string StatusText");
        AssertDoesNotContain(stateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(stateText, "public partial bool IsPreviewing");
        AssertDoesNotContain(stateText, "public event EventHandler? PreviewStartRequested");
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
        AssertContains(captureStateText, "public partial ObservableCollection<CaptureDevice> Devices");
        AssertContains(audioStateText, "public partial bool IsAudioPreviewActive");
        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");
        AssertDoesNotContain(audioStateText, "SelectedDeviceAudioMode");
        AssertDoesNotContain(audioStateText, "AnalogAudioGainPercent");
        AssertContains(flashbackStateText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(uiDispatchControllerText, "public required DispatcherQueue DispatcherQueue { get; init; }");
        AssertContains(uiDispatchControllerText, "public required Func<bool> IsDisposing { get; init; }");
        AssertContains(recordingTransitionControllerText, "private sealed class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerText, "public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewLifecycleControllerText, "_previewReinitializeController = new MainViewModelPreviewReinitializeController(_viewModel, this);");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(deviceAudioRequestControllerText, "private sealed class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(recordingSettingsAutomationControllerText, "private sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerText, "_viewModel._sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(captureModeOptionRebuildControllerText, "private sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionRebuildControllerText.Split('\n').Length >= 100,
            "capture mode option rebuild controller is a substantial ownership file");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void UpdateSelectedFormat()");
        AssertContains(deviceFormatProbeControllerText, "private sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceTelemetryControllerText, "private sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(runtimeLifecycleControllerText, "private sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController = new MainViewModelRuntimeEventIngressController(_viewModel);");
        AssertContains(runtimeLifecycleControllerText, "public void Start()");
        AssertContains(runtimeLifecycleControllerText, "=> _eventIngressController.Attach();");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController.Detach();");
        AssertContains(runtimeLifecycleControllerText, "public void InitializePresentation()");
        AssertContains(runtimeLifecycleControllerText, "_viewModel._latestSourceTelemetry = _viewModel._captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(runtimeLifecycleControllerText, "_viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot(_viewModel._latestSourceTelemetry, allowAutoRetarget: false);");
        AssertContains(runtimeLifecycleControllerText, "_viewModel.UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(runtimeLifecycleControllerText, "_viewModel.UpdateLiveCaptureInfo();");
        AssertContains(runtimeLifecycleControllerText, "SetupTimer();");
        AssertContains(runtimeLifecycleControllerText, "_viewModel.UpdateDiskSpace();");
        AssertContains(runtimeEventIngressControllerText, "private sealed class MainViewModelRuntimeEventIngressController");
        AssertEqual(
            true,
            runtimeEventIngressControllerText.Split('\n').Length >= 100,
            "runtime event ingress controller is a substantial ownership file");
        AssertContains(runtimeEventIngressControllerText, "public void Attach()");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._deviceService.FormatProbeCompleted += _viewModel._deviceFormatProbeController.OnDeviceFormatProbeCompleted;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.ErrorOccurred += OnCaptureError;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.PreCleanupRequested += OnCapturePreCleanupRequested;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.FrameCaptured += OnFrameCaptured;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.AudioLevelUpdated += _viewModel.OnAudioLevelUpdated;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.MicrophoneAudioLevelUpdated += _viewModel.OnMicrophoneAudioLevelUpdated;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.SourceTelemetryUpdated += _viewModel._sourceTelemetryController.OnSourceTelemetryUpdated;");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._audioDeviceWatcher.DevicesChanged += _viewModel.OnAudioDevicesChanged;");
        AssertContains(runtimeEventIngressControllerText, "public void Detach()");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._deviceService.FormatProbeCompleted -= _viewModel._deviceFormatProbeController.OnDeviceFormatProbeCompleted;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertContains(runtimeEventIngressControllerText, "_viewModel._captureService.AudioLevelUpdated -= _viewModel.OnAudioLevelUpdated;");
        AssertContains(runtimeEventIngressControllerText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");
        AssertContains(disposalText, "_runtimeLifecycleController.StopForDispose();");
        AssertDoesNotContain(disposalText, "_captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertDoesNotContain(disposalText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");

        AssertContains(dependenciesText, "internal sealed class MainViewModelDependencies");
        AssertContains(dependenciesText, "public static MainViewModelDependencies CreateDefault()");
        AssertContains(dependenciesText, "var captureService = new CaptureService();");
        AssertContains(dependenciesText, "new CaptureSessionCoordinator(captureService)");
        AssertContains(dependenciesText, "DispatcherQueue.GetForCurrentThread()");
        AssertContains(dependenciesText, "new AudioDeviceWatcher()");

        return Task.CompletedTask;
    }
}
