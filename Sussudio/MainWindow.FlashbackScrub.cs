using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing Flashback pointer scrub adapter. FlashbackScrubInteractionController
// owns active scrub state, scrub throttling, and pointer lifecycle around scrub
// commands.
public sealed partial class MainWindow
{
    private FlashbackScrubInteractionController _flashbackScrubInteractionController = null!;

    private void InitializeFlashbackScrubInteractionController()
    {
        _flashbackScrubInteractionController = new FlashbackScrubInteractionController(new FlashbackScrubInteractionControllerContext
        {
            ViewModel = ViewModel,
            ScrubArea = FlashbackScrubArea,
            PositionMagneticPlayhead = (x, width) => PositionFlashbackPlayhead(x, width, FlashbackPlayheadMotion.Magnetic),
            RefreshCtiMotion = RefreshFlashbackCtiMotion,
            GetTickCount64 = () => Environment.TickCount64,
        });
    }

    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerPressed(sender as UIElement, e);

    private void FlashbackScrubArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerMoved(sender as UIElement, e);

    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerReleased(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCanceled(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCaptureLost(sender as UIElement, e);
}
