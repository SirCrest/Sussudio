using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for Flashback playback button and floating-label state.
public sealed partial class MainWindow
{
    private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;

    private void InitializeFlashbackPlaybackPresentationController()
    {
        _flashbackPlaybackPresentationController = new FlashbackPlaybackPresentationController(new FlashbackPlaybackPresentationControllerContext
        {
            PlayPauseIcon = FlashbackPlayPauseIcon,
            GoLiveButton = FlashbackGoLiveButton,
            BufferDurationText = FlashbackBufferDurationText,
            PlayheadTimeText = FlashbackPlayheadTimeText,
        });
    }
}
