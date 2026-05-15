using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for capture, audio, microphone, and encoder selection
// synchronization. Debounced collection-change logic lives in the controller.
public sealed partial class MainWindow
{
    private CaptureSelectionBindingController _captureSelectionBindingController = null!;

    private void InitializeCaptureSelectionBindingController()
    {
        _captureSelectionBindingController = new CaptureSelectionBindingController(
            new CaptureSelectionBindingControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                DeviceComboBox = DeviceComboBox,
                AudioInputComboBox = AudioInputComboBox,
                MicrophoneComboBox = MicrophoneComboBox,
                ResolutionComboBox = ResolutionComboBox,
                FrameRateComboBox = FrameRateComboBox,
                FormatComboBox = FormatComboBox,
                QualityComboBox = QualityComboBox,
                PresetComboBox = PresetComboBox,
                SplitEncodeComboBox = SplitEncodeComboBox,
                ApplyDeviceButton = ApplyDeviceButton,
                DeviceAudioControlPanel = DeviceAudioControlPanel,
                DeviceAudioModeToggle = DeviceAudioModeToggle,
                AnalogAudioGainPanel = AnalogAudioGainPanel,
                AnalogAudioGainSlider = AnalogAudioGainSlider,
                AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock
            });
    }

    private void AttachCaptureSelectionBindings()
        => _captureSelectionBindingController.AttachCollectionBindings();

    private void AttachRecordingStringSelectionBindings()
        => _captureSelectionBindingController.AttachRecordingStringSelectionBindings();

    private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)
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

    private void EnsureDeviceSelection()
        => _captureSelectionBindingController.EnsureDeviceSelection();

    private void HandleSelectedDevicePropertyChanged()
        => _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();

    private void EnsureAudioInputSelection()
        => _captureSelectionBindingController.EnsureAudioInputSelection();

    private void EnsureMicrophoneSelection()
        => _captureSelectionBindingController.EnsureMicrophoneSelection();

    private void EnsureDeviceAudioModeSelection()
        => _captureSelectionBindingController.EnsureDeviceAudioModeSelection();

    private void ApplyDeviceAudioControlState()
        => _captureSelectionBindingController.ApplyDeviceAudioControlState();

    private void EnsureResolutionSelection()
        => _captureSelectionBindingController.EnsureResolutionSelection();

    private void HandleAvailableResolutionsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();

    private void EnsureFrameRateSelection()
        => _captureSelectionBindingController.EnsureFrameRateSelection();

    private void HandleAvailableFrameRatesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();

    private void EnsureFormatSelection()
        => _captureSelectionBindingController.EnsureFormatSelection();

    private void EnsureQualitySelection()
        => _captureSelectionBindingController.EnsureQualitySelection();

    private void EnsurePresetSelection()
        => _captureSelectionBindingController.EnsurePresetSelection();

    private void HandleAvailablePresetsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();

    private void EnsureSplitEncodeModeSelection()
        => _captureSelectionBindingController.EnsureSplitEncodeModeSelection();

    private void HandleAvailableSplitEncodeModesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();

    private bool HasPendingDeviceSelection()
        => _captureSelectionBindingController.HasPendingDeviceSelection();

    private void UpdateDeviceApplyButtonState()
        => _captureSelectionBindingController.UpdateDeviceApplyButtonState();
}
