using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class PreviewStartupOverlayControllerContext
{
    public required Panel PreviewLoadingOverlay { get; init; }
    public required Action<UIElement> FadeInElement { get; init; }
    public required Action<UIElement> FadeOutElement { get; init; }
}

internal sealed class PreviewStartupOverlayController
{
    private readonly PreviewStartupOverlayControllerContext _context;

    public PreviewStartupOverlayController(PreviewStartupOverlayControllerContext context)
    {
        _context = context;
    }

    public void Start()
    {
        var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];
        ring.IsActive = true;
        _context.FadeInElement(_context.PreviewLoadingOverlay);
    }

    public void Stop(bool isPreviewReinitAnimating)
    {
        if (_context.PreviewLoadingOverlay.Visibility == Visibility.Collapsed)
        {
            return;
        }

        var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];
        ring.IsActive = false;
        if (isPreviewReinitAnimating)
        {
            _context.PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
            _context.PreviewLoadingOverlay.Opacity = 1.0;
            return;
        }

        _context.FadeOutElement(_context.PreviewLoadingOverlay);
    }
}
