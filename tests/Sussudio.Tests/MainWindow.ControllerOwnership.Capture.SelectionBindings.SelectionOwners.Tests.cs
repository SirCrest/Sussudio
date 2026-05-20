using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var deviceSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceSelection.cs").Replace("\r\n", "\n");
        var audioSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.AudioSelection.cs").Replace("\r\n", "\n");
        var captureModeSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.CaptureModeSelection.cs").Replace("\r\n", "\n");
        var recordingSelectionText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.RecordingSelection.cs").Replace("\r\n", "\n");
        var selectionFamilyText = string.Join(
            "\n",
            deviceSelectionText,
            audioSelectionText,
            captureModeSelectionText,
            recordingSelectionText);
        var selectionNormalizerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureComboBoxSelectionNormalizer.cs").Replace("\r\n", "\n");

        AssertContains(deviceSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(audioSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(captureModeSelectionText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(recordingSelectionText, "internal sealed partial class CaptureSelectionBindingController");

        AssertContains(deviceSelectionText, "public void EnsureDeviceSelection()");
        AssertContains(deviceSelectionText, "public void AttachDeviceSelectionChangedBinding()");
        AssertContains(deviceSelectionText, "_context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();");
        AssertContains(deviceSelectionText, "public void HandleSelectedDevicePropertyChanged()");
        AssertContains(deviceSelectionText, "DEVICE_SELECTION_SYNC");
        AssertContains(deviceSelectionText, "EnsureDeviceSelection();");
        AssertContains(deviceSelectionText, "UpdateDeviceApplyButtonState();");
        AssertContains(deviceSelectionText, "public bool HasPendingDeviceSelection()");
        AssertContains(deviceSelectionText, "public void UpdateDeviceApplyButtonState()");
        var selectedDevicePropertyChangedText = deviceSelectionText.Substring(
            deviceSelectionText.IndexOf("public void HandleSelectedDevicePropertyChanged()", System.StringComparison.Ordinal));
        AssertOccursBefore(selectedDevicePropertyChangedText, "DEVICE_SELECTION_SYNC", "EnsureDeviceSelection();");
        AssertOccursBefore(selectedDevicePropertyChangedText, "EnsureDeviceSelection();", "UpdateDeviceApplyButtonState();");
        AssertOccursBefore(deviceSelectionText, "public void EnsureDeviceSelection()", "public void HandleSelectedDevicePropertyChanged()");

        AssertContains(audioSelectionText, "public void EnsureAudioInputSelection()");
        AssertContains(audioSelectionText, "public void EnsureMicrophoneSelection()");
        AssertOccursBefore(audioSelectionText, "public void EnsureAudioInputSelection()", "public void EnsureMicrophoneSelection()");

        AssertContains(captureModeSelectionText, "public void EnsureResolutionSelection()");
        AssertContains(captureModeSelectionText, "public void EnsureFrameRateSelection()");
        AssertOccursBefore(captureModeSelectionText, "public void EnsureResolutionSelection()", "public void EnsureFrameRateSelection()");

        AssertContains(recordingSelectionText, "public void EnsureFormatSelection()");
        AssertContains(recordingSelectionText, "public void EnsureQualitySelection()");
        AssertContains(recordingSelectionText, "public void EnsurePresetSelection()");
        AssertContains(recordingSelectionText, "public void EnsureSplitEncodeModeSelection()");
        AssertContains(recordingSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);");
        AssertOccursBefore(recordingSelectionText, "public void EnsureFormatSelection()", "public void EnsureQualitySelection()");
        AssertOccursBefore(recordingSelectionText, "public void EnsureQualitySelection()", "public void EnsurePresetSelection()");
        AssertOccursBefore(recordingSelectionText, "public void EnsurePresetSelection()", "public void EnsureSplitEncodeModeSelection()");

        AssertContains(deviceSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(");
        AssertContains(audioSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(");
        AssertContains(captureModeSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(");
        AssertContains(captureModeSelectionText, "CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "internal static class CaptureComboBoxSelectionNormalizer");
        AssertContains(selectionNormalizerText, "public static CaptureDevice? ResolveCaptureDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static AudioInputDevice? ResolveAudioInputDeviceSelection(");
        AssertContains(selectionNormalizerText, "public static ResolutionOption? ResolveResolutionSelection(");
        AssertContains(selectionNormalizerText, "public static FrameRateOption? ResolveFrameRateSelection(");
        AssertContains(selectionNormalizerText, "public static string? ResolveStringSelection(");
        AssertContains(selectionNormalizerText, "public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertContains(selectionNormalizerText, "public static bool IsAutoFrameRateOption(FrameRateOption option)");

        AssertDoesNotContain(bindingsText, "DeviceComboBox.SelectionChanged +=");
        AssertDoesNotContain(controllerText, "public bool HasPendingDeviceSelection()");
        AssertDoesNotContain(controllerText, "public void UpdateDeviceApplyButtonState()");
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
        AssertDoesNotContain(selectionFamilyText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(selectionFamilyText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(selectionFamilyText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(selectionFamilyText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(selectionFamilyText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(selectionFamilyText, "AvailableFrameRates.FirstOrDefault(option =>");

        return Task.CompletedTask;
    }
}
