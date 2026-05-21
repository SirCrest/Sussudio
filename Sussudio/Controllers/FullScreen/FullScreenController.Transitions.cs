using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Sussudio.Controllers;

internal sealed partial class FullScreenController
{
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

        var timelineVisibleAtExit = ShouldShowFlashbackTimeline();
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
}
