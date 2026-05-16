using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Sussudio.Controllers;

internal sealed class PreviewSurfacePresentationControllerContext
{
    public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }
    public required FrameworkElement PreviewContentGrid { get; init; }
    public required FrameworkElement RecordingGlowBorder { get; init; }
    public required UIElement VideoShadowHost { get; init; }
    public required UIElement ControlBarShadowHost { get; init; }
    public required FrameworkElement ControlBarBorder { get; init; }
}

internal sealed class PreviewSurfacePresentationController
{
    private readonly PreviewSurfacePresentationControllerContext _context;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;

    public PreviewSurfacePresentationController(PreviewSurfacePresentationControllerContext context)
    {
        _context = context;
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
            if (_videoShadowVisual != null)
            {
                _videoShadowVisual.Size = Vector2.Zero;
            }

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

        // Update shadow visual to match the video content rect.
        // VideoShadowHost is a sibling of PreviewBorder - shadow casts onto app background.
        if (_videoShadowVisual != null)
        {
            const float borderMarginH = 12f; // PreviewBorder Margin left/right
            const float borderMarginV = 6f;  // PreviewBorder Margin top/bottom
            const float hostMargin = 16f;    // PreviewShadowHost Margin
            _videoShadowVisual.Offset = new Vector3(
                borderMarginH + hostMargin + (float)marginH,
                borderMarginV + hostMargin + (float)marginV, 0);
            _videoShadowVisual.Size = new Vector2(Math.Max(0, (float)fitW), Math.Max(0, (float)fitH));
        }
    }

    public void SetupVideoFrameShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(_context.VideoShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 16;
        shadow.Color = Windows.UI.Color.FromArgb(160, 0, 0, 0);
        shadow.Offset = new Vector3(0, 2, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;

        spriteVisual.Opacity = 0f; // Start invisible - faded in with preview entrance.
        _videoShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(_context.VideoShadowHost, spriteVisual);
    }

    public void SetupControlBarShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(_context.ControlBarShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 12;
        shadow.Color = Windows.UI.Color.FromArgb(120, 0, 0, 0);
        shadow.Offset = new Vector3(0, 1, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;
        spriteVisual.Opacity = 0f; // Start invisible - faded in with control bar entrance.

        _controlBarShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(_context.ControlBarShadowHost, spriteVisual);

        // Track control bar size changes to keep the shadow aligned.
        _context.ControlBarBorder.SizeChanged += (_, e) =>
        {
            if (_controlBarShadowVisual == null)
            {
                return;
            }

            var margin = _context.ControlBarBorder.Margin;
            _controlBarShadowVisual.Offset = new Vector3((float)margin.Left, (float)margin.Top, 0);
            _controlBarShadowVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    public void SetGpuPreviewVisibility(Visibility visibility)
    {
        // PreviewLetterboxBackground stays Collapsed - letterbox areas must be
        // transparent so the Composition DropShadow is visible around the video.
        _context.GetPreviewSwapChainPanel().Visibility = visibility;
    }

    public void ClearVideoFrameShadow()
    {
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(_context.VideoShadowHost, null);
    }

    public void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => PreviewShadowFadeAnimator.FadeIn(_videoShadowVisual, delayMs, durationMs);

    public void FadeOutVideoFrameShadow(int durationMs)
        => PreviewShadowFadeAnimator.FadeOut(_videoShadowVisual, durationMs);

    public void FadeInControlBarShadow(int delayMs, int durationMs)
        => PreviewShadowFadeAnimator.FadeIn(_controlBarShadowVisual, delayMs, durationMs);
}
