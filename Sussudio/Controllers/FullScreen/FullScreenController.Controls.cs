using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;

namespace Sussudio.Controllers;

internal sealed partial class FullScreenController
{
    public void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)
        {
            e.Handled = true;
            Exit();
            return;
        }

        HandleFlashbackKeyDown(e);
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

    private void HandleFlashbackKeyDown(KeyRoutedEventArgs e)
    {
        if (!_context.ViewModel.IsFlashbackEnabled || _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)
        {
            return;
        }

        if (_context.HandleFlashbackKeyboardCommand(e.Key))
        {
            e.Handled = true;
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
