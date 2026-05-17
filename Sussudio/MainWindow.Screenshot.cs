using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// XAML/automation adapter for screenshot capture. Whole-window native capture
// and preview-frame capture behavior live in dedicated screenshot controllers.
public sealed partial class MainWindow
{
    private PreviewScreenshotController _previewScreenshotController = null!;
    private WindowScreenshotController _windowScreenshotController = null!;

    private void InitializePreviewScreenshotController()
    {
        _previewScreenshotController = new PreviewScreenshotController(new PreviewScreenshotControllerContext
        {
            ViewModel = ViewModel,
            ScreenshotButton = ScreenshotButton,
        });
    }

    private void InitializeWindowScreenshotController()
    {
        _windowScreenshotController = new WindowScreenshotController(
            _dispatcherQueue,
            () => _hwnd);
    }

    private Task CapturePreviewScreenshotAsync()
        => _previewScreenshotController.CaptureAsync();

    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));
    }

    public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
        => _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);
}
