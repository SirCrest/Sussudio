using System;
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
    private readonly MainViewModelRuntimeLifecycleController _runtimeLifecycleController;
    private readonly MainViewModelRecordingTransitionController _recordingTransitionController;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private readonly MainViewModelDeviceAudioRequestController _deviceAudioRequestController;
    private readonly MainViewModelRecordingCapabilityController _recordingCapabilityController;
    private readonly MainViewModelCaptureSettingsAutomationController _captureSettingsAutomationController;
    private readonly MainViewModelRecordingSettingsAutomationController _recordingSettingsAutomationController;

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

    public Task ToggleRecordingAsync()
        => _recordingTransitionController.ToggleRecordingAsync();

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => SetRecordingDesiredStateAsync(enabled, cancellationToken);

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingAndWaitAsync(cancellationToken);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingForEmergencyAsync(cancellationToken);

    public MainViewModel()
        : this(MainViewModelDependencies.CreateDefault())
    {
    }

    internal MainViewModel(MainViewModelDependencies dependencies)
    {
        _deviceService = dependencies.DeviceService;
        _captureService = dependencies.CaptureService;
        _sessionCoordinator = dependencies.SessionCoordinator;
        _audioRampTraceRecorder = new AudioRampTraceRecorder(
            new AudioRampTraceRecorderContext
            {
                GetRuntimeSnapshot = () => _captureService.GetRuntimeSnapshot(),
                GetPreviewVolume = () => PreviewVolume,
                GetIsAudioEnabled = () => IsAudioEnabled,
                GetIsAudioPreviewEnabled = () => IsAudioPreviewEnabled,
                GetAudioPeak = () => AudioPeak,
                Log = message => Logger.Log(message),
            });
        _previewAudioVolumeTransitionController = new PreviewAudioVolumeTransitionController(
            new PreviewAudioVolumeTransitionControllerContext
            {
                GetPreviewVolume = () => PreviewVolume,
                SetPreviewVolume = value => PreviewVolume = value,
                SetSessionPreviewVolume = volume => _sessionCoordinator.SetPreviewVolume(volume),
                BeginTraceSession = BeginAudioRampTraceSession,
                CompleteTraceSession = CompleteAudioRampTraceSession,
                RecordTracePoint = RecordAudioRampTracePoint,
                Log = (message, caller) => Logger.Log(message, caller),
            });
        _deviceAudioControlService = dependencies.DeviceAudioControlService;
        _dispatcherQueue = dependencies.DispatcherQueue;
        _uiDispatchController = new MainViewModelUiDispatchController(
            new MainViewModelUiDispatchControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                IsDisposing = () => Volatile.Read(ref _disposeState) != 0,
                Log = message => Logger.Log(message),
                LogException = exception => Logger.LogException(exception),
                SetStatusText = value => StatusText = value,
            });
        _audioDeviceWatcher = dependencies.AudioDeviceWatcher;
        _recordingTransitionController = new MainViewModelRecordingTransitionController(this);
        _previewLifecycleController = new MainViewModelPreviewLifecycleController(this);
        _deviceAudioRequestController = new MainViewModelDeviceAudioRequestController(this);
        _recordingCapabilityController = new MainViewModelRecordingCapabilityController(this);
        _captureSettingsAutomationController = new MainViewModelCaptureSettingsAutomationController(this);
        _recordingSettingsAutomationController = new MainViewModelRecordingSettingsAutomationController(this);
        _deviceFormatProbeController = new MainViewModelDeviceFormatProbeController(this);
        _sourceTelemetryController = new MainViewModelSourceTelemetryController(this);
        _runtimeLifecycleController = new MainViewModelRuntimeLifecycleController(this);

        _runtimeLifecycleController.Start();
        _runtimeLifecycleController.InitializePresentation();
    }
    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    // -- Capture and recording lifecycle facade methods stay in this root compatibility facade -----
    // -- Recording observable state is in MainViewModel.RecordingState.cs -----
    // -- Capture settings projection adapter is in MainViewModel.CaptureSettings.cs -----
    // -- Automation methods are split across MainViewModel.Automation*.cs ---------

    // -- Partial class references ----
    // Capture lifecycle facade: this file; preview lifecycle owner: MainViewModelPreviewLifecycleController.cs; preview reinitialize transaction: MainViewModelPreviewReinitializeController.cs
    // Recording lifecycle facade: this file; transition owner: MainViewModelRecordingTransitionController.cs
    // Recording state: MainViewModel.RecordingState.cs
    // Capture settings projection: MainViewModel.CaptureSettings.cs and CaptureSettingsProjectionBuilder.cs
    // Flashback automation and buffer/GPU reactions: MainViewModel.FlashbackSettings.cs
    // Audio automation: MainViewModel.AutomationAudio.cs
    // HDR automation: MainViewModel.CaptureModeTransactions.cs
    // Automation snapshots and probes: MainViewModel.AutomationSnapshots.cs
    // Automation options projection: MainViewModel.AutomationOptionsSnapshot.cs
    // Device and audio-input selection automation: MainViewModel.AutomationDeviceSelection.cs
    // Settings automation facade: MainViewModel.AutomationSettings.cs; capture controller: MainViewModelCaptureSettingsAutomationController.cs; recording controller: MainViewModelRecordingSettingsAutomationController.cs; preview automation: MainViewModelPreviewLifecycleController.cs; capture-mode transactions: MainViewModel.CaptureModeTransactions.cs
    // UI-only automation: MainViewModel.AutomationUi.cs
    // UI dispatch policy: MainViewModelUiDispatchController.cs; adapter/fan-out: MainViewModel.Dispatching.cs
    // Audio monitoring: MainViewModel.AudioMonitoring.cs
    // Audio input selection/property changes: MainViewModel.AudioInputSelection.cs
    // Audio ramp trace recorder: AudioRampTraceRecorder.cs; adapter: MainViewModel.AudioRampTrace.cs
    // Preview audio volume transitions: PreviewAudioVolumeTransitionController.cs
    // Microphone endpoint volume: MainViewModel.MicrophoneVolume.cs
    // Device-native audio controls: request lifetime and property-change adapter: MainViewModelDeviceAudioRequestController.cs; guards: MainViewModel.AudioControls.cs; mode writes: MainViewModel.DeviceAudioMode.cs; refresh: MainViewModel.DeviceAudioRefresh.cs; analog gain writes: MainViewModel.AnalogAudioGain.cs
    // Watcher-driven audio endpoint discovery: MainViewModel.AudioDeviceDiscovery.cs
    // Audio capture/preview property changes: MainViewModel.AudioPropertyChanges.cs
    // Audio input/microphone property changes: focused partials
    // Device management: MainViewModel.DeviceManagement.cs
    // Device selection reactions: MainViewModel.DeviceSelection.cs
    // Device format probe reconciliation: MainViewModelDeviceFormatProbeController.cs; pure retarget policy: DeviceFormatProbeRetargetPolicy.cs
    // Capture mode transactions: MainViewModel.CaptureModeTransactions.cs
    // Frame-rate selection: MainViewModel.FrameRateOptions.cs; rebuild: MainViewModel.FrameRateOptionRebuild.cs
    // Runtime bootstrap/timer: MainViewModelRuntimeLifecycleController.cs; capture-event ingress: MainViewModelRuntimeEventIngressController.cs
    // Automatic frame-rate selection policy: MainViewModel.FrameRateAutoSelectionPolicy.cs
    // Frame-rate/mode selection state: MainViewModel.ModeSelectionState.cs
    // Frame-rate timing state wrappers: MainViewModel.FrameRateTiming.cs; pure timing policy: FrameRateTimingPolicy.cs
    // Resolution option rebuild/dropdown mutation: MainViewModel.ResolutionOptionRebuild.cs; effective resolution state-backed policy delegates: MainViewModel.ResolutionOptions.cs
    // Disposal / teardown: MainViewModel.Disposal.cs
    // Recording runtime status and output drive presentation: MainViewModel.RecordingRuntime.cs
    // Capture presentation labels: MainViewModel.CapturePresentation.cs
    // Source telemetry ingress/projection: MainViewModelSourceTelemetryController.cs
    // Settings lifecycle/reactions and adapter: MainViewModel.SettingsPersistence.cs; projection: MainViewModelSettingsPersistenceProjection.cs
    // Flashback settings reactions: encoder: MainViewModel.FlashbackEncoderSettings.cs; enable/restart/buffer/GPU: MainViewModel.FlashbackSettings.cs
    // Recording capability refresh and option application: MainViewModelRecordingCapabilityController.cs
}
