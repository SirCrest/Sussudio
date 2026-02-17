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
using Windows.Media.Capture;
using Windows.Storage.Pickers;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable
{
    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly ICaptureSessionCoordinator _sessionCoordinator;
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
    public partial ObservableCollection<FrameRateOption> AvailableFrameRates { get; set; } = new();

    [ObservableProperty]
    public partial double SelectedFrameRate { get; set; } = 60;

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
    private bool _hasUserOverriddenFrameRateForCurrentMode;
    private bool _hasUserOverriddenResolutionForCurrentMode;
    private bool _forceSourceAutoRetarget;
    private string? _lastSourceModeKey;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private int? _lastTelemetryAgeBucket;
    private List<string> _detectedRecordingFormats = new();

    // Flag to prevent reinitialization during initial device setup
    private bool _isChangingDevice;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableRecordingFormats { get; set; } = new() { DefaultRecordingFormat, "HEVC (MP4)", "Uncompressed (AVI)" };

    [ObservableProperty]
    public partial string SelectedRecordingFormat { get; set; } = DefaultRecordingFormat;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableQualities { get; set; } = new() { "Auto", "Low", "Medium", "High", "Very High", "Lossless", "Custom" };

    [ObservableProperty]
    public partial string SelectedQuality { get; set; } = "High";

    [ObservableProperty]
    public partial double CustomBitrateMbps { get; set; } = 50;

    [ObservableProperty]
    public partial bool IsCustomBitrateVisible { get; set; }

    [ObservableProperty]
    public partial bool IsHdrEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsHdrAvailable { get; set; }

    [ObservableProperty]
    public partial string HdrResolutionSupportHint { get; set; } = string.Empty;

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
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    [ObservableProperty]
    public partial double AudioPeak { get; set; }

    [ObservableProperty]
    public partial bool AudioClipping { get; set; }

    private const double MeterFloorDb = -60.0;
    private const double MeterDecayDbPerSecond = 40.0 / 1.7; // OBS-like PPM decay
    private double _audioMeterDb = MeterFloorDb;
    private long _audioMeterLastTick;
    private int _disposeState;

    public MediaCapture? MediaCapture => _captureService.MediaCapture;
    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRuntimeSnapshot(), cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetHealthSnapshot(), cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRecordingStats(), cancellationToken);


    public MainViewModel()
    {
        _deviceService = new DeviceService();
        _captureService = new CaptureService();
        _sessionCoordinator = new CaptureSessionCoordinator(_captureService);
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;
        _latestSourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);

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
            CustomBitrateMbps = CustomBitrateMbps,
            IsHdrEnabled = IsHdrEnabled,
            OutputPath = OutputPath,
            RecordingTime = RecordingTime,
            RecordingSizeInfo = RecordingSizeInfo,
            RecordingBitrateInfo = RecordingBitrateInfo,
            AudioPeak = AudioPeak,
            AudioClipping = AudioClipping
        }, cancellationToken);
    }

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
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
        await RefreshRecordingFormatsAsync();
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

        formats.Add("Uncompressed (AVI)");

        void ApplyFormats()
        {
            _detectedRecordingFormats = formats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
            if (IsRecording)
            {
                RecordingTime = _recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                UpdateRecordingStats();
            }
            UpdateDiskSpace();
            RefreshSourceTelemetrySummaryAge();
        };
        _timer.Start();
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
            StatusText = status;
        });
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StatusText = $"Error: {ex.Message}";
        });
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            AudioPeak = UpdateMeterLevel(e.Peak);
            AudioClipping = e.Clipped;
        });
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
            _lastSourceModeKey = modeKey;
            if (allowAutoRetarget)
            {
                _forceSourceAutoRetarget = true;
                _hasUserOverriddenResolutionForCurrentMode = false;
                _hasUserOverriddenFrameRateForCurrentMode = false;
            }
        }

        var shouldRebuildModeOptions = allowAutoRetarget &&
                                       (_forceSourceAutoRetarget ||
                                        (snapshot.HasSignalData && AvailableResolutions.Count == 0));
        if (shouldRebuildModeOptions)
        {
            RebuildResolutionOptions();
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
        SourceTargetSummaryText = $"Target: {(SelectedResolution ?? "?")} @ {friendly:0} (exact {exactText})";
    }

    public async Task RefreshDevicesAsync()
    {
        StatusText = "Scanning for devices...";
        Devices.Clear();
        AudioInputDevices.Clear();

        try
        {
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var audioDevices = await _deviceService.EnumerateAudioCaptureDevicesAsync();
            foreach (var audioDevice in audioDevices)
            {
                AudioInputDevices.Add(audioDevice);
            }

            if (AudioInputDevices.Count > 0)
            {
                SelectedAudioInputDevice = AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
                    ?? AudioInputDevices[0];
            }

            var devices = await _deviceService.EnumerateVideoCaptureDevicesAsync();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
            var discoverySummary = _deviceService.LastDiscoverySummary;
            Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

            if (Devices.Count > 0)
            {
                StatusText = $"Found {Devices.Count} device(s)";

                SelectedDevice = Devices[0];
                Logger.Log($"Auto-selected device: {SelectedDevice?.Name}");

                // Auto-start preview (StartPreviewAsync will initialize device if needed)
                await StartPreviewAsync();
            }
            else
            {
                StatusText = "No compatible video capture devices found (see log for discovery summary)";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error scanning devices: {ex.Message}";
        }
    }

    partial void OnSelectedDeviceChanged(CaptureDevice? value)
    {
        // Set flag to prevent format change from triggering reinitialization during device setup
        _isChangingDevice = true;
        ResetFrameRateSelectionState();
        HdrResolutionSupportHint = string.Empty;

        AvailableFormats.Clear();
        AvailableResolutions.Clear();
        AvailableFrameRates.Clear();
        _resolutionToFormats.Clear();
        ApplySourceTelemetrySnapshot(
            SourceSignalTelemetrySnapshot.CreateUnavailable("awaiting-source-telemetry"),
            allowAutoRetarget: false);

        if (value != null)
        {
            // Build resolution-to-format capability matrix from device's supported formats.
            foreach (var format in value.SupportedFormats)
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

            IsHdrAvailable = value.IsHdrCapable;
            if (!IsHdrAvailable)
            {
                IsHdrEnabled = false;
            }

            RebuildResolutionOptions();

            // Device initialization is now handled by StartPreviewAsync
        }

        _isChangingDevice = false;
    }

    partial void OnSelectedResolutionChanged(string? value)
    {
        if (!_isRebuildingModeOptions && !_isApplyingAutomaticResolutionSelection)
        {
            _hasUserOverriddenResolutionForCurrentMode = true;
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
        if (!_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection)
        {
            _hasUserOverriddenFrameRateForCurrentMode = true;
        }

        var selected = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, value));
        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(value, MidpointRounding.AwayFromZero);
        SelectedExactFrameRate = selected?.Value ?? value;
        SelectedExactFrameRateArg = selected?.Rational;

        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void UpdateSelectedFormat()
    {
        if (!TryParseResolutionKey(SelectedResolution, out var width, out var height))
        {
            SelectedFormat = null;
            return;
        }

        var candidates = AvailableFormats
            .Where(f => f.Width == width && f.Height == height)
            .ToList();
        if (IsHdrEnabled)
        {
            candidates = candidates.Where(f => f.IsHdr).ToList();
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

        var timingFamily = ResolvePreferredTimingFamily(SelectedResolution, SelectedFrameRate);
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
        var sourceFormats = _detectedRecordingFormats.Count > 0
            ? _detectedRecordingFormats
            : AvailableRecordingFormats.ToList();
        var formats = IsHdrEnabled
            ? sourceFormats.Where(IsHdrCompatibleRecordingFormat).ToList()
            : sourceFormats.ToList();

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

    private int GetHdrPixelFormatRank(MediaFormat format)
    {
        if (!IsHdrEnabled)
        {
            return 0;
        }

        if (format.PixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return format.IsHdr ? 1 : 2;
    }

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (!_isChangingDevice)
        {
            ResetModeSelectionState();
            RebuildResolutionOptions();
            RebuildRecordingFormatOptions();
        }
    }

    private void RebuildResolutionOptions()
    {
        var previousSelection = SelectedResolution;
        var previousRate = SelectedFrameRate;
        var options = _resolutionToFormats
            .Select(entry =>
            {
                var formats = entry.Value;
                var first = formats[0];
                var hdrSupported = formats.Any(f => f.IsHdr);
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

        string? hdrHint = null;
        var allowSourceAutoSelect = _forceSourceAutoRetarget || !_hasUserOverriddenResolutionForCurrentMode;
        var sourceSelected = allowSourceAutoSelect
            ? TrySelectSourceResolutionOption(options, previousSelection)
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
        if (selected == null)
        {
            selected = IsHdrEnabled
                ? SelectHdrResolutionOption(options, previousSelection, previousRate, out hdrHint)
                : options.FirstOrDefault(option =>
                    option.IsEnabled &&
                    string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase))
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

        _isRebuildingModeOptions = true;
        try
        {
            AvailableResolutions.Clear();
            foreach (var option in options)
            {
                AvailableResolutions.Add(option);
            }

            _isApplyingAutomaticResolutionSelection = true;
            SelectedResolution = selected?.Value;
            _isApplyingAutomaticResolutionSelection = false;
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

    private void RebuildFrameRateOptions()
    {
        var previousRate = SelectedFrameRate;
        var options = new List<FrameRateOption>();
        var timingFamily = ResolvePreferredTimingFamily(SelectedResolution, previousRate);
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(SelectedResolution) &&
            _resolutionToFormats.TryGetValue(SelectedResolution, out var formats))
        {
            options = formats
                .GroupBy(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .Select(group =>
                {
                    var allFormats = group.ToList();
                    var hdrSupported = allFormats.Any(format => format.IsHdr);
                    var enabled = !IsHdrEnabled || hdrSupported;
                    var selectionPool = IsHdrEnabled && hdrSupported
                        ? allFormats.Where(format => format.IsHdr).ToList()
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

        var sourceRate = ResolveDetectedSourceFrameRate(SelectedResolution, options, previousRate);
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
                             ResolutionHasTimingFamilyVariant(SelectedResolution, option.FriendlyValue, sourceTimingFamily) &&
                             IsFriendlyFrameRateMatch(option.FriendlyValue, sourceFriendlyRate.Value) &&
                             option.Value > sourceRate.Rate.Value + 0.03)
                    {
                        enabled = false;
                        disableReason = $"Source timing is {sourceRate.Arg ?? sourceRate.Rate.Value.ToString("0.###")} so this duplicate variant is hidden.";
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
                    DisableReason = enabled ? string.Empty : disableReason
                };
            })
            .ToList();

        options = cappedOptions;
        DetectedSourceFrameRate = sourceRate.Rate;
        DetectedSourceFrameRateArg = sourceRate.Arg;
        SourceFrameRateOrigin = sourceRate.Origin;

        _isRebuildingModeOptions = true;
        try
        {
            AvailableFrameRates.Clear();
            foreach (var option in options)
            {
                AvailableFrameRates.Add(option);
            }

            FrameRateOption? selected = null;
            if ((_forceSourceAutoRetarget || !_hasUserOverriddenFrameRateForCurrentMode) && sourceRate.Rate.HasValue)
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

            selected ??= options.FirstOrDefault(option =>
                option.IsEnabled && IsFrameRateMatch(option.Value, previousRate))
                ?? options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate))
                ?? options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 60))
                ?? options.FirstOrDefault(option => option.IsEnabled)
                ?? options.FirstOrDefault();

            _isApplyingAutomaticFrameRateSelection = true;
            SelectedFrameRate = selected?.Value ?? 0;
            _isApplyingAutomaticFrameRateSelection = false;
            SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);
            SelectedExactFrameRate = selected?.Value;
            SelectedExactFrameRateArg = selected?.Rational;
            DisabledFrameRateReason = selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty;
            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = $"No HDR-capable frame rate is available for {SelectedResolution}.";
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

    private void ResetFrameRateSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
    }

    private void ResetModeSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
        _hasUserOverriddenResolutionForCurrentMode = false;
        _forceSourceAutoRetarget = false;
        _lastSourceModeKey = null;
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
        if (string.IsNullOrWhiteSpace(resolutionKey))
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
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        return formats.Any(format =>
            (!hdrOnly || format.IsHdr) &&
            IsFrameRateMatch(format.FrameRateExact, frameRate));
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
            ? formats.Where(format => format.IsHdr).ToList()
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
            .ThenBy(format => MediaFormat.GetPixelFormatPriority(format.PixelFormat))
            .ThenByDescending(format => format.IsHdr)
            .First();
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
        if (value != null && !_isChangingDevice && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Format changed to {value.Width}x{value.Height}@{value.FrameRate}fps - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("format change"), "format change reinitialize");
        }
    }

    private async Task ReinitializeDeviceAsync(string reason)
    {
        if (SelectedDevice == null || SelectedFormat == null)
            return;

        try
        {
            StatusText = "Applying new settings...";
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (IsPreviewing)
            {
                await StopPreviewAsync();
            }

            // Reinitialize the device with new settings
            IsInitialized = false;
            await InitializeDeviceAsync();

            // Restart preview
            if (IsInitialized)
            {
                await StartPreviewAsync();

                StatusText = $"Preview: {SelectedFormat.Width}x{SelectedFormat.Height}@{SelectedFormat.FrameRate}fps";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to apply format: {ex.Message}";
        }
    }

    partial void OnSelectedQualityChanged(string value)
    {
        IsCustomBitrateVisible = value == "Custom";
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

        // Toggle audio preview if preview is already running
        if (IsPreviewing && IsInitialized)
        {
            EnqueueUiOperation(async () =>
            {
                if (value)
                {
                    await _sessionCoordinator.StartAudioPreviewAsync();
                }
                else
                {
                    await _sessionCoordinator.StopAudioPreviewAsync();
                }
            }, "audio preview toggle");
        }
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

    partial void OnIsAudioEnabledChanged(bool value)
    {
        Logger.Log($"Audio capture enabled: {value}");

        if (!value)
        {
            if (IsAudioPreviewEnabled)
            {
                IsAudioPreviewEnabled = false;
            }

            if (_captureService.IsAudioPreviewActive)
            {
                EnqueueUiOperation(() => _sessionCoordinator.StopAudioPreviewAsync(), "audio preview stop");
            }

            ResetAudioMeter();
        }
    }

    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _bitrateSamples.Clear();
        }
    }

    private void ResetAudioMeter()
    {
        _audioMeterDb = MeterFloorDb;
        _audioMeterLastTick = 0;
        AudioPeak = 0;
        AudioClipping = false;
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
            RecordingBitrateInfo = "Bitrate: --";
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
            OnPropertyChanged(nameof(MediaCapture));
            Logger.Log($"MediaCapture object: {MediaCapture}");
            System.Diagnostics.Debug.WriteLine($"MediaCapture object: {MediaCapture}");
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

    public async Task StartPreviewAsync()
    {
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
            Logger.Log("Setting IsPreviewing = true");
            System.Diagnostics.Debug.WriteLine("Setting IsPreviewing = true");
            IsPreviewing = true;
            OnPropertyChanged(nameof(MediaCapture));
            Logger.Log($"MediaCapture in ViewModel: {MediaCapture}");
            System.Diagnostics.Debug.WriteLine($"MediaCapture in ViewModel: {MediaCapture}");
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

    public async Task StopPreviewAsync()
    {
        IsPreviewing = false;

        // Stop audio preview
        if (_captureService.IsAudioPreviewActive)
        {
            await _sessionCoordinator.StopAudioPreviewAsync();
        }

        StatusText = "Preview stopped";
    }

    public async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            await StopRecordingAsync();
        }
        else
        {
            await StartRecordingAsync();
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
        try
        {
            await _sessionCoordinator.StopRecordingAsync();
            IsRecording = false;
            _recordingStopwatch.Stop();
            StatusText = $"Recording saved ({RecordingTime})";
        }
        catch (Exception ex)
        {
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
            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            IsHdrEnabled = enabled;
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
            if (enabled == IsPreviewing)
            {
                return;
            }

            if (enabled)
            {
                await StartPreviewAsync();
            }
            else
            {
                await StopPreviewAsync();
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
            "Uncompressed (AVI)" => RecordingFormat.UncompressedAvi,
            _ => RecordingFormat.H264Mp4
        };

        var quality = SelectedQuality switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Very High" => VideoQuality.VeryHigh,
            "Lossless" => VideoQuality.Lossless,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };

        var selectedFrameRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? SelectedFrameRate;
        var runtimeRate = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate;
        var runtimeRateArg = runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (TryParseResolutionKey(SelectedResolution, out var selectedWidth, out var selectedHeight))
        {
            runtimeMatchesResolution =
                (runtime.ActualWidth == selectedWidth && runtime.ActualHeight == selectedHeight) ||
                (runtime.NegotiatedWidth == selectedWidth && runtime.NegotiatedHeight == selectedHeight);
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
                requestedFrameRateArg = SelectedFrameRate.ToString("0.###");
            }
        }

        var settings = new CaptureSettings
        {
            Width = SelectedFormat?.Width ?? 1920,
            Height = SelectedFormat?.Height ?? 1080,
            FrameRate = SelectedFrameRate,
            RequestedFrameRateArg = requestedFrameRateArg,
            RequestedFrameRateNumerator = requestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedFrameRateDenominator,
            RequestedPixelFormat = SelectedFormat?.PixelFormat,
            Format = format,
            Quality = quality,
            CustomBitrateMbps = CustomBitrateMbps,
            HdrEnabled = IsHdrEnabled,
            OutputPath = OutputPath,
            AudioEnabled = IsAudioEnabled
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

        _timer?.Stop();
        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.AudioLevelUpdated -= OnAudioLevelUpdated;
        _captureService.SourceTelemetryUpdated -= OnSourceTelemetryUpdated;
        var stepTimeoutMs = GetIntFromEnv(
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
        var disposeTimeoutMs = GetIntFromEnv(
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
        var disposeTimeoutMs = GetIntFromEnv(
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
