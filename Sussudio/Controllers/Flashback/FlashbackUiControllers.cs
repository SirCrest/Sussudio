using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Models;
using Sussudio.ViewModels;
using VirtualKey = Windows.System.VirtualKey;

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

internal sealed class FlashbackSettingsBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleSwitch FlashbackEnabledToggle { get; init; }
    public required ToggleSwitch FlashbackGpuDecodeToggle { get; init; }
    public required ComboBox FlashbackBufferDurationCombo { get; init; }
    public required Action ApplyFlashbackTimelineLockout { get; init; }
}

internal sealed class FlashbackSettingsBindingController
{
    private readonly FlashbackSettingsBindingControllerContext _context;

    public FlashbackSettingsBindingController(FlashbackSettingsBindingControllerContext context)
    {
        _context = context;
    }

    public void ApplyInitialSettings()
    {
        _context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;
        _context.ApplyFlashbackTimelineLockout();
        SyncBufferDurationSelection();
    }

    public void AttachBindings()
    {
        _context.FlashbackGpuDecodeToggle.Toggled += (s, e) =>
            _context.ViewModel.FlashbackGpuDecode = _context.FlashbackGpuDecodeToggle.IsOn;
    }

    public void SyncGpuDecodeToggle()
    {
        if (_context.FlashbackGpuDecodeToggle.IsOn != _context.ViewModel.FlashbackGpuDecode)
        {
            _context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;
        }
    }

    public void SyncBufferDurationSelection()
    {
        var selectedMinutes = _context.ViewModel.FlashbackBufferMinutes.ToString();
        if (_context.FlashbackBufferDurationCombo.SelectedItem is ComboBoxItem current &&
            current.Tag is string currentTag &&
            currentTag == selectedMinutes)
        {
            return;
        }

        foreach (ComboBoxItem item in _context.FlashbackBufferDurationCombo.Items)
        {
            if (item.Tag is string tag && tag == selectedMinutes)
            {
                _context.FlashbackBufferDurationCombo.SelectedItem = item;
                break;
            }
        }
    }

    public void HandleBufferDurationSelectionChanged()
    {
        if (_context.FlashbackBufferDurationCombo.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            int.TryParse(tag, out var minutes))
        {
            _context.ViewModel.FlashbackBufferMinutes = minutes;
            Sussudio.Logger.Log($"FLASHBACK_UI_BUFFER_DURATION_CHANGED minutes={minutes}");
        }
    }
}

internal sealed class FlashbackMarkerPresentationControllerContext
{
    public required FrameworkElement ScrubArea { get; init; }
    public required FrameworkElement InPointMarker { get; init; }
    public required FrameworkElement OutPointMarker { get; init; }
    public required FrameworkElement SelectionRegion { get; init; }
}

internal sealed class FlashbackMarkerPresentationController
{
    private readonly FlashbackMarkerPresentationControllerContext _context;

    public FlashbackMarkerPresentationController(FlashbackMarkerPresentationControllerContext context)
    {
        _context = context;
    }

    public static string FormatDuration(TimeSpan value)
    {
        var totalMinutes = (int)value.TotalMinutes;
        var seconds = value.Seconds;
        return $"{totalMinutes}:{seconds:D2}";
    }

