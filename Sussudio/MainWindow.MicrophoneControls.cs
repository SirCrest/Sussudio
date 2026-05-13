using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing microphone controls adapter. MicrophoneControlsController owns
// slider synchronization, save triggers, and mic-meter row animation state.
public sealed partial class MainWindow
{
    private MicrophoneControlsController _microphoneControlsController = null!;

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
