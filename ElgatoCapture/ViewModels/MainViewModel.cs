using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    [ObservableProperty]
    private ObservableCollection<CaptureDevice> _devices = new();

    [ObservableProperty]
    private CaptureDevice? _selectedDevice;

    [ObservableProperty]
    private ObservableCollection<MediaFormat> _availableFormats = new();

    [ObservableProperty]
    private MediaFormat? _selectedFormat;

    // Resolution/Frame Rate separation
    [ObservableProperty]
    private ObservableCollection<string> _availableResolutions = new();

    [ObservableProperty]
    private string? _selectedResolution;

    [ObservableProperty]
    private ObservableCollection<double> _availableFrameRates = new();

    [ObservableProperty]
    private double _selectedFrameRate = 60;

    // Mapping from resolution string to available frame rates
    private Dictionary<string, List<double>> _resolutionToFrameRates = new();

    // Flag to prevent reinitialization during initial device setup
    private bool _isChangingDevice;

    [ObservableProperty]
    private ObservableCollection<string> _availableRecordingFormats = new() { "H.264 (MP4)", "HEVC (MP4)", "Uncompressed (AVI)" };

    [ObservableProperty]
    private string _selectedRecordingFormat = "H.264 (MP4)";

    [ObservableProperty]
    private ObservableCollection<string> _availableQualities = new() { "Auto", "Low", "Medium", "High", "Very High", "Lossless", "Custom" };

    [ObservableProperty]
    private string _selectedQuality = "High";

    [ObservableProperty]
    private double _customBitrateMbps = 50;

    [ObservableProperty]
    private bool _isCustomBitrateVisible;

    [ObservableProperty]
    private bool _isHdrEnabled;

    [ObservableProperty]
    private bool _isHdrAvailable;

    [ObservableProperty]
    private bool _isAudioEnabled = true;

    [ObservableProperty]
    private bool _isAudioPreviewEnabled = true;

    [ObservableProperty]
    private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _recordingTime = "00:00:00";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isPreviewing;

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private string _diskSpaceInfo = "";

    public MediaCapture? MediaCapture => _captureService.MediaCapture;

    public event EventHandler? RequestPreviewStop;
    public event EventHandler? PreviewNeedsRestart;

    public MainViewModel()
    {
        _deviceService = new DeviceService();
        _captureService = new CaptureService();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _captureService.StatusChanged += OnCaptureStatusChanged;
        _captureService.ErrorOccurred += OnCaptureError;
        _captureService.FrameCaptured += OnFrameCaptured;
        _captureService.RequestPreviewStop += OnRequestPreviewStop;
        _captureService.PreviewNeedsRestart += OnPreviewNeedsRestart;

        SetupTimer();
        UpdateDiskSpace();
    }

    private void OnRequestPreviewStop(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Logger.Log("=== Preview stop requested (MediaCapture about to be disposed) ===");
            RequestPreviewStop?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnPreviewNeedsRestart(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Logger.Log("=== Preview needs restart (MediaCapture reinitialized) ===");
            OnPropertyChanged(nameof(MediaCapture));
            PreviewNeedsRestart?.Invoke(this, EventArgs.Empty);
        });
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

    public async Task RefreshDevicesAsync()
    {
        StatusText = "Scanning for devices...";
        Devices.Clear();

        try
        {
            var devices = await _deviceService.EnumerateVideoCaptureDevicesAsync();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            if (Devices.Count > 0)
            {
                StatusText = $"Found {Devices.Count} device(s)";

                // Allow UI to process the devices collection update before selecting
                await Task.Delay(50);

                SelectedDevice = Devices[0];
                Logger.Log($"Auto-selected device: {SelectedDevice?.Name}");

                // Auto-start preview (StartPreviewAsync will initialize device if needed)
                await StartPreviewAsync();
            }
            else
            {
                StatusText = "No video capture devices found";
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
                await ReinitializeWithNewFormatAsync();
            });
        }
    }

    private async Task ReinitializeWithNewFormatAsync()
    {
        if (SelectedDevice == null || SelectedFormat == null)
            return;

        try
        {
            StatusText = "Applying new format...";

            // Stop preview (this will stop the frame reader in MainWindow)
            IsPreviewing = false;

            // Small delay to let UI update
            await Task.Delay(100);

            // Reinitialize the device with new settings
            IsInitialized = false;
            await InitializeDeviceAsync();

            // Restart preview
            if (IsInitialized)
            {
                IsPreviewing = true;
                OnPropertyChanged(nameof(MediaCapture));

                // Restart audio preview if it was enabled
                if (IsAudioPreviewEnabled)
                {
                    await _captureService.StartAudioPreviewAsync();
                }

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

    partial void OnIsAudioPreviewEnabledChanged(bool value)
    {
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
            if (IsAudioPreviewEnabled)
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

        return new CaptureSettings
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
    }

    public void Dispose()
    {
        _timer?.Stop();
        _captureService.StatusChanged -= OnCaptureStatusChanged;
        _captureService.ErrorOccurred -= OnCaptureError;
        _captureService.FrameCaptured -= OnFrameCaptured;
        _captureService.RequestPreviewStop -= OnRequestPreviewStop;
        _captureService.PreviewNeedsRestart -= OnPreviewNeedsRestart;
        _captureService.Dispose();
    }
}
