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
    private long _lastScrubUpdateTick;
    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var targetPosition = ComputeFlashbackScrubPosition(e);
        if (!ViewModel.FlashbackBeginScrub(targetPosition))
        {
            _lastScrubPointerPosition = null;
            ViewModel.ReportFlashbackPlaybackRejection("scrub begin", "FLASHBACK_UI_SCRUB_BEGIN_REJECTED");
            return;
        }

        _isFlashbackScrubbing = true;
        _lastScrubPointerPosition = targetPosition;
        _lastScrubUpdateTick = 0;
        (sender as UIElement)?.CapturePointer(e.Pointer);
        UpdateFlashbackScrubVisual(e);
    }
    private void FlashbackScrubArea_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isFlashbackScrubbing) return;

        // Throttle scrub updates to ~60fps to avoid flooding the decoder
        var now = Environment.TickCount64;
        if (now - _lastScrubUpdateTick < 16) return;
        _lastScrubUpdateTick = now;

        var targetPosition = ComputeFlashbackScrubPosition(e);
        if (!ViewModel.FlashbackUpdateScrub(targetPosition))
        {
            ViewModel.ReportFlashbackPlaybackRejection("scrub update", "FLASHBACK_UI_SCRUB_UPDATE_REJECTED");
            EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "update_rejected");
            return;
        }
        _lastScrubPointerPosition = targetPosition;
        UpdateFlashbackScrubVisual(e);
    }
    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        TimeSpan? releasePosition = null;
        if (_isFlashbackScrubbing)
        {
            var targetPosition = ComputeFlashbackScrubPosition(e);
            releasePosition = targetPosition;
            _lastScrubPointerPosition = targetPosition;
            if (!ViewModel.FlashbackUpdateScrub(targetPosition))
            {
                ViewModel.ReportFlashbackPlaybackRejection("scrub release update", "FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED");
            }
            else
            {
                UpdateFlashbackScrubVisual(e);
            }
        }

        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "released", releasePosition);
    }

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CANCELED carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "cancelled", carriedPosition);
    }

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CAPTURE_LOST carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "capture_lost", carriedPosition);
    }

    private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)
    {
        if (!_isFlashbackScrubbing)
        {
            return;
        }

        _isFlashbackScrubbing = false;
        _lastScrubUpdateTick = 0;
        _lastScrubPointerPosition = null;
        element?.ReleasePointerCapture(pointer);
        var ended = releasePosition.HasValue
            ? ViewModel.FlashbackEndScrubAt(releasePosition.Value)
            : ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            ViewModel.ReportFlashbackPlaybackRejection($"scrub end ({reason})", $"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}");
        }
        Logger.Log($"FLASHBACK_UI_SCRUB_END reason={reason}");
        // Hand the visual back to the extrapolation driver from wherever the
        // pointer left it.
        RefreshFlashbackCtiMotion("scrub_end");
    }
    private void UpdateFlashbackScrubVisual(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(FlashbackScrubArea).Position;
        var width = FlashbackScrubArea.ActualWidth;
        if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return;

        var x = Math.Clamp(fraction * width, 0, width);
        // Magnetic = ease-out toward the pointer; longer than the 16ms pointer
        // throttle so successive events overlap into a single smooth trail
        // rather than 16ms-stepped jitter.
        PositionFlashbackPlayhead(x, width, FlashbackPlayheadMotion.Magnetic);

        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        if (IsUsableFlashbackDuration(bufferDuration))
        {
            ViewModel.FlashbackPlaybackPosition = TimeSpan.FromSeconds(fraction * bufferDuration.TotalSeconds);
        }
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
    private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return; // Guard: fires during XAML init before ViewModel is set
        if (FlashbackBufferDurationCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag
            && int.TryParse(tag, out var minutes))
        {
            ViewModel.FlashbackBufferMinutes = minutes;
            Logger.Log($"FLASHBACK_UI_BUFFER_DURATION_CHANGED minutes={minutes}");
        }
    }
    private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.RestartFlashbackAsync(), nameof(FlashbackApplyButton_Click));
    }
    private void UpdateFlashbackStateUI()
    {
        var state = ViewModel.FlashbackState;
        FlashbackPlayPauseIcon.Glyph = state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live ? "\uE769" : "\uE768";
        FlashbackGoLiveButton.IsEnabled = state != FlashbackPlaybackState.Live && state != FlashbackPlaybackState.Disabled;

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
        FlashbackBufferDurationText.Text = FormatFlashbackDuration(duration);
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
        var isLive = state == FlashbackPlaybackState.Live;

        if (isLive)
        {
            FlashbackPlayheadTimeText.Text = "LIVE";
        }
        else
        {
            var gapFromLive = ViewModel.FlashbackGapFromLive;
            var totalStr = FormatFlashbackDuration(bufferDuration);
            FlashbackPlayheadTimeText.Text = $"-{FormatFlashbackDuration(gapFromLive)} / {totalStr}";
        }

        if (!_isFlashbackScrubbing
            && state != FlashbackPlaybackState.Playing
            && state != FlashbackPlaybackState.Scrubbing)
        {
            RefreshFlashbackCtiMotion("position_change");
        }
    }
    private static string FormatFlashbackDuration(TimeSpan ts)
    {
        var totalMinutes = (int)ts.TotalMinutes;
        var seconds = ts.Seconds;
        return $"{totalMinutes}:{seconds:D2}";
    }
    private void UpdateFlashbackMarkers()
    {
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        var trackWidth = FlashbackScrubArea.ActualWidth;
        var trackHeight = FlashbackScrubArea.ActualHeight;
        var hasUsableTrack = IsUsableFlashbackTrackDimension(trackWidth) &&
                             IsUsableFlashbackTrackDimension(trackHeight);
        var hasUsableDuration = IsUsableFlashbackDuration(bufferDuration);

        TimeSpan? inPtVal = null, outPtVal = null;

        if (hasUsableTrack && hasUsableDuration && ViewModel.FlashbackInPoint is TimeSpan inPt)
        {
            inPtVal = inPt;
            var inX = Math.Clamp(inPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            FlashbackInPointMarker.Visibility = Visibility.Visible;
            FlashbackInPointMarker.Height = trackHeight;
            Canvas.SetLeft(FlashbackInPointMarker, inX - 1);
        }
        else
        {
            FlashbackInPointMarker.Visibility = Visibility.Collapsed;
        }

        if (hasUsableTrack && hasUsableDuration && ViewModel.FlashbackOutPoint is TimeSpan outPt)
        {
            outPtVal = outPt;
            var outX = Math.Clamp(outPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            FlashbackOutPointMarker.Visibility = Visibility.Visible;
            FlashbackOutPointMarker.Height = trackHeight;
            Canvas.SetLeft(FlashbackOutPointMarker, outX - 1);
        }
        else
        {
            FlashbackOutPointMarker.Visibility = Visibility.Collapsed;
        }

        // Selection region between in/out points
        if (inPtVal is TimeSpan inVal && outPtVal is TimeSpan outVal && hasUsableTrack && hasUsableDuration)
        {
            var inFrac = inVal.TotalSeconds / bufferDuration.TotalSeconds;
            var outFrac = outVal.TotalSeconds / bufferDuration.TotalSeconds;
            var selLeft = Math.Clamp(inFrac * trackWidth, 0, trackWidth);
            var selRight = Math.Clamp(outFrac * trackWidth, 0, trackWidth);
            FlashbackSelectionRegion.Visibility = Visibility.Visible;
            FlashbackSelectionRegion.Height = trackHeight;
            FlashbackSelectionRegion.Width = Math.Max(0, selRight - selLeft);
            Canvas.SetLeft(FlashbackSelectionRegion, selLeft);
        }
        else
        {
            FlashbackSelectionRegion.Visibility = Visibility.Collapsed;
        }
    }
    #endregion

    private TimeSpan ComputeFlashbackScrubPosition(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(FlashbackScrubArea).Position;
        var width = FlashbackScrubArea.ActualWidth;
        if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return TimeSpan.Zero;

        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        if (!IsUsableFlashbackDuration(bufferDuration)) return TimeSpan.Zero;

        return TimeSpan.FromSeconds(fraction * bufferDuration.TotalSeconds);
    }

    private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)
    {
        fraction = 0;
        if (!IsUsableFlashbackTrackDimension(width) || !double.IsFinite(x))
        {
            return false;
        }

        fraction = Math.Clamp(x / width, 0, 1);
        return true;
    }

    private static bool IsUsableFlashbackTrackDimension(double value)
        => double.IsFinite(value) && value > 0;

    private static bool IsUsableFlashbackDuration(TimeSpan value)
        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;
}
