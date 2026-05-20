using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private long _lastPlaybackCadencePtsTicks = -1;
    private long _playbackPtsCadenceMismatchCount;
    private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;
    private double _lastPlaybackPtsCadenceDeltaMs;
    private double _lastPlaybackPtsCadenceExpectedMs;

    public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);
    public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);
    public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;
    public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;

    private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)
    {
        if (pts <= TimeSpan.Zero || expectedFrameDuration <= TimeSpan.Zero)
        {
            return;
        }

        var currentTicks = pts.Ticks;
        var previousTicks = Volatile.Read(ref _lastPlaybackCadencePtsTicks);
        if (previousTicks <= 0)
        {
            Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, currentTicks);
            return;
        }

        var deltaTicks = currentTicks - previousTicks;
        var deltaMs = deltaTicks / (double)TimeSpan.TicksPerMillisecond;
        var expectedMs = expectedFrameDuration.TotalMilliseconds;
        var toleranceMs = Math.Max(2.0, expectedMs * 0.25);
        if (deltaTicks <= 0)
        {
            RecordPlaybackPtsCadenceMismatch(deltaMs, expectedMs, toleranceMs, pts);
            return;
        }

        Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, currentTicks);
        if (deltaTicks > TimeSpan.TicksPerSecond)
        {
            return;
        }

        if (Math.Abs(deltaMs - expectedMs) <= toleranceMs)
        {
            return;
        }

        RecordPlaybackPtsCadenceMismatch(deltaMs, expectedMs, toleranceMs, pts);
    }

    private void ResetPlaybackPtsCadenceBaseline()
        => Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, 0);

    private void RecordPlaybackPtsCadenceMismatch(double deltaMs, double expectedMs, double toleranceMs, TimeSpan pts)
    {
        var count = Interlocked.Increment(ref _playbackPtsCadenceMismatchCount);
        _lastPlaybackPtsCadenceDeltaMs = deltaMs;
        _lastPlaybackPtsCadenceExpectedMs = expectedMs;
        Interlocked.Exchange(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (count <= 3 || count % 120 == 0)
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH count={count} " +
                $"delta_ms={deltaMs:0.###} expected_ms={expectedMs:0.###} tolerance_ms={toleranceMs:0.###} " +
                $"pts_ms={(long)pts.TotalMilliseconds} target_fps={_playbackTargetFps:0.###}");
        }
    }
}
