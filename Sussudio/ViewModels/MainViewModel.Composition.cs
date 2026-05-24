using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

// Construction and collaborator wiring for the MainViewModel compatibility
// facade. Keep feature behavior in focused partials/controllers.
public partial class MainViewModel
{
    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly CaptureSessionCoordinator _sessionCoordinator;
    private readonly AudioRampTraceRecorder _audioRampTraceRecorder;
    private readonly PreviewAudioVolumeTransitionController _previewAudioVolumeTransitionController;
    private readonly NativeXuAudioControlService _deviceAudioControlService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly AudioDeviceWatcher _audioDeviceWatcher;
    private readonly MainViewModelUiDispatchController _uiDispatchController;
    private readonly MainViewModelDeviceFormatProbeController _deviceFormatProbeController;
    private readonly MainViewModelSourceTelemetryController _sourceTelemetryController;
    private readonly MainViewModelDeviceRefreshController _deviceRefreshController;
    private readonly MainViewModelRuntimeLifecycleController _runtimeLifecycleController;
    private readonly MainViewModelRecordingTransitionController _recordingTransitionController;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private readonly MainViewModelDeviceAudioRequestController _deviceAudioRequestController;
    private readonly MainViewModelRecordingCapabilityController _recordingCapabilityController;
    private readonly MainViewModelCaptureSettingsAutomationController _captureSettingsAutomationController;
    private readonly MainViewModelRecordingSettingsAutomationController _recordingSettingsAutomationController;
    private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;
    private readonly MainViewModelCaptureModeOptionRebuildController _captureModeOptionRebuildController;
    private readonly MainViewModelDisposalController _disposalController;

    public MainViewModel()
        : this(MainViewModelDependencies.CreateDefault())
    {
    }

    internal MainViewModel(MainViewModelDependencies dependencies)
    {
        _deviceService = dependencies.DeviceService;
        _captureService = dependencies.CaptureService;
        _sessionCoordinator = dependencies.SessionCoordinator;
        _audioRampTraceRecorder = CreateAudioRampTraceRecorder();
        _previewAudioVolumeTransitionController = CreatePreviewAudioVolumeTransitionController();
        _deviceAudioControlService = dependencies.DeviceAudioControlService;
        _dispatcherQueue = dependencies.DispatcherQueue;
        _audioDeviceWatcher = dependencies.AudioDeviceWatcher;
        _frameRateTimingResolver = MainViewModelControllerGraph.CreateFrameRateTimingResolver(this);

        var controllerGraph = MainViewModelControllerGraph.Create(this);
        _uiDispatchController = controllerGraph.UiDispatchController;
        _recordingTransitionController = controllerGraph.RecordingTransitionController;
        _previewLifecycleController = controllerGraph.PreviewLifecycleController;
        _deviceAudioRequestController = controllerGraph.DeviceAudioRequestController;
        _recordingCapabilityController = controllerGraph.RecordingCapabilityController;
        _captureSettingsAutomationController = controllerGraph.CaptureSettingsAutomationController;
        _recordingSettingsAutomationController = controllerGraph.RecordingSettingsAutomationController;
        _captureModeOptionRebuildController = controllerGraph.CaptureModeOptionRebuildController;
        _deviceFormatProbeController = controllerGraph.DeviceFormatProbeController;
        _sourceTelemetryController = controllerGraph.SourceTelemetryController;
        _deviceRefreshController = controllerGraph.DeviceRefreshController;
        _runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;
        _disposalController = controllerGraph.DisposalController;

        _runtimeLifecycleController.Start();
        _runtimeLifecycleController.InitializePresentation();
    }

    private bool EnqueueUiOperation(Func<Task> operation, string operationName, bool allowDuringDispose = false)
        => _uiDispatchController.Enqueue(operation, operationName, allowDuringDispose);

    private Task ExecuteUiOperationAsync(Func<Task> operation, string operationName)
        => _uiDispatchController.ExecuteAsync(operation, operationName);

    private async Task NotifyPreviewReinitRequestedAsync(string reason)
    {
        var handlers = PreviewReinitRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<string, Task> handler in handlers.GetInvocationList())
        {
            await handler(reason);
        }
    }

    private async Task NotifyRendererStopAsync()
    {
        var handlers = PreviewRendererStopRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<Task> handler in handlers.GetInvocationList())
        {
            await handler();
        }
    }

    private Task InvokeOnUiThreadAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private static async Task AwaitWithTimeoutAsync(Task task, int timeoutMs, string operationName)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"{operationName} timed out after {timeoutMs} ms.");
        }

        await task.ConfigureAwait(false);
    }
}

// Construction seam for the root compatibility view model. MainViewModel keeps
// the XAML/automation-facing property surface, while this type owns the default
// service graph until a fuller composition root can inject feature view models.
internal sealed class MainViewModelDependencies
{
    private MainViewModelDependencies(
        DeviceService deviceService,
        CaptureService captureService,
        CaptureSessionCoordinator sessionCoordinator,
        NativeXuAudioControlService deviceAudioControlService,
        DispatcherQueue dispatcherQueue,
        AudioDeviceWatcher audioDeviceWatcher)
    {
        DeviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        CaptureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        SessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        DeviceAudioControlService = deviceAudioControlService ?? throw new ArgumentNullException(nameof(deviceAudioControlService));
        DispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        AudioDeviceWatcher = audioDeviceWatcher ?? throw new ArgumentNullException(nameof(audioDeviceWatcher));
    }

    public DeviceService DeviceService { get; }
    public CaptureService CaptureService { get; }
    public CaptureSessionCoordinator SessionCoordinator { get; }
    public NativeXuAudioControlService DeviceAudioControlService { get; }
    public DispatcherQueue DispatcherQueue { get; }
    public AudioDeviceWatcher AudioDeviceWatcher { get; }

    public static MainViewModelDependencies CreateDefault()
    {
        var captureService = new CaptureService();
        return new MainViewModelDependencies(
            new DeviceService(),
            captureService,
            new CaptureSessionCoordinator(captureService),
            new NativeXuAudioControlService(),
            DispatcherQueue.GetForCurrentThread(),
            new AudioDeviceWatcher());
    }
}
