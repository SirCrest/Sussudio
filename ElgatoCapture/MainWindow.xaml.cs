using System;
using System.Collections.Generic;
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
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastLogTick;
    private long _previewLastResizeLogTick;
    private long _previewLastPresentedTick;
    private long _previewResizeSuppressUntilTick;
    private int _previewUiInFlight;
    private long _lastDiagnosticsUiUpdateTick;
    private readonly object _previewCadenceLock = new();
    private readonly double[] _previewDisplayIntervalWindowMs = new double[300];
    private int _previewDisplayIntervalCount;
    private int _previewDisplayIntervalIndex;
    private long _previewLastDisplayTick;
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private long _previewMinPresentationIntervalMs;
    private readonly int _previewResizeDebounceMs;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private int _automationServicesStarted;
    private DispatcherQueueTimer? _infoBarDismissTimer;
    private string? _lastHdrRuntimeStateNotification;
    private int _deviceSelectionSyncQueued;
    private int _audioSelectionSyncQueued;
    private int _resolutionSelectionSyncQueued;
    private int _frameRateSelectionSyncQueued;
    private int _formatSelectionSyncQueued;
    private int _qualitySelectionSyncQueued;

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private void EnsureDeviceSelection()
    {
        if (ViewModel.Devices.Count == 0)
        {
            DeviceComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedDevice != null
            ? ViewModel.Devices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.Devices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedDevice, matchingDevice))
        {
            ViewModel.SelectedDevice = matchingDevice;
        }

        if (!ReferenceEquals(DeviceComboBox.SelectedItem, matchingDevice))
        {
            DeviceComboBox.SelectedItem = matchingDevice;
        }
    }

    private void EnsureAudioInputSelection()
    {
        if (ViewModel.AudioInputDevices.Count == 0)
        {
            AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedAudioInputDevice != null
            ? ViewModel.AudioInputDevices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedAudioInputDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.AudioInputDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedAudioInputDevice, matchingDevice))
        {
            ViewModel.SelectedAudioInputDevice = matchingDevice;
        }

        if (!ReferenceEquals(AudioInputComboBox.SelectedItem, matchingDevice))
        {
            AudioInputComboBox.SelectedItem = matchingDevice;
        }
    }

    private void EnsureResolutionSelection()
    {
        if (ViewModel.AvailableResolutions.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                ResolutionComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingResolution = ViewModel.AvailableResolutions.FirstOrDefault(option =>
            string.Equals(option.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableResolutions.FirstOrDefault();
        if (matchingResolution == null)
        {
            return;
        }

        if (!string.Equals(matchingResolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedResolution = matchingResolution.Value;
        }

        if (ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
            !string.Equals(selectedResolutionOption.Value, matchingResolution.Value, StringComparison.OrdinalIgnoreCase))
        {
            ResolutionComboBox.SelectedItem = matchingResolution;
        }
    }

    private void EnsureFrameRateSelection()
    {
        if (ViewModel.AvailableFrameRates.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FrameRateComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingRate = ViewModel.AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, ViewModel.SelectedFrameRate))
            ?? ViewModel.AvailableFrameRates.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableFrameRates.FirstOrDefault();
        if (matchingRate == null)
        {
            return;
        }

        if (!IsFrameRateMatch(matchingRate.Value, ViewModel.SelectedFrameRate))
        {
            ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !IsFrameRateMatch(currentFps.Value, matchingRate.Value))
        {
            FrameRateComboBox.SelectedItem = matchingRate;
        }
    }

    private void EnsureFormatSelection()
    {
        if (ViewModel.AvailableRecordingFormats.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FormatComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingFormat = ViewModel.AvailableRecordingFormats
            .FirstOrDefault(format => string.Equals(format, ViewModel.SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableRecordingFormats.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingFormat))
        {
            return;
        }

        if (!string.Equals(matchingFormat, ViewModel.SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedRecordingFormat = matchingFormat;
        }

        if (!string.Equals(FormatComboBox.SelectedItem as string, matchingFormat, StringComparison.OrdinalIgnoreCase))
        {
            FormatComboBox.SelectedItem = matchingFormat;
        }
    }

    private void EnsureQualitySelection()
    {
        if (ViewModel.AvailableQualities.Count == 0)
        {
            QualityComboBox.SelectedItem = null;
            return;
        }

        var matchingQuality = ViewModel.AvailableQualities
            .FirstOrDefault(quality => string.Equals(quality, ViewModel.SelectedQuality, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableQualities.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingQuality))
        {
            return;
        }

        if (!string.Equals(matchingQuality, ViewModel.SelectedQuality, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedQuality = matchingQuality;
        }

        if (!string.Equals(QualityComboBox.SelectedItem as string, matchingQuality, StringComparison.OrdinalIgnoreCase))
        {
            QualityComboBox.SelectedItem = matchingQuality;
        }
    }

    private void QueueDeviceSelectionSync()
    {
        if (Interlocked.Exchange(ref _deviceSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureDeviceSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _deviceSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueAudioSelectionSync()
    {
        if (Interlocked.Exchange(ref _audioSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureAudioInputSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _audioSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueResolutionSelectionSync()
    {
        if (Interlocked.Exchange(ref _resolutionSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureResolutionSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _resolutionSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueFrameRateSelectionSync()
    {
        if (Interlocked.Exchange(ref _frameRateSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureFrameRateSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _frameRateSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueFormatSelectionSync()
    {
        if (Interlocked.Exchange(ref _formatSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureFormatSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _formatSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueQualitySelectionSync()
    {
        if (Interlocked.Exchange(ref _qualitySelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureQualitySelection();
            }
            finally
            {
                Interlocked.Exchange(ref _qualitySelectionSyncQueued, 0);
            }
        });
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

    private long ResolvePreviewExpectedIntervalMs()
    {
        var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;
        if (sourceFps <= 0)
        {
            sourceFps = 60;
        }

        return Math.Max(1L, (long)Math.Round(1000.0 / sourceFps));
    }

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

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
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();
        _previewResizeDebounceMs = GetIntFromEnv("ELGATOCAPTURE_PREVIEW_RESIZE_DEBOUNCE_MS", defaultValue: 250, minValue: 50, maxValue: 2000);

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
        ViewModel.PreviewFrameReady += ViewModel_PreviewFrameReady;

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

        ViewModel.Devices.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueDeviceSelectionSync();
        };

        ViewModel.AudioInputDevices.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueAudioSelectionSync();
        };

        ViewModel.AvailableResolutions.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueResolutionSelectionSync();
        };

        // Subscribe to collection changes to sync SelectedItem after items are added
        ViewModel.AvailableFrameRates.CollectionChanged += (s, e) =>
        {
            // After items are added, sync the selected frame rate
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                QueueFrameRateSelectionSync();
            }
        };

        ViewModel.AvailableRecordingFormats.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueFormatSelectionSync();
        };

        ViewModel.AvailableQualities.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueQualitySelectionSync();
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
        HdrToggle.IsOn = ViewModel.IsHdrEnabled;
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
        TrueHdrPreviewToggle.IsOn = ViewModel.IsTrueHdrPreviewEnabled;
        TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
        UpdateAudioMeterLevel(ViewModel.AudioPeak);
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioInputSelection();
        EnsureResolutionSelection();
        EnsureFrameRateSelection();
        EnsureFormatSelection();
        EnsureQualitySelection();

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
        TrueHdrPreviewToggle.Toggled += (s, e) => ViewModel.IsTrueHdrPreviewEnabled = TrueHdrPreviewToggle.IsOn;
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

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewFrameReady -= ViewModel_PreviewFrameReady;

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
        var gpuActive = ViewModel.IsPreviewing && ViewModel.PreviewGpuActive;
        var frameReaderActive = ViewModel.IsPreviewing && !gpuActive;
        var rendererMode = ViewModel.IsPreviewing
            ? (gpuActive ? (ViewModel.PreviewRendererMode.Length > 0 ? ViewModel.PreviewRendererMode : "MediaPlayerElement") : "DirectShowRawPipe")
            : "None";
        var blankSuspected = frameReaderActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        var stallSuspected = frameReaderActive &&
                             lastPresentedTick > 0 &&
                             nowTick - lastPresentedTick > 3000;
        var cadence = GetPreviewCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            FrameReaderActive = frameReaderActive,
            PlaceholderVisible = !ViewModel.IsPreviewing,
            FramesArrived = gpuActive ? 0 : framesArrived,
            FramesDisplayed = gpuActive ? 0 : framesDisplayed,
            FramesDropped = gpuActive ? 0 : framesDropped,
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
        var requestedFrameRateText = snapshot.RequestedFrameRateArg
            ?? snapshot.RequestedFrameRate?.ToString("0.###")
            ?? "?";
        var negotiatedFrameRateText = snapshot.NegotiatedFrameRateArg
            ?? snapshot.ActualFrameRateArg
            ?? snapshot.NegotiatedFrameRate?.ToString("0.###")
            ?? snapshot.ActualFrameRate?.ToString("0.###")
            ?? "?";
        var captureHealth = snapshot.PreviewBlankSuspected || snapshot.PreviewStalled
            ? "Attention"
            : "Healthy";
        var hdrVerdict = snapshot.HdrTruthVerdict?.FinalClassification ?? "Inconclusive";
        var mismatchSummary = snapshot.LastVerification?.Mismatches.Count > 0
            ? string.Join(", ", snapshot.LastVerification.Mismatches.Take(3))
            : "(none)";

        DiagHeaderSummaryTextBlock.Text =
            $"State: {snapshot.SessionState} | Preview: {OnOff(snapshot.IsPreviewing)} | Recording: {OnOff(snapshot.IsRecording)} | Health: {captureHealth}";

        DiagSessionTextBlock.Text =
            $"Performance score: {snapshot.PerformanceScore:0.##} ({(snapshot.PerformancePerfectionMet ? "Within target" : "Needs tuning")}){Environment.NewLine}" +
            $"Telemetry: {snapshot.SourceTelemetryAvailability}/{snapshot.SourceTelemetryConfidence} ({snapshot.SourceTelemetryOriginDetail})";

        DiagDeviceTextBlock.Text =
            $"Video: {snapshot.SelectedDeviceName ?? "(none)"}{Environment.NewLine}" +
            $"Audio input: {snapshot.SelectedAudioInputDeviceName ?? "(default)"}";

        DiagCaptureTextBlock.Text =
            $"Mode: {snapshot.NegotiatedWidth?.ToString() ?? snapshot.ActualWidth?.ToString() ?? "?"}x{snapshot.NegotiatedHeight?.ToString() ?? snapshot.ActualHeight?.ToString() ?? "?"} @ {negotiatedFrameRateText} fps{Environment.NewLine}" +
            $"Requested: {snapshot.RequestedWidth?.ToString() ?? "?"}x{snapshot.RequestedHeight?.ToString() ?? "?"} @ {requestedFrameRateText} fps{Environment.NewLine}" +
            $"HDR verdict: {hdrVerdict}";

        DiagCaptureAdvancedTextBlock.Text =
            $"Compact mode enabled. Advanced capture internals are hidden by default.{Environment.NewLine}" +
            $"Observed formats: P010={snapshot.ObservedP010FrameCount}, NV12={snapshot.ObservedNv12FrameCount}, Other={snapshot.ObservedOtherFrameCount}";

        DiagAudioTextBlock.Text =
            $"Capture audio: {OnOff(snapshot.IsAudioEnabled)} | Preview audio: {OnOff(snapshot.IsAudioPreviewEnabled)}{Environment.NewLine}" +
            $"Signal present: {YesNo(snapshot.AudioSignalPresent)} | Peak: {snapshot.AudioPeak:0.000} | Clip: {YesNo(snapshot.AudioClipping)}";

        DiagPreviewTextBlock.Text =
            $"Renderer: {snapshot.PreviewRendererMode}{Environment.NewLine}" +
            $"Frames: in={snapshot.PreviewFramesArrived}, shown={snapshot.PreviewFramesDisplayed}, dropped={snapshot.PreviewFramesDropped}{Environment.NewLine}" +
            $"Cadence: p95={snapshot.PreviewCadenceP95IntervalMs:0.##} ms, slow={snapshot.PreviewCadenceSlowFramePercent:0.##}%";

        DiagRecordingTextBlock.Text =
            $"Backend: {snapshot.RecordingBackend} | Audio path: {snapshot.AudioPathMode}{Environment.NewLine}" +
            $"File growth: {YesNo(snapshot.RecordingFileGrowing)} | Bytes: {snapshot.RecordingTotalBytes}{Environment.NewLine}" +
            $"Finalize: {snapshot.LastFinalizeStatus}";

        DiagOutputTextBlock.Text =
            $"Output: {snapshot.LastOutputPath ?? "(none)"}{Environment.NewLine}" +
            $"Perf summary: {snapshot.PerformanceSummary}";

        if (snapshot.VerificationInProgress)
        {
            DiagVerificationTextBlock.Text = "Verification: running";
            DiagVerificationAdvancedTextBlock.Text = "Waiting for ffprobe result.";
            return;
        }

        if (snapshot.LastVerification == null)
        {
            DiagVerificationTextBlock.Text = "Verification: not run";
            DiagVerificationAdvancedTextBlock.Text = "No verification result yet.";
            return;
        }

        var verification = snapshot.LastVerification;
        DiagVerificationTextBlock.Text =
            $"Verification: {(verification.Succeeded ? "PASS" : "FAIL")} ({verification.VerificationMode}){Environment.NewLine}" +
            $"{verification.Message}";
        DiagVerificationAdvancedTextBlock.Text =
            $"Mismatch codes: {mismatchSummary}{Environment.NewLine}" +
            $"Primary: {verification.PrimaryMismatchCode ?? "(none)"}";
    }

    private void StopPreviewForShutdown()
    {
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        try
        {
            PreviewPlayerElement.MediaPlayer?.Pause();
        }
        catch
        {
            // Best effort only.
        }
        PreviewPlayerElement.Source = null;
        PreviewPlayerElement.Visibility = Visibility.Collapsed;
        _previewSource = null;
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
                    await StartPreviewRendererAsync();
                    PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
                    PreviewButton.Content = "Stop Preview";
                    TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                }
                else
                {
                    PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
                    await StopPreviewRendererAsync();
                    FadeInElement(NoDevicePlaceholder);
                    PreviewButton.Content = "Start Preview";
                    TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                }
                break;

            case nameof(MainViewModel.PreviewGpuActive):
            case nameof(MainViewModel.PreviewPlaybackSource):
            case nameof(MainViewModel.PreviewRendererMode):
                if (ViewModel.IsPreviewing)
                {
                    // Preview can transition between CPU/GPU renderers once the capture backend has negotiated formats.
                    // Re-evaluate the renderer selection without toggling preview state (no placeholder blink).
                    await StartPreviewRendererAsync();
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
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
                TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                RefreshHdrHintText();
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
                EnsureDeviceSelection();
                break;

            case nameof(MainViewModel.SelectedResolution):
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.SelectedFrameRate):
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.AvailableFrameRates):
                FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsHdrAvailable):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsHdrEnabled):
                if (HdrToggle.IsOn != ViewModel.IsHdrEnabled)
                {
                    HdrToggle.IsOn = ViewModel.IsHdrEnabled;
                }
                break;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                if (TrueHdrPreviewToggle.IsOn != ViewModel.IsTrueHdrPreviewEnabled)
                {
                    TrueHdrPreviewToggle.IsOn = ViewModel.IsTrueHdrPreviewEnabled;
                }
                break;

            case nameof(MainViewModel.HdrResolutionSupportHint):
            case nameof(MainViewModel.HdrReadinessReason):
            case nameof(MainViewModel.HdrRuntimeState):
                RefreshHdrHintText();
                if (string.Equals(ViewModel.HdrRuntimeState, "Degraded", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ViewModel.HdrRuntimeState, "Violation", StringComparison.OrdinalIgnoreCase))
                {
                    var notificationKey = $"{ViewModel.HdrRuntimeState}:{ViewModel.HdrReadinessReason}";
                    if (!string.Equals(notificationKey, _lastHdrRuntimeStateNotification, StringComparison.Ordinal))
                    {
                        _lastHdrRuntimeStateNotification = notificationKey;
                        ShowStatusNotification(
                            string.IsNullOrWhiteSpace(ViewModel.HdrReadinessReason)
                                ? "HDR pipeline contract was violated."
                                : $"HDR issue: {ViewModel.HdrReadinessReason}",
                            InfoBarSeverity.Warning);
                    }
                }
                break;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                UpdateFpsTelemetryTooltip();
                break;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.CustomBitrateMbps):
                if (double.IsNaN(CustomBitrateNumberBox.Value) ||
                    Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01)
                {
                    CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
                }
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
                EnsureAudioInputSelection();
                break;

            case nameof(MainViewModel.SelectedRecordingFormat):
                EnsureFormatSelection();
                break;

            case nameof(MainViewModel.SelectedQuality):
                EnsureQualitySelection();
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

    private void RefreshHdrHintText()
    {
        var resolutionHint = ViewModel.HdrResolutionSupportHint?.Trim();
        var readinessHint = ViewModel.HdrReadinessReason?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (ViewModel.IsRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }
        ToolTipService.SetToolTip(HdrToggle,
            string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint);
    }

    private void UpdateFpsTelemetryTooltip()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText))
            parts.Add(ViewModel.SourceTelemetrySummaryText);
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText))
            parts.Add(ViewModel.SourceTargetSummaryText);
        ToolTipService.SetToolTip(FrameRateComboBox,
            parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null);
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

    private Task StartPreviewRendererAsync()
    {
        _previewFramesArrived = 0;
        _previewFramesDisplayed = 0;
        _previewFramesDropped = 0;
        _previewLastLogTick = 0;
        _previewLastResizeLogTick = 0;
        _previewLastPresentedTick = 0;
        _previewResizeSuppressUntilTick = 0;
        _previewUiInFlight = 0;
        ResetPreviewCadenceTracking();
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        if (ViewModel.PreviewGpuActive && ViewModel.PreviewPlaybackSource != null)
        {
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            _previewSource = null;

            PreviewPlayerElement.Source = ViewModel.PreviewPlaybackSource;
            PreviewPlayerElement.Visibility = Visibility.Visible;

            try
            {
                PreviewPlayerElement.MediaPlayer?.Play();
            }
            catch (Exception ex)
            {
                Logger.Log($"GPU preview play warning: {ex.Message}");
            }

            Logger.Log($"Preview renderer started (mode=MediaPlayerElement, cap=none, expectedIntervalMs={_previewMinPresentationIntervalMs}, trueHdr={ViewModel.IsTrueHdrPreviewEnabled}).");
            return Task.CompletedTask;
        }

        _previewSource = new SoftwareBitmapSource();
        PreviewImage.Source = _previewSource;
        PreviewImage.Visibility = Visibility.Visible;
        PreviewPlayerElement.Source = null;
        PreviewPlayerElement.Visibility = Visibility.Collapsed;
        Logger.Log($"Preview renderer started (mode=DirectShowRawPipe, cap=none, expectedIntervalMs={_previewMinPresentationIntervalMs}).");
        return Task.CompletedTask;
    }

    private Task StopPreviewRendererAsync()
    {
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        try
        {
            PreviewPlayerElement.MediaPlayer?.Pause();
        }
        catch
        {
            // Best effort only.
        }
        PreviewPlayerElement.Source = null;
        PreviewPlayerElement.Visibility = Visibility.Collapsed;
        _previewSource = null;
        _previewLastPresentedTick = 0;
        _previewResizeSuppressUntilTick = 0;
        _previewUiInFlight = 0;
        ResetPreviewCadenceTracking();
        _previewMinPresentationIntervalMs = Math.Max(1L, (long)Math.Round(1000.0 / 60.0));
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }

    private async void ViewModel_PreviewFrameReady(object? sender, PreviewFrame frame)
    {
        Interlocked.Increment(ref _previewFramesArrived);
        var nowTick = Environment.TickCount64;
        var resizeSuppressUntil = Interlocked.Read(ref _previewResizeSuppressUntilTick);
        if (nowTick < resizeSuppressUntil)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            return;
        }

        if (Interlocked.CompareExchange(ref _previewUiInFlight, 1, 0) != 0)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            MaybeLogPreviewStats(nowTick, queueDelayMs: -1, setMs: -1);
            return;
        }

        SoftwareBitmap? bitmap = null;
        try
        {
            bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)frame.Width, (int)frame.Height, BitmapAlphaMode.Premultiplied);
            bitmap.CopyFromBuffer(frame.Buffer.AsBuffer());
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview frame conversion failed: {ex.Message}");
            Interlocked.Increment(ref _previewFramesDropped);
            bitmap?.Dispose();
            Interlocked.Exchange(ref _previewUiInFlight, 0);
            return;
        }

        var enqueueTick = Environment.TickCount64;
        var enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            var uiStartTick = Environment.TickCount64;
            var queueDelayMs = uiStartTick - enqueueTick;
            var setStopwatch = Stopwatch.StartNew();
            try
            {
                if (_previewSource != null)
                {
                    await _previewSource.SetBitmapAsync(bitmap);
                    Interlocked.Increment(ref _previewFramesDisplayed);
                    Interlocked.Exchange(ref _previewLastPresentedTick, uiStartTick);
                    TrackPreviewDisplayCadence();
                }
                else
                {
                    Interlocked.Increment(ref _previewFramesDropped);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException && ex is not OperationCanceledException)
                {
                    Logger.Log($"Preview render failed: {ex.Message}");
                }

                Interlocked.Increment(ref _previewFramesDropped);
            }
            finally
            {
                setStopwatch.Stop();
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
            await ViewModel.ToggleRecordingAsync();

            if (ViewModel.IsRecording)
            {
                var rendererActive =
                    (ViewModel.PreviewGpuActive && ViewModel.PreviewPlaybackSource != null && PreviewPlayerElement.Visibility == Visibility.Visible) ||
                    (!ViewModel.PreviewGpuActive && _previewSource != null && PreviewImage.Visibility == Visibility.Visible);
                var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
                Logger.Log(
                    $"PreviewStateDuringRecording: rendererActive={rendererActive}, " +
                    $"placeholderVisible={placeholderVisible}");

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
