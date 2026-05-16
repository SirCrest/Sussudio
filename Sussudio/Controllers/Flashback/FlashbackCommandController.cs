using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
using Sussudio.ViewModels;
using Windows.System;

namespace Sussudio.Controllers;

internal sealed class FlashbackCommandControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleSwitch FlashbackEnabledToggle { get; init; }
    public required Func<Func<Task>, string, Task> RunUiEventHandlerAsync { get; init; }
}

internal sealed class FlashbackCommandController
{
    private readonly FlashbackCommandControllerContext _context;
    private bool _suppressFlashbackEnabledToggle;

    public FlashbackCommandController(FlashbackCommandControllerContext context)
    {
        _context = context;
    }

    public void SetInPointAtPlayhead()
    {
        // Pass the visual playhead position (FlashbackPlaybackPosition is set by
        // the timer to controller.PlaybackPosition during Playing, and by the
        // PointerMoved handler to fraction*bufferDuration during Scrubbing).
        // The parameterless overload reads controller.PlaybackPosition which is
        // keyframe-snapped; clicking In mid-GOP would otherwise land hundreds of
        // milliseconds before where the user is pointing.
        var pos = _context.ViewModel.FlashbackSetInPointAt(_context.ViewModel.FlashbackPlaybackPosition);
        if (pos.HasValue)
        {
            _context.ViewModel.FlashbackInPoint = pos.Value;
            Sussudio.Logger.Log($"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}");
        }
        else
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("set in point", "FLASHBACK_UI_SET_IN_REJECTED");
        }
    }

    public void SetOutPointAtPlayhead()
    {
        var pos = _context.ViewModel.FlashbackSetOutPointAt(_context.ViewModel.FlashbackPlaybackPosition);
        if (pos.HasValue)
        {
            _context.ViewModel.FlashbackOutPoint = pos.Value;
            Sussudio.Logger.Log($"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}");
        }
        else
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("set out point", "FLASHBACK_UI_SET_OUT_REJECTED");
        }
    }

    public void ClearInOutPoints()
    {
        if (!_context.ViewModel.FlashbackClearInOutPoints())
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("clear in/out", "FLASHBACK_UI_CLEAR_INOUT_REJECTED");
            return;
        }

        _context.ViewModel.FlashbackInPoint = null;
        _context.ViewModel.FlashbackOutPoint = null;
        Sussudio.Logger.Log("FLASHBACK_UI_CLEAR_INOUT");
    }

    public void TogglePlayPause()
    {
        var state = _context.ViewModel.FlashbackState;
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live)
        {
            if (!_context.ViewModel.FlashbackPause())
            {
                _context.ViewModel.ReportFlashbackPlaybackRejection("pause", "FLASHBACK_UI_PAUSE_REJECTED");
            }
            else
            {
                Sussudio.Logger.Log("FLASHBACK_UI_PAUSE");
            }
        }
        else if (state == FlashbackPlaybackState.Paused || state == FlashbackPlaybackState.Scrubbing)
        {
            if (!_context.ViewModel.FlashbackPlay())
            {
                _context.ViewModel.ReportFlashbackPlaybackRejection("play", "FLASHBACK_UI_PLAY_REJECTED");
            }
            else
            {
                Sussudio.Logger.Log("FLASHBACK_UI_PLAY");
            }
        }
    }

    public void GoLive()
    {
        if (!_context.ViewModel.FlashbackGoLive())
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("go live", "FLASHBACK_UI_GOLIVE_REJECTED");
        }
        else
        {
            Sussudio.Logger.Log("FLASHBACK_UI_GOLIVE");
        }
    }

    public bool HandleFullScreenKeyboardCommand(VirtualKey key)
    {
        switch (key)
        {
            case VirtualKey.I:
                SetInPointAtPlayhead();
                return true;
            case VirtualKey.O:
                SetOutPointAtPlayhead();
                return true;
            case VirtualKey.Space:
                TogglePlayPause();
                return true;
            case VirtualKey.L:
                GoLive();
                return true;
            case VirtualKey.Left:
                NudgePlayback(TimeSpan.FromSeconds(-1), "nudge left", "FLASHBACK_UI_NUDGE_REJECTED direction=left");
                return true;
            case VirtualKey.Right:
                NudgePlayback(TimeSpan.FromSeconds(1), "nudge right", "FLASHBACK_UI_NUDGE_REJECTED direction=right");
                return true;
            default:
                return false;
        }
    }

    private void NudgePlayback(TimeSpan offset, string operationName, string rejectionDetail)
    {
        if (!_context.ViewModel.FlashbackNudge(offset))
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection(operationName, rejectionDetail);
        }
    }

    public void Export(string operationName)
        => _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.ExportFlashbackAsync(), operationName);

    public void SaveLast5m(string operationName)
        => _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.SaveFlashbackLast5mAsync(), operationName);

    public void ApplySettings(string operationName)
        => _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.RestartFlashbackAsync(), operationName);

    public void ToggleEnabled(string operationName)
    {
        if (_suppressFlashbackEnabledToggle)
        {
            return;
        }

        var requestedEnabled = _context.FlashbackEnabledToggle.IsOn;
        _ = _context.RunUiEventHandlerAsync(
            () => ApplyFlashbackEnabledToggleAsync(requestedEnabled),
            operationName);
    }

    private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)
    {
        var previousEnabled = _context.ViewModel.IsFlashbackEnabled;
        _context.ViewModel.IsFlashbackEnabled = requestedEnabled;
        try
        {
            await _context.ViewModel.SetFlashbackEnabledAsync(requestedEnabled);
        }
        catch
        {
            _context.ViewModel.IsFlashbackEnabled = previousEnabled;
            _suppressFlashbackEnabledToggle = true;
            try
            {
                _context.FlashbackEnabledToggle.IsOn = previousEnabled;
            }
            finally
            {
                _suppressFlashbackEnabledToggle = false;
            }

            throw;
        }
    }
}
