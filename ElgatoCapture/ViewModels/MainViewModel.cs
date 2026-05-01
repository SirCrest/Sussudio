using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ElgatoCapture.Models;
using Microsoft.UI.Dispatching;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel
{
    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly CaptureSessionCoordinator _sessionCoordinator;
    private readonly NativeXuAudioControlService _deviceAudioControlService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly AudioDeviceWatcher _audioDeviceWatcher;
    private readonly Stopwatch _recordingStopwatch = new();
    private DispatcherQueueTimer? _timer;
    private IntPtr _windowHandle;
    private readonly Queue<(long Tick, long Bytes)> _bitrateSamples = new();
    private readonly Queue<(long Tick, long Bytes)> _flashbackBitrateSamples = new();
    private const int BitrateWindowMs = 5000;
    private const string DefaultRecordingFormat = "H.264";
    private const string HevcRecordingFormat = "HEVC";
    private const string Av1RecordingFormat = "AV1";
    private const int DefaultDisposeTimeoutMs = 30000;
    private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;
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

    // Resolution/Frame Rate separation
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
    private List<string> _detectedRecordingFormats = new();
    private long _deviceScanGeneration;

    // Flag to prevent reinitialization during initial device setup
    private bool _isChangingDevice;
    private bool _suppressFormatChangeReinitialize;
    private bool _isRevertingHdrToggle;
    private int _recordingToggleInProgress;
    // Holds the in-flight ToggleRecordingAsync task so the window-close path can
    // observe (and await) an already-running stop instead of short-circuiting on
    // the CAS gate above. Cleared in the toggle's finally block.
    private volatile Task? _activeRecordingToggleTask;
    private int _activeRecordingTransitionTarget = -1;
    private bool _isLoadingSettings;
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
    private int _flashbackSettingsRestartGeneration;
    private bool _suppressMicrophoneMonitorUpdate;
    [ObservableProperty]
    public partial bool IsRecordingTransitioning { get; set; }

    [ObservableProperty]
    public partial bool IsFfmpegMissing { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableRecordingFormats { get; set; } =
        new() { DefaultRecordingFormat, HevcRecordingFormat, Av1RecordingFormat };

    [ObservableProperty]
    public partial string SelectedRecordingFormat { get; set; } = DefaultRecordingFormat;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableQualities { get; set; } = new() { "Auto", "Low", "Medium", "High", "Super High", "Custom" };

    [ObservableProperty]
    public partial string SelectedQuality { get; set; } = "Medium";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailablePresets { get; set; } = new()
    {
        "Auto", "P1", "P2", "P3", "P4", "P5", "P6", "P7"
    };

    [ObservableProperty]
    public partial string SelectedPreset { get; set; } = "Auto";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableSplitEncodeModes { get; set; } = new()
    {
        "Auto", "Disabled", "2-way", "3-way"
    };

    [ObservableProperty]
    public partial string SelectedSplitEncodeMode { get; set; } = "Auto";

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
    public partial double CustomBitrateMbps { get; set; } = 50;

    [ObservableProperty]
    public partial bool IsCustomBitrateVisible { get; set; }

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
    public partial string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string RecordingTime { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string RecordingSizeInfo { get; set; } = "--";

    [ObservableProperty]
    public partial string RecordingBitrateInfo { get; set; } = "--";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

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

    // Flashback timeline properties
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

    /// <summary>
    /// Written by WASAPI callback thread via Volatile.Write, read by UI timer.
    /// Bypasses PropertyChanged to avoid per-frame dispatch + 53-case switch overhead.
    /// </summary>
    public double AudioMeterTarget;
    public double MicrophoneMeterTarget;
    public bool MicrophoneClipping { get; set; }
    private int _audioMeterTimerNeeded;
    private int _microphoneMeterTimerNeeded;

    /// <summary>
    /// Fires once when audio transitions from silent to active, signaling MainWindow
    /// to start the audio meter animation timer. Reset when the timer stops itself.
    /// </summary>
    public event Action? AudioMeterActivated;
    public event Action? MicrophoneMeterActivated;

    private const double MeterFloorDb = -60.0;
    private const double MeterDecayDbPerSecond = 40.0 / 1.7; // OBS-like PPM decay
    private double _audioMeterDb = MeterFloorDb;
    private long _audioMeterLastTick;
    private double _micMeterDb = MeterFloorDb;
    private long _micMeterLastTick;
    private int _disposeState;
    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private bool _cancelPreviewRestartAfterReinitialize;
    private Task? _pendingFlashbackCycleTask;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;

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
    {
        _deviceService = new DeviceService();
        _deviceService.FormatProbeCompleted += OnDeviceFormatProbeCompleted;
        _captureService = new CaptureService();
        _sessionCoordinator = new CaptureSessionCoordinator(_captureService);
        _deviceAudioControlService = new NativeXuAudioControlService();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.PreCleanupRequested += OnCapturePreCleanupRequested;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;
        _captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;
        _latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);
        UpdateHdrRuntimeStatusFromCapture();
        UpdateLiveCaptureInfo();

        _audioDeviceWatcher = new AudioDeviceWatcher();
        _audioDeviceWatcher.DevicesChanged += OnAudioDevicesChanged;

        SetupTimer();
        UpdateDiskSpace();
    }
    private bool EnqueueUiOperation(Func<Task> operation, string operationName, bool allowDuringDispose = false)
    {
        if (!allowDuringDispose && Volatile.Read(ref _disposeState) != 0)
        {
            Logger.Log($"UI_OPERATION_SKIP op='{operationName}' reason=disposing");
            return false;
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            if (!allowDuringDispose && Volatile.Read(ref _disposeState) != 0)
            {
                Logger.Log($"UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
                return;
            }

            _ = ExecuteUiOperationAsync(operation, operationName);
        });
        if (!enqueued)
        {
            Logger.Log($"UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        }

        return enqueued;
    }

    private async Task ExecuteUiOperationAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"{operationName} failed: {ex.Message}";
        }
    }

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
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return operation();
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                registration.Dispose();
                registration = default;

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                await operation();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            Logger.Log("INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
            completion.TrySetException(new InvalidOperationException("Failed to enqueue UI operation."));
        }

        return completion.Task;
    }

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return Task.FromResult(operation());
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                registration.Dispose();
                registration = default;

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                completion.TrySetResult(operation());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            Logger.Log("INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
            completion.TrySetException(new InvalidOperationException("Failed to enqueue UI operation."));
        }

        return completion.Task;
    }

    private static async Task AwaitWithTimeoutAsync(Task task, int timeoutMs, string operationName)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"{operationName} timed out after {timeoutMs} ms.");
        }

        await task.ConfigureAwait(false);
    }

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    private void SetupTimer()
    {
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();

            if (IsRecording)
            {
                RecordingTime = _recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                UpdateRecordingStats();
            }

            if (!IsRecording && _captureService.IsFlashbackActive)
            {
                UpdateFlashbackBitrate();
            }

            if (IsPreviewing || IsRecording)
            {
                UpdateLiveCaptureInfo(runtimeSnapshot);
            }
            else
            {
                ResetLiveCaptureInfo();
            }

            UpdateDiskSpace();
            RefreshSourceTelemetrySummaryAge();
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        };
        _timer.Start();
    }

    private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        IsAudioPreviewActive = runtime.IsAudioPreviewActive;

        var width = runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth;
        var height = runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight;
        LiveResolution = width.HasValue && height.HasValue
            ? $"{width.Value}x{height.Value}"
            : LiveInfoUnavailable;

        var frameRateValue = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate;
        if (frameRateValue.HasValue && frameRateValue.Value > 0)
        {
            LiveFrameRate = frameRateValue.Value.ToString("0.00");
        }
        else
        {
            LiveFrameRate = LiveInfoUnavailable;
        }

        var pixelFormat =
            runtime.ReaderSourceSubtype ??
            runtime.VideoNegotiatedSubtype ??
            runtime.NegotiatedPixelFormat ??
            runtime.LatestObservedFramePixelFormat ??
            runtime.RequestedReaderSubtype ??
            runtime.RequestedPixelFormat;
        var codecSuffix = _captureService.EncoderCodecName switch
        {
            "hevc_nvenc" => " / HEVC",
            "h264_nvenc" => " / H264",
            "av1_nvenc" => " / AV1",
            _ => ""
        };
        LivePixelFormat = string.IsNullOrWhiteSpace(pixelFormat) ? LiveInfoUnavailable : pixelFormat + codecSuffix;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    private void UpdateDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(OutputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            DiskSpaceInfo = $"Free: {freeGb:F1} GB";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}");
            DiskSpaceInfo = "";
        }
    }

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = status;
            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        }
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = $"Error: {ex.Message}";
            IsInitialized = _captureService.IsInitialized;
            IsPreviewing = _captureService.IsVideoPreviewActive;
            IsRecording = _captureService.IsRecording;
            if (!IsPreviewing && !IsRecording)
            {
                ResetAudioMeter();
            }

            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void OnCapturePreCleanupRequested()
    {
        // Fires on a background thread before CaptureService.CleanupAsync disposes
        // the shared D3D11 device. Stop the renderer first to prevent the same race
        // as the reinit crash (renderer calling native D3D on a dying device).
        var handlers = PreviewRendererStopRequested;
        if (handlers != null)
        {
            foreach (Func<Task> handler in handlers.GetInvocationList())
            {
                try { handler().GetAwaiter().GetResult(); }
                catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
            }
        }
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _audioMeterDb, ref _audioMeterLastTick);
        Volatile.Write(ref AudioMeterTarget, level);
        AudioPeak = e.Peak;

        if (level > 0 && Interlocked.CompareExchange(ref _audioMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => AudioMeterActivated?.Invoke());
        }

        if (e.Clipped)
        {
            _dispatcherQueue.TryEnqueue(() => AudioClipping = true);
        }
    }

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _micMeterDb, ref _micMeterLastTick);
        Volatile.Write(ref MicrophoneMeterTarget, level);
        MicrophoneClipping = e.Clipped;

        if (level > 0 && Interlocked.CompareExchange(ref _microphoneMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => MicrophoneMeterActivated?.Invoke());
        }
    }

    partial void OnIsMicrophoneEnabledChanged(bool value)
    {
        SaveSettings();
        if (_suppressMicrophoneMonitorUpdate)
        {
            return;
        }

        if (!IsRecording)
        {
            var device = SelectedMicrophoneDevice;
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(value, device?.Id, device?.Name),
                "mic monitor toggle");
        }
    }

    partial void OnIsCustomAudioInputEnabledChanged(bool value)
    {
        if (IsRecording)
        {
            Logger.Log("Custom audio input change ignored while recording");
            return;
        }

        if (value)
        {
            if (AudioInputDevices.Count == 0)
            {
                Logger.Log("Custom audio input enabled but no audio devices found");
                IsCustomAudioInputEnabled = false;
                return;
            }

            if (SelectedAudioInputDevice == null)
            {
                SelectedAudioInputDevice = AudioInputDevices[0];
            }
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio toggle"), "custom audio toggle");
        SaveSettings();
    }

    partial void OnSelectedAudioInputDeviceChanged(AudioInputDevice? value)
    {
        if (IsRecording)
        {
            return;
        }

        if (!IsCustomAudioInputEnabled || value == null)
        {
            return;
        }

        EnqueueUiOperation(() => ApplyAudioInputSelectionAsync("custom audio device change"), "custom audio device change");
        SaveSettings();
    }

    partial void OnSelectedMicrophoneDeviceChanged(AudioInputDevice? value)
    {
        if (value != null)
        {
            try
            {
                var pendingSavedVolume = _pendingSavedMicrophoneVolume;
                var pendingSavedVolumeDeviceId = _pendingSavedMicrophoneVolumeDeviceId;
                if (pendingSavedVolume.HasValue &&
                    string.Equals(value.Id, pendingSavedVolumeDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var savedVolume = Math.Clamp(pendingSavedVolume.Value, 0.0, 100.0);
                    if (Math.Abs(MicrophoneVolume - savedVolume) > 0.5)
                    {
                        MicrophoneVolume = savedVolume;
                    }
                    else
                    {
                        SetMicrophoneEndpointVolume(savedVolume);
                    }
                }
                else
                {
                    _pendingSavedMicrophoneVolume = null;
                    _pendingSavedMicrophoneVolumeDeviceId = null;
                    var endpointVolume = GetMicrophoneEndpointVolume();
                    if (Math.Abs(MicrophoneVolume - endpointVolume) > 0.5)
                    {
                        MicrophoneVolume = endpointVolume;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainViewModel mic volume readback: {ex.Message}");
            }
        }

        SaveSettings();

        // Update mic monitoring when device changes
        if (IsMicrophoneEnabled && !IsRecording && value != null)
        {
            EnqueueUiOperation(
                () => _sessionCoordinator.UpdateMicrophoneMonitorAsync(true, value.Id, value.Name),
                "mic monitor device switch");
        }
    }

    partial void OnSelectedDeviceAudioModeChanged(string value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Device audio mode change ignored while recording");
            return;
        }
        var oldCts = _deviceAudioModeCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var targetDevice = SelectedDevice;
        _deviceAudioModeCts = cts;
        var enqueued = EnqueueUiOperation(async () =>
        {
            try
            {
                if (Volatile.Read(ref _disposeState) == 0)
                {
                    await ApplyDeviceAudioModeAsync("device audio mode change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Device audio mode change canceled because selected device changed");
            }
            finally
            {
                if (ReferenceEquals(_deviceAudioModeCts, cts))
                {
                    _deviceAudioModeCts = null;
                }

                cts.Dispose();
            }
        }, "device audio mode change", allowDuringDispose: true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioModeCts, cts))
            {
                _deviceAudioModeCts = null;
            }

            cts.Dispose();
        }
        SaveSettings();
    }

    partial void OnAnalogAudioGainPercentChanged(double value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Analog audio gain change ignored while recording");
            return;
        }

        if (!string.Equals(SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            SaveSettings();
            return;
        }

        // Debounce the XU write to avoid flooding the hardware with commands
        // while the user drags the slider (same hazard class as AT SET bricking).
        var targetDevice = SelectedDevice;
        if (targetDevice == null)
        {
            SaveSettings();
            return;
        }
        var oldCts = _gainXuDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainXuDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token).ConfigureAwait(false);
                var enqueued = EnqueueUiOperation(async () =>
                {
                    try
                    {
                        if (Volatile.Read(ref _disposeState) == 0)
                        {
                            await ApplyAnalogAudioGainAsync("analog audio gain change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log("Analog audio gain change canceled because selected device changed");
                    }
                    finally
                    {
                        if (ReferenceEquals(_gainXuDebounceCts, cts))
                        {
                            _gainXuDebounceCts = null;
                        }

                        cts.Dispose();
                    }
                }, "analog audio gain change", allowDuringDispose: true);
                if (!enqueued)
                {
                    if (ReferenceEquals(_gainXuDebounceCts, cts))
                    {
                        _gainXuDebounceCts = null;
                    }

                    cts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(_gainXuDebounceCts, cts))
                {
                    _gainXuDebounceCts = null;
                }

                cts.Dispose();
            }
        });
        SaveSettings();
    }

    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");
        var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);

        if (value)
        {
            // Re-enable audio preview and start it if we're already previewing
            IsAudioPreviewEnabled = true;
            if (IsPreviewing && IsInitialized)
            {
                EnqueueUiOperation(async () =>
                {
                    if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled)
                    {
                        Logger.Log($"AUDIO_TOGGLE_SKIP op=enable stale_generation={changeGeneration}");
                        return;
                    }

                    await _sessionCoordinator.StartAudioPreviewAsync();
                    // Cycle the flashback encoder so it reconnects its audio feed.
                    // Without this, the first recording after audio off->on produces
                    // an empty file because the flashback sink's audio path is stale.
                    await _sessionCoordinator.RestartFlashbackAsync(BuildCaptureSettings());
                }, "audio preview restart + flashback cycle");
            }
        }
        else
        {
            if (IsAudioPreviewEnabled)
            {
                IsAudioPreviewEnabled = false;
            }

            // Delay teardown so the 300ms WASAPI volume ramp to silence completes first
            EnqueueUiOperation(async () =>
            {
                await Task.Delay(350);
                if (changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled)
                {
                    Logger.Log($"AUDIO_TOGGLE_SKIP op=disable stale_generation={changeGeneration}");
                    return;
                }

                await _sessionCoordinator.StopAudioPreviewWithTeardownAsync();
            }, "audio capture teardown");

            ResetAudioMeter();
        }

        SaveSettings();
    }

    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _bitrateSamples.Clear();

            if (_pendingModeOptionsRefresh)
            {
                _pendingModeOptionsRefresh = false;
                RebuildResolutionOptions();
            }
        }
    }

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }

    private void ResetAudioMeter()
    {
        _audioMeterDb = MeterFloorDb;
        _audioMeterLastTick = 0;
        _micMeterDb = MeterFloorDb;
        _micMeterLastTick = 0;
        AudioPeak = 0;
        Volatile.Write(ref AudioMeterTarget, 0.0);
        Volatile.Write(ref MicrophoneMeterTarget, 0.0);
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
        AudioClipping = false;
        MicrophoneClipping = false;
    }

    public void ResetAudioMeterTimerFlag()
    {
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
    }

    private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)
    {
        var targetDb = peak > 0 ? 20.0 * Math.Log10(peak) : MeterFloorDb;
        if (targetDb < MeterFloorDb) targetDb = MeterFloorDb;
        if (targetDb > 0) targetDb = 0;

        var nowTick = Environment.TickCount64;
        if (lastTick == 0)
        {
            meterDb = targetDb;
            lastTick = nowTick;
        }
        else
        {
            var dtSeconds = Math.Max(0, (nowTick - lastTick) / 1000.0);
            lastTick = nowTick;

            if (targetDb >= meterDb)
            {
                meterDb = targetDb;
            }
            else
            {
                var decay = MeterDecayDbPerSecond * dtSeconds;
                meterDb = Math.Max(targetDb, meterDb - decay);
            }
        }

        var level = (meterDb - MeterFloorDb) / -MeterFloorDb;
        return Math.Clamp(level, 0, 1);
    }

    private void UpdateRecordingStats()
    {
        var stats = _captureService.GetRecordingStats();
        var totalBytes = stats.TotalBytes;
        RecordingSizeInfo = FormatBytes(totalBytes);

        var now = Environment.TickCount64;
        _bitrateSamples.Enqueue((now, totalBytes));
        while (_bitrateSamples.Count > 0 && now - _bitrateSamples.Peek().Tick > BitrateWindowMs)
        {
            _bitrateSamples.Dequeue();
        }

        if (_bitrateSamples.Count >= 2)
        {
            var first = _bitrateSamples.Peek();
            var last = _bitrateSamples.Last();
            var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
            var deltaSeconds = Math.Max(0.001, (last.Tick - first.Tick) / 1000.0);
            var bitsPerSecond = (deltaBytes * 8.0) / deltaSeconds;
            RecordingBitrateInfo = FormatBitrate(bitsPerSecond);
        }
        else
        {
            RecordingBitrateInfo = "--";
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double scale = 1024;
        double value = Math.Max(0, bytes);
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var unit = 0;
        while (value >= scale && unit < units.Length - 1)
        {
            value /= scale;
            unit++;
        }
        return $"{Math.Round(value):0} {units[unit]}";
    }

    private static string FormatBitrate(double bitsPerSecond)
    {
        if (bitsPerSecond <= 0)
        {
            return "0 bps";
        }

        string[] units = { "bps", "Kbps", "Mbps", "Gbps" };
        var unit = 0;
        while (bitsPerSecond >= 1000 && unit < units.Length - 1)
        {
            bitsPerSecond /= 1000;
            unit++;
        }
        return $"{Math.Round(bitsPerSecond):0} {units[unit]}";
    }

    // -- Capture lifecycle methods are in MainViewModel.Capture.cs -----
    // -- Automation methods are in MainViewModel.Automation.cs ---------

    // -- Partial class references ----
    // Capture lifecycle: MainViewModel.Capture.cs
    // Automation / flashback: MainViewModel.Automation.cs
    // Audio controls: MainViewModel.AudioControls.cs
    // Device management: MainViewModel.DeviceManagement.cs
    // Source telemetry: MainViewModel.Telemetry.cs
    // Settings persistence: MainViewModel.Settings.cs

    private async Task DisposeCoreAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 1)
        {
            return;
        }
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
        _gainFlashDebounceCts?.Cancel();
        _gainXuDebounceCts?.Cancel();
        _deviceAudioModeCts?.Cancel();
        _deviceAudioRefreshCts?.Cancel();
        _timer?.Stop();
        _deviceService.FormatProbeCompleted -= OnDeviceFormatProbeCompleted;
        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.PreCleanupRequested -= OnCapturePreCleanupRequested;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.AudioLevelUpdated -= OnAudioLevelUpdated;
        _captureService.MicrophoneAudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated -= OnSourceTelemetryUpdated;
        _audioDeviceWatcher.DevicesChanged -= OnAudioDevicesChanged;
        _audioDeviceWatcher.Dispose();
        var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

        try
        {
            await AwaitWithTimeoutAsync(_sessionCoordinator.CleanupAsync(), stepTimeoutMs, "Coordinator cleanup")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel cleanup during dispose failed: {ex.Message}");
        }

        try
        {
            await AwaitWithTimeoutAsync(_sessionCoordinator.DisposeAsync().AsTask(), stepTimeoutMs, "Coordinator dispose")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Coordinator dispose failed: {ex.Message}");
        }

        try
        {
            await AwaitWithTimeoutAsync(_captureService.DisposeAsync().AsTask(), stepTimeoutMs, "Capture service dispose")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Capture service async dispose failed: {ex.Message}");
            _captureService.Dispose();
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback — MainWindow.Closed calls
    // await ViewModel.DisposeAsync(). This sync path exists for GC finalizer safety
    // and uses Task.Run to avoid deadlocking if called from a UI context.
    public void Dispose()
    {
        var disposeTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);
        var disposeTask = Task.Run(DisposeCoreAsync);
        var completed = Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).GetAwaiter().GetResult();
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            disposeTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);
        var disposeTask = DisposeCoreAsync();
        var completed = await Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).ConfigureAwait(false);
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel async dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            await disposeTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel async dispose failed: {ex.Message}");
        }
    }
}