    public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)
    {
        var trackWidth = _context.ScrubArea.ActualWidth;
        var trackHeight = _context.ScrubArea.ActualHeight;
        var hasUsableTrack = IsUsableTrackDimension(trackWidth) &&
                             IsUsableTrackDimension(trackHeight);
        var hasUsableDuration = IsUsableDuration(bufferDuration);

        TimeSpan? inPtVal = null, outPtVal = null;

        if (hasUsableTrack && hasUsableDuration && inPoint is TimeSpan inPt)
        {
            inPtVal = inPt;
            var inX = Math.Clamp(inPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            _context.InPointMarker.Visibility = Visibility.Visible;
            _context.InPointMarker.Height = trackHeight;
            Canvas.SetLeft(_context.InPointMarker, inX - 1);
        }
        else
        {
            _context.InPointMarker.Visibility = Visibility.Collapsed;
        }

        if (hasUsableTrack && hasUsableDuration && outPoint is TimeSpan outPt)
        {
            outPtVal = outPt;
            var outX = Math.Clamp(outPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            _context.OutPointMarker.Visibility = Visibility.Visible;
            _context.OutPointMarker.Height = trackHeight;
            Canvas.SetLeft(_context.OutPointMarker, outX - 1);
        }
        else
        {
            _context.OutPointMarker.Visibility = Visibility.Collapsed;
        }

        if (inPtVal is TimeSpan inVal && outPtVal is TimeSpan outVal && hasUsableTrack && hasUsableDuration)
        {
            var inFrac = inVal.TotalSeconds / bufferDuration.TotalSeconds;
            var outFrac = outVal.TotalSeconds / bufferDuration.TotalSeconds;
            var selLeft = Math.Clamp(inFrac * trackWidth, 0, trackWidth);
            var selRight = Math.Clamp(outFrac * trackWidth, 0, trackWidth);
            _context.SelectionRegion.Visibility = Visibility.Visible;
            _context.SelectionRegion.Height = trackHeight;
            _context.SelectionRegion.Width = Math.Max(0, selRight - selLeft);
            Canvas.SetLeft(_context.SelectionRegion, selLeft);
        }
        else
        {
            _context.SelectionRegion.Visibility = Visibility.Collapsed;
        }
    }

    private static bool IsUsableTrackDimension(double value)
        => double.IsFinite(value) && value > 0;

    private static bool IsUsableDuration(TimeSpan value)
        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;
}

internal sealed class FlashbackPlaybackPresentationControllerContext
{
    public required FontIcon PlayPauseIcon { get; init; }
    public required Button GoLiveButton { get; init; }
    public required TextBlock BufferDurationText { get; init; }
    public required TextBlock PlayheadTimeText { get; init; }
}

internal sealed class FlashbackPlaybackPresentationController
{
    private readonly FlashbackPlaybackPresentationControllerContext _context;

    public FlashbackPlaybackPresentationController(FlashbackPlaybackPresentationControllerContext context)
    {
        _context = context;
    }

    public static string GetPlayPauseGlyph(FlashbackPlaybackState state)
        => state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Live
            ? "\uE769"
            : "\uE768";

    public static bool IsGoLiveEnabled(FlashbackPlaybackState state)
        => state != FlashbackPlaybackState.Live && state != FlashbackPlaybackState.Disabled;

    public static string FormatPositionLabel(
        FlashbackPlaybackState state,
        TimeSpan bufferDuration,
        TimeSpan gapFromLive)
    {
        if (state == FlashbackPlaybackState.Live)
        {
            return "LIVE";
        }

        var totalText = FlashbackMarkerPresentationController.FormatDuration(bufferDuration);
        return $"-{FlashbackMarkerPresentationController.FormatDuration(gapFromLive)} / {totalText}";
    }

    public void UpdateState(FlashbackPlaybackState state)
    {
        _context.PlayPauseIcon.Glyph = GetPlayPauseGlyph(state);
        _context.GoLiveButton.IsEnabled = IsGoLiveEnabled(state);
    }

    public void UpdateBufferFill(TimeSpan duration)
    {
        _context.BufferDurationText.Text = FlashbackMarkerPresentationController.FormatDuration(duration);
    }

    public void UpdatePosition(
        FlashbackPlaybackState state,
        TimeSpan bufferDuration,
        TimeSpan gapFromLive)
    {
        _context.PlayheadTimeText.Text = FormatPositionLabel(state, bufferDuration, gapFromLive);
    }
}

internal sealed class FlashbackPlaybackUiCoordinatorContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Action<double, double> ApplyTrackSize { get; init; }
    public required Action RequestPlayheadSnapOnNextUpdate { get; init; }
    public required Action UpdateMarkers { get; init; }
    public required Action<string> RefreshCtiMotion { get; init; }
    public required Func<bool> IsScrubbing { get; init; }
    public required Action StartPlaybackPolling { get; init; }
    public required Action StopPlaybackPolling { get; init; }
    public required FlashbackPlaybackPresentationController PlaybackPresentation { get; init; }
}

internal sealed class FlashbackPlaybackUiCoordinator
{
    private readonly FlashbackPlaybackUiCoordinatorContext _context;

    public FlashbackPlaybackUiCoordinator(FlashbackPlaybackUiCoordinatorContext context)
    {
        _context = context;
    }

