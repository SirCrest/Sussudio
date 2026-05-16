namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void EnsureFormatSelection()
    {
        if (_context.ViewModel.AvailableRecordingFormats.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.FormatComboBox.SelectedItem = null;
            }

            return;
        }

        ApplyStringComboBoxSelection(
            _context.FormatComboBox,
            _context.ViewModel.AvailableRecordingFormats,
            () => _context.ViewModel.SelectedRecordingFormat,
            value => _context.ViewModel.SelectedRecordingFormat = value);
    }

    public void EnsureQualitySelection() =>
        ApplyStringComboBoxSelection(
            _context.QualityComboBox,
            _context.ViewModel.AvailableQualities,
            () => _context.ViewModel.SelectedQuality,
            value => _context.ViewModel.SelectedQuality = value);

    public void EnsurePresetSelection() =>
        ApplyStringComboBoxSelection(
            _context.PresetComboBox,
            _context.ViewModel.AvailablePresets,
            () => _context.ViewModel.SelectedPreset,
            value => _context.ViewModel.SelectedPreset = value);

    public void EnsureSplitEncodeModeSelection() =>
        ApplyStringComboBoxSelection(
            _context.SplitEncodeComboBox,
            _context.ViewModel.AvailableSplitEncodeModes,
            () => _context.ViewModel.SelectedSplitEncodeMode,
            value => _context.ViewModel.SelectedSplitEncodeMode = value);
}
