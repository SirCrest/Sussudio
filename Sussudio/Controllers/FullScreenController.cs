using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FullScreenControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Grid RootGrid { get; init; }
    public required UIElement RootElement { get; init; }
    public required FrameworkElement RootFrameworkElement { get; init; }
    public required Border PreviewBorder { get; init; }
    public required Border PreviewShadowHost { get; init; }
    public required Grid PreviewContentGrid { get; init; }
    public required Border VideoShadowHost { get; init; }
    public required Border SettingsOverlayPanel { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required Border FlashbackTimelinePanel { get; init; }
    public required Border ControlBarShadowHost { get; init; }
    public required Border ControlBarBorder { get; init; }
    public required StackPanel FullScreenControlsOverlay { get; init; }
    public required Button FullScreenButton { get; init; }
    public required FontIcon FullScreenButtonIcon { get; init; }
    public required MenuFlyoutItem FullScreenMenuItem { get; init; }
    public required Func<AppWindow> GetAppWindow { get; init; }
    public required Action EndFlashbackScrubForFullScreen { get; init; }
    public required Action ResetFlashbackTimelineAnimation { get; init; }
    public required Action ResetSettingsShelfAnimation { get; init; }
    public required Func<bool> ShouldShowFlashbackTimeline { get; init; }
    public required Action<bool> SyncFlashbackTimelineToggle { get; init; }
    public required Action HideStatsDockPanelImmediate { get; init; }
    public required Action ShowStatsDockPanel { get; init; }
    public required Action UpdateVideoContentOverlays { get; init; }
    public required Action FadeInVideoShadow { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
}

internal sealed class FullScreenController
{
    private const int AutoHideDelayMs = 3000;
    private const double HotZoneHeight = 150;

    private readonly FullScreenControllerContext _context;
    private bool _isFullScreen;
    private bool _isTransitioning;
    private Windows.Graphics.RectInt32 _preFullScreenBounds;
    private Windows.Graphics.PointInt32 _preFullScreenPosition;
    private bool _preFullScreenSettingsVisible;
    private bool _preFullScreenStatsDockVisible;
    private bool _controlsVisible;
    private DispatcherQueueTimer? _autoHideTimer;
    private bool _pointerOverControls;
    private Brush? _preFullScreenControlBarBackground;
    private Brush? _preFullScreenFlashbackTimelineBackground;

    public FullScreenController(FullScreenControllerContext context)
    {
        _context = context;
    }

    public bool IsFullScreen => _isFullScreen;

    public void Toggle()
    {
        if (_isFullScreen)
        {
            Exit();
        }
        else
        {
            Enter();
        }
    }

    public Task SetEnabledAsync(bool enabled)
        => enabled ? EnterAsync() : ExitAsync();

    public void Enter()
        => _ = RunTransitionAsync(EnterAsync, "enter");

    public void Exit()
        => _ = RunTransitionAsync(ExitAsync, "exit");

    public async Task EnterAsync()
    {
        if (_isTransitioning || _isFullScreen) return;
        _isTransitioning = true;

        var appWindow = _context.GetAppWindow();
        _preFullScreenPosition = appWindow.Position;
        _preFullScreenBounds = new Windows.Graphics.RectInt32(
            appWindow.Position.X, appWindow.Position.Y,
            appWindow.Size.Width, appWindow.Size.Height);
        _preFullScreenSettingsVisible = _context.SettingsOverlayPanel.Visibility == Visibility.Visible;
        _preFullScreenStatsDockVisible = _context.StatsDockPanel.Visibility == Visibility.Visible;

        var transform = _context.PreviewBorder.TransformToVisual(_context.RootElement);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = _context.PreviewBorder.ActualWidth;
        var preH = _context.PreviewBorder.ActualHeight;

        _context.EndFlashbackScrubForFullScreen();

        PrepareChromeForOverlay();
        ApplyFullScreenChromeMaterials();

        _context.RootGrid.Children.Remove(_context.FlashbackTimelinePanel);
        _context.FullScreenControlsOverlay.Children.Insert(0, _context.FlashbackTimelinePanel);

        _context.RootGrid.Children.Remove(_context.ControlBarShadowHost);
        _context.ControlBarShadowHost.Visibility = Visibility.Collapsed;

        _context.RootGrid.Children.Remove(_context.ControlBarBorder);
        _context.ControlBarBorder.Visibility = Visibility.Visible;
        _context.FullScreenControlsOverlay.Children.Add(_context.ControlBarBorder);

        _context.FullScreenControlsOverlay.Visibility = Visibility.Visible;
        var overlayVisual = ElementCompositionPreview.GetElementVisual(_context.FullScreenControlsOverlay);
        overlayVisual.Properties.InsertVector3("Translation", new Vector3(0, 300, 0));
        overlayVisual.Opacity = 0;
        _context.FullScreenControlsOverlay.IsHitTestVisible = false;
        _controlsVisible = false;
        _pointerOverControls = false;

        if (_preFullScreenSettingsVisible)
        {
            _context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;
            _context.SettingsOverlayPanel.Height = double.NaN;
            _context.SettingsOverlayPanel.Opacity = 1;
            _context.ResetSettingsShelfAnimation();
        }

        if (_preFullScreenStatsDockVisible)
        {
            _context.HideStatsDockPanelImmediate();
        }

        _context.PreviewBorder.Margin = new Thickness(0);
        _context.PreviewShadowHost.Margin = new Thickness(0);
        _context.PreviewShadowHost.CornerRadius = new CornerRadius(0);

        _context.RootGrid.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        _context.VideoShadowHost.Visibility = Visibility.Collapsed;

        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

        await WaitForSizeChangedAsync(_context.PreviewContentGrid, 200).ConfigureAwait(true);

        var postTransform = _context.PreviewBorder.TransformToVisual(_context.RootElement);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = _context.PreviewBorder.ActualWidth;
        var postH = _context.PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            await AnimateFullScreenRectAsync(
                prePosition, preW, preH,
                postPosition, postW, postH,
                () =>
                {
                    _isFullScreen = true;
                    _isTransitioning = false;
                    UpdateButtonState();
                }).ConfigureAwait(true);
        }
        else
        {
            _isFullScreen = true;
            _isTransitioning = false;
            UpdateButtonState();
        }
    }

    public async Task ExitAsync()
    {
        if (_isTransitioning || !_isFullScreen) return;
        _isTransitioning = true;

        StopAutoHideTimer();
        var overlayVisual = ElementCompositionPreview.GetElementVisual(_context.FullScreenControlsOverlay);
        overlayVisual.StopAnimation("Translation");
        overlayVisual.StopAnimation("Opacity");
        overlayVisual.Properties.InsertVector3("Translation", Vector3.Zero);
        overlayVisual.Opacity = 1;

        var timelineVisibleAtExit = _context.ShouldShowFlashbackTimeline();
        PrepareChromeForOverlay();
        _context.FullScreenControlsOverlay.Children.Remove(_context.FlashbackTimelinePanel);
        _context.FlashbackTimelinePanel.Visibility = Visibility.Collapsed;
        _context.RootGrid.Children.Add(_context.FlashbackTimelinePanel);

        _context.FullScreenControlsOverlay.Children.Remove(_context.ControlBarBorder);
        _context.ControlBarBorder.Visibility = Visibility.Collapsed;
        _context.FullScreenControlsOverlay.Visibility = Visibility.Collapsed;
        _controlsVisible = false;

        _context.RootGrid.Children.Add(_context.ControlBarShadowHost);
        _context.RootGrid.Children.Add(_context.ControlBarBorder);

        var transform = _context.PreviewBorder.TransformToVisual(_context.RootElement);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = _context.PreviewBorder.ActualWidth;
        var preH = _context.PreviewBorder.ActualHeight;

        var appWindow = _context.GetAppWindow();
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        appWindow.Move(_preFullScreenPosition);
        appWindow.Resize(new Windows.Graphics.SizeInt32(_preFullScreenBounds.Width, _preFullScreenBounds.Height));

        _context.PreviewBorder.Margin = new Thickness(12, 6, 12, 6);
        _context.PreviewShadowHost.Margin = new Thickness(16);
        _context.PreviewShadowHost.CornerRadius = new CornerRadius(4);

        _context.ControlBarBorder.Visibility = Visibility.Visible;
        _context.ControlBarShadowHost.Visibility = Visibility.Visible;
        RestoreWindowedChromeMaterials();
        if (_preFullScreenSettingsVisible)
        {
            _context.SettingsOverlayPanel.Visibility = Visibility.Visible;
        }

        if (_preFullScreenStatsDockVisible)
        {
            _context.ShowStatsDockPanel();
        }

        if (timelineVisibleAtExit)
        {
            _context.FlashbackTimelinePanel.Visibility = Visibility.Visible;
        }

        _context.RootGrid.Background = null;

        await WaitForSizeChangedAsync(_context.PreviewContentGrid, 200).ConfigureAwait(true);

        var postTransform = _context.PreviewBorder.TransformToVisual(_context.RootElement);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = _context.PreviewBorder.ActualWidth;
        var postH = _context.PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            await AnimateFullScreenRectAsync(
                prePosition, preW, preH,
                postPosition, postW, postH,
                CompleteExit).ConfigureAwait(true);
        }
        else
        {
            CompleteExit();
        }
    }

    public void OnPointerActivity(PointerRoutedEventArgs e)
    {
        if (_context.IsWindowClosing() || !_isFullScreen || _isTransitioning) return;

        var position = e.GetCurrentPoint(_context.RootElement).Position;
        var contentHeight = _context.RootFrameworkElement.ActualHeight;
        var inHotZone = position.Y >= contentHeight - HotZoneHeight;
        _pointerOverControls = IsPointerWithinControlsBand(position, contentHeight);

        if (!_controlsVisible && inHotZone)
        {
            ShowControls();
            StartAutoHideTimer();
        }
        else if (_controlsVisible && (inHotZone || _pointerOverControls))
        {
            ResetAutoHideTimer();
        }
    }

    public void OnControlsPointerEntered()
    {
        _pointerOverControls = true;
        ResetAutoHideTimer();
    }

    public void OnControlsPointerExited(PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint(_context.RootElement).Position;
        _pointerOverControls = IsPointerWithinControlsBand(position);
        if (!_context.IsWindowClosing() && _isFullScreen && _controlsVisible)
        {
            ResetAutoHideTimer();
        }
    }

    public void StopAutoHideTimer()
    {
        _autoHideTimer?.Stop();
        _autoHideTimer = null;
    }

    private static async Task RunTransitionAsync(Func<Task> transition, string operation)
    {
        try
        {
            await transition().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.Log($"FULLSCREEN_TRANSITION_FAIL operation={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void CompleteExit()
    {
        _isFullScreen = false;
        _isTransitioning = false;
        UpdateButtonState();
        _context.UpdateVideoContentOverlays();
        _context.VideoShadowHost.Visibility = Visibility.Visible;
        _context.FadeInVideoShadow();
    }

    private void PrepareChromeForOverlay()
    {
        _context.ResetFlashbackTimelineAnimation();

        _context.ControlBarBorder.Opacity = 1;
        if (_context.ControlBarBorder.RenderTransform is TranslateTransform controlBarTranslate)
        {
            controlBarTranslate.Y = 0;
        }

        var showTimeline = _context.ShouldShowFlashbackTimeline();
        _context.FlashbackTimelinePanel.Visibility = showTimeline ? Visibility.Visible : Visibility.Collapsed;
        _context.FlashbackTimelinePanel.Height = double.NaN;
        _context.FlashbackTimelinePanel.Opacity = 1;
        _context.FlashbackTimelinePanel.IsHitTestVisible = _context.ViewModel.IsFlashbackEnabled;
        _context.SyncFlashbackTimelineToggle(showTimeline);
    }

    private void ApplyFullScreenChromeMaterials()
    {
        _preFullScreenControlBarBackground ??= _context.ControlBarBorder.Background;
        _preFullScreenFlashbackTimelineBackground ??= _context.FlashbackTimelinePanel.Background;

        _context.ControlBarBorder.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x14, 0x14, 0x14));
        _context.FlashbackTimelinePanel.Background = new SolidColorBrush(
            Windows.UI.Color.FromArgb(0xF2, 0x20, 0x20, 0x20));
    }

    private void RestoreWindowedChromeMaterials()
    {
        if (_preFullScreenControlBarBackground != null)
        {
            _context.ControlBarBorder.Background = _preFullScreenControlBarBackground;
            _preFullScreenControlBarBackground = null;
        }

        if (_preFullScreenFlashbackTimelineBackground != null)
        {
            _context.FlashbackTimelinePanel.Background = _preFullScreenFlashbackTimelineBackground;
            _preFullScreenFlashbackTimelineBackground = null;
        }
    }

    private Task AnimateFullScreenRectAsync(
        Windows.Foundation.Point prePos, double preW, double preH,
        Windows.Foundation.Point postPos, double postW, double postH,
        Action onCompleted)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scaleX = (float)(preW / postW);
        var scaleY = (float)(preH / postH);
        var offsetX = (float)(prePos.X - postPos.X + (preW - postW) / 2);
        var offsetY = (float)(prePos.Y - postPos.Y + (preH - postH) / 2);

        var visual = ElementCompositionPreview.GetElementVisual(_context.PreviewBorder);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3((float)(postW / 2), (float)(postH / 2), 0);

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

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        props.StartAnimation("Progress", progressAnim);
        batch.End();

        visual.StartAnimation("Scale", scaleExpr);
        visual.StartAnimation("Offset", offsetExpr);

        batch.Completed += (_, _) =>
        {
            if (!_context.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    visual.StopAnimation("Scale");
                    visual.StopAnimation("Offset");
                    visual.Scale = Vector3.One;
                    visual.Offset = Vector3.Zero;
                    visual.CenterPoint = Vector3.Zero;
                    onCompleted();
                    completion.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }))
            {
                completion.TrySetException(new InvalidOperationException("Failed to enqueue full-screen animation completion on the UI thread."));
            }
        };

        return completion.Task;
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

    private void UpdateButtonState()
    {
        if (_isFullScreen)
        {
            _context.FullScreenButtonIcon.Glyph = "\uE73F";
            ToolTipService.SetToolTip(_context.FullScreenButton, "Exit full screen");
            _context.FullScreenMenuItem.Text = "Exit Full Screen";
            if (_context.FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE73F";
        }
        else
        {
            _context.FullScreenButtonIcon.Glyph = "\uE740";
            ToolTipService.SetToolTip(_context.FullScreenButton, "Full screen");
            _context.FullScreenMenuItem.Text = "Enter Full Screen";
            if (_context.FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE740";
        }
    }

    private void ShowControls()
    {
        if (_controlsVisible) return;
        _controlsVisible = true;
        _context.FullScreenControlsOverlay.IsHitTestVisible = true;

        var visual = ElementCompositionPreview.GetElementVisual(_context.FullScreenControlsOverlay);
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

    private void HideControls()
    {
        if (!_controlsVisible) return;
        if (_pointerOverControls) return;
        _controlsVisible = false;

        var visual = ElementCompositionPreview.GetElementVisual(_context.FullScreenControlsOverlay);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));

        var hideOffset = (float)Math.Max(200, _context.FullScreenControlsOverlay.ActualHeight + 20);

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
            _context.DispatcherQueue.TryEnqueue(() =>
            {
                if (!_controlsVisible)
                {
                    _context.FullScreenControlsOverlay.IsHitTestVisible = false;
                }
            });
        };
    }

    private bool IsPointerWithinControlsBand(
        Windows.Foundation.Point position,
        double? contentHeight = null)
    {
        if (_context.FullScreenControlsOverlay.Visibility != Visibility.Visible)
        {
            return false;
        }

        var height = _context.FullScreenControlsOverlay.ActualHeight;
        if (height <= 0)
        {
            return false;
        }

        var visibleContentHeight = contentHeight ?? _context.RootFrameworkElement.ActualHeight;
        return position.Y >= visibleContentHeight - height - 8;
    }

    private void StartAutoHideTimer()
    {
        if (_autoHideTimer == null)
        {
            _autoHideTimer = _context.DispatcherQueue.CreateTimer();
            _autoHideTimer.Interval = TimeSpan.FromMilliseconds(AutoHideDelayMs);
            _autoHideTimer.IsRepeating = false;
            _autoHideTimer.Tick += (_, _) => HideControls();
        }
        _autoHideTimer.Start();
    }

    private void ResetAutoHideTimer()
    {
        _autoHideTimer?.Stop();
        _autoHideTimer?.Start();
    }
}
