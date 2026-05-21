using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;
    private FlashbackPlaybackUiCoordinator _flashbackPlaybackUiCoordinator = null!;

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

    private void InitializeFlashbackPlaybackUiCoordinator()
    {
        _flashbackPlaybackUiCoordinator = new FlashbackPlaybackUiCoordinator(new FlashbackPlaybackUiCoordinatorContext
        {
            ViewModel = ViewModel,
            ApplyTrackSize = _flashbackTimelineController.ApplyTrackSize,
            RequestPlayheadSnapOnNextUpdate = RequestFlashbackPlayheadSnapOnNextUpdate,
            UpdateMarkers = UpdateFlashbackMarkers,
            RefreshCtiMotion = RefreshFlashbackCtiMotion,
            IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,
            StartPlaybackPolling = StartFlashbackPlaybackPolling,
            StopPlaybackPolling = StopFlashbackPlaybackPolling,
            PlaybackPresentation = _flashbackPlaybackPresentationController,
        });
    }

    private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)
        => _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);

    private void UpdateFlashbackStateUI()
        => _flashbackPlaybackUiCoordinator.UpdateState();

    private void UpdateFlashbackBufferFill()
        => _flashbackPlaybackUiCoordinator.UpdateBufferFill();

    private void UpdateFlashbackPositionUI()
        => _flashbackPlaybackUiCoordinator.UpdatePosition();

    private void UpdateFlashbackBufferPresentation()
        => _flashbackPlaybackUiCoordinator.UpdateBufferPresentation();
}
