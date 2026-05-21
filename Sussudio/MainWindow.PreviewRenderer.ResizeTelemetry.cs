using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

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
