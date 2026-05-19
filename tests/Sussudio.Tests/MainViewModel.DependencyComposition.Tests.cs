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
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs").Replace("\r\n", "\n");
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
        AssertContains(controllerGraphText, "new MainViewModelPreviewLifecycleController(\n                new MainViewModelPreviewLifecycleControllerContext");
        AssertContains(controllerGraphText, "SessionCoordinator = viewModel._sessionCoordinator,");
        AssertContains(controllerGraphText, "BuildCaptureSettings = viewModel.BuildCaptureSettings,");
        AssertContains(controllerGraphText, "InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),");
        AssertContains(controllerGraphText, "RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,");
        AssertContains(controllerGraphText, "CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(");
        AssertContains(controllerGraphText, "new MainViewModelPreviewReinitializeControllerContext");
        AssertContains(controllerGraphText, "IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),");
        AssertContains(controllerGraphText, "ClearPendingFlashbackCycleIfSameAndCompleted = task =>");
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
        AssertContains(controllerGraphText, "private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(");
        AssertContains(controllerGraphText, "new MainViewModelRecordingTransitionController(\n                new MainViewModelRecordingTransitionControllerContext");
        AssertContains(controllerGraphText, "StartRecordingAsync = (settings, cancellationToken) =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),");
        AssertContains(controllerGraphText, "StopRecordingAsync = cancellationToken =>");
        AssertContains(controllerGraphText, "viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),");
        AssertContains(controllerGraphText, "var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);");
        AssertContains(controllerGraphText, "var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);");
        AssertContains(controllerGraphText, "var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "new MainViewModelCaptureModeOptionRebuildController(viewModel)");
        AssertContains(controllerGraphText, "var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);");
        AssertContains(controllerGraphText, "var sourceTelemetryController = CreateSourceTelemetryController(viewModel);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeLifecycleController(\n                new MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(controllerGraphText, "CreateEventIngressController = () => CreateRuntimeEventIngressController(viewModel, previewLifecycleController),");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "private static MainViewModelDisposalController CreateDisposalController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDisposalController(\n                new MainViewModelDisposalControllerContext");
        AssertContains(controllerGraphText, "TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,");
        AssertContains(controllerGraphText, "CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),");
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
        AssertContains(recordingTransitionControllerText, "private sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertDoesNotContain(recordingTransitionControllerOperationsText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(recordingTransitionControllerText, "public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerOperationsText, "await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertDoesNotContain(recordingTransitionControllerOperationsText, "await _viewModel.InitializeDeviceAsync(cancellationToken);");
        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewLifecycleControllerText, "private sealed class MainViewModelPreviewLifecycleControllerContext");
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
        AssertContains(previewReinitializeControllerText, "private sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "private sealed class MainViewModelPreviewReinitializeControllerContext");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerText, "private sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(");
        AssertContains(controllerGraphText, "new MainViewModelDeviceRefreshControllerContext");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");
        AssertContains(controllerGraphText, "BeginBackgroundFormatProbe = (device, scanGeneration) =>");
        AssertContains(deviceAudioRequestControllerText, "private sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "private sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertDoesNotContain(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "private sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerGainText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertDoesNotContain(deviceAudioRequestControllerGainText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(controllerGraphText, "ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(controllerGraphText, "ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "private sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(controllerGraphText, "private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(controllerGraphText, "SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,");
        AssertContains(controllerGraphText, "ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,");
        AssertContains(recordingSettingsAutomationControllerText, "private sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerText, "private sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertContains(recordingSettingsAutomationControllerText, "_context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingCapabilityControllerText, "private sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "private sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingCapabilityControllerContext");
        AssertContains(controllerGraphText, "ReplaceAvailableRecordingFormats = formats =>");
        AssertContains(controllerGraphText, "NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),");
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
        AssertContains(deviceFormatProbeControllerText, "private sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(deviceFormatProbeRetargetApplierText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(deviceFormatProbeRetargetApplierText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(deviceFormatProbeRetargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "_viewModel.");
        AssertEqual(
            true,
            deviceFormatProbeRetargetApplierText.Split('\n').Length >= 100,
            "device format probe retarget applier is a substantial ownership file");
        AssertContains(deviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeRetargetApplierContext");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceTelemetryControllerText, "private sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerText, "private sealed class MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "private readonly MainViewModelSourceTelemetryControllerContext _context;");
        AssertDoesNotContain(sourceTelemetryControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(sourceTelemetryControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelSourceTelemetryControllerContext");
        AssertContains(controllerGraphText, "SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,");
        AssertContains(controllerGraphText, "RebuildResolutionOptions = viewModel.RebuildResolutionOptions,");
        AssertContains(controllerGraphText, "UpdateTargetSummary = viewModel.UpdateTargetSummary,");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(runtimeLifecycleControllerText, "private sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerText, "private sealed class MainViewModelRuntimeLifecycleControllerContext");
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
        AssertContains(runtimeEventIngressControllerText, "private sealed class MainViewModelRuntimeEventIngressController");
        AssertContains(runtimeEventIngressControllerText, "private sealed class MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(runtimeEventIngressControllerText, "private readonly MainViewModelRuntimeEventIngressControllerContext _context;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(runtimeEventIngressControllerText, "_viewModel.");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertContains(runtimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(controllerGraphText, "private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeEventIngressControllerContext");
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
        AssertContains(disposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalText, "=> _disposalController.Dispose();");
        AssertContains(disposalText, "=> await _disposalController.DisposeAsync().ConfigureAwait(false);");
        AssertContains(disposalControllerText, "private sealed class MainViewModelDisposalController");
        AssertContains(disposalControllerText, "private sealed class MainViewModelDisposalControllerContext");
        AssertContains(disposalControllerText, "private readonly MainViewModelDisposalControllerContext _context;");
        AssertDoesNotContain(disposalControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(disposalControllerText, "_viewModel.");
        AssertEqual(
            true,
            disposalControllerText.Split('\n').Length >= 100,
            "view-model disposal controller is a substantial ownership file");
        AssertContains(disposalControllerText, "private const int DefaultDisposeTimeoutMs = 30000;");
        AssertContains(disposalControllerText, "private async Task DisposeCoreAsync()");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.CancelPendingAudioControlWork();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
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
