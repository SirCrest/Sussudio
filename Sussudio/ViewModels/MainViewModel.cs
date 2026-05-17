using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
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

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _captureService.SetPreviewFrameSink(sink);
    }

    internal void CancelPendingPreviewRestart()
    {
        if (IsPreviewReinitializing)
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }
    }


    public MainViewModel()
        : this(MainViewModelDependencies.CreateDefault())
    {
    }

    internal MainViewModel(MainViewModelDependencies dependencies)
    {
        _deviceService = dependencies.DeviceService;
        _deviceService.FormatProbeCompleted += OnDeviceFormatProbeCompleted;
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

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.PreCleanupRequested += OnCapturePreCleanupRequested;

        // Subscribe to system power events to recover capture after sleep/hibernate
        // resume. SystemEvents.PowerModeChanged is the standard .NET desktop API for
        // S3/S4 wake notifications — Microsoft.Windows.System.Power.PowerManager has
        // no Resuming event (only battery/display/effective-power-mode changes), so
        // this is the only managed surface that delivers the wake signal we need.
        // The event fires on a system thread-pool thread; the handler dispatches the
        // actual reinit to the UI thread via EnqueueUiOperation.
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;
        _captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;
        _latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);
        UpdateHdrRuntimeStatusFromCapture();
        UpdateLiveCaptureInfo();

        _audioDeviceWatcher = dependencies.AudioDeviceWatcher;
        _audioDeviceWatcher.DevicesChanged += OnAudioDevicesChanged;

        SetupTimer();
        UpdateDiskSpace();
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
    // Capture lifecycle: MainViewModel.Capture.cs
    // Recording lifecycle: MainViewModel.RecordingLifecycle.cs; operations: MainViewModel.RecordingOperations.cs
    // Recording state: MainViewModel.RecordingState.cs
    // Capture settings projection: MainViewModel.CaptureSettings.cs
    // Flashback automation: MainViewModel.AutomationFlashback.cs
    // Recording lifecycle automation: MainViewModel.AutomationRecordingLifecycle.cs
    // Audio automation: MainViewModel.AutomationAudio.cs; microphone automation: MainViewModel.AutomationMicrophone.cs
    // HDR automation: MainViewModel.AutomationHdr.cs
    // Automation options projection: MainViewModel.AutomationOptionsSnapshot.cs
    // Device-selection automation: MainViewModel.AutomationDeviceSelection.cs
    // Capture-mode automation: MainViewModel.AutomationCaptureMode.cs; frame rate: MainViewModel.AutomationFrameRate.cs; video format: MainViewModel.AutomationVideoFormat.cs; MJPEG decoder count: MainViewModel.AutomationMjpegDecoderCount.cs
    // UI-only automation: MainViewModel.AutomationUi.cs
    // Recording format automation: MainViewModel.AutomationRecordingFormat.cs; recording quality: MainViewModel.AutomationRecordingQuality.cs; split encode mode: MainViewModel.AutomationSplitEncodeMode.cs; custom bitrate: MainViewModel.AutomationCustomBitrate.cs; encoder preset: MainViewModel.AutomationEncoderPreset.cs; output path: MainViewModel.AutomationOutputPath.cs
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
    // Device format probes: MainViewModel.DeviceFormatProbes.cs
    // Capture option visibility: MainViewModel.CaptureOptionVisibility.cs
    // Frame-rate selection: MainViewModel.FrameRateOptions.cs; rebuild: MainViewModel.FrameRateOptionRebuild.cs
    // Automatic frame-rate selection policy: MainViewModel.FrameRateAutoSelectionPolicy.cs
    // Frame-rate/mode selection state: MainViewModel.ModeSelectionState.cs
    // Frame-rate timing state wrappers: MainViewModel.FrameRateTiming.cs; pure timing policy: FrameRateTimingPolicy.cs
    // Auto resolution options: MainViewModel.AutoResolutionOptions.cs; selection: MainViewModel.AutoResolutionSelection.cs
    // Resolution selection policy: MainViewModel.ResolutionSelectionPolicy.cs
    // Disposal / teardown: MainViewModel.Disposal.cs
    // Runtime status/timers: MainViewModel.Runtime.cs, MainViewModel.CaptureRuntimeEvents.cs, and MainViewModel.RecordingRuntime.cs
    // Live-signal presentation: MainViewModel.LiveSignalPresentation.cs
    // Source telemetry: MainViewModel.Telemetry.cs
    // HDR runtime presentation: MainViewModel.HdrRuntimePresentation.cs
    // Target-summary presentation: MainViewModel.TargetSummaryPresentation.cs
    // Settings lifecycle/reactions: MainViewModel.Settings.cs; load/save projection: MainViewModel.SettingsPersistence.cs
    // Flashback settings reactions: encoder: MainViewModel.FlashbackEncoderSettings.cs; buffer/GPU: MainViewModel.FlashbackSettings.cs
    // Recording capability refresh: MainViewModel.RecordingCapabilityRefresh.cs
}
