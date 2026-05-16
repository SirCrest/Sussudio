using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class AudioControlBindingController
{
    public void EnsureAudioControlSelections()
    {
        _context.EnsureAudioInputSelection();
        _context.EnsureMicrophoneSelection();
        _context.EnsureDeviceAudioModeSelection();
    }

    public void AttachAudioSelectionBindings()
    {
        _context.AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.AudioInputComboBox.SelectedItem is AudioInputDevice device &&
                device != _context.ViewModel.SelectedAudioInputDevice)
            {
                _context.ViewModel.SelectedAudioInputDevice = device;
            }
        };

        _context.MicrophoneComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.MicrophoneComboBox.SelectedItem is AudioInputDevice device &&
                device != _context.ViewModel.SelectedMicrophoneDevice)
            {
                _context.ViewModel.SelectedMicrophoneDevice = device;
            }
        };

        _context.DeviceAudioModeToggle.Toggled += (s, e) =>
        {
            var mode = _context.DeviceAudioModeToggle.IsOn ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
            if (!string.Equals(mode, _context.ViewModel.SelectedDeviceAudioMode, StringComparison.OrdinalIgnoreCase))
            {
                _context.ViewModel.SelectedDeviceAudioMode = mode;
            }
        };
    }
}
