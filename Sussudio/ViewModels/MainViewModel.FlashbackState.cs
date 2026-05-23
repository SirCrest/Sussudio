using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private readonly BitrateSampleWindow _flashbackBitrateSamples = new(BitrateWindowMs);
    private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;
    private bool _suppressFlashbackFormatCycle;
    private bool _suppressFlashbackEncoderSettingsCycle;
    private CancellationTokenSource? _exportCts;
    private int _flashbackExportOperationId;
    private int _flashbackSettingsRestartGeneration;
    private Task? _pendingFlashbackCycleTask;

    [ObservableProperty]
    public partial bool FlashbackGpuDecode { get; set; } = true;

    [ObservableProperty]
    public partial int FlashbackBufferMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial bool IsFlashbackEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFlashbackTimelineVisible { get; set; }

    [ObservableProperty]
    public partial FlashbackPlaybackState FlashbackState { get; set; } = FlashbackPlaybackState.Disabled;

    [ObservableProperty]
    public partial double FlashbackBufferFillPercent { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackBufferFilledDuration { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackPlaybackPosition { get; set; }

    [ObservableProperty]
    public partial TimeSpan FlashbackGapFromLive { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackInPoint { get; set; }

    [ObservableProperty]
    public partial TimeSpan? FlashbackOutPoint { get; set; }

    [ObservableProperty]
    public partial long FlashbackBufferDiskBytes { get; set; }

    [ObservableProperty]
    public partial string FlashbackBitrateInfo { get; set; } = "";

    [ObservableProperty]
    public partial double FlashbackExportProgress { get; set; }

    [ObservableProperty]
    public partial bool IsFlashbackExporting { get; set; }

    [ObservableProperty]
    public partial bool IsDiskWarningActive { get; set; }

    partial void OnIsFlashbackEnabledChanged(bool value)
    {
        if (!value)
        {
            IsFlashbackTimelineVisible = false;
        }
    }

    /// <summary>
    /// Updates flashback buffer status properties from the buffer manager.
    /// Called from a periodic timer on the UI thread.
    /// </summary>
    public void UpdateFlashbackBufferStatus()
    {
        var bufferStatus = _sessionCoordinator.GetFlashbackBufferStatus();
        if (!bufferStatus.IsActive)
        {
            if (FlashbackState != FlashbackPlaybackState.Disabled)
                FlashbackState = FlashbackPlaybackState.Disabled;
            FlashbackBufferFillPercent = 0;
            FlashbackBufferFilledDuration = TimeSpan.Zero;
            FlashbackBufferDiskBytes = 0;
            FlashbackBitrateInfo = "";
            IsDiskWarningActive = false;
            FlashbackInPoint = null;
            FlashbackOutPoint = null;
            _flashbackBitrateSamples.Clear();
            return;
        }

        FlashbackBufferFilledDuration = bufferStatus.FilledDuration;
        FlashbackBufferDiskBytes = bufferStatus.DiskBytes;
        FlashbackBufferFillPercent = bufferStatus.BufferDuration.TotalSeconds > 0
            ? Math.Clamp(bufferStatus.FilledDuration.TotalSeconds / bufferStatus.BufferDuration.TotalSeconds * 100, 0, 100)
            : 0;

        IsDiskWarningActive = bufferStatus.IsDiskWarningActive;

        UpdateFlashbackBitrate();

        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        if (playback.IsActive)
        {
            FlashbackState = playback.State;
            if (playback.State != FlashbackPlaybackState.Scrubbing)
                FlashbackPlaybackPosition = playback.PlaybackPosition;
            FlashbackGapFromLive = playback.GapFromLive;
            FlashbackInPoint = playback.InPoint;
            FlashbackOutPoint = playback.OutPoint;
        }
        else
        {
            if (FlashbackState != FlashbackPlaybackState.Live)
                FlashbackState = FlashbackPlaybackState.Live;
        }
    }

    private void UpdateFlashbackBitrate()
    {
        var diskBytes = _sessionCoordinator.FlashbackTotalBytesWritten;
        var now = Environment.TickCount64;
        var smoothed = _flashbackBitrateSamples.AddSampleAndCompute(now, diskBytes);
        FlashbackBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "";
    }
}
