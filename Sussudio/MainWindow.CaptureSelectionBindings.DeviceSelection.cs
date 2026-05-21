namespace Sussudio;

public sealed partial class MainWindow
{
    private void AttachDeviceSelectionChangedBinding()
        => _captureSelectionBindingController.AttachDeviceSelectionChangedBinding();

    private void EnsureDeviceSelection()
        => _captureSelectionBindingController.EnsureDeviceSelection();

    private void HandleSelectedDevicePropertyChanged()
        => _captureSelectionBindingController.HandleSelectedDevicePropertyChanged();

    private bool HasPendingDeviceSelection()
        => _captureSelectionBindingController.HasPendingDeviceSelection();

    private void UpdateDeviceApplyButtonState()
        => _captureSelectionBindingController.UpdateDeviceApplyButtonState();
}
