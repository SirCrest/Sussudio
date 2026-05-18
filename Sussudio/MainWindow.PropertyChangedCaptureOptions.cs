namespace Sussudio;

// XAML-facing adapter for capture-option and source-signal property changes.
public sealed partial class MainWindow
{
    private bool TryHandleCaptureOptionPropertyChanged(string propertyName)
        => _captureOptionBindingController.TryHandlePropertyChanged(propertyName);
}
