using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private MediaFrameReader? _previewFrameReader;
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();

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
        ViewModel.RequestPreviewStop += ViewModel_RequestPreviewStop;
        ViewModel.PreviewNeedsRestart += ViewModel_PreviewNeedsRestart;

        // Wire up UI controls to ViewModel
        SetupBindings();

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        ((FrameworkElement)this.Content).Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void SetupBindings()
    {
        // Bind all collections to ComboBoxes
        DeviceComboBox.ItemsSource = ViewModel.Devices;
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
                            .FirstOrDefault(f => Math.Abs(f - ViewModel.SelectedFrameRate) < 0.1);
                        if (matchingRate > 0)
                        {
                            FrameRateComboBox.SelectedItem = matchingRate;
                        }
                    }
                });
            }
        };

        // Set initial values
        OutputPathTextBox.Text = ViewModel.OutputPath;
        DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;

        // Wire up selection changes with loop prevention
        DeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DeviceComboBox.SelectedItem != null &&
                DeviceComboBox.SelectedItem != ViewModel.SelectedDevice)
            {
                ViewModel.SelectedDevice = (ElgatoCapture.Models.CaptureDevice)DeviceComboBox.SelectedItem;
            }
        };

        ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (ResolutionComboBox.SelectedItem is string resolution &&
                resolution != ViewModel.SelectedResolution)
            {
                ViewModel.SelectedResolution = resolution;
            }
        };

        FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (FrameRateComboBox.SelectedItem is double frameRate &&
                Math.Abs(frameRate - ViewModel.SelectedFrameRate) > 0.1)
            {
                ViewModel.SelectedFrameRate = frameRate;
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
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe immediately - we only want this to run once
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
        await ViewModel.RefreshDevicesAsync();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.RequestPreviewStop -= ViewModel_RequestPreviewStop;
        ViewModel.PreviewNeedsRestart -= ViewModel_PreviewNeedsRestart;
        StopPreviewInternal();
        ViewModel.Dispose();
    }

    private void ViewModel_RequestPreviewStop(object? sender, EventArgs e)
    {
        Logger.Log("=== Stopping preview (MediaCapture about to be disposed) ===");
        StopPreviewInternal();
    }

    private async void ViewModel_PreviewNeedsRestart(object? sender, EventArgs e)
    {
        Logger.Log("=== Restarting preview after MediaCapture reinitialization ===");

        // Stop the current frame reader
        StopPreviewInternal();

        // Restart preview with the new MediaCapture instance
        if (ViewModel.IsPreviewing && ViewModel.MediaCapture != null)
        {
            await StartPreviewInternalAsync();
        }
    }

    private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                if (ViewModel.IsPreviewing)
                {
                    await StartPreviewInternalAsync();
                    NoDevicePlaceholder.Visibility = Visibility.Collapsed;
                    PreviewButton.Content = "Stop Preview";
                }
                else
                {
                    StopPreviewInternal();
                    NoDevicePlaceholder.Visibility = Visibility.Visible;
                    PreviewButton.Content = "Start Preview";
                }
                break;

            case nameof(MainViewModel.IsRecording):
                RecordingIndicator.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                // Toggle record button content between normal and recording states
                RecordButtonNormalContent.Visibility = ViewModel.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                RecordButtonRecordingContent.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.StatusText):
                StatusTextBlock.Text = ViewModel.StatusText;
                break;

            case nameof(MainViewModel.RecordingTime):
                RecordingTimeTextBlock.Text = ViewModel.RecordingTime;
                break;

            case nameof(MainViewModel.DiskSpaceInfo):
                DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
                break;

            case nameof(MainViewModel.OutputPath):
                OutputPathTextBox.Text = ViewModel.OutputPath;
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
                if (ResolutionComboBox.SelectedItem as string != ViewModel.SelectedResolution)
                {
                    ResolutionComboBox.SelectedItem = ViewModel.SelectedResolution;
                }
                break;

            case nameof(MainViewModel.SelectedFrameRate):
                // Sync is handled by CollectionChanged subscription in SetupBindings
                // This handles cases where SelectedFrameRate changes without collection changing
                if (ViewModel.SelectedFrameRate > 0 && FrameRateComboBox.Items.Count > 0)
                {
                    var matchingRate = ViewModel.AvailableFrameRates
                        .FirstOrDefault(f => Math.Abs(f - ViewModel.SelectedFrameRate) < 0.1);
                    if (matchingRate > 0 && FrameRateComboBox.SelectedItem is not double currentFps ||
                        FrameRateComboBox.SelectedItem is double currentFps2 && Math.Abs(currentFps2 - matchingRate) > 0.1)
                    {
                        FrameRateComboBox.SelectedItem = matchingRate;
                    }
                }
                break;

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                break;

            case nameof(MainViewModel.IsHdrAvailable):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable;
                break;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
                break;
        }
    }

    private async System.Threading.Tasks.Task StartPreviewInternalAsync()
    {
        Logger.Log("=== START PREVIEW BEGIN ===");
        System.Diagnostics.Debug.WriteLine("=== StartPreviewInternalAsync BEGIN ===");

        if (ViewModel.MediaCapture == null)
        {
            Logger.Log("ERROR: MediaCapture is NULL!");
            System.Diagnostics.Debug.WriteLine("ERROR: MediaCapture is NULL!");
            ViewModel.StatusText = "Preview failed: MediaCapture not initialized";
            return;
        }

        Logger.Log($"MediaCapture state: {ViewModel.MediaCapture}");
        System.Diagnostics.Debug.WriteLine($"MediaCapture state: {ViewModel.MediaCapture}");

        try
        {
            // Create a preview source
            _previewSource = new SoftwareBitmapSource();
            PreviewImage.Source = _previewSource;
            Logger.Log("Preview image source created");
            System.Diagnostics.Debug.WriteLine("Preview image source created");

            // Find the video preview stream
            Logger.Log("Finding frame source groups...");
            System.Diagnostics.Debug.WriteLine("Finding frame source groups...");
            var frameSourceGroups = await Windows.Media.Capture.Frames.MediaFrameSourceGroup.FindAllAsync();
            Logger.Log($"Found {frameSourceGroups.Count} frame source groups");
            System.Diagnostics.Debug.WriteLine($"Found {frameSourceGroups.Count} frame source groups");

            Logger.Log($"MediaCapture.FrameSources count: {ViewModel.MediaCapture.FrameSources.Count}");
            System.Diagnostics.Debug.WriteLine($"MediaCapture.FrameSources count: {ViewModel.MediaCapture.FrameSources.Count}");

            foreach (var source in ViewModel.MediaCapture.FrameSources)
            {
                Logger.Log($"  Source ID: {source.Key}, Type: {source.Value.Info.MediaStreamType}, Kind: {source.Value.Info.SourceKind}");
                System.Diagnostics.Debug.WriteLine($"  - Source ID: {source.Key}");
                System.Diagnostics.Debug.WriteLine($"    MediaStreamType: {source.Value.Info.MediaStreamType}");
                System.Diagnostics.Debug.WriteLine($"    SourceKind: {source.Value.Info.SourceKind}");
            }

            var colorSourceInfo = ViewModel.MediaCapture.FrameSources.Values
                .FirstOrDefault(source => source.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoPreview ||
                                         source.Info.MediaStreamType == Windows.Media.Capture.MediaStreamType.VideoRecord);

            if (colorSourceInfo != null)
            {
                Logger.Log($"Found color source: {colorSourceInfo.Info.Id}");
                System.Diagnostics.Debug.WriteLine($"Found color source: {colorSourceInfo.Info.Id}");

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
                    // Find format matching user's selection
                    desiredFormat = colorSourceInfo.SupportedFormats
                        .FirstOrDefault(f =>
                            f.VideoFormat.Width == ViewModel.SelectedFormat.Width &&
                            f.VideoFormat.Height == ViewModel.SelectedFormat.Height &&
                            Math.Abs(f.FrameRate.Numerator / (double)f.FrameRate.Denominator - ViewModel.SelectedFormat.FrameRate) < 1);

                    if (desiredFormat == null)
                    {
                        // Fallback: match resolution only
                        desiredFormat = colorSourceInfo.SupportedFormats
                            .FirstOrDefault(f =>
                                f.VideoFormat.Width == ViewModel.SelectedFormat.Width &&
                                f.VideoFormat.Height == ViewModel.SelectedFormat.Height);
                    }
                }

                // Final fallback: use largest format
                desiredFormat ??= colorSourceInfo.SupportedFormats
                    .OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height)
                    .FirstOrDefault();

                if (desiredFormat != null)
                {
                    var fps = desiredFormat.FrameRate.Numerator / (double)desiredFormat.FrameRate.Denominator;
                    Logger.Log($"Setting preview format to: {desiredFormat.Subtype} {desiredFormat.VideoFormat.Width}x{desiredFormat.VideoFormat.Height}@{fps:F0}fps");
                    await colorSourceInfo.SetFormatAsync(desiredFormat);
                }

                Logger.Log("Creating frame reader...");
                System.Diagnostics.Debug.WriteLine("Creating frame reader...");

                // Create frame reader for preview
                _previewFrameReader = await ViewModel.MediaCapture.CreateFrameReaderAsync(colorSourceInfo);
                Logger.Log("Frame reader created successfully");
                System.Diagnostics.Debug.WriteLine("Frame reader created successfully");

                _previewFrameReader.FrameArrived += PreviewFrameReader_FrameArrived;
                Logger.Log("FrameArrived event handler attached");
                System.Diagnostics.Debug.WriteLine("FrameArrived event handler attached");

                var startResult = await _previewFrameReader.StartAsync();
                Logger.Log($"Frame reader start result: {startResult}");
                System.Diagnostics.Debug.WriteLine($"Frame reader start result: {startResult}");

                if (startResult == Windows.Media.Capture.Frames.MediaFrameReaderStartStatus.Success)
                {
                    Logger.Log("✓ Preview started successfully!");
                    System.Diagnostics.Debug.WriteLine("✓ Preview started successfully!");
                    ViewModel.StatusText = "Preview active - waiting for frames...";
                }
                else
                {
                    Logger.Log($"✗ Frame reader failed to start: {startResult}");
                    System.Diagnostics.Debug.WriteLine($"✗ Frame reader failed to start: {startResult}");
                    ViewModel.StatusText = $"Preview failed to start: {startResult}";
                }
            }
            else
            {
                Logger.Log("No suitable frame source found, trying fallback...");
                System.Diagnostics.Debug.WriteLine("No suitable frame source found, trying fallback...");
                // Fallback: try using MediaCapture.StartPreviewAsync() for older devices
                await ViewModel.MediaCapture.StartPreviewAsync();
                Logger.Log("Fallback preview started");
                System.Diagnostics.Debug.WriteLine("Fallback preview started");
                ViewModel.StatusText = "Preview active (fallback mode)";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"✗ EXCEPTION in StartPreviewInternalAsync: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
            ViewModel.StatusText = $"Preview failed: {ex.Message}";
        }

        Logger.Log("=== START PREVIEW END ===");
        System.Diagnostics.Debug.WriteLine("=== StartPreviewInternalAsync END ===");
    }

    private void StopPreviewInternal()
    {
        try
        {
            if (_previewFrameReader != null)
            {
                _previewFrameReader.FrameArrived -= PreviewFrameReader_FrameArrived;
                _ = _previewFrameReader.StopAsync();
                _previewFrameReader.Dispose();
                _previewFrameReader = null;
            }

            if (ViewModel.MediaCapture != null)
            {
                _ = ViewModel.MediaCapture.StopPreviewAsync();
            }

            PreviewImage.Source = null;
            _previewSource = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stop preview failed: {ex.Message}");
        }
    }

    private static int _frameCounter = 0;

    private async void PreviewFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        _frameCounter++;
        if (_frameCounter % 30 == 1) // Log every 30th frame to avoid spam
        {
            Logger.Log($"Frame #{_frameCounter} arrived");
            System.Diagnostics.Debug.WriteLine($"Frame #{_frameCounter} arrived");
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
                System.Diagnostics.Debug.WriteLine("  - VideoMediaFrame is NULL");
            }
            return;
        }

        SoftwareBitmap? softwareBitmap = null;

        // Try to get SoftwareBitmap directly first
        if (frameRef.VideoMediaFrame.SoftwareBitmap != null)
        {
            softwareBitmap = frameRef.VideoMediaFrame.SoftwareBitmap;
            if (_frameCounter <= 3)
            {
                Logger.Log("  - Got SoftwareBitmap directly");
                System.Diagnostics.Debug.WriteLine("  - Got SoftwareBitmap directly");
            }
        }
        // If not available, try to get it from Direct3DSurface (hardware-accelerated formats like YUY2, NV12)
        else if (frameRef.VideoMediaFrame.Direct3DSurface != null)
        {
            if (_frameCounter <= 3)
            {
                Logger.Log("  - SoftwareBitmap is NULL, trying Direct3DSurface...");
                System.Diagnostics.Debug.WriteLine("  - SoftwareBitmap is NULL, trying Direct3DSurface...");
            }

            try
            {
                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frameRef.VideoMediaFrame.Direct3DSurface);
                if (_frameCounter <= 3)
                {
                    Logger.Log($"  - ✓ Created SoftwareBitmap from Direct3DSurface");
                    System.Diagnostics.Debug.WriteLine($"  - ✓ Created SoftwareBitmap from Direct3DSurface");
                }
            }
            catch (Exception ex)
            {
                if (_frameCounter <= 3)
                {
                    Logger.Log($"  - ✗ Failed to create from surface: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  - ✗ Failed to create from surface: {ex.Message}");
                }
                return;
            }
        }
        else
        {
            if (_frameCounter <= 5)
            {
                Logger.Log("  - No SoftwareBitmap or Direct3DSurface available");
                System.Diagnostics.Debug.WriteLine("  - No SoftwareBitmap or Direct3DSurface available");
            }
            return;
        }

        if (softwareBitmap == null)
        {
            return;
        }

        if (_frameCounter <= 3)
        {
            Logger.Log($"  - Bitmap size: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}");
            Logger.Log($"  - Bitmap format: {softwareBitmap.BitmapPixelFormat}");
            Logger.Log($"  - Alpha mode: {softwareBitmap.BitmapAlphaMode}");
            System.Diagnostics.Debug.WriteLine($"  - Bitmap size: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}");
            System.Diagnostics.Debug.WriteLine($"  - Bitmap format: {softwareBitmap.BitmapPixelFormat}");
            System.Diagnostics.Debug.WriteLine($"  - Alpha mode: {softwareBitmap.BitmapAlphaMode}");
        }

        // Convert to BGRA8 premultiplied for display
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
            softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            if (_frameCounter <= 3)
            {
                Logger.Log("  - Converted bitmap format");
                System.Diagnostics.Debug.WriteLine("  - Converted bitmap format");
            }
        }

        // Update the image on the UI thread
        var bitmap = softwareBitmap;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                if (_previewSource != null)
                {
                    await _previewSource.SetBitmapAsync(bitmap);
                    if (_frameCounter <= 3)
                    {
                        Logger.Log("  - ✓ Bitmap displayed on UI");
                        System.Diagnostics.Debug.WriteLine("  - ✓ Bitmap displayed on UI");
                    }
                }
                else
                {
                    if (_frameCounter <= 3)
                    {
                        Logger.Log("  - ✗ _previewSource is NULL!");
                        System.Diagnostics.Debug.WriteLine("  - ✗ _previewSource is NULL!");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"  - ✗ Error displaying frame: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  - ✗ Error displaying frame: {ex.Message}");
            }
        });
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshDevicesAsync();
    }

    private async void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsPreviewing)
        {
            await ViewModel.StopPreviewAsync();
        }
        else
        {
            await ViewModel.StartPreviewAsync();
        }
    }

    private async void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleRecordingAsync();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.BrowseOutputPathAsync();
    }
}
