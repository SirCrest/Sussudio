using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing audio and microphone presentation adapter. The controllers own
// property routing, control updates, slider synchronization, save triggers, and
// mic-meter row animation state.
public sealed partial class MainWindow
{
    private AudioControlPresentationController _audioControlPresentationController = null!;
    private MicrophoneControlsController _microphoneControlsController = null!;

    private bool TryHandleAudioPropertyChanged(string propertyName)
        => _audioControlPresentationController.TryHandlePropertyChanged(propertyName);

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

    private void InitializeMicrophoneControlsController()
    {
        _microphoneControlsController = new MicrophoneControlsController(new MicrophoneControlsControllerContext
        {
            ViewModel = ViewModel,
            MicVolumeSlider = MicVolumeSlider,
            MicVolumeShelfSlider = MicVolumeShelfSlider,
            MicVolumeLabel = MicVolumeLabel,
            MicMeterRow = MicMeterRow,
            DeviceAudioRowTranslate = DeviceAudioRowTranslate,
            MicMeterRowTranslate = MicMeterRowTranslate,
            ResetMicrophoneMeterVisuals = ResetMicrophoneMeterVisuals,
        });
    }

    private void SetupMicrophoneVolumeBindings()
        => _microphoneControlsController.AttachVolumeBindings();

    private void SyncMicrophoneVolumeControls(double volumePercent)
        => _microphoneControlsController.SyncVolumeControls(volumePercent);

    private void ApplyInitialMicrophoneControlsVisibility()
        => _microphoneControlsController.ApplyInitialVisibility();

    private void UpdateMicrophoneControlsVisibility()
        => _microphoneControlsController.UpdateVisibility();

    private void StopMicMeterRowAnimation()
        => _microphoneControlsController.StopRowAnimation();
}
