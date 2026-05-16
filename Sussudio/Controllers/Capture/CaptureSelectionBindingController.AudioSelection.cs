namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void EnsureAudioInputSelection()
    {
        if (_context.ViewModel.AudioInputDevices.Count == 0)
        {
            _context.AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(
            _context.ViewModel.AudioInputDevices,
            _context.ViewModel.SelectedAudioInputDevice);
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(_context.ViewModel.SelectedAudioInputDevice, matchingDevice))
        {
            _context.ViewModel.SelectedAudioInputDevice = matchingDevice;
        }

        if (!ReferenceEquals(_context.AudioInputComboBox.SelectedItem, matchingDevice))
        {
            _context.AudioInputComboBox.SelectedItem = matchingDevice;
        }
    }

    public void EnsureMicrophoneSelection()
    {
        if (_context.ViewModel.MicrophoneDevices.Count == 0)
        {
            _context.MicrophoneComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(
            _context.ViewModel.MicrophoneDevices,
            _context.ViewModel.SelectedMicrophoneDevice);
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(_context.ViewModel.SelectedMicrophoneDevice, matchingDevice))
        {
            _context.ViewModel.SelectedMicrophoneDevice = matchingDevice;
        }

        if (!ReferenceEquals(_context.MicrophoneComboBox.SelectedItem, matchingDevice))
        {
            _context.MicrophoneComboBox.SelectedItem = matchingDevice;
        }
    }
}
