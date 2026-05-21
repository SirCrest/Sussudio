using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingPropertyRouter_LivesInController()
    {
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowCaptureSelectionBindingsAdapterSource();
        var propertyChangesText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.PropertyChanges.cs").Replace("\r\n", "\n");

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
}
