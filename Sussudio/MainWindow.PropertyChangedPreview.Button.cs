using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewButtonPresentationController _previewButtonPresentationController = null!;

    private void InitializePreviewButtonPresentationController()
    {
        _previewButtonPresentationController = new PreviewButtonPresentationController(new PreviewButtonPresentationControllerContext
        {
            PreviewButton = PreviewButton,
            PreviewButtonIcon = PreviewButtonIcon,
        });
    }

    private void ShowStopPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStopPreview();

    private void ShowStartPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStartPreview();
}
