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
        var affordances = BuildAffordances();
        _selectedDecoderCount = affordances.InitialDecoderCount;
        _context.DecoderCountComboBox.SelectedItem = _selectedDecoderCount;
    }

    public void UpdateDecoderCountVisibility()
    {
        var affordances = BuildAffordances();
        _context.DecoderCountPanel.Visibility = ToVisibility(affordances.ShowDecoderCount);
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
        var affordances = BuildAffordances();
        _context.HdrToggle.IsEnabled = affordances.EnableHdrToggle;
        _context.TrueHdrPreviewToggle.IsEnabled = affordances.EnableTrueHdrPreviewToggle;
    }

    public void ApplyBitrateVisibility()
    {
        var affordances = BuildAffordances();
        _context.CustomBitratePanel.Visibility = ToVisibility(affordances.ShowCustomBitrate);
        _context.PresetPanel.Visibility = ToVisibility(affordances.ShowPreset);
    }

    public void ApplyAudioClipVisibility()
    {
        var affordances = BuildAffordances();
        _context.AudioClipText.Visibility = ToVisibility(affordances.ShowAudioClip);
    }

    private CaptureOptionPresentationAffordances BuildAffordances()
        => CaptureOptionPresentationPolicy.Build(BuildPolicyInput());

    private CaptureOptionPresentationInput BuildPolicyInput()
    {
        var selectedFrameRateOption = _context.FrameRateComboBox.SelectedItem as FrameRateOption;
        return new CaptureOptionPresentationInput(
            SelectedVideoFormat: _context.VideoFormatComboBox.SelectedItem as string ?? _context.ViewModel.SelectedVideoFormat,
            SelectedFormatPixelFormat: _context.ViewModel.SelectedFormat?.PixelFormat,
            SelectedFrameRateOptionFriendlyValue: selectedFrameRateOption?.FriendlyValue,
            SelectedFrameRateOptionValue: selectedFrameRateOption?.Value,
            SelectedFrameRateFallback: _context.ViewModel.SelectedFrameRate,
            MjpegDecoderCount: _context.ViewModel.MjpegDecoderCount,
            IsHdrAvailable: _context.ViewModel.IsHdrAvailable,
            IsRecording: _context.ViewModel.IsRecording,
            SourceIsHdr: _context.ViewModel.SourceIsHdr,
            IsHdrEnabled: _context.ViewModel.IsHdrEnabled,
            IsCustomBitrateVisible: _context.ViewModel.IsCustomBitrateVisible,
            AudioClipping: _context.ViewModel.AudioClipping);
    }

    private static Visibility ToVisibility(bool isVisible)
        => isVisible ? Visibility.Visible : Visibility.Collapsed;
}
