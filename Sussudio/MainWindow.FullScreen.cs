using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing full-screen adapter. FullScreenController owns the transition
// state and overlay mechanics; this partial keeps MainWindow event names stable.
public sealed partial class MainWindow
{
    private void InitializeFullScreenController()
    {
        ElementCompositionPreview.SetIsTranslationEnabled(FullScreenControlsOverlay, true);

        _fullScreenController = new FullScreenController(new FullScreenControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            RootGrid = (Grid)Content,
            RootElement = (UIElement)Content,
            RootFrameworkElement = (FrameworkElement)Content,
            PreviewBorder = PreviewBorder,
            PreviewShadowHost = PreviewShadowHost,
            PreviewContentGrid = PreviewContentGrid,
            VideoShadowHost = VideoShadowHost,
            SettingsOverlayPanel = SettingsOverlayPanel,
            StatsDockPanel = StatsDockPanel,
            FlashbackTimelinePanel = FlashbackTimelinePanel,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
            FullScreenControlsOverlay = FullScreenControlsOverlay,
            FullScreenButton = FullScreenButton,
            FullScreenButtonIcon = FullScreenButtonIcon,
            FullScreenMenuItem = FullScreenMenuItem,
            GetAppWindow = GetAppWindow,
            EndFlashbackScrubForFullScreen = EndFlashbackScrubForFullScreen,
            ResetFlashbackTimelineAnimation = ResetFlashbackTimelineAnimationForFullScreen,
            ResetSettingsShelfAnimation = ResetSettingsShelfAnimationForFullScreen,
            ShouldShowFlashbackTimeline = ShouldShowFlashbackTimeline,
            SyncFlashbackTimelineToggle = SyncFlashbackTimelineToggle,
            HideStatsDockPanelImmediate = () => HideStatsDockPanel(immediate: true),
            ShowStatsDockPanel = ShowStatsDockPanel,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            FadeInVideoShadow = () => CompositionShadowFadeAnimator.FadeIn(_videoShadowVisual, delayMs: 0, durationMs: 400),
            IsWindowClosing = () => _isWindowClosing,
        });
    }

    #region Full screen mode
    private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && _fullScreenController.IsFullScreen)
        {
            e.Handled = true;
            ExitFullScreen();
            return;
        }

        // Flashback keyboard shortcuts (only when timeline is visible)
        if (ViewModel.IsFlashbackEnabled && FlashbackTimelinePanel.Visibility == Visibility.Visible)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.I:
                    FlashbackInButton_Click(sender, e);
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.O:
                    FlashbackOutButton_Click(sender, e);
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.Space:
                    FlashbackPlayPauseButton_Click(sender, e);
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.L:
                    FlashbackGoLiveButton_Click(sender, e);
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.Left:
                    if (!ViewModel.FlashbackNudge(TimeSpan.FromSeconds(-1)))
                    {
                        ViewModel.ReportFlashbackPlaybackRejection("nudge left", "FLASHBACK_UI_NUDGE_REJECTED direction=left");
                    }
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.Right:
                    if (!ViewModel.FlashbackNudge(TimeSpan.FromSeconds(1)))
                    {
                        ViewModel.ReportFlashbackPlaybackRejection("nudge right", "FLASHBACK_UI_NUDGE_REJECTED direction=right");
                    }
                    e.Handled = true;
                    return;
            }
        }
    }

    private void PreviewBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
        => _fullScreenController.Toggle();

    public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(
            () => _fullScreenController.SetEnabledAsync(enabled),
            cancellationToken);

    private void EnterFullScreen()
        => _fullScreenController.Enter();

    private void ExitFullScreen()
        => _fullScreenController.Exit();

    private Task EnterFullScreenAsync()
        => _fullScreenController.EnterAsync();

    private Task ExitFullScreenAsync()
        => _fullScreenController.ExitAsync();

    private bool ShouldShowFlashbackTimeline()
    {
        return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;
    }

    private void EndFlashbackScrubForFullScreen()
    {
        if (!_isFlashbackScrubbing)
        {
            return;
        }

        var carriedPosition = _lastScrubPointerPosition;
        Logger.Log($"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        _isFlashbackScrubbing = false;
        _lastScrubUpdateTick = 0;
        _lastScrubPointerPosition = null;
        var ended = carriedPosition.HasValue
            ? ViewModel?.FlashbackEndScrubAt(carriedPosition.Value) ?? false
            : ViewModel?.FlashbackEndScrub() ?? false;
        if (!ended)
        {
            ViewModel?.ReportFlashbackPlaybackRejection("scrub end (fullscreen_enter)", "FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter");
        }
    }
    #endregion

    #region Fullscreen overlay controls
    private void OnFullScreenPointerActivity(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnPointerActivity(e);

    private void OnFullScreenControlsPointerEntered(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerEntered();

    private void OnFullScreenControlsPointerExited(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerExited(e);

    private void StopFullScreenAutoHideTimer()
        => _fullScreenController.StopAutoHideTimer();
    #endregion
}
