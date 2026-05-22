using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var selectionNormalizerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureComboBoxSelectionNormalizer.cs").Replace("\r\n", "\n");

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
        AssertDoesNotContain(controllerText, "private static void EnsureStringComboBoxSelection(");
        AssertDoesNotContain(controllerText, "private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)");
        AssertDoesNotContain(controllerText, "private static bool IsAutoFrameRateOption(FrameRateOption option)");
        AssertDoesNotContain(controllerText, "items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))");
        AssertDoesNotContain(controllerText, "AvailableResolutions.FirstOrDefault(option =>");
        AssertDoesNotContain(controllerText, "AvailableFrameRates.FirstOrDefault(option =>");

        return Task.CompletedTask;
    }
}
