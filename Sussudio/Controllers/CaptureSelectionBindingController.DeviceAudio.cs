using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void EnsureDeviceAudioModeSelection()
    {
        if (_context.ViewModel.AvailableDeviceAudioModes.Count == 0)
        {
            return;
        }

        var selectedMode = _context.ViewModel.SelectedDeviceAudioMode;
        var matchingMode = _context.ViewModel.AvailableDeviceAudioModes.FirstOrDefault(mode =>
            string.Equals(mode, selectedMode, StringComparison.OrdinalIgnoreCase))
            ?? _context.ViewModel.AvailableDeviceAudioModes.FirstOrDefault();
        if (matchingMode == null)
        {
            return;
        }

        if (!string.Equals(_context.ViewModel.SelectedDeviceAudioMode, matchingMode, StringComparison.OrdinalIgnoreCase))
        {
            _context.ViewModel.SelectedDeviceAudioMode = matchingMode;
        }

        var shouldBeOn = string.Equals(matchingMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        if (_context.DeviceAudioModeToggle.IsOn != shouldBeOn)
        {
            _context.DeviceAudioModeToggle.IsOn = shouldBeOn;
        }
    }

    public void ApplyDeviceAudioControlState()
    {
        _context.DeviceAudioControlPanel.Visibility =
            _context.ViewModel.IsDeviceAudioControlSupported ? Visibility.Visible : Visibility.Collapsed;
        EnsureDeviceAudioModeSelection();

        var analogGain = Math.Clamp(_context.ViewModel.AnalogAudioGainPercent, 0.0, 100.0);
        if (Math.Abs(_context.AnalogAudioGainSlider.Value - analogGain) > 0.1)
        {
            _context.AnalogAudioGainSlider.Value = analogGain;
        }

        _context.AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(analogGain)}%";
        var analogModeActive = string.Equals(
            _context.ViewModel.SelectedDeviceAudioMode,
            DeviceAudioMode.Analog,
            StringComparison.OrdinalIgnoreCase);
        _context.AnalogAudioGainPanel.Visibility =
            _context.ViewModel.IsDeviceAudioControlSupported && analogModeActive
                ? Visibility.Visible
                : Visibility.Collapsed;
        _context.AnalogAudioGainSlider.IsEnabled =
            _context.ViewModel.IsDeviceAudioControlSupported &&
            analogModeActive &&
            !_context.ViewModel.IsRecording;
    }
}
