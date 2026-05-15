using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for capture-device refresh/apply button workflows.
public sealed partial class MainWindow
{
    private CaptureDeviceActionController _captureDeviceActionController = null!;

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

    private Task RefreshDevicesFromButtonAsync()
        => _captureDeviceActionController.RefreshDevicesAsync();

    private Task ApplySelectedDeviceFromButtonAsync()
        => _captureDeviceActionController.ApplySelectedDeviceAsync();

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));
    }

    private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));
    }
}
