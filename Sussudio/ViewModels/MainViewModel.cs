using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;

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
    private readonly MainViewModelDeviceRefreshController _deviceRefreshController;
    private readonly MainViewModelRuntimeLifecycleController _runtimeLifecycleController;
    private readonly MainViewModelRecordingTransitionController _recordingTransitionController;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private readonly MainViewModelDeviceAudioRequestController _deviceAudioRequestController;
    private readonly MainViewModelRecordingCapabilityController _recordingCapabilityController;
    private readonly MainViewModelCaptureSettingsAutomationController _captureSettingsAutomationController;
    private readonly MainViewModelRecordingSettingsAutomationController _recordingSettingsAutomationController;
    private readonly MainViewModelCaptureModeOptionRebuildController _captureModeOptionRebuildController;
    private readonly MainViewModelDisposalController _disposalController;

    public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
        => _deviceRefreshController.RefreshDevicesAsync(cancellationToken);

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
    // -- Device refresh facade methods stay in this root compatibility facade -----
    // -- Recording observable state and lifecycle facade methods are in MainViewModel.RecordingState.cs -----
    // -- Capture settings projection adapter is in MainViewModel.CaptureSettings.cs -----
    // -- Automation methods are split across MainViewModel.Automation*.cs ---------

    // -- Partial class references ----
    // Preview lifecycle facade/state/events: MainViewModel.PreviewState.cs; preview lifecycle owner: MainViewModelPreviewLifecycleController.cs; preview reinitialize transaction: MainViewModelPreviewReinitializeController.cs
    // Recording lifecycle facade/state: MainViewModel.RecordingState.cs; transition owner: MainViewModelRecordingTransitionController.cs
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
    // Audio ramp trace and preview-volume wiring: MainViewModel.AudioRampTrace.cs
    // Microphone endpoint volume: MainViewModel.MicrophoneVolume.cs
    // Device-native audio controls: request lifetime and property-change adapter: MainViewModelDeviceAudioRequestController.cs; guards: MainViewModel.AudioControls.cs; mode writes: MainViewModel.DeviceAudioMode.cs; refresh: MainViewModel.DeviceAudioRefresh.cs; analog gain writes: MainViewModel.AnalogAudioGain.cs
    // Watcher-driven audio endpoint discovery: MainViewModel.AudioDeviceDiscovery.cs
    // Audio capture/preview property changes: MainViewModel.AudioPropertyChanges.cs
    // Audio input/microphone property changes: focused partials
    // Device refresh facade: this file; refresh owner: MainViewModelDeviceRefreshController.cs
    // Device selection reactions: MainViewModel.DeviceSelection.cs
    // Device format probe reconciliation: MainViewModelDeviceFormatProbeController.cs; pure retarget policy: DeviceFormatProbeRetargetPolicy.cs
    // Capture mode transactions: MainViewModel.CaptureModeTransactions.cs
    // Frame-rate selection: MainViewModel.FrameRateOptions.cs; pure policies: FrameRateAutoSelectionPolicy.cs and FrameRateSourceFilterPolicy.cs; capture option rebuild adapters: MainViewModel.CaptureModeTransactions.cs; rebuild owner: MainViewModelCaptureModeOptionRebuildController.cs
    // Controller graph construction: MainViewModelControllerGraph.cs
    // Runtime bootstrap/timer: MainViewModelRuntimeLifecycleController.cs; capture-event ingress: MainViewModelRuntimeEventIngressController.cs
    // Automatic frame-rate selection policy: FrameRateAutoSelectionPolicy.cs
    // Frame-rate/mode selection state: MainViewModel.ModeSelectionState.cs
    // Frame-rate timing state wrappers: MainViewModel.FrameRateTiming.cs; pure timing policy: FrameRateTimingPolicy.cs
    // Resolution option rebuild adapter: MainViewModel.CaptureModeTransactions.cs; rebuild owner: MainViewModelCaptureModeOptionRebuildController.Resolution.cs; effective resolution state-backed policy delegates: MainViewModel.ResolutionOptions.cs
    // Disposal / teardown: MainViewModel.Disposal.cs; bounded policy owner: MainViewModelDisposalController.cs
    // Recording runtime status and output drive presentation: MainViewModel.RecordingRuntime.cs
    // Capture presentation labels: MainViewModel.CapturePresentation.cs
    // Source telemetry ingress/projection: MainViewModelSourceTelemetryController.cs
    // Settings lifecycle/reactions and IO adapter: MainViewModel.SettingsPersistence.cs; load-plan application: MainViewModel.SettingsLoadApplication.cs; projection: MainViewModelSettingsPersistenceProjection.cs
    // Flashback settings reactions: encoder: MainViewModel.FlashbackEncoderSettings.cs; enable/restart/buffer/GPU: MainViewModel.FlashbackSettings.cs
    // Recording capability refresh and option application: MainViewModelRecordingCapabilityController.cs
}
