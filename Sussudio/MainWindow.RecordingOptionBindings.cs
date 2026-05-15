using System;

namespace Sussudio;

// Recording and capture-option event bindings. Initial value projection and
// option visibility stay in MainWindow.Bindings.cs/CaptureOptionPresentation.cs.
public sealed partial class MainWindow
{
    private void AttachRecordingOptionBindings()
    {
        FormatComboBox.SelectionChanged += (s, e) =>
        {
            if (FormatComboBox.SelectedItem is string format)
            {
                ViewModel.SelectedRecordingFormat = format;
            }
        };

        QualityComboBox.SelectionChanged += (s, e) =>
        {
            if (QualityComboBox.SelectedItem is string quality)
            {
                ViewModel.SelectedQuality = quality;
            }
        };

        PresetComboBox.SelectionChanged += (s, e) =>
        {
            if (PresetComboBox.SelectedItem is string preset)
            {
                ViewModel.SelectedPreset = preset;
            }
        };

        SplitEncodeComboBox.SelectionChanged += (s, e) =>
        {
            if (SplitEncodeComboBox.SelectedItem is string splitMode)
            {
                ViewModel.SelectedSplitEncodeMode = splitMode;
            }
        };

        VideoFormatComboBox.SelectionChanged += (s, e) =>
        {
            if (VideoFormatComboBox.SelectedItem is string videoFormat)
            {
                ViewModel.SelectedVideoFormat = videoFormat;
            }

            UpdateDecoderCountVisibility();
        };

        CustomBitrateNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(CustomBitrateNumberBox.Value))
            {
                ViewModel.CustomBitrateMbps = CustomBitrateNumberBox.Value;
            }
        };
        HdrToggle.Click += (s, e) => ViewModel.IsHdrEnabled = HdrToggle.IsChecked == true;
        TrueHdrPreviewToggle.Click += (s, e) =>
            ViewModel.IsTrueHdrPreviewEnabled = TrueHdrPreviewToggle.IsChecked == true;
    }
}
