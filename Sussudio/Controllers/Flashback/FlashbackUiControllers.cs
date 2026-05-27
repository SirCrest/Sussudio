using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
