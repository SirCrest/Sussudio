using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureDeviceActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureDeviceActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureDeviceActionController _captureDeviceActionController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureDeviceActionController()");
        AssertContains(adapterText, "RefreshButton = RefreshButton,");
        AssertContains(adapterText, "ApplyDeviceButton = ApplyDeviceButton,");
        AssertContains(adapterText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState");
        AssertContains(adapterText, "private Task RefreshDevicesFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.RefreshDevicesAsync();");
        AssertContains(adapterText, "private Task ApplySelectedDeviceFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.ApplySelectedDeviceAsync();");
        AssertContains(adapterText, "private void RefreshButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(adapterText, "private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(adapterText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(adapterText, "UpdateDeviceApplyButtonState();");

        return Task.CompletedTask;
    }
}
