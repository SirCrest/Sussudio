using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// Flashback timeline presentation glue. Command behavior lives in
// FlashbackCommandController; playback UI sequencing lives in
// FlashbackPlaybackUiCoordinator, and scrub/playhead, markers, playback,
// and settings each have their own focused controller.
public sealed partial class MainWindow
{
    private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;
    private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;
    private FlashbackPlaybackUiCoordinator _flashbackPlaybackUiCoordinator = null!;
    private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;

    private void InitializeFlashbackMarkerPresentationController()
    {
        _flashbackMarkerPresentationController = new FlashbackMarkerPresentationController(new FlashbackMarkerPresentationControllerContext
        {
            ScrubArea = FlashbackScrubArea,
            InPointMarker = FlashbackInPointMarker,
            OutPointMarker = FlashbackOutPointMarker,
            SelectionRegion = FlashbackSelectionRegion,
        });
    }

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

    private void InitializeFlashbackExportProgressPresentationController()
    {
        _flashbackExportProgressPresentationController = new FlashbackExportProgressPresentationController(
            new FlashbackExportProgressPresentationControllerContext
            {
                FlashbackExportProgressBar = FlashbackExportProgressBar,
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

    private void UpdateFlashbackMarkers()
        => _flashbackMarkerPresentationController.UpdateMarkers(
            ViewModel.FlashbackBufferFilledDuration,
            ViewModel.FlashbackInPoint,
            ViewModel.FlashbackOutPoint);

    private void UpdateFlashbackExportProgress(double progress)
        => _flashbackExportProgressPresentationController.UpdateProgress(progress);

    private void UpdateFlashbackExportingPresentation(bool isExporting)
        => _flashbackExportProgressPresentationController.UpdateExporting(isExporting);
}
