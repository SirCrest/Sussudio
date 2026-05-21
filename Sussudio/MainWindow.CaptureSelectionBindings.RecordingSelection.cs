namespace Sussudio;

public sealed partial class MainWindow
{
    private void EnsureFormatSelection()
        => _captureSelectionBindingController.EnsureFormatSelection();

    private void EnsureQualitySelection()
        => _captureSelectionBindingController.EnsureQualitySelection();

    private void EnsurePresetSelection()
        => _captureSelectionBindingController.EnsurePresetSelection();

    private void HandleAvailablePresetsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailablePresetsPropertyChanged();

    private void EnsureSplitEncodeModeSelection()
        => _captureSelectionBindingController.EnsureSplitEncodeModeSelection();

    private void HandleAvailableSplitEncodeModesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableSplitEncodeModesPropertyChanged();
}
