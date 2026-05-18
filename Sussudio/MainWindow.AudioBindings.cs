using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for audio and microphone binding setup. Runtime
// projection routing lives with the feature controllers.
public sealed partial class MainWindow
{
    private AudioControlBindingController _audioControlBindingController = null!;

    private void InitializeAudioControlBindingController()
    {
        _audioControlBindingController = new AudioControlBindingController(new AudioControlBindingControllerContext
        {
            ViewModel = ViewModel,
            AudioRecordToggle = AudioRecordToggle,
            AudioPreviewToggle = AudioPreviewToggle,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
            CustomAudioToggle = CustomAudioToggle,
            MicrophoneToggle = MicrophoneToggle,
            AudioInputComboBox = AudioInputComboBox,
            MicrophoneComboBox = MicrophoneComboBox,
            DeviceAudioModeToggle = DeviceAudioModeToggle,
            AnalogAudioGainSlider = AnalogAudioGainSlider,
            AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock,
            AudioMeterTrack = AudioMeterTrack,
            MicMeterTrack = MicMeterTrack,
            InitializeAudioMeterBrushes = InitializeAudioMeterBrushes,
            EnsureAudioMeterTimerRunning = EnsureAudioMeterTimerRunning,
            SetAudioMeterMonitoringState = SetAudioMeterMonitoringState,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            IsPreviewAudioFadeInActive = () => IsPreviewAudioFadeInActive,
            IsPreviewAudioFadeAnimationActive = () => IsPreviewAudioFadeAnimationActive,
            CancelPreviewAudioFadeInForUser = CancelPreviewAudioFadeInForUser,
            SetupMicrophoneVolumeBindings = SetupMicrophoneVolumeBindings,
            ApplyInitialMicrophoneControlsVisibility = ApplyInitialMicrophoneControlsVisibility,
            ApplyDeviceAudioControlState = ApplyDeviceAudioControlState,
            ResetAudioMeterVisuals = ResetAudioMeterVisuals,
            SetAudioMeterTargetLevel = SetAudioMeterTargetLevel,
            EnsureAudioInputSelection = EnsureAudioInputSelection,
            EnsureMicrophoneSelection = EnsureMicrophoneSelection,
            EnsureDeviceAudioModeSelection = EnsureDeviceAudioModeSelection,
            AnimateAudioMeterTick = AnimateAudioMeterTick
        });
    }

    private void AttachAudioMeterActivationBindings()
    {
        _audioControlBindingController.AttachAudioMeterActivationBindings();
    }

    private void ApplyInitialAudioControlBindings()
        => _audioControlBindingController.ApplyInitialAudioControlBindings();

    private void ApplyInitialAudioMeterPresentation()
        => _audioControlBindingController.ApplyInitialAudioMeterPresentation();

    private void EnsureAudioControlSelections()
        => _audioControlBindingController.EnsureAudioControlSelections();

    private void AttachAudioSelectionBindings()
        => _audioControlBindingController.AttachAudioSelectionBindings();

    private void AttachAudioRecordPreviewToggleBindings()
        => _audioControlBindingController.AttachAudioRecordPreviewToggleBindings();

    private void AttachAudioInputToggleBindings()
        => _audioControlBindingController.AttachAudioInputToggleBindings();

    private void AttachDeviceAudioGainAndMeterBindings()
        => _audioControlBindingController.AttachDeviceAudioGainAndMeterBindings();
}
