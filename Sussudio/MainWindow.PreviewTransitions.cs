using System.Threading.Tasks;
using Sussudio.Controllers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio;

// XAML-facing preview transition/presentation adapter. Focused controllers own
// button actions, audio fades, delayed fade-in, reinit transitions, startup
// overlay presentation, and preview content/shell animations.
public sealed partial class MainWindow
{
    private PreviewAudioFadeController _previewAudioFadeController = null!;
    private PreviewButtonActionController _previewButtonActionController = null!;
    private PreviewFadeInController _previewFadeInController = null!;
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;

    private void InitializePreviewAudioFadeController()
    {
        _previewAudioFadeController = new PreviewAudioFadeController(new PreviewAudioFadeControllerContext
        {
            ViewModel = ViewModel,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
        });
    }

    private void InitializePreviewButtonActionController()
    {
        _previewButtonActionController = new PreviewButtonActionController(new PreviewButtonActionControllerContext
        {
            ViewModel = ViewModel,
            SetPreviewStopRequestedByUser = SetPreviewStopRequestedByUser,
            GetPreviewStartupAttemptId = () => PreviewStartupAttemptId,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewAudioFadeOutAsync = () => StartPreviewAudioFadeOutAsync(),
            AnimatePreviewOutAsync = AnimatePreviewOutAsync,
            ClearPreviewReinitAnimation = operationName =>
            {
                _previewReinitTransitionController.Clear(operationName, operationName: operationName);
            },
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
        });
    }

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

    private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;

    private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;

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

    private void PrimePreviewAudioFadeIn()
        => _previewAudioFadeController.PrimeFadeIn();

    private void StartPreviewAudioFadeIn(int durationMs = 900)
        => _previewAudioFadeController.StartFadeIn(durationMs);

    private Task StartPreviewAudioFadeOutAsync(int durationMs = 450)
        => _previewAudioFadeController.StartFadeOutAsync(durationMs);

    private void CancelPreviewAudioFadeInForUser()
        => _previewAudioFadeController.CancelFadeInForUser();

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

    private Task TogglePreviewFromButtonAsync()
        => _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));
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
        => _previewRendererHostController.StopRendererForReinitTeardownAsync();

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
