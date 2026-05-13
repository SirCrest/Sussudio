using System;
using Microsoft.UI.Dispatching;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackPollingControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
}

internal sealed class FlashbackPollingController
{
    private readonly FlashbackPollingControllerContext _context;
    private DispatcherQueueTimer? _statusTimer;
    private DispatcherQueueTimer? _playbackTimer;

    public FlashbackPollingController(FlashbackPollingControllerContext context)
    {
        _context = context;
    }

    public void StartStatusPolling()
    {
        _statusTimer ??= _context.DispatcherQueue.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromMilliseconds(250);
        _statusTimer.IsRepeating = true;
        _statusTimer.Tick -= StatusTimer_Tick;
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();
    }

    public void StopStatusPolling()
    {
        if (_statusTimer is null)
        {
            return;
        }

        _statusTimer.Stop();
        _statusTimer.Tick -= StatusTimer_Tick;
        StopPlaybackPolling();
    }

    public void StartPlaybackPolling()
    {
        _playbackTimer ??= _context.DispatcherQueue.CreateTimer();
        if (_playbackTimer.IsRunning)
        {
            return;
        }

        _playbackTimer.Interval = TimeSpan.FromMilliseconds(33);
        _playbackTimer.IsRepeating = true;
        _playbackTimer.Tick -= PlaybackTimer_Tick;
        _playbackTimer.Tick += PlaybackTimer_Tick;
        _playbackTimer.Start();
    }

    public void StopPlaybackPolling()
    {
        if (_playbackTimer is null)
        {
            return;
        }

        _playbackTimer.Stop();
        _playbackTimer.Tick -= PlaybackTimer_Tick;
    }

    private void StatusTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_context.IsWindowClosing())
            {
                return;
            }

            _context.ViewModel.UpdateFlashbackBufferStatus();
        }
        catch (Exception ex)
        {
            Sussudio.Logger.Log($"FLASHBACK_STATUS_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void PlaybackTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_context.IsWindowClosing())
            {
                return;
            }

            var playback = _context.ViewModel.GetFlashbackPlaybackSnapshot();
            if (!playback.IsActive || playback.State != FlashbackPlaybackState.Playing)
            {
                StopPlaybackPolling();
                return;
            }

            _context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;
        }
        catch (Exception ex)
        {
            Sussudio.Logger.Log($"FLASHBACK_PLAYBACK_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
            StopPlaybackPolling();
        }
    }
}
