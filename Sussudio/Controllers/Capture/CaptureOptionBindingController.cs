using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal static class CaptureOptionPresentationPolicy
{
    internal static CaptureOptionPresentationAffordances Build(CaptureOptionPresentationInput input)
    {
        var selectedFrameRate = ResolveSelectedFrameRate(input);
        var showCustomBitrate = input.IsCustomBitrateVisible;

        return new CaptureOptionPresentationAffordances(
            InitialDecoderCount: Math.Clamp(input.MjpegDecoderCount, 1, 8),
            ShowDecoderCount: ShouldShowDecoderCount(input.SelectedVideoFormat, input.SelectedFormatPixelFormat, selectedFrameRate),
            EnableHdrToggle: input.IsHdrAvailable && !input.IsRecording && input.SourceIsHdr != false,
            EnableTrueHdrPreviewToggle: input.IsHdrEnabled && !input.IsRecording,
            ShowCustomBitrate: showCustomBitrate,
            ShowPreset: !showCustomBitrate,
            ShowAudioClip: input.AudioClipping);
    }

    private static double ResolveSelectedFrameRate(CaptureOptionPresentationInput input)
    {
        if (input.SelectedFrameRateOptionFriendlyValue is > 0)
        {
            return input.SelectedFrameRateOptionFriendlyValue.Value;
        }

        if (input.SelectedFrameRateOptionValue is > 0)
        {
            return input.SelectedFrameRateOptionValue.Value;
        }

        return input.SelectedFrameRateFallback;
    }

    private static bool ShouldShowDecoderCount(string? selectedVideoFormat, string? selectedFormatPixelFormat, double selectedFrameRate)
    {
        // Show decoder count when MJPG is explicitly selected, or when Auto resolves
        // to a device-native MJPG format at high frame rates.
        var isExplicitMjpg = string.Equals(selectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var isAutoWithMjpgDevice = string.Equals(selectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(selectedFormatPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        return (isExplicitMjpg || isAutoWithMjpgDevice) && selectedFrameRate >= 90;
    }
}

internal readonly record struct CaptureOptionPresentationInput(
    string? SelectedVideoFormat,
    string? SelectedFormatPixelFormat,
    double? SelectedFrameRateOptionFriendlyValue,
    double? SelectedFrameRateOptionValue,
    double SelectedFrameRateFallback,
    int MjpegDecoderCount,
    bool IsHdrAvailable,
    bool IsRecording,
    bool? SourceIsHdr,
    bool IsHdrEnabled,
    bool IsCustomBitrateVisible,
    bool AudioClipping);

internal readonly record struct CaptureOptionPresentationAffordances(
    int InitialDecoderCount,
    bool ShowDecoderCount,
    bool EnableHdrToggle,
    bool EnableTrueHdrPreviewToggle,
    bool ShowCustomBitrate,
    bool ShowPreset,
    bool ShowAudioClip);

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
    public required Action ApplyAudioClipVisibility { get; init; }
    public required Action RefreshHdrHintText { get; init; }
    public required Action UpdateFpsTelemetryTooltip { get; init; }
    public required Action UpdateVideoContentOverlays { get; init; }
    public required Action<bool> SetHdrPassthroughEnabled { get; init; }
    public required Action UpdateDecoderCountVisibility { get; init; }
    public required Action EnsureResolutionSelection { get; init; }
    public required Action EnsureFrameRateSelection { get; init; }
    public required Action EnsureFormatSelection { get; init; }
    public required Action EnsureQualitySelection { get; init; }
    public required Action EnsurePresetSelection { get; init; }
    public required Action EnsureSplitEncodeModeSelection { get; init; }
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

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.AudioClipping):
                _context.ApplyAudioClipVisibility();
                return true;

            case nameof(MainViewModel.IsHdrAvailable):
            case nameof(MainViewModel.SourceIsHdr):
                _context.ApplyHdrToggleEnabledState();
                return true;

            case nameof(MainViewModel.IsHdrEnabled):
                HandleHdrEnabledChanged();
                return true;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                HandleTrueHdrPreviewEnabledChanged();
                return true;

            case nameof(MainViewModel.HdrResolutionSupportHint):
            case nameof(MainViewModel.HdrReadinessReason):
            case nameof(MainViewModel.HdrRuntimeState):
                _context.RefreshHdrHintText();
                return true;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                _context.UpdateFpsTelemetryTooltip();
                return true;

            case nameof(MainViewModel.SourceWidth):
            case nameof(MainViewModel.SourceHeight):
                _context.UpdateVideoContentOverlays();
                return true;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                _context.ApplyBitrateVisibility();
                return true;

            case nameof(MainViewModel.CustomBitrateMbps):
                HandleCustomBitratePropertyChanged();
                return true;

            default:
                return false;
        }
    }

    public void HandleCustomBitratePropertyChanged()
    {
        if (double.IsNaN(_context.CustomBitrateNumberBox.Value) ||
            Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01)
        {
            _context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;
        }
    }

    public void HandleHdrEnabledChanged()
    {
        if (_context.HdrToggle.IsChecked != _context.ViewModel.IsHdrEnabled)
        {
            _context.HdrToggle.IsChecked = _context.ViewModel.IsHdrEnabled;
        }

        _context.ApplyHdrToggleEnabledState();
    }

    public void HandleTrueHdrPreviewEnabledChanged()
    {
        if (_context.TrueHdrPreviewToggle.IsChecked != _context.ViewModel.IsTrueHdrPreviewEnabled)
        {
            _context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;
        }

        _context.SetHdrPassthroughEnabled(_context.ViewModel.IsTrueHdrPreviewEnabled);
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

internal static class CaptureOptionTooltipFormatter
{
    public static string? BuildHdrHintText(string? resolutionHint, string? readinessHint, bool isRecording)
    {
        resolutionHint = resolutionHint?.Trim();
        readinessHint = readinessHint?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (isRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }

        return string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint;
    }

    public static string? BuildFpsTelemetryTooltip(string? sourceTelemetrySummaryText, string? sourceTargetSummaryText)
    {
        if (string.IsNullOrWhiteSpace(sourceTelemetrySummaryText))
        {
            return string.IsNullOrWhiteSpace(sourceTargetSummaryText) ? null : sourceTargetSummaryText;
        }

        if (string.IsNullOrWhiteSpace(sourceTargetSummaryText))
        {
            return sourceTelemetrySummaryText;
        }

        return $"{sourceTelemetrySummaryText}{Environment.NewLine}{sourceTargetSummaryText}";
    }
}