    public void HandleTrackSizeChanged(double width, double height)
    {
        _context.ApplyTrackSize(width, height);

        // Track resize jumps the playhead to its layout-correct position
        // without sweeping through stale translation from the old width.
        _context.RequestPlayheadSnapOnNextUpdate();

        UpdatePosition();
        _context.UpdateMarkers();
        _context.RefreshCtiMotion("size_changed");
    }

    public void UpdateState()
    {
        var state = _context.ViewModel.FlashbackState;
        _context.PlaybackPresentation.UpdateState(state);

        // Keep the 30Hz playback timer running during Playing; its writes to
        // FlashbackPlaybackPosition still feed label text and VM consumers. CTI
        // visuals are driven by long-horizon extrapolation re-anchored on edges.
        if (state == FlashbackPlaybackState.Playing)
        {
            _context.StartPlaybackPolling();
        }
        else
        {
            _context.StopPlaybackPolling();
        }

        _context.RefreshCtiMotion("state_change");
    }

    public void UpdateBufferFill()
    {
        var duration = _context.ViewModel.FlashbackBufferFilledDuration;
        _context.PlaybackPresentation.UpdateBufferFill(duration);
    }

    public void UpdateBufferPresentation()
    {
        UpdateBufferFill();
        UpdatePosition();
        _context.UpdateMarkers();
    }

    // Position-changed handler. Visual CTI motion is driven by RefreshCtiMotion;
    // this method refreshes label text. For Paused/Live states a position change
    // implies seek or scrub-end, so it also re-anchors. Playing ticks deliberately
    // skip re-anchor.
    public void UpdatePosition()
    {
        var state = _context.ViewModel.FlashbackState;
        var bufferDuration = _context.ViewModel.FlashbackBufferFilledDuration;
        _context.PlaybackPresentation.UpdatePosition(
            state,
            bufferDuration,
            _context.ViewModel.FlashbackGapFromLive);

        if (!_context.IsScrubbing()
            && state != FlashbackPlaybackState.Playing
            && state != FlashbackPlaybackState.Scrubbing)
        {
            _context.RefreshCtiMotion("position_change");
        }
    }
}

internal sealed class FlashbackExportProgressPresentationControllerContext
{
    public required ProgressBar FlashbackExportProgressBar { get; init; }
}

internal sealed class FlashbackExportProgressPresentationController
{
    private readonly FlashbackExportProgressPresentationControllerContext _context;

    public FlashbackExportProgressPresentationController(FlashbackExportProgressPresentationControllerContext context)
    {
        _context = context;
    }

    public void UpdateProgress(double progress)
    {
        _context.FlashbackExportProgressBar.Value = progress;
    }

    public void UpdateExporting(bool isExporting)
    {
        _context.FlashbackExportProgressBar.Visibility = isExporting
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!isExporting)
        {
            _context.FlashbackExportProgressBar.Value = 0;
        }
    }
}

internal sealed class FlashbackPropertyChangedControllerContext
{
    public required Func<bool> IsTimelineVisible { get; init; }
    public required Func<double> GetExportProgress { get; init; }
    public required Func<bool> IsExporting { get; init; }
    public required Action<bool> ApplyTimelineVisibility { get; init; }
    public required Action ApplyTimelineLockout { get; init; }
    public required Func<bool> IsFlashbackEnabled { get; init; }
    public required Action<bool> UpdateFlashbackKeepAliveHint { get; init; }
    public required Action UpdateState { get; init; }
    public required Action UpdateBuffer { get; init; }
    public required Action UpdatePlaybackPosition { get; init; }
    public required Action UpdateRangeMarkers { get; init; }
    public required Action<double> UpdateExportProgress { get; init; }
    public required Action<bool> UpdateExportingPresentation { get; init; }
    public required Action SyncGpuDecodeSetting { get; init; }
    public required Action SyncBufferDurationSetting { get; init; }
}

internal sealed class FlashbackPropertyChangedController
{
    private readonly FlashbackPropertyChangedControllerContext _context;

    public FlashbackPropertyChangedController(FlashbackPropertyChangedControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsFlashbackTimelineVisible):
                _context.ApplyTimelineVisibility(_context.IsTimelineVisible());
                return true;

            case nameof(MainViewModel.IsFlashbackEnabled):
                _context.ApplyTimelineLockout();
                _context.UpdateFlashbackKeepAliveHint(_context.IsFlashbackEnabled());
                return true;

