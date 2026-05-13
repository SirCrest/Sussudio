using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;

namespace Sussudio;

// Presentation rules for capture option affordances: HDR readiness hints,
// FPS telemetry hints, MJPEG decoder visibility, bitrate mode, and audio clip
// status.
public sealed partial class MainWindow
{
    private int _selectedDecoderCount = 4;

    private void UpdateDecoderCountVisibility()
    {
        var selectedFormat = VideoFormatComboBox.SelectedItem as string ?? ViewModel.SelectedVideoFormat;
        var selectedFrameRate = GetSelectedFriendlyFrameRate();

        // Show decoder count when MJPG is explicitly selected, OR when auto
        // resolves to a format that would use the parallel MJPEG pipeline
        // (i.e. the device's native format is MJPG at high frame rates).
        var isExplicitMjpg = string.Equals(selectedFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var isAutoWithMjpgDevice = string.Equals(selectedFormat, "Auto", StringComparison.OrdinalIgnoreCase)
            && string.Equals(ViewModel.SelectedFormat?.PixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        DecoderCountPanel.Visibility =
            (isExplicitMjpg || isAutoWithMjpgDevice) && selectedFrameRate >= 90
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private double GetSelectedFriendlyFrameRate()
    {
        if (FrameRateComboBox.SelectedItem is FrameRateOption option)
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

        return ViewModel.SelectedFrameRate;
    }

    private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DecoderCountComboBox.SelectedItem is int count)
        {
            _selectedDecoderCount = count;
            if (ViewModel.MjpegDecoderCount != count)
            {
                ViewModel.MjpegDecoderCount = count;
            }
        }
    }

    private void RefreshHdrHintText()
    {
        var resolutionHint = ViewModel.HdrResolutionSupportHint?.Trim();
        var readinessHint = ViewModel.HdrReadinessReason?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (ViewModel.IsRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }

        ToolTipService.SetToolTip(HdrToggle,
            string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint);
    }

    private void UpdateFpsTelemetryTooltip()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText))
        {
            parts.Add(ViewModel.SourceTelemetrySummaryText);
        }

        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText))
        {
            parts.Add(ViewModel.SourceTargetSummaryText);
        }

        ToolTipService.SetToolTip(FrameRateComboBox,
            parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null);
    }

    private void ApplyHdrToggleEnabledState()
    {
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                              !ViewModel.IsRecording &&
                              ViewModel.SourceIsHdr != false;
        TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
    }

    private void ApplyBitrateVisibility()
    {
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        PresetPanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyAudioClipVisibility()
    {
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
    }
}
