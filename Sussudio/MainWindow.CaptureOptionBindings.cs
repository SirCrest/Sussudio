using System;
using Sussudio.Models;

namespace Sussudio;

// Capture and recording option binding setup. Presentation-only option affordance
// rules stay in MainWindow.CaptureOptionPresentation.cs.
public sealed partial class MainWindow
{
    private void InitializeCaptureOptionCollections()
    {
        VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;
        DecoderCountComboBox.Items.Clear();
        for (var i = 1; i <= 8; i++)
        {
            DecoderCountComboBox.Items.Add(i);
        }
    }

    private void ApplyInitialCaptureOptionSelections()
    {
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        PresetComboBox.SelectedItem = ViewModel.SelectedPreset;
        SplitEncodeComboBox.SelectedItem = ViewModel.SelectedSplitEncodeMode;
        VideoFormatComboBox.SelectedItem = ViewModel.SelectedVideoFormat;
        _selectedDecoderCount = Math.Clamp(ViewModel.MjpegDecoderCount, 1, 8);
        DecoderCountComboBox.SelectedItem = _selectedDecoderCount;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        ApplyBitrateVisibility();
        HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
        TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        ApplyHdrToggleEnabledState();
    }

    private void EnsureInitialCaptureOptionSelections()
    {
        EnsureResolutionSelection();
        EnsureFrameRateSelection();
        EnsureFormatSelection();
        EnsureQualitySelection();
        EnsurePresetSelection();
        EnsureSplitEncodeModeSelection();
        UpdateDecoderCountVisibility();
    }

    private void AttachCaptureModeSelectionBindings()
    {
        ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (ResolutionComboBox.SelectedItem is ResolutionOption resolution &&
                resolution.IsEnabled &&
                !string.Equals(resolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedResolution = resolution.Value;
            }
        };

        FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (FrameRateComboBox.SelectedItem is FrameRateOption frameRate &&
                frameRate.IsEnabled)
            {
                if (IsAutoFrameRateOption(frameRate))
                {
                    if (!ViewModel.IsAutoFrameRateSelected)
                    {
                        ViewModel.SelectedFrameRate = frameRate.Value;
                    }
                }
                else if (!IsFrameRateMatch(frameRate.Value, ViewModel.SelectedFrameRate))
                {
                    ViewModel.SelectedFrameRate = frameRate.Value;
                }
            }

            UpdateDecoderCountVisibility();
        };
    }
}
