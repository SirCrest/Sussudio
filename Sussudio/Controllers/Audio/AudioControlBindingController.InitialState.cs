namespace Sussudio.Controllers;

internal sealed partial class AudioControlBindingController
{
    public void ApplyInitialAudioControlBindings()
    {
        _context.AudioRecordToggle.IsChecked = _context.ViewModel.IsAudioEnabled;
        _context.AudioPreviewToggle.IsChecked = _context.ViewModel.IsAudioPreviewEnabled;
        _context.AudioPreviewToggle.IsEnabled = _context.ViewModel.IsAudioEnabled;
        _context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);
        // Save the user's preferred volume, start at 0 for hidden audio priming.
        _context.PrimePreviewAudioFadeIn();
        _context.PreviewVolumeSlider.ValueChanged += (s, e) =>
        {
            _context.ViewModel.PreviewVolume = e.NewValue / 100.0;
            _context.PreviewVolumeLabel.Text = $"{(int)e.NewValue}%";
        };
        _context.PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
        {
            if (_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive())
            {
                // User explicitly grabbed the slider during a preview volume fade.
                // Pause the volume animation so it doesn't overwrite their choice
                // (Stop() would snap properties back to base values).
                _context.CancelPreviewAudioFadeInForUser();
            }

            _context.ViewModel.SavePreviewVolume();
        };
        _context.SetupMicrophoneVolumeBindings();
        _context.CustomAudioToggle.IsChecked = _context.ViewModel.IsCustomAudioInputEnabled;
        _context.CustomAudioToggle.IsEnabled = !_context.ViewModel.IsRecording;
        _context.MicrophoneToggle.IsChecked = _context.ViewModel.IsMicrophoneEnabled;
        _context.MicrophoneToggle.IsEnabled = !_context.ViewModel.IsRecording;
        _context.AudioInputComboBox.IsEnabled = _context.ViewModel.IsCustomAudioInputEnabled && !_context.ViewModel.IsRecording;
        _context.AudioInputComboBox.SelectedItem = _context.ViewModel.SelectedAudioInputDevice;
        _context.MicrophoneComboBox.IsEnabled = _context.ViewModel.IsMicrophoneEnabled && !_context.ViewModel.IsRecording;
        _context.MicrophoneComboBox.SelectedItem = _context.ViewModel.SelectedMicrophoneDevice;
        _context.ApplyInitialMicrophoneControlsVisibility();
        _context.ApplyDeviceAudioControlState();
    }
}
