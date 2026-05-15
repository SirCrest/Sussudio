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
    // Recording lifecycle: MainViewModel.RecordingLifecycle.cs
    // Recording state: MainViewModel.RecordingState.cs
    // Capture settings projection: MainViewModel.CaptureSettings.cs
    // Automation / flashback: MainViewModel.Automation.cs
    // Device-selection automation: MainViewModel.AutomationDeviceSelection.cs
    // Capture-mode automation: MainViewModel.AutomationCaptureMode.cs
    // UI-only automation: MainViewModel.AutomationUi.cs
    // Recording settings automation: MainViewModel.AutomationRecordingSettings.cs
    // Audio monitoring: MainViewModel.AudioMonitoring.cs
    // Microphone endpoint volume: MainViewModel.MicrophoneVolume.cs
    // Device-native audio controls: MainViewModel.AudioControls.cs
    // Device-native audio cancellation: MainViewModel.AudioControlCancellation.cs
    // Watcher-driven audio endpoint discovery: MainViewModel.AudioDeviceDiscovery.cs
    // Audio property changes: MainViewModel.AudioPropertyChanges.cs
    // Device management: MainViewModel.DeviceManagement.cs
    // Device format probes: MainViewModel.DeviceFormatProbes.cs
    // Frame-rate options: MainViewModel.FrameRateOptions.cs
    // Frame-rate timing policy: MainViewModel.FrameRateTiming.cs
    // Auto resolution options: MainViewModel.AutoResolutionOptions.cs
    // Resolution selection policy: MainViewModel.ResolutionSelectionPolicy.cs
    // Disposal / teardown: MainViewModel.Disposal.cs
    // Runtime status/timers: MainViewModel.Runtime.cs
    // Source telemetry: MainViewModel.Telemetry.cs
    // Settings persistence: MainViewModel.Settings.cs
    // Flashback settings reactions: MainViewModel.FlashbackSettings.cs
    // Recording capability refresh: MainViewModel.RecordingCapabilityRefresh.cs
}