            case nameof(MainViewModel.FlashbackState):
                _context.UpdateState();
                return true;

            case nameof(MainViewModel.FlashbackBufferFillPercent):
            case nameof(MainViewModel.FlashbackBufferDiskBytes):
                _context.UpdateBuffer();
                return true;

            case nameof(MainViewModel.FlashbackPlaybackPosition):
                _context.UpdatePlaybackPosition();
                return true;

            case nameof(MainViewModel.FlashbackInPoint):
            case nameof(MainViewModel.FlashbackOutPoint):
                _context.UpdateRangeMarkers();
                return true;

            case nameof(MainViewModel.FlashbackExportProgress):
                _context.UpdateExportProgress(_context.GetExportProgress());
                return true;

            case nameof(MainViewModel.IsFlashbackExporting):
                _context.UpdateExportingPresentation(_context.IsExporting());
                return true;

            case nameof(MainViewModel.FlashbackGpuDecode):
                _context.SyncGpuDecodeSetting();
                return true;

            case nameof(MainViewModel.FlashbackBufferMinutes):
                _context.SyncBufferDurationSetting();
                return true;

            default:
                return false;
        }
    }
}

internal sealed class FlashbackTimelineControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleButton FlashbackToggle { get; init; }
    public required FrameworkElement FlashbackTimelinePanel { get; init; }
    public required FrameworkElement FlashbackTrackBackground { get; init; }
    public required FrameworkElement FlashbackScrubArea { get; init; }
    public required FrameworkElement FlashbackPlayhead { get; init; }
    public required FrameworkElement FlashbackLiveEdge { get; init; }
    public required Action SnapPlayheadOnNextOpen { get; init; }
    public required Action StartStatusPolling { get; init; }
    public required Action StopStatusPolling { get; init; }
    public required Action ClearScrubInteraction { get; init; }
}

internal sealed class FlashbackTimelineController
{
    private readonly FlashbackTimelineControllerContext _context;
    private readonly FlashbackTimelineAnimationController _animationController;
    private bool _suppressToggle;

    public FlashbackTimelineController(FlashbackTimelineControllerContext context)
    {
        _context = context;
        _animationController = new FlashbackTimelineAnimationController(
            context.FlashbackTimelinePanel,
            context.SnapPlayheadOnNextOpen,
            ShouldKeepTimelineVisibleAfterAnimation);
    }

    public void OnToggleChecked()
    {
        if (_suppressToggle)
        {
            return;
        }

        if (!_context.ViewModel.IsFlashbackEnabled)
        {
            ApplyLockout();
            return;
        }

        _context.ViewModel.IsFlashbackTimelineVisible = true;
    }

    public void OnToggleUnchecked()
    {
        if (_suppressToggle)
        {
            return;
        }

        _context.ViewModel.IsFlashbackTimelineVisible = false;
    }

    public void ApplyVisibility(bool show)
    {
        if (show && !_context.ViewModel.IsFlashbackEnabled)
        {
            _context.ViewModel.IsFlashbackTimelineVisible = false;
            show = false;
        }

        SyncToggle(show);
        _context.FlashbackToggle.IsEnabled = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackTimelinePanel.IsHitTestVisible = _context.ViewModel.IsFlashbackEnabled;

        if (show)
        {
            if (!_animationController.IsAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)
            {
                _animationController.Animate(show: true);
            }

            _context.StartStatusPolling();
            return;
        }

        _context.StopStatusPolling();
        if (!_animationController.IsAnimating && _context.FlashbackTimelinePanel.Visibility != Visibility.Collapsed)
        {
            _animationController.Animate(show: false);
        }
    }

    public void ApplyTrackSize(double width, double height)
    {
        _context.FlashbackTrackBackground.Width = width;
        _context.FlashbackTrackBackground.Height = height;
        _context.FlashbackScrubArea.Width = width;
        _context.FlashbackScrubArea.Height = height;
        _context.FlashbackPlayhead.Height = height;
        _context.FlashbackLiveEdge.Height = height;

        Canvas.SetLeft(_context.FlashbackLiveEdge, width - 2);
    }

