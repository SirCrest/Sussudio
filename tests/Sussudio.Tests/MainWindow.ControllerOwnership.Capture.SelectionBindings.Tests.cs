using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var collectionBindingsText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.CollectionBindings.cs").Replace("\r\n", "\n");
        var deviceAudioText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceAudio.cs").Replace("\r\n", "\n");
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.PropertyChanges.cs").Replace("\r\n", "\n");
        var selectionSyncText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.SelectionSync.cs").Replace("\r\n", "\n");
        var deviceSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceSelection.cs").Replace("\r\n", "\n");
        var audioSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.AudioSelection.cs").Replace("\r\n", "\n");
        var captureModeSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.CaptureModeSelection.cs").Replace("\r\n", "\n");
        var recordingSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.RecordingSelection.cs").Replace("\r\n", "\n");
        var stringSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.StringSelection.cs").Replace("\r\n", "\n");
        var selectionFamilyText = string.Join(
            "\n",
            deviceSelectionText,
            audioSelectionText,
            captureModeSelectionText,
            recordingSelectionText,
            stringSelectionText);
        var selectionControllerFamilyText = string.Join(
            "\n",
            controllerText,
            collectionBindingsText,
            deviceAudioText,
            propertyChangesText,
            selectionSyncText,
            selectionFamilyText);
        var selectionNormalizerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureComboBoxSelectionNormalizer.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
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
        AssertContains(propertyChangesText, "public bool TryHandlePropertyChanged(string? propertyName)");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedFrameRate):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedDeviceAudioMode):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AnalogAudioGainPercent):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertContains(propertyChangesText, "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedDevice):", "case nameof(MainViewModel.SelectedResolution):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedResolution):", "case nameof(MainViewModel.SelectedFrameRate):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.IsAutoFrameRateSelected):", "case nameof(MainViewModel.AvailableResolutions):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableResolutions):", "case nameof(MainViewModel.AvailableFrameRates):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableFrameRates):", "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableDeviceAudioModes):", "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedAudioInputDevice):", "case nameof(MainViewModel.SelectedMicrophoneDevice):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedMicrophoneDevice):", "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedRecordingFormat):", "case nameof(MainViewModel.SelectedQuality):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedQuality):", "case nameof(MainViewModel.AvailablePresets):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailablePresets):", "case nameof(MainViewModel.SelectedPreset):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.SelectedPreset):", "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertOccursBefore(propertyChangesText, "case nameof(MainViewModel.AvailableSplitEncodeModes):", "case nameof(MainViewModel.SelectedSplitEncodeMode):");
        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(propertyChangedText, "if (TryHandleCaptureSelectionPropertyChanged(propertyName))");
        AssertContains(controllerText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(controllerText, "private readonly CaptureSelectionBindingControllerContext _context;");
        AssertContains(controllerText, "public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)");
        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingControllerContext");
        AssertContains(collectionBindingsText, "public void AttachCollectionBindings()");
        AssertContains(selectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureSelectionBindingController.SelectionState.cs")),
            "empty selection-state marker partial should stay removed");
        AssertContains(deviceSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(audioSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(captureModeSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(recordingSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(stringSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(selectionSyncText, "public void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(selectionSyncText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;");
        AssertContains(selectionSyncText, "EnsureResolutionSelection();");
        AssertContains(selectionSyncText, "public void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(selectionSyncText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;");
        AssertContains(selectionSyncText, "EnsureFrameRateSelection();");
        AssertContains(selectionSyncText, "public void HandleAvailablePresetsPropertyChanged()");
        AssertContains(selectionSyncText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;");
        AssertContains(selectionSyncText, "EnsurePresetSelection();");
        AssertContains(selectionSyncText, "public void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(selectionSyncText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;");
        AssertContains(selectionSyncText, "EnsureSplitEncodeModeSelection();");
        AssertOccursBefore(selectionSyncText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;", "EnsureResolutionSelection();");
        AssertOccursBefore(selectionSyncText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;", "EnsureFrameRateSelection();");
        AssertOccursBefore(selectionSyncText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;", "EnsurePresetSelection();");
        AssertOccursBefore(selectionSyncText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;", "EnsureSplitEncodeModeSelection();");
        AssertContains(collectionBindingsText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(collectionBindingsText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
        AssertDoesNotContain(controllerText, "public void AttachCollectionBindings()");
        AssertDoesNotContain(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertDoesNotContain(controllerText, "AttachCollectionSync(");
        AssertDoesNotContain(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(adapterText, "_captureSelectionBindingController.AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionControllerFamilyText, "public void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(selectionControllerFamilyText, "private static void AttachStringSelection(");
        AssertDoesNotContain(selectionControllerFamilyText, "AttachStringSelection(_context.FormatComboBox");
        AssertContains(deviceSelectionText, "public void EnsureDeviceSelection()");
        AssertContains(deviceSelectionText, "public void HandleSelectedDevicePropertyChanged()");
        AssertContains(audioSelectionText, "public void EnsureAudioInputSelection()");
        AssertContains(audioSelectionText, "public void EnsureMicrophoneSelection()");
        AssertContains(captureModeSelectionText, "public void EnsureResolutionSelection()");
        AssertContains(captureModeSelectionText, "public void EnsureFrameRateSelection()");
        AssertContains(recordingSelectionText, "public void EnsureFormatSelection()");
        AssertContains(recordingSelectionText, "public void EnsureQualitySelection()");
        AssertContains(recordingSelectionText, "public void EnsurePresetSelection()");
        AssertContains(recordingSelectionText, "public void EnsureSplitEncodeModeSelection()");
        AssertContains(deviceSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(");
        AssertContains(audioSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(");
        AssertContains(captureModeSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(");
        AssertContains(captureModeSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(");
        AssertContains(stringSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);");
        AssertContains(selectionNormalizerText, "internal static class CaptureComboBoxSelectionNormalizer");
        AssertContains(selectionNormalizerText, "public static CaptureDevice? ResolveCaptureDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static AudioInputDevice? ResolveAudioInputDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static ResolutionOption? ResolveResolutionSelection(");
        AssertContains(selectionNormalizerText, "public static FrameRateOption? ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "public static string? ResolveStringSelection(");
        AssertContains(selectionNormalizerText, "public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(selectionNormalizerText, "public static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertContains(deviceSelectionText, "DEVICE_SELECTION_SYNC");
        AssertContains(deviceSelectionText, "EnsureDeviceSelection();");
        AssertContains(deviceSelectionText, "UpdateDeviceApplyButtonState();");
        AssertContains(deviceSelectionText, "public bool HasPendingDeviceSelection()");
        AssertContains(deviceSelectionText, "public void UpdateDeviceApplyButtonState()");
        AssertDoesNotContain(controllerText, "public bool HasPendingDeviceSelection()");
        AssertDoesNotContain(controllerText, "public void UpdateDeviceApplyButtonState()");
        AssertContains(propertyChangesText, "HandleSelectedDevicePropertyChanged();");
        AssertContains(propertyChangesText, "EnsureResolutionSelection();");
        AssertContains(propertyChangesText, "EnsureFrameRateSelection();");
        AssertContains(propertyChangesText, "HandleAvailableResolutionsPropertyChanged();");
        AssertContains(propertyChangesText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertContains(propertyChangesText, "ApplyDeviceAudioControlState();");
        AssertContains(propertyChangesText, "EnsureAudioInputSelection();");
        AssertContains(propertyChangesText, "EnsureMicrophoneSelection();");
        AssertContains(propertyChangesText, "EnsureFormatSelection();");
        AssertContains(propertyChangesText, "EnsureQualitySelection();");
        AssertContains(propertyChangesText, "HandleAvailablePresetsPropertyChanged();");
        AssertContains(propertyChangesText, "EnsurePresetSelection();");
        AssertContains(propertyChangesText, "HandleAvailableSplitEncodeModesPropertyChanged();");
        AssertContains(propertyChangesText, "EnsureSplitEncodeModeSelection();");
        var selectedDevicePropertyChangedText = deviceSelectionText.Substring(
            deviceSelectionText.IndexOf("public void HandleSelectedDevicePropertyChanged()", System.StringComparison.Ordinal));
        AssertOccursBefore(selectedDevicePropertyChangedText, "DEVICE_SELECTION_SYNC", "EnsureDeviceSelection();");
        AssertOccursBefore(selectedDevicePropertyChangedText, "EnsureDeviceSelection();", "UpdateDeviceApplyButtonState();");
        AssertOccursBefore(deviceSelectionText, "public void EnsureDeviceSelection()", "public void HandleSelectedDevicePropertyChanged()");
        AssertOccursBefore(audioSelectionText, "public void EnsureAudioInputSelection()", "public void EnsureMicrophoneSelection()");
        AssertOccursBefore(captureModeSelectionText, "public void EnsureResolutionSelection()", "public void EnsureFrameRateSelection()");
        AssertOccursBefore(recordingSelectionText, "public void EnsureFormatSelection()", "public void EnsureQualitySelection()");
        AssertOccursBefore(recordingSelectionText, "public void EnsureQualitySelection()", "public void EnsurePresetSelection()");
        AssertOccursBefore(recordingSelectionText, "public void EnsurePresetSelection()", "public void EnsureSplitEncodeModeSelection()");
        AssertDoesNotContain(selectionFamilyText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(selectionFamilyText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(selectionFamilyText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(selectionFamilyText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(selectionFamilyText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(selectionFamilyText, "AvailableFrameRates.FirstOrDefault(option =>");
        AssertContains(deviceAudioText, "public void ApplyDeviceAudioControlState()");
        AssertContains(deviceAudioText, "public void EnsureDeviceAudioModeSelection()");
        AssertContains(deviceSelectionText, "public bool HasPendingDeviceSelection()");
        AssertContains(selectionSyncText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "public void EnsureDeviceSelection()");
        AssertDoesNotContain(controllerText, "public void HandleSelectedDevicePropertyChanged()");
        AssertDoesNotContain(controllerText, "public void EnsureAudioInputSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureMicrophoneSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureResolutionSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureFrameRateSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureFormatSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureQualitySelection()");
        AssertDoesNotContain(controllerText, "public void EnsurePresetSelection()");
        AssertDoesNotContain(controllerText, "public void EnsureSplitEncodeModeSelection()");
        AssertDoesNotContain(controllerText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(controllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(controllerText, "DEVICE_SELECTION_SYNC");
        AssertDoesNotContain(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedAudioInputDevice):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");
        AssertDoesNotContain(propertyChangedText, "DEVICE_SELECTION_SYNC");
        AssertDoesNotContain(propertyChangedText, "DeviceComboBox.SelectedItem");
        AssertDoesNotContain(propertyChangedText, "ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;");
        AssertDoesNotContain(propertyChangedText, "FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;");
        AssertDoesNotContain(propertyChangedText, "PresetComboBox.ItemsSource = ViewModel.AvailablePresets;");
        AssertDoesNotContain(propertyChangedText, "SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedDevice):");
        AssertDoesNotContain(propertyChangedText, "HandleSelectedDevicePropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableResolutions):");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableResolutionsPropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableFrameRates):");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDeviceAudioControlSupported):");
        AssertDoesNotContain(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.SelectedRecordingFormat):");
        AssertDoesNotContain(propertyChangedText, "EnsureFormatSelection();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.AvailableSplitEncodeModes):");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableSplitEncodeModesPropertyChanged();");

        return Task.CompletedTask;
    }
}
