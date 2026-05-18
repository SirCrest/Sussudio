using System;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    public void InitializeCollections()
    {
        _context.VideoFormatComboBox.ItemsSource = _context.ViewModel.AvailableVideoFormats;
        _context.DecoderCountComboBox.Items.Clear();
        for (var i = 1; i <= 8; i++)
        {
            _context.DecoderCountComboBox.Items.Add(i);
        }
    }

    public void ApplyInitialSelections()
    {
        _context.FormatComboBox.SelectedItem = _context.ViewModel.SelectedRecordingFormat;
        _context.QualityComboBox.SelectedItem = _context.ViewModel.SelectedQuality;
        _context.PresetComboBox.SelectedItem = _context.ViewModel.SelectedPreset;
        _context.SplitEncodeComboBox.SelectedItem = _context.ViewModel.SelectedSplitEncodeMode;
        _context.VideoFormatComboBox.SelectedItem = _context.ViewModel.SelectedVideoFormat;
        _context.ApplyInitialDecoderCountSelection();
        _context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;
        _context.ApplyBitrateVisibility();
        _context.HdrToggle.IsChecked = _context.ViewModel.IsHdrEnabled;
        _context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;
        _context.ShowAllCaptureOptionsToggle.IsChecked = _context.ViewModel.ShowAllCaptureOptions;
        _context.ApplyHdrToggleEnabledState();
    }

    public void EnsureInitialSelections()
    {
        _context.EnsureResolutionSelection();
        _context.EnsureFrameRateSelection();
        _context.EnsureFormatSelection();
        _context.EnsureQualitySelection();
        _context.EnsurePresetSelection();
        _context.EnsureSplitEncodeModeSelection();
        _context.UpdateDecoderCountVisibility();
    }

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

    public void AttachShowAllCaptureOptionsBinding()
    {
        _context.ShowAllCaptureOptionsToggle.Click += (s, e) =>
            _context.ViewModel.ShowAllCaptureOptions = _context.ShowAllCaptureOptionsToggle.IsChecked == true;
    }

    private void AttachHdrToggleBindings()
    {
        _context.HdrToggle.Click += (s, e) =>
            _context.ViewModel.IsHdrEnabled = _context.HdrToggle.IsChecked == true;
        _context.TrueHdrPreviewToggle.Click += (s, e) =>
            _context.ViewModel.IsTrueHdrPreviewEnabled = _context.TrueHdrPreviewToggle.IsChecked == true;
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
