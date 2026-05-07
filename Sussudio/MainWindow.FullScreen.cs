using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Full-screen and restore behavior for the preview window. This is a shell/UI
// mode change only; capture and preview renderer ownership stay unchanged.
public sealed partial class MainWindow
{
    #region Full screen mode
    private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)
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
    {
        if (_isFullScreen)
        {
            ExitFullScreen();
        }
        else
        {
            EnterFullScreen();
        }
    }
    private async void EnterFullScreen()
    {
        if (_isFullScreenTransitioning || _isFullScreen) return;
        _isFullScreenTransitioning = true;

        var appWindow = GetAppWindow();
        _preFullScreenPosition = appWindow.Position;
        _preFullScreenBounds = new Windows.Graphics.RectInt32(
            appWindow.Position.X, appWindow.Position.Y,
            appWindow.Size.Width, appWindow.Size.Height);
        _preFullScreenSettingsVisible = SettingsOverlayPanel.Visibility == Visibility.Visible;
        _preFullScreenStatsDockVisible = StatsDockPanel.Visibility == Visibility.Visible;

        // Capture pre-transition preview rect
        var transform = PreviewBorder.TransformToVisual((UIElement)Content);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = PreviewBorder.ActualWidth;
        var preH = PreviewBorder.ActualHeight;

        // Clean up scrub state if user is scrubbing when fullscreen triggers
        if (_isFlashbackScrubbing)
        {
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

        // Move controls to the fullscreen overlay for VLC-style slide-up behavior.
        // The overlay floats above the preview at the bottom of the screen.
        var mainGrid = (Grid)Content;
        PrepareFullScreenChromeForOverlay();
        ApplyFullScreenChromeMaterials();

        // Always move the timeline to the overlay so the flashback toggle works in fullscreen.
        // Insert at index 0 so it stacks above the control bar.
        mainGrid.Children.Remove(FlashbackTimelinePanel);
        FullScreenControlsOverlay.Children.Insert(0, FlashbackTimelinePanel);

        mainGrid.Children.Remove(ControlBarShadowHost);
        ControlBarShadowHost.Visibility = Visibility.Collapsed;

        mainGrid.Children.Remove(ControlBarBorder);
        ControlBarBorder.Visibility = Visibility.Visible;
        FullScreenControlsOverlay.Children.Add(ControlBarBorder);

        // Overlay starts off-screen (translated down, invisible).
        // Use Translation (additive to layout position) not Offset (replaces it).
        FullScreenControlsOverlay.Visibility = Visibility.Visible;
        var overlayVisual = ElementCompositionPreview.GetElementVisual(FullScreenControlsOverlay);
        overlayVisual.Properties.InsertVector3("Translation", new Vector3(0, 300, 0));
        overlayVisual.Opacity = 0;
        FullScreenControlsOverlay.IsHitTestVisible = false;
        _fullScreenControlsVisible = false;
        _fullScreenPointerOverControls = false;

        if (_preFullScreenSettingsVisible)
        {
            SettingsOverlayPanel.Visibility = Visibility.Collapsed;
            SettingsOverlayPanel.Height = double.NaN;
            SettingsOverlayPanel.Opacity = 1;
            _isSettingsShelfAnimating = false;
        }
        if (_preFullScreenStatsDockVisible)
        {
            HideStatsDockPanel(immediate: true);
        }

        PreviewBorder.Margin = new Thickness(0);
        PreviewShadowHost.Margin = new Thickness(0);
        PreviewShadowHost.CornerRadius = new CornerRadius(0);

        ((Grid)Content).Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        VideoShadowHost.Visibility = Visibility.Collapsed;

        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

        // Wait for layout, then animate
        await WaitForSizeChangedAsync(PreviewContentGrid, 200);

        var postTransform = PreviewBorder.TransformToVisual((UIElement)Content);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = PreviewBorder.ActualWidth;
        var postH = PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            AnimateFullScreenRect(
                prePosition, preW, preH,
                postPosition, postW, postH,
                () =>
                {
                    _isFullScreen = true;
                    _isFullScreenTransitioning = false;
                    UpdateFullScreenButtonState();
                });
        }
        else
        {
            _isFullScreen = true;
            _isFullScreenTransitioning = false;
            UpdateFullScreenButtonState();
        }
    }
    private async void ExitFullScreen()
    {
        if (_isFullScreenTransitioning || !_isFullScreen) return;
        _isFullScreenTransitioning = true;

        // Tear down fullscreen overlay
        StopFullScreenAutoHideTimer();
        var overlayVisual = ElementCompositionPreview.GetElementVisual(FullScreenControlsOverlay);
        overlayVisual.StopAnimation("Translation");
        overlayVisual.StopAnimation("Opacity");
        overlayVisual.Properties.InsertVector3("Translation", Vector3.Zero);
        overlayVisual.Opacity = 1;

        // Move controls back to main grid (collapsed to avoid layout glitch during transition)
        var mainGrid = (Grid)Content;
        var timelineVisibleAtExit = ShouldShowFlashbackTimeline();
        PrepareFullScreenChromeForOverlay();
        FullScreenControlsOverlay.Children.Remove(FlashbackTimelinePanel);
        FlashbackTimelinePanel.Visibility = Visibility.Collapsed;
        mainGrid.Children.Add(FlashbackTimelinePanel);

        FullScreenControlsOverlay.Children.Remove(ControlBarBorder);
        ControlBarBorder.Visibility = Visibility.Collapsed;
        FullScreenControlsOverlay.Visibility = Visibility.Collapsed;
        _fullScreenControlsVisible = false;

        // Re-add to main grid (Grid.Row attached properties are preserved from XAML)
        mainGrid.Children.Add(ControlBarShadowHost);
        mainGrid.Children.Add(ControlBarBorder);

        var transform = PreviewBorder.TransformToVisual((UIElement)Content);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = PreviewBorder.ActualWidth;
        var preH = PreviewBorder.ActualHeight;

        // Commit layout restoration
        var appWindow = GetAppWindow();
        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
        appWindow.Move(_preFullScreenPosition);
        appWindow.Resize(new Windows.Graphics.SizeInt32(_preFullScreenBounds.Width, _preFullScreenBounds.Height));

        PreviewBorder.Margin = new Thickness(12, 6, 12, 6);
        PreviewShadowHost.Margin = new Thickness(16);
        PreviewShadowHost.CornerRadius = new CornerRadius(4);

        ControlBarBorder.Visibility = Visibility.Visible;
        ControlBarShadowHost.Visibility = Visibility.Visible;
        RestoreWindowedChromeMaterials();
        if (_preFullScreenSettingsVisible)
        {
            SettingsOverlayPanel.Visibility = Visibility.Visible;
        }
        if (_preFullScreenStatsDockVisible)
        {
            ShowStatsDockPanel();
        }
        if (timelineVisibleAtExit)
        {
            FlashbackTimelinePanel.Visibility = Visibility.Visible;
        }

        ((Grid)Content).Background = null;

        await WaitForSizeChangedAsync(PreviewContentGrid, 200);

        var postTransform = PreviewBorder.TransformToVisual((UIElement)Content);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = PreviewBorder.ActualWidth;
        var postH = PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            AnimateFullScreenRect(
                prePosition, preW, preH,
                postPosition, postW, postH,
                () =>
                {
                    _isFullScreen = false;
                    _isFullScreenTransitioning = false;
                    UpdateFullScreenButtonState();
                    UpdateVideoContentOverlays();
                    VideoShadowHost.Visibility = Visibility.Visible;
                    FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
                });
        }
        else
        {
            _isFullScreen = false;
            _isFullScreenTransitioning = false;
            UpdateFullScreenButtonState();
            UpdateVideoContentOverlays();
            VideoShadowHost.Visibility = Visibility.Visible;
            FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        }
    }

    private bool ShouldShowFlashbackTimeline()
    {
        return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;
    }

    private void PrepareFullScreenChromeForOverlay()
    {
        _flashbackTimelineStoryboard?.Stop();
        _flashbackTimelineStoryboard = null;
        _isFlashbackTimelineAnimating = false;

        ControlBarBorder.Opacity = 1;
        if (ControlBarBorder.RenderTransform is TranslateTransform controlBarTranslate)
        {
            controlBarTranslate.Y = 0;
        }

        var showTimeline = ShouldShowFlashbackTimeline();
        FlashbackTimelinePanel.Visibility = showTimeline ? Visibility.Visible : Visibility.Collapsed;
        FlashbackTimelinePanel.Height = double.NaN;
        FlashbackTimelinePanel.Opacity = 1;
        FlashbackTimelinePanel.IsHitTestVisible = ViewModel.IsFlashbackEnabled;
        SyncFlashbackTimelineToggle(showTimeline);
    }

    private void ApplyFullScreenChromeMaterials()
    {
        _preFullScreenControlBarBackground ??= ControlBarBorder.Background;
        _preFullScreenFlashbackTimelineBackground ??= FlashbackTimelinePanel.Background;

        ControlBarBorder.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x14, 0x14, 0x14));
        FlashbackTimelinePanel.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x20, 0x20, 0x20));
    }

    private void RestoreWindowedChromeMaterials()
    {
        if (_preFullScreenControlBarBackground != null)
        {
            ControlBarBorder.Background = _preFullScreenControlBarBackground;
            _preFullScreenControlBarBackground = null;
        }

        if (_preFullScreenFlashbackTimelineBackground != null)
        {
            FlashbackTimelinePanel.Background = _preFullScreenFlashbackTimelineBackground;
            _preFullScreenFlashbackTimelineBackground = null;
        }
    }

    private void AnimateFullScreenRect(
        Windows.Foundation.Point prePos, double preW, double preH,
        Windows.Foundation.Point postPos, double postW, double postH,
        Action onCompleted)
    {
        var scaleX = (float)(preW / postW);
        var scaleY = (float)(preH / postH);
        // Offset compensates for CenterPoint at element center:
        // the scale-induced position shift is (postSize/2)*(1-scale),
        // so offset = positionDelta + (preSize-postSize)/2.
        var offsetX = (float)(prePos.X - postPos.X + (preW - postW) / 2);
        var offsetY = (float)(prePos.Y - postPos.Y + (preH - postH) / 2);

        var visual = ElementCompositionPreview.GetElementVisual(PreviewBorder);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3((float)(postW / 2), (float)(postH / 2), 0);

        // Single progress scalar drives both properties — one timeline,
        // one easing evaluation per frame, zero desync.
        var props = compositor.CreatePropertySet();
        props.InsertScalar("Progress", 0f);

        var duration = TimeSpan.FromMilliseconds(350);
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0f), new Vector2(0f, 1f));

        var progressAnim = compositor.CreateScalarKeyFrameAnimation();
        progressAnim.InsertKeyFrame(1f, 1f, easing);
        progressAnim.Duration = duration;

        var scaleExpr = compositor.CreateExpressionAnimation(
            "Vector3(s.X + (1 - s.X) * p.Progress, s.Y + (1 - s.Y) * p.Progress, 1)");
        scaleExpr.SetVector3Parameter("s", new Vector3(scaleX, scaleY, 1));
        scaleExpr.SetReferenceParameter("p", props);

        var offsetExpr = compositor.CreateExpressionAnimation(
            "Vector3(o.X * (1 - p.Progress), o.Y * (1 - p.Progress), 0)");
        offsetExpr.SetVector3Parameter("o", new Vector3(offsetX, offsetY, 0));
        offsetExpr.SetReferenceParameter("p", props);

        // Batch tracks the finite-duration progress animation for Completed.
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        props.StartAnimation("Progress", progressAnim);
        batch.End();

        // Expression animations run outside the batch (indefinite lifetime,
        // stopped explicitly in Completed).
        visual.StartAnimation("Scale", scaleExpr);
        visual.StartAnimation("Offset", offsetExpr);

        batch.Completed += (_, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                visual.StopAnimation("Scale");
                visual.StopAnimation("Offset");
                visual.Scale = Vector3.One;
                visual.Offset = Vector3.Zero;
                visual.CenterPoint = Vector3.Zero;
                onCompleted();
            });
        };
    }
    private static Task WaitForSizeChangedAsync(FrameworkElement element, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        SizeChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            element.SizeChanged -= handler;
            tcs.TrySetResult(true);
        };
        element.SizeChanged += handler;

        // Timeout fallback
        _ = Task.Delay(timeoutMs).ContinueWith(_ =>
        {
            element.DispatcherQueue.TryEnqueue(() =>
            {
                element.SizeChanged -= handler;
                tcs.TrySetResult(false);
            });
        });

        return tcs.Task;
    }
    private void UpdateFullScreenButtonState()
    {
        if (_isFullScreen)
        {
            FullScreenButtonIcon.Glyph = "\uE73F";
            ToolTipService.SetToolTip(FullScreenButton, "Exit full screen");
            FullScreenMenuItem.Text = "Exit Full Screen";
            if (FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE73F";
        }
        else
        {
            FullScreenButtonIcon.Glyph = "\uE740";
            ToolTipService.SetToolTip(FullScreenButton, "Full screen");
            FullScreenMenuItem.Text = "Enter Full Screen";
            if (FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE740";
        }
    }
    #endregion

    #region Fullscreen overlay controls (VLC-style slide-up on mouse activity)
    private void ShowFullScreenControls()
    {
        if (_fullScreenControlsVisible) return;
        _fullScreenControlsVisible = true;
        FullScreenControlsOverlay.IsHitTestVisible = true;

        var visual = ElementCompositionPreview.GetElementVisual(FullScreenControlsOverlay);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));

        var translationAnim = compositor.CreateVector3KeyFrameAnimation();
        translationAnim.InsertKeyFrame(1f, Vector3.Zero, easing);
        translationAnim.Duration = TimeSpan.FromMilliseconds(400);

        var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.InsertKeyFrame(1f, 1f, easing);
        opacityAnim.Duration = TimeSpan.FromMilliseconds(400);

        visual.StartAnimation("Translation", translationAnim);
        visual.StartAnimation("Opacity", opacityAnim);
    }

    private void HideFullScreenControls()
    {
        if (!_fullScreenControlsVisible) return;
        if (_fullScreenPointerOverControls) return;
        _fullScreenControlsVisible = false;

        var visual = ElementCompositionPreview.GetElementVisual(FullScreenControlsOverlay);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));

        var hideOffset = (float)Math.Max(200, FullScreenControlsOverlay.ActualHeight + 20);

        var translationAnim = compositor.CreateVector3KeyFrameAnimation();
        translationAnim.InsertKeyFrame(1f, new Vector3(0, hideOffset, 0), easing);
        translationAnim.Duration = TimeSpan.FromMilliseconds(300);

        var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.InsertKeyFrame(1f, 0f, easing);
        opacityAnim.Duration = TimeSpan.FromMilliseconds(300);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        visual.StartAnimation("Translation", translationAnim);
        visual.StartAnimation("Opacity", opacityAnim);
        batch.End();

        batch.Completed += (_, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!_fullScreenControlsVisible)
                {
                    FullScreenControlsOverlay.IsHitTestVisible = false;
                }
            });
        };
    }

    private void OnFullScreenPointerActivity(object sender, PointerRoutedEventArgs e)
    {
        if (_isWindowClosing || !_isFullScreen || _isFullScreenTransitioning) return;

        var position = e.GetCurrentPoint((UIElement)Content).Position;
        var contentHeight = ((FrameworkElement)Content).ActualHeight;
        var inHotZone = position.Y >= contentHeight - FullScreenHotZoneHeight;
        _fullScreenPointerOverControls = IsPointerWithinFullScreenControlsBand(position, contentHeight);

        if (!_fullScreenControlsVisible && inHotZone)
        {
            // Mouse entered bottom hot zone — reveal controls
            ShowFullScreenControls();
            StartFullScreenAutoHideTimer();
        }
        else if (_fullScreenControlsVisible && (inHotZone || _fullScreenPointerOverControls))
        {
            // Mouse still near bottom or over the controls — keep them alive
            ResetFullScreenAutoHideTimer();
        }
    }

    private void OnFullScreenControlsPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _fullScreenPointerOverControls = true;
        ResetFullScreenAutoHideTimer();
    }

    private void OnFullScreenControlsPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint((UIElement)Content).Position;
        _fullScreenPointerOverControls = IsPointerWithinFullScreenControlsBand(position);
        if (!_isWindowClosing && _isFullScreen && _fullScreenControlsVisible)
        {
            ResetFullScreenAutoHideTimer();
        }
    }

    private bool IsPointerWithinFullScreenControlsBand(
        Windows.Foundation.Point position,
        double? contentHeight = null)
    {
        if (FullScreenControlsOverlay.Visibility != Visibility.Visible)
        {
            return false;
        }

        var height = FullScreenControlsOverlay.ActualHeight;
        if (height <= 0)
        {
            return false;
        }

        var visibleContentHeight = contentHeight ?? ((FrameworkElement)Content).ActualHeight;
        return position.Y >= visibleContentHeight - height - 8;
    }

    private void StartFullScreenAutoHideTimer()
    {
        if (_fullScreenAutoHideTimer == null)
        {
            _fullScreenAutoHideTimer = DispatcherQueue.CreateTimer();
            _fullScreenAutoHideTimer.Interval = TimeSpan.FromMilliseconds(FullScreenAutoHideDelayMs);
            _fullScreenAutoHideTimer.IsRepeating = false;
            _fullScreenAutoHideTimer.Tick += (_, _) => HideFullScreenControls();
        }
        _fullScreenAutoHideTimer.Start();
    }

    private void ResetFullScreenAutoHideTimer()
    {
        _fullScreenAutoHideTimer?.Stop();
        _fullScreenAutoHideTimer?.Start();
    }

    private void StopFullScreenAutoHideTimer()
    {
        _fullScreenAutoHideTimer?.Stop();
        _fullScreenAutoHideTimer = null;
    }
    #endregion
}
