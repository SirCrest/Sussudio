using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureOptionPresentationControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ComboBox VideoFormatComboBox { get; init; }
    public required ComboBox FrameRateComboBox { get; init; }
    public required FrameworkElement DecoderCountPanel { get; init; }
    public required ComboBox DecoderCountComboBox { get; init; }
    public required ToggleButton HdrToggle { get; init; }
    public required ToggleButton TrueHdrPreviewToggle { get; init; }
    public required FrameworkElement CustomBitratePanel { get; init; }
    public required FrameworkElement PresetPanel { get; init; }
    public required FrameworkElement AudioClipText { get; init; }
}

internal sealed class CaptureOptionPresentationController
{
    private readonly CaptureOptionPresentationControllerContext _context;
    private int _selectedDecoderCount = 4;

    public CaptureOptionPresentationController(CaptureOptionPresentationControllerContext context)
    {
        _context = context;
    }

    public void ApplyInitialDecoderCountSelection()
    {
        _selectedDecoderCount = Math.Clamp(_context.ViewModel.MjpegDecoderCount, 1, 8);
        _context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;
    }

    public void UpdateDecoderCountVisibility()
    {
        var selectedFormat = _context.VideoFormatComboBox.SelectedItem as string ?? _context.ViewModel.SelectedVideoFormat;
        var selectedFrameRate = GetSelectedFriendlyFrameRate();

        // Show decoder count when MJPG is explicitly selected, OR when auto
        // resolves to a format that would use the parallel MJPEG pipeline
        // (i.e. the device's native format is MJPG at high frame rates).
        var isExplicitMjpg = string.Equals(selectedFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var isAutoWithMjpgDevice = string.Equals(selectedFormat, "Auto", StringComparison.OrdinalIgnoreCase)
            && string.Equals(_context.ViewModel.SelectedFormat?.PixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        _context.DecoderCountPanel.Visibility =
            (isExplicitMjpg || isAutoWithMjpgDevice) && selectedFrameRate >= 90
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    public void HandleDecoderCountSelectionChanged()
    {
        if (_context.DecoderCountComboBox.SelectedItem is int count)
        {
            _selectedDecoderCount = count;
            if (_context.ViewModel.MjpegDecoderCount != count)
            {
                _context.ViewModel.MjpegDecoderCount = count;
            }
        }
    }

    public void RefreshHdrHintText()
    {
        var tooltip = CaptureOptionTooltipFormatter.BuildHdrHintText(
            _context.ViewModel.HdrResolutionSupportHint,
            _context.ViewModel.HdrReadinessReason,
            _context.ViewModel.IsRecording);
        ToolTipService.SetToolTip(_context.HdrToggle, tooltip);
    }

    public void UpdateFpsTelemetryTooltip()
    {
        var tooltip = CaptureOptionTooltipFormatter.BuildFpsTelemetryTooltip(
            _context.ViewModel.SourceTelemetrySummaryText,
            _context.ViewModel.SourceTargetSummaryText);
        ToolTipService.SetToolTip(_context.FrameRateComboBox, tooltip);
    }

    public void ApplyHdrToggleEnabledState()
    {
        _context.HdrToggle.IsEnabled = _context.ViewModel.IsHdrAvailable &&
                                       !_context.ViewModel.IsRecording &&
                                       _context.ViewModel.SourceIsHdr != false;
        _context.TrueHdrPreviewToggle.IsEnabled = _context.ViewModel.IsHdrEnabled && !_context.ViewModel.IsRecording;
    }

    public void ApplyBitrateVisibility()
    {
        _context.CustomBitratePanel.Visibility = _context.ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        _context.PresetPanel.Visibility = _context.ViewModel.IsCustomBitrateVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ApplyAudioClipVisibility()
    {
        _context.AudioClipText.Visibility = _context.ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
    }

    private double GetSelectedFriendlyFrameRate()
    {
        if (_context.FrameRateComboBox.SelectedItem is FrameRateOption option)
        {
            if (option.FriendlyValue > 0)
            {
                return option.FriendlyValue;
            }

            if (option.Value > 0)
            {
                return option.Value;
            }
        }

        return _context.ViewModel.SelectedFrameRate;
    }
}
