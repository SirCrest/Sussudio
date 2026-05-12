using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// XAML/automation adapter for whole-window screenshot capture. Native capture
// and image encoding live in WindowScreenshotController.
public sealed partial class MainWindow
{
    private WindowScreenshotController _windowScreenshotController = null!;

    private void InitializeWindowScreenshotController()
    {
        _windowScreenshotController = new WindowScreenshotController(
            _dispatcherQueue,
            () => _hwnd);
    }

    public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
        => _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);
}
