using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;

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

    private void UpdateFlashbackMarkers()
        => _flashbackMarkerPresentationController.UpdateMarkers(
            ViewModel.FlashbackBufferFilledDuration,
            ViewModel.FlashbackInPoint,
            ViewModel.FlashbackOutPoint);
}
