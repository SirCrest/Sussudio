using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var stateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var captureHdrStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureHdrState.cs").Replace("\r\n", "\n");
        var captureSourceStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSourceState.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
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

        AssertContains(controllerGraphText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphText, "public static MainViewModelControllerGraph Create(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "var uiDispatchController = CreateUiDispatchController(viewModel);");
        AssertContains(controllerGraphText, "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);");
        AssertContains(controllerGraphText, "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "var sourceTelemetryController = CreateSourceTelemetryController(viewModel);");
        AssertContains(controllerGraphText, "public MainViewModelDisposalController DisposalController { get; }");
        AssertDoesNotContain(controllerGraphText, "RuntimeLifecycleController.Start();");
        AssertOccursBefore(
            rootText,
            "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;",
            "_runtimeLifecycleController.Start();");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(viewModel, previewLifecycleController);");

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
        AssertContains(captureStateText, "public partial ObservableCollection<CaptureDevice> Devices");
        AssertContains(captureStateText, "public partial ObservableCollection<ResolutionOption> AvailableResolutions");
        AssertContains(captureStateText, "public partial ObservableCollection<FrameRateOption> AvailableFrameRates");
        AssertDoesNotContain(captureStateText, "public partial bool IsHdrEnabled");
        AssertDoesNotContain(captureStateText, "public partial string SourceTelemetryAvailability");
        AssertContains(captureHdrStateText, "private const string HdrToggleBlockedWhileRecordingMessage");
        AssertContains(captureHdrStateText, "public partial bool IsHdrEnabled");
        AssertContains(captureHdrStateText, "public partial string HdrRuntimeState");
        AssertContains(captureSourceStateText, "private SourceSignalTelemetrySnapshot _latestSourceTelemetry");
        AssertContains(captureSourceStateText, "public partial double? DetectedSourceFrameRate");
        AssertContains(captureSourceStateText, "public partial string SourceTelemetryAvailability");
        AssertContains(audioStateText, "public partial bool IsAudioPreviewActive");
        AssertContains(flashbackStateText, "partial void OnIsFlashbackEnabledChanged(bool value)");

        AssertContains(dependenciesText, "internal sealed class MainViewModelDependencies");
        AssertContains(dependenciesText, "public static MainViewModelDependencies CreateDefault()");
        AssertContains(dependenciesText, "var captureService = new CaptureService();");
        AssertContains(dependenciesText, "new CaptureSessionCoordinator(captureService)");
        AssertContains(dependenciesText, "DispatcherQueue.GetForCurrentThread()");
        AssertContains(dependenciesText, "new AudioDeviceWatcher()");

        return Task.CompletedTask;
    }
}
