using System;
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
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;
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

    private void InitializePreviewReinitTransitionController()
        => _previewReinitTransitionController = new PreviewReinitTransitionController();

    private bool IsPreviewReinitAnimating
        => _previewReinitTransitionController.IsAnimating;

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

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _previewReinitTransitionController.BeginAnimateOut(reason, nameof(ViewModel_PreviewReinitRequested));
        await AnimatePreviewOutAsync();
    }

    private Task ViewModel_PreviewRendererStopRequested()
    {
        // Stop the render thread before the capture pipeline teardown. This ensures
        // no native D3D calls (VideoProcessorBlt/Present) are in flight when
        // UnifiedVideoCapture disposes the shared D3D11 device and DXGI manager.
        //
        // IMPORTANT: this only drains and detaches the active renderer. The later
        // attach step may replace the SwapChainPanel surface for HDR/SDR or mode
        // changes because WinUI can keep native DXGI state behind a panel even
        // after SetSwapChain(null). Replacing the surface happens after capture
        // teardown so the old renderer is no longer receiving frames.
        var renderer = _previewRendererHostController.Renderer;
        if (renderer != null)
        {
            Logger.Log("PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
            try
            {
                DisposeD3DPreviewRendererForReinit();
            }
            catch (TimeoutException ex)
            {
                // Render thread did not exit before its stop timeout. The renderer's
                // stop path has already logged details and the fresh attach path will
                // replace the panel surface if needed. Swallow the exception so reinit
                // can continue rather than crashing the UI thread mid-resolution-change.
                Logger.Log($"PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
            }
        }

        return Task.CompletedTask;
    }

    private void PreparePreviewStartupPresentation()
        => _previewTransitionAnimationController.PrepareStartupPresentation();

    private void RevealPreviewUnavailablePlaceholder()
        => _previewTransitionAnimationController.RevealUnavailablePlaceholder();

    private void HandlePreviewReinitializingChanged()
    {
        UpdateDeviceApplyButtonState();
        switch (_previewReinitTransitionController.GetCompletionPresentation(
            ViewModel.IsPreviewReinitializing,
            ViewModel.IsPreviewing,
            IsPreviewFirstVisualConfirmed))
        {
            case PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder:
                _previewReinitTransitionController.Clear(nameof(HandleViewModelPropertyChangedAsync), logWhenInactive: false);
                RevealPreviewUnavailablePlaceholder();
                break;

            case PreviewReinitCompletionPresentation.ResetConfirmedVisual:
                _previewReinitTransitionController.ResetConfirmedVisualTransition(
                    PreviewStartupAttemptLabel,
                    "reinit-stop-failed",
                    nameof(HandleViewModelPropertyChangedAsync));
                StopPreviewStartupOverlay();
                ResetPreviewContentTransform();
                break;

            case PreviewReinitCompletionPresentation.ShowStartPreviewButton:
                ShowStartPreviewButtonPresentation();
                break;
        }
    }

    private static void FadeOutElement(UIElement element)
        => PreviewTransitionAnimationController.FadeOutElement(element);

    private static void FadeInElement(UIElement element)
        => PreviewTransitionAnimationController.FadeInElement(element);
}
