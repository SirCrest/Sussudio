using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Playback;
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private MediaFrameReader? _previewFrameReader;
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private MediaPlayer? _previewMediaPlayer;
    private bool _previewUsedLegacyStart;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastLogTick;
    private long _previewLastResizeLogTick;
    private long _previewLastPresentedTick;
    private long _previewResizeSuppressUntilTick;
    private long _previewGpuStartTick;
    private long _previewGpuLastPositionMs;
    private long _previewGpuLastProgressTick;
    private int _previewUiInFlight;
    private long _lastDiagnosticsUiUpdateTick;
    private readonly object _previewCadenceLock = new();
    private readonly double[] _previewDisplayIntervalWindowMs = new double[300];
    private int _previewDisplayIntervalCount;
    private int _previewDisplayIntervalIndex;
    private long _previewLastDisplayTick;
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private readonly bool _previewCapOverrideProvided;
    private readonly int _previewPresentationFpsCap;
    private int _previewActivePresentationFpsCap;
    private long _previewMinPresentationIntervalMs;
    private readonly int _previewResizeDebounceMs;
    private readonly bool _preferGpuPreview;
    private readonly bool _forceFrameReaderDuringRecording;
    private readonly int _previewShutdownTimeoutMs;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private int _automationServicesStarted;
    private DispatcherQueueTimer? _infoBarDismissTimer;

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static double GetFrameRate(MediaFrameFormat format)
    {
        if (format.FrameRate.Denominator == 0)
        {
            return 0;
        }

        return format.FrameRate.Numerator / (double)format.FrameRate.Denominator;
    }

    private static bool IsHdrSubtype(string? subtype)
        => !string.IsNullOrWhiteSpace(subtype) &&
           (subtype.Contains("P010", StringComparison.OrdinalIgnoreCase) ||
            subtype.Contains("HDR", StringComparison.OrdinalIgnoreCase));

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    private static bool TryGetIntFromEnv(string variableName, out int parsedValue)
    {
        parsedValue = 0;
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        return !string.IsNullOrWhiteSpace(rawValue) && int.TryParse(rawValue, out parsedValue);
    }

    private static bool GetBoolFromEnv(string variableName, bool defaultValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(rawValue, out var intValue))
        {
            return intValue != 0;
        }

        return defaultValue;
    }

    private int ResolvePreviewPresentationCap(MediaFrameFormat? activeFormat)
    {
        if (_previewCapOverrideProvided)
        {
            return _previewPresentationFpsCap;
        }

        return ComputeAdaptivePreviewCap(activeFormat);
    }

    private int ComputeAdaptivePreviewCap(MediaFrameFormat? activeFormat)
    {
        if (activeFormat == null)
        {
            return Math.Clamp(60, 15, _previewPresentationFpsCap);
        }

        var sourceFps = (int)Math.Round(GetFrameRate(activeFormat));
        if (sourceFps <= 0)
        {
            sourceFps = 60;
        }

        var pixelCount = (long)activeFormat.VideoFormat.Width * activeFormat.VideoFormat.Height;
        int adaptiveCap;

        if (pixelCount >= 3840L * 2160L)
        {
            adaptiveCap = sourceFps >= 60 ? 45 : 30;
        }
        else if (pixelCount >= 2560L * 1440L)
        {
            adaptiveCap = sourceFps >= 60 ? 60 : 45;
        }
        else if (pixelCount >= 1920L * 1080L)
        {
            adaptiveCap = Math.Min(sourceFps, 60);
        }
        else
        {
            adaptiveCap = Math.Min(sourceFps, 90);
        }

        return Math.Clamp(adaptiveCap, 15, _previewPresentationFpsCap);
    }

    private bool TryStartGpuPreview(MediaFrameSource colorSourceInfo)
    {
        if (!_preferGpuPreview)
        {
            Logger.Log("GPU preview path disabled via ELGATOCAPTURE_PREVIEW_USE_GPU.");
            return false;
        }

        try
        {
            StopGpuPreview();

            var mediaSource = MediaSource.CreateFromMediaFrameSource(colorSourceInfo);
            _previewMediaPlayer = new MediaPlayer
            {
                RealTimePlayback = true,
                IsMuted = true,
                Source = mediaSource
            };

            PreviewPlayerElement.SetMediaPlayer(_previewMediaPlayer);
            PreviewPlayerElement.Visibility = Visibility.Visible;
            PreviewImage.Visibility = Visibility.Collapsed;
            _previewMediaPlayer.Play();
            var nowTick = Environment.TickCount64;
            Interlocked.Exchange(ref _previewGpuStartTick, nowTick);
            Interlocked.Exchange(ref _previewGpuLastProgressTick, nowTick);
            Interlocked.Exchange(ref _previewGpuLastPositionMs, 0);
            Logger.Log("Preview started on GPU path (MediaPlayerElement).");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"GPU preview path unavailable: {ex.Message}. Falling back to frame-reader preview.");
            StopGpuPreview();
            return false;
        }
    }

    private void StopGpuPreview()
    {
        try
        {
            if (_previewMediaPlayer != null)
            {
                _previewMediaPlayer.Pause();
                _previewMediaPlayer.Source = null;
                _previewMediaPlayer.Dispose();
                _previewMediaPlayer = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"GPU preview cleanup warning: {ex.Message}");
        }
        finally
        {
            PreviewPlayerElement.SetMediaPlayer(null);
            PreviewPlayerElement.Visibility = Visibility.Collapsed;
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();
        var automationToken = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_TOKEN");
        var automationPipeName = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = "ElgatoCaptureAutomation";
        }

        _automationDiagnosticsHub = new AutomationDiagnosticsHub(
            ViewModel,
            GetPreviewRuntimeSnapshotAsync,
            new RecordingVerifier());
        _automationDiagnosticsHub.SnapshotUpdated += AutomationDiagnosticsHub_SnapshotUpdated;

        var automationDispatcher = new AutomationCommandDispatcher(
            ViewModel,
            _automationDiagnosticsHub,
            this,
            automationToken);
        _automationPipeServer = new NamedPipeAutomationServer(automationDispatcher, automationPipeName);
        _previewCapOverrideProvided = TryGetIntFromEnv("ELGATOCAPTURE_PREVIEW_FPS_CAP", out var previewCapOverrideRaw);
        _previewPresentationFpsCap = Math.Clamp(_previewCapOverrideProvided ? previewCapOverrideRaw : 120, 15, 120);
        _previewActivePresentationFpsCap = Math.Clamp(60, 15, _previewPresentationFpsCap);
        _previewMinPresentationIntervalMs = Math.Max(1, 1000 / _previewActivePresentationFpsCap);
        _previewResizeDebounceMs = GetIntFromEnv("ELGATOCAPTURE_PREVIEW_RESIZE_DEBOUNCE_MS", defaultValue: 250, minValue: 50, maxValue: 2000);
        _preferGpuPreview = GetBoolFromEnv("ELGATOCAPTURE_PREVIEW_USE_GPU", defaultValue: true);
        _forceFrameReaderDuringRecording = GetBoolFromEnv("ELGATOCAPTURE_FORCE_FRAME_READER_DURING_RECORDING", defaultValue: false);
        _previewShutdownTimeoutMs = GetIntFromEnv("ELGATOCAPTURE_PREVIEW_SHUTDOWN_TIMEOUT_MS", defaultValue: 3000, minValue: 250, maxValue: 30000);

        // Set window handle for folder picker
        var hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(hwnd);

        // Set initial window size and constraints
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

        // Ensure window is not maximized
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;

            // Force normal (non-maximized) state
            presenter.Restore();
        }

        // Set window size to accommodate 1920x1080 preview + UI controls
        // Height calculation: 1080px video + ~250px UI controls + ~120px padding/spacing/titlebar
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));

        // Set title bar icon
        appWindow.SetIcon("Assets\\AppIcon.ico");

        // Subscribe to ViewModel changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Wire up UI controls to ViewModel
        SetupBindings();

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        var mainContent = (FrameworkElement)this.Content;
        mainContent.Loaded += MainWindow_Loaded;
        mainContent.SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;

    }

    private void SetupBindings()
    {
        // Bind all collections to ComboBoxes
        DeviceComboBox.ItemsSource = ViewModel.Devices;
        AudioInputComboBox.ItemsSource = ViewModel.AudioInputDevices;
        ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
        FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
        FormatComboBox.ItemsSource = ViewModel.AvailableRecordingFormats;
        QualityComboBox.ItemsSource = ViewModel.AvailableQualities;

        // Subscribe to collection changes to sync SelectedItem after items are added
        ViewModel.AvailableFrameRates.CollectionChanged += (s, e) =>
        {
            // After items are added, sync the selected frame rate
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.SelectedFrameRate > 0 && ViewModel.AvailableFrameRates.Count > 0)
                    {
                        // Find matching item in the collection
                        var matchingRate = ViewModel.AvailableFrameRates
                            .FirstOrDefault(f => IsFrameRateMatch(f.Value, ViewModel.SelectedFrameRate));
                        if (matchingRate != null)
                        {
                            FrameRateComboBox.SelectedItem = matchingRate;
                        }
                    }
                });
            }
        };

        ViewModel.AvailableRecordingFormats.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ViewModel.AvailableRecordingFormats.Count == 0)
                {
                    return;
                }

                var matchingFormat = ViewModel.AvailableRecordingFormats
                    .FirstOrDefault(f => string.Equals(f, ViewModel.SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(matchingFormat))
                {
                    FormatComboBox.SelectedItem = matchingFormat;
                    return;
                }

                var fallbackFormat = ViewModel.AvailableRecordingFormats[0];
                FormatComboBox.SelectedItem = fallbackFormat;
                ViewModel.SelectedRecordingFormat = fallbackFormat;
            });
        };

        // Set initial values
        OutputPathTextBox.Text = ViewModel.OutputPath;
        DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
        RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
        RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
        AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        CustomAudioToggle.IsOn = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        var customAudioVisible = ViewModel.IsCustomAudioInputEnabled ? Visibility.Visible : Visibility.Collapsed;
        AudioInputLabel.Visibility = customAudioVisible;
        AudioInputComboBox.Visibility = customAudioVisible;
        AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        UpdateAudioMeterLevel(ViewModel.AudioPeak);
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
        HdrFpsHintTextBlock.Text = ViewModel.HdrResolutionSupportHint;
        HdrFpsHintTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.HdrResolutionSupportHint)
            ? Visibility.Collapsed
            : Visibility.Visible;
        SourceTelemetryTextBlock.Text = ViewModel.SourceTelemetrySummaryText;
        SourceTelemetryTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText)
            ? Visibility.Collapsed
            : Visibility.Visible;
        TargetTelemetryTextBlock.Text = ViewModel.SourceTargetSummaryText;
        TargetTelemetryTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText)
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Wire up selection changes with loop prevention
        DeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DeviceComboBox.SelectedItem != null &&
                DeviceComboBox.SelectedItem != ViewModel.SelectedDevice)
            {
                ViewModel.SelectedDevice = (ElgatoCapture.Models.CaptureDevice)DeviceComboBox.SelectedItem;
            }
        };

        AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (AudioInputComboBox.SelectedItem is ElgatoCapture.Models.AudioInputDevice device &&
                device != ViewModel.SelectedAudioInputDevice)
            {
                ViewModel.SelectedAudioInputDevice = device;
            }
        };

        ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (ResolutionComboBox.SelectedItem is ResolutionOption resolution &&
                resolution.IsEnabled &&
                !string.Equals(resolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedResolution = resolution.Value;
            }
        };

        FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (FrameRateComboBox.SelectedItem is FrameRateOption frameRate &&
                frameRate.IsEnabled &&
                !IsFrameRateMatch(frameRate.Value, ViewModel.SelectedFrameRate))
            {
                ViewModel.SelectedFrameRate = frameRate.Value;
            }
        };

        FormatComboBox.SelectionChanged += (s, e) =>
        {
            if (FormatComboBox.SelectedItem is string format)
            {
                ViewModel.SelectedRecordingFormat = format;
            }
        };

        QualityComboBox.SelectionChanged += (s, e) =>
        {
            if (QualityComboBox.SelectedItem is string quality)
            {
                ViewModel.SelectedQuality = quality;
            }
        };

        CustomBitrateNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(CustomBitrateNumberBox.Value))
            {
                ViewModel.CustomBitrateMbps = CustomBitrateNumberBox.Value;
            }
        };

        HdrToggle.Toggled += (s, e) => ViewModel.IsHdrEnabled = HdrToggle.IsOn;
        AudioRecordToggle.Checked += (s, e) => ViewModel.IsAudioEnabled = true;
        AudioRecordToggle.Unchecked += (s, e) => ViewModel.IsAudioEnabled = false;
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
        CustomAudioToggle.Toggled += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsOn;
        AudioMeterTrack.SizeChanged += (s, e) => UpdateAudioMeterLevel(ViewModel.AudioPeak);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe immediately - we only want this to run once
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        _ = RunUiEventHandlerAsync(async () =>
        {
            Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await ViewModel.InitializeAsync();
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                StartAutomationServices();
            }
        }, nameof(MainWindow_Loaded));
    }

    private void StartAutomationServices()
    {
        if (Interlocked.Exchange(ref _automationServicesStarted, 1) != 0)
        {
            return;
        }

        _automationDiagnosticsHub.Start();
        _automationPipeServer.Start();
        var automationToken = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_TOKEN");
        var automationPipeName = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = "ElgatoCaptureAutomation";
        }

        Logger.Log(
            $"Automation control ready on pipe '{automationPipeName}' (token required={!string.IsNullOrWhiteSpace(automationToken)}).");
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var nowTick = Environment.TickCount64;
        Interlocked.Exchange(ref _previewResizeSuppressUntilTick, nowTick + _previewResizeDebounceMs);

        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        var lastLogTick = Interlocked.Read(ref _previewLastResizeLogTick);
        if (nowTick - lastLogTick < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick) == lastLogTick)
        {
            Logger.Log($"Preview resize active. Suppressing frame presents for {_previewResizeDebounceMs}ms.");
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (Interlocked.Exchange(ref _windowCloseCleanupStarted, 1) != 0)
        {
            return;
        }

        if (this.Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }

        try
        {
            StopPreviewForShutdown();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            _automationDiagnosticsHub.SnapshotUpdated -= AutomationDiagnosticsHub_SnapshotUpdated;
            await _automationPipeServer.DisposeAsync();
            await _automationDiagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            await ViewModel.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }
    }

    private void AutomationDiagnosticsHub_SnapshotUpdated(object? sender, AutomationSnapshot snapshot)
    {
        var nowTick = Environment.TickCount64;
        var lastUpdateTick = Interlocked.Read(ref _lastDiagnosticsUiUpdateTick);
        if (nowTick - lastUpdateTick < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastDiagnosticsUiUpdateTick, nowTick, lastUpdateTick) != lastUpdateTick)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!DiagnosticsExpander.IsExpanded)
            {
                return;
            }

            UpdateDiagnosticsPanel(snapshot);
        });
    }

    private readonly record struct PreviewCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SlowFrameCount,
        double SlowFramePercent);

    private void TrackPreviewDisplayCadence()
    {
        var nowTick = Stopwatch.GetTimestamp();
        var previousTick = Interlocked.Exchange(ref _previewLastDisplayTick, nowTick);
        if (previousTick <= 0)
        {
            return;
        }

        var intervalMs = (nowTick - previousTick) * 1000.0 / Stopwatch.Frequency;
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        lock (_previewCadenceLock)
        {
            _previewDisplayIntervalWindowMs[_previewDisplayIntervalIndex] = intervalMs;
            _previewDisplayIntervalIndex = (_previewDisplayIntervalIndex + 1) % _previewDisplayIntervalWindowMs.Length;
            if (_previewDisplayIntervalCount < _previewDisplayIntervalWindowMs.Length)
            {
                _previewDisplayIntervalCount++;
            }
        }
    }

    private void ResetPreviewCadenceTracking()
    {
        Interlocked.Exchange(ref _previewLastDisplayTick, 0);
        lock (_previewCadenceLock)
        {
            Array.Clear(_previewDisplayIntervalWindowMs, 0, _previewDisplayIntervalWindowMs.Length);
            _previewDisplayIntervalCount = 0;
            _previewDisplayIntervalIndex = 0;
        }
    }

    private PreviewCadenceMetrics GetPreviewCadenceMetrics(double expectedIntervalMs)
    {
        double[] samples;
        lock (_previewCadenceLock)
        {
            if (_previewDisplayIntervalCount <= 0)
            {
                return new PreviewCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    MaxIntervalMs: 0,
                    JitterStdDevMs: 0,
                    SlowFrameCount: 0,
                    SlowFramePercent: 0);
            }

            samples = new double[_previewDisplayIntervalCount];
            for (var i = 0; i < _previewDisplayIntervalCount; i++)
            {
                var ringIndex = (_previewDisplayIntervalIndex - _previewDisplayIntervalCount + i + _previewDisplayIntervalWindowMs.Length)
                    % _previewDisplayIntervalWindowMs.Length;
                samples[i] = _previewDisplayIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var targetIntervalMs = expectedIntervalMs > 0 ? expectedIntervalMs : average;
        var slowThresholdMs = targetIntervalMs * 1.6;

        long slowFrameCount = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;
            if (samples[i] >= slowThresholdMs)
            {
                slowFrameCount++;
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PreviewCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SlowFrameCount: slowFrameCount,
            SlowFramePercent: slowPercent);
    }

    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var nowTick = Environment.TickCount64;
        var framesArrived = Interlocked.Read(ref _previewFramesArrived);
        var framesDisplayed = Interlocked.Read(ref _previewFramesDisplayed);
        var framesDropped = Interlocked.Read(ref _previewFramesDropped);
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        var gpuActive = _previewMediaPlayer != null;
        var frameReaderActive = _previewFrameReader != null;
        var gpuStartTick = Interlocked.Read(ref _previewGpuStartTick);
        var gpuLastProgressTick = Interlocked.Read(ref _previewGpuLastProgressTick);
        var gpuLastPositionMs = Interlocked.Read(ref _previewGpuLastPositionMs);

        if (gpuActive && _previewMediaPlayer?.PlaybackSession != null)
        {
            try
            {
                var positionMs = (long)_previewMediaPlayer.PlaybackSession.Position.TotalMilliseconds;
                if (positionMs > gpuLastPositionMs)
                {
                    Interlocked.Exchange(ref _previewGpuLastPositionMs, positionMs);
                    Interlocked.Exchange(ref _previewGpuLastProgressTick, nowTick);
                    gpuLastPositionMs = positionMs;
                    gpuLastProgressTick = nowTick;
                }
            }
            catch
            {
                // Best-effort diagnostics only.
            }
        }

        var rendererMode = gpuActive
            ? "GpuMediaPlayer"
            : frameReaderActive
                ? "FrameReader"
                : "None";

        var frameReaderBlank = ViewModel.IsPreviewing &&
                               frameReaderActive &&
                               framesArrived > 30 &&
                               framesDisplayed == 0;
        var gpuBlank = ViewModel.IsPreviewing &&
                       gpuActive &&
                       gpuStartTick > 0 &&
                       nowTick - gpuStartTick > 5000 &&
                       gpuLastPositionMs <= 0;
        var blankSuspected = frameReaderBlank || gpuBlank;

        var frameReaderStall = ViewModel.IsPreviewing &&
                               frameReaderActive &&
                               lastPresentedTick > 0 &&
                               nowTick - lastPresentedTick > 3000;
        var gpuStall = ViewModel.IsPreviewing &&
                       gpuActive &&
                       gpuLastProgressTick > 0 &&
                       nowTick - gpuLastProgressTick > 4000;
        var stallSuspected = frameReaderStall || gpuStall;
        var cadence = GetPreviewCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            FrameReaderActive = frameReaderActive,
            PlaceholderVisible = !ViewModel.IsPreviewing,
            FramesArrived = framesArrived,
            FramesDisplayed = framesDisplayed,
            FramesDropped = framesDropped,
            DisplayCadenceSampleCount = cadence.SampleCount,
            DisplayCadenceObservedFps = cadence.ObservedFps,
            DisplayCadenceExpectedIntervalMs = cadence.ExpectedIntervalMs,
            DisplayCadenceAverageIntervalMs = cadence.AverageIntervalMs,
            DisplayCadenceP95IntervalMs = cadence.P95IntervalMs,
            DisplayCadenceMaxIntervalMs = cadence.MaxIntervalMs,
            DisplayCadenceJitterStdDevMs = cadence.JitterStdDevMs,
            DisplayCadenceSlowFrameCount = cadence.SlowFrameCount,
            DisplayCadenceSlowFramePercent = cadence.SlowFramePercent,
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode
        };
    }

    private Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<PreviewRuntimeSnapshot>(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return Task.FromResult(GetPreviewRuntimeSnapshot());
        }

        var completion = new TaskCompletionSource<PreviewRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
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

                completion.TrySetResult(GetPreviewRuntimeSnapshot());
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
            completion.TrySetException(new InvalidOperationException("Failed to enqueue preview snapshot operation."));
        }

        return completion.Task;
    }

    private void UpdateDiagnosticsPanel(AutomationSnapshot snapshot)
    {
        static string OnOff(bool value) => value ? "On" : "Off";
        static string YesNo(bool value) => value ? "Yes" : "No";
        static string DescribeMismatch(string mismatch)
        {
            if (string.IsNullOrWhiteSpace(mismatch))
            {
                return "Unknown verification issue.";
            }

            return mismatch switch
            {
                var m when m.StartsWith("codec-mismatch", StringComparison.OrdinalIgnoreCase)
                    => "Codec did not match the selected recording format.",
                var m when m.StartsWith("container-mismatch", StringComparison.OrdinalIgnoreCase)
                    => "Container did not match expected output format.",
                var m when m.StartsWith("resolution-mismatch", StringComparison.OrdinalIgnoreCase)
                    => "Output resolution did not match requested resolution.",
                var m when m.StartsWith("fps-mismatch", StringComparison.OrdinalIgnoreCase)
                    => "Output frame rate did not match requested frame rate.",
                var m when m.StartsWith("pixfmt-not-10bit", StringComparison.OrdinalIgnoreCase)
                    => "HDR expected 10-bit output, but output pixel format was not 10-bit.",
                var m when m.StartsWith("colorimetry-mismatch", StringComparison.OrdinalIgnoreCase)
                    => "HDR color metadata did not match expected BT.2020/PQ values.",
                var m when m.StartsWith("hdr-metadata-missing", StringComparison.OrdinalIgnoreCase)
                    => "HDR mastering metadata was expected but missing.",
                var m when m.StartsWith("cadence-drop-high", StringComparison.OrdinalIgnoreCase)
                    => "Output cadence suggests dropped frames were too high.",
                var m when m.StartsWith("cadence-gaps-high", StringComparison.OrdinalIgnoreCase)
                    => "Output cadence has too many severe frame-time gaps.",
                var m when m.StartsWith("cadence-p95-high", StringComparison.OrdinalIgnoreCase)
                    => "Output frame pacing p95 latency is too high.",
                var m when m.StartsWith("ffprobe-failed", StringComparison.OrdinalIgnoreCase)
                    => "ffprobe analysis failed or timed out.",
                var m when m.StartsWith("output-not-found", StringComparison.OrdinalIgnoreCase)
                    => "Output file was not found for verification.",
                var m when m.StartsWith("output-empty", StringComparison.OrdinalIgnoreCase)
                    => "Output file is empty.",
                _ => mismatch
            };
        }

        DiagSessionTextBlock.Text =
            $"State: {snapshot.SessionState}{Environment.NewLine}" +
            $"Preview: {OnOff(snapshot.IsPreviewing)} | Recording: {OnOff(snapshot.IsRecording)}{Environment.NewLine}" +
            $"Performance: {snapshot.PerformanceScore:0.##} ({(snapshot.PerformancePerfectionMet ? "Perfect" : "Tune")})";

        DiagDeviceTextBlock.Text =
            $"Video Device: {snapshot.SelectedDeviceName ?? "(none)"}{Environment.NewLine}" +
            $"Audio Device: {snapshot.SelectedAudioInputDeviceName ?? "(default)"}";

        var requestedFrameRateText = snapshot.RequestedFrameRateArg
            ?? snapshot.RequestedFrameRate?.ToString("0.###")
            ?? "?";
        var actualFrameRateText = snapshot.NegotiatedFrameRateArg
            ?? snapshot.ActualFrameRateArg
            ?? snapshot.NegotiatedFrameRate?.ToString("0.###")
            ?? snapshot.ActualFrameRate?.ToString("0.###")
            ?? "?";
        var requestedFormatText = snapshot.RequestedReaderSubtype
            ?? snapshot.RequestedPixelFormat
            ?? "(unknown)";
        var negotiatedFormatText = snapshot.ReaderSourceSubtype
            ?? snapshot.NegotiatedPixelFormat
            ?? "(unknown)";
        var observedFormatText = snapshot.LatestObservedFramePixelFormat
            ?? snapshot.FirstObservedFramePixelFormat
            ?? "(unknown)";
        var observedCountsText =
            $"P010={snapshot.ObservedP010FrameCount}, NV12={snapshot.ObservedNv12FrameCount}, Other={snapshot.ObservedOtherFrameCount}";
        var sourceFrameRateText = snapshot.DetectedSourceFrameRateArg
            ?? snapshot.DetectedSourceFrameRate?.ToString("0.###")
            ?? "?";
        var sourceResolutionText = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth}x{snapshot.SourceHeight}"
            : "?x?";
        var sourceHdrText = snapshot.SourceIsHdr.HasValue
            ? (snapshot.SourceIsHdr.Value ? "HDR" : "SDR")
            : "HDR?";

        static string NormalizeFormatToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var value = text.Trim();
            if (value.Contains("P010", StringComparison.OrdinalIgnoreCase))
            {
                return "P010";
            }
            if (value.Contains("NV12", StringComparison.OrdinalIgnoreCase))
            {
                return "NV12";
            }
            if (value.Contains("YUY2", StringComparison.OrdinalIgnoreCase))
            {
                return "YUY2";
            }

            return value.ToUpperInvariant();
        }

        var negotiatedToken = NormalizeFormatToken(negotiatedFormatText);
        var observedToken = NormalizeFormatToken(observedFormatText);
        var observedFrameCount = snapshot.ObservedP010FrameCount +
                                 snapshot.ObservedNv12FrameCount +
                                 snapshot.ObservedOtherFrameCount;
        var hasObservedToken = !string.IsNullOrWhiteSpace(observedToken) &&
                               !string.Equals(observedToken, "(UNKNOWN)", StringComparison.OrdinalIgnoreCase);
        var hasObservedEvidence = observedFrameCount > 0 || hasObservedToken;
        var formatMismatch = hasObservedEvidence &&
                             !string.IsNullOrWhiteSpace(negotiatedToken) &&
                             !string.IsNullOrWhiteSpace(observedToken) &&
                             !string.Equals(negotiatedToken, observedToken, StringComparison.OrdinalIgnoreCase);
        var hdrExpected = snapshot.IsHdrEnabled || snapshot.HdrOutputActive || snapshot.RequestedHdrEnabled == true;
        var hdrObserved10Bit = string.Equals(observedToken, "P010", StringComparison.OrdinalIgnoreCase) ||
                               snapshot.ObservedP010FrameCount > 0;
        var hdrMismatch = hdrExpected && hasObservedEvidence && !hdrObserved10Bit;
        var mismatchStatusText = (formatMismatch, hdrMismatch) switch
        {
            (true, true) => "Format mismatch and HDR mismatch",
            (true, false) => "Format mismatch",
            (false, true) => "HDR mismatch (expected 10-bit)",
            _ => "Healthy"
        };
        string captureStatus;
        if (!snapshot.IsPreviewing && !snapshot.IsRecording)
        {
            captureStatus = "Idle";
        }
        else if (!hasObservedEvidence)
        {
            captureStatus = snapshot.IsPreviewing
                ? "Waiting for preview frames"
                : snapshot.IsRecording
                    ? "Waiting for capture frames"
                    : "Idle";
        }
        else
        {
            captureStatus = mismatchStatusText;
        }

        DiagCaptureTextBlock.Text =
            $"Requested: {snapshot.RequestedWidth?.ToString() ?? "?"}x{snapshot.RequestedHeight?.ToString() ?? "?"} @ {requestedFrameRateText} ({snapshot.RequestedFrameRate?.ToString("0.###") ?? "?"} fps), fmt={requestedFormatText}{Environment.NewLine}" +
            $"Negotiated: {snapshot.NegotiatedWidth?.ToString() ?? snapshot.ActualWidth?.ToString() ?? "?"}x{snapshot.NegotiatedHeight?.ToString() ?? snapshot.ActualHeight?.ToString() ?? "?"} @ {actualFrameRateText} ({snapshot.NegotiatedFrameRate?.ToString("0.###") ?? snapshot.ActualFrameRate?.ToString("0.###") ?? "?"} fps), fmt={negotiatedFormatText}{Environment.NewLine}" +
            $"Observed: fmt={observedFormatText} | {observedCountsText}{Environment.NewLine}" +
            $"Source: {sourceResolutionText} @ {sourceFrameRateText} | {sourceHdrText} | {snapshot.SourceTelemetryAvailability}/{snapshot.SourceTelemetryConfidence} ({snapshot.SourceTelemetryOriginDetail}){Environment.NewLine}" +
            $"Telemetry: backend={snapshot.SourceTelemetryBackend}, circuit={snapshot.SourceTelemetryCircuitState}, suppressed={YesNo(snapshot.SourceTelemetrySuppressed)}{(string.IsNullOrWhiteSpace(snapshot.SourceTelemetrySuppressedReason) ? string.Empty : $" ({snapshot.SourceTelemetrySuppressedReason})")}{Environment.NewLine}" +
            $"Status: {captureStatus}";

        DiagAudioTextBlock.Text =
            $"Capture Audio: {OnOff(snapshot.IsAudioEnabled)} | Preview Audio: {OnOff(snapshot.IsAudioPreviewEnabled)}{Environment.NewLine}" +
            $"Signal: {YesNo(snapshot.AudioSignalPresent)} | Muted Suspected: {YesNo(snapshot.AudioMutedSuspected)} | Peak: {snapshot.AudioPeak:0.000}{Environment.NewLine}" +
            $"Drops: Realtime={snapshot.AudioQueueDropsRealtime} | FileWriter={snapshot.AudioQueueDropsFileWriter}";

        DiagPreviewTextBlock.Text =
            $"Renderer: {snapshot.PreviewRendererMode}{Environment.NewLine}" +
            $"Frames: Arrived={snapshot.PreviewFramesArrived}, Displayed={snapshot.PreviewFramesDisplayed}, Dropped={snapshot.PreviewFramesDropped}{Environment.NewLine}" +
            $"Health: Blank={YesNo(snapshot.PreviewBlankSuspected)} | Stall={YesNo(snapshot.PreviewStalled)} | " +
            $"Cadence avg/p95={snapshot.PreviewCadenceAverageIntervalMs:0.##}/{snapshot.PreviewCadenceP95IntervalMs:0.##} ms | Slow={snapshot.PreviewCadenceSlowFramePercent:0.##}%";

        DiagRecordingTextBlock.Text =
            $"Backend: {snapshot.RecordingBackend} | Audio Path: {snapshot.AudioPathMode}{Environment.NewLine}" +
            $"Growing: {YesNo(snapshot.RecordingFileGrowing)} | Bytes: {snapshot.RecordingTotalBytes}{Environment.NewLine}" +
            $"Mux: {snapshot.MuxResult} | Capture Drop: {snapshot.CaptureCadenceEstimatedDropPercent:0.##}% | p95={snapshot.CaptureCadenceP95IntervalMs:0.##} ms";

        DiagOutputTextBlock.Text =
            $"Finalize: {snapshot.LastFinalizeStatus}{Environment.NewLine}" +
            $"Output: {snapshot.LastOutputPath ?? "(none)"}{Environment.NewLine}" +
            $"Perf Summary: {snapshot.PerformanceSummary}";

        if (snapshot.IsRecording)
        {
            if (snapshot.LastVerification is { } lastVerification)
            {
                DiagVerificationTextBlock.Text =
                    $"Verification: waiting for recording to stop{Environment.NewLine}" +
                    $"Last result: {(lastVerification.Succeeded ? "PASS" : "FAIL")} ({lastVerification.VerificationMode})";
            }
            else
            {
                DiagVerificationTextBlock.Text = "Verification: waiting for recording to stop";
            }
        }
        else if (snapshot.VerificationInProgress)
        {
            DiagVerificationTextBlock.Text = "Verification: in progress (analyzing recording...)";
        }
        else if (snapshot.LastVerification == null)
        {
            var waitingForAutoVerify = !string.IsNullOrWhiteSpace(snapshot.LastOutputPath) &&
                                       !string.Equals(snapshot.LastFinalizeStatus, "None", StringComparison.OrdinalIgnoreCase);
            DiagVerificationTextBlock.Text = waitingForAutoVerify
                ? "Verification: waiting to analyze recording..."
                : "Verification: not run yet";
        }
        else
        {
            var verification = snapshot.LastVerification;
            var cadenceSuffix = verification.CadenceSampleCount.HasValue
                ? $"{Environment.NewLine}Cadence: drop={verification.CadenceEstimatedDropPercent.GetValueOrDefault():0.##}% | " +
                  $"p95={verification.CadenceP95IntervalMs.GetValueOrDefault():0.##} ms"
                : string.Empty;
            var mediaSummary =
                $"{verification.DetectedVideoCodec ?? "?"}, {verification.DetectedPixelFormat ?? "?"}, " +
                $"{verification.DetectedWidth?.ToString() ?? "?"}x{verification.DetectedHeight?.ToString() ?? "?"} @ {verification.DetectedFrameRate?.ToString("0.###") ?? "?"} fps";
            var mismatchReasons = verification.Mismatches
                .Take(3)
                .Select(DescribeMismatch)
                .ToList();
            var mismatchSuffix = verification.Mismatches.Count > mismatchReasons.Count
                ? $" (+{verification.Mismatches.Count - mismatchReasons.Count} more)"
                : string.Empty;
            var reasonText = verification.Succeeded
                ? "All strict checks passed."
                : mismatchReasons.Count > 0
                    ? $"{string.Join(" | ", mismatchReasons)}{mismatchSuffix}"
                    : verification.Message;
            var primaryMismatchText = verification.Succeeded || string.IsNullOrWhiteSpace(verification.PrimaryMismatchCode)
                ? string.Empty
                : $"{Environment.NewLine}Primary: {verification.PrimaryMismatchCode}" +
                  $"{(string.IsNullOrWhiteSpace(verification.PrimaryMismatchExpected) ? string.Empty : $" | expected={verification.PrimaryMismatchExpected}")}" +
                  $"{(string.IsNullOrWhiteSpace(verification.PrimaryMismatchActual) ? string.Empty : $" | actual={verification.PrimaryMismatchActual}")}";
            DiagVerificationTextBlock.Text =
                $"Verification: {(verification.Succeeded ? "PASS" : "FAIL")} ({verification.VerificationMode}){Environment.NewLine}" +
                $"Reason: {reasonText}{primaryMismatchText}{Environment.NewLine}" +
                $"Media: {mediaSummary}{cadenceSuffix}";
        }

        var events = _automationDiagnosticsHub.GetRecentEvents(6);
        DiagEventsListView.ItemsSource = events
            .Select(evt => $"{evt.TimestampUtc:HH:mm:ss} [{evt.Severity}] {evt.Category}: {evt.Message}")
            .ToList();
    }

    private void StopPreviewForShutdown()
    {
        StopGpuPreview();

        if (_previewFrameReader != null)
        {
            try
            {
                _previewFrameReader.FrameArrived -= PreviewFrameReader_FrameArrived;
                var stopTask = _previewFrameReader.StopAsync().AsTask();
                var completed = Task.WhenAny(stopTask, Task.Delay(_previewShutdownTimeoutMs)).GetAwaiter().GetResult();
                if (completed != stopTask)
                {
                    Logger.Log($"Frame reader stop during shutdown timed out after {_previewShutdownTimeoutMs} ms.");
                }
                else
                {
                    stopTask.GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Frame reader stop during shutdown failed: {ex.Message}");
            }
            finally
            {
                _previewFrameReader.Dispose();
                _previewFrameReader = null;
            }
        }

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
        _previewGpuStartTick = 0;
        _previewGpuLastProgressTick = 0;
        _previewGpuLastPositionMs = 0;
        ResetPreviewCadenceTracking();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }

    private async Task HandleViewModelPropertyChangedAsync(System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                if (ViewModel.IsPreviewing)
                {
                    FadeOutElement(NoDevicePlaceholder);
                    PreviewLoadingOverlay.Visibility = Visibility.Visible;
                    await StartPreviewInternalAsync();
                    PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
                    PreviewButton.Content = "Stop Preview";
                }
                else
                {
                    PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
                    await StopPreviewInternalAsync();
                    FadeInElement(NoDevicePlaceholder);
                    PreviewButton.Content = "Start Preview";
                }
                break;

            case nameof(MainViewModel.IsRecording):
                RecordingIndicator.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                // Toggle record button content between normal and recording states
                RecordButtonNormalContent.Visibility = ViewModel.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                RecordButtonRecordingContent.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                AudioRecordToggle.IsEnabled = !ViewModel.IsRecording;
                CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                if (ViewModel.IsRecording)
                    RecPulseStoryboard.Begin();
                else
                {
                    RecPulseStoryboard.Stop();
                    Title = "Elgato Capture";
                }
                break;

            case nameof(MainViewModel.StatusText):
                StatusTextBlock.Text = ViewModel.StatusText;
                var statusText = ViewModel.StatusText;
                if (statusText.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    statusText.Contains("failed", StringComparison.OrdinalIgnoreCase))
                    ShowStatusNotification(statusText, InfoBarSeverity.Error);
                else if (statusText.Contains("saved", StringComparison.OrdinalIgnoreCase) ||
                         statusText.Contains("Found", StringComparison.OrdinalIgnoreCase))
                    ShowStatusNotification(statusText, InfoBarSeverity.Success);
                break;

            case nameof(MainViewModel.RecordingTime):
                RecordingTimeTextBlock.Text = ViewModel.RecordingTime;
                if (ViewModel.IsRecording)
                    Title = $"Elgato Capture \u2014 REC {ViewModel.RecordingTime}";
                break;

            case nameof(MainViewModel.DiskSpaceInfo):
                DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
                break;
            case nameof(MainViewModel.RecordingSizeInfo):
                RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
                break;
            case nameof(MainViewModel.RecordingBitrateInfo):
                RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
                break;

            case nameof(MainViewModel.OutputPath):
                OutputPathTextBox.Text = ViewModel.OutputPath;
                break;

            case nameof(MainViewModel.AudioPeak):
                UpdateAudioMeterLevel(ViewModel.AudioPeak);
                break;

            case nameof(MainViewModel.AudioClipping):
                AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.SelectedDevice):
                Logger.Log($"=== SelectedDevice PropertyChanged ===");
                Logger.Log($"  ViewModel.SelectedDevice: {ViewModel.SelectedDevice?.Name ?? "NULL"}");
                Logger.Log($"  ViewModel.Devices count: {ViewModel.Devices.Count}");
                Logger.Log($"  DeviceComboBox.Items count: {DeviceComboBox.Items.Count}");
                Logger.Log($"  DeviceComboBox.SelectedItem: {((ElgatoCapture.Models.CaptureDevice?)DeviceComboBox.SelectedItem)?.Name ?? "NULL"}");

                if (DeviceComboBox.SelectedItem != ViewModel.SelectedDevice)
                {
                    Logger.Log($"  Setting DeviceComboBox.SelectedItem to: {ViewModel.SelectedDevice?.Name ?? "NULL"}");

                    // Dispatch to UI thread to ensure ComboBox has processed collection changes
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        if (DeviceComboBox.SelectedItem != ViewModel.SelectedDevice)
                        {
                            DeviceComboBox.SelectedItem = ViewModel.SelectedDevice;
                            Logger.Log($"  After dispatch - DeviceComboBox.SelectedItem: {((ElgatoCapture.Models.CaptureDevice?)DeviceComboBox.SelectedItem)?.Name ?? "NULL"}");
                        }
                    });
                }
                else
                {
                    Logger.Log("  Already equal, skipping update");
                }
                break;

            case nameof(MainViewModel.SelectedResolution):
                if (ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
                    !string.Equals(selectedResolutionOption.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
                {
                    var matchingResolution = ViewModel.AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase));
                    ResolutionComboBox.SelectedItem = matchingResolution;
                }
                break;

            case nameof(MainViewModel.SelectedFrameRate):
                // Sync is handled by CollectionChanged subscription in SetupBindings
                // This handles cases where SelectedFrameRate changes without collection changing
                if (ViewModel.SelectedFrameRate > 0 && FrameRateComboBox.Items.Count > 0)
                {
                    var matchingRate = ViewModel.AvailableFrameRates
                        .FirstOrDefault(f => IsFrameRateMatch(f.Value, ViewModel.SelectedFrameRate));
                    if (matchingRate != null &&
                        (FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
                         !IsFrameRateMatch(currentFps.Value, matchingRate.Value)))
                    {
                        FrameRateComboBox.SelectedItem = matchingRate;
                    }
                }
                break;

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                break;

            case nameof(MainViewModel.AvailableFrameRates):
                FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
                break;

            case nameof(MainViewModel.IsHdrAvailable):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable;
                break;

            case nameof(MainViewModel.HdrResolutionSupportHint):
                HdrFpsHintTextBlock.Text = ViewModel.HdrResolutionSupportHint;
                HdrFpsHintTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.HdrResolutionSupportHint)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                break;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
                SourceTelemetryTextBlock.Text = ViewModel.SourceTelemetrySummaryText;
                SourceTelemetryTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                break;

            case nameof(MainViewModel.SourceTargetSummaryText):
                TargetTelemetryTextBlock.Text = ViewModel.SourceTargetSummaryText;
                TargetTelemetryTextBlock.Visibility = string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                break;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.IsCustomAudioInputEnabled):
                if (CustomAudioToggle.IsOn != ViewModel.IsCustomAudioInputEnabled)
                {
                    CustomAudioToggle.IsOn = ViewModel.IsCustomAudioInputEnabled;
                }
                var isVisible = ViewModel.IsCustomAudioInputEnabled ? Visibility.Visible : Visibility.Collapsed;
                AudioInputLabel.Visibility = isVisible;
                AudioInputComboBox.Visibility = isVisible;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                if (AudioInputComboBox.SelectedItem != ViewModel.SelectedAudioInputDevice)
                {
                    AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
                }
                break;

            case nameof(MainViewModel.SelectedRecordingFormat):
                if (FormatComboBox.SelectedItem as string != ViewModel.SelectedRecordingFormat)
                {
                    FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
                }
                break;

            case nameof(MainViewModel.IsAudioEnabled):
                if (AudioRecordToggle.IsChecked != ViewModel.IsAudioEnabled)
                {
                    AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
                }
                AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
                if (!ViewModel.IsAudioEnabled && AudioPreviewToggle.IsChecked == true)
                {
                    AudioPreviewToggle.IsChecked = false;
                }
                break;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                if (AudioPreviewToggle.IsChecked != ViewModel.IsAudioPreviewEnabled)
                {
                    AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
                }
                break;
        }
    }

    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
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

                action();
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
            completion.TrySetException(new InvalidOperationException("Failed to enqueue window action on the UI thread."));
        }

        return completion.Task;
    }

    private Microsoft.UI.Windowing.AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        }, cancellationToken);
    }

    public Task MaximizeAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }, cancellationToken);
    }

    public Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Restore();
            }
        }, cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return Task.CompletedTask;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            RequestWindowClose();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
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

                RequestWindowClose();
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
            if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
            {
                completion.TrySetResult(null);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException("Failed to enqueue window close action on the UI thread."));
            }
        }

        return completion.Task;
    }

    private void RequestWindowClose()
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _windowCloseRequested, 1) != 0)
        {
            return;
        }

        try
        {
            Close();
        }
        catch (Exception ex) when (IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            Application.Current.Exit();
        }
        catch
        {
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            throw;
        }
    }

    private static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ViewModel.StatusText = $"{operationName} failed: {ex.Message}";
        }
    }

    private async Task StartPreviewInternalAsync(bool forceFrameReader = false)
    {
        Logger.Log("=== START PREVIEW BEGIN ===");

        if (ViewModel.MediaCapture == null)
        {
            Logger.Log("ERROR: MediaCapture is NULL!");
            ViewModel.StatusText = "Preview failed: MediaCapture not initialized";
            return;
        }

        Logger.Log($"MediaCapture state: {ViewModel.MediaCapture}");

        try
        {
            StopGpuPreview();
            _previewSource = null;
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            _previewFramesArrived = 0;
            _previewFramesDisplayed = 0;
            _previewFramesDropped = 0;
            _previewLastLogTick = 0;
            _previewLastResizeLogTick = 0;
            _previewLastPresentedTick = 0;
            _previewResizeSuppressUntilTick = 0;
            _previewGpuStartTick = 0;
            _previewGpuLastProgressTick = 0;
            _previewGpuLastPositionMs = 0;
            _previewUiInFlight = 0;
            ResetPreviewCadenceTracking();
            Logger.Log($"Preview presentation cap max: {_previewPresentationFpsCap} fps (override={_previewCapOverrideProvided})");
            Logger.Log($"Preview resize debounce: {_previewResizeDebounceMs} ms");
            Logger.Log($"Preview mode: {(forceFrameReader ? "Frame-reader forced" : (_preferGpuPreview ? "GPU-first" : "Frame-reader only"))}");

            // Find the video preview stream
            Logger.Log("Finding frame source groups...");
            var frameSourceGroups = await Windows.Media.Capture.Frames.MediaFrameSourceGroup.FindAllAsync();
            Logger.Log($"Found {frameSourceGroups.Count} frame source groups");

            Logger.Log($"MediaCapture.FrameSources count: {ViewModel.MediaCapture.FrameSources.Count}");

            foreach (var source in ViewModel.MediaCapture.FrameSources)
            {
                Logger.Log($"  Source ID: {source.Key}, Type: {source.Value.Info.MediaStreamType}, Kind: {source.Value.Info.SourceKind}");
            }

            var colorSourceInfo = ViewModel.MediaCapture.FrameSources.Values
                .FirstOrDefault(source => source.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoPreview)
                ?? ViewModel.MediaCapture.FrameSources.Values
                    .FirstOrDefault(source => source.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoRecord);

            if (colorSourceInfo != null)
            {
                Logger.Log($"Found color source: {colorSourceInfo.Info.Id}");

                // Log available formats
                Logger.Log($"Available formats count: {colorSourceInfo.SupportedFormats.Count}");
                foreach (var format in colorSourceInfo.SupportedFormats)
                {
                    Logger.Log($"  Format: {format.Subtype} {format.VideoFormat.Width}x{format.VideoFormat.Height}");
                }

                // Set the format to match the user's selected resolution/framerate
                Windows.Media.Capture.Frames.MediaFrameFormat? desiredFormat = null;

                if (ViewModel.SelectedFormat != null)
                {
                    // Find format matching user selection, including pixel format preference.
                    desiredFormat = colorSourceInfo.SupportedFormats
                        .Where(f =>
                            f.VideoFormat.Width == ViewModel.SelectedFormat.Width &&
                            f.VideoFormat.Height == ViewModel.SelectedFormat.Height &&
                            Math.Abs(GetFrameRate(f) - ViewModel.SelectedFormat.FrameRate) < 1)
                        .OrderBy(f => string.Equals(f.Subtype, ViewModel.SelectedFormat.PixelFormat, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                        .ThenBy(f => IsHdrSubtype(f.Subtype) == ViewModel.SelectedFormat.IsHdr ? 0 : 1)
                        .ThenBy(f => MediaFormat.GetPixelFormatPriority(f.Subtype))
                        .FirstOrDefault();

                    if (desiredFormat == null)
                    {
                        // Fallback: match resolution only
                        desiredFormat = colorSourceInfo.SupportedFormats
                            .Where(f =>
                                f.VideoFormat.Width == ViewModel.SelectedFormat.Width &&
                                f.VideoFormat.Height == ViewModel.SelectedFormat.Height)
                            .OrderBy(f => MediaFormat.GetPixelFormatPriority(f.Subtype))
                            .ThenByDescending(GetFrameRate)
                            .FirstOrDefault();
                    }
                }

                // Final fallback: use largest format
                desiredFormat ??= colorSourceInfo.SupportedFormats
                    .OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height)
                    .ThenByDescending(GetFrameRate)
                    .ThenBy(f => MediaFormat.GetPixelFormatPriority(f.Subtype))
                    .FirstOrDefault();

                if (desiredFormat != null)
                {
                    var fps = desiredFormat.FrameRate.Numerator / (double)desiredFormat.FrameRate.Denominator;
                    Logger.Log($"Setting preview format to: {desiredFormat.Subtype} {desiredFormat.VideoFormat.Width}x{desiredFormat.VideoFormat.Height}@{fps:F0}fps");
                    await colorSourceInfo.SetFormatAsync(desiredFormat);
                }

                _previewActivePresentationFpsCap = ResolvePreviewPresentationCap(desiredFormat);
                _previewMinPresentationIntervalMs = Math.Max(1, 1000 / _previewActivePresentationFpsCap);
                Logger.Log($"Preview presentation cap active: {_previewActivePresentationFpsCap} fps");

                if (!forceFrameReader && TryStartGpuPreview(colorSourceInfo))
                {
                    Logger.Log("Preview started successfully");
                    ViewModel.StatusText = "Preview active (GPU presenter)";
                }
                else
                {
                    _previewSource = new SoftwareBitmapSource();
                    PreviewImage.Source = _previewSource;
                    PreviewImage.Visibility = Visibility.Visible;
                    Logger.Log("Preview image source created (frame-reader fallback)");
                    Logger.Log("Creating frame reader...");

                    // Create frame reader for fallback preview
                    _previewFrameReader = await ViewModel.MediaCapture.CreateFrameReaderAsync(colorSourceInfo);
                    Logger.Log("Frame reader created successfully");

                    _previewFrameReader.FrameArrived += PreviewFrameReader_FrameArrived;
                    Logger.Log("FrameArrived event handler attached");

                    var startResult = await _previewFrameReader.StartAsync();
                    Logger.Log($"Frame reader start result: {startResult}");

                    if (startResult == Windows.Media.Capture.Frames.MediaFrameReaderStartStatus.Success)
                    {
                        Logger.Log("Preview started successfully");
                        ViewModel.StatusText = "Preview active (frame-reader fallback)";
                    }
                    else
                    {
                        Logger.Log($"Frame reader failed to start: {startResult}");
                        ViewModel.StatusText = $"Preview failed to start: {startResult}";
                    }
                }
            }
            else
            {
                Logger.Log("No suitable frame source found, trying fallback...");
                // Fallback: try using MediaCapture.StartPreviewAsync() for older devices
                await ViewModel.MediaCapture.StartPreviewAsync();
                _previewUsedLegacyStart = true;
                Logger.Log("Fallback preview started");
                ViewModel.StatusText = "Preview active (fallback mode)";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ViewModel.StatusText = $"Preview failed: {ex.Message}";
        }

        Logger.Log("=== START PREVIEW END ===");
    }

    private async Task StopPreviewInternalAsync()
    {
        try
        {
            StopGpuPreview();

            if (_previewFrameReader != null)
            {
                _previewFrameReader.FrameArrived -= PreviewFrameReader_FrameArrived;
                await _previewFrameReader.StopAsync();
                _previewFrameReader.Dispose();
                _previewFrameReader = null;
            }

            if (_previewUsedLegacyStart && ViewModel.MediaCapture != null)
            {
                await ViewModel.MediaCapture.StopPreviewAsync();
                _previewUsedLegacyStart = false;
            }

            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            _previewSource = null;
            _previewLastPresentedTick = 0;
            _previewResizeSuppressUntilTick = 0;
            _previewGpuStartTick = 0;
            _previewGpuLastProgressTick = 0;
            _previewGpuLastPositionMs = 0;
            _previewUiInFlight = 0;
            ResetPreviewCadenceTracking();
            _previewActivePresentationFpsCap = Math.Clamp(60, 15, _previewPresentationFpsCap);
            _previewMinPresentationIntervalMs = Math.Max(1, 1000 / _previewActivePresentationFpsCap);
            _frameCounter = 0; // Reset for next preview session
        }
        catch (Exception ex)
        {
            Logger.Log($"Stop preview failed: {ex.Message}");
        }
    }

    private static int _frameCounter = 0;

    private async void PreviewFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        _frameCounter++;
        if (_frameCounter % 30 == 1) // Log every 30th frame to avoid spam
        {
            Logger.Log($"Frame #{_frameCounter} arrived");
        }

        MediaFrameReference? frame;
        try
        {
            frame = sender.TryAcquireLatestFrame();
        }
        catch (ObjectDisposedException)
        {
            // MediaCapture was disposed (e.g., during recording mode switch)
            // This is expected and we should just return silently
            return;
        }

        if (frame == null)
        {
            return;
        }

        using var frameRef = frame;

        if (frameRef.VideoMediaFrame == null)
        {
            if (_frameCounter <= 5)
            {
                Logger.Log("  - VideoMediaFrame is NULL");
            }
            return;
        }

        Interlocked.Increment(ref _previewFramesArrived);
        var nowTick = Environment.TickCount64;
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        if (lastPresentedTick > 0 && nowTick - lastPresentedTick < _previewMinPresentationIntervalMs)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            return;
        }

        var resizeSuppressUntil = Interlocked.Read(ref _previewResizeSuppressUntilTick);
        if (nowTick < resizeSuppressUntil)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            return;
        }

        var inFlight = Interlocked.CompareExchange(ref _previewUiInFlight, 1, 0);
        if (inFlight != 0)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            MaybeLogPreviewStats(nowTick, queueDelayMs: -1, setMs: -1);
            return;
        }

        SoftwareBitmap? softwareBitmap = null;

        try
        {
            // Try to get SoftwareBitmap directly first
            if (frameRef.VideoMediaFrame.SoftwareBitmap != null)
            {
                // Copy to detach lifetime from MediaFrameReference
                softwareBitmap = SoftwareBitmap.Copy(frameRef.VideoMediaFrame.SoftwareBitmap);
                if (_frameCounter <= 3)
                {
                    Logger.Log("  - Got SoftwareBitmap directly");
                }
            }
            // If not available, try to get it from Direct3DSurface (hardware-accelerated formats like YUY2, NV12)
            else if (frameRef.VideoMediaFrame.Direct3DSurface != null)
            {
                if (_frameCounter <= 3)
                {
                    Logger.Log("  - SoftwareBitmap is NULL, trying Direct3DSurface...");
                }

                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frameRef.VideoMediaFrame.Direct3DSurface);
                if (_frameCounter <= 3)
                {
                    Logger.Log("  - Created SoftwareBitmap from Direct3DSurface");
                }
            }
            else
            {
                if (_frameCounter <= 5)
                {
                    Logger.Log("  - No SoftwareBitmap or Direct3DSurface available");
                }

                Interlocked.Increment(ref _previewFramesDropped);
                return;
            }

            if (softwareBitmap == null)
            {
                Interlocked.Increment(ref _previewFramesDropped);
                return;
            }

            if (_frameCounter <= 3)
            {
                Logger.Log($"  - Bitmap: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}, {softwareBitmap.BitmapPixelFormat}, {softwareBitmap.BitmapAlphaMode}");
            }

            // Convert to BGRA8 premultiplied for display.
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                var converted = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                softwareBitmap.Dispose();
                softwareBitmap = converted;
                if (_frameCounter <= 3)
                {
                    Logger.Log("  - Converted bitmap format");
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException && ex is not OperationCanceledException)
            {
                Logger.Log($"  - Error preparing frame for preview: {ex.Message}");
            }

            Interlocked.Increment(ref _previewFramesDropped);
            softwareBitmap?.Dispose();
            Interlocked.Exchange(ref _previewUiInFlight, 0);
            return;
        }

        // Update the image on the UI thread.
        var enqueueTick = Environment.TickCount64;
        var bitmap = softwareBitmap;
        var enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            var uiStartTick = Environment.TickCount64;
            var queueDelayMs = uiStartTick - enqueueTick;
            var setStopwatch = Stopwatch.StartNew();
            try
            {
                var suppressUntil = Interlocked.Read(ref _previewResizeSuppressUntilTick);
                if (uiStartTick < suppressUntil)
                {
                    Interlocked.Increment(ref _previewFramesDropped);
                    return;
                }

                    if (_previewSource != null)
                    {
                        await _previewSource.SetBitmapAsync(bitmap);
                        setStopwatch.Stop();
                        if (_frameCounter <= 3)
                    {
                        Logger.Log("  - Bitmap displayed on UI");
                    }

                        Interlocked.Increment(ref _previewFramesDisplayed);
                        Interlocked.Exchange(ref _previewLastPresentedTick, uiStartTick);
                        TrackPreviewDisplayCadence();
                    }
                    else
                    {
                    if (_frameCounter <= 3)
                    {
                        Logger.Log("  - _previewSource is NULL");
                    }

                    Interlocked.Increment(ref _previewFramesDropped);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException && ex is not OperationCanceledException)
                {
                    Logger.Log($"  - Error displaying frame: {ex.Message}");
                }

                Interlocked.Increment(ref _previewFramesDropped);
            }
            finally
            {
                if (setStopwatch.IsRunning)
                {
                    setStopwatch.Stop();
                }

                Interlocked.Exchange(ref _previewUiInFlight, 0);
                MaybeLogPreviewStats(uiStartTick, queueDelayMs, (long)setStopwatch.ElapsedMilliseconds);
                bitmap.Dispose();
            }
        });

        if (!enqueued)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            Interlocked.Exchange(ref _previewUiInFlight, 0);
            bitmap.Dispose();
        }
    }

    private void MaybeLogPreviewStats(long nowTick, long queueDelayMs, long setMs)
    {
        var inFlightNow = Volatile.Read(ref _previewUiInFlight);
        var issue = inFlightNow > 1 || queueDelayMs >= 50 || setMs >= 50;
        if (!issue)
        {
            return;
        }

        var last = Interlocked.Read(ref _previewLastLogTick);
        if (nowTick - last < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastLogTick, nowTick, last) != last)
        {
            return;
        }

        var arrived = Interlocked.Read(ref _previewFramesArrived);
        var displayed = Interlocked.Read(ref _previewFramesDisplayed);
        var dropped = Interlocked.Read(ref _previewFramesDropped);
        var queueDelayText = queueDelayMs >= 0 ? $"{queueDelayMs}ms" : "n/a";
        var setText = setMs >= 0 ? $"{setMs}ms" : "n/a";
        Logger.Log($"Preview UI stall: inFlight={inFlightNow} queueDelay={queueDelayText} setMs={setText} arrived={arrived} displayed={displayed} dropped={dropped}");
    }

    private void UpdateAudioMeterLevel(double level)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var trackWidth = AudioMeterTrack.ActualWidth;
        if (trackWidth <= 0) return;
        AudioMeterClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * clamped, 8);
    }

    private void ShowStatusNotification(string message, InfoBarSeverity severity, int autoCloseMs = 5000)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;

        _infoBarDismissTimer?.Stop();
        _infoBarDismissTimer = _dispatcherQueue.CreateTimer();
        _infoBarDismissTimer.Interval = TimeSpan.FromMilliseconds(autoCloseMs);
        _infoBarDismissTimer.IsRepeating = false;
        _infoBarDismissTimer.Tick += (_, _) => StatusInfoBar.IsOpen = false;
        _infoBarDismissTimer.Start();
    }

    private static void FadeOutElement(UIElement element)
    {
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Completed += (_, _) =>
        {
            element.Visibility = Visibility.Collapsed;
            element.Opacity = 1.0;
        };
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private static void FadeInElement(UIElement element)
    {
        element.Opacity = 0.0;
        element.Visibility = Visibility.Visible;
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            RefreshButton.Content = new Microsoft.UI.Xaml.Controls.ProgressRing { Width = 16, Height = 16, IsActive = true };
            RefreshButton.IsEnabled = false;
            try
            {
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                RefreshButton.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uE72C", FontSize = 14 };
                RefreshButton.IsEnabled = true;
            }
        }, nameof(RefreshButton_Click));
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (ViewModel.IsPreviewing)
            {
                await ViewModel.StopPreviewAsync();
            }
            else
            {
                await ViewModel.StartPreviewAsync();
            }
        }, nameof(PreviewButton_Click));
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            var startingRecording = !ViewModel.IsRecording;

            if (startingRecording &&
                _forceFrameReaderDuringRecording &&
                _previewMediaPlayer != null)
            {
                // Optional compatibility mode for specific drivers:
                // switch GPU preview to frame-reader path before recording.
                Logger.Log("Switching preview to frame-reader mode before recording (compatibility mode).");
                await StartPreviewInternalAsync(forceFrameReader: true);
            }

            await ViewModel.ToggleRecordingAsync();

            if (ViewModel.IsRecording)
            {
                var gpuActive = _previewMediaPlayer != null;
                var frameReaderActive = _previewFrameReader != null;
                var rendererActive = gpuActive || frameReaderActive;
                var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
                Logger.Log(
                    $"PreviewStateDuringRecording: rendererActive={rendererActive}, gpuActive={gpuActive}, " +
                    $"frameReaderActive={frameReaderActive}, placeholderVisible={placeholderVisible}");

                if (!rendererActive || placeholderVisible)
                {
                    Logger.Log("WARNING: preview renderer appears inactive while recording.");
                }
            }
        }, nameof(RecordButton_Click));
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.BrowseOutputPathAsync(), nameof(BrowseButton_Click));
    }
}
