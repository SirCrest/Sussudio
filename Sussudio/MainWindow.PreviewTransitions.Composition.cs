using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewAudioFadeController _previewAudioFadeController = null!;
    private PreviewButtonActionController _previewButtonActionController = null!;
    private PreviewFadeInController _previewFadeInController = null!;
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;

    private void InitializePreviewAudioFadeController()
    {
        _previewAudioFadeController = new PreviewAudioFadeController(new PreviewAudioFadeControllerContext
        {
            ViewModel = ViewModel,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
        });
    }

    private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;

    private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;

    private void PrimePreviewAudioFadeIn()
        => _previewAudioFadeController.PrimeFadeIn();

    private void StartPreviewAudioFadeIn(int durationMs = 900)
        => _previewAudioFadeController.StartFadeIn(durationMs);

    private Task StartPreviewAudioFadeOutAsync(int durationMs = 450)
        => _previewAudioFadeController.StartFadeOutAsync(durationMs);

    private void CancelPreviewAudioFadeInForUser()
        => _previewAudioFadeController.CancelFadeInForUser();

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

    private Task TogglePreviewFromButtonAsync()
        => _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));
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

    private void SchedulePreviewFadeIn()
        => _previewFadeInController.Schedule();

    private void StopPreviewFadeInTimer()
        => _previewFadeInController.Stop();

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
        });
    }

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);

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

    private void InitializePreviewReinitTransitionController()
        => _previewReinitTransitionController = new PreviewReinitTransitionController();

    private bool IsPreviewReinitAnimating
        => _previewReinitTransitionController.IsAnimating;

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

    private void HandlePreviewReinitializingChanged()
        => _previewReinitTransitionController.HandleReinitializingChanged(
            new PreviewReinitCompletionPresentationContext
            {
                IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,
                IsPreviewing = ViewModel.IsPreviewing,
                IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
                AttemptLabel = PreviewStartupAttemptLabel,
                CallerName = nameof(HandleViewModelPropertyChangedAsync),
                UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
                RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
                StopPreviewStartupOverlay = StopPreviewStartupOverlay,
                ResetPreviewContentTransform = ResetPreviewContentTransform,
                ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            });
}
