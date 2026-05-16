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
}
