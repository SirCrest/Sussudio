namespace Sussudio;

public sealed partial class MainWindow
{
    private void EnsureResolutionSelection()
        => _captureSelectionBindingController.EnsureResolutionSelection();

    private void HandleAvailableResolutionsPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableResolutionsPropertyChanged();

    private void EnsureFrameRateSelection()
        => _captureSelectionBindingController.EnsureFrameRateSelection();

    private void HandleAvailableFrameRatesPropertyChanged()
        => _captureSelectionBindingController.HandleAvailableFrameRatesPropertyChanged();
}
