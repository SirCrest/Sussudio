using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview surface adapter. PreviewSurfacePresentationController
// owns content-fit sizing, panel visibility, and composition shadows around the
// video frame and control bar.
public sealed partial class MainWindow
{
    private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;

    private void InitializePreviewSurfacePresentationController()
    {
        _previewSurfacePresentationController = new PreviewSurfacePresentationController(new PreviewSurfacePresentationControllerContext
        {
            GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
            PreviewContentGrid = PreviewContentGrid,
            RecordingGlowBorder = RecordingGlowBorder,
            VideoShadowHost = VideoShadowHost,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
        });
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Composition transform only - overlay sizing is driven by the container.
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _d3dRenderer?.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateVideoContentOverlays();

    private void UpdateVideoContentOverlays()
        => _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);

    private void SetupVideoFrameShadow()
        => _previewSurfacePresentationController.SetupVideoFrameShadow();

    private void SetupControlBarShadow()
        => _previewSurfacePresentationController.SetupControlBarShadow();

    private void SetGpuPreviewVisibility(Visibility visibility)
        => _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);

    private void ClearVideoFrameShadow()
        => _previewSurfacePresentationController.ClearVideoFrameShadow();

    private void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => _previewSurfacePresentationController.FadeInVideoFrameShadow(delayMs, durationMs);

    private void FadeOutVideoFrameShadow(int durationMs)
        => _previewSurfacePresentationController.FadeOutVideoFrameShadow(durationMs);

    private void FadeInControlBarShadow(int delayMs, int durationMs)
        => _previewSurfacePresentationController.FadeInControlBarShadow(delayMs, durationMs);
}
