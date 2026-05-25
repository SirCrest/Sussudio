using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.CaptureSelectionBindings.Composition.cs")), "MainWindow capture selection adapter folded into MainWindow.CaptureBindings.cs");

        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(bindingsText, "AttachDeviceSelectionChangedBinding();");
        AssertContains(propertyChangedText, "TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
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

    internal static Task CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertContains(controllerText, "public void EnsureDeviceAudioModeSelection()");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingCollectionSync_LivesInControllerPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)");
        AssertContains(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertContains(controllerText, "public void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(controllerText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;");
        AssertContains(controllerText, "EnsureResolutionSelection();");
        AssertContains(controllerText, "public void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(controllerText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;");
        AssertContains(controllerText, "EnsureFrameRateSelection();");
        AssertContains(controllerText, "public void HandleAvailablePresetsPropertyChanged()");
        AssertContains(controllerText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;");
        AssertContains(controllerText, "EnsurePresetSelection();");
        AssertContains(controllerText, "public void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(controllerText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;");
        AssertContains(controllerText, "EnsureSplitEncodeModeSelection();");
        AssertOccursBefore(controllerText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;", "EnsureResolutionSelection();");
        AssertOccursBefore(controllerText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;", "EnsureFrameRateSelection();");
        AssertOccursBefore(controllerText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;", "EnsurePresetSelection();");
        AssertOccursBefore(controllerText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;", "EnsureSplitEncodeModeSelection();");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureSelectionBindingController.SelectionState.cs")),
            "empty selection-state marker partial should stay removed");
        AssertDoesNotContain(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(adapterText, "_captureSelectionBindingController.AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingPropertyRouter_LivesInController()
    {
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

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

        AssertContains(adapterText, "=> _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(propertyChangedText, "HandleSelectedDevicePropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableResolutionsPropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableFrameRatesPropertyChanged();");
        AssertDoesNotContain(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertDoesNotContain(propertyChangedText, "EnsureFormatSelection();");
        AssertDoesNotContain(propertyChangedText, "HandleAvailableSplitEncodeModesPropertyChanged();");

        return Task.CompletedTask;
    }

    internal static Task CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var selectionNormalizerText = controllerText.Substring(
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));
        var bindingControllerText = controllerText.Substring(
            0,
            controllerText.IndexOf("internal static class CaptureComboBoxSelectionNormalizer", System.StringComparison.Ordinal));

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void EnsureDeviceSelection()");
        AssertContains(controllerText, "public void AttachDeviceSelectionChangedBinding()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public void HandleSelectedDevicePropertyChanged()");
        AssertContains(controllerText, "DEVICE_SELECTION_SYNC");
        AssertContains(controllerText, "EnsureDeviceSelection();");
        AssertContains(controllerText, "UpdateDeviceApplyButtonState();");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(controllerText, "public void UpdateDeviceApplyButtonState()");
        var selectedDevicePropertyChangedText = controllerText.Substring(
            controllerText.IndexOf("public void HandleSelectedDevicePropertyChanged()", System.StringComparison.Ordinal));
        AssertOccursBefore(selectedDevicePropertyChangedText, "DEVICE_SELECTION_SYNC", "EnsureDeviceSelection();");
        AssertOccursBefore(selectedDevicePropertyChangedText, "EnsureDeviceSelection();", "UpdateDeviceApplyButtonState();");
        AssertOccursBefore(controllerText, "public void EnsureDeviceSelection()", "public void HandleSelectedDevicePropertyChanged()");

        AssertContains(controllerText, "public void EnsureAudioInputSelection()");
        AssertContains(controllerText, "public void EnsureMicrophoneSelection()");
        AssertOccursBefore(controllerText, "public void EnsureAudioInputSelection()", "public void EnsureMicrophoneSelection()");

        AssertContains(controllerText, "public void EnsureResolutionSelection()");
        AssertContains(controllerText, "public void EnsureFrameRateSelection()");
        AssertOccursBefore(controllerText, "public void EnsureResolutionSelection()", "public void EnsureFrameRateSelection()");

        AssertContains(controllerText, "public void EnsureFormatSelection()");
        AssertContains(controllerText, "public void EnsureQualitySelection()");
        AssertContains(controllerText, "public void EnsurePresetSelection()");
        AssertContains(controllerText, "public void EnsureSplitEncodeModeSelection()");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);");
        AssertOccursBefore(controllerText, "public void EnsureFormatSelection()", "public void EnsureQualitySelection()");
        AssertOccursBefore(controllerText, "public void EnsureQualitySelection()", "public void EnsurePresetSelection()");
        AssertOccursBefore(controllerText, "public void EnsurePresetSelection()", "public void EnsureSplitEncodeModeSelection()");

        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(");
        AssertContains(controllerText, "CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "internal static class CaptureComboBoxSelectionNormalizer");
        AssertContains(selectionNormalizerText, "public static CaptureDevice? ResolveCaptureDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static AudioInputDevice? ResolveAudioInputDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static ResolutionOption? ResolveResolutionSelection(");
        AssertContains(selectionNormalizerText, "public static FrameRateOption? ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "public static string? ResolveStringSelection(");
        AssertContains(selectionNormalizerText, "public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(selectionNormalizerText, "public static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertDoesNotContain(bindingsText, "DeviceComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingControllerText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(bindingControllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(bindingControllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(bindingControllerText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(bindingControllerText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(bindingControllerText, "AvailableFrameRates.FirstOrDefault(option =>");

        return Task.CompletedTask;
    }
}
