using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing Flashback export progress adapter. The controller owns the
// progress bar's value, visibility, and reset semantics.
public sealed partial class MainWindow
{
    private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;

    private void InitializeFlashbackExportProgressPresentationController()
    {
        _flashbackExportProgressPresentationController = new FlashbackExportProgressPresentationController(
            new FlashbackExportProgressPresentationControllerContext
            {
                FlashbackExportProgressBar = FlashbackExportProgressBar,
            });
    }

    private void UpdateFlashbackExportProgress(double progress)
        => _flashbackExportProgressPresentationController.UpdateProgress(progress);

    private void UpdateFlashbackExportingPresentation(bool isExporting)
        => _flashbackExportProgressPresentationController.UpdateExporting(isExporting);
}
