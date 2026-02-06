using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using Microsoft.UI.Dispatching;
using Windows.Media.Capture;
using Windows.Storage.Pickers;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Stopwatch _recordingStopwatch = new();
    private DispatcherQueueTimer? _timer;
    private IntPtr _windowHandle;
    private readonly Queue<(long Tick, long Bytes)> _bitrateSamples = new();
    private const int BitrateWindowMs = 5000;

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
    public partial ObservableCollection<string> AvailableResolutions { get; set; } = new();

    [ObservableProperty]
    public partial string? SelectedResolution { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<double> AvailableFrameRates { get; set; } = new();

    [ObservableProperty]
    public partial double SelectedFrameRate { get; set; } = 60;

    // Mapping from resolution string to available frame rates
    private Dictionary<string, List<double>> _resolutionToFrameRates = new();

    // Flag to prevent reinitialization during initial device setup
    private bool _isChangingDevice;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableRecordingFormats { get; set; } = new() { "H.264 (MP4)", "HEVC (MP4)", "Uncompressed (AVI)" };

    [ObservableProperty]
    public partial string SelectedRecordingFormat { get; set; } = "H.264 (MP4)";

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

    public MediaCapture? MediaCapture => _captureService.MediaCapture;


    public MainViewModel()
    {
        _deviceService = new DeviceService();
        _captureService = new CaptureService();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.AudioLevelUpdated += OnAudioLevelUpdated;

        SetupTimer();
        UpdateDiskSpace();
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
            AvailableRecordingFormats.Clear();
            foreach (var format in formats)
            {
                AvailableRecordingFormats.Add(format);
            }

            var preferred = "H.264 (MP4)";
            if (formats.Contains(preferred))
            {
                SelectedRecordingFormat = preferred;
            }
            else if (!string.IsNullOrWhiteSpace(formats.FirstOrDefault()))
            {
                SelectedRecordingFormat = formats.First();
            }
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
        };
        _timer.Start();
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

        AvailableFormats.Clear();
        AvailableResolutions.Clear();
        AvailableFrameRates.Clear();
        _resolutionToFrameRates.Clear();

        if (value != null)
        {
            // Build resolution-to-framerate mapping from device's supported formats
            foreach (var format in value.SupportedFormats)
            {
                AvailableFormats.Add(format);

                var resolutionKey = $"{format.Width}x{format.Height}";
                if (!_resolutionToFrameRates.ContainsKey(resolutionKey))
                {
                    _resolutionToFrameRates[resolutionKey] = new List<double>();
                }

                if (!_resolutionToFrameRates[resolutionKey].Contains(format.FrameRate))
                {
                    _resolutionToFrameRates[resolutionKey].Add(format.FrameRate);
                }
            }

            // Sort frame rates descending for each resolution
            foreach (var key in _resolutionToFrameRates.Keys.ToList())
            {
                _resolutionToFrameRates[key] = _resolutionToFrameRates[key].OrderByDescending(f => f).ToList();
            }

            // Add unique resolutions sorted by pixel count descending
            var sortedResolutions = _resolutionToFrameRates.Keys
                .OrderByDescending(r =>
                {
                    var parts = r.Split('x');
                    return int.Parse(parts[0]) * int.Parse(parts[1]);
                })
                .ToList();

            foreach (var resolution in sortedResolutions)
            {
                AvailableResolutions.Add(resolution);
            }

            // Select first resolution (will trigger OnSelectedResolutionChanged)
            if (AvailableResolutions.Count > 0)
            {
                SelectedResolution = AvailableResolutions[0];
            }

            IsHdrAvailable = value.IsHdrCapable;
            if (!IsHdrAvailable)
            {
                IsHdrEnabled = false;
            }

            // Device initialization is now handled by StartPreviewAsync
        }

        _isChangingDevice = false;
    }

    partial void OnSelectedResolutionChanged(string? value)
    {
        AvailableFrameRates.Clear();

        if (value != null && _resolutionToFrameRates.TryGetValue(value, out var frameRates))
        {
            foreach (var frameRate in frameRates)
            {
                AvailableFrameRates.Add(frameRate);
            }

            // Prefer 60fps as default if available, otherwise use highest
            if (AvailableFrameRates.Count > 0)
            {
                var preferred60 = AvailableFrameRates.FirstOrDefault(f => Math.Abs(f - 60) < 0.1);
                SelectedFrameRate = preferred60 > 0 ? preferred60 : AvailableFrameRates[0];
            }
        }

        UpdateSelectedFormat();
    }

    partial void OnSelectedFrameRateChanged(double value)
    {
        UpdateSelectedFormat();
    }

    private void UpdateSelectedFormat()
    {
        // Find the MediaFormat matching current resolution and frame rate
        if (SelectedResolution != null)
        {
            var parts = SelectedResolution.Split('x');
            if (parts.Length == 2 && uint.TryParse(parts[0], out var width) && uint.TryParse(parts[1], out var height))
            {
                SelectedFormat = AvailableFormats.FirstOrDefault(f =>
                    f.Width == width && f.Height == height && Math.Abs(f.FrameRate - SelectedFrameRate) < 0.1);
            }
        }
    }

    partial void OnSelectedFormatChanged(MediaFormat? value)
    {
        // If preview is active and this isn't during initial device setup, reinitialize with new format
        if (value != null && !_isChangingDevice && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Format changed to {value.Width}x{value.Height}@{value.FrameRate}fps - reinitializing device ===");
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await ReinitializeDeviceAsync("format change");
            });
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

        _dispatcherQueue.TryEnqueue(async () =>
        {
            await ApplyAudioInputSelectionAsync("custom audio toggle");
        });
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

        _dispatcherQueue.TryEnqueue(async () =>
        {
            await ApplyAudioInputSelectionAsync("custom audio device change");
        });
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
            _dispatcherQueue.TryEnqueue(async () =>
            {
                if (value)
                {
                    await _captureService.StartAudioPreviewAsync();
                }
                else
                {
                    await _captureService.StopAudioPreviewAsync();
                }
            });
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

        await _captureService.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);
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
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    await _captureService.StopAudioPreviewAsync();
                });
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

            await _captureService.InitializeAsync(SelectedDevice, settings);
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
                await _captureService.StartAudioPreviewAsync();
            }
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
            await _captureService.StopAudioPreviewAsync();
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
            await _captureService.StartRecordingAsync(settings);
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
            await _captureService.StopRecordingAsync();
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

        var settings = new CaptureSettings
        {
            Width = SelectedFormat?.Width ?? 1920,
            Height = SelectedFormat?.Height ?? 1080,
            FrameRate = SelectedFrameRate,
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

    public void Dispose()
    {
        _timer?.Stop();
        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.AudioLevelUpdated -= OnAudioLevelUpdated;
        _captureService.Dispose();
    }
}
