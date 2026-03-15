using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable
{
    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly ICaptureSessionCoordinator _sessionCoordinator;
    private readonly NativeXuAudioControlService _deviceAudioControlService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Stopwatch _recordingStopwatch = new();
    private DispatcherQueueTimer? _timer;
    private IntPtr _windowHandle;
    private readonly Queue<(long Tick, long Bytes)> _bitrateSamples = new();
    private const int BitrateWindowMs = 5000;
    private const string DefaultRecordingFormat = "H.264 (MP4)";
    private const string HevcRecordingFormat = "HEVC (MP4)";
    private const string Av1RecordingFormat = "AV1 (MP4)";
    private const int DefaultDisposeTimeoutMs = 30000;
    private const string HdrToggleBlockedWhileRecordingMessage = "Stop recording before switching between HDR and SDR pipelines.";
    private const string LiveInfoUnavailable = "\u2014";
    private const string AutoResolutionValue = "Auto";
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
    private bool _isLoadingSettings;
    private string? _pendingSavedDeviceId;
    private string? _pendingSavedAudioDeviceId;
    private string? _pendingSavedDeviceAudioMode;
    private double? _pendingSavedAnalogAudioGainPercent;
    private bool _isRefreshingDeviceAudioControls;
    private CancellationTokenSource? _gainFlashDebounceCts;
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
    public partial int MjpegDecoderCount { get; set; } = 4;

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

    /// <summary>
    /// Written by WASAPI callback thread via Volatile.Write, read by UI timer.
    /// Bypasses PropertyChanged to avoid per-frame dispatch + 53-case switch overhead.
    /// </summary>
    public double AudioMeterTarget;
    private int _audioMeterTimerNeeded;

    /// <summary>
    /// Fires once when audio transitions from silent to active, signaling MainWindow
    /// to start the audio meter animation timer. Reset when the timer stops itself.
    /// </summary>
    public event Action? AudioMeterActivated;

    private const double MeterFloorDb = -60.0;
    private const double MeterDecayDbPerSecond = 40.0 / 1.7; // OBS-like PPM decay
    private double _audioMeterDb = MeterFloorDb;
    private long _audioMeterLastTick;
    private int _disposeState;
    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private bool _cancelPreviewRestartAfterReinitialize;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;

    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRuntimeSnapshot(), cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetHealthSnapshot(), cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRecordingStats(), cancellationToken);
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();
    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath) => _captureService.CapturePreviewFrameAsync(outputPath);
    public CaptureSettings GetCurrentSettings() => BuildCaptureSettings();

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
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;
        _latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);
        UpdateHdrRuntimeStatusFromCapture();
        UpdateLiveCaptureInfo();

        SetupTimer();
        UpdateDiskSpace();
    }

    private void EnqueueUiOperation(Func<Task> operation, string operationName)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _ = ExecuteUiOperationAsync(operation, operationName);
        });
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
            completion.TrySetException(new InvalidOperationException("Failed to enqueue UI operation."));
        }

        return completion.Task;
    }

    public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() => new ViewModelRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = IsInitialized,
            IsPreviewing = IsPreviewing,
            IsRecording = IsRecording,
            IsAudioEnabled = IsAudioEnabled,
            IsAudioPreviewEnabled = IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
            StatusText = StatusText,
            SelectedDeviceId = SelectedDevice?.Id,
            SelectedDeviceName = SelectedDevice?.Name,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedAudioInputDeviceName = SelectedAudioInputDevice?.Name,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            SelectedFriendlyFrameRate = SelectedFriendlyFrameRate,
            SelectedExactFrameRate = SelectedExactFrameRate,
            SelectedExactFrameRateArg = SelectedExactFrameRateArg,
            DisabledResolutionReason = DisabledResolutionReason,
            DisabledFrameRateReason = DisabledFrameRateReason,
            HdrResolutionSupportHint = HdrResolutionSupportHint,
            DetectedSourceFrameRate = DetectedSourceFrameRate,
            DetectedSourceFrameRateArg = DetectedSourceFrameRateArg,
            SourceFrameRateOrigin = SourceFrameRateOrigin,
            SourceWidth = SourceWidth,
            SourceHeight = SourceHeight,
            SourceIsHdr = SourceIsHdr,
            SourceTelemetryAvailability = SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = SourceTelemetryDiagnosticSummary,
            SourceTelemetryTimestampUtc = SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = ComputeTelemetryAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow),
            SourceTelemetrySummaryText = SourceTelemetrySummaryText,
            SourceTargetSummaryText = SourceTargetSummaryText,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            SelectedVideoFormat = SelectedVideoFormat,
            CustomBitrateMbps = CustomBitrateMbps,
            ShowAllCaptureOptions = ShowAllCaptureOptions,
            PreviewVolumePercent = PreviewVolume * 100.0,
            IsStatsVisible = IsStatsVisible,
            IsHdrAvailable = IsHdrAvailable,
            IsHdrEnabled = IsHdrEnabled,
            HdrRuntimeState = HdrRuntimeState,
            HdrReadinessReason = HdrReadinessReason,
            LiveResolution = LiveResolution,
            LiveFrameRate = LiveFrameRate,
            LivePixelFormat = LivePixelFormat,
            OutputPath = OutputPath,
            RecordingTime = RecordingTime,
            RecordingSizeInfo = RecordingSizeInfo,
            RecordingBitrateInfo = RecordingBitrateInfo,
            AudioPeak = AudioPeak,
            AudioClipping = AudioClipping
        }, cancellationToken);
    }

    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() => new AutomationOptionsSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Devices = Devices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AudioInputDevices = AudioInputDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedAudioInputDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            Resolutions = AvailableResolutions
                .Select(option => new AutomationResolutionOption
                {
                    Value = option.Value,
                    Width = (int)option.Width,
                    Height = (int)option.Height,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            FrameRates = AvailableFrameRates
                .Select(option => new AutomationFrameRateOption
                {
                    Value = option.Value,
                    FriendlyValue = option.FriendlyValue,
                    ExactValueArg = option.Rational ?? string.Empty,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = IsFrameRateMatch(option.Value, SelectedFrameRate)
                })
                .ToArray(),
            RecordingFormats = BuildStringOptions(AvailableRecordingFormats, SelectedRecordingFormat),
            Qualities = BuildStringOptions(AvailableQualities, SelectedQuality),
            Presets = BuildStringOptions(AvailablePresets, SelectedPreset),
            SplitEncodeModes = BuildStringOptions(AvailableSplitEncodeModes, SelectedSplitEncodeMode),
            VideoFormats = BuildStringOptions(AvailableVideoFormats, SelectedVideoFormat),
            MjpegDecoderCounts = Enumerable.Range(1, 8)
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == Math.Clamp(MjpegDecoderCount, 1, 8)
                })
                .ToArray(),
            SelectedDeviceId = SelectedDevice?.Id,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            SelectedVideoFormat = SelectedVideoFormat,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8),
            ShowAllCaptureOptions = ShowAllCaptureOptions,
            PreviewVolumePercent = PreviewVolume * 100.0,
            IsStatsVisible = IsStatsVisible
        }, cancellationToken);
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

    public async Task InitializeAsync()
    {
        var formatsTask = RefreshRecordingFormatsAsync();
        var splitTask = RefreshSplitEncodeModesAsync();
        await Task.WhenAll(formatsTask, splitTask);
        LoadSettings();
    }

    partial void OnOutputPathChanged(string value)
    {
        SaveSettings();
    }

    partial void OnSelectedRecordingFormatChanged(string value)
    {
        SaveSettings();
    }

    partial void OnCustomBitrateMbpsChanged(double value)
    {
        SaveSettings();
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = SettingsService.Load();

            if (!string.IsNullOrWhiteSpace(settings.OutputPath) && Directory.Exists(settings.OutputPath))
            {
                OutputPath = settings.OutputPath;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat) &&
                AvailableRecordingFormats.Contains(settings.SelectedRecordingFormat))
            {
                SelectedRecordingFormat = settings.SelectedRecordingFormat;
            }
            else if (!string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat))
            {
                Logger.Log($"SETTINGS_LOAD: saved format '{settings.SelectedRecordingFormat}' not available, using default.");
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedQuality) &&
                AvailableQualities.Contains(settings.SelectedQuality))
            {
                SelectedQuality = settings.SelectedQuality;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedPreset) &&
                AvailablePresets.Contains(settings.SelectedPreset))
            {
                SelectedPreset = settings.SelectedPreset;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedSplitEncodeMode) &&
                AvailableSplitEncodeModes.Contains(settings.SelectedSplitEncodeMode))
            {
                SelectedSplitEncodeMode = settings.SelectedSplitEncodeMode;
            }

            if (settings.CustomBitrateMbps.HasValue)
            {
                CustomBitrateMbps = settings.CustomBitrateMbps.Value;
            }

            if (settings.IsHdrEnabled.HasValue)
            {
                IsHdrEnabled = settings.IsHdrEnabled.Value;
            }

            if (settings.IsAudioEnabled.HasValue)
            {
                IsAudioEnabled = settings.IsAudioEnabled.Value;
            }

            if (settings.IsAudioPreviewEnabled.HasValue)
            {
                IsAudioPreviewEnabled = settings.IsAudioPreviewEnabled.Value;
            }

            if (settings.IsCustomAudioInputEnabled.HasValue)
            {
                IsCustomAudioInputEnabled = settings.IsCustomAudioInputEnabled.Value;
            }

            if (settings.PreviewVolume.HasValue)
            {
                PreviewVolume = Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0);
            }

            if (settings.ShowAllCaptureOptions.HasValue)
            {
                ShowAllCaptureOptions = settings.ShowAllCaptureOptions.Value;
            }

            if (settings.IsStatsVisible.HasValue)
            {
                IsStatsVisible = settings.IsStatsVisible.Value;
            }

            if (!string.IsNullOrWhiteSpace(settings.SelectedDeviceAudioMode) &&
                AvailableDeviceAudioModes.Contains(settings.SelectedDeviceAudioMode, StringComparer.OrdinalIgnoreCase))
            {
                SelectedDeviceAudioMode = settings.SelectedDeviceAudioMode;
            }

            if (settings.AnalogAudioGainPercent.HasValue)
            {
                AnalogAudioGainPercent = Math.Clamp(settings.AnalogAudioGainPercent.Value, 0.0, 100.0);
            }

            // Defer device selection until RefreshDevicesAsync populates the device list
            _pendingSavedDeviceId = settings.SelectedDeviceId;
            _pendingSavedAudioDeviceId = settings.SelectedAudioInputDeviceId;
            _pendingSavedDeviceAudioMode = settings.SelectedDeviceAudioMode;
            _pendingSavedAnalogAudioGainPercent = settings.AnalogAudioGainPercent;
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_LOAD: unexpected error: {ex.Message}");
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void SaveSettings()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        try
        {
            var settings = new UserSettings
            {
                SelectedDeviceId = SelectedDevice?.Id,
                OutputPath = OutputPath,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                CustomBitrateMbps = CustomBitrateMbps,
                IsHdrEnabled = IsHdrEnabled,
                IsAudioEnabled = IsAudioEnabled,
                IsAudioPreviewEnabled = IsAudioPreviewEnabled,
                IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                PreviewVolume = VolumeSaveOverride ?? PreviewVolume,
                ShowAllCaptureOptions = ShowAllCaptureOptions,
                IsStatsVisible = IsStatsVisible,
                SelectedDeviceAudioMode = SelectedDeviceAudioMode,
                AnalogAudioGainPercent = AnalogAudioGainPercent,
            };

            SettingsService.Save(settings);
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_SAVE: unexpected error: {ex.Message}");
        }
    }

    private async Task RefreshRecordingFormatsAsync()
    {
        var support = await FFmpegEncoderService.GetEncoderSupportAsync();
        var formats = new List<string>();

        if (support.HasH264)
        {
            formats.Add("H.264 (MP4)");
        }

        if (support.HasHevc)
        {
            formats.Add("HEVC (MP4)");
        }

        if (support.HasAv1)
        {
            formats.Add("AV1 (MP4)");
        }

        void ApplyFormats()
        {
            _detectedRecordingFormats = formats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            IsFfmpegMissing = _detectedRecordingFormats.Count == 0;
            if (IsFfmpegMissing)
            {
                Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
            }
            RebuildRecordingFormatOptions();
            Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyFormats();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(ApplyFormats);
        }
    }

    private async Task RefreshSplitEncodeModesAsync()
    {
        var support = await FFmpegEncoderService.GetSplitEncodeSupportAsync();
        var modes = new List<string> { "Auto", "Disabled", "2-way", "3-way" };

        void ApplyModes()
        {
            AvailableSplitEncodeModes.Clear();
            foreach (var mode in modes)
            {
                AvailableSplitEncodeModes.Add(mode);
            }
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyModes();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(ApplyModes);
        }
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

    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
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
        LivePixelFormat = string.IsNullOrWhiteSpace(pixelFormat) ? LiveInfoUnavailable : pixelFormat;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    private void RefreshSourceTelemetrySummaryAge()
    {
        var ageSeconds = ComputeTelemetryAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
        var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
        if (_lastTelemetryAgeBucket.HasValue &&
            ageBucket.HasValue &&
            _lastTelemetryAgeBucket.Value == ageBucket.Value)
        {
            return;
        }

        var refreshedSummary = BuildSourceTelemetrySummaryText(_latestSourceTelemetry, DateTimeOffset.UtcNow);
        if (!string.Equals(SourceTelemetrySummaryText, refreshedSummary, StringComparison.Ordinal))
        {
            SourceTelemetrySummaryText = refreshedSummary;
        }

        _lastTelemetryAgeBucket = ageBucket;
    }

    private void UpdateDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(OutputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            DiskSpaceInfo = $"Free: {freeGb:F1} GB";
        }
        catch
        {
            DiskSpaceInfo = "";
        }
    }

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = status;
            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        });
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        _dispatcherQueue.TryEnqueue(() =>
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
        });
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak);
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

    private void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ApplySourceTelemetrySnapshot(snapshot, allowAutoRetarget: true);
        });
    }

    private void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)
    {
        _latestSourceTelemetry = snapshot;
        SourceWidth = snapshot.Width;
        SourceHeight = snapshot.Height;
        SourceIsHdr = snapshot.IsHdr;
        if (!IsRecording && IsHdrEnabled && snapshot.IsHdr == false)
        {
            IsHdrEnabled = false;
        }
        SourceTelemetryAvailability = snapshot.Availability.ToString();
        SourceTelemetryOriginDetail = snapshot.OriginDetail;
        SourceTelemetryConfidence = snapshot.Confidence.ToString();
        SourceTelemetryDiagnosticSummary = snapshot.DiagnosticSummary;
        SourceTelemetryTimestampUtc = snapshot.TimestampUtc;
        DetectedSourceFrameRate = snapshot.FrameRateExact;
        DetectedSourceFrameRateArg = snapshot.FrameRateArg;
        SourceFrameRateOrigin = snapshot.Origin != SourceTelemetryOrigin.Unknown
            ? snapshot.Origin.ToString()
            : "Unknown";
        _lastTelemetryAgeBucket = null;
        SourceTelemetrySummaryText = BuildSourceTelemetrySummaryText(snapshot, DateTimeOffset.UtcNow);

        var modeKey = snapshot.GetModeKey();
        if (!string.IsNullOrWhiteSpace(modeKey) &&
            !string.Equals(modeKey, _lastSourceModeKey, StringComparison.Ordinal))
        {
            if (allowAutoRetarget)
            {
                var shouldAutoRetargetResolution =
                    IsAutoResolutionValue(SelectedResolution) ||
                    !_hasUserOverriddenResolutionForCurrentMode;
                var shouldAutoRetargetFrameRate =
                    IsAutoFrameRateSelected ||
                    !_hasUserOverriddenFrameRateForCurrentMode;
                _lastSourceModeKey = modeKey;
                _forceSourceAutoRetarget = shouldAutoRetargetResolution || shouldAutoRetargetFrameRate;
                if (shouldAutoRetargetResolution)
                {
                    _hasUserOverriddenResolutionForCurrentMode = false;
                }

                if (shouldAutoRetargetFrameRate)
                {
                    _hasUserOverriddenFrameRateForCurrentMode = false;
                }
            }
        }

        var shouldRebuildModeOptions = allowAutoRetarget &&
                                       (_forceSourceAutoRetarget ||
                                        (snapshot.HasSignalData && AvailableResolutions.Count == 0));
        if (shouldRebuildModeOptions)
        {
            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        else
        {
            UpdateTargetSummary();
        }
    }

    private static string BuildSourceTelemetrySummaryText(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)
    {
        if (!snapshot.HasSignalData &&
            snapshot.Availability is Models.SourceTelemetryAvailability.Unavailable or Models.SourceTelemetryAvailability.Unknown)
        {
            return "Source: waiting for signal telemetry";
        }

        var resolution = snapshot.HasDimensions
            ? $"{snapshot.Width}x{snapshot.Height}"
            : "?x?";
        var fps = snapshot.FrameRateArg ??
                  snapshot.FrameRateExact?.ToString("0.###") ??
                  "?";
        var hdr = snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? "HDR" : "SDR") : "HDR?";
        var ageText = BuildTelemetryAgeText(snapshot.TimestampUtc, nowUtc);
        return $"Source: {resolution} @ {fps} | {hdr} | {snapshot.Availability}/{snapshot.Confidence} | {ageText}";
    }

    private static string BuildTelemetryAgeText(DateTimeOffset timestampUtc, DateTimeOffset nowUtc)
    {
        var ageSeconds = ComputeTelemetryAgeSeconds(timestampUtc, nowUtc);
        if (!ageSeconds.HasValue)
        {
            return "updated ?";
        }

        return ageSeconds.Value <= 0
            ? "updated now"
            : $"updated {ageSeconds.Value}s ago";
    }

    private static int? ComputeTelemetryAgeSeconds(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (!timestampUtc.HasValue)
        {
            return null;
        }

        var age = nowUtc - timestampUtc.Value;
        if (age < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Floor(age.TotalSeconds);
    }

    private void UpdateTargetSummary()
    {
        var friendly = SelectedFriendlyFrameRate ?? Math.Round(SelectedFrameRate);
        var exact = SelectedExactFrameRate ?? SelectedFrameRate;
        var exactArg = SelectedExactFrameRateArg;
        var exactText = !string.IsNullOrWhiteSpace(exactArg)
            ? exactArg
            : exact > 0
                ? exact.ToString("0.###")
                : "?";
        var hdrStateText = string.IsNullOrWhiteSpace(HdrRuntimeState) ? "Unknown" : HdrRuntimeState;
        SourceTargetSummaryText = $"Target: {GetSelectedResolutionDisplayText()} @ {friendly:0} (exact {exactText}) | HDR={hdrStateText}";
    }

    public async Task RefreshDevicesAsync()
    {
        StatusText = "Scanning for devices...";

        try
        {
            var discoveryStopwatch = Stopwatch.StartNew();
            var scanGeneration = Interlocked.Increment(ref _deviceScanGeneration);
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousDeviceId = SelectedDevice?.Id;
            var audioDevices = await _deviceService.EnumerateAudioCaptureDevicesAsync();
            var devices = await _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);
            discoveryStopwatch.Stop();

            ReplaceCollection(AudioInputDevices, audioDevices.ToList());
            var savedAudioId = _pendingSavedAudioDeviceId;
            _pendingSavedAudioDeviceId = null;
            SelectedAudioInputDevice =
                AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
                ?? (!string.IsNullOrWhiteSpace(savedAudioId) ? AudioInputDevices.FirstOrDefault(d => d.Id == savedAudioId) : null)
                ?? AudioInputDevices.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(savedAudioId) && SelectedAudioInputDevice?.Id != savedAudioId)
            {
                Logger.Log($"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.");
            }

            ReplaceCollection(Devices, devices.ToList());
            foreach (var discoveredDevice in Devices)
            {
                _deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);
            }

            var discoverySummary = _deviceService.LastDiscoverySummary;
            Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

            if (Devices.Count > 0)
            {
                StatusText = discoveryStopwatch.ElapsedMilliseconds <= 1500
                    ? $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms"
                    : $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms (slow scan: waiting on system device enumeration/probe startup)";

                var savedDeviceId = _pendingSavedDeviceId;
                _pendingSavedDeviceId = null;
                var nextSelectedDevice =
                    Devices.FirstOrDefault(d => d.Id == previousDeviceId)
                    ?? (!string.IsNullOrWhiteSpace(savedDeviceId) ? Devices.FirstOrDefault(d => d.Id == savedDeviceId) : null)
                    ?? Devices[0];
                if (!string.IsNullOrWhiteSpace(savedDeviceId) && nextSelectedDevice.Id != savedDeviceId)
                {
                    Logger.Log($"SETTINGS_RESTORE: saved device '{savedDeviceId}' not found, using fallback.");
                }
                SelectedDevice = nextSelectedDevice;
                Logger.Log($"Auto-selected device: {SelectedDevice?.Name}");

                // Auto-start preview (StartPreviewAsync will initialize device if needed)
                try
                {
                    await StartPreviewAsync(userInitiated: false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Auto-start preview failed after device scan: {ex.Message}");
                    StatusText = $"Preview failed to start: {ex.Message}";
                }
            }
            else
            {
                SelectedDevice = null;
                StatusText = "No compatible video capture devices found (see log for discovery summary)";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error scanning devices: {ex.Message}";
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    partial void OnSelectedDeviceChanged(CaptureDevice? value)
    {
        RebuildSelectedDeviceCapabilities(value, resetTelemetryState: true);
        EnqueueUiOperation(() => RefreshDeviceAudioControlsAsync(applySavedState: true), "device audio controls refresh");
        SaveSettings();
    }

    private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)
    {
        _isChangingDevice = true;
        try
        {
            ResetFrameRateSelectionState();
            HdrResolutionSupportHint = string.Empty;

            AvailableFormats.Clear();
            AvailableFrameRates.Clear();
            _resolutionToFormats.Clear();
            if (resetTelemetryState)
            {
                _pendingSdrAutoSelectionForDeviceChange = device != null && !IsHdrEnabled;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                ApplySourceTelemetrySnapshot(
                    SourceSignalTelemetrySnapshot.CreateUnavailable("awaiting-source-telemetry"),
                    allowAutoRetarget: false);
            }

            if (device != null)
            {
                foreach (var format in device.SupportedFormats)
                {
                    AvailableFormats.Add(format);

                    var resolutionKey = GetResolutionKey(format.Width, format.Height);
                    if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
                    {
                        formats = new List<MediaFormat>();
                        _resolutionToFormats[resolutionKey] = formats;
                    }

                    formats.Add(format);
                }

                IsHdrAvailable = device.IsHdrCapable;
                if (!IsHdrAvailable)
                {
                    IsHdrEnabled = false;
                }
            }

            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        finally
        {
            _isChangingDevice = false;
        }
    }

    private void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (e.RequestId != Interlocked.Read(ref _deviceScanGeneration))
            {
                return;
            }

            var target = Devices.FirstOrDefault(d => string.Equals(d.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return;
            }

            if (!e.Succeeded)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                Logger.Log($"Format probe failed for {e.DeviceName}: {e.Error}");
                return;
            }

            target.SupportedFormats.Clear();
            foreach (var format in e.Formats)
            {
                target.SupportedFormats.Add(new MediaFormat
                {
                    Width = format.Width,
                    Height = format.Height,
                    FrameRate = format.FrameRate,
                    FrameRateNumerator = format.FrameRateNumerator,
                    FrameRateDenominator = format.FrameRateDenominator,
                    PixelFormat = format.PixelFormat,
                    IsHdr = format.IsHdr
                });
            }

            target.IsHdrCapable = e.IsHdrCapable;

            if (SelectedDevice == null ||
                !string.Equals(SelectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var preserveActiveSelection = IsPreviewing || IsRecording;
            var allowProbeDrivenRetarget = IsPreviewing && IsInitialized && !IsRecording;
            var previousResolution = SelectedResolution;
            var previousFrameRate = SelectedFrameRate;
            Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

            if (preserveActiveSelection)
            {
                Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
            }

            _suppressFormatChangeReinitialize = preserveActiveSelection;
            try
            {
                RebuildSelectedDeviceCapabilities(SelectedDevice, resetTelemetryState: false);
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }
            Logger.Log($"Format probe rebuild done: SelectedRes={SelectedResolution} SelectedFormat={SelectedFormat?.Width}x{SelectedFormat?.Height}@{SelectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) || !IsFrameRateMatch(previousFrameRate, SelectedFrameRate)}");

            var modeChanged = !string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) ||
                              !IsFrameRateMatch(previousFrameRate, SelectedFrameRate);

            if (allowProbeDrivenRetarget &&
                IsHdrEnabled &&
                modeChanged)
            {
                Logger.Log($"Format probe updated HDR mode set; applying new mode {SelectedResolution}@{SelectedFrameRate:0.###} via device renegotiation.");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (HDR retarget)"), "format probe hdr retarget");
                return;
            }

            if (allowProbeDrivenRetarget &&
                !IsHdrEnabled &&
                SelectedFormat?.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (ShouldPreserveMjpegHighFrameRateMode(SelectedFormat))
                {
                    Logger.Log(
                        $"Format probe preserved special MJPG HFR mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                        "skipping SDR NV12 retarget.");
                    return;
                }

                var preferredRate = previousFrameRate > 0 ? previousFrameRate : SelectedFrameRate;
                var preferredBucket = GetFriendlyFrameRateBucket(preferredRate);
                var nv12Candidates = target.SupportedFormats
                    .Where(format => format.PixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                MediaFormat? selectedNv12 = nv12Candidates
                    .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredBucket)
                    .OrderByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                selectedNv12 ??= nv12Candidates
                    .OrderBy(format => Math.Abs(format.FrameRateExact - preferredRate))
                    .ThenByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                if (selectedNv12 != null)
                {
                    var targetResolution = GetResolutionKey(selectedNv12.Width, selectedNv12.Height);
                    if (!string.Equals(targetResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log(
                            $"Format probe detected MJPG-only mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                            $"retargeting SDR to NV12-capable mode {targetResolution}@{selectedNv12.FrameRateExact:0.###}.");

                        _isRebuildingModeOptions = true;
                        _isApplyingAutomaticResolutionSelection = true;
                        try
                        {
                            SelectedResolution = targetResolution;
                        }
                        finally
                        {
                            _isApplyingAutomaticResolutionSelection = false;
                            _isRebuildingModeOptions = false;
                        }

                        _suppressFormatChangeReinitialize = true;
                        try
                        {
                            RebuildFrameRateOptions();
                        }
                        finally
                        {
                            _suppressFormatChangeReinitialize = false;
                        }
                        EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (SDR nv12 retarget)"), "format probe sdr retarget");
                        return;
                    }
                }
            }

            // After probes complete, compare the live session negotiated resolution against
            // the now-resolved SelectedFormat. This catches the startup case where preview began
            // with an incomplete format list (probes not yet done) and therefore initialized at
            // a lower resolution than the user saved selection.
            if (allowProbeDrivenRetarget && SelectedFormat != null)
            {
                var runtime = GetCaptureRuntimeSnapshot();
                Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={SelectedFormat.Width}x{SelectedFormat.Height}");
                if (runtime.ActualWidth == null || runtime.ActualHeight == null)
                {
                    Logger.Log("Format probe session mismatch check skipped: runtime width/height not yet available.");
                }
                else if (runtime.ActualWidth != SelectedFormat.Width || runtime.ActualHeight != SelectedFormat.Height)
                {
                    Logger.Log(
                        $"Format probe detected session/format mismatch: " +
                        $"session={runtime.ActualWidth}x{runtime.ActualHeight} " +
                        $"selected={SelectedFormat.Width}x{SelectedFormat.Height}; reinitializing.");
                    EnqueueUiOperation(
                        () => ReinitializeDeviceAsync("format probe (session mismatch)"),
                        "format probe session mismatch");
                    return;
                }
            }

            if (preserveActiveSelection &&
                !allowProbeDrivenRetarget &&
                modeChanged &&
                !string.IsNullOrWhiteSpace(previousResolution) &&
                AvailableResolutions.Any(option => string.Equals(option.Value, previousResolution, StringComparison.OrdinalIgnoreCase)))
            {
                _isRebuildingModeOptions = true;
                _isApplyingAutomaticResolutionSelection = true;
                try
                {
                    SelectedResolution = previousResolution;
                    SelectedFrameRate = previousFrameRate;
                    UpdateSelectedFormat();
                    UpdateTargetSummary();
                }
                finally
                {
                    _isApplyingAutomaticResolutionSelection = false;
                    _isRebuildingModeOptions = false;
                }
            }
        });
    }

    partial void OnSelectedResolutionChanged(string? value)
    {
        if (TryResolveResolutionKey(value, out var resolvedResolutionKey))
        {
            _lastKnownResolutionKey = resolvedResolutionKey;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticResolutionSelection)
        {
            _hasUserOverriddenResolutionForCurrentMode = !IsAutoResolutionValue(value);
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (_isRebuildingModeOptions)
        {
            return;
        }

        _forceSourceAutoRetarget = false;
        ResetFrameRateSelectionState();
        RebuildFrameRateOptions();
        UpdateTargetSummary();
    }

    partial void OnSelectedFrameRateChanged(double value)
    {
        if (IsAutoFrameRateValue(value))
        {
            SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);
            return;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection)
        {
            IsAutoFrameRateSelected = false;
            _hasUserOverriddenFrameRateForCurrentMode = true;
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        var selected = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, value));
        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(value, MidpointRounding.AwayFromZero);
        SelectedExactFrameRate = selected?.Value ?? value;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? value;
        }

        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void UpdateSelectedFormat()
    {
        if (!TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            SelectedFormat = null;
            return;
        }

        var candidates = AvailableFormats
            .Where(f => f.Width == width && f.Height == height)
            .ToList();
        if (IsHdrEnabled)
        {
            candidates = candidates.Where(IsHdrModeCandidate).ToList();
        }

        if (candidates.Count == 0)
        {
            SelectedFormat = null;
            return;
        }

        var selectedRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : GetFriendlyFrameRateBucket(SelectedFrameRate);

        var timingFamily = ResolvePreferredTimingFamily(resolutionKey, SelectedFrameRate);
        if (selectedRateOption != null &&
            TryInferFrameRateTimingFamily(selectedRateOption.Rational, selectedRateOption.Value, out var optionFamily))
        {
            timingFamily = optionFamily;
        }

        var rateCandidates = candidates
            .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket)
            .ToList();
        if (rateCandidates.Count == 0)
        {
            rateCandidates = candidates;
        }

        SelectedFormat = SelectPreferredFrameRateFormat(rateCandidates, friendlyBucket, timingFamily);
    }

    private static bool IsHdrCompatibleRecordingFormat(string format)
        => format.Contains("HEVC", StringComparison.OrdinalIgnoreCase) ||
           format.Contains("AV1", StringComparison.OrdinalIgnoreCase);

    private void RebuildRecordingFormatOptions()
    {
        var sourceFormats = (_detectedRecordingFormats.Count > 0
            ? _detectedRecordingFormats
            : AvailableRecordingFormats.ToList())
            .ToList();
        if (sourceFormats.Count == 0)
        {
            sourceFormats.Add(DefaultRecordingFormat);
        }
        var formats = IsHdrEnabled
            ? sourceFormats.Where(IsHdrCompatibleRecordingFormat).ToList()
            : sourceFormats.ToList();
        if (formats.Count == 0 && AvailableRecordingFormats.Count > 0)
        {
            // Keep the last known real formats visible if capability refresh temporarily produced none.
            formats = AvailableRecordingFormats.ToList();
        }

        AvailableRecordingFormats.Clear();
        foreach (var format in formats)
        {
            AvailableRecordingFormats.Add(format);
        }

        string? targetFormat;
        if (IsHdrEnabled)
        {
            targetFormat = formats.FirstOrDefault(format =>
                string.Equals(format, HevcRecordingFormat, StringComparison.OrdinalIgnoreCase))
                ?? formats.FirstOrDefault(format =>
                    string.Equals(format, Av1RecordingFormat, StringComparison.OrdinalIgnoreCase))
                ?? formats.FirstOrDefault();
        }
        else
        {
            targetFormat = SelectedRecordingFormat;
            if (string.IsNullOrWhiteSpace(targetFormat) ||
                !formats.Any(format => string.Equals(format, targetFormat, StringComparison.OrdinalIgnoreCase)))
            {
                targetFormat = formats.FirstOrDefault(format =>
                    format.Contains("H.264", StringComparison.OrdinalIgnoreCase) ||
                    format.Contains("H264", StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault();
            }
        }

        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            targetFormat = DefaultRecordingFormat;
        }

        var previousSelection = SelectedRecordingFormat;
        SelectedRecordingFormat = targetFormat;
        if (string.Equals(previousSelection, targetFormat, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedRecordingFormat));
        }

        if (IsHdrEnabled && !IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
        {
            StatusText = "HDR recording requires HEVC or AV1 (10-bit).";
        }

        Logger.Log($"Selected recording format: {SelectedRecordingFormat}");
    }

    private static bool IsHdrModeCandidate(MediaFormat format)
        => format.IsHdr || MediaFormat.IsTrue10BitPixelFormat(format.PixelFormat);

    private static bool ShouldPreserveMjpegHighFrameRateMode(MediaFormat? format)
        => format != null &&
           CaptureSettings.IsMjpegHighFrameRateMode(
               format.PixelFormat,
               format.Width,
               format.Height,
               format.FrameRateExact,
               hdrEnabled: false);

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (_isRevertingHdrToggle)
        {
            return;
        }

        if (value)
        {
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (IsRecording)
        {
            _isRevertingHdrToggle = true;
            try
            {
                IsHdrEnabled = !value;
            }
            finally
            {
                _isRevertingHdrToggle = false;
            }

            StatusText = HdrToggleBlockedWhileRecordingMessage;
            return;
        }

        if (!_isChangingDevice)
        {
            _suppressFormatChangeReinitialize = true;
            try
            {
                ResetModeSelectionState();
                RebuildResolutionOptions();
                RebuildRecordingFormatOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            if (IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)
            {
                Logger.Log($"HDR toggle changed to {(value ? "On" : "Off")} - forcing immediate device renegotiation");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("HDR toggle"), "hdr toggle reinitialize");
            }
        }

        SaveSettings();
    }

    partial void OnShowAllCaptureOptionsChanged(bool value)
    {
        if (IsRecording)
        {
            _pendingModeOptionsRefresh = true;
            SaveSettings();
            return;
        }

        _pendingModeOptionsRefresh = false;
        RebuildResolutionOptions();
        SaveSettings();
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        SaveSettings();
    }

    private void RebuildResolutionOptions()
    {
        var previousSelection = SelectedResolution;
        var previousRate = SelectedFrameRate;
        var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
            ? previousSelection
            : _lastKnownResolutionKey;
        var options = _resolutionToFormats
            .Select(entry =>
            {
                var formats = entry.Value;
                var first = formats[0];
                var hdrSupported = formats.Any(IsHdrModeCandidate);
                var enabled = !IsHdrEnabled || hdrSupported;
                return new ResolutionOption
                {
                    Value = entry.Key,
                    Width = first.Width,
                    Height = first.Height,
                    IsEnabled = enabled,
                    DisableReason = enabled
                        ? string.Empty
                        : "HDR mode is not supported at this resolution."
                };
            })
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();

        if (!ShowAllCaptureOptions &&
            _latestSourceTelemetry.HasDimensions)
        {
            options = options
                .Where(DoesResolutionMatchSourceAspectRatio)
                .ToList();
        }

        var autoSelection = ResolveAutoCaptureSelection(options);
        var autoOption = options.Count > 0
            ? CreateAutoResolutionOption()
            : null;

        if (options.Count == 0)
        {
            if (SelectedDevice != null && IsPreviewing && AvailableResolutions.Count > 0)
            {
                var retainedSelection = AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    ?? AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                    ?? AvailableResolutions.FirstOrDefault();
                if (retainedSelection != null)
                {
                    _isRebuildingModeOptions = true;
                    _isApplyingAutomaticResolutionSelection = true;
                    try
                    {
                        var previousSelectedResolution = SelectedResolution;
                        SelectedResolution = retainedSelection.Value;
                        if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            OnPropertyChanged(nameof(SelectedResolution));
                        }

                        if (TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                        {
                            _lastKnownResolutionKey = retainedResolutionKey;
                        }
                    }
                    finally
                    {
                        _isApplyingAutomaticResolutionSelection = false;
                        _isRebuildingModeOptions = false;
                    }
                }

                RebuildFrameRateOptions();
                UpdateTargetSummary();
                return;
            }

            _isRebuildingModeOptions = true;
            try
            {
                AvailableResolutions.Clear();
                _isApplyingAutomaticResolutionSelection = true;
                SelectedResolution = null;
                _isApplyingAutomaticResolutionSelection = false;
                ClearAutoResolutionState();
                HdrResolutionSupportHint = string.Empty;
                DisabledResolutionReason = string.Empty;
            }
            finally
            {
                _isApplyingAutomaticResolutionSelection = false;
                _isRebuildingModeOptions = false;
            }

            RebuildFrameRateOptions();
            UpdateTargetSummary();
            return;
        }

        string? hdrHint = null;
        var allowSourceAutoSelect = IsHdrEnabled && (_forceSourceAutoRetarget || !_hasUserOverriddenResolutionForCurrentMode);
        var sourceSelected = allowSourceAutoSelect
            ? TrySelectSourceResolutionOption(options, desiredSelection)
            : null;
        var sourceSelectedValue = sourceSelected?.Value;
        if (IsHdrEnabled &&
            sourceSelected is { IsEnabled: true } &&
            previousRate > 0 &&
            !ResolutionSupportsFrameRate(sourceSelected.Value, previousRate, hdrOnly: true))
        {
            var sourceMax = GetMaxFrameRateForResolution(sourceSelected.Value, hdrOnly: true);
            if (sourceMax > 0)
            {
                hdrHint = $"HDR at {sourceSelected.Value} supported up to {FormatFriendlyFrameRate(sourceMax)} fps.";
            }

            sourceSelected = null;
        }

        var selected = sourceSelected;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            TrySelectSdrAutoResolutionOption(options, out var sdrAutoSelection, out var sdrAutoFriendlyBucket))
        {
            selected = sdrAutoSelection;
            _pendingSdrAutoFriendlyFrameRateBucket = sdrAutoFriendlyBucket;
        }

        if (selected == null)
        {
            selected = IsHdrEnabled
                ? SelectHdrResolutionOption(options, desiredSelection, previousRate, out hdrHint)
                : options.FirstOrDefault(option =>
                    option.IsEnabled &&
                    string.Equals(option.Value, desiredSelection, StringComparison.OrdinalIgnoreCase))
                    ?? options.FirstOrDefault(option => option.IsEnabled)
                    ?? options.FirstOrDefault();

            if (IsHdrEnabled &&
                !string.IsNullOrWhiteSpace(sourceSelectedValue) &&
                selected != null &&
                !string.Equals(sourceSelectedValue, selected.Value, StringComparison.OrdinalIgnoreCase) &&
                previousRate > 0)
            {
                var sourceMax = GetMaxFrameRateForResolution(sourceSelectedValue, hdrOnly: true);
                if (sourceMax > 0 && previousRate > sourceMax + 0.01)
                {
                    hdrHint = $"HDR at {sourceSelectedValue} supported up to {FormatFriendlyFrameRate(sourceMax)} fps; switched to {selected.Value} to keep {FormatFriendlyFrameRate(previousRate)} fps.";
                }
            }
        }

        var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
        var selectedDropdownOption = selectAutoOption
            ? autoOption
            : selected;
        var availableOptions = autoOption == null
            ? options
            : new[] { autoOption }.Concat(options).ToList();

        _isRebuildingModeOptions = true;
        try
        {
            UpdateAutoResolutionState(autoSelection);
            AvailableResolutions.Clear();
            foreach (var option in availableOptions)
            {
                AvailableResolutions.Add(option);
            }

            _isApplyingAutomaticResolutionSelection = true;
            if (selectedDropdownOption != null)
            {
                var previousSelectedResolution = SelectedResolution;
                SelectedResolution = selectedDropdownOption.Value;
                if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                {
                    OnPropertyChanged(nameof(SelectedResolution));
                }
            }

            _isApplyingAutomaticResolutionSelection = false;
            if (selected != null)
            {
                _lastKnownResolutionKey = selected.Value;
            }

            if (IsHdrEnabled)
            {
                HdrResolutionSupportHint = hdrHint ?? BuildHdrSupportHintForResolution(selected?.Value);
            }
            else
            {
                HdrResolutionSupportHint = string.Empty;
            }

            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = "No HDR-capable resolution is available for this device.";
            }

            DisabledResolutionReason = selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty;
        }
        finally
        {
            _isApplyingAutomaticResolutionSelection = false;
            _isRebuildingModeOptions = false;
        }

        RebuildFrameRateOptions();
    }

    public void SelectAutoFrameRate()
        => SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);

    private void SelectAutoFrameRate(bool rebuildOptions)
    {
        IsAutoFrameRateSelected = true;
        _hasUserOverriddenFrameRateForCurrentMode = false;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;

        if (rebuildOptions)
        {
            RebuildFrameRateOptions();
            return;
        }

        var currentOptions = AvailableFrameRates
            .Where(option => !IsAutoFrameRateValue(option.FriendlyValue))
            .ToList();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, currentOptions, SelectedFrameRate);
        var sourceTimingFamilyKnown = TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        FrameRateOption? selected = null;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
        {
            selected = currentOptions.FirstOrDefault(option =>
                option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
        }

        if (selected == null &&
            sourceRate.Rate.HasValue)
        {
            selected = currentOptions
                .Where(option => option.IsEnabled)
                .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                .ThenBy(option =>
                    sourceTimingFamilyKnown &&
                    TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                    optionFamily == sourceTimingFamily
                        ? 0
                        : 1)
                .FirstOrDefault();
        }

        selected ??= currentOptions.FirstOrDefault(option => option.IsEnabled)
            ?? currentOptions.FirstOrDefault();

        ApplyResolvedFrameRateSelection(selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void RebuildFrameRateOptions()
    {
        var previousRate = SelectedFrameRate;
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var timingFamily = ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
            _resolutionToFormats.TryGetValue(selectedResolutionKey, out var formats))
        {
            options = formats
                .GroupBy(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .Select(group =>
                {
                    var allFormats = group.ToList();
                    var hdrSupported = allFormats.Any(IsHdrModeCandidate);
                    var enabled = !IsHdrEnabled || hdrSupported;
                    var selectionPool = IsHdrEnabled && hdrSupported
                        ? allFormats.Where(IsHdrModeCandidate).ToList()
                        : allFormats;
                    var preferred = SelectPreferredFrameRateFormat(selectionPool, group.Key, timingFamily);
                    var numerator = preferred.FrameRateNumerator > 0 ? preferred.FrameRateNumerator : (uint?)null;
                    var denominator = preferred.FrameRateDenominator > 0 ? preferred.FrameRateDenominator : (uint?)null;
                    return new FrameRateOption
                    {
                        FriendlyValue = group.Key,
                        Value = preferred.FrameRateExact,
                        Rational = preferred.FrameRateRational,
                        Numerator = numerator,
                        Denominator = denominator,
                        IsEnabled = enabled,
                        DisableReason = enabled
                            ? string.Empty
                            : "HDR mode is not supported at this frame rate."
                    };
                })
                .OrderByDescending(option => option.FriendlyValue)
                .ToList();
        }

        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceTimingFamilyKnown = TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        var sourceFriendlyRate = sourceRate.Rate.HasValue
            ? Math.Round(sourceRate.Rate.Value, MidpointRounding.AwayFromZero)
            : (double?)null;
        var cappedOptions = options
            .Select(option =>
            {
                var enabled = option.IsEnabled;
                var disableReason = option.DisableReason;

                if (enabled && sourceFriendlyRate.HasValue)
                {
                    if (option.FriendlyValue > sourceFriendlyRate.Value + 0.01)
                    {
                        enabled = false;
                        disableReason = $"Source signal is {sourceFriendlyRate.Value:0} fps; higher capture fps duplicates frames.";
                    }
                    else if (sourceTimingFamilyKnown &&
                             sourceRate.Rate.HasValue &&
                             TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                             optionFamily != FrameRateTimingFamily.Unknown &&
                             sourceTimingFamily != FrameRateTimingFamily.Unknown &&
                             optionFamily != sourceTimingFamily &&
                             ResolutionHasTimingFamilyVariant(selectedResolutionKey, option.FriendlyValue, sourceTimingFamily) &&
                             IsFriendlyFrameRateMatch(option.FriendlyValue, sourceFriendlyRate.Value) &&
                             option.Value > sourceRate.Rate.Value + 0.03)
                    {
                        enabled = false;
                        disableReason = $"Source timing is {sourceRate.Arg ?? sourceRate.Rate.Value.ToString("0.###")} so this duplicate variant is hidden.";
                    }
                    else
                    {
                        var roundedSourceFriendlyRate = (int)Math.Round(sourceFriendlyRate.Value, MidpointRounding.AwayFromZero);
                        var roundedOptionFriendlyRate = (int)Math.Round(option.FriendlyValue, MidpointRounding.AwayFromZero);
                        if (roundedOptionFriendlyRate > 0 &&
                            roundedOptionFriendlyRate <= roundedSourceFriendlyRate &&
                            roundedSourceFriendlyRate % roundedOptionFriendlyRate != 0)
                        {
                            enabled = false;
                            disableReason = $"{roundedOptionFriendlyRate:0} fps is not a clean divisor of source {roundedSourceFriendlyRate:0} fps.";
                        }
                    }
                }

                return new FrameRateOption
                {
                    FriendlyValue = option.FriendlyValue,
                    Value = option.Value,
                    Rational = option.Rational,
                    Numerator = option.Numerator,
                    Denominator = option.Denominator,
                    IsEnabled = enabled,
                    DisableReason = enabled ? string.Empty : disableReason,
                    DisplayTextOverride = option.DisplayTextOverride
                };
            })
            .ToList();

        options = ShowAllCaptureOptions
            ? cappedOptions
                .Select(option =>
                {
                    if (option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                    {
                        return option;
                    }

                    return new FrameRateOption
                    {
                        FriendlyValue = option.FriendlyValue,
                        Value = option.Value,
                        Rational = option.Rational,
                        Numerator = option.Numerator,
                        Denominator = option.Denominator,
                        IsEnabled = true,
                        DisableReason = string.Empty,
                        DisplayTextOverride = option.DisplayTextOverride
                    };
                })
                .ToList()
            : cappedOptions
                .Where(option => option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                .ToList();
        var autoFrameRateOption = options.Count > 0
            ? new FrameRateOption
            {
                FriendlyValue = AutoFrameRateValue,
                Value = AutoFrameRateValue,
                IsEnabled = true,
                DisplayTextOverride = "Auto"
            }
            : null;
        var availableOptions = autoFrameRateOption == null
            ? options
            : new[] { autoFrameRateOption }.Concat(options).ToList();
        DetectedSourceFrameRate = sourceRate.Rate;
        DetectedSourceFrameRateArg = sourceRate.Arg;
        SourceFrameRateOrigin = sourceRate.Origin;

        _isRebuildingModeOptions = true;
        try
        {
            AvailableFrameRates.Clear();
            foreach (var option in availableOptions)
            {
                AvailableFrameRates.Add(option);
            }

            FrameRateOption? selected = null;
            var selectAutoOption = autoFrameRateOption != null &&
                                   (IsAutoFrameRateSelected || !_hasUserOverriddenFrameRateForCurrentMode);
            if (selectAutoOption &&
                !IsHdrEnabled &&
                _pendingSdrAutoSelectionForDeviceChange &&
                _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
            {
                selected = options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
            }

            if (selected == null &&
                selectAutoOption &&
                sourceRate.Rate.HasValue)
            {
                selected = options
                    .Where(option => option.IsEnabled)
                    .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                    .ThenBy(option =>
                        sourceTimingFamilyKnown &&
                        TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                        optionFamily == sourceTimingFamily
                            ? 0
                            : 1)
                    .FirstOrDefault();
            }

            if (selected == null)
            {
                selected = selectAutoOption
                    ? options.FirstOrDefault(option => option.IsEnabled)
                        ?? options.FirstOrDefault()
                    : options.FirstOrDefault(option =>
                        option.IsEnabled && IsFrameRateMatch(option.Value, previousRate))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 60))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 30))
                        ?? options.FirstOrDefault(option => option.IsEnabled)
                        ?? options.FirstOrDefault();
            }

            if (autoFrameRateOption != null)
            {
                IsAutoFrameRateSelected = selectAutoOption;
            }
            var fallbackRate = previousRate > 0
                ? previousRate
                : 60;
            ApplyResolvedFrameRateSelection(selected, fallbackRate);
            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = $"No HDR-capable frame rate is available for {GetSelectedResolutionDisplayText()}.";
            }

            if (!IsHdrEnabled && _pendingSdrAutoSelectionForDeviceChange && selected != null)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
            }
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
            _isRebuildingModeOptions = false;
        }

        UpdateSelectedFormat();
        UpdateTargetSummary();
        _forceSourceAutoRetarget = false;
    }

    private sealed record AutoCaptureSelection(
        ResolutionOption Resolution,
        int FriendlyFrameRate,
        double ExactFrameRate);

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => IsAutoResolutionValue(previousSelection) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_hasUserOverriddenResolutionForCurrentMode;

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var rankedOptions = options
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        var eligibleOptions = rankedOptions.Where(option => option.IsEnabled).ToList();
        if (eligibleOptions.Count == 0)
        {
            eligibleOptions = rankedOptions;
        }

        var sourceFriendlyCap = _latestSourceTelemetry.HasFrameRate
            ? (int?)Math.Round(_latestSourceTelemetry.FrameRateExact!.Value, MidpointRounding.AwayFromZero)
            : null;
        var friendlyBuckets = eligibleOptions
            .SelectMany(GetAutoEligibleFormats)
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (friendlyBuckets.Count == 0)
        {
            return BuildAutoCaptureSelectionFallback(eligibleOptions);
        }

        var bestFriendlyBucket = friendlyBuckets
            .FirstOrDefault(bucket => !sourceFriendlyCap.HasValue || bucket <= sourceFriendlyCap.Value);
        if (bestFriendlyBucket == 0)
        {
            bestFriendlyBucket = friendlyBuckets[0];
        }

        var matchingResolutions = eligibleOptions
            .Where(option => ResolutionSupportsFriendlyFrameRate(
                option.Value,
                bestFriendlyBucket,
                hdrOnly: IsHdrEnabled,
                sdrOnly: !IsHdrEnabled))
            .ToList();
        if (matchingResolutions.Count == 0)
        {
            matchingResolutions = eligibleOptions;
        }

        var chosenResolution = SelectBestAutoResolutionCandidate(matchingResolutions) ?? eligibleOptions[0];
        var preferredFormat = SelectPreferredAutoFrameRateFormat(chosenResolution.Value, bestFriendlyBucket);
        return new AutoCaptureSelection(
            chosenResolution,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private AutoCaptureSelection? BuildAutoCaptureSelectionFallback(IReadOnlyList<ResolutionOption> options)
    {
        var fallback = options.FirstOrDefault();
        if (fallback == null)
        {
            return null;
        }

        var preferredBucket = GetMaxFrameRateFriendlyBucket(fallback.Value);
        var preferredFormat = SelectPreferredAutoFrameRateFormat(fallback.Value, preferredBucket);
        return new AutoCaptureSelection(
            fallback,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private IEnumerable<MediaFormat> GetAutoEligibleFormats(ResolutionOption option)
    {
        if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
        {
            return Enumerable.Empty<MediaFormat>();
        }

        var filtered = formats
            .Where(format => !IsHdrEnabled || IsHdrModeCandidate(format))
            .ToList();
        return filtered.Count > 0 ? filtered : formats;
    }

    private ResolutionOption? SelectBestAutoResolutionCandidate(IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var ranked = candidates
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return ranked[0];
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return ranked[0];
        }

        return ranked.FirstOrDefault(option => option.Width <= sourceWidth && option.Height <= sourceHeight)
            ?? ranked[0];
    }

    private MediaFormat SelectPreferredAutoFrameRateFormat(string resolutionKey, int preferredFriendlyBucket)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            throw new InvalidOperationException($"No formats are available for resolution '{resolutionKey}'.");
        }

        var timingFamily = FrameRateTimingFamily.Unknown;
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamily))
        {
            timingFamily = sourceFamily;
        }

        var selectionPool = formats
            .Where(format =>
                (!IsHdrEnabled || IsHdrModeCandidate(format)) &&
                GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
            .ToList();
        if (selectionPool.Count == 0)
        {
            selectionPool = formats
                .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
                .ToList();
        }
        if (selectionPool.Count == 0)
        {
            selectionPool = formats.ToList();
            preferredFriendlyBucket = GetFriendlyFrameRateBucket(selectionPool.Max(format => format.FrameRateExact));
        }

        return SelectPreferredFrameRateFormat(selectionPool, preferredFriendlyBucket, timingFamily);
    }

    private int GetMaxFrameRateFriendlyBucket(string resolutionKey)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            return 0;
        }

        var filtered = formats
            .Where(format => !IsHdrEnabled || IsHdrModeCandidate(format))
            .ToList();
        if (filtered.Count == 0)
        {
            filtered = formats.ToList();
        }

        return filtered
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .DefaultIfEmpty()
            .Max();
    }

    private bool DoesResolutionMatchSourceAspectRatio(ResolutionOption option)
    {
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return true;
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0 || option.Width == 0 || option.Height == 0)
        {
            return true;
        }

        var reducedSource = ReduceAspectRatio(sourceWidth, sourceHeight);
        var reducedOption = ReduceAspectRatio(option.Width, option.Height);
        return reducedSource.Width == reducedOption.Width &&
               reducedSource.Height == reducedOption.Height;
    }

    private static (uint Width, uint Height) ReduceAspectRatio(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return (width, height);
        }

        var divisor = GreatestCommonDivisor(width, height);
        return divisor == 0
            ? (width, height)
            : (width / divisor, height / divisor);
    }

    private static uint GreatestCommonDivisor(uint a, uint b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return a;
    }

    private static bool IsSourceFilteredFrameRateDisableReason(string? disableReason)
        => !string.IsNullOrWhiteSpace(disableReason) &&
           (disableReason.IndexOf("higher capture fps", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("duplicate variant", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("not a clean divisor", StringComparison.OrdinalIgnoreCase) >= 0);

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

    private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
    {
        AutoResolvedWidth = selection?.Resolution.Width;
        AutoResolvedHeight = selection?.Resolution.Height;
        AutoResolvedFrameRate = selection?.ExactFrameRate;
    }

    private void ClearAutoResolutionState()
    {
        AutoResolvedWidth = null;
        AutoResolvedHeight = null;
        AutoResolvedFrameRate = null;
    }

    private string GetSelectedResolutionDisplayText()
    {
        if (string.IsNullOrWhiteSpace(SelectedResolution))
        {
            return "?";
        }

        if (!IsAutoResolutionValue(SelectedResolution))
        {
            return SelectedResolution;
        }

        var friendlyRate = SelectedFriendlyFrameRate
            ?? (AutoResolvedFrameRate.HasValue
                ? Math.Round(AutoResolvedFrameRate.Value, MidpointRounding.AwayFromZero)
                : (double?)null);
        if (AutoResolvedWidth.HasValue &&
            AutoResolvedHeight.HasValue &&
            friendlyRate.HasValue)
        {
            return $"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)";
        }

        return AutoResolutionValue;
    }

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, AutoResolutionValue, StringComparison.OrdinalIgnoreCase);

    private bool TryResolveResolutionKey(string? resolutionValue, out string resolutionKey)
    {
        resolutionKey = string.Empty;
        if (string.IsNullOrWhiteSpace(resolutionValue))
        {
            return false;
        }

        if (IsAutoResolutionValue(resolutionValue))
        {
            if (AutoResolvedWidth.HasValue &&
                AutoResolvedHeight.HasValue &&
                AutoResolvedWidth.Value > 0 &&
                AutoResolvedHeight.Value > 0)
            {
                resolutionKey = GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value);
                return true;
            }

            return false;
        }

        if (!TryParseResolutionKey(resolutionValue, out var width, out var height))
        {
            return false;
        }

        resolutionKey = GetResolutionKey(width, height);
        return true;
    }

    private string? GetEffectiveResolutionKey(string? resolutionValue)
        => TryResolveResolutionKey(resolutionValue, out var resolutionKey)
            ? resolutionKey
            : null;

    private bool TryGetEffectiveResolutionSelection(out string resolutionKey, out uint width, out uint height)
    {
        resolutionKey = string.Empty;
        width = 0;
        height = 0;

        if (!TryResolveResolutionKey(SelectedResolution, out resolutionKey) ||
            !TryParseResolutionKey(resolutionKey, out width, out height))
        {
            resolutionKey = string.Empty;
            width = 0;
            height = 0;
            return false;
        }

        return true;
    }

    private void ResetFrameRateSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
        IsAutoFrameRateSelected = true;
    }

    private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)
    {
        _isApplyingAutomaticFrameRateSelection = true;
        try
        {
            SelectedFrameRate = selected?.Value ?? fallbackRate;
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
        }

        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);
        SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;
        }

        DisabledFrameRateReason = selected is { IsEnabled: false }
            ? selected.DisableReason
            : string.Empty;
    }

    private void ResetModeSelectionState()
    {
        ResetFrameRateSelectionState();
        _hasUserOverriddenResolutionForCurrentMode = false;
        _forceSourceAutoRetarget = false;
        _lastSourceModeKey = null;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;
    }

    private ResolutionOption? TrySelectSourceResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection)
    {
        if (options.Count == 0 || !_latestSourceTelemetry.HasDimensions)
        {
            return null;
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return null;
        }

        var exact = options.FirstOrDefault(option =>
            option.IsEnabled &&
            option.Width == sourceWidth &&
            option.Height == sourceHeight);
        if (exact != null)
        {
            return exact;
        }

        var sourceKey = GetResolutionKey(sourceWidth, sourceHeight);
        var enabled = options.Where(option => option.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            return options.FirstOrDefault();
        }

        return SelectNearestResolution(sourceKey, enabled)
            ?? SelectNearestResolution(previousSelection, enabled)
            ?? enabled.FirstOrDefault();
    }

    private ResolutionOption? SelectHdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection,
        double preferredFrameRate,
        out string? hint)
    {
        hint = null;
        if (options.Count == 0)
        {
            return null;
        }

        var previous = options.FirstOrDefault(option =>
            string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase));
        if (previous is { IsEnabled: true } &&
            ResolutionSupportsFrameRate(previous.Value, preferredFrameRate, hdrOnly: true))
        {
            hint = BuildHdrSupportHintForResolution(previous.Value);
            return previous;
        }

        var sameFpsCandidates = options
            .Where(option =>
                option.IsEnabled &&
                ResolutionSupportsFrameRate(option.Value, preferredFrameRate, hdrOnly: true))
            .ToList();

        var selected = SelectNearestResolution(previousSelection, sameFpsCandidates)
            ?? SelectNearestResolution(previousSelection, options.Where(option => option.IsEnabled).ToList())
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            !string.Equals(previousSelection, selected?.Value, StringComparison.OrdinalIgnoreCase))
        {
            var previousMax = GetMaxFrameRateForResolution(previousSelection, hdrOnly: true);
            if (previousMax > 0)
            {
                hint = $"HDR at {previousSelection} supported up to {FormatFriendlyFrameRate(previousMax)} fps.";
            }
        }

        hint ??= BuildHdrSupportHintForResolution(selected?.Value);
        return selected;
    }

    private bool TrySelectSdrAutoResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        out ResolutionOption? selected,
        out int selectedFriendlyBucket)
    {
        selected = null;
        selectedFriendlyBucket = 60;
        if (options.Count == 0)
        {
            return false;
        }

        var enabledOptions = options
            .Where(option => option.IsEnabled)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();
        if (enabledOptions.Count == 0)
        {
            return false;
        }

        var sdrFriendlyBucketsByResolution = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in enabledOptions)
        {
            if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
            {
                continue;
            }

            var buckets = formats
                .Where(format => !IsHdrModeCandidate(format))
                .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .ToHashSet();
            if (buckets.Count > 0)
            {
                sdrFriendlyBucketsByResolution[option.Value] = buckets;
            }
        }

        if (sdrFriendlyBucketsByResolution.Count == 0)
        {
            return false;
        }

        foreach (var friendlyBucket in new[] { 60, 30 })
        {
            var match = enabledOptions.FirstOrDefault(option =>
                sdrFriendlyBucketsByResolution.TryGetValue(option.Value, out var buckets) &&
                buckets.Contains(friendlyBucket));
            if (match != null)
            {
                selected = match;
                selectedFriendlyBucket = friendlyBucket;
                return true;
            }
        }

        selected = enabledOptions.FirstOrDefault(option => sdrFriendlyBucketsByResolution.ContainsKey(option.Value));
        if (selected == null)
        {
            return false;
        }

        selectedFriendlyBucket = ResolvePreferredFriendlyBucketForResolution(selected.Value, sdrOnly: true) ?? 30;
        return true;
    }

    private static ResolutionOption? SelectNearestResolution(string? baselineResolution, IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!TryParseResolutionKey(baselineResolution, out var baseWidth, out var baseHeight))
        {
            return candidates
                .OrderByDescending(option => (long)option.Width * option.Height)
                .FirstOrDefault();
        }

        var baseArea = (long)baseWidth * baseHeight;
        var lowerCandidate = candidates
            .Where(option => ((long)option.Width * option.Height) < baseArea)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
        if (lowerCandidate != null)
        {
            return lowerCandidate;
        }

        return candidates
            .OrderBy(option => Math.Abs(((long)option.Width * option.Height) - baseArea))
            .ThenByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
    }

    private static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(resolutionKey) || IsAutoResolutionValue(resolutionKey))
        {
            return false;
        }

        var parts = resolutionKey.Split('x');
        return parts.Length == 2 &&
               uint.TryParse(parts[0], out width) &&
               uint.TryParse(parts[1], out height);
    }

    private bool ResolutionSupportsFrameRate(string resolutionKey, double frameRate, bool hdrOnly)
    {
        if (frameRate <= 0)
        {
            return false;
        }

        var requestedBucket = GetFriendlyFrameRateBucket(frameRate);
        return ResolutionSupportsFriendlyFrameRate(
            resolutionKey,
            requestedBucket,
            hdrOnly: hdrOnly,
            sdrOnly: !hdrOnly);
    }

    private bool ResolutionSupportsFriendlyFrameRate(
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        return formats.Any(format =>
            (!hdrOnly || IsHdrModeCandidate(format)) &&
            (!sdrOnly || !IsHdrModeCandidate(format)) &&
            GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket);
    }

    private int? ResolvePreferredFriendlyBucketForResolution(string resolutionKey, bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return null;
        }

        var buckets = formats
            .Where(format => !sdrOnly || !IsHdrModeCandidate(format))
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (buckets.Count == 0)
        {
            return null;
        }

        if (buckets.Contains(60))
        {
            return 60;
        }

        if (buckets.Contains(30))
        {
            return 30;
        }

        return buckets[0];
    }

    private bool ResolutionHasTimingFamilyVariant(
        string? resolutionKey,
        double friendlyFrameRate,
        FrameRateTimingFamily family)
    {
        if (family == FrameRateTimingFamily.Unknown ||
            string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        var bucket = (int)Math.Round(friendlyFrameRate, MidpointRounding.AwayFromZero);
        foreach (var format in formats)
        {
            if (GetFriendlyFrameRateBucket(format.FrameRateExact) != bucket)
            {
                continue;
            }

            if (TryInferFrameRateTimingFamily(format.FrameRateRational, format.FrameRateExact, out var formatFamily) &&
                formatFamily == family)
            {
                return true;
            }
        }

        return false;
    }

    private double GetMaxFrameRateForResolution(string? resolutionKey, bool hdrOnly)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return 0;
        }

        var candidates = hdrOnly
            ? formats.Where(IsHdrModeCandidate).ToList()
            : formats;
        if (candidates.Count == 0)
        {
            return 0;
        }

        return candidates.Max(format => format.FrameRateExact);
    }

    private string BuildHdrSupportHintForResolution(string? resolutionKey)
    {
        if (!IsHdrEnabled || string.IsNullOrWhiteSpace(resolutionKey))
        {
            return string.Empty;
        }

        var maxHdrRate = GetMaxFrameRateForResolution(resolutionKey, hdrOnly: true);
        if (maxHdrRate <= 0)
        {
            return $"HDR is not supported at {resolutionKey}.";
        }

        if (SelectedFrameRate > 0 && maxHdrRate >= SelectedFrameRate - 0.01)
        {
            return string.Empty;
        }

        return $"HDR at {resolutionKey} supported up to {FormatFriendlyFrameRate(maxHdrRate)} fps.";
    }

    private static string FormatFriendlyFrameRate(double frameRate)
        => $"{Math.Round(frameRate):0}";

    private FrameRateTimingFamily ResolvePreferredTimingFamily(string? resolutionKey, double previousRate)
    {
        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg,
                    runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate,
                    out var runtimeFamily))
            {
                return runtimeFamily;
            }

            if (runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.NegotiatedFrameRateArg,
                    runtime.NegotiatedFrameRate,
                    out var negotiatedFamily))
            {
                return negotiatedFamily;
            }
        }

        if (TryInferFrameRateTimingFamily(SelectedFormat?.FrameRateRational, SelectedFormat?.FrameRateExact, out var selectedFamily))
        {
            return selectedFamily;
        }

        var selectedOption = AvailableFrameRates.FirstOrDefault(option => IsFrameRateMatch(option.Value, previousRate));
        if (selectedOption != null &&
            TryInferFrameRateTimingFamily(selectedOption.Rational, selectedOption.Value, out var optionFamily))
        {
            return optionFamily;
        }

        if (TryInferFrameRateTimingFamily(null, previousRate, out var previousFamily))
        {
            return previousFamily;
        }

        return FrameRateTimingFamily.Unknown;
    }

    private static MediaFormat SelectPreferredFrameRateFormat(
        IReadOnlyList<MediaFormat> candidates,
        int friendlyBucket,
        FrameRateTimingFamily timingFamily)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No frame-rate candidates are available.");
        }

        return candidates
            .OrderBy(format => GetTimingFamilyRank(format, friendlyBucket, timingFamily))
            .ThenBy(format => Math.Abs(format.FrameRateExact - friendlyBucket))
            .ThenByDescending(IsHdrModeCandidate)
            .ThenBy(format => GetEffectivePixelFormatPriority(format))
            .First();
    }

    /// <summary>
    /// At 4K HFR (≥3840x2160 @ ≥100fps SDR), prefer MJPG over NV12. The UVC driver
    /// presents NV12 at these rates, but it's actually CPU-decoded MJPG causing frame
    /// drops. Selecting raw MJPG lets MF use GPU DXVA decode via hardware transforms.
    /// </summary>
    private static int GetEffectivePixelFormatPriority(MediaFormat format)
    {
        if (format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100 &&
            format.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return MediaFormat.GetPixelFormatPriority(format.PixelFormat);
    }

    private static int GetTimingFamilyRank(MediaFormat format, int friendlyBucket, FrameRateTimingFamily timingFamily)
    {
        if (format.FrameRateNumerator > 0 && format.FrameRateDenominator > 0)
        {
            return timingFamily switch
            {
                FrameRateTimingFamily.Ntsc1001 when format.FrameRateDenominator == 1001
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer when format.FrameRateDenominator == 1
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                FrameRateTimingFamily.Ntsc1001 => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                _ => 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100)
            };
        }

        return 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100);
    }

    private static bool TryInferFrameRateTimingFamily(
        string? frameRateArg,
        double? frameRate,
        out FrameRateTimingFamily family)
    {
        family = FrameRateTimingFamily.Unknown;

        if (TryParseFrameRateRational(frameRateArg, out var numerator, out var denominator))
        {
            if (denominator == 1001)
            {
                family = FrameRateTimingFamily.Ntsc1001;
                return true;
            }

            if (denominator == 1)
            {
                family = FrameRateTimingFamily.Integer;
                return true;
            }
        }

        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return false;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01)
        {
            family = FrameRateTimingFamily.Integer;
            return true;
        }

        var ntscCandidate = rounded * 1000.0 / 1001.0;
        if (Math.Abs(value - ntscCandidate) <= 0.03)
        {
            family = FrameRateTimingFamily.Ntsc1001;
            return true;
        }

        return false;
    }

    private (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(
        string? resolutionKey,
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
    {
        if (_latestSourceTelemetry.HasFrameRate)
        {
            return (
                _latestSourceTelemetry.FrameRateExact,
                _latestSourceTelemetry.FrameRateArg,
                _latestSourceTelemetry.Origin != SourceTelemetryOrigin.Unknown
                    ? _latestSourceTelemetry.Origin.ToString()
                    : "SourceTelemetry");
        }

        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualFrameRate.HasValue &&
                runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight)
            {
                return (
                    runtime.ActualFrameRate,
                    runtime.ActualFrameRateArg ??
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }

            if (runtime.NegotiatedFrameRate.HasValue &&
                runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight)
            {
                return (
                    runtime.NegotiatedFrameRate,
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }
        }

        if (SelectedFormat != null &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFormat.FrameRateExact)))
        {
            return (
                SelectedFormat.FrameRateExact,
                string.IsNullOrWhiteSpace(SelectedFormat.FrameRateRational)
                    ? null
                    : SelectedFormat.FrameRateRational,
                "SelectedMode");
        }

        if (previousRate > 0 &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)))
        {
            return (previousRate, null, "SelectedMode");
        }

        return (null, null, "Unknown");
    }

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsFriendlyFrameRateMatch(double optionFriendlyRate, double requestedRate)
        => Math.Round(optionFriendlyRate) == Math.Round(requestedRate);

    private static bool IsAutoFrameRateValue(double value)
        => value == AutoFrameRateValue || value < 0;

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    private static bool TryParseFrameRateRational(string? rational, out uint numerator, out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (string.IsNullOrWhiteSpace(rational))
        {
            return false;
        }

        var split = rational.Split('/');
        return split.Length == 2 &&
               uint.TryParse(split[0], out numerator) &&
               uint.TryParse(split[1], out denominator) &&
               denominator > 0;
    }

    partial void OnSelectedFormatChanged(MediaFormat? value)
    {
        // If preview is active and this isn't during initial device setup, reinitialize with new format
        if (value != null && !_isChangingDevice && !_suppressFormatChangeReinitialize && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Format changed to {value.Width}x{value.Height}@{value.FrameRate}fps - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("format change"), "format change reinitialize");
        }
    }

    partial void OnSelectedVideoFormatChanged(string value)
    {
        if (!_isChangingDevice && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Video format override changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("video format override"), "video format override reinitialize");
        }
    }

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(videoFormat))
            {
                throw new ArgumentException("Video format is required.", nameof(videoFormat));
            }

            var match = AvailableVideoFormats.FirstOrDefault(
                format => string.Equals(format, videoFormat, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new InvalidOperationException($"Video format '{videoFormat}' is not available.");
            }

            SelectedVideoFormat = match;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    partial void OnMjpegDecoderCountChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 8);
        if (clamped != value)
        {
            MjpegDecoderCount = clamped;
            return;
        }

        if (!_isChangingDevice &&
            IsPreviewing &&
            IsInitialized &&
            BuildCaptureSettings().UseMjpegHighFrameRateMode)
        {
            Logger.Log($"=== MJPEG decoder count changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("mjpeg decoder count"), "mjpeg decoder count reinitialize");
        }
    }

    private async Task ReinitializeDeviceAsync(string reason)
    {
        if (SelectedDevice == null || SelectedFormat == null)
            return;

        await _previewReinitializeGate.WaitAsync();
        var shouldRestartPreview = IsPreviewing;
        try
        {
            StatusText = "Applying new settings...";
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = true;
                _cancelPreviewRestartAfterReinitialize = false;
                await NotifyPreviewReinitRequestedAsync(reason);
            }

            if (IsPreviewing)
            {
                await StopPreviewAsync(userInitiated: false);
            }

            // Reinitialize the device with new settings
            IsInitialized = false;
            await InitializeDeviceAsync();

            // Restart preview
            if (IsInitialized && shouldRestartPreview && !_cancelPreviewRestartAfterReinitialize)
            {
                await StartPreviewAsync(userInitiated: false);

                StatusText = $"Preview: {SelectedFormat.Width}x{SelectedFormat.Height}@{SelectedFormat.FrameRate}fps";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to apply format: {ex.Message}";
        }
        finally
        {
            _cancelPreviewRestartAfterReinitialize = false;
            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = false;
            }
            _previewReinitializeGate.Release();
        }
    }

    partial void OnSelectedQualityChanged(string value)
    {
        IsCustomBitrateVisible = value == "Custom";
        SaveSettings();
    }

    partial void OnSelectedPresetChanged(string value)
    {
        SaveSettings();
    }

    partial void OnSelectedSplitEncodeModeChanged(string value)
    {
        SaveSettings();
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

        EnqueueUiOperation(() => ApplyDeviceAudioModeAsync("device audio mode change"), "device audio mode change");
        SaveSettings();
    }

    public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            SelectedDeviceAudioMode = mode;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            AnalogAudioGainPercent = Math.Clamp(gainPercent, 0.0, 100.0);
            return Task.CompletedTask;
        }, cancellationToken);
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

        EnqueueUiOperation(() => ApplyAnalogAudioGainAsync("analog audio gain change"), "analog audio gain change");
        SaveSettings();
    }

    internal bool SuppressVolumeSave { get; set; }

    /// <summary>
    /// When non-null, SaveSettings writes this value for PreviewVolume instead of the
    /// current (animation-transient) property value. Set during the entrance volume
    /// fade-in to prevent intermediate 0 values from corrupting persisted settings.
    /// </summary>
    internal double? VolumeSaveOverride { get; set; }

    partial void OnPreviewVolumeChanged(double value)
    {
        _captureService.SetPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));
    }

    /// <summary>
    /// Persists the current preview volume to settings. Called by the UI on
    /// pointer release so the slider doesn't trigger disk I/O on every tick.
    /// </summary>
    internal void SavePreviewVolume() => SaveSettings();

    private static AutomationStringOption[] BuildStringOptions(
        IEnumerable<string> values,
        string selectedValue)
    {
        return values
            .Select(value => new AutomationStringOption
            {
                Value = value,
                Label = value,
                IsEnabled = true,
                DisableReason = string.Empty,
                IsSelected = string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)
            })
            .ToArray();
    }

    partial void OnIsAudioPreviewEnabledChanged(bool value)
    {
        if (value && !IsAudioEnabled)
        {
            Logger.Log("Audio preview requested but audio capture is disabled");
            IsAudioPreviewEnabled = false;
            return;
        }
        else if (!value && !IsRecording)
        {
            ResetAudioMeter();
        }

        // Toggle audio monitoring: mute/unmute WASAPI playback without stopping it
        if (IsPreviewing && IsInitialized)
        {
            if (value)
            {
                // Ensure playback exists (no-op if already running), then unmute
                EnqueueUiOperation(async () =>
                {
                    await _sessionCoordinator.StartAudioPreviewAsync();
                    _captureService.SetMonitoringMuted(false);
                }, "audio monitoring unmute");
            }
            else
            {
                // Just mute — keep WASAPI playback alive for instant re-enable
                _captureService.SetMonitoringMuted(true);
            }
        }

        SaveSettings();
    }

    private async Task ApplyAudioInputSelectionAsync(string reason)
    {
        if (!IsInitialized)
        {
            return;
        }

        string? audioDeviceId = null;
        string? audioDeviceName = null;

        if (IsCustomAudioInputEnabled)
        {
            audioDeviceId = SelectedAudioInputDevice?.Id;
            audioDeviceName = SelectedAudioInputDevice?.Name;
        }
        else
        {
            audioDeviceId = SelectedDevice?.AudioDeviceId;
            audioDeviceName = SelectedDevice?.AudioDeviceName;
        }

        Logger.Log($"=== Updating audio input ({reason}) ===");
        Logger.Log($"  Audio device: {audioDeviceName ?? "(none)"}");

        await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);
    }

    private async Task RefreshDeviceAudioControlsAsync(bool applySavedState)
    {
        var device = SelectedDevice;
        if (device == null)
        {
            WithAudioControlRefreshSuppressed(() =>
            {
                IsDeviceAudioControlSupported = false;
                SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;
                AnalogAudioGainPercent = 50;
            });

            return;
        }

        // Always show device audio controls for supported 4K X devices,
        // even if the payload-mutation service can't read selector 3.
        if (NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _))
        {
            WithAudioControlRefreshSuppressed(() => IsDeviceAudioControlSupported = true);
        }

        var state = await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = state.IsSupported;
            if (state.IsSupported)
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(state.Mode ?? _pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(
                    state.AnalogGainPercent ?? _pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent,
                    0.0,
                    100.0);
            }
            else
            {
                SelectedDeviceAudioMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
                AnalogAudioGainPercent = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
            }
        });

        if (!applySavedState || !state.IsSupported)
        {
            return;
        }

        var desiredMode = NormalizeDeviceAudioMode(_pendingSavedDeviceAudioMode ?? SelectedDeviceAudioMode);
        var desiredGain = Math.Clamp(_pendingSavedAnalogAudioGainPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        _pendingSavedDeviceAudioMode = null;
        _pendingSavedAnalogAudioGainPercent = null;

        // Do NOT apply saved audio mode on restore. The AT SET command crashes the
        // 4K X USB link, and the payload mutation path doesn't work reliably.
        // Just sync the UI to the saved setting — the user can switch manually.
        Logger.Log($"NATIVEXU_AUDIO_RESTORE_READ_ONLY desired='{desiredMode}' device='{state.Mode}'");

        var refreshedState = await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
        WithAudioControlRefreshSuppressed(() =>
        {
            IsDeviceAudioControlSupported = refreshedState.IsSupported;
            SelectedDeviceAudioMode = NormalizeDeviceAudioMode(refreshedState.Mode ?? desiredMode);
            AnalogAudioGainPercent = Math.Clamp(refreshedState.AnalogGainPercent ?? desiredGain, 0.0, 100.0);
        });
    }

    private async Task ApplyDeviceAudioModeAsync(
        string reason,
        string? explicitMode = null,
        bool reapplyAnalogGain = true,
        bool persistSettings = true)
    {
        var device = SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return;
        }

        var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);
        Logger.Log($"=== Updating device audio mode ({reason}) ===");
        Logger.Log($"  Mode: {mode}");

        // Use the same three-command flash-based sequence that Elgato Studio uses:
        // GPIO prep (0x5B) → Flash read (0x52) → Flash write (0x51).
        // This is seamless — no USB re-enumeration, no preview interruption.
        var isAnalog = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var gainByte = MapPercentToGainByte(AnalogAudioGainPercent);
        var applied = await NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte).ConfigureAwait(false);

        if (!applied)
        {
            // Still read state for diagnostics even on failure
            await _deviceAudioControlService.ReadStateAsync(device).ConfigureAwait(false);
            // Revert the UI toggle — hardware is still on the previous mode.
            // Selector 3 read-back doesn't reflect I2C state, so just flip back.
            var revertMode = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase) ? DeviceAudioMode.Hdmi : DeviceAudioMode.Analog;
            WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = revertMode);

            StatusText = $"Device audio mode change failed ({mode})";
            return;
        }

        StatusText = $"Device audio mode set to {mode}";
        if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            await ApplyAnalogAudioGainAsync("analog gain after mode switch", AnalogAudioGainPercent, persistSettings: false).ConfigureAwait(false);
        }

        // Trust the mode we just set — don't read back from the old selector 3 service,
        // which doesn't reflect the new AT+I2C switch mechanism.
        WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);

        if (persistSettings)
        {
            SaveSettings();
        }
    }

    private async Task ApplyAnalogAudioGainAsync(
        string reason,
        double? explicitPercent = null,
        bool persistSettings = true)
    {
        var device = SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return;
        }

        var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        var gainByte = MapPercentToGainByte(gainPercent);
        Logger.Log($"=== Updating analog audio gain ({reason}) ===");
        Logger.Log($"  GainPercent: {gainPercent:0} GainByte: 0x{gainByte:X2}");

        // Send I2C-only (no flash) for fast slider response.
        // Flash persistence is debounced — only written after slider settles.
        var applied = await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false).ConfigureAwait(false);

        if (!applied)
        {
            StatusText = $"Analog audio gain change failed ({gainPercent:0}%)";
            return;
        }

        StatusText = $"Analog audio gain set to {gainPercent:0}%";
        WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);

        // Debounce flash persistence: cancel any pending write, schedule a new one after 300ms
        _gainFlashDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _gainFlashDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, cts.Token).ConfigureAwait(false);
                if (!cts.Token.IsCancellationRequested)
                {
                    await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer gain change — expected
            }
        }, cts.Token);

        if (persistSettings)
        {
            SaveSettings();
        }
    }

    private void WithAudioControlRefreshSuppressed(Action action)
    {
        _isRefreshingDeviceAudioControls = true;
        try { action(); }
        finally { _isRefreshingDeviceAudioControls = false; }
    }

    private string NormalizeDeviceAudioMode(string? mode)
        => string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase) ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;

    private async Task<bool> TryApplyAtDeviceAudioModeAsync(CaptureDevice device, string mode)
    {
        var analogMode = string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        var desiredSource = analogMode ? 1 : 0;

        // Read current input source to avoid redundant firmware switches
        var currentSource = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, 0x35, "InputSourceCheck").ConfigureAwait(false);
        if (currentSource is { Length: >= 1 } && currentSource[0] == desiredSource)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_SKIP mode='{mode}' already={desiredSource}");
            return true;
        }

        // Switching input source while the source reader is streaming crashes the USB link.
        // Stop the preview first, send the command, wait for the firmware to settle, restart.
        var wasPreviewing = IsPreviewing;
        if (wasPreviewing)
        {
            Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW mode='{mode}'");
            try
            {
                await StopPreviewAsync(userInitiated: false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_STOP_PREVIEW_WARN error={ex.Message}");
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        var inputApplied = await NativeXuAtCommandProvider.SetInputSourceAsync(device, desiredSource).ConfigureAwait(false);
        Logger.Log($"NATIVEXU_AUDIO_MODE_AT mode='{mode}' inputApplied={inputApplied}");

        if (wasPreviewing)
        {
            // The firmware re-enumerates the USB device with a new PID after input source
            // switch. We must re-discover devices to get the new symbolic link, then restart.
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var delayMs = attempt * 1000;
                Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_PREVIEW mode='{mode}' attempt={attempt} delayMs={delayMs}");
                await Task.Delay(delayMs).ConfigureAwait(false);
                try
                {
                    await RefreshDevicesAsync().ConfigureAwait(false);
                    await StartPreviewAsync(userInitiated: false).ConfigureAwait(false);
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_OK attempt={attempt}");
                    break;
                }
                catch (Exception ex) when (attempt < 5)
                {
                    Logger.Log($"NATIVEXU_AUDIO_MODE_AT_RESTART_RETRY attempt={attempt} error={ex.Message}");
                }
            }
        }

        return inputApplied;
    }

    /// <summary>
    /// Maps a 0-100% slider position to a 0-255 gain byte using a log audio taper.
    /// Standard audio curve: gain = (e^(k*x) - 1) / (e^k - 1), where k controls steepness.
    /// k=4 gives a natural audio-fader feel across the full 0-255 range.
    /// </summary>
    private const double GainCurveK = 4.0;

    private static byte MapPercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        // Log curve: moves quickly through quiet low bytes, more resolution at top
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    private static double MapGainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        // Inverse: exponential
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }

    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");

        if (value)
        {
            // Re-enable audio preview and start it if we're already previewing
            IsAudioPreviewEnabled = true;
            if (IsPreviewing && IsInitialized)
            {
                EnqueueUiOperation(() => _sessionCoordinator.StartAudioPreviewAsync(), "audio preview restart");
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
                await _sessionCoordinator.StopAudioPreviewAsync(teardownCapture: true);
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
        AudioPeak = 0;
        Volatile.Write(ref AudioMeterTarget, 0.0);
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        AudioClipping = false;
    }

    public void ResetAudioMeterTimerFlag()
    {
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
    }

    private double UpdateMeterLevel(double peak)
    {
        var targetDb = peak > 0 ? 20.0 * Math.Log10(peak) : MeterFloorDb;
        if (targetDb < MeterFloorDb) targetDb = MeterFloorDb;
        if (targetDb > 0) targetDb = 0;

        var nowTick = Environment.TickCount64;
        if (_audioMeterLastTick == 0)
        {
            _audioMeterDb = targetDb;
            _audioMeterLastTick = nowTick;
        }
        else
        {
            var dtSeconds = Math.Max(0, (nowTick - _audioMeterLastTick) / 1000.0);
            _audioMeterLastTick = nowTick;

            if (targetDb >= _audioMeterDb)
            {
                _audioMeterDb = targetDb;
            }
            else
            {
                var decay = MeterDecayDbPerSecond * dtSeconds;
                _audioMeterDb = Math.Max(targetDb, _audioMeterDb - decay);
            }
        }

        var level = (_audioMeterDb - MeterFloorDb) / -MeterFloorDb;
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

    private async Task InitializeDeviceAsync()
    {
        Logger.Log("=== InitializeDeviceAsync BEGIN ===");
        System.Diagnostics.Debug.WriteLine("=== InitializeDeviceAsync BEGIN ===");

        if (SelectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            System.Diagnostics.Debug.WriteLine("ERROR: SelectedDevice is NULL");
            return;
        }

        Logger.Log($"Device: {SelectedDevice.Name} (ID: {SelectedDevice.Id})");
        System.Diagnostics.Debug.WriteLine($"Device: {SelectedDevice.Name} (ID: {SelectedDevice.Id})");

        try
        {
            StatusText = "Initializing device...";
            var settings = BuildCaptureSettings();
            Logger.Log($"Settings: {settings.Width}x{settings.Height} @ {settings.FrameRate}fps");
            Logger.Log($"Format: {settings.Format}, HDR: {settings.HdrEnabled}, Audio: {settings.AudioEnabled}");
            System.Diagnostics.Debug.WriteLine($"Settings: {settings.Width}x{settings.Height} @ {settings.FrameRate}fps");
            System.Diagnostics.Debug.WriteLine($"Format: {settings.Format}, HDR: {settings.HdrEnabled}, Audio: {settings.AudioEnabled}");

            await _sessionCoordinator.InitializeAsync(SelectedDevice, settings);
            Logger.Log("✓ CaptureService initialized");
            System.Diagnostics.Debug.WriteLine("✓ CaptureService initialized");

            IsInitialized = true;
            StatusText = "Device ready";
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"✗ EXCEPTION: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
            StatusText = $"Failed to initialize: {ex.Message}";
            IsInitialized = false;
        }

        Logger.Log("=== InitializeDeviceAsync END ===");
        System.Diagnostics.Debug.WriteLine("=== InitializeDeviceAsync END ===");
    }

    public async Task StartPreviewAsync(bool userInitiated = true)
    {
        if (userInitiated)
        {
            _cancelPreviewRestartAfterReinitialize = false;
        }

        PreviewStartRequested?.Invoke(this, EventArgs.Empty);
        Logger.Log("=== StartPreviewAsync (ViewModel) BEGIN ===");
        Logger.Log($"IsInitialized: {IsInitialized}");
        System.Diagnostics.Debug.WriteLine("=== StartPreviewAsync (ViewModel) BEGIN ===");
        System.Diagnostics.Debug.WriteLine($"IsInitialized: {IsInitialized}");

        if (!IsInitialized)
        {
            Logger.Log("Device not initialized, initializing now...");
            System.Diagnostics.Debug.WriteLine("Device not initialized, initializing now...");
            await InitializeDeviceAsync();
        }

        Logger.Log($"After initialization - IsInitialized: {IsInitialized}");
        System.Diagnostics.Debug.WriteLine($"After initialization - IsInitialized: {IsInitialized}");

        if (IsInitialized)
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartVideoPreviewAsync(settings).ConfigureAwait(true);

            Logger.Log("Setting IsPreviewing = true");
            System.Diagnostics.Debug.WriteLine("Setting IsPreviewing = true");
            IsPreviewing = true;
            StatusText = "Preview starting...";

            // Start audio preview if enabled
            if (IsAudioPreviewEnabled && IsAudioEnabled)
            {
                Logger.Log("Starting audio preview...");
                await _sessionCoordinator.StartAudioPreviewAsync();
            }

            ApplySourceTelemetrySnapshot(_captureService.GetLatestSourceTelemetrySnapshot(), allowAutoRetarget: true);
        }
        else
        {
            Logger.Log("✗ Cannot start preview - device not initialized");
            System.Diagnostics.Debug.WriteLine("✗ Cannot start preview - device not initialized");
            StatusText = "Cannot start preview - device not initialized";
        }

        Logger.Log("=== StartPreviewAsync (ViewModel) END ===");
        System.Diagnostics.Debug.WriteLine("=== StartPreviewAsync (ViewModel) END ===");
    }

    public async Task StopPreviewAsync(bool userInitiated = true)
    {
        if (userInitiated && IsPreviewReinitializing)
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }

        PreviewStopRequested?.Invoke(this, EventArgs.Empty);
        await _sessionCoordinator.StopVideoPreviewAsync();
        IsPreviewing = false;

        // Stop audio preview
        if (_captureService.IsAudioPreviewActive)
        {
            await _sessionCoordinator.StopAudioPreviewAsync();
        }

        if (!IsPreviewReinitializing)
        {
            StatusText = "Preview stopped";
        }
    }

    public async Task ToggleRecordingAsync()
    {
        if (Interlocked.CompareExchange(ref _recordingToggleInProgress, 1, 0) != 0)
        {
            Logger.Log("Recording toggle rejected: operation already in progress.");
            return;
        }

        try
        {
            IsRecordingTransitioning = true;

            if (IsRecording)
            {
                StatusText = "Stopping recording...";
                await StopRecordingAsync();
            }
            else
            {
                StatusText = "Starting recording...";
                await StartRecordingAsync();
            }
        }
        finally
        {
            IsRecordingTransitioning = false;
            Interlocked.Exchange(ref _recordingToggleInProgress, 0);
        }
    }

    private async Task StartRecordingAsync()
    {
        if (SelectedDevice == null)
        {
            StatusText = "No device selected";
            return;
        }

        if (!IsInitialized)
        {
            await InitializeDeviceAsync();
        }

        try
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartRecordingAsync(settings);

            IsRecording = true;
            _recordingStopwatch.Restart();
            _bitrateSamples.Clear();
            RecordingSizeInfo = "0 B";
            RecordingBitrateInfo = "--";
            StatusText = "Recording...";
        }
        catch (Exception ex)
        {
            StatusText = $"Recording failed: {ex.Message}";
        }
    }

    private async Task StopRecordingAsync()
    {
        // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
        // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
        _recordingStopwatch.Stop();

        try
        {
            await _sessionCoordinator.StopRecordingAsync();
            IsRecording = false;
            StatusText = $"Recording saved ({RecordingTime})";
        }
        catch (Exception ex)
        {
            // Even if finalization fails, unblock the UI and allow subsequent attempts.
            IsRecording = false;
            StatusText = $"Stop recording failed: {ex.Message}";
        }
    }

    public async Task BrowseOutputPathAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            // Initialize the picker with the window handle for WinUI 3
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                OutputPath = folder.Path;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error selecting folder: {ex.Message}";
        }
    }

    public Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => RefreshDevicesAsync(), cancellationToken);

    public Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var target = ResolveDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Capture device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            SelectedDevice = target;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SelectAudioInputDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var target = ResolveAudioDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Audio input device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            SelectedAudioInputDevice = target;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetCustomAudioInputEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Custom audio input cannot be changed while recording.");
            }

            IsCustomAudioInputEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableResolutions.FirstOrDefault(r =>
                string.Equals(r.Value, resolution, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Resolution '{resolution}' is not available.");
            }
            if (!matched.IsEnabled)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(matched.DisableReason)
                        ? $"Resolution '{resolution}' is currently disabled."
                        : matched.DisableReason);
            }

            SelectedResolution = matched.Value;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (AvailableFrameRates.Count == 0)
            {
                throw new InvalidOperationException("No frame rates are available.");
            }

            var enabledRates = AvailableFrameRates
                .Where(rate => rate.IsEnabled)
                .ToList();
            if (enabledRates.Count == 0)
            {
                throw new InvalidOperationException("No enabled frame rates are available for the current selection.");
            }

            if (IsAutoFrameRateValue(frameRate))
            {
                var autoRate = enabledRates.FirstOrDefault(rate => IsAutoFrameRateValue(rate.Value));
                if (autoRate == null)
                {
                    throw new InvalidOperationException("Auto frame rate is not available for the current selection.");
                }

                SelectAutoFrameRate();
                return Task.CompletedTask;
            }

            var requestedFriendly = Math.Round(frameRate);
            var friendlyMatches = enabledRates
                .Where(rate => Math.Round(rate.FriendlyValue) == requestedFriendly)
                .OrderBy(rate => Math.Abs(rate.FriendlyValue - frameRate))
                .ThenBy(rate => Math.Abs(rate.Value - frameRate))
                .ToList();

            var matched = (friendlyMatches.Count > 0 ? friendlyMatches : enabledRates)
                .OrderBy(rate => Math.Abs(rate.Value - frameRate))
                .First();

            if (friendlyMatches.Count == 0 && !IsFrameRateMatch(matched.Value, frameRate))
            {
                throw new InvalidOperationException(
                    $"Frame rate '{frameRate:0.###}' is not available for {SelectedResolution ?? "the current resolution"}.");
            }

            SelectedFrameRate = matched.Value;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableRecordingFormats.FirstOrDefault(value =>
                string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Recording format '{format}' is not available.");
            }
            if (IsHdrEnabled && !IsHdrCompatibleRecordingFormat(matched))
            {
                throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
            }

            SelectedRecordingFormat = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailablePresets.FirstOrDefault(value =>
                string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Preset '{preset}' is not available.");
            }

            SelectedPreset = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableSplitEncodeModes.FirstOrDefault(value =>
                string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
            }

            SelectedSplitEncodeMode = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetShowAllCaptureOptionsAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            ShowAllCaptureOptions = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);
            SavePreviewVolume();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
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

    public Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsSettingsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableQualities.FirstOrDefault(value =>
                string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Quality '{quality}' is not available.");
            }

            SelectedQuality = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            CustomBitrateMbps = Math.Clamp(bitrateMbps, 1, 300);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);
            }

            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            IsHdrEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("True HDR preview cannot be changed while recording.");
            }

            IsTrueHdrPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("Output path cannot be empty.");
            }

            Directory.CreateDirectory(outputPath);
            OutputPath = outputPath;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (!enabled && IsPreviewReinitializing)
            {
                CancelPendingPreviewRestart();
                if (!IsPreviewing)
                {
                    return;
                }
            }

            if (enabled == IsPreviewing)
            {
                return;
            }

            if (enabled)
            {
                await StartPreviewAsync(userInitiated: true);
            }
            else
            {
                await StopPreviewAsync(userInitiated: true);
            }
        }, cancellationToken);
    }

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (enabled == IsRecording)
            {
                return;
            }

            if (enabled)
            {
                await StartRecordingAsync();
            }
            else
            {
                await StopRecordingAsync();
            }
        }, cancellationToken);
    }

    private CaptureDevice? ResolveDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = Devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return Devices.FirstOrDefault(d => string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private AudioInputDevice? ResolveAudioDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = AudioInputDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return AudioInputDevices.FirstOrDefault(d => string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private CaptureSettings BuildCaptureSettings()
    {
        var format = SelectedRecordingFormat switch
        {
            "HEVC (MP4)" => RecordingFormat.HevcMp4,
            "AV1 (MP4)" => RecordingFormat.Av1Mp4,
            _ => RecordingFormat.H264Mp4
        };

        var quality = SelectedQuality switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Super High" => VideoQuality.SuperHigh,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };

        var selectedFrameRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = IsAutoResolutionValue(SelectedResolution) && AutoResolvedFrameRate.HasValue && AutoResolvedFrameRate.Value > 0
            ? AutoResolvedFrameRate.Value
            : SelectedFrameRate > 0
            ? SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? SelectedFormat?.FrameRateExact
                ?? 60;
        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate;
        var runtimeRateArg = runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (effectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (runtime.ActualWidth == effectiveWidth && runtime.ActualHeight == effectiveHeight) ||
                (runtime.NegotiatedWidth == effectiveWidth && runtime.NegotiatedHeight == effectiveHeight);
        }

        if (runtimeMatchesResolution &&
            runtimeRate.HasValue &&
            runtimeRate.Value > 0 &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, runtimeRate.Value))
        {
            if (!string.IsNullOrWhiteSpace(runtimeRateArg))
            {
                requestedFrameRateArg = runtimeRateArg;
            }

            if (runtime.NegotiatedFrameRateNumerator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = runtime.NegotiatedFrameRateDenominator;
            }
            else if (TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (sourceTelemetry.HasFrameRate &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, sourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(sourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = sourceTelemetry.FrameRateArg;
            }

            if (TryParseFrameRateRational(sourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
            {
                requestedFrameRateNumerator = sourceNumerator;
                requestedFrameRateDenominator = sourceDenominator;
            }
        }

        if ((requestedFrameRateNumerator == null || requestedFrameRateDenominator == null) &&
            TryParseFrameRateRational(requestedFrameRateArg, out var parsedNumerator, out var parsedDenominator))
        {
            requestedFrameRateNumerator = parsedNumerator;
            requestedFrameRateDenominator = parsedDenominator;
        }

        if (requestedFrameRateNumerator == null || requestedFrameRateDenominator == null)
        {
            if (SelectedFormat?.FrameRateNumerator > 0 && SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = SelectedFormat.FrameRateRational;
            }
            else
            {
                requestedFrameRateArg = effectiveFrameRate.ToString("0.###");
            }
        }

        var settings = new CaptureSettings
        {
            Width = effectiveResolutionKnown ? effectiveWidth : (SelectedFormat?.Width ?? 1920),
            Height = effectiveResolutionKnown ? effectiveHeight : (SelectedFormat?.Height ?? 1080),
            FrameRate = effectiveFrameRate,
            RequestedFrameRateArg = requestedFrameRateArg,
            RequestedFrameRateNumerator = requestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedFrameRateDenominator,
            RequestedPixelFormat = string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase)
                ? SelectedFormat?.PixelFormat
                : SelectedVideoFormat,
            ForceMjpegDecode = string.Equals(SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase),
            Format = format,
            Quality = quality,
            NvencPreset = SelectedPreset,
            SplitEncodeMode = SelectedSplitEncodeMode,
            CustomBitrateMbps = CustomBitrateMbps,
            HdrEnabled = IsHdrEnabled,
            HdrOutputMode = IsHdrEnabled ? HdrOutputMode.Hdr10Pq : HdrOutputMode.Off,
            PreviewMode = IsTrueHdrPreviewEnabled ? PreviewMode.TrueHdr : PreviewMode.GpuFast,
            OutputPath = OutputPath,
            AudioEnabled = IsAudioEnabled,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8)
        };

        settings.UseCustomAudioInput = IsCustomAudioInputEnabled;
        if (IsCustomAudioInputEnabled && SelectedAudioInputDevice != null)
        {
            settings.AudioDeviceId = SelectedAudioInputDevice.Id;
            settings.AudioDeviceName = SelectedAudioInputDevice.Name;
        }

        return settings;
    }

    private async Task DisposeCoreAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 1)
        {
            return;
        }

        _gainFlashDebounceCts?.Cancel();
        _gainFlashDebounceCts?.Dispose();
        _timer?.Stop();
        _deviceService.FormatProbeCompleted -= OnDeviceFormatProbeCompleted;
        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.AudioLevelUpdated -= OnAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated -= OnSourceTelemetryUpdated;
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
