namespace Sussudio;

// XAML-facing preview renderer reinit adapter. The host controller owns the
// unsafe-window telemetry and D3D renderer retirement semantics.
public sealed partial class MainWindow
{
    public long RendererReinitUnsafeWindows
        => _previewRendererHostController.RendererReinitUnsafeWindows;

    private void DisposeD3DPreviewRendererForReinit()
        => _previewRendererHostController.DisposeD3DPreviewRendererForReinit();
}
