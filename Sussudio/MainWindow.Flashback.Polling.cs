using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private FlashbackPollingController _flashbackPollingController = null!;

    private void InitializeFlashbackPollingController()
    {
        _flashbackPollingController = new FlashbackPollingController(new FlashbackPollingControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            IsWindowClosing = () => _isWindowClosing,
        });
    }

    private void StartFlashbackStatusPolling()
        => _flashbackPollingController.StartStatusPolling();

    private void StopFlashbackStatusPolling()
    {
        _flashbackPollingController.StopStatusPolling();
        StopFlashbackCtiAnchorTimer();
    }

    private void StartFlashbackPlaybackPolling()
        => _flashbackPollingController.StartPlaybackPolling();

    private void StopFlashbackPlaybackPolling()
        => _flashbackPollingController.StopPlaybackPolling();
}
