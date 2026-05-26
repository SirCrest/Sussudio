using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var dependenciesText = compositionText;

        AssertContains(rootText, "public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel");
        AssertContains(rootText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "private readonly DeviceService _deviceService;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Composition.cs")),
            "MainViewModel.Composition.cs folded into MainViewModel.cs");
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

        AssertContains(controllerGraphText, "private sealed class MainViewModelControllerGraph");
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

        AssertContains(rootText, "public partial bool IsStatsVisible");
        AssertContains(rootText, "public partial bool IsSettingsVisible");
        AssertContains(rootText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(rootText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationUi.cs")),
            "MainViewModel.AutomationUi.cs folded into MainViewModel.cs");
        AssertContains(rootText, "public partial string StatusText");
        AssertContains(rootText, "private IntPtr _windowHandle;");
        AssertContains(rootText, "public void SetWindowHandle(IntPtr handle)");
        AssertContains(rootText, "_windowHandle = handle;");
        AssertContains(rootText, "private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)");
        AssertDoesNotContain(rootText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.State.cs")),
            "MainViewModel.State.cs folded into MainViewModel.cs");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertContains(rootText, "public partial bool IsPreviewing");
        AssertContains(rootText, "public event EventHandler? PreviewStartRequested");
        AssertContains(rootText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewState.cs")),
            "MainViewModel.PreviewState.cs folded into MainViewModel.cs");
        AssertContains(captureStateText, "public partial ObservableCollection<CaptureDevice> Devices");
        AssertContains(captureStateText, "public partial ObservableCollection<ResolutionOption> AvailableResolutions");
        AssertContains(captureStateText, "public partial ObservableCollection<FrameRateOption> AvailableFrameRates");
        AssertContains(captureStateText, "private const string HdrToggleBlockedWhileRecordingMessage");
        AssertContains(captureStateText, "public partial bool IsHdrEnabled");
        AssertContains(captureStateText, "public partial string HdrRuntimeState");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureHdrState.cs")),
            "MainViewModel.CaptureHdrState.cs folded into MainViewModel.CaptureState.cs");
        AssertContains(captureStateText, "private SourceSignalTelemetrySnapshot _latestSourceTelemetry");
        AssertContains(captureStateText, "public partial double? DetectedSourceFrameRate");
        AssertContains(captureStateText, "public partial string SourceTelemetryAvailability");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureSourceState.cs")),
            "MainViewModel.CaptureSourceState.cs folded into MainViewModel.CaptureState.cs");
        AssertContains(audioStateText, "public partial bool IsAudioPreviewActive");
        AssertContains(audioStateText, "private AudioRampTraceRecorder CreateAudioRampTraceRecorder()");
        AssertContains(audioStateText, "public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioRampTrace.cs")),
            "MainViewModel.AudioRampTrace.cs folded into MainViewModel.AudioState.cs");
        AssertContains(flashbackStateText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(flashbackStateText, "public void UpdateFlashbackBufferStatus()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");

        AssertContains(dependenciesText, "internal sealed class MainViewModelDependencies");
        AssertContains(dependenciesText, "public static MainViewModelDependencies CreateDefault()");
        AssertContains(dependenciesText, "var captureService = new CaptureService();");
        AssertContains(dependenciesText, "new CaptureSessionCoordinator(captureService)");
        AssertContains(dependenciesText, "DispatcherQueue.GetForCurrentThread()");
        AssertContains(dependenciesText, "new AudioDeviceWatcher()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModelDependencies.cs")),
            "MainViewModelDependencies.cs folded into MainViewModel.cs");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelUiDispatchController_UsesDependencyCompositionContext()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphText, "private sealed class MainViewModelControllerGraph");
        AssertContains(controllerGraphText, "private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "DispatcherQueue = viewModel._dispatcherQueue,");
        AssertContains(controllerGraphText, "IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,");
        AssertContains(controllerGraphText, "SetStatusText = value => viewModel.StatusText = value,");

        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchController");
        AssertContains(uiDispatchControllerText, "private readonly MainViewModelUiDispatchControllerContext _context;");
        AssertDoesNotContain(uiDispatchControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(uiDispatchControllerText, "_viewModel.");
        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(uiDispatchControllerText, "public required DispatcherQueue DispatcherQueue { get; init; }");
        AssertContains(uiDispatchControllerText, "public required Func<bool> IsDisposing { get; init; }");

        return Task.CompletedTask;
    }

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
