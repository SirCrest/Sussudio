using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void AttachDeviceSelectionChangedBinding()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.AttachDeviceSelectionChangedBinding();");
        AssertContains(adapterText, "private void HandleSelectedDevicePropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailablePresetsPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();");
        AssertContains(adapterText, "private void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(adapterText, "private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");

        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(bindingsText, "AttachDeviceSelectionChangedBinding();");
        AssertContains(propertyChangedText, "if (TryHandleCaptureSelectionPropertyChanged(propertyName))");

        AssertContains(controllerText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(controllerText, "private readonly CaptureSelectionBindingControllerContext _context;");
        AssertContains(controllerText, "public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)");
        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingControllerContext");

        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(propertyChangedText, "DEVICE_SELECTION_SYNC");
        AssertDoesNotContain(propertyChangedText, "DeviceComboBox.SelectedItem");
        AssertDoesNotContain(propertyChangedText, "ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;");
        AssertDoesNotContain(propertyChangedText, "FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;");
        AssertDoesNotContain(propertyChangedText, "PresetComboBox.ItemsSource = ViewModel.AvailablePresets;");
        AssertDoesNotContain(propertyChangedText, "SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");

        return Task.CompletedTask;
    }
}
