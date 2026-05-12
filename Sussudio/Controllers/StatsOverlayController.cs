using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsOverlayControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required FrameworkElement FrameTimeOverlay { get; init; }
    public required ToggleButton FrameTimeOverlayToggle { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required Action UpdateStatsDock { get; init; }
    public required Action<StatsSnapshot> UpdateFrameTimeOverlay { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed class StatsOverlayController
{
    private const double StatsDockPanelWidth = 360;

    private readonly StatsOverlayControllerContext _context;
    private DispatcherQueueTimer? _statsPollTimer;
    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;

    public StatsOverlayController(StatsOverlayControllerContext context)
    {
        _context = context;
    }

    public bool IsFrameTimeOverlayVisible
        => _context.FrameTimeOverlay.Visibility == Visibility.Visible;

    public void ApplyStatsVisibility(bool visible, bool immediate = false)
    {
        if (visible)
        {
            ShowDockPanel();
            _context.UpdateStatsDock();
            StartPolling();
            return;
        }

        if (!IsFrameTimeOverlayVisible)
        {
            StopPolling();
        }

        HideDockPanel(immediate);
    }

    public void SetFrameTimeOverlayVisible(bool visible)
    {
        if (_context.FrameTimeOverlayToggle.IsChecked != visible)
        {
            _context.FrameTimeOverlayToggle.IsChecked = visible;
        }

        if (visible)
        {
            SetVisibilityIfChanged(_context.FrameTimeOverlay, Visibility.Visible);
            StartPolling();
            _context.UpdateFrameTimeOverlay(_context.GetStatsSnapshot());
            return;
        }

        SetVisibilityIfChanged(_context.FrameTimeOverlay, Visibility.Collapsed);
        if (_context.StatsDockPanel.Visibility != Visibility.Visible)
        {
            StopPolling();
        }
    }

    public void StartPolling()
    {
        _statsPollTimer ??= _context.DispatcherQueue.CreateTimer();
        _statsPollTimer.Interval = TimeSpan.FromMilliseconds(500);
        _statsPollTimer.IsRepeating = true;
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer.Tick += StatsPollTimer_Tick;
        _statsPollTimer.Start();
    }

    public void StopPolling()
    {
        if (_statsPollTimer == null)
        {
            return;
        }

        _statsPollTimer.Stop();
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer = null;
    }

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

    private void StatsPollTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            _context.UpdateStatsDock();
            if (IsFrameTimeOverlayVisible)
            {
                _context.UpdateFrameTimeOverlay(_context.GetStatsSnapshot());
            }
        }
        catch (Exception ex)
        {
            _context.Log($"STATS_POLL_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
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

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}
