using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed partial class StatsOverlayController
{
    private const double StatsDockPanelWidth = 360;

    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;

    public void ShowDockPanel()
    {
        EnsureDockAnimations();
        StopDockAnimation();
        _context.StatsDockPanel.Width = 0;
        _context.StatsDockPanel.Opacity = 0;
        _context.StatsDockPanel.Visibility = Visibility.Visible;
        _statsDockStoryboard = _showStatsDockStoryboard;
        _showStatsDockStoryboard?.Begin();
    }

    public void HideDockPanel(bool immediate = false)
    {
        EnsureDockAnimations();
        StopDockAnimation();
        if (immediate || _context.StatsDockPanel.Visibility != Visibility.Visible)
        {
            _context.StatsDockPanel.Width = 0;
            _context.StatsDockPanel.Visibility = Visibility.Collapsed;
            _context.StatsDockPanel.Opacity = 1;
            return;
        }

        _statsDockStoryboard = _hideStatsDockStoryboard;
        _hideStatsDockStoryboard?.Begin();
    }

    private void StopDockAnimation()
    {
        _statsDockStoryboard?.Stop();
        _statsDockStoryboard = null;
    }

    private void EnsureDockAnimations()
    {
        _showStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: true);
        _hideStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: false);
    }

    private Storyboard CreateStatsDockStoryboard(bool showing)
    {
        var durationMs = showing ? 400 : 300;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        var widthAnim = new DoubleAnimation
        {
            To = showing ? StatsDockPanelWidth : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(widthAnim, _context.StatsDockPanel);
        Storyboard.SetTargetProperty(widthAnim, "Width");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, _context.StatsDockPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(widthAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_statsDockStoryboard, storyboard))
            {
                return;
            }

            _statsDockStoryboard = null;
            if (showing)
            {
                _context.StatsDockPanel.Width = StatsDockPanelWidth;
                _context.StatsDockPanel.Opacity = 1;
                return;
            }

            _context.StatsDockPanel.Width = 0;
            _context.StatsDockPanel.Visibility = Visibility.Collapsed;
            _context.StatsDockPanel.Opacity = 1;
        };

        return storyboard;
    }
}
