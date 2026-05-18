using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing recording-state presentation adapter. RecordingStatePresentationController
// owns the property routing, ViewModel-derived recording lockouts, and record-button chrome delegation.
public sealed partial class MainWindow
{
    private RecordingButtonChromeController _recordingButtonChromeController = null!;
    private RecordingStatePresentationController _recordingStatePresentationController = null!;

    private bool TryHandleRecordingPropertyChanged(string propertyName)
        => _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);

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

    private void ApplyInitialRecordingStatePresentation()
        => _recordingStatePresentationController.HandleFfmpegMissingChanged();
}
