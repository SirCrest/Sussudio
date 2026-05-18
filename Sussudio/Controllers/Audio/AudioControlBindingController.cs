using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class AudioControlBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleButton AudioRecordToggle { get; init; }
    public required ToggleButton AudioPreviewToggle { get; init; }
    public required Slider PreviewVolumeSlider { get; init; }
    public required TextBlock PreviewVolumeLabel { get; init; }
    public required CheckBox CustomAudioToggle { get; init; }
    public required CheckBox MicrophoneToggle { get; init; }
    public required ComboBox AudioInputComboBox { get; init; }
    public required ComboBox MicrophoneComboBox { get; init; }
    public required ToggleSwitch DeviceAudioModeToggle { get; init; }
    public required Slider AnalogAudioGainSlider { get; init; }
    public required TextBlock AnalogAudioGainValueTextBlock { get; init; }
    public required FrameworkElement AudioMeterTrack { get; init; }
    public required FrameworkElement MicMeterTrack { get; init; }
    public required Action InitializeAudioMeterBrushes { get; init; }
    public required Action EnsureAudioMeterTimerRunning { get; init; }
    public required Action<bool> SetAudioMeterMonitoringState { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<bool> IsPreviewAudioFadeInActive { get; init; }
    public required Func<bool> IsPreviewAudioFadeAnimationActive { get; init; }
    public required Action CancelPreviewAudioFadeInForUser { get; init; }
    public required Action SetupMicrophoneVolumeBindings { get; init; }
    public required Action ApplyInitialMicrophoneControlsVisibility { get; init; }
    public required Action ApplyDeviceAudioControlState { get; init; }
    public required Action ResetAudioMeterVisuals { get; init; }
    public required Action<double> SetAudioMeterTargetLevel { get; init; }
    public required Action EnsureAudioInputSelection { get; init; }
    public required Action EnsureMicrophoneSelection { get; init; }
    public required Action EnsureDeviceAudioModeSelection { get; init; }
    public required Action AnimateAudioMeterTick { get; init; }
}

internal sealed partial class AudioControlBindingController
{
    private readonly AudioControlBindingControllerContext _context;

    public AudioControlBindingController(AudioControlBindingControllerContext context)
    {
        _context = context;
    }
}
