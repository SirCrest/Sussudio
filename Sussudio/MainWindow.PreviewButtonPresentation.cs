using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for preview button chrome. PreviewButton_Click still owns
// the command behavior; this partial only delegates glyph and tooltip state.
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