    public void ApplyLockout()
    {
        var flashbackEnabled = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackToggle.IsEnabled = flashbackEnabled;
        _context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;
        if (flashbackEnabled)
        {
            return;
        }

        if (_context.ViewModel.IsFlashbackTimelineVisible)
        {
            _context.ViewModel.IsFlashbackTimelineVisible = false;
        }

        SyncToggle(isVisible: false);
        _context.StopStatusPolling();
        _context.ClearScrubInteraction();
        CollapseImmediately();
    }

    public void SyncToggle(bool isVisible)
    {
        if (_context.FlashbackToggle.IsChecked == isVisible)
        {
            return;
        }

        _suppressToggle = true;
        try
        {
            _context.FlashbackToggle.IsChecked = isVisible;
        }
        finally
        {
            _suppressToggle = false;
        }
    }

    public void CollapseImmediately()
        => _animationController.CollapseImmediately();

    public void ResetAnimationForFullScreen()
        => _animationController.ResetForFullScreen();

    private bool ShouldKeepTimelineVisibleAfterAnimation()
        => _context.ViewModel.IsFlashbackEnabled &&
           _context.ViewModel.IsFlashbackTimelineVisible;
}

internal sealed class FlashbackTimelineAnimationController
{
    private readonly FrameworkElement _timelinePanel;
    private readonly Action _snapPlayheadOnNextOpen;
    private readonly Func<bool> _shouldRemainVisible;
    private Storyboard? _timelineStoryboard;

    public FlashbackTimelineAnimationController(
        FrameworkElement timelinePanel,
        Action snapPlayheadOnNextOpen,
        Func<bool> shouldRemainVisible)
    {
        _timelinePanel = timelinePanel;
        _snapPlayheadOnNextOpen = snapPlayheadOnNextOpen;
        _shouldRemainVisible = shouldRemainVisible;
    }

    public bool IsAnimating { get; private set; }

    public void Animate(bool show)
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        IsAnimating = true;
        if (show)
        {
            _snapPlayheadOnNextOpen();
        }

        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            _timelinePanel.Opacity = 0;
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Visibility = Visibility.Visible;
            _timelinePanel.UpdateLayout();
            targetHeight = _timelinePanel.ActualHeight;
            _timelinePanel.Height = 0;
        }
        else
        {
            targetHeight = _timelinePanel.ActualHeight;
            _timelinePanel.Height = targetHeight;
        }

        var heightAnimation = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnimation, _timelinePanel);
        Storyboard.SetTargetProperty(heightAnimation, "Height");

        var fadeAnimation = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fadeAnimation, _timelinePanel);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnimation);
        storyboard.Children.Add(fadeAnimation);
        storyboard.Completed += (_, _) => CompleteAnimation(storyboard);

        _timelineStoryboard = storyboard;
        storyboard.Begin();
    }

    public void CollapseImmediately()
    {
        StopCurrentAnimation();
        _timelinePanel.Visibility = Visibility.Collapsed;
        _timelinePanel.Height = double.NaN;
        _timelinePanel.Opacity = 1;
    }

    public void ResetForFullScreen()
    {
        StopCurrentAnimation();
    }

    private void CompleteAnimation(Storyboard storyboard)
    {
        if (!ReferenceEquals(_timelineStoryboard, storyboard))
        {
            return;
        }

        if (_shouldRemainVisible())
        {
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Opacity = 1;
        }
        else
        {
            _timelinePanel.Visibility = Visibility.Collapsed;
            _timelinePanel.Height = double.NaN;
            _timelinePanel.Opacity = 1;
        }

        _timelineStoryboard = null;
        IsAnimating = false;
    }

    private void StopCurrentAnimation()
    {
        _timelineStoryboard?.Stop();
        _timelineStoryboard = null;
        IsAnimating = false;
    }
}

internal sealed class FlashbackScrubInteractionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required FrameworkElement ScrubArea { get; init; }
    public required Action<double, double> PositionMagneticPlayhead { get; init; }
    public required Action<string> RefreshCtiMotion { get; init; }
    public required Func<long> GetTickCount64 { get; init; }
}

internal sealed class FlashbackScrubInteractionController
{
    private readonly FlashbackScrubInteractionControllerContext _context;
    private bool _isScrubbing;
    private TimeSpan? _lastPointerPosition;
    private long _lastUpdateTick;

    public FlashbackScrubInteractionController(FlashbackScrubInteractionControllerContext context)
    {
        _context = context;
    }

