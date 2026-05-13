using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for preview-frame screenshots. Whole-window automation
// screenshots stay in MainWindow.Screenshot.cs.
public sealed partial class MainWindow
{
    private PreviewScreenshotController _previewScreenshotController = null!;

    private void InitializePreviewScreenshotController()
    {
        _previewScreenshotController = new PreviewScreenshotController(new PreviewScreenshotControllerContext
        {
            ViewModel = ViewModel,
            ScreenshotButton = ScreenshotButton,
        });
    }

    private Task CapturePreviewScreenshotAsync()
        => _previewScreenshotController.CaptureAsync();
}
