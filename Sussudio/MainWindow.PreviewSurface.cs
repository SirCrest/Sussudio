using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview surface adapter. PreviewSurfacePresentationController
// owns content-fit sizing and panel visibility; PreviewSurfaceShadowController
// owns composition shadows around the video frame and control bar.
public sealed partial class MainWindow
{
    private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;
    private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;

    private void InitializePreviewSurfacePresentationController()
    {
        _previewSurfaceShadowController = new PreviewSurfaceShadowController(new PreviewSurfaceShadowControllerContext
        {
            VideoShadowHost = VideoShadowHost,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
        });

        _previewSurfacePresentationController = new PreviewSurfacePresentationController(
            new PreviewSurfacePresentationControllerContext
            {
                GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
                PreviewContentGrid = PreviewContentGrid,
                RecordingGlowBorder = RecordingGlowBorder,
            },
            _previewSurfaceShadowController);
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Composition transform only - overlay sizing is driven by the container.
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateVideoContentOverlays();

    private void UpdateVideoContentOverlays()
        => _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);

    private void SetupVideoFrameShadow()
        => _previewSurfaceShadowController.SetupVideoFrameShadow();

    private void SetupControlBarShadow()
        => _previewSurfaceShadowController.SetupControlBarShadow();

    private void SetGpuPreviewVisibility(Visibility visibility)
        => _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);

    private void ClearVideoFrameShadow()
        => _previewSurfaceShadowController.ClearVideoFrameShadow();

    private void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);

    private void FadeOutVideoFrameShadow(int durationMs)
        => _previewSurfaceShadowController.FadeOutVideoFrameShadow(durationMs);

    private void FadeInControlBarShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInControlBarShadow(delayMs, durationMs);
}
