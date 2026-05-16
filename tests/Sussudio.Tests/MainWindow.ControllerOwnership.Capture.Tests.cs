using System.Collections;
using System.Reflection;
using System.Linq;
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
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.PropertyChanges.cs").Replace("\r\n", "\n");
        var selectionSyncText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.SelectionSync.cs").Replace("\r\n", "\n");
        var selectionStateText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.SelectionState.cs").Replace("\r\n", "\n");
        var deviceSelectionText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.DeviceSelection.cs").Replace("\r\n", "\n");
        var audioSelectionText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.AudioSelection.cs").Replace("\r\n", "\n");
        var captureModeSelectionText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.CaptureModeSelection.cs").Replace("\r\n", "\n");
        var recordingSelectionText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.RecordingSelection.cs").Replace("\r\n", "\n");
        var stringSelectionText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.StringSelection.cs").Replace("\r\n", "\n");
        var selectionFamilyText = string.Join(
            "\n",
            deviceSelectionText,
            audioSelectionText,
            captureModeSelectionText,
            recordingSelectionText,
            stringSelectionText);
        var selectionNormalizerText = ReadRepoFile("Sussudio/Controllers/CaptureComboBoxSelectionNormalizer.cs").Replace("\r\n", "\n");

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
        AssertContains(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.AttachRecordingStringSelectionBindings();");
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
        AssertContains(contextText, "internal sealed class CaptureSelectionBindingControllerContext");
        AssertContains(selectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(selectionStateText, "internal sealed partial class CaptureSelectionBindingController");
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
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "public void AttachRecordingStringSelectionBindings()");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
        AssertContains(controllerText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(controllerText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
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
        AssertDoesNotContain(selectionStateText, "public void EnsureDeviceSelection()");
        AssertDoesNotContain(selectionStateText, "public void HandleSelectedDevicePropertyChanged()");
        AssertDoesNotContain(selectionStateText, "public void EnsureAudioInputSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureMicrophoneSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureResolutionSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureFrameRateSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureFormatSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureQualitySelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsurePresetSelection()");
        AssertDoesNotContain(selectionStateText, "public void EnsureSplitEncodeModeSelection()");
        AssertDoesNotContain(selectionFamilyText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(selectionFamilyText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(selectionFamilyText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(selectionFamilyText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(selectionFamilyText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(selectionFamilyText, "AvailableFrameRates.FirstOrDefault(option =>");
        AssertContains(deviceAudioText, "public void ApplyDeviceAudioControlState()");
        AssertContains(deviceAudioText, "public void EnsureDeviceAudioModeSelection()");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
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

    private static Task CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks()
    {
        var normalizerType = RequireType("Sussudio.Controllers.CaptureComboBoxSelectionNormalizer");
        var captureDeviceType = RequireType("Sussudio.Models.CaptureDevice");
        var audioInputDeviceType = RequireType("Sussudio.Models.AudioInputDevice");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var resolveCaptureDevice = RequireNormalizerMethod(normalizerType, "ResolveCaptureDeviceSelection");
        var resolveAudioInputDevice = RequireNormalizerMethod(normalizerType, "ResolveAudioInputDeviceSelection");
        var resolveResolution = RequireNormalizerMethod(normalizerType, "ResolveResolutionSelection");
        var resolveFrameRate = RequireNormalizerMethod(normalizerType, "ResolveFrameRateSelection");
        var resolveString = RequireNormalizerMethod(normalizerType, "ResolveStringSelection");

        var staleCaptureDevice = CreateNormalizerDevice(captureDeviceType, "DEVICE-A", "old device");
        var firstCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-b", "first device");
        var liveCaptureDevice = CreateNormalizerDevice(captureDeviceType, "device-a", "live device");
        var captureDevices = CreateNormalizerList(captureDeviceType, firstCaptureDevice, liveCaptureDevice);
        AssertEqual(
            liveCaptureDevice,
            resolveCaptureDevice.Invoke(null, new[] { captureDevices, staleCaptureDevice }),
            "capture-device matching returns live collection instance by case-insensitive id");

        var staleAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "MIC-1", "old mic");
        var firstAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "line-1", "first input");
        var liveAudioDevice = CreateNormalizerDevice(audioInputDeviceType, "mic-1", "live mic");
        var audioDevices = CreateNormalizerList(audioInputDeviceType, firstAudioDevice, liveAudioDevice);
        AssertEqual(
            liveAudioDevice,
            resolveAudioInputDevice.Invoke(null, new[] { audioDevices, staleAudioDevice }),
            "audio-device matching returns live collection instance by case-insensitive id");

        var disabledExactResolution = CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: false);
        var enabledFallbackResolution = CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true);
        var resolutionOptions = CreateResolutionOptionList(resolutionType, disabledExactResolution, enabledFallbackResolution);
        AssertEqual(
            disabledExactResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "3840X2160" }),
            "resolution exact selected value wins before enabled fallback");
        AssertEqual(
            enabledFallbackResolution,
            resolveResolution.Invoke(null, new[] { resolutionOptions, "1280x720" }),
            "resolution falls back to first enabled value");

        var disabledExactFrameRate = CreateFrameRateOption(frameRateType, 60d, 59.94d, "60000/1001", isEnabled: false);
        var autoFrameRate = CreateFrameRateOption(frameRateType, 0d, 0d, string.Empty, isEnabled: true);
        var enabledFrameRate = CreateFrameRateOption(frameRateType, 120d, 120d, "120/1", isEnabled: true);
        var frameRateOptions = CreateFrameRateOptionList(frameRateType, disabledExactFrameRate, autoFrameRate, enabledFrameRate);
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, true }),
            "auto frame-rate item wins when auto frame-rate is selected");
        AssertEqual(
            disabledExactFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 59.94d, false }),
            "frame-rate exact selected value wins before enabled fallback");
        AssertEqual(
            autoFrameRate,
            resolveFrameRate.Invoke(null, new object[] { frameRateOptions, 30d, false }),
            "frame-rate fallback preserves first enabled item ordering");

        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "quality" }),
            "string fallback is case-insensitive");
        AssertEqual(
            "Quality",
            resolveString.Invoke(null, new object[] { new[] { "Quality", "Preset" }, "Missing" }),
            "string fallback uses the first item when no case-insensitive match exists");

        return Task.CompletedTask;
    }

    private static MethodInfo RequireNormalizerMethod(Type normalizerType, string methodName)
        => normalizerType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"CaptureComboBoxSelectionNormalizer.{methodName} was not found.");

    private static object CreateNormalizerDevice(Type deviceType, string id, string name)
    {
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException($"Failed to create {deviceType.Name}.");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", name);
        return device;
    }

    private static object CreateNormalizerList(Type elementType, params object[] items)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
                           ?? throw new InvalidOperationException($"Failed to create list for {elementType.Name}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
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

    private static Task CaptureOptionPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureOptionPresentationController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/CaptureOptionPresentationPolicy.cs").Replace("\r\n", "\n");
        var tooltipFormatterText = ReadRepoFile("Sussudio/Controllers/CaptureOptionTooltipFormatter.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var captureOptionPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedCaptureOptions.cs").Replace("\r\n", "\n");
        var outputPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedOutput.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private CaptureOptionPresentationController _captureOptionPresentationController = null!;");
        AssertContains(captureOptionText, "private void InitializeCaptureOptionPresentationController()");
        AssertContains(captureOptionText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionText, "AudioClipText = AudioClipText");
        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateDecoderCountVisibility();");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.HandleDecoderCountSelectionChanged();");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.RefreshHdrHintText();");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.UpdateFpsTelemetryTooltip();");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyHdrToggleEnabledState();");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyBitrateVisibility();");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "=> _captureOptionPresentationController.ApplyAudioClipVisibility();");

        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class CaptureOptionPresentationController");
        AssertContains(controllerText, "private int _selectedDecoderCount = 4;");
        AssertContains(controllerText, "public void ApplyInitialDecoderCountSelection()");
        AssertContains(controllerText, "_selectedDecoderCount = affordances.InitialDecoderCount;");
        AssertContains(controllerText, "_context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;");
        AssertContains(controllerText, "public void UpdateDecoderCountVisibility()");
        AssertContains(policyText, "InitialDecoderCount: Math.Clamp(input.MjpegDecoderCount, 1, 8)");
        AssertContains(controllerText, "public void HandleDecoderCountSelectionChanged()");
        AssertContains(controllerText, "_context.ViewModel.MjpegDecoderCount = count;");
        AssertContains(controllerText, "public void RefreshHdrHintText()");
        AssertContains(controllerText, "public void UpdateFpsTelemetryTooltip()");
        AssertContains(controllerText, "public void ApplyHdrToggleEnabledState()");
        AssertContains(controllerText, "public void ApplyBitrateVisibility()");
        AssertContains(controllerText, "public void ApplyAudioClipVisibility()");
        AssertContains(controllerText, "_context.ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(controllerText, "CaptureOptionPresentationPolicy.Build(BuildPolicyInput())");
        AssertContains(controllerText, "private CaptureOptionPresentationInput BuildPolicyInput()");
        AssertContains(controllerText, "private static Visibility ToVisibility(bool isVisible)");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildHdrHintText(");
        AssertContains(controllerText, "CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip(");
        AssertContains(policyText, "internal static class CaptureOptionPresentationPolicy");
        AssertContains(policyText, "internal static CaptureOptionPresentationAffordances Build(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationInput(");
        AssertContains(policyText, "internal readonly record struct CaptureOptionPresentationAffordances(");
        AssertContains(policyText, "private static double ResolveSelectedFrameRate(CaptureOptionPresentationInput input)");
        AssertContains(policyText, "private static bool ShouldShowDecoderCount(");
        AssertContains(policyText, "selectedFrameRate >= 90");
        AssertDoesNotContain(policyText, "Microsoft.UI.Xaml");
        AssertContains(tooltipFormatterText, "internal static class CaptureOptionTooltipFormatter");
        AssertContains(tooltipFormatterText, "public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)");
        AssertContains(tooltipFormatterText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(tooltipFormatterText, "public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)");
        AssertDoesNotContain(controllerText, "var combinedHint =");
        AssertDoesNotContain(controllerText, "var parts = new List<string>();");
        AssertContains(controllerText, "_context.ViewModel.SourceTelemetrySummaryText");
        AssertContains(controllerText, "_context.ViewModel.SourceTargetSummaryText");
        AssertContains(mainWindowText, "InitializeCaptureOptionPresentationController();");
        AssertContains(propertyChangedText, "TryHandleOutputPropertyChanged(propertyName)");
        AssertContains(propertyChangedText, "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertContains(outputPropertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(captureOptionPropertyChangedText, "ApplyAudioClipVisibility();");
        AssertContains(captureOptionPropertyChangedText, "ApplyHdrToggleEnabledState();");
        AssertContains(captureOptionPropertyChangedText, "RefreshHdrHintText();");
        AssertContains(captureOptionPropertyChangedText, "UpdateFpsTelemetryTooltip();");
        AssertContains(captureOptionPropertyChangedText, "ApplyBitrateVisibility();");
        AssertDoesNotContain(bindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(bindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(bindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(bindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(bindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n"), "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertDoesNotContain(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertDoesNotContain(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertDoesNotContain(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertDoesNotContain(controllerText, "var isExplicitMjpg =");
        AssertDoesNotContain(controllerText, "var isAutoWithMjpgDevice =");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsHdrAvailable &&");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsCustomBitrateVisible ? Visibility.Visible");
        AssertDoesNotContain(controllerText, "_context.ViewModel.AudioClipping ? Visibility.Visible");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionPresentationPolicy_PreservesAffordanceRules()
    {
        var policyType = RequireType("Sussudio.Controllers.CaptureOptionPresentationPolicy");
        var inputType = RequireType("Sussudio.Controllers.CaptureOptionPresentationInput");
        var build = policyType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build was not found.");
        var constructor = inputType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 12);

        object Build(
            string? selectedVideoFormat,
            string? selectedFormatPixelFormat,
            double? selectedFrameRateOptionFriendlyValue,
            double? selectedFrameRateOptionValue,
            double selectedFrameRateFallback,
            int mjpegDecoderCount = 4,
            bool isHdrAvailable = true,
            bool isRecording = false,
            bool? sourceIsHdr = true,
            bool isHdrEnabled = true,
            bool isCustomBitrateVisible = false,
            bool audioClipping = false)
        {
            var input = constructor.Invoke(new object?[]
            {
                selectedVideoFormat,
                selectedFormatPixelFormat,
                selectedFrameRateOptionFriendlyValue,
                selectedFrameRateOptionValue,
                selectedFrameRateFallback,
                mjpegDecoderCount,
                isHdrAvailable,
                isRecording,
                sourceIsHdr,
                isHdrEnabled,
                isCustomBitrateVisible,
                audioClipping
            });

            return build.Invoke(null, new[] { input })
                ?? throw new InvalidOperationException("CaptureOptionPresentationPolicy.Build returned null.");
        }

        var explicitMjpgHighFps = Build("MJPG", null, 90d, null, 60d);
        AssertEqual(true, GetBoolProperty(explicitMjpgHighFps, "ShowDecoderCount"), "explicit MJPG at 90 FPS shows decoder count");

        var explicitMjpgLowFps = Build("MJPG", null, 89.99d, null, 120d);
        AssertEqual(false, GetBoolProperty(explicitMjpgLowFps, "ShowDecoderCount"), "explicit MJPG below 90 FPS hides decoder count");

        var autoMjpgValueFps = Build("Auto", "MJPG", 0d, 120d, 60d);
        AssertEqual(true, GetBoolProperty(autoMjpgValueFps, "ShowDecoderCount"), "Auto with MJPG device format uses frame-rate option value fallback");

        var autoNonMjpgHighFps = Build("Auto", "NV12", null, 120d, 60d);
        AssertEqual(false, GetBoolProperty(autoNonMjpgHighFps, "ShowDecoderCount"), "Auto with non-MJPG device format hides decoder count");

        var fallbackFrameRate = Build("MJPG", null, null, null, 120d);
        AssertEqual(true, GetBoolProperty(fallbackFrameRate, "ShowDecoderCount"), "missing frame-rate option falls back to selected frame rate");

        var sourceUnknown = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: null);
        AssertEqual(true, GetBoolProperty(sourceUnknown, "EnableHdrToggle"), "unknown source HDR state does not disable HDR toggle");

        var sdrSource = Build("Auto", "NV12", null, null, 60d, sourceIsHdr: false);
        AssertEqual(false, GetBoolProperty(sdrSource, "EnableHdrToggle"), "SDR source disables HDR toggle");

        var recording = Build("Auto", "NV12", null, null, 60d, isRecording: true);
        AssertEqual(false, GetBoolProperty(recording, "EnableHdrToggle"), "recording disables HDR toggle");
        AssertEqual(false, GetBoolProperty(recording, "EnableTrueHdrPreviewToggle"), "recording disables true-HDR preview toggle");

        var unavailableHdr = Build("Auto", "NV12", null, null, 60d, isHdrAvailable: false);
        AssertEqual(false, GetBoolProperty(unavailableHdr, "EnableHdrToggle"), "HDR unavailable disables HDR toggle");

        var customBitrate = Build("Auto", "NV12", null, null, 60d, isCustomBitrateVisible: true, audioClipping: true);
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowCustomBitrate"), "custom bitrate shows custom panel");
        AssertEqual(false, GetBoolProperty(customBitrate, "ShowPreset"), "custom bitrate hides preset panel");
        AssertEqual(true, GetBoolProperty(customBitrate, "ShowAudioClip"), "audio clipping shows warning text");

        var lowDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 0);
        var highDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 9);
        var normalDecoderCount = Build("Auto", "NV12", null, null, 60d, mjpegDecoderCount: 5);
        AssertEqual(1, GetIntProperty(lowDecoderCount, "InitialDecoderCount"), "decoder count clamps low");
        AssertEqual(8, GetIntProperty(highDecoderCount, "InitialDecoderCount"), "decoder count clamps high");
        AssertEqual(5, GetIntProperty(normalDecoderCount, "InitialDecoderCount"), "decoder count preserves valid values");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionBindings_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionBindings.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsText = ReadRepoFile("Sussudio/MainWindow.RecordingOptionBindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/CaptureOptionBindingController.cs").Replace("\r\n", "\n");
        var controllerContextText = ReadRepoFile("Sussudio/Controllers/CaptureOptionBindingController.Context.cs").Replace("\r\n", "\n");
        var controllerInitializationText = ReadRepoFile("Sussudio/Controllers/CaptureOptionBindingController.Initialization.cs").Replace("\r\n", "\n");
        var controllerSelectionHandlersText = ReadRepoFile("Sussudio/Controllers/CaptureOptionBindingController.SelectionHandlers.cs").Replace("\r\n", "\n");
        var controllerText = string.Join(
            "\n",
            controllerRootText,
            controllerContextText,
            controllerInitializationText,
            controllerSelectionHandlersText);
        var selectionBindingControllerText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var recordingOptionBindingsWithoutVideoFormat = recordingOptionBindingsText.Replace("VideoFormatComboBox.SelectionChanged +=", string.Empty);

        AssertContains(captureOptionBindingsText, "private CaptureOptionBindingController _captureOptionBindingController = null!;");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionBindingController()");
        AssertContains(captureOptionBindingsText, "ResolutionComboBox = ResolutionComboBox,");
        AssertContains(captureOptionBindingsText, "VideoFormatComboBox = VideoFormatComboBox,");
        AssertContains(captureOptionBindingsText, "TrueHdrPreviewToggle = TrueHdrPreviewToggle,");
        AssertContains(captureOptionBindingsText, "ApplyInitialDecoderCountSelection = ApplyInitialDecoderCountSelection,");
        AssertContains(captureOptionBindingsText, "EnsureSplitEncodeModeSelection = EnsureSplitEncodeModeSelection,");
        AssertContains(captureOptionBindingsText, "AttachRecordingStringSelectionBindings = AttachRecordingStringSelectionBindings");
        AssertContains(captureOptionBindingsText, "private void InitializeCaptureOptionCollections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.InitializeCollections();");
        AssertContains(captureOptionBindingsText, "private void ApplyInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.ApplyInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void EnsureInitialCaptureOptionSelections()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.EnsureInitialSelections();");
        AssertContains(captureOptionBindingsText, "private void AttachCaptureModeSelectionBindings()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.AttachCaptureModeSelectionBindings();");
        AssertContains(captureOptionBindingsText, "private void HandleCustomBitratePropertyChanged()");
        AssertContains(captureOptionBindingsText, "=> _captureOptionBindingController.HandleCustomBitratePropertyChanged();");
        AssertContains(recordingOptionBindingsText, "private void AttachRecordingOptionBindings()");
        AssertContains(recordingOptionBindingsText, "=> _captureOptionBindingController.AttachRecordingOptionBindings();");
        AssertContains(mainWindowText, "InitializeCaptureOptionBindingController();");

        AssertContains(controllerRootText, "internal sealed partial class CaptureOptionBindingController");
        AssertContains(controllerRootText, "private readonly CaptureOptionBindingControllerContext _context;");
        AssertContains(controllerRootText, "public CaptureOptionBindingController(CaptureOptionBindingControllerContext context)");
        AssertContains(controllerContextText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerInitializationText, "public void InitializeCollections()");
        AssertContains(controllerInitializationText, "public void ApplyInitialSelections()");
        AssertContains(controllerInitializationText, "public void EnsureInitialSelections()");
        AssertContains(controllerSelectionHandlersText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerSelectionHandlersText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void InitializeCollections()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void ApplyInitialSelections()");
        AssertDoesNotContain(controllerSelectionHandlersText, "public void EnsureInitialSelections()");
        AssertDoesNotContain(controllerInitializationText, "public void AttachCaptureModeSelectionBindings()");
        AssertDoesNotContain(controllerInitializationText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerRootText, "public void InitializeCollections()");
        AssertDoesNotContain(controllerRootText, "public void ApplyInitialSelections()");
        AssertDoesNotContain(controllerRootText, "public void EnsureInitialSelections()");
        AssertDoesNotContain(controllerRootText, "public void AttachCaptureModeSelectionBindings()");
        AssertDoesNotContain(controllerRootText, "public void AttachRecordingOptionBindings()");
        AssertDoesNotContain(controllerRootText, "internal sealed class CaptureOptionBindingControllerContext");
        AssertContains(controllerText, "public void InitializeCollections()");
        AssertContains(controllerText, "_context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;");
        AssertContains(controllerText, "for (var i = 1; i <= 8; i++)");
        AssertContains(controllerText, "_context.DecoderCountComboBox.Items.Add(i);");
        AssertContains(controllerText, "public void ApplyInitialSelections()");
        AssertContains(controllerText, "_context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");
        AssertContains(controllerText, "_context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;");
        AssertContains(controllerText, "_context.ApplyInitialDecoderCountSelection();");
        AssertContains(controllerText, "_context.ApplyBitrateVisibility();");
        AssertContains(controllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertContains(controllerText, "public void EnsureInitialSelections()");
        AssertContains(controllerText, "_context.EnsureSplitEncodeModeSelection();");
        AssertContains(controllerText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerText, "public void AttachCaptureModeSelectionBindings()");
        AssertContains(controllerText, "_context.ResolutionComboBox.SelectionChanged +=");
        AssertContains(controllerText, "_context.FrameRateComboBox.SelectionChanged +=");
        AssertContains(controllerText, "!string.Equals(resolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase)");
        AssertContains(controllerText, "if (CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(frameRate))");
        AssertContains(controllerText, "if (!_context.ViewModel.IsAutoFrameRateSelected)");
        AssertContains(controllerText, "else if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))");
        AssertDoesNotContain(controllerSelectionHandlersText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerSelectionHandlersText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(controllerSelectionHandlersText, "=> option.Value <= 0 || option.FriendlyValue <= 0;");
        AssertContains(controllerText, "public void AttachRecordingOptionBindings()");
        AssertContains(controllerText, "_context.AttachRecordingStringSelectionBindings();");
        AssertContains(controllerText, "_context.VideoFormatComboBox.SelectionChanged +=");
        AssertContains(controllerText, "_context.UpdateDecoderCountVisibility();");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.ValueChanged +=");
        AssertContains(controllerText, "if (!double.IsNaN(_context.CustomBitrateNumberBox.Value))");
        AssertContains(controllerText, "_context.HdrToggle.Click +=");
        AssertContains(controllerText, "_context.TrueHdrPreviewToggle.Click +=");
        AssertContains(controllerText, "public void HandleCustomBitratePropertyChanged()");
        AssertContains(controllerText, "Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(controllerText, "_context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;");

        AssertContains(bindingsText, "InitializeCaptureOptionCollections();");
        AssertContains(bindingsText, "ApplyInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "EnsureInitialCaptureOptionSelections();");
        AssertContains(bindingsText, "AttachCaptureModeSelectionBindings();");
        AssertContains(bindingsText, "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "InitializeCaptureOptionCollections();", "ApplyInitialCaptureOptionSelections();");
        AssertOccursBefore(bindingsText, "ApplyInitialCaptureOptionSelections();", "AttachRecordingOptionBindings();");
        AssertOccursBefore(bindingsText, "EnsureInitialCaptureOptionSelections();", "AttachCaptureModeSelectionBindings();");
        AssertOccursBefore(bindingsText, "AttachCaptureModeSelectionBindings();", "AttachRecordingOptionBindings();");
        AssertContains(selectionBindingControllerText, "public void AttachRecordingStringSelectionBindings()");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);");
        AssertContains(selectionBindingControllerText, "AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);");
        AssertContains(selectionBindingControllerText, "private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)");

        AssertDoesNotContain(captureOptionBindingsText, "VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;");
        AssertDoesNotContain(captureOptionBindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(captureOptionBindingsText, "ViewModel.SelectedFrameRate =");
        AssertDoesNotContain(recordingOptionBindingsWithoutVideoFormat, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "VideoFormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "HdrToggle.Click +=");
        AssertDoesNotContain(recordingOptionBindingsText, "TrueHdrPreviewToggle.Click +=");
        AssertDoesNotContain(recordingOptionBindingsText, "ViewModel.SelectedRecordingFormat =");
        AssertDoesNotContain(recordingOptionBindingsText, "QualityComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "PresetComboBox.SelectionChanged +=");
        AssertDoesNotContain(recordingOptionBindingsText, "SplitEncodeComboBox.SelectionChanged +=");
        AssertDoesNotContain(propertyChangedText, "CustomBitrateNumberBox.Value");
        AssertDoesNotContain(propertyChangedText, "Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01");
        AssertContains(propertyChangedText, "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertContains(ReadRepoFile("Sussudio/MainWindow.PropertyChangedCaptureOptions.cs").Replace("\r\n", "\n"), "HandleCustomBitratePropertyChanged();");
        AssertDoesNotContain(bindingsText, "ResolutionComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FrameRateComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "FormatComboBox.SelectionChanged +=");
        AssertDoesNotContain(bindingsText, "CustomBitrateNumberBox.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "HdrToggle.Click +=");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionTooltipFormatter_PreservesTooltipTextPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.CaptureOptionTooltipFormatter");
        var buildHdrHintText = formatterType.GetMethod("BuildHdrHintText", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildHdrHintText was not found.");
        var buildFpsTelemetryTooltip = formatterType.GetMethod("BuildFpsTelemetryTooltip", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip was not found.");

        string? Hdr(string? resolutionHint, string? readinessHint, bool isRecording)
            => buildHdrHintText.Invoke(null, new object?[] { resolutionHint, readinessHint, isRecording })?.ToString();

        string? Fps(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)
            => buildFpsTelemetryTooltip.Invoke(null, new object?[] { sourceTelemetrySummaryText, sourceTargetSummaryText })?.ToString();

        var stopRecordingText = "Stop recording before switching between HDR and SDR pipelines.";
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower",
            Hdr("  4K HDR requires 59.94 or lower ", " Source is SDR ", isRecording: false),
            "HDR hint trims and combines readiness before resolution support");
        AssertEqual(
            "4K HDR requires 59.94 or lower",
            Hdr("4K HDR requires 59.94 or lower", null, isRecording: false),
            "HDR hint uses resolution when readiness is empty");
        AssertEqual(
            stopRecordingText,
            Hdr(null, null, isRecording: true),
            "HDR hint uses recording guard when no other hint exists");
        AssertEqual(
            $"Source is SDR{System.Environment.NewLine}4K HDR requires 59.94 or lower{System.Environment.NewLine}{stopRecordingText}",
            Hdr("4K HDR requires 59.94 or lower", "Source is SDR", isRecording: true),
            "HDR hint appends recording guard after existing hints");
        AssertEqual(
            null,
            Hdr(" ", null, isRecording: false),
            "HDR hint returns null when no hint text exists");

        AssertEqual(
            $"Telemetry: NativeXu{System.Environment.NewLine}Target: 3840 x 2160",
            Fps("Telemetry: NativeXu", "Target: 3840 x 2160"),
            "FPS tooltip combines telemetry and target summaries");
        AssertEqual(
            "  Telemetry: NativeXu  ",
            Fps("  Telemetry: NativeXu  ", null),
            "FPS tooltip preserves existing telemetry summary whitespace");
        AssertEqual(
            "Target: 3840 x 2160",
            Fps(null, "Target: 3840 x 2160"),
            "FPS tooltip uses target summary when telemetry is empty");
        AssertEqual(
            null,
            Fps(" ", null),
            "FPS tooltip returns null when both summaries are empty");

        return Task.CompletedTask;
    }
}
