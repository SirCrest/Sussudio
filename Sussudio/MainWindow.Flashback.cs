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
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

public sealed partial class MainWindow
{
    #region Flashback Timeline
    private void AnimateFlashbackTimeline(bool show)
    {
        _isFlashbackTimelineAnimating = true;
        if (show)
        {
            // First placement after the timeline panel becomes visible must
            // snap; the previous Translation.X from a prior open is meaningless.
            _snapFlashbackPlayheadOnNextUpdate = true;
        }
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
        StopFlashbackCtiAnchorTimer();
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
    // CTI motion — the timeline UI is an abstraction over the video pipeline.
    // The video pipeline ticks at 30Hz; the display refreshes at 60-144Hz. The
    // playhead is therefore driven NOT by per-tick eases (which restart at
    // every source update and reintroduce 30Hz stutter) but by a single long-
    // horizon linear extrapolation that the compositor evaluates every frame.
    //
    // Anchor model. We trust the video layer's "current position" only at
    // discrete moments — state edges (play/pause/seek/scrub-end/Live) plus a
    // 1Hz drift correction. At each anchor we compute (fraction_now,
    // fraction_in_60_seconds) and start a 60s linear ScalarKeyFrameAnimation
    // on Translation.X. The implicit-start animation reads the visual's
    // current Translation.X and tweens linearly to the horizon target —
    // velocity is continuous across re-anchors, so the user sees smooth flow.
    //
    // During Playing in a live-recording buffer both position and buffer-end
    // grow at 1ms/ms; the playhead moves slowly because the fraction barely
    // changes. The 60s linear segment is a faithful local approximation of the
    // exact (pos+t)/(buf+t) hyperbola for any reasonable buffer length.
    //
    // Active scrub is the one exception: each PointerMoved event fires a
    // short Magnetic ease (60ms cubic ease-out) toward the pointer x. That
    // looks tight under the finger and absorbs 16ms pointer jitter without
    // visible step.
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic   = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon    = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection   = TimeSpan.FromMilliseconds(1000);

    private void EnsureFlashbackPlayheadVisuals()
    {
        if (_flashbackPlayheadVisualsReady) return;

        _flashbackPlayheadVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayhead);
        _flashbackPlayheadHandleVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayheadHandle);
        _flashbackPlayheadLabelVisual = ElementCompositionPreview.GetElementVisual(FlashbackPlayheadTimeBorder);
        _flashbackPlayheadCompositor = _flashbackPlayheadVisual.Compositor;
        _flashbackPlayheadEaseLinear = _flashbackPlayheadCompositor.CreateLinearEasingFunction();
        _flashbackPlayheadEaseWeighted = _flashbackPlayheadCompositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0.7f), new Vector2(0.1f, 1.0f));

        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayhead, true);
        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayheadHandle, true);
        ElementCompositionPreview.SetIsTranslationEnabled(FlashbackPlayheadTimeBorder, true);

        // Anchor Canvas.Left at 0; from now on Translation.X carries the position.
        Canvas.SetLeft(FlashbackPlayhead, 0);
        Canvas.SetLeft(FlashbackPlayheadHandle, 0);
        Canvas.SetLeft(FlashbackPlayheadTimeBorder, 0);

        _flashbackPlayheadVisualsReady = true;
        // First placement after init must snap — otherwise the playhead would
        // sweep from x=0 when the timeline opens.
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    // Pointer-driven scrub uses this. Snap or short Magnetic ease toward an
    // absolute x. Steady-state Playing/Paused/Live motion is driven by
    // RefreshFlashbackCtiMotion, not this method.
    private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)
    {
        EnsureFlashbackPlayheadVisuals();

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
        var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));

        var lineX = (float)(x - 1);
        var handleX = (float)(x - 5);
        var labelTargetX = (float)labelX;

        if (_snapFlashbackPlayheadOnNextUpdate)
        {
            _snapFlashbackPlayheadOnNextUpdate = false;
            motion = FlashbackPlayheadMotion.Snap;
        }

        if (motion == FlashbackPlayheadMotion.Snap)
        {
            SnapFlashbackPlayheadX(_flashbackPlayheadVisual, lineX);
            SnapFlashbackPlayheadX(_flashbackPlayheadHandleVisual, handleX);
            SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelTargetX);
            return;
        }

        // Magnetic ease toward pointer.
        AnimateFlashbackPlayheadX(_flashbackPlayheadVisual, lineX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
        AnimateFlashbackPlayheadX(_flashbackPlayheadHandleVisual, handleX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
        AnimateFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelTargetX, _flashbackPlayheadEaseWeighted, FlashbackPlayheadDurationMagnetic);
    }

    private void AnimateFlashbackPlayheadX(Visual? visual, float targetX, CompositionEasingFunction? easing, TimeSpan duration)
    {
        if (visual == null || _flashbackPlayheadCompositor == null || easing == null) return;
        var anim = _flashbackPlayheadCompositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, targetX, easing);
        anim.Duration = duration;
        visual.StartAnimation("Translation.X", anim);
    }

    private static void SnapFlashbackPlayheadX(Visual? visual, float targetX)
    {
        if (visual == null) return;
        visual.StopAnimation("Translation.X");
        visual.Properties.InsertVector3("Translation", new Vector3(targetX, 0f, 0f));
    }

    // Continuous-time CTI motion. Drives the playhead, handle, and floating
    // time label via a single 60-second linear ScalarKeyFrameAnimation on
    // Translation.X. The compositor evaluates this animation at the display's
    // refresh rate (60-144Hz), so motion is fluid regardless of the 30Hz
    // playback-position polling cadence. Re-anchored only on:
    //   - state edges (play/pause/Live/Scrubbing transitions)
    //   - panel show / SizeChanged
    //   - explicit seek (Paused/Live/Scrubbing position writes)
    //   - 1Hz drift correction during Playing or Paused
    // Active scrub is excluded — pointer events drive the visual via
    // PositionFlashbackPlayhead(.., Magnetic).
    private void RefreshFlashbackCtiMotion(string reason)
    {
        if (_isFlashbackScrubbing) return;
        if (_isWindowClosing) return;

        EnsureFlashbackPlayheadVisuals();

        var trackW = FlashbackScrubArea.ActualWidth;
        if (!IsUsableFlashbackTrackDimension(trackW)) return;

        var state = ViewModel.FlashbackState;

        // Anchor-timer lifecycle: only run during steady states with motion.
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Paused)
            StartFlashbackCtiAnchorTimer();
        else
            StopFlashbackCtiAnchorTimer();

        var stateChanged = state != _flashbackLastCtiState;
        _flashbackLastCtiState = state;

        var explicitStart = stateChanged
                          || _snapFlashbackPlayheadOnNextUpdate
                          || reason == "size_changed"
                          || reason == "panel_show"
                          || reason == "scrub_end"
                          || reason == "seek";
        if (_snapFlashbackPlayheadOnNextUpdate) _snapFlashbackPlayheadOnNextUpdate = false;

        if (state == FlashbackPlaybackState.Live)
        {
            // Right-edge pin. No motion to extrapolate.
            SnapPlayheadVisualsToFraction(1.0, trackW);
            return;
        }

        var bufferDurMs = ViewModel.FlashbackBufferFilledDuration.TotalMilliseconds;
        if (bufferDurMs <= 0) return;

        var posMs = ViewModel.FlashbackPlaybackPosition.TotalMilliseconds;

        var posRate = state == FlashbackPlaybackState.Playing ? 1.0 : 0.0;
        var bufRate = ViewModel.IsFlashbackEnabled ? 1.0 : 0.0;
        var horizonMs = FlashbackCtiExtrapolationHorizon.TotalMilliseconds;

        var posHorizon = Math.Max(0.0, posMs + posRate * horizonMs);
        var bufHorizon = Math.Max(1.0, bufferDurMs + bufRate * horizonMs);

        var fracNow = Math.Clamp(posMs / bufferDurMs, 0.0, 1.0);
        var fracHorizon = Math.Clamp(posHorizon / bufHorizon, 0.0, 1.0);

        StartLinearPlayheadExtrapolation(fracNow, fracHorizon, trackW, FlashbackCtiExtrapolationHorizon, explicitStart);
    }

    private void StartLinearPlayheadExtrapolation(double fracStart, double fracEnd, double trackW, TimeSpan duration, bool explicitStart)
    {
        if (_flashbackPlayheadCompositor == null) return;
        var linear = _flashbackPlayheadEaseLinear;
        if (linear == null) return;

        var startX = fracStart * trackW;
        var endX = fracEnd * trackW;

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
        var labelStart = (float)Math.Clamp(startX - labelW / 2, 0, Math.Max(0, trackW - labelW));
        var labelEnd = (float)Math.Clamp(endX - labelW / 2, 0, Math.Max(0, trackW - labelW));

        StartLinearKeyframe(_flashbackPlayheadVisual, (float)(startX - 1), (float)(endX - 1), duration, linear, explicitStart);
        StartLinearKeyframe(_flashbackPlayheadHandleVisual, (float)(startX - 5), (float)(endX - 5), duration, linear, explicitStart);
        StartLinearKeyframe(_flashbackPlayheadLabelVisual, labelStart, labelEnd, duration, linear, explicitStart);
    }

    private static void StartLinearKeyframe(Visual? v, float startX, float endX, TimeSpan duration, CompositionEasingFunction linear, bool explicitStart)
    {
        if (v == null) return;
        var anim = v.Compositor.CreateScalarKeyFrameAnimation();
        if (explicitStart) anim.InsertKeyFrame(0f, startX);
        anim.InsertKeyFrame(1f, endX, linear);
        anim.Duration = duration;
        v.StartAnimation("Translation.X", anim);
    }

    private void SnapPlayheadVisualsToFraction(double frac, double trackW)
    {
        var x = frac * trackW;
        SnapFlashbackPlayheadX(_flashbackPlayheadVisual, (float)(x - 1));
        SnapFlashbackPlayheadX(_flashbackPlayheadHandleVisual, (float)(x - 5));

        FlashbackPlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = FlashbackPlayheadTimeBorder.DesiredSize.Width;
        var labelX = (float)Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackW - labelW));
        SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelX);
    }

    private void StartFlashbackCtiAnchorTimer()
    {
        _flashbackCtiAnchorTimer ??= _dispatcherQueue.CreateTimer();
        if (_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Interval = FlashbackCtiAnchorDriftCorrection;
        _flashbackCtiAnchorTimer.IsRepeating = true;
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Tick += FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Start();
        _flashbackCtiAnchorRunning = true;
    }

    private void StopFlashbackCtiAnchorTimer()
    {
        if (_flashbackCtiAnchorTimer == null || !_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Stop();
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorRunning = false;
    }

    private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_isWindowClosing) return;
            RefreshFlashbackCtiMotion("anchor_tick");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CTI_ANCHOR_TICK_FAIL type={ex.GetType().Name} msg={ex.Message}");
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
