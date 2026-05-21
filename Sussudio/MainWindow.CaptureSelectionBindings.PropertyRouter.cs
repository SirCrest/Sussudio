namespace Sussudio;

public sealed partial class MainWindow
{
    private bool TryHandleCaptureSelectionPropertyChanged(string? propertyName)
        => _captureSelectionBindingController.TryHandlePropertyChanged(propertyName);
}
