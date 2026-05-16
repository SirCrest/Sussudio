namespace Sussudio.Controllers;

internal sealed partial class AudioControlBindingController
{
    public void AttachAudioRecordPreviewToggleBindings()
    {
        _context.AudioRecordToggle.Checked += (s, e) => _context.ViewModel.IsAudioEnabled = true;
        _context.AudioRecordToggle.Unchecked += (s, e) => _context.ViewModel.IsAudioEnabled = false;
        _context.AudioPreviewToggle.Checked += (s, e) => _context.ViewModel.IsAudioPreviewEnabled = true;
        _context.AudioPreviewToggle.Unchecked += (s, e) => _context.ViewModel.IsAudioPreviewEnabled = false;
    }

    public void AttachAudioInputToggleBindings()
    {
        _context.CustomAudioToggle.Click += (s, e) => _context.ViewModel.IsCustomAudioInputEnabled = _context.CustomAudioToggle.IsChecked == true;
        _context.MicrophoneToggle.Click += (s, e) => _context.ViewModel.IsMicrophoneEnabled = _context.MicrophoneToggle.IsChecked == true;
    }
}
