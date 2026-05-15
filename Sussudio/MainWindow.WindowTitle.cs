using Sussudio.Controllers;

namespace Sussudio;

// Window title presentation: base build stamp plus recording timer suffix.
public sealed partial class MainWindow
{
    private WindowTitleController _windowTitleController = null!;

    private void InitializeWindowTitleController()
        => _windowTitleController = new WindowTitleController();

    private void ApplyWindowTitle()
        => Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);
}
