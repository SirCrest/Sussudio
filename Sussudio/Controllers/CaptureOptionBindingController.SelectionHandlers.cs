using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    public void AttachCaptureModeSelectionBindings()
    {
        _context.ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.ResolutionComboBox.SelectedItem is ResolutionOption resolution &&
                resolution.IsEnabled &&
                !string.Equals(resolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            {
                _context.ViewModel.SelectedResolution = resolution.Value;
            }
        };

        _context.FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.FrameRateComboBox.SelectedItem is FrameRateOption frameRate &&
                frameRate.IsEnabled)
            {
                if (CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(frameRate))
                {
                    if (!_context.ViewModel.IsAutoFrameRateSelected)
                    {
                        _context.ViewModel.SelectedFrameRate = frameRate.Value;
                    }
                }
                else if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))
                {
                    _context.ViewModel.SelectedFrameRate = frameRate.Value;
                }
            }

            _context.UpdateDecoderCountVisibility();
        };
    }

    public void AttachRecordingOptionBindings()
    {
        _context.AttachRecordingStringSelectionBindings();

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
        _context.HdrToggle.Click += (s, e) => _context.ViewModel.IsHdrEnabled = _context.HdrToggle.IsChecked == true;
        _context.TrueHdrPreviewToggle.Click += (s, e) =>
            _context.ViewModel.IsTrueHdrPreviewEnabled = _context.TrueHdrPreviewToggle.IsChecked == true;
    }

    public void HandleCustomBitratePropertyChanged()
    {
        if (double.IsNaN(_context.CustomBitrateNumberBox.Value) ||
            Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01)
        {
            _context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;
        }
    }

}
