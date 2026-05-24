using System;
using Microsoft.UI.Xaml.Media;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for audio and microphone binding/presentation setup.
// Runtime projection routing, meter smoothing, and microphone row animation
// live with the feature controllers.
public sealed partial class MainWindow
{
    private AudioControlBindingController _audioControlBindingController = null!;
    private AudioControlPresentationController _audioControlPresentationController = null!;
    private AudioMeterController _audioMeterController = null!;
    private MicrophoneControlsController _microphoneControlsController = null!;

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

    private void InitializeAudioMeterBrushes()
    {
        _audioMeterController = new AudioMeterController(new AudioMeterControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            AudioMeterTrack = AudioMeterTrack,
            AudioMeterContent = AudioMeterContent,
            AudioMeterRawFill = AudioMeterRawFill,
            AudioMeterFill = AudioMeterFill,
            AudioMeterRawClip = AudioMeterRawClip,
            AudioMeterColorClip = AudioMeterColorClip,
            AudioPeakHoldIndicator = AudioPeakHoldIndicator,
            AudioPeakHoldTranslate = AudioPeakHoldTranslate,
            AudioRangeMinMarker = AudioRangeMinMarker,
            AudioRangeMinTranslate = AudioRangeMinTranslate,
            AudioRangeMaxMarker = AudioRangeMaxMarker,
            AudioRangeMaxTranslate = AudioRangeMaxTranslate,
            MicMeterTrack = MicMeterTrack,
            MicMeterContent = MicMeterContent,
            MicMeterClip = MicMeterClip,
        });
        _audioMeterController.Initialize();
    }

    private void AnimateAudioMeterTick()
        => _audioMeterController.AnimateTick();

    private void ResetAudioMeterVisuals()
        => _audioMeterController.ResetVisuals();

    private void ResetMicrophoneMeterVisuals()
        => _audioMeterController.ResetMicrophoneVisuals();

    private void SetAudioMeterTargetLevel(double targetLevel)
        => _audioMeterController.SetAudioMeterTargetLevel(targetLevel);

    private void EnsureAudioMeterTimerRunning()
        => _audioMeterController.EnsureTimerRunning();

    private void StopAudioMeterTimer()
        => _audioMeterController.StopTimer();

    private void SetAudioMeterMonitoringState(bool isMonitoring)
        => _audioMeterController.SetMonitoringState(isMonitoring);

    private void AnimateAudioMeterDisabled(bool isDisabled)
        => _audioMeterController.AnimateDisabled(isDisabled);

    private static double TranslateMarker(double trackWidth, double level, double markerWidth)
        => AudioMeterController.TranslateMarker(trackWidth, level, markerWidth);

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
