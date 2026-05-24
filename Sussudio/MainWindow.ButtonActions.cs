using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for MainWindow recording, button, and output-path
// workflows. The controllers own the actual action/display policies; this
// partial keeps event handlers and private adapter methods near the buttons and
// recording chrome they drive.
public sealed partial class MainWindow
{
    private RecordingButtonActionController _recordingButtonActionController = null!;
    private RecordingButtonChromeController _recordingButtonChromeController = null!;
    private RecordingStatePresentationController _recordingStatePresentationController = null!;
    private CaptureDeviceActionController _captureDeviceActionController = null!;
    private OutputPathController _outputPathController = null!;
    private PreviewScreenshotController _previewScreenshotController = null!;

    private void InitializeRecordingButtonActionController()
    {
        _recordingButtonActionController = new RecordingButtonActionController(new RecordingButtonActionControllerContext
        {
            ViewModel = ViewModel,
            GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(
                _previewRendererHostController.HasD3DRenderer && PreviewSwapChainPanel.Visibility == Visibility.Visible,
                _previewRendererHostController.IsCpuPreviewSourceAttached && PreviewImage.Visibility == Visibility.Visible,
                NoDevicePlaceholder.Visibility == Visibility.Visible)
        });
    }

    private void InitializeRecordingButtonChromeController()
    {
        _recordingButtonChromeController = new RecordingButtonChromeController(new RecordingButtonChromeControllerContext
        {
            RecordingGlowBorder = RecordingGlowBorder,
            RecordingGlowPulseStoryboard = RecordingGlowPulseStoryboard,
            RecPulseStoryboard = RecPulseStoryboard,
            RecordButton = RecordButton,
            RecordButtonNormalContent = RecordButtonNormalContent,
            RecordButtonStartingContent = RecordButtonStartingContent,
            RecordButtonRecordingContent = RecordButtonRecordingContent,
        });
    }

    private void InitializeRecordingStatePresentationController()
    {
        _recordingStatePresentationController = new RecordingStatePresentationController(new RecordingStatePresentationControllerContext
        {
            ViewModel = ViewModel,
            RecordingButtonChrome = _recordingButtonChromeController,
            AudioRecordToggle = AudioRecordToggle,
            CustomAudioToggle = CustomAudioToggle,
            MicrophoneToggle = MicrophoneToggle,
            AudioInputComboBox = AudioInputComboBox,
            MicrophoneComboBox = MicrophoneComboBox,
            DeviceAudioModeToggle = DeviceAudioModeToggle,
            AnalogAudioGainSlider = AnalogAudioGainSlider,
            ResetAudioMeterVisuals = ResetAudioMeterVisuals,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            RefreshHdrHintText = RefreshHdrHintText,
            UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
            ApplyWindowTitle = ApplyWindowTitle,
        });
    }

    private void InitializeCaptureDeviceActionController()
    {
        _captureDeviceActionController = new CaptureDeviceActionController(new CaptureDeviceActionControllerContext
        {
            ViewModel = ViewModel,
            RefreshButton = RefreshButton,
            ApplyDeviceButton = ApplyDeviceButton,
            DeviceComboBox = DeviceComboBox,
            UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState
        });
    }

    private void InitializeOutputPathController()
    {
        _outputPathController = new OutputPathController(new OutputPathControllerContext
        {
            OutputPathTextBox = OutputPathTextBox,
            GetWindowHandle = () => _hwnd,
            GetOutputPath = () => ViewModel.OutputPath,
            SetOutputPath = path => ViewModel.OutputPath = path,
            SetStatusText = text => ViewModel.StatusText = text,
            OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()
        });
    }

    private void InitializePreviewScreenshotController()
    {
        _previewScreenshotController = new PreviewScreenshotController(new PreviewScreenshotControllerContext
        {
            ViewModel = ViewModel,
            ScreenshotButton = ScreenshotButton,
        });
    }

    private Task ToggleRecordingFromButtonAsync()
        => _recordingButtonActionController.ToggleRecordingAsync();

    private bool TryHandleRecordingPropertyChanged(string propertyName)
        => _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);

    private void ApplyInitialRecordingStatePresentation()
        => _recordingStatePresentationController.HandleFfmpegMissingChanged();

    private Task RefreshDevicesFromButtonAsync()
        => _captureDeviceActionController.RefreshDevicesAsync();

    private Task ApplySelectedDeviceFromButtonAsync()
        => _captureDeviceActionController.ApplySelectedDeviceAsync();

    private void AttachOutputPathDisplay()
        => _outputPathController.AttachDisplay();

    private void UpdateOutputPathDisplay()
        => _outputPathController.UpdateDisplay();

    private Task BrowseOutputPathFromButtonAsync()
        => _outputPathController.BrowseAsync();

    private Task OpenRecordingsFolderFromButtonAsync()
        => _outputPathController.OpenRecordingsFolderIfAvailableAsync();

    private Task CapturePreviewScreenshotAsync()
        => _previewScreenshotController.CaptureAsync();

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));
    }

    private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));
    }

    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));
    }

    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));
    }

    private bool TryHandleOutputPropertyChanged(string propertyName)
        => _outputPathController.TryHandlePropertyChanged(propertyName);
}
