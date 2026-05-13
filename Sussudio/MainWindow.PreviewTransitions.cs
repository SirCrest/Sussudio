using System.Threading.Tasks;
using Sussudio.Controllers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio;

// XAML-facing preview transition adapter. PreviewTransitionAnimationController
// owns preview content/shell fade and scale transitions plus unavailable-state
// placeholder presentation.
public sealed partial class MainWindow
{
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;

    private void InitializePreviewTransitionAnimationController()
    {
        _previewTransitionAnimationController = new PreviewTransitionAnimationController(new PreviewTransitionAnimationControllerContext
        {
            PreviewBorder = PreviewBorder,
            PreviewBorderScale = PreviewBorderScale,
            PreviewContentGrid = PreviewContentGrid,
            PreviewContentScale = PreviewContentScale,
            NoDevicePlaceholder = NoDevicePlaceholder,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewStartupOverlay = StartPreviewStartupOverlay,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
        });
    }

    private void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
        => _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);

    private void ResetPreviewContentTransform()
        => _previewTransitionAnimationController.ResetPreviewContentTransform();

    private Task AnimatePreviewOutAsync()
    {
        FadeOutShadow(_videoShadowVisual, durationMs: 150);
        return _previewTransitionAnimationController.AnimatePreviewOutAsync();
    }

    private Task AnimatePreviewInAsync()
    {
        FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        return _previewTransitionAnimationController.AnimatePreviewInAsync();
    }

    private void PreparePreviewStartupPresentation()
        => _previewTransitionAnimationController.PrepareStartupPresentation();

    private void RevealPreviewUnavailablePlaceholder()
        => _previewTransitionAnimationController.RevealUnavailablePlaceholder();

    private static void FadeOutElement(UIElement element)
        => PreviewTransitionAnimationController.FadeOutElement(element);

    private static void FadeInElement(UIElement element)
        => PreviewTransitionAnimationController.FadeInElement(element);
}
