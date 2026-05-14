using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private DispatcherQueueTimer? _timer;
    private IntPtr _windowHandle;
    private readonly Queue<(long Tick, long Bytes)> _flashbackBitrateSamples = new();
    private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;
    private const int PreviewReinitializeDebounceMs = 250;
    private const string HdrToggleBlockedWhileRecordingMessage = "Stop recording before switching between HDR and SDR pipelines.";
    private const string LiveInfoUnavailable = "\u2014";
    private const string AutoResolutionValue = "Source";
    private const double AutoFrameRateValue = 0;

    [ObservableProperty]
    public partial ObservableCollection<CaptureDevice> Devices { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> AudioInputDevices { get; set; } = new();

    [ObservableProperty]
    public partial AudioInputDevice? SelectedAudioInputDevice { get; set; }

    [ObservableProperty]
    public partial bool IsCustomAudioInputEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> MicrophoneDevices { get; set; } = new();

    [ObservableProperty]
    public partial bool IsMicrophoneEnabled { get; set; }

    [ObservableProperty]
    public partial AudioInputDevice? SelectedMicrophoneDevice { get; set; }

    [ObservableProperty]
    public partial CaptureDevice? SelectedDevice { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MediaFormat> AvailableFormats { get; set; } = new();

    [ObservableProperty]
    public partial MediaFormat? SelectedFormat { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ResolutionOption> AvailableResolutions { get; set; } = new();

    [ObservableProperty]
    public partial string? SelectedResolution { get; set; }

    [ObservableProperty]
    public partial uint? AutoResolvedWidth { get; set; }

    [ObservableProperty]
    public partial uint? AutoResolvedHeight { get; set; }

    [ObservableProperty]
    public partial double? AutoResolvedFrameRate { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<FrameRateOption> AvailableFrameRates { get; set; } = new();

    [ObservableProperty]
    public partial double SelectedFrameRate { get; set; } = 60;

    [ObservableProperty]
    public partial bool ShowAllCaptureOptions { get; set; }

    public bool IsAutoFrameRateSelected
    {
        get => _isAutoFrameRateSelected;
        private set => SetProperty(ref _isAutoFrameRateSelected, value);
    }

    // Resolution capability matrix keyed by "{width}x{height}".
    private readonly Dictionary<string, List<MediaFormat>> _resolutionToFormats =
        new(StringComparer.OrdinalIgnoreCase);
    private enum FrameRateTimingFamily
    {
        Unknown,
        Integer,
        Ntsc1001
    }
    private bool _isRebuildingModeOptions;
    private bool _isApplyingAutomaticFrameRateSelection;
    private bool _isApplyingAutomaticResolutionSelection;
    private bool _isAutoFrameRateSelected = true;
    private bool _hasUserOverriddenFrameRateForCurrentMode;
    private bool _hasUserOverriddenResolutionForCurrentMode;
    private bool _forceSourceAutoRetarget;
    private string? _lastSourceModeKey;
    private string? _lastKnownResolutionKey;
    private bool _pendingSdrAutoSelectionForDeviceChange;
    private int? _pendingSdrAutoFriendlyFrameRateBucket;
    private bool _pendingModeOptionsRefresh;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private int? _lastTelemetryAgeBucket;
    private long _deviceScanGeneration;

    // Flag to prevent reinitialization during initial device setup.
    private bool _isChangingDevice;
    private bool _suppressFormatChangeReinitialize;
    private bool _isRevertingHdrToggle;
    private bool _isLoadingSettings;
    private bool _suppressFlashbackFormatCycle;
    private bool _suppressFlashbackEncoderSettingsCycle;
    private string? _pendingSavedDeviceId;
    private string? _pendingSavedAudioDeviceId;
    private string? _pendingSavedMicrophoneDeviceId;
    private double? _pendingSavedMicrophoneVolume;
    private string? _pendingSavedMicrophoneVolumeDeviceId;
    private string? _pendingSavedDeviceAudioMode;
    private double? _pendingSavedAnalogAudioGainPercent;
    private bool _isRefreshingDeviceAudioControls;
    private CancellationTokenSource? _gainFlashDebounceCts;
    private CancellationTokenSource? _gainXuDebounceCts;
    private CancellationTokenSource? _deviceAudioModeCts;
    private CancellationTokenSource? _deviceAudioRefreshCts;
    private CancellationTokenSource? _exportCts;
    private int _flashbackExportOperationId;
    private int _audioEnabledChangeGeneration;
    private bool _suppressAudioPreviewEnabledChangeOperation;
    private int _flashbackSettingsRestartGeneration;
    private bool _suppressMicrophoneMonitorUpdate;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableVideoFormats { get; set; } = new()
    {
        "Auto", "MJPG", "NV12", "P010"
    };

    [ObservableProperty]
    public partial string SelectedVideoFormat { get; set; } = "Auto";

    [ObservableProperty]
    public partial int MjpegDecoderCount { get; set; } = 6;

    [ObservableProperty]
    public partial bool IsHdrEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsHdrAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsTrueHdrPreviewEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; }

    [ObservableProperty]
    public partial bool FlashbackGpuDecode { get; set; } = true;

    [ObservableProperty]
    public partial int FlashbackBufferMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial string HdrResolutionSupportHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HdrRuntimeState { get; set; } = "Inactive";

    [ObservableProperty]
    public partial string HdrReadinessReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? DetectedSourceFrameRate { get; set; }

    [ObservableProperty]
    public partial string? DetectedSourceFrameRateArg { get; set; }

    [ObservableProperty]
    public partial string SourceFrameRateOrigin { get; set; } = "Unknown";

    [ObservableProperty]
    public partial double? SelectedFriendlyFrameRate { get; set; }

    [ObservableProperty]
    public partial double? SelectedExactFrameRate { get; set; }

    [ObservableProperty]
    public partial string? SelectedExactFrameRateArg { get; set; }

    [ObservableProperty]
    public partial string DisabledResolutionReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisabledFrameRateReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? SourceWidth { get; set; }

    [ObservableProperty]
    public partial int? SourceHeight { get; set; }

    [ObservableProperty]
    public partial bool? SourceIsHdr { get; set; }

    [ObservableProperty]
    public partial string SourceTelemetryAvailability { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string SourceTelemetryOriginDetail { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string SourceTelemetryConfidence { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string? SourceTelemetryDiagnosticSummary { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? SourceTelemetryTimestampUtc { get; set; }

    [ObservableProperty]
    public partial string SourceTelemetrySummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SourceTargetSummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAudioEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewActive { get; set; }

    [ObservableProperty]
    public partial double PreviewVolume { get; set; } = 1.0;

    [ObservableProperty]
    public partial double MicrophoneVolume { get; set; } = 100.0;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableDeviceAudioModes { get; set; } = new()
    {
        DeviceAudioMode.Hdmi,
        DeviceAudioMode.Analog
    };

    [ObservableProperty]
    public partial bool IsDeviceAudioControlSupported { get; set; }

    [ObservableProperty]
    public partial string SelectedDeviceAudioMode { get; set; } = DeviceAudioMode.Hdmi;

    [ObservableProperty]
    public partial double AnalogAudioGainPercent { get; set; } = 50;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    [ObservableProperty]
    public partial double AudioPeak { get; set; }

    [ObservableProperty]
    public partial bool AudioClipping { get; set; }

    [ObservableProperty]
    public partial bool IsFlashbackEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFlashbackTimelineVisible { get; set; }

    [ObservableProperty]
    public partial FlashbackPlaybackState FlashbackState { get; set; } = FlashbackPlaybackState.Disabled;

    [ObservableProperty]
    public partial double FlashbackBufferFillPercent { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackBufferFilledDuration { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackPlaybackPosition { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackGapFromLive { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackInPoint { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackOutPoint { get; set; }

    [ObservableProperty]
    public partial long FlashbackBufferDiskBytes { get; set; }

    [ObservableProperty]
    public partial string FlashbackBitrateInfo { get; set; } = "";

    [ObservableProperty]
    public partial double FlashbackExportProgress { get; set; }

    [ObservableProperty]
    public partial bool IsFlashbackExporting { get; set; }

    [ObservableProperty]
    public partial bool IsDiskWarningActive { get; set; }

    partial void OnIsFlashbackEnabledChanged(bool value)
    {
        if (!value)
        {
            IsFlashbackTimelineVisible = false;
        }
    }

    private int _disposeState;
    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;
    private Task? _pendingFlashbackCycleTask;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;
}