    public bool IsScrubbing => _isScrubbing;

    public void PointerPressed(UIElement? element, PointerRoutedEventArgs e)
    {
        var targetPosition = ComputeScrubPosition(e);
        if (!_context.ViewModel.FlashbackBeginScrub(targetPosition))
        {
            _lastPointerPosition = null;
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub begin", "FLASHBACK_UI_SCRUB_BEGIN_REJECTED");
            return;
        }

        _isScrubbing = true;
        _lastPointerPosition = targetPosition;
        _lastUpdateTick = 0;
        element?.CapturePointer(e.Pointer);
        UpdateVisual(e);
    }

    public void PointerMoved(UIElement? element, PointerRoutedEventArgs e)
    {
        if (!_isScrubbing) return;

        // Throttle scrub updates to ~60fps to avoid flooding the decoder.
        var now = _context.GetTickCount64();
        if (now - _lastUpdateTick < 16) return;
        _lastUpdateTick = now;

        var targetPosition = ComputeScrubPosition(e);
        if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub update", "FLASHBACK_UI_SCRUB_UPDATE_REJECTED");
            End(element, e.Pointer, "update_rejected");
            return;
        }

        _lastPointerPosition = targetPosition;
        UpdateVisual(e);
    }

    public void PointerReleased(UIElement? element, PointerRoutedEventArgs e)
    {
        TimeSpan? releasePosition = null;
        if (_isScrubbing)
        {
            var targetPosition = ComputeScrubPosition(e);
            releasePosition = targetPosition;
            _lastPointerPosition = targetPosition;
            if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))
            {
                _context.ViewModel.ReportFlashbackPlaybackRejection("scrub release update", "FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED");
            }
            else
            {
                UpdateVisual(e);
            }
        }

        End(element, e.Pointer, "released", releasePosition);
    }

    public void PointerCanceled(UIElement? element, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isScrubbing ? _lastPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CANCELED carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        End(element, e.Pointer, "cancelled", carriedPosition);
    }

    public void PointerCaptureLost(UIElement? element, PointerRoutedEventArgs e)
    {
        var carriedPosition = _isScrubbing ? _lastPointerPosition : null;
        Logger.Log($"FLASHBACK_SCRUB_END_CAPTURE_LOST carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        End(element, e.Pointer, "capture_lost", carriedPosition);
    }

    public void EndForFullScreen()
    {
        if (!_isScrubbing)
        {
            return;
        }

        var carriedPosition = _lastPointerPosition;
        Logger.Log($"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}");
        ClearLocalState();
        var ended = carriedPosition.HasValue
            ? _context.ViewModel.FlashbackEndScrubAt(carriedPosition.Value)
            : _context.ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection("scrub end (fullscreen_enter)", "FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter");
        }
    }

    public void ClearForLockout()
    {
        ClearLocalState();
    }

    private void End(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)
    {
        if (!_isScrubbing)
        {
            return;
        }

        ClearLocalState();
        element?.ReleasePointerCapture(pointer);
        var ended = releasePosition.HasValue
            ? _context.ViewModel.FlashbackEndScrubAt(releasePosition.Value)
            : _context.ViewModel.FlashbackEndScrub();
        if (!ended)
        {
            _context.ViewModel.ReportFlashbackPlaybackRejection($"scrub end ({reason})", $"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}");
        }

        Logger.Log($"FLASHBACK_UI_SCRUB_END reason={reason}");
        // Hand the visual back to the extrapolation driver from wherever the
        // pointer left it.
        _context.RefreshCtiMotion("scrub_end");
    }

    private void ClearLocalState()
    {
        _isScrubbing = false;
        _lastUpdateTick = 0;
        _lastPointerPosition = null;
    }

    private void UpdateVisual(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(_context.ScrubArea).Position;
        var width = _context.ScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.TryComputeFraction(pos.X, width, out var fraction)) return;

        var x = Math.Clamp(fraction * width, 0, width);
        // Magnetic = ease-out toward the pointer; longer than the 16ms pointer
        // throttle so successive events overlap into a single smooth trail
        // rather than 16ms-stepped jitter.
        _context.PositionMagneticPlayhead(x, width);

        var bufferDuration = _context.ViewModel.FlashbackBufferFilledDuration;
        if (FlashbackTimelineGeometry.IsUsableDuration(bufferDuration))
        {
            _context.ViewModel.FlashbackPlaybackPosition = FlashbackTimelineGeometry.ComputePosition(fraction, bufferDuration);
        }
    }

    private TimeSpan ComputeScrubPosition(PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(_context.ScrubArea).Position;
        var width = _context.ScrubArea.ActualWidth;
        return FlashbackTimelineGeometry.TryComputePosition(
            pos.X,
            width,
            _context.ViewModel.FlashbackBufferFilledDuration,
            out var position)
            ? position
            : TimeSpan.Zero;
    }
}

