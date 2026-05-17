using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var stateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs").Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs").Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs").Replace("\r\n", "\n");
        var runtimeWiringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RuntimeWiring.cs").Replace("\r\n", "\n");
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
        AssertContains(rootText, "AttachRuntimeWiring();");
        AssertContains(rootText, "InitializeRuntimePresentation();");
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
        AssertContains(stateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
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
        AssertContains(flashbackStateText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(uiDispatchControllerText, "public required DispatcherQueue DispatcherQueue { get; init; }");
        AssertContains(uiDispatchControllerText, "public required Func<bool> IsDisposing { get; init; }");

        AssertContains(runtimeWiringText, "private void AttachRuntimeWiring()");
        AssertContains(runtimeWiringText, "_deviceService.FormatProbeCompleted += OnDeviceFormatProbeCompleted;");
        AssertContains(runtimeWiringText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertContains(runtimeWiringText, "_captureService.ErrorOccurred += OnCaptureError;");
        AssertContains(runtimeWiringText, "_captureService.PreCleanupRequested += OnCapturePreCleanupRequested;");
        AssertContains(runtimeWiringText, "_captureService.FrameCaptured += OnFrameCaptured;");
        AssertContains(runtimeWiringText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertContains(runtimeWiringText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(runtimeWiringText, "_captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;");
        AssertContains(runtimeWiringText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(runtimeWiringText, "_audioDeviceWatcher.DevicesChanged += OnAudioDevicesChanged;");
        AssertContains(runtimeWiringText, "private void DetachRuntimeWiring()");
        AssertContains(runtimeWiringText, "_deviceService.FormatProbeCompleted -= OnDeviceFormatProbeCompleted;");
        AssertContains(runtimeWiringText, "_captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertContains(runtimeWiringText, "_captureService.AudioLevelUpdated -= OnAudioLevelUpdated;");
        AssertContains(runtimeWiringText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");
        AssertContains(runtimeWiringText, "private void InitializeRuntimePresentation()");
        AssertContains(runtimeWiringText, "_latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(runtimeWiringText, "ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);");
        AssertContains(runtimeWiringText, "UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(runtimeWiringText, "UpdateLiveCaptureInfo();");
        AssertContains(runtimeWiringText, "SetupTimer();");
        AssertContains(runtimeWiringText, "UpdateDiskSpace();");
        AssertContains(disposalText, "DetachRuntimeWiring();");
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
