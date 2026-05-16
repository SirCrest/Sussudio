using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for top-level window resize telemetry. Preview surface
// sizing stays with MainWindow.PreviewSurface.cs; close routing/finalization
// lives with the close lifecycle and recording finalization controllers.
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
            _previewRendererHostController.HasD3DRenderer,
            PreviewSwapChainPanel.Visibility);
    }

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();
}
