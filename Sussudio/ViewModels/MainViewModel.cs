using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Preview;

namespace Sussudio.ViewModels;

/// <summary>
/// UI-facing state coordinator. MainViewModel translates user settings and
/// automation requests into serialized CaptureService operations while keeping
/// WinUI properties, saved settings, and diagnostics summaries coherent.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel
{
    private IntPtr _windowHandle;
    private const string LiveInfoUnavailable = "\u2014";

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

    public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
        => _deviceRefreshController.RefreshDevicesAsync(cancellationToken);

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _captureService.SetPreviewFrameSink(sink);
    }

    internal void CancelPendingPreviewRestart()
        => _previewLifecycleController.CancelPendingPreviewRestart();

    private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
        => _previewLifecycleController.InitializeDeviceAsync(cancellationToken);

    public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
        => _previewLifecycleController.StartPreviewAsync(userInitiated, cancellationToken);

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);

    public Task StopPreviewAsync()
        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated)
        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline)
        => StopPreviewAsync(userInitiated, teardownPipeline, CancellationToken.None);

    public Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        => _previewLifecycleController.ApplySelectedDeviceAsync(device, cancellationToken);

    private Task ReinitializeDeviceAsync(string reason)
        => _previewLifecycleController.ReinitializeDeviceAsync(reason);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
        => _previewLifecycleController.StopPreviewAsync(userInitiated, teardownPipeline, cancellationToken);

    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }
    public Action<bool>? FrameTimeOverlayVisibilityHandler { get; set; }

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;

    public Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsSettingsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsStatsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            FrameTimeOverlayVisibilityHandler?.Invoke(visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFlashbackTimelineVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsFlashbackTimelineVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    private int _disposeState;

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

    private void CancelActiveFlashbackExportForDispose()
    {
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback only. MainWindow.Closed calls
    // await ViewModel.DisposeAsync(); this sync path is for GC finalizer safety.
    public void Dispose()
        => _disposalController.Dispose();

    public async ValueTask DisposeAsync()
        => await _disposalController.DisposeAsync().ConfigureAwait(false);
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
