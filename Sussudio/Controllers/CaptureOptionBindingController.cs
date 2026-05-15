using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureOptionBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ComboBox ResolutionComboBox { get; init; }
    public required ComboBox FrameRateComboBox { get; init; }
    public required ComboBox FormatComboBox { get; init; }
    public required ComboBox QualityComboBox { get; init; }
    public required ComboBox PresetComboBox { get; init; }
    public required ComboBox SplitEncodeComboBox { get; init; }
    public required ComboBox VideoFormatComboBox { get; init; }
    public required ComboBox DecoderCountComboBox { get; init; }
    public required NumberBox CustomBitrateNumberBox { get; init; }
    public required ToggleButton HdrToggle { get; init; }
    public required ToggleButton TrueHdrPreviewToggle { get; init; }
    public required Action ApplyInitialDecoderCountSelection { get; init; }
    public required Action ApplyBitrateVisibility { get; init; }
    public required Action ApplyHdrToggleEnabledState { get; init; }
    public required Action UpdateDecoderCountVisibility { get; init; }
    public required Action EnsureResolutionSelection { get; init; }
    public required Action EnsureFrameRateSelection { get; init; }
    public required Action EnsureFormatSelection { get; init; }
    public required Action EnsureQualitySelection { get; init; }
    public required Action EnsurePresetSelection { get; init; }
    public required Action EnsureSplitEncodeModeSelection { get; init; }
    public required Action AttachRecordingStringSelectionBindings { get; init; }
}

internal sealed class CaptureOptionBindingController
{
    private readonly CaptureOptionBindingControllerContext _context;

    public CaptureOptionBindingController(CaptureOptionBindingControllerContext context)
    {
        _context = context;
    }

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
                if (IsAutoFrameRateOption(frameRate))
                {
                    if (!_context.ViewModel.IsAutoFrameRateSelected)
                    {
                        _context.ViewModel.SelectedFrameRate = frameRate.Value;
                    }
                }
                else if (!IsFrameRateMatch(frameRate.Value, _context.ViewModel.SelectedFrameRate))
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

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;
}
