using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for MainWindow button workflows. The controllers own the
// actual action policies; this partial keeps event handlers and private adapter
// methods near the buttons that invoke them.
public sealed partial class MainWindow
{
    private RecordingButtonActionController _recordingButtonActionController = null!;
    private CaptureDeviceActionController _captureDeviceActionController = null!;

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

    private Task ToggleRecordingFromButtonAsync()
        => _recordingButtonActionController.ToggleRecordingAsync();

    private Task RefreshDevicesFromButtonAsync()
        => _captureDeviceActionController.RefreshDevicesAsync();

    private Task ApplySelectedDeviceFromButtonAsync()
        => _captureDeviceActionController.ApplySelectedDeviceAsync();

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
}
