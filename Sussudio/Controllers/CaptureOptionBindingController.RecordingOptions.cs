using System;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    public void AttachRecordingOptionBindings()
    {
        AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);
        AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);
        AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);
        AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);

        _context.VideoFormatComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.VideoFormatComboBox.SelectedItem is string videoFormat)
            {
                _context.ViewModel.SelectedVideoFormat = videoFormat;
            }

            _context.UpdateDecoderCountVisibility();
        };

        _context.CustomBitrateNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(_context.CustomBitrateNumberBox.Value))
            {
                _context.ViewModel.CustomBitrateMbps = _context.CustomBitrateNumberBox.Value;
            }
        };
        AttachHdrToggleBindings();
    }

    public void HandleCustomBitratePropertyChanged()
    {
        if (double.IsNaN(_context.CustomBitrateNumberBox.Value) ||
            Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01)
        {
            _context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;
        }
    }

    private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)
    {
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string value)
            {
                setVmProp(value);
            }
        };
    }
}
