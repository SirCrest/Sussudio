using System;
using Sussudio.Models;

namespace Sussudio;

// Audio and microphone control binding setup. Runtime projection remains in
// MainWindow.PropertyChangedAudio.cs and the feature controllers.
public sealed partial class MainWindow
{
    private void AttachAudioMeterActivationBindings()
    {
        InitializeAudioMeterBrushes();
        ViewModel.AudioMeterActivated += EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated += EnsureAudioMeterTimerRunning;
    }

    private void ApplyInitialAudioControlBindings()
    {
        AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
        // Save the user's preferred volume, start at 0 for hidden audio priming.
        PrimePreviewAudioFadeIn();
        PreviewVolumeSlider.ValueChanged += (s, e) =>
        {
            ViewModel.PreviewVolume = e.NewValue / 100.0;
            PreviewVolumeLabel.Text = $"{(int)e.NewValue}%";
        };
        PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
        {
            if (IsPreviewAudioFadeInActive || IsPreviewAudioFadeAnimationActive)
            {
                // User explicitly grabbed the slider during a preview volume fade.
                // Pause the volume animation so it doesn't overwrite their choice
                // (Stop() would snap properties back to base values).
                CancelPreviewAudioFadeInForUser();
            }

            ViewModel.SavePreviewVolume();
        };
        SetupMicrophoneVolumeBindings();
        CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
        MicrophoneToggle.IsEnabled = !ViewModel.IsRecording;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
        MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
        MicrophoneComboBox.SelectedItem = ViewModel.SelectedMicrophoneDevice;
        ApplyInitialMicrophoneControlsVisibility();
        ApplyDeviceAudioControlState();
    }

    private void ApplyInitialAudioMeterPresentation()
    {
        ResetAudioMeterVisuals();
        SetAudioMeterTargetLevel(ViewModel.AudioMeterTarget);
    }

    private void EnsureAudioControlSelections()
    {
        EnsureAudioInputSelection();
        EnsureMicrophoneSelection();
        EnsureDeviceAudioModeSelection();
    }

    private void AttachAudioSelectionBindings()
    {
        AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (AudioInputComboBox.SelectedItem is AudioInputDevice device &&
                device != ViewModel.SelectedAudioInputDevice)
            {
                ViewModel.SelectedAudioInputDevice = device;
            }
        };

        MicrophoneComboBox.SelectionChanged += (s, e) =>
        {
            if (MicrophoneComboBox.SelectedItem is AudioInputDevice device &&
                device != ViewModel.SelectedMicrophoneDevice)
            {
                ViewModel.SelectedMicrophoneDevice = device;
            }
        };

        DeviceAudioModeToggle.Toggled += (s, e) =>
        {
            var mode = DeviceAudioModeToggle.IsOn ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
            if (!string.Equals(mode, ViewModel.SelectedDeviceAudioMode, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedDeviceAudioMode = mode;
            }
        };
    }

    private void AttachAudioRecordPreviewToggleBindings()
    {
        AudioRecordToggle.Checked += (s, e) => ViewModel.IsAudioEnabled = true;
        AudioRecordToggle.Unchecked += (s, e) => ViewModel.IsAudioEnabled = false;
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
    }

    private void AttachAudioInputToggleBindings()
    {
        CustomAudioToggle.Click += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsChecked == true;
        MicrophoneToggle.Click += (s, e) => ViewModel.IsMicrophoneEnabled = MicrophoneToggle.IsChecked == true;
    }

    private void AttachDeviceAudioGainAndMeterBindings()
    {
        AnalogAudioGainSlider.ValueChanged += (s, e) =>
        {
            ViewModel.AnalogAudioGainPercent = e.NewValue;
            AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(e.NewValue)}%";
        };
        AudioMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        MicMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
    }
}
