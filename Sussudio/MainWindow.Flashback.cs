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
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Flashback timeline UI. The controls here change presentation state only;
// live capture and the continuous Flashback encoder keep running in CaptureService.
public sealed partial class MainWindow
{
    #region Flashback Timeline
    private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var w = e.NewSize.Width;
        var h = e.NewSize.Height;

        // Size elements that fill the track
        FlashbackTrackBackground.Width = w;
        FlashbackTrackBackground.Height = h;
        FlashbackScrubArea.Width = w;
        FlashbackScrubArea.Height = h;
        FlashbackPlayhead.Height = h;
        FlashbackLiveEdge.Height = h;

        // Live edge at right
        Canvas.SetLeft(FlashbackLiveEdge, w - 2);

        // Track resized — playhead must jump to its new layout-correct position
        // without sweeping through a stale Translation.X from the old width.
        _snapFlashbackPlayheadOnNextUpdate = true;

        // Re-layout current positions
        UpdateFlashbackPositionUI();
        UpdateFlashbackMarkers();
        RefreshFlashbackCtiMotion("size_changed");
    }
    private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
    {
        // Pass the visual playhead position (FlashbackPlaybackPosition is set by
        // the timer to controller.PlaybackPosition during Playing, and by the
        // PointerMoved handler to fraction*bufferDuration during Scrubbing).
        // The parameterless overload reads controller.PlaybackPosition which is
        // keyframe-snapped — clicking In mid-GOP would otherwise land hundreds of
        // milliseconds before where the user is pointing.
        var pos = ViewModel.FlashbackSetInPointAt(ViewModel.FlashbackPlaybackPosition);
        if (pos.HasValue)
        {
            ViewModel.FlashbackInPoint = pos.Value;
            Logger.Log($"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}");
        }
        else
        {
            ViewModel.ReportFlashbackPlaybackRejection("set in point", "FLASHBACK_UI_SET_IN_REJECTED");
        }
    }
    private void FlashbackOutButton_Click(object sender, RoutedEventArgs e)
    {
        var pos = ViewModel.FlashbackSetOutPointAt(ViewModel.FlashbackPlaybackPosition);
        if (pos.HasValue)
        {
            ViewModel.FlashbackOutPoint = pos.Value;
            Logger.Log($"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}");
        }
        else
        {
            ViewModel.ReportFlashbackPlaybackRejection("set out point", "FLASHBACK_UI_SET_OUT_REJECTED");
        }
    }
    private void FlashbackClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.FlashbackClearInOutPoints())
        {
            ViewModel.ReportFlashbackPlaybackRejection("clear in/out", "FLASHBACK_UI_CLEAR_INOUT_REJECTED");
            return;
        }
        ViewModel.FlashbackInPoint = null;
        ViewModel.FlashbackOutPoint = null;
        Logger.Log("FLASHBACK_UI_CLEAR_INOUT");
    }
    private void FlashbackPlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        var state = ViewModel.FlashbackState;
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live)
        {
            if (!ViewModel.FlashbackPause())
            {
                ViewModel.ReportFlashbackPlaybackRejection("pause", "FLASHBACK_UI_PAUSE_REJECTED");
            }
            else
            {
                Logger.Log("FLASHBACK_UI_PAUSE");
            }
        }
        else if (state == FlashbackPlaybackState.Paused || state == FlashbackPlaybackState.Scrubbing)
        {
            if (!ViewModel.FlashbackPlay())
            {
                ViewModel.ReportFlashbackPlaybackRejection("play", "FLASHBACK_UI_PLAY_REJECTED");
            }
            else
            {
                Logger.Log("FLASHBACK_UI_PLAY");
            }
        }
    }
    private void FlashbackGoLiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.FlashbackGoLive())
        {
            ViewModel.ReportFlashbackPlaybackRejection("go live", "FLASHBACK_UI_GOLIVE_REJECTED");
        }
        else
        {
            Logger.Log("FLASHBACK_UI_GOLIVE");
        }
    }
    private void FlashbackExportButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.ExportFlashbackAsync(), nameof(FlashbackExportButton_Click));
    }
    private void FlashbackSaveLast5mButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.SaveFlashbackLast5mAsync(), nameof(FlashbackSaveLast5mButton_Click));
    }
    private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressFlashbackEnabledToggle)
        {
            return;
        }

        var requestedEnabled = FlashbackEnabledToggle.IsOn;
        _ = RunUiEventHandlerAsync(
            () => ApplyFlashbackEnabledToggleAsync(requestedEnabled),
            nameof(FlashbackEnabledToggle_Toggled));
    }

    private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)
    {
        var previousEnabled = ViewModel.IsFlashbackEnabled;
        ViewModel.IsFlashbackEnabled = requestedEnabled;
        try
        {
            await ViewModel.SetFlashbackEnabledAsync(requestedEnabled);
        }
        catch
        {
            ViewModel.IsFlashbackEnabled = previousEnabled;
            _suppressFlashbackEnabledToggle = true;
            try
            {
                FlashbackEnabledToggle.IsOn = previousEnabled;
            }
            finally
            {
                _suppressFlashbackEnabledToggle = false;
            }
            throw;
        }
    }
    private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.RestartFlashbackAsync(), nameof(FlashbackApplyButton_Click));
    }
    private void UpdateFlashbackStateUI()
    {
        var state = ViewModel.FlashbackState;
        _flashbackPlaybackPresentationController.UpdateState(state);

        // Keep the 30Hz playback timer running during Playing \u2014 its writes to
        // FlashbackPlaybackPosition still feed the floating-label text and any
        // VM consumers. The CTI visual is no longer driven by these writes;
        // it is driven by the long-horizon extrapolation re-anchored on state
        // edges (which we are at now).
        if (state == FlashbackPlaybackState.Playing)
            StartFlashbackPlaybackPolling();
        else
            StopFlashbackPlaybackPolling();

        RefreshFlashbackCtiMotion("state_change");
    }
    private void UpdateFlashbackBufferFill()
    {
        var duration = ViewModel.FlashbackBufferFilledDuration;
        _flashbackPlaybackPresentationController.UpdateBufferFill(duration);
    }
    private static string FormatDiskSize(long bytes)
    {
        const double scale = 1024;
        double value = Math.Max(0, bytes);
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var unit = 0;
        while (value >= scale && unit < units.Length - 1)
        {
            value /= scale;
            unit++;
        }
        return unit >= 3 ? $"{value:F1} {units[unit]}" : $"{Math.Round(value):0} {units[unit]}";
    }
    // Position-changed handler. The VISUAL motion is driven by
    // RefreshFlashbackCtiMotion; this method only refreshes the floating
    // label text (gap-from-live / total). For Paused/Live states a position
    // change implies a seek or scrub-end, so we also trigger a re-anchor —
    // during Playing the 30Hz tick stream would re-anchor 30 times per second
    // (defeating the whole point), so we deliberately skip re-anchor there
    // and let the extrapolation + 1Hz drift correction handle it.
    private void UpdateFlashbackPositionUI()
    {
        var state = ViewModel.FlashbackState;
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        _flashbackPlaybackPresentationController.UpdatePosition(
            state,
            bufferDuration,
            ViewModel.FlashbackGapFromLive);

        if (!_isFlashbackScrubbing
            && state != FlashbackPlaybackState.Playing
            && state != FlashbackPlaybackState.Scrubbing)
        {
            RefreshFlashbackCtiMotion("position_change");
        }
    }
    #endregion

}
