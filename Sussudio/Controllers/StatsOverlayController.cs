using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsOverlayControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required ToggleButton StatsToggle { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required FrameworkElement FrameTimeOverlay { get; init; }
    public required ToggleButton FrameTimeOverlayToggle { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Action<bool> SetStatsVisible { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required Action UpdateStatsDock { get; init; }
    public required Action<StatsSnapshot> UpdateFrameTimeOverlay { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed partial class StatsOverlayController
{
    private readonly StatsOverlayControllerContext _context;
    private DispatcherQueueTimer? _statsPollTimer;

    public StatsOverlayController(StatsOverlayControllerContext context)
    {
        _context = context;
    }

    public bool IsFrameTimeOverlayVisible
        => _context.FrameTimeOverlay.Visibility == Visibility.Visible;

    public void HandleStatsToggleChecked()
    {
        if (_context.IsWindowClosing())
        {
            return;
        }

        _context.SetStatsVisible(true);
    }

    public void HandleStatsToggleUnchecked()
        => _context.SetStatsVisible(false);

    public void SyncStatsVisibility(bool visible, bool immediate = false)
    {
        if (_context.StatsToggle.IsChecked != visible)
        {
            _context.StatsToggle.IsChecked = visible;
        }

        ApplyStatsVisibility(visible, immediate);
    }

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

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}
