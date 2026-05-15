using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var contextText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.Context.cs").Replace("\r\n", "\n");
        var deviceAudioText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.DeviceAudio.cs").Replace("\r\n", "\n");
        var selectionSyncText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.SelectionSync.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(propertyChangedText, "EnsureResolutionSelection();");
        AssertContains(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertContains(controllerText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(contextText, "internal sealed class CaptureSelectionBindingControllerContext");
        AssertContains(selectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
        AssertContains(deviceAudioText, "public void ApplyDeviceAudioControlState()");
        AssertContains(deviceAudioText, "public void EnsureDeviceAudioModeSelection()");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(selectionSyncText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");

        return Task.CompletedTask;
    }

    private static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureDeviceActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureDeviceActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureDeviceActionController _captureDeviceActionController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureDeviceActionController()");
        AssertContains(adapterText, "RefreshButton = RefreshButton,");
        AssertContains(adapterText, "ApplyDeviceButton = ApplyDeviceButton,");
        AssertContains(adapterText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState");
        AssertContains(adapterText, "private Task RefreshDevicesFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.RefreshDevicesAsync();");
        AssertContains(adapterText, "private Task ApplySelectedDeviceFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.ApplySelectedDeviceAsync();");
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(eventHandlersText, "UpdateDeviceApplyButtonState();");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionPresentation_LivesInFocusedPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionPresentation.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.RecordingOptionBindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertContains(captureOptionText, "private double GetSelectedFriendlyFrameRate()");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(captureOptionText, "ViewModel.SourceTelemetrySummaryText");
        AssertContains(bindingsText, "ApplyHdrToggleEnabledState();");
        AssertContains(bindingsText, "RefreshHdrHintText();");
        AssertContains(bindingsText, "UpdateFpsTelemetryTooltip();");
        AssertContains(bindingsText, "ApplyBitrateVisibility();");
        AssertContains(bindingsText, "ApplyAudioClipVisibility();");
        AssertContains(bindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "EnsureSplitEncodeModeSelection();", "AttachRecordingOptionBindings();");
        AssertContains(recordingOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(recordingOptionBindingsText, "FormatComboBox.SelectionChanged +=");
        AssertContains(recordingOptionBindingsText, "ViewModel.SelectedRecordingFormat = format;");
        AssertContains(recordingOptionBindingsText, "QualityComboBox.SelectionChanged +=");
        AssertContains(recordingOptionBindingsText, "PresetComboBox.SelectionChanged +=");
        AssertContains(recordingOptionBindingsText, "SplitEncodeComboBox.SelectionChanged +=");
        AssertContains(recordingOptionBindingsText, "VideoFormatComboBox.SelectionChanged +=");
        AssertContains(recordingOptionBindingsText, "UpdateDecoderCountVisibility();");
        AssertContains(recordingOptionBindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(recordingOptionBindingsText, "HdrToggle.Click +=");
        AssertContains(recordingOptionBindingsText, "TrueHdrPreviewToggle.Click +=");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(propertyChangedText, "ApplyAudioClipVisibility();");
        AssertContains(propertyChangedText, "ApplyHdrToggleEnabledState();");
        AssertContains(propertyChangedText, "RefreshHdrHintText();");
        AssertContains(propertyChangedText, "UpdateFpsTelemetryTooltip();");
        AssertContains(propertyChangedText, "ApplyBitrateVisibility();");
        AssertDoesNotContain(bindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(bindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(bindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(bindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n"), "private int _selectedDecoderCount = 4;");

        return Task.CompletedTask;
    }
}
