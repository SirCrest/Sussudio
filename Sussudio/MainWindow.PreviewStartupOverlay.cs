using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for preview startup loading overlay presentation.
// Timeout and watchdog behavior is owned by PreviewStartupWatchdogController;
// MainWindow.PreviewStartupWatchdog.cs wires the XAML-facing adapter.
public sealed partial class MainWindow
{
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
            FadeInElement = FadeInElement,
            FadeOutElement = FadeOutElement,
        });
    }

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);
}
