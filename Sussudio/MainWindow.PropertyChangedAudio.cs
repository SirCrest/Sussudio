using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for audio and microphone property-change projection.
// AudioControlPresentationController owns the concrete control updates.
public sealed partial class MainWindow
{
    private AudioControlPresentationController _audioControlPresentationController = null!;

    private void InitializeAudioControlPresentationController()
    {
        _audioControlPresentationController = new AudioControlPresentationController(new AudioControlPresentationControllerContext
        {
            ViewModel = ViewModel,
            CustomAudioToggle = CustomAudioToggle,
            AudioInputComboBox = AudioInputComboBox,
            MicrophoneToggle = MicrophoneToggle,
            MicrophoneComboBox = MicrophoneComboBox,
            AudioRecordToggle = AudioRecordToggle,
            AudioPreviewToggle = AudioPreviewToggle,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
            IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,
            SetAudioMeterMonitoringState = SetAudioMeterMonitoringState,
            AnimateAudioMeterDisabled = AnimateAudioMeterDisabled,
            UpdateMicrophoneControlsVisibility = UpdateMicrophoneControlsVisibility,
            SyncMicrophoneVolumeControls = SyncMicrophoneVolumeControls
        });
    }

    private void HandleCustomAudioInputEnabledChanged()
        => _audioControlPresentationController.HandleCustomAudioInputEnabledChanged();

    private void HandleMicrophoneEnabledChanged()
        => _audioControlPresentationController.HandleMicrophoneEnabledChanged();

    private void HandleAudioEnabledChanged()
        => _audioControlPresentationController.HandleAudioEnabledChanged();

    private void HandleAudioPreviewEnabledChanged()
        => _audioControlPresentationController.HandleAudioPreviewEnabledChanged();

    private void HandleAudioPreviewActiveChanged()
        => _audioControlPresentationController.HandleAudioPreviewActiveChanged();

    private void HandlePreviewVolumeChanged()
        => _audioControlPresentationController.HandlePreviewVolumeChanged();

    private void HandleMicrophoneVolumeChanged()
        => _audioControlPresentationController.HandleMicrophoneVolumeChanged();
}
