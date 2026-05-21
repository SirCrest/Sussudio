using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview startup overlay adapter.
public sealed partial class MainWindow
{
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
        });
    }

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);
}