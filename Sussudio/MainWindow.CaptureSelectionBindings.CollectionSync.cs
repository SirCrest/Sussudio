namespace Sussudio;

public sealed partial class MainWindow
{
    private void AttachCaptureSelectionBindings()
        => _captureSelectionBindingController.AttachCollectionBindings();
}
