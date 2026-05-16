using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public bool TryHandlePropertyChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.SelectedDevice):
                HandleSelectedDevicePropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedResolution):
                EnsureResolutionSelection();
                return true;

            case nameof(MainViewModel.SelectedFrameRate):
            case nameof(MainViewModel.IsAutoFrameRateSelected):
                EnsureFrameRateSelection();
                return true;

            case nameof(MainViewModel.AvailableResolutions):
                HandleAvailableResolutionsPropertyChanged();
                return true;

            case nameof(MainViewModel.AvailableFrameRates):
                HandleAvailableFrameRatesPropertyChanged();
                return true;

            case nameof(MainViewModel.IsDeviceAudioControlSupported):
            case nameof(MainViewModel.SelectedDeviceAudioMode):
            case nameof(MainViewModel.AnalogAudioGainPercent):
            case nameof(MainViewModel.AvailableDeviceAudioModes):
                ApplyDeviceAudioControlState();
                return true;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                EnsureAudioInputSelection();
                return true;

            case nameof(MainViewModel.SelectedMicrophoneDevice):
                EnsureMicrophoneSelection();
                return true;

            case nameof(MainViewModel.SelectedRecordingFormat):
                EnsureFormatSelection();
                return true;

            case nameof(MainViewModel.SelectedQuality):
                EnsureQualitySelection();
                return true;

            case nameof(MainViewModel.AvailablePresets):
                HandleAvailablePresetsPropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedPreset):
                EnsurePresetSelection();
                return true;

            case nameof(MainViewModel.AvailableSplitEncodeModes):
                HandleAvailableSplitEncodeModesPropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedSplitEncodeMode):
                EnsureSplitEncodeModeSelection();
                return true;

            default:
                return false;
        }
    }
}
