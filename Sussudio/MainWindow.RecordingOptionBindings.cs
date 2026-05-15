namespace Sussudio;

// XAML-facing adapter for recording/capture option event bindings.
public sealed partial class MainWindow
{
    private void AttachRecordingOptionBindings()
        => _captureOptionBindingController.AttachRecordingOptionBindings();
}