internal static class FlashbackTimelineGeometry
{
    public static bool TryComputeFraction(double x, double width, out double fraction)
    {
        fraction = 0;
        if (!IsUsableTrackDimension(width) || !double.IsFinite(x))
        {
            return false;
        }

        fraction = Math.Clamp(x / width, 0, 1);
        return true;
    }

    public static bool TryComputePosition(double x, double width, TimeSpan bufferDuration, out TimeSpan position)
    {
        position = TimeSpan.Zero;
        if (!TryComputeFraction(x, width, out var fraction) || !IsUsableDuration(bufferDuration))
        {
            return false;
        }

        position = ComputePosition(fraction, bufferDuration);
        return true;
    }

    public static TimeSpan ComputePosition(double fraction, TimeSpan bufferDuration)
        => IsUsableDuration(bufferDuration)
            ? TimeSpan.FromSeconds(Math.Clamp(fraction, 0, 1) * bufferDuration.TotalSeconds)
            : TimeSpan.Zero;

    public static bool IsUsableTrackDimension(double value)
        => double.IsFinite(value) && value > 0;

    public static bool IsUsableDuration(TimeSpan value)
        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;
}

internal sealed class FlashbackPlayheadMotionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsScrubbing { get; init; }
    public required FrameworkElement ScrubArea { get; init; }
    public required FrameworkElement Playhead { get; init; }
    public required FrameworkElement PlayheadHandle { get; init; }
    public required FrameworkElement PlayheadTimeBorder { get; init; }
}

internal sealed class FlashbackPlayheadMotionController
{
    private enum FlashbackPlayheadMotion
    {
        Snap,
        Magnetic,
    }

    private readonly FlashbackPlayheadMotionControllerContext _context;
    private Visual? _flashbackPlayheadVisual;
    private Visual? _flashbackPlayheadHandleVisual;
    private Visual? _flashbackPlayheadLabelVisual;
    private Compositor? _flashbackPlayheadCompositor;
    private CompositionEasingFunction? _flashbackPlayheadEaseWeighted;
    private bool _flashbackPlayheadVisualsReady;
    private bool _snapFlashbackPlayheadOnNextUpdate;
    private FlashbackPlaybackState? _flashbackLastCtiState;
    private DispatcherQueueTimer? _flashbackCtiAnchorTimer;
    private CompositionEasingFunction? _flashbackPlayheadEaseLinear;
    private bool _flashbackCtiAnchorRunning;
    private static readonly TimeSpan FlashbackPlayheadDurationMagnetic = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan FlashbackCtiExtrapolationHorizon = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FlashbackCtiAnchorDriftCorrection = TimeSpan.FromMilliseconds(1000);

    public FlashbackPlayheadMotionController(FlashbackPlayheadMotionControllerContext context)
    {
        _context = context;
    }

