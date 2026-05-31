using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureState.cs")),
            "MainViewModel.CaptureState.cs folded into MainViewModel.cs");
        AssertContains(captureStateText, "public partial ObservableCollection<CaptureDevice> Devices");
        AssertContains(captureStateText, "public partial ObservableCollection<ResolutionOption> AvailableResolutions");
        AssertContains(captureStateText, "public partial ObservableCollection<FrameRateOption> AvailableFrameRates");
        AssertContains(captureStateText, "private const string HdrToggleBlockedWhileRecordingMessage");
        AssertContains(captureStateText, "public partial bool IsHdrEnabled");
        AssertContains(captureStateText, "public partial string HdrRuntimeState");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureHdrState.cs")),
            "MainViewModel.CaptureHdrState.cs folded into MainViewModel.cs");
        AssertContains(captureStateText, "private SourceSignalTelemetrySnapshot _latestSourceTelemetry");
        AssertContains(captureStateText, "public partial double? DetectedSourceFrameRate");
        AssertContains(captureStateText, "public partial string SourceTelemetryAvailability");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureSourceState.cs")),
            "MainViewModel.CaptureSourceState.cs folded into MainViewModel.cs");
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
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs").Replace("\r\n", "\n");

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

internal static Task MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = captureSettingsAutomationControllerText;
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var frameRateTimingResolverText = captureModeOptionRebuildControllerText;
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = deviceFormatProbeControllerText;

        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");
        AssertDoesNotContain(audioStateText, "SelectedDeviceAudioMode");
        AssertDoesNotContain(audioStateText, "AnalogAudioGainPercent");

        AssertContains(controllerGraphText, "var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);");
        AssertContains(controllerGraphText, "var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);");
        AssertContains(controllerGraphText, "var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");

        AssertContains(controllerGraphText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),");
        AssertContains(controllerGraphText, "viewModel._frameRateTimingResolver);");
        AssertContains(controllerGraphText, "private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureModeOptionRebuildController(\n                new MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(controllerGraphText, "TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,");
        AssertContains(controllerGraphText, "ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,");
        AssertContains(controllerGraphText, "SetSelectedFormat = value => viewModel.SelectedFormat = value,");

        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(");
        AssertContains(controllerGraphText, "new MainViewModelDeviceRefreshControllerContext");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");
        AssertContains(controllerGraphText, "BeginBackgroundFormatProbe = (device, scanGeneration) =>");

        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerText, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(controllerGraphText, "ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(controllerGraphText, "ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>");

        AssertContains(captureSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationControllerContext");
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

        AssertContains(recordingSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertContains(recordingSettingsAutomationControllerText, "_context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsAutomationControllerContext");

        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingCapabilityControllerContext");
        AssertContains(controllerGraphText, "ReplaceAvailableRecordingFormats = formats =>");
        AssertContains(controllerGraphText, "NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),");

        AssertContains(captureModeOptionRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, double, FrameRateTimingFamily> ResolvePreferredTimingFamily");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, IReadOnlyList<FrameRateOption>, double, (double? Rate, string? Arg, string Origin)> ResolveDetectedSourceFrameRate");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public required Func<string?, IReadOnlyList<FrameRateTimingVariant>> BuildFrameRateTimingVariants");
        AssertContains(frameRateTimingResolverText, "namespace Sussudio.Controllers;");
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolver");
        AssertContains(frameRateTimingResolverText, "public FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(frameRateTimingResolverText, "public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(frameRateTimingResolverText, "public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelFrameRateTimingResolver.cs")),
            "frame-rate timing resolver lives with capture mode option rebuild owner");
        AssertContains(captureModeOptionRebuildControllerText, "public required string AutoResolutionValue { get; init; }");
        AssertContains(captureModeOptionRebuildControllerText, "public required double AutoFrameRateValue { get; init; }");
        AssertContains(controllerGraphText, "AutoResolutionValue = AutoResolutionValue,");
        AssertContains(controllerGraphText, "AutoFrameRateValue = AutoFrameRateValue,");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "_viewModel.");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionRebuildControllerText.Split('\n').Length >= 300,
            "capture mode option rebuild controller is a substantial ownership file");
        AssertContains(captureModeOptionRebuildControllerText, "_frameRateTimingResolver.ResolveDetectedSourceFrameRate(");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");

        AssertContains(deviceFormatProbeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(deviceFormatProbeRetargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
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

        return Task.CompletedTask;
    }

internal static Task MainViewModelRuntimeControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var sourceTelemetryControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs").Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs").Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs").Replace("\r\n", "\n");

        AssertContains(sourceTelemetryControllerText, "namespace Sussudio.Controllers;");
        AssertContains(sourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryController");
        AssertContains(sourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "private readonly MainViewModelSourceTelemetryControllerContext _context;");
        AssertDoesNotContain(sourceTelemetryControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(sourceTelemetryControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelSourceTelemetryControllerContext");
        AssertContains(sourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Func<string?, bool> IsAutoResolutionValue { get; init; }");
        AssertContains(sourceTelemetryControllerText, "public required Action RebuildResolutionOptions { get; init; }");
        AssertContains(controllerGraphText, "SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,");
        AssertContains(controllerGraphText, "BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,");
        AssertContains(controllerGraphText, "IsAutoResolutionValue = MainViewModel.IsAutoResolutionValue,");
        AssertContains(controllerGraphText, "RebuildResolutionOptions = viewModel.RebuildResolutionOptions,");
        AssertContains(controllerGraphText, "UpdateTargetSummary = viewModel.UpdateTargetSummary,");
        AssertContains(sourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(sourceTelemetryControllerText, "public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)");
        AssertContains(sourceTelemetryControllerText, "public void RefreshSourceTelemetrySummaryAge()");

        AssertContains(controllerGraphText, "private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(");
        AssertContains(controllerGraphText, "new MainViewModelRuntimeLifecycleController(\n                new MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(controllerGraphText, "CreateEventIngressController = () => CreateRuntimeEventIngressController(");
        AssertContains(controllerGraphText, "deviceFormatProbeController,");
        AssertContains(controllerGraphText, "sourceTelemetryController),");
        AssertContains(controllerGraphText, "ApplySourceTelemetrySnapshot = sourceTelemetryController.ApplySourceTelemetrySnapshot,");
        AssertContains(controllerGraphText, "RefreshSourceTelemetrySummaryAge = sourceTelemetryController.RefreshSourceTelemetrySummaryAge,");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var runtimeLifecycleController = CreateRuntimeLifecycleController(");

        AssertContains(runtimeLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(runtimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeEventIngressController _eventIngressController;");
        AssertContains(runtimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(runtimeLifecycleControllerText, "private readonly MainViewModelRuntimeLifecycleControllerContext _context;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeLifecycleController.cs")),
            "runtime lifecycle controller folded into MainViewModelLifecycleController.cs");
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
        AssertContains(disposalControllerText, "internal sealed class MainViewModelDisposalControllerContext");
        AssertContains(disposalControllerText, "private readonly MainViewModelDisposalControllerContext _context;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDisposalController.cs")),
            "disposal controller folded into MainViewModelLifecycleController.cs");
        AssertContains(disposalControllerText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
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

    internal static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var runtimeEventIngressControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeEventIngressControllerText, "_context.SetIsInitialized(_context.IsCaptureInitialized());");
        AssertContains(runtimeEventIngressControllerText, "_context.SetIsPreviewing(_context.IsVideoPreviewActive());");
        AssertContains(runtimeEventIngressControllerText, "_context.SetIsRecording(_context.IsCaptureRecording());");
        AssertContains(runtimeEventIngressControllerText, "_context.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeEventIngressControllerText, "_context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}
