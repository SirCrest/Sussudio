using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
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
    private readonly MainViewModelRuntimeLifecycleController _runtimeLifecycleController;
    private readonly MainViewModelRecordingTransitionController _recordingTransitionController;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _captureService.SetPreviewFrameSink(sink);
    }

    internal void CancelPendingPreviewRestart()
        => _previewLifecycleController.CancelPendingPreviewRestart();


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
        _deviceFormatProbeController = new MainViewModelDeviceFormatProbeController(this);
        _runtimeLifecycleController = new MainViewModelRuntimeLifecycleController(this);

        _runtimeLifecycleController.Start();
        _runtimeLifecycleController.InitializePresentation();
    }
    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    // -- Capture lifecycle methods are in MainViewModel.Capture.cs -----
    // -- Recording lifecycle methods are in MainViewModel.RecordingLifecycle.cs -----
    // -- Recording observable state is in MainViewModel.RecordingState.cs -----
    // -- Capture settings projection is in MainViewModel.CaptureSettings.cs -----
    // -- Automation methods are split across MainViewModel.Automation*.cs ---------

    // -- Partial class references ----
    // Capture lifecycle facade: MainViewModel.Capture.cs; preview lifecycle owner: MainViewModelPreviewLifecycleController.cs
    // Recording lifecycle facade: MainViewModel.RecordingLifecycle.cs; transition owner: MainViewModelRecordingTransitionController.cs
    // Recording state: MainViewModel.RecordingState.cs
    // Capture settings projection: MainViewModel.CaptureSettings.cs
    // Flashback automation: MainViewModel.AutomationFlashback.cs
    // Audio automation: MainViewModel.AutomationAudio.cs
    // HDR automation: MainViewModel.AutomationHdr.cs
    // Automation snapshots and probes: MainViewModel.AutomationSnapshots.cs
    // Automation options projection: MainViewModel.AutomationOptionsSnapshot.cs
    // Device-selection automation: MainViewModel.AutomationDeviceSelection.cs; audio-input automation: MainViewModel.AutomationAudioInputSelection.cs
    // Capture settings automation: MainViewModel.AutomationCaptureSettings.cs
    // UI-only automation: MainViewModel.AutomationUi.cs; stats/overlay automation: MainViewModel.AutomationStatsUi.cs
    // Recording settings automation: MainViewModel.AutomationRecordingSettings.cs
    // UI dispatch policy: MainViewModelUiDispatchController.cs; adapter/fan-out: MainViewModel.Dispatching.cs
    // Audio monitoring: MainViewModel.AudioMonitoring.cs
    // Audio input selection: MainViewModel.AudioInputSelection.cs
    // Audio ramp trace recorder: AudioRampTraceRecorder.cs; adapter: MainViewModel.AudioRampTrace.cs
    // Preview audio volume transitions: PreviewAudioVolumeTransitionController.cs
    // Microphone endpoint volume: MainViewModel.MicrophoneVolume.cs
    // Device-native audio controls: guards: MainViewModel.AudioControls.cs; mode writes: MainViewModel.DeviceAudioMode.cs; refresh: MainViewModel.DeviceAudioRefresh.cs; analog gain writes: MainViewModel.AnalogAudioGain.cs
    // Device-native audio cancellation: MainViewModel.AudioControlCancellation.cs
    // Watcher-driven audio endpoint discovery: MainViewModel.AudioDeviceDiscovery.cs
    // Audio capture/preview property changes: MainViewModel.AudioPropertyChanges.cs
    // Audio input/microphone/device-audio property changes: focused partials
    // Device management: MainViewModel.DeviceManagement.cs
    // Device selection reactions: MainViewModel.DeviceSelection.cs
    // Device format probe reconciliation: MainViewModelDeviceFormatProbeController.cs; pure retarget policy: DeviceFormatProbeRetargetPolicy.cs
    // Capture option visibility: MainViewModel.CaptureOptionVisibility.cs
    // Frame-rate selection: MainViewModel.FrameRateOptions.cs; rebuild: MainViewModel.FrameRateOptionRebuild.cs
    // Runtime wiring/bootstrap/timer/capture-event ingress: MainViewModelRuntimeLifecycleController.cs
    // Automatic frame-rate selection policy: MainViewModel.FrameRateAutoSelectionPolicy.cs
    // Frame-rate/mode selection state: MainViewModel.ModeSelectionState.cs
    // Frame-rate timing state wrappers: MainViewModel.FrameRateTiming.cs; pure timing policy: FrameRateTimingPolicy.cs
    // Auto resolution options: MainViewModel.AutoResolutionOptions.cs; state-backed selection: MainViewModel.ResolutionOptions.cs
    // Resolution selection policy: MainViewModel.ResolutionSelectionPolicy.cs
    // Disposal / teardown: MainViewModel.Disposal.cs
    // Recording runtime status and output drive presentation: MainViewModel.RecordingRuntime.cs
    // Live-signal presentation: MainViewModel.LiveSignalPresentation.cs
    // Source telemetry: MainViewModel.Telemetry.cs
    // Target/HDR presentation: MainViewModel.TargetPresentation.cs
    // Settings lifecycle/reactions: MainViewModel.Settings.cs; adapter: MainViewModel.SettingsPersistence.cs; projection: MainViewModelSettingsPersistenceProjection.cs
    // Flashback settings reactions: encoder: MainViewModel.FlashbackEncoderSettings.cs; buffer/GPU: MainViewModel.FlashbackSettings.cs
    // Recording capability refresh: MainViewModel.RecordingCapabilityRefresh.cs
}
