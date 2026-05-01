using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.ViewModels;
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
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    #region Flashback Timeline
    private void AnimateFlashbackTimeline(bool show)
    {
        _isFlashbackTimelineAnimating = true;
        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            FlashbackTimelinePanel.Opacity = 0;
            FlashbackTimelinePanel.Height = double.NaN;
            FlashbackTimelinePanel.Visibility = Visibility.Visible;
            FlashbackTimelinePanel.UpdateLayout();
            targetHeight = FlashbackTimelinePanel.ActualHeight;
            FlashbackTimelinePanel.Height = 0;
        }
        else
        {
            targetHeight = FlashbackTimelinePanel.ActualHeight;
            FlashbackTimelinePanel.Height = targetHeight;
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, FlashbackTimelinePanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, FlashbackTimelinePanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (show)
            {
                FlashbackTimelinePanel.Height = double.NaN;
                FlashbackTimelinePanel.Opacity = 1;
            }
            else
            {
                FlashbackTimelinePanel.Visibility = Visibility.Collapsed;
                FlashbackTimelinePanel.Height = double.NaN;
                FlashbackTimelinePanel.Opacity = 1;
            }
            _isFlashbackTimelineAnimating = false;
        };
        storyboard.Begin();
    }
    private DispatcherQueueTimer? _flashbackStatusTimer;
    private DispatcherQueueTimer? _flashbackPlaybackTimer; // 30Hz position poll for smooth CTI during playback
    private void FlashbackToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isFlashbackTimelineAnimating)
            AnimateFlashbackTimeline(show: true);
        StartFlashbackStatusPolling();
    }
    private void FlashbackToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isFlashbackTimelineAnimating)
            AnimateFlashbackTimeline(show: false);
        StopFlashbackStatusPolling();
    }
    private void StartFlashbackStatusPolling()
    {
        _flashbackStatusTimer ??= _dispatcherQueue.CreateTimer();
        _flashbackStatusTimer.Interval = TimeSpan.FromMilliseconds(250);
        _flashbackStatusTimer.IsRepeating = true;
        _flashbackStatusTimer.Tick -= FlashbackStatusTimer_Tick;
        _flashbackStatusTimer.Tick += FlashbackStatusTimer_Tick;
        _flashbackStatusTimer.Start();
    }
    private void StopFlashbackStatusPolling()
    {
        if (_flashbackStatusTimer == null) return;
        _flashbackStatusTimer.Stop();
        _flashbackStatusTimer.Tick -= FlashbackStatusTimer_Tick;
        StopFlashbackPlaybackPolling();
    }
    private void StartFlashbackPlaybackPolling()
    {
        _flashbackPlaybackTimer ??= _dispatcherQueue.CreateTimer();
        if (_flashbackPlaybackTimer.IsRunning) return;
        _flashbackPlaybackTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30Hz
        _flashbackPlaybackTimer.IsRepeating = true;
        _flashbackPlaybackTimer.Tick -= FlashbackPlaybackTimer_Tick;
        _flashbackPlaybackTimer.Tick += FlashbackPlaybackTimer_Tick;
        _flashbackPlaybackTimer.Start();
    }
    private void StopFlashbackPlaybackPolling()
    {
        if (_flashbackPlaybackTimer == null) return;
        _flashbackPlaybackTimer.Stop();
        _flashbackPlaybackTimer.Tick -= FlashbackPlaybackTimer_Tick;
    }
    private void FlashbackStatusTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_isWindowClosing) return;
            ViewModel.UpdateFlashbackBufferStatus();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_STATUS_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }
    private void FlashbackPlaybackTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_isWindowClosing) return;
            var playback = ViewModel.GetFlashbackPlaybackSnapshot();
            if (!playback.IsActive || playback.State != FlashbackPlaybackState.Playing)
            {
                StopFlashbackPlaybackPolling();
                return;
            }
            ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
            StopFlashbackPlaybackPolling();
        }
    }
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

        // Re-layout current positions
        UpdateFlashbackPositionUI();
        UpdateFlashbackMarkers();
    }
    private long _lastScrubUpdateTick;
    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var targetPosition = ComputeFlashbackScrubPosition(e);
        if (!ViewModel.FlashbackBeginScrub(targetPosition))
        {
            Logger.Log("FLASHBACK_UI_SCRUB_BEGIN_REJECTED");
            return;
        }

        _isFlashbackScrubbing = true;
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
        ViewModel.FlashbackUpdateScrub(targetPosition);
        UpdateFlashbackScrubVisual(e);
    }
    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        => EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "released");

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
        => EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "cancelled");

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        => EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, "capture_lost");

    private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason)
    {
        if (!_isFlashbackScrubbing)
        {
            return;
        }

        _isFlashbackScrubbing = false;
        element?.ReleasePointerCapture(pointer);
        ViewModel.FlashbackEndScrub();
        Logger.Log($"FLASHBACK_UI_SCRUB_END reason={reason}");
    }
    private void UpdateFlashbackScrubVisual(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(FlashbackScrubArea).Position;
        var width = FlashbackScrubArea.ActualWidth;
        if (width <= 0) return;

        var x = Math.Clamp(pos.X, 0, width);
        PositionFlashbackPlayhead(x, width);

        var fraction = Math.Clamp(pos.X / width, 0, 1);
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        ViewModel.FlashbackPlaybackPosition = TimeSpan.FromSeconds(fraction * bufferDuration.TotalSeconds);
    }
    private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
    {
        var pos = ViewModel.FlashbackSetInPoint();
        if (pos.HasValue) ViewModel.FlashbackInPoint = pos.Value;
    }
    private void FlashbackOutButton_Click(object sender, RoutedEventArgs e)
    {
        var pos = ViewModel.FlashbackSetOutPoint();
        if (pos.HasValue) ViewModel.FlashbackOutPoint = pos.Value;
    }
    private void FlashbackClearButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FlashbackClearInOutPoints();
        ViewModel.FlashbackInPoint = null;
        ViewModel.FlashbackOutPoint = null;
    }
    private void FlashbackPlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        var state = ViewModel.FlashbackState;
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live)
        {
            ViewModel.FlashbackPause();
        }
        else if (state == FlashbackPlaybackState.Paused || state == FlashbackPlaybackState.Scrubbing)
        {
            ViewModel.FlashbackPlay();
        }
    }
    private void FlashbackGoLiveButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FlashbackGoLive();
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

        // Start/stop the 30Hz playback position timer based on state.
        // Playing needs smooth CTI; other states use the 250ms buffer status timer.
        if (state == FlashbackPlaybackState.Playing)
            StartFlashbackPlaybackPolling();
        else
            StopFlashbackPlaybackPolling();
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
    private void UpdateFlashbackPositionUI()
    {
        var pos = ViewModel.FlashbackPlaybackPosition;
        var state = ViewModel.FlashbackState;
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        var isLive = state == FlashbackPlaybackState.Live;

        // Format floating time label
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

        // Update playhead + handle + floating label position
        // Live mode: pin to right edge. Otherwise: position fraction.
        var trackWidth = FlashbackScrubArea.ActualWidth;
        if (isLive)
        {
            PositionFlashbackPlayhead(trackWidth, trackWidth);
        }
        else if (bufferDuration.TotalSeconds > 0)
        {
            var fraction = pos.TotalSeconds / bufferDuration.TotalSeconds;
            var x = Math.Clamp(fraction * trackWidth, 0, trackWidth);
            PositionFlashbackPlayhead(x, trackWidth);
        }
    }
    private void PositionFlashbackPlayhead(double x, double trackWidth)
    {
        Canvas.SetLeft(FlashbackPlayhead, x - 1); // center 2px line
        Canvas.SetLeft(FlashbackPlayheadHandle, x - 5); // center 10px circle

        // Position floating time label, clamped to track bounds
        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
        var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));
        Canvas.SetLeft(FlashbackPlayheadTimeBorder, labelX);
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

        TimeSpan? inPtVal = null, outPtVal = null;

        if (ViewModel.FlashbackInPoint is TimeSpan inPt && bufferDuration.TotalSeconds > 0)
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

        if (ViewModel.FlashbackOutPoint is TimeSpan outPt && bufferDuration.TotalSeconds > 0)
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
        if (inPtVal is TimeSpan inVal && outPtVal is TimeSpan outVal && bufferDuration.TotalSeconds > 0)
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
        if (width <= 0) return TimeSpan.Zero;

        var fraction = Math.Clamp(pos.X / width, 0, 1);
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        return TimeSpan.FromSeconds(fraction * bufferDuration.TotalSeconds);
    }
}
