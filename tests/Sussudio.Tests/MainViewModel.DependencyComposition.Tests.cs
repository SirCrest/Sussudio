using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Composition.cs").Replace("\r\n", "\n");
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

        AssertContains(rootText, "public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel");
        AssertContains(rootText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertDoesNotContain(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertDoesNotContain(rootText, "private readonly DeviceService _deviceService;");
        AssertContains(compositionText, "public MainViewModel()\n        : this(MainViewModelDependencies.CreateDefault())");
        AssertContains(compositionText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(compositionText, "private readonly DeviceService _deviceService;");
        AssertContains(compositionText, "_deviceService = dependencies.DeviceService;");
        AssertContains(compositionText, "_captureService = dependencies.CaptureService;");
        AssertContains(compositionText, "_sessionCoordinator = dependencies.SessionCoordinator;");
        AssertContains(compositionText, "_audioRampTraceRecorder = CreateAudioRampTraceRecorder();");
        AssertContains(compositionText, "_previewAudioVolumeTransitionController = CreatePreviewAudioVolumeTransitionController();");
        AssertContains(compositionText, "_deviceAudioControlService = dependencies.DeviceAudioControlService;");
        AssertContains(compositionText, "_dispatcherQueue = dependencies.DispatcherQueue;");
        AssertContains(compositionText, "_audioDeviceWatcher = dependencies.AudioDeviceWatcher;");
        AssertContains(compositionText, "var controllerGraph = MainViewModelControllerGraph.Create(this);");
        AssertContains(compositionText, "_uiDispatchController = controllerGraph.UiDispatchController;");
        AssertContains(compositionText, "_recordingTransitionController = controllerGraph.RecordingTransitionController;");
        AssertContains(compositionText, "_previewLifecycleController = controllerGraph.PreviewLifecycleController;");
        AssertContains(compositionText, "_deviceAudioRequestController = controllerGraph.DeviceAudioRequestController;");
        AssertContains(compositionText, "_recordingCapabilityController = controllerGraph.RecordingCapabilityController;");
        AssertContains(compositionText, "_captureSettingsAutomationController = controllerGraph.CaptureSettingsAutomationController;");
        AssertContains(compositionText, "_recordingSettingsAutomationController = controllerGraph.RecordingSettingsAutomationController;");
        AssertContains(compositionText, "_captureModeOptionRebuildController = controllerGraph.CaptureModeOptionRebuildController;");
        AssertDoesNotContain(rootText, "_resolutionOptionRebuildController");
        AssertDoesNotContain(compositionText, "_resolutionOptionRebuildController");
        AssertDoesNotContain(compositionText, "new MainViewModelResolutionOptionRebuildController");
        AssertContains(compositionText, "_deviceFormatProbeController = controllerGraph.DeviceFormatProbeController;");
        AssertContains(compositionText, "_sourceTelemetryController = controllerGraph.SourceTelemetryController;");
        AssertContains(compositionText, "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;");
        AssertContains(compositionText, "_disposalController = controllerGraph.DisposalController;");
        AssertContains(compositionText, "_runtimeLifecycleController.Start();");
        AssertContains(compositionText, "_runtimeLifecycleController.InitializePresentation();");
        AssertDoesNotContain(compositionText, "new MainViewModelUiDispatchController(");
        AssertDoesNotContain(compositionText, "new MainViewModelRecordingTransitionController(this)");
        AssertDoesNotContain(compositionText, "new MainViewModelRuntimeLifecycleController(this)");
        AssertDoesNotContain(compositionText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(compositionText, "_captureService = new CaptureService();");
        AssertDoesNotContain(compositionText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(compositionText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(compositionText, "_audioDeviceWatcher = new AudioDeviceWatcher();");
        AssertDoesNotContain(compositionText, "new AudioRampTraceRecorderContext");
        AssertDoesNotContain(compositionText, "new PreviewAudioVolumeTransitionControllerContext");
        AssertDoesNotContain(compositionText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertDoesNotContain(compositionText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(compositionText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
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
            compositionText,
            "_runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;",
            "_runtimeLifecycleController.Start();");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(");

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
