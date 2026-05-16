using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Sussudio.Controllers;

internal sealed class PreviewSurfaceShadowControllerContext
{
    public required UIElement VideoShadowHost { get; init; }
    public required UIElement ControlBarShadowHost { get; init; }
    public required FrameworkElement ControlBarBorder { get; init; }
}

internal sealed class PreviewSurfaceShadowController
{
    private readonly PreviewSurfaceShadowControllerContext _context;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;

    public PreviewSurfaceShadowController(PreviewSurfaceShadowControllerContext context)
    {
        _context = context;
    }

    public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)
    {
        if (_videoShadowVisual == null)
        {
            return;
        }

        const float borderMarginH = 12f; // PreviewBorder Margin left/right
        const float borderMarginV = 6f;  // PreviewBorder Margin top/bottom
        const float hostMargin = 16f;    // PreviewShadowHost Margin
        _videoShadowVisual.Offset = new Vector3(
            borderMarginH + hostMargin + (float)marginH,
            borderMarginV + hostMargin + (float)marginV, 0);
        _videoShadowVisual.Size = new Vector2(Math.Max(0, (float)fitW), Math.Max(0, (float)fitH));
    }

    public void ClearVideoFrameBounds()
    {
        if (_videoShadowVisual != null)
        {
            _videoShadowVisual.Size = Vector2.Zero;
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
