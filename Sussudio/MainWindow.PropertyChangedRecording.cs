using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing recording-state presentation adapter. RecordingStatePresentationController
// owns ViewModel-derived recording lockouts and delegates record-button chrome.
public sealed partial class MainWindow
{
    private RecordingButtonChromeController _recordingButtonChromeController = null!;
    private RecordingStatePresentationController _recordingStatePresentationController = null!;

    private bool TryHandleRecordingPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsRecording):
                HandleRecordingChanged();
                return true;

            case nameof(MainViewModel.IsRecordingTransitioning):
                HandleRecordingTransitioningChanged();
                return true;

            case nameof(MainViewModel.IsFfmpegMissing):
                HandleFfmpegMissingChanged();
                return true;

            default:
                return false;
        }
    }

    private void InitializeRecordingButtonChromeController()
    {
        _recordingButtonChromeController = new RecordingButtonChromeController(new RecordingButtonChromeControllerContext
        {
            RecordingGlowBorder = RecordingGlowBorder,
            RecordingGlowPulseStoryboard = RecordingGlowPulseStoryboard,
            RecPulseStoryboard = RecPulseStoryboard,
            RecordButton = RecordButton,
            RecordButtonNormalContent = RecordButtonNormalContent,
            RecordButtonStartingContent = RecordButtonStartingContent,
            RecordButtonRecordingContent = RecordButtonRecordingContent,
        });
    }

    private void InitializeRecordingStatePresentationController()
    {
        _recordingStatePresentationController = new RecordingStatePresentationController(new RecordingStatePresentationControllerContext
        {
            ViewModel = ViewModel,
            RecordingButtonChrome = _recordingButtonChromeController,
            AudioRecordToggle = AudioRecordToggle,
            CustomAudioToggle = CustomAudioToggle,
            MicrophoneToggle = MicrophoneToggle,
            AudioInputComboBox = AudioInputComboBox,
            MicrophoneComboBox = MicrophoneComboBox,
            DeviceAudioModeToggle = DeviceAudioModeToggle,
            AnalogAudioGainSlider = AnalogAudioGainSlider,
            ResetAudioMeterVisuals = ResetAudioMeterVisuals,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            RefreshHdrHintText = RefreshHdrHintText,
            UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
            ApplyWindowTitle = ApplyWindowTitle,
        });
    }

    private void HandleRecordingChanged()
        => _recordingStatePresentationController.HandleRecordingChanged();

    private void HandleRecordingTransitioningChanged()
        => _recordingStatePresentationController.HandleRecordingTransitioningChanged();

    private void HandleFfmpegMissingChanged()
        => _recordingStatePresentationController.HandleFfmpegMissingChanged();
}