    public void RequestSnapOnNextUpdate()
    {
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    public void PositionMagneticPlayhead(double x, double trackWidth)
    {
        PositionFlashbackPlayhead(x, trackWidth, FlashbackPlayheadMotion.Magnetic);
    }

    public void RefreshCtiMotion(string reason)
    {
        if (_context.IsScrubbing()) return;
        if (_context.IsWindowClosing()) return;

        EnsureFlashbackPlayheadVisuals();

        var trackW = _context.ScrubArea.ActualWidth;
        if (!FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)) return;

        var state = _context.ViewModel.FlashbackState;

        // Anchor-timer lifecycle: only run during steady states with motion.
        if (state == FlashbackPlaybackState.Playing || state == FlashbackPlaybackState.Paused)
            StartFlashbackCtiAnchorTimer();
        else
            StopCtiAnchorTimer();

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

        var bufferDurMs = _context.ViewModel.FlashbackBufferFilledDuration.TotalMilliseconds;
        if (bufferDurMs <= 0) return;

        var posMs = _context.ViewModel.FlashbackPlaybackPosition.TotalMilliseconds;

        var posRate = state == FlashbackPlaybackState.Playing ? 1.0 : 0.0;
        var bufRate = _context.ViewModel.IsFlashbackEnabled ? 1.0 : 0.0;
        var horizonMs = FlashbackCtiExtrapolationHorizon.TotalMilliseconds;

        var posHorizon = Math.Max(0.0, posMs + posRate * horizonMs);
        var bufHorizon = Math.Max(1.0, bufferDurMs + bufRate * horizonMs);

        var fracNow = Math.Clamp(posMs / bufferDurMs, 0.0, 1.0);
        var fracHorizon = Math.Clamp(posHorizon / bufHorizon, 0.0, 1.0);

        StartLinearPlayheadExtrapolation(fracNow, fracHorizon, trackW, FlashbackCtiExtrapolationHorizon, explicitStart);
    }

    public void StopCtiAnchorTimer()
    {
        if (_flashbackCtiAnchorTimer == null || !_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Stop();
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorRunning = false;
    }

    private void StartFlashbackCtiAnchorTimer()
    {
        _flashbackCtiAnchorTimer ??= _context.DispatcherQueue.CreateTimer();
        if (_flashbackCtiAnchorRunning) return;
        _flashbackCtiAnchorTimer.Interval = FlashbackCtiAnchorDriftCorrection;
        _flashbackCtiAnchorTimer.IsRepeating = true;
        _flashbackCtiAnchorTimer.Tick -= FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Tick += FlashbackCtiAnchorTimer_Tick;
        _flashbackCtiAnchorTimer.Start();
        _flashbackCtiAnchorRunning = true;
    }

    private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (_context.IsWindowClosing()) return;
            RefreshCtiMotion("anchor_tick");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CTI_ANCHOR_TICK_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnsureFlashbackPlayheadVisuals()
    {
        if (_flashbackPlayheadVisualsReady) return;

        _flashbackPlayheadVisual = ElementCompositionPreview.GetElementVisual(_context.Playhead);
        _flashbackPlayheadHandleVisual = ElementCompositionPreview.GetElementVisual(_context.PlayheadHandle);
        _flashbackPlayheadLabelVisual = ElementCompositionPreview.GetElementVisual(_context.PlayheadTimeBorder);
        _flashbackPlayheadCompositor = _flashbackPlayheadVisual.Compositor;
        _flashbackPlayheadEaseLinear = _flashbackPlayheadCompositor.CreateLinearEasingFunction();
        _flashbackPlayheadEaseWeighted = _flashbackPlayheadCompositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0.7f), new Vector2(0.1f, 1.0f));

        ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);
        ElementCompositionPreview.SetIsTranslationEnabled(_context.PlayheadHandle, true);
        ElementCompositionPreview.SetIsTranslationEnabled(_context.PlayheadTimeBorder, true);

        // Anchor Canvas.Left at 0; from now on Translation.X carries the position.
        Canvas.SetLeft(_context.Playhead, 0);
        Canvas.SetLeft(_context.PlayheadHandle, 0);
        Canvas.SetLeft(_context.PlayheadTimeBorder, 0);

        _flashbackPlayheadVisualsReady = true;
        // First placement after init must snap; otherwise the playhead would
        // sweep from x=0 when the timeline opens.
        _snapFlashbackPlayheadOnNextUpdate = true;
    }

    private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)
    {
        EnsureFlashbackPlayheadVisuals();

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
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

    private void StartLinearPlayheadExtrapolation(double fracStart, double fracEnd, double trackW, TimeSpan duration, bool explicitStart)
    {
        if (_flashbackPlayheadCompositor == null) return;
        var linear = _flashbackPlayheadEaseLinear;
        if (linear == null) return;

        var startX = fracStart * trackW;
        var endX = fracEnd * trackW;

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
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

        _context.PlayheadTimeBorder.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelW = _context.PlayheadTimeBorder.DesiredSize.Width;
        var labelX = (float)Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackW - labelW));
        SnapFlashbackPlayheadX(_flashbackPlayheadLabelVisual, labelX);
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
}
