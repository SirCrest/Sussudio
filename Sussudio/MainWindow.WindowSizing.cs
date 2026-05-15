using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for top-level window resize telemetry. Preview surface
// sizing stays with MainWindow.PreviewSurface.cs; close/finalize handling stays
// in MainWindow.CloseLifecycle.cs.
public sealed partial class MainWindow
{
    private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;

    private void InitializePreviewResizeTelemetryController()
    {
        _previewResizeTelemetryController = new PreviewResizeTelemetryController();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _previewResizeTelemetryController.HandleSizeChanged(
            ViewModel.IsPreviewing,
            _d3dRenderer != null,
            PreviewSwapChainPanel.Visibility);
    }

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();
}
