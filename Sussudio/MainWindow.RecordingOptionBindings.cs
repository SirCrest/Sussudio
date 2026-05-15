namespace Sussudio;

// Recording and capture-option event bindings. Initial value projection and
// option visibility stay in CaptureOptionBindings.cs/CaptureOptionPresentation.cs.
public sealed partial class MainWindow
{
    private void AttachRecordingOptionBindings()
    {
        AttachRecordingStringSelectionBindings();

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
