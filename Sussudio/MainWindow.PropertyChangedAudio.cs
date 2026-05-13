namespace Sussudio;

// Audio and microphone UI projections for ViewModel.PropertyChanged. Runtime
// audio state changes still live in MainViewModel and CaptureService.
public sealed partial class MainWindow
{
    private void HandleCustomAudioInputEnabledChanged()
    {
        if ((CustomAudioToggle.IsChecked == true) != ViewModel.IsCustomAudioInputEnabled)
        {
            CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
        }

        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
    }

    private void HandleMicrophoneEnabledChanged()
    {
        if ((MicrophoneToggle.IsChecked == true) != ViewModel.IsMicrophoneEnabled)
        {
            MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
        }

        MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
        UpdateMicrophoneControlsVisibility();
    }

    private void HandleAudioEnabledChanged()
    {
        if (AudioRecordToggle.IsChecked != ViewModel.IsAudioEnabled)
        {
            AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        }

        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        if (!ViewModel.IsAudioEnabled && AudioPreviewToggle.IsChecked == true)
        {
            AudioPreviewToggle.IsChecked = false;
        }

        AnimateAudioMeterDisabled(!ViewModel.IsAudioEnabled);
    }

    private void HandleAudioPreviewEnabledChanged()
    {
        if (AudioPreviewToggle.IsChecked != ViewModel.IsAudioPreviewEnabled)
        {
            AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        }
    }

    private void HandleAudioPreviewActiveChanged()
    {
        SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
    }

    private void HandlePreviewVolumeChanged()
    {
        if (IsPreviewAudioFadeInActive)
        {
            return;
        }

        var volumePct = ViewModel.PreviewVolume * 100;
        if (PreviewVolumeSlider.Value != volumePct)
        {
            PreviewVolumeSlider.Value = volumePct;
        }

        PreviewVolumeLabel.Text = $"{(int)volumePct}%";
    }

    private void HandleMicrophoneVolumeChanged()
    {
        SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);
    }
}
