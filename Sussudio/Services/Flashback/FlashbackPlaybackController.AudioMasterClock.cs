using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // Last sampled audio rendering PTS plus the wall-clock anchor used for
    // extrapolated audio-master pacing between WASAPI render callbacks.
    private long _audioClockPtsTicks;
    private long _audioClockWallTicks;

    private const long AudioMasterClockStaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;

    /// <summary>
    /// Audio-video drift in milliseconds. Positive = audio ahead, negative = audio behind.
    /// Uses the PTS of the chunk WASAPI is currently rendering (not just enqueued).
    /// </summary>
    public double AvDriftMs
    {
        get
        {
            var renderingPts = _audioPlayback?.RenderingPtsTicks ?? 0;
            var videoPts = Interlocked.Read(ref _lastVideoPtsTicks);
            if (renderingPts == 0 || videoPts == 0) return 0;
            return TimeSpan.FromTicks(renderingPts - videoPts).TotalMilliseconds;
        }
    }

    private void RefreshAudioMasterClock()
    {
        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }
    }

    private bool TryGetFreshAudioMasterClock(
        out long extrapolatedAudioTicks,
        out long wallElapsedTicks,
        out bool hasAudioClockSample)
    {
        RefreshAudioMasterClock();

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        hasAudioClockSample = audioClockPts > 0;
        extrapolatedAudioTicks = 0;
        wallElapsedTicks = 0;
        if (!hasAudioClockSample)
        {
            return false;
        }

        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
        if (wallElapsedTicks > AudioMasterClockStaleThresholdTicks)
        {
            return false;
        }

        extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
        return true;
    }

    /// <summary>
    /// Re-syncs the cached audio clock from WASAPI (matching the resync done by
    /// <see cref="PaceFrameInterval"/>) and returns the extrapolated drift in
    /// milliseconds (positive = video ahead of audio). Returns false if the audio clock
    /// is unavailable, has never been sampled, or is stale (>200ms since last update) -
    /// callers must fall back to wall-clock pacing in that case.
    /// </summary>
    private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)
    {
        driftMs = 0;
        if (!TryGetFreshAudioMasterClock(out var extrapolatedAudioTicks, out _, out _))
        {
            return false;
        }

        driftMs = (videoPtsTicks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
        return true;
    }
}
