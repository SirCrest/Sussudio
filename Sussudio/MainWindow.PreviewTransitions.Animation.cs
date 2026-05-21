using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview transition animation adapter.
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
            FadeOutVideoFrameShadow = FadeOutVideoFrameShadow,
            FadeInVideoFrameShadow = FadeInVideoFrameShadow,
        });
    }

    private void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
        => _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);

    private void ResetPreviewContentTransform()
        => _previewTransitionAnimationController.ResetPreviewContentTransform();

    private Task AnimatePreviewOutAsync()
        => _previewTransitionAnimationController.AnimatePreviewOutAsync();

    private Task AnimatePreviewInAsync()
        => _previewTransitionAnimationController.AnimatePreviewInAsync();

    private void PreparePreviewStartupPresentation()
        => _previewTransitionAnimationController.PrepareStartupPresentation();

    private void RevealPreviewUnavailablePlaceholder()
        => _previewTransitionAnimationController.RevealUnavailablePlaceholder();
}