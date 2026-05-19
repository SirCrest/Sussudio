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
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs").Replace("\r\n", "\n");
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs").Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs").Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDisposalController.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs").Replace("\r\n", "\n");
        var recordingTransitionControllerOperationsText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Operations.cs").Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs").Replace("\r\n", "\n");
        var previewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerGainText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Gain.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var captureModeOptionFrameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.FrameRate.cs").Replace("\r\n", "\n");
        var captureModeOptionResolutionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Resolution.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs").Replace("\r\n", "\n");
        var dependenciesText = ReadRepoFile("Sussudio/ViewModels/MainViewModelDependencies.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "public MainViewModel()\n        : this(MainViewModelDependencies.CreateDefault())");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "_deviceService = dependencies.DeviceService;");
        AssertContains(rootText, "_captureService = dependencies.CaptureService;");
        AssertContains(rootText, "_sessionCoordinator = dependencies.SessionCoordinator;");
        AssertContains(rootText, "_audioRampTraceRecorder = CreateAudioRampTraceRecorder();");
        AssertContains(rootText, "_previewAudioVolumeTransitionController = CreatePreviewAudioVolumeTransitionController();");
        AssertContains(rootText, "_deviceAudioControlService = dependencies.DeviceAudioControlService;");
        AssertContains(rootText, "_dispatcherQueue = dependencies.DispatcherQueue;");
        AssertContains(rootText, "_audioDeviceWatcher = dependencies.AudioDeviceWatcher;");
        AssertContains(rootText, "var controllerGraph = MainViewModelControllerGraph.Create(this);");
        AssertContains(rootText, "_uiDispatchController = controllerGraph.UiDispatchController;");
        AssertContains(rootText, "_recordingTransitionController = controllerGraph.RecordingTransitionController;");
        AssertContains(rootText, "_previewLifecycleController = controllerGraph.PreviewLifecycleController;");
        AssertContains(rootText, "_deviceAudioRequestController = controllerGraph.DeviceAudioRequestController;");
        AssertContains(rootText, "_recordingCapabilityController = controllerGraph.RecordingCapabilityController;");
        AssertContains(rootText, "_captureSettingsAutomationController = controllerGraph.CaptureSettingsAutomationController;");
        AssertContains(rootText, "_recordingSettingsAutomationController = controllerGraph.RecordingSettingsAutomationController;");
        AssertContains(rootText, "_captureModeOptionRebuildController = controllerGraph.CaptureModeOptionRebuildController;");
        AssertDoesNotContain(rootText, "_resolutionOptionRebuildController");
        AssertDoesNotContain(rootText, "new MainViewModelResolutionOptionRebuildController");
        AssertContains(rootText, "_deviceFormatProbeController = controllerGraph.DeviceFormatProbeController;");
        AssertContains(rootText, "_sourceTelemetryController = controllerGraph.SourceTelemetryController;");
        AssertContains(rootText, "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;");
        AssertContains(rootText, "_disposalController = controllerGraph.DisposalController;");
        AssertContains(rootText, "_runtimeLifecycleController.Start();");
        AssertContains(rootText, "_runtimeLifecycleController.InitializePresentation();");
        AssertDoesNotContain(rootText, "new MainViewModelUiDispatchController(");
        AssertDoesNotContain(rootText, "new MainViewModelRecordingTransitionController(this)");
        AssertDoesNotContain(rootText, "new MainViewModelRuntimeLifecycleController(this)");
        AssertDoesNotContain(rootText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(rootText, "_captureService = new CaptureService();");
        AssertDoesNotContain(rootText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(rootText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(rootText, "_audioDeviceWatcher = new AudioDeviceWatcher();");
        AssertDoesNotContain(rootText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(rootText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertDoesNotContain(rootText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertDoesNotContain(rootText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(rootText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertDoesNotContain(rootText, "[ObservableProperty]");
        AssertContains(controllerGraphText, "private sealed class MainViewModelControllerGraph");
        AssertContains(controllerGraphText, "public static MainViewModelControllerGraph Create(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "DispatcherQueue = viewModel._dispatcherQueue,");
        AssertContains(controllerGraphText, "IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,");
        AssertContains(controllerGraphText, "SetStatusText = value => viewModel.StatusText = value,");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphText, "new MainViewModelPreviewLifecycleController(\n                viewModel,\n                new MainViewModelPreviewLifecycleControllerContext");
        AssertContains(controllerGraphText, "SessionCoordinator = viewModel._sessionCoordinator,");
        AssertContains(controllerGraphText, "BuildCaptureSettings = viewModel.BuildCaptureSettings,");
        AssertContains(controllerGraphText, "InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),");
        AssertContains(controllerGraphText, "RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,");
        AssertContains(controllerGraphText, "IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,");
        AssertContains(controllerGraphText, "ApplyLatestSourceTelemetryForPreviewStart = () =>");
        AssertContains(controllerGraphText, "new MainViewModelRecordingTransitionController(viewModel, previewLifecycleController)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceAudioRequestController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingCapabilityController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureSettingsAutomationController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsAutomationController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureModeOptionRebuildController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelSourceTelemetryController(viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceRefreshController(viewModel, previewLifecycleController)");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = new MainViewModelRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var deviceRefreshController = new MainViewModelDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeLifecycleController(viewModel, previewLifecycleController)");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = new MainViewModelRuntimeLifecycleController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "new MainViewModelDisposalController(viewModel)");
        AssertContains(controllerGraphText, "public MainViewModelDisposalController DisposalController { get; }");
        AssertDoesNotContain(controllerGraphText, "RuntimeLifecycleController.Start();");
        AssertOccursBefore(
            rootText,
            "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;",
            "_runtimeLifecycleController.Start();");
        AssertContains(stateText, "public partial bool IsStatsVisible");
        AssertContains(stateText, "public partial bool IsSettingsVisible");
        AssertContains(stateText, "public partial string StatusText");
        AssertContains(stateText, "private IntPtr _windowHandle;");
        AssertContains(stateText, "public void SetWindowHandle(IntPtr handle)");
        AssertContains(stateText, "_windowHandle = handle;");
        AssertDoesNotContain(rootText, "public void SetWindowHandle(IntPtr handle)");
        AssertContains(stateText, "private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)");
        AssertDoesNotContain(rootText, "private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)");
        AssertDoesNotContain(stateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(stateText, "public partial bool IsPreviewing");
        AssertDoesNotContain(stateText, "public event EventHandler? PreviewStartRequested");
        AssertDoesNotContain(rootText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "private Task ReinitializeDeviceAsync(string reason)");
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
        AssertContains(recordingTransitionControllerText, "private sealed partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerOperationsText, "await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertDoesNotContain(recordingTransitionControllerOperationsText, "await _viewModel.InitializeDeviceAsync(cancellationToken);");
        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleControllerContext");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewLifecycleControllerContext _context;");
        AssertContains(previewLifecycleControllerText, "public required CaptureSessionCoordinator SessionCoordinator { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<CaptureSettings> BuildCaptureSettings { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Func<bool> IsAudioPreviewActive { get; init; }");
        AssertContains(previewLifecycleControllerText, "public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }");
        AssertContains(previewLifecycleControllerText, "public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(previewLifecycleControllerText, "_previewReinitializeController = new MainViewModelPreviewReinitializeController(_viewModel, this);");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(deviceAudioRequestControllerText, "private sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertDoesNotContain(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "private sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerGainText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
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
        AssertContains(captureModeOptionRebuildControllerText, "private sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionFrameRateRebuildControllerText, "private sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionFrameRateRebuildControllerText.Split('\n').Length >= 100,
            "capture mode option frame-rate rebuild partial is a substantial ownership file");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionFrameRateRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void UpdateSelectedFormat()");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "private sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionResolutionRebuildControllerText.Split('\n').Length >= 100,
            "capture mode option resolution rebuild partial is a substantial ownership file");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");
        AssertContains(deviceFormatProbeControllerText, "private sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = new MainViewModelDeviceFormatProbeRetargetApplier(_viewModel);");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(deviceFormatProbeRetargetApplierText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertEqual(
            true,
            deviceFormatProbeRetargetApplierText.Split('\n').Length >= 100,
            "device format probe retarget applier is a substantial ownership file");
        AssertContains(deviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceTelemetryControllerText, "private sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(runtimeLifecycleControllerText, "private sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerText, "_eventIngressController = new MainViewModelRuntimeEventIngressController(_viewModel, previewLifecycleController);");
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
        AssertContains(runtimeEventIngressControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(runtimeEventIngressControllerText, "_previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));");
        AssertContains(runtimeEventIngressControllerText, "_previewLifecycleController.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertContains(runtimeEventIngressControllerText, "_previewLifecycleController.ReinitializeDeviceAsync(\"system resume\")");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"system resume\")");
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
        AssertContains(disposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalText, "=> _disposalController.Dispose();");
        AssertContains(disposalText, "=> await _disposalController.DisposeAsync().ConfigureAwait(false);");
        AssertContains(disposalControllerText, "private sealed class MainViewModelDisposalController");
        AssertEqual(
            true,
            disposalControllerText.Split('\n').Length >= 100,
            "view-model disposal controller is a substantial ownership file");
        AssertContains(disposalControllerText, "private const int DefaultDisposeTimeoutMs = 30000;");
        AssertContains(disposalControllerText, "private async Task DisposeCoreAsync()");
        AssertContains(disposalControllerText, "_viewModel.CancelActiveFlashbackExportForDispose();");
        AssertContains(disposalControllerText, "_viewModel._deviceAudioRequestController.CancelPendingAudioControlWork();");
        AssertContains(disposalControllerText, "_viewModel._runtimeLifecycleController.StopForDispose();");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS");
        AssertContains(disposalControllerText, "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS");
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
