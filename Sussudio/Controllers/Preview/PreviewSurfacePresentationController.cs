using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class PreviewSurfacePresentationControllerContext
{
    public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }
    public required FrameworkElement PreviewContentGrid { get; init; }
    public required FrameworkElement RecordingGlowBorder { get; init; }
}

internal sealed class PreviewSurfacePresentationController
{
    private readonly PreviewSurfacePresentationControllerContext _context;
    private readonly PreviewSurfaceShadowController _shadowController;

    public PreviewSurfacePresentationController(
        PreviewSurfacePresentationControllerContext context,
        PreviewSurfaceShadowController shadowController)
    {
        _context = context;
        _shadowController = shadowController;
    }

    public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)
    {
        var srcW = (double)(sourceWidth ?? 0);
        var srcH = (double)(sourceHeight ?? 0);
        // Use the container size, not the SwapChainPanel, because the panel is
        // explicitly sized to the fitted video rect.
        var dstW = _context.PreviewContentGrid.ActualWidth;
        var dstH = _context.PreviewContentGrid.ActualHeight;

        if (dstW <= 0 || dstH <= 0)
        {
            _context.RecordingGlowBorder.Margin = new Thickness(0);
            _shadowController.ClearVideoFrameBounds();

            return;
        }

        double fitW, fitH;
        if (srcW <= 0 || srcH <= 0)
        {
            // Source dimensions unknown - fill the container (same as old Stretch behavior).
            fitW = dstW;
            fitH = dstH;
        }
        else
        {
            var srcAspect = srcW / srcH;
            var dstAspect = dstW / dstH;

            if (srcAspect > dstAspect)
            {
                fitW = dstW;
                fitH = dstW / srcAspect;
            }
            else
            {
                fitH = dstH;
                fitW = dstH * srcAspect;
            }
        }

        // Resize SwapChainPanel to exactly the video content area (no letterbox).
        var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();
        previewSwapChainPanel.Width = fitW;
        previewSwapChainPanel.Height = fitH;

        var marginH = (dstW - fitW) / 2;
        var marginV = (dstH - fitH) / 2;
        var videoMargin = new Thickness(marginH, marginV, marginH, marginV);
        _context.RecordingGlowBorder.Margin = videoMargin;

        _shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);
    }

    public void SetGpuPreviewVisibility(Visibility visibility)
    {
        // PreviewLetterboxBackground stays Collapsed - letterbox areas must be
        // transparent so the Composition DropShadow is visible around the video.
        _context.GetPreviewSwapChainPanel().Visibility = visibility;
    }
}
