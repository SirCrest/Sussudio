using System.Threading.Tasks;
using Sussudio.Controllers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio;

// XAML-facing preview transition adapter. PreviewTransitionAnimationController
// owns preview content/shell fade and scale transitions plus unavailable-state
// placeholder presentation; focused controllers own delayed fade-in and startup
// overlay presentation.
public sealed partial class MainWindow
{
    private PreviewFadeInController _previewFadeInController = null!;
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;

    private void InitializePreviewFadeInController()
    {
        _previewFadeInController = new PreviewFadeInController(new PreviewFadeInControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            GetRenderer = () => _previewRendererHostController.Renderer,
            AnimatePreviewInAsync = AnimatePreviewInAsync,
            StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),
        });
    }

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
            FadeInElement = FadeInElement,
            FadeOutElement = FadeOutElement,
        });
    }

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

    private void SchedulePreviewFadeIn()
        => _previewFadeInController.Schedule();

    private void StopPreviewFadeInTimer()
        => _previewFadeInController.Stop();

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);

    private Task AnimatePreviewOutAsync()
    {
        FadeOutVideoFrameShadow(durationMs: 150);
        return _previewTransitionAnimationController.AnimatePreviewOutAsync();
    }

    private Task AnimatePreviewInAsync()
    {
        FadeInVideoFrameShadow(delayMs: 0, durationMs: 400);
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
