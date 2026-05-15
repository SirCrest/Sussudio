using System;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for Flashback timeline marker presentation.
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

    private static string FormatFlashbackDuration(TimeSpan ts)
        => FlashbackMarkerPresentationController.FormatDuration(ts);

    private void UpdateFlashbackMarkers()
        => _flashbackMarkerPresentationController.UpdateMarkers(
            ViewModel.FlashbackBufferFilledDuration,
            ViewModel.FlashbackInPoint,
            ViewModel.FlashbackOutPoint);
}
