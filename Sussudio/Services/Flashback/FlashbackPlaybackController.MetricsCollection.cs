using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // Playback cadence metrics are written on the playback thread and read from
    // UI/diagnostics snapshots.
    private long _playbackFrameCount;
    private long _playbackPreviewPresentId;
    private long _playbackLateFrames;
    private long _playbackDroppedFrames;
    private long _playbackSegmentSwitches;
    private long _playbackFmp4Reopens;
    private long _playbackReopenAudioNullWindowCount;
    private long _playbackWriteHeadWaits;
    private long _playbackNearLiveSnaps;
    private long _playbackDecodeErrorSnaps;
    private long _playbackSubmitFailures;
    private long _lastPlaybackDropUtcUnixMs;
    private string _lastPlaybackDropReason = string.Empty;
    private long _lastSubmitFailureUtcUnixMs;
    private string _lastSubmitFailure = string.Empty;
    private long _lastSegmentSwitchUtcUnixMs;
    private long _lastFmp4ReopenUtcUnixMs;
    private long _lastWriteHeadWaitGapMs;
    private double _playbackTargetFps;
    private double _playbackObservedFps;
    private double _playbackAvgFrameMs;
    private long _playbackSeekForwardDecodeCapHits;
    private int _lastPlaybackSeekHitForwardDecodeCap;
    private readonly Stopwatch _playbackFpsClock = new();
    private const int PlaybackCadenceSampleCapacity = 240;
    private readonly object _playbackCadenceLock = new();
    private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];
    private int _playbackFrameIntervalHead;
    private int _playbackFrameIntervalCount;
    private long _playbackSlowFrameCount;

    // --- Playback metric collection ---

    private bool SeekToWithCapTelemetry(
        FlashbackDecoder decoder,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken)
    {
        Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 0);
        var succeeded = decoder.SeekTo(seekTarget, cancellationToken);
        if (decoder.LastSeekHitForwardDecodeCap)
        {
            Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 1);
            Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits);
            Logger.Log(
                $"FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP reason={reason} " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} success={succeeded}");
        }

        return succeeded;
    }

    private void TrackPlaybackCadence(double intervalMs, double expectedFrameMs)
    {
        if (intervalMs <= 0 || double.IsNaN(intervalMs) || double.IsInfinity(intervalMs))
        {
            return;
        }

        lock (_playbackCadenceLock)
        {
            _playbackFrameIntervalsMs[_playbackFrameIntervalHead] = intervalMs;
            _playbackFrameIntervalHead = (_playbackFrameIntervalHead + 1) % _playbackFrameIntervalsMs.Length;
            if (_playbackFrameIntervalCount < _playbackFrameIntervalsMs.Length)
            {
                _playbackFrameIntervalCount++;
            }
        }

        if (expectedFrameMs > 0 && intervalMs > expectedFrameMs * 1.5)
        {
            Interlocked.Increment(ref _playbackSlowFrameCount);
        }
    }

}
