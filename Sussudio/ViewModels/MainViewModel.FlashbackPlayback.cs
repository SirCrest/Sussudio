using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback playback snapshot access plus buffer, bitrate, and UI state projection.
/// </summary>
public partial class MainViewModel
{
    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
        => _sessionCoordinator.GetFlashbackPlaybackSnapshot();

    public void ReportFlashbackPlaybackRejection(string action, string logToken)
    {
        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var lastFailure = string.IsNullOrWhiteSpace(playback.LastCommandFailure)
            ? "none"
            : playback.LastCommandFailure;
        var message =
            $"Flashback {action} rejected (state={playback.State}, " +
            $"threadAlive={playback.ThreadAlive}, pending={playback.PendingCommands}, " +
            $"lastFailure={lastFailure}).";

        Logger.Log(
            $"{logToken} state={playback.State} threadAlive={playback.ThreadAlive} " +
            $"pending={playback.PendingCommands} lastFailure='{lastFailure}' " +
            $"failureUtc={playback.LastCommandFailureUtcUnixMs}");
        StatusText = message;
    }

    public IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
        => _sessionCoordinator.GetFlashbackSegments();

    public Task<IReadOnlyList<FlashbackSegmentInfo>> GetFlashbackSegmentsAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);

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

        // Sync state from controller
        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        if (playback.IsActive)
        {
            FlashbackState = playback.State;
            // Don't overwrite UI-driven position during scrub
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
