using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback metric collection and reset ---

    private static double PercentileFromSorted(double[] sortedSamples, double percentile)
    {
        if (sortedSamples.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedSamples.Length) - 1;
        index = Math.Clamp(index, 0, sortedSamples.Length - 1);
        return sortedSamples[index];
    }

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

    private bool TryDecodeNextVideoFrameWithMetrics(
        FlashbackDecoder decoder,
        out DecodedVideoFrame frame,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var decoded = decoder.TryDecodeNextVideoFrame(out frame, cancellationToken);
        if (decoded)
        {
            var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            TrackPlaybackDecodeDuration(elapsedMs, decoder.LastDecodePhaseTimings);
        }

        return decoded;
    }

    private void TrackPlaybackDecodeDuration(
        double elapsedMs,
        FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)
    {
        if (elapsedMs <= 0 || double.IsNaN(elapsedMs) || double.IsInfinity(elapsedMs))
        {
            return;
        }

        lock (_playbackDecodeLock)
        {
            if (_playbackDecodeDurationCount == 0 ||
                elapsedMs >= _playbackMaxDecodeTotalMs)
            {
                _playbackMaxDecodeTotalMs = elapsedMs;
                _playbackMaxDecodeReceiveMs = phaseTimings.ReceiveMs;
                _playbackMaxDecodeFeedMs = phaseTimings.FeedMs;
                _playbackMaxDecodeReadMs = phaseTimings.ReadMs;
                _playbackMaxDecodeSendMs = phaseTimings.SendMs;
                _playbackMaxDecodeAudioMs = phaseTimings.AudioMs;
                _playbackMaxDecodeConvertMs = phaseTimings.ConvertMs;
                Interlocked.Exchange(ref _playbackMaxDecodeUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                Interlocked.Exchange(ref _playbackMaxDecodePositionMs, (long)Math.Max(0, PlaybackPosition.TotalMilliseconds));
                Volatile.Write(ref _playbackMaxDecodePhase, ResolveDominantDecodePhase(phaseTimings));
            }

            _playbackDecodeDurationsMs[_playbackDecodeDurationHead] = elapsedMs;
            _playbackDecodeDurationHead = (_playbackDecodeDurationHead + 1) % _playbackDecodeDurationsMs.Length;
            if (_playbackDecodeDurationCount < _playbackDecodeDurationsMs.Length)
            {
                _playbackDecodeDurationCount++;
            }
        }
    }

    private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)
    {
        var phase = "receive";
        var max = phaseTimings.ReceiveMs;
        if (phaseTimings.FeedMs > max) { phase = "feed"; max = phaseTimings.FeedMs; }
        if (phaseTimings.ReadMs > max) { phase = "read"; max = phaseTimings.ReadMs; }
        if (phaseTimings.SendMs > max) { phase = "send"; max = phaseTimings.SendMs; }
        if (phaseTimings.AudioMs > max) { phase = "audio"; max = phaseTimings.AudioMs; }
        if (phaseTimings.ConvertMs > max) { phase = "convert"; }
        return phase;
    }

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
        Interlocked.Exchange(ref _playbackPreviewPresentId, 0);
        Interlocked.Exchange(ref _playbackLateFrames, 0);
        Interlocked.Exchange(ref _playbackDroppedFrames, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDelayDoubles, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDelayShrinks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterUnavailableFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterStaleFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDriftOutlierFallbacks, 0);
        Volatile.Write(ref _playbackAudioMasterLastFallbackReason, string.Empty);
        _playbackAudioMasterLastFallbackDriftMs = 0;
        _playbackAudioMasterLastFallbackClockAgeMs = 0;
        ClearPendingAudioMasterFallback();
        Volatile.Write(ref _lastPlaybackDropReason, string.Empty);
        Interlocked.Exchange(ref _lastPlaybackDropUtcUnixMs, 0);
        Interlocked.Exchange(ref _playbackSubmitFailures, 0);
        ClearLastSubmitFailure();
        // Reset audio clock extrapolation so stale PTS doesn't cause a jump.
        Interlocked.Exchange(ref _audioClockPtsTicks, 0);
        Interlocked.Exchange(ref _audioClockWallTicks, 0);
        _playbackTargetFps = 0;
        _playbackObservedFps = 0;
        _playbackAvgFrameMs = 0;
        Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, -1);
        Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);
        Interlocked.Exchange(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs, 0);
        _lastPlaybackPtsCadenceDeltaMs = 0;
        _lastPlaybackPtsCadenceExpectedMs = 0;
        Interlocked.Exchange(ref _playbackSeekForwardDecodeCapHits, 0);
        Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 0);
        _playbackFpsClock.Reset();
        Interlocked.Exchange(ref _playbackSlowFrameCount, 0);
        lock (_playbackCadenceLock)
        {
            Array.Clear(_playbackFrameIntervalsMs);
            _playbackFrameIntervalHead = 0;
            _playbackFrameIntervalCount = 0;
        }

        lock (_playbackDecodeLock)
        {
            Array.Clear(_playbackDecodeDurationsMs);
            _playbackDecodeDurationHead = 0;
            _playbackDecodeDurationCount = 0;
            _playbackMaxDecodeTotalMs = 0;
            _playbackMaxDecodeReceiveMs = 0;
            _playbackMaxDecodeFeedMs = 0;
            _playbackMaxDecodeReadMs = 0;
            _playbackMaxDecodeSendMs = 0;
            _playbackMaxDecodeAudioMs = 0;
            _playbackMaxDecodeConvertMs = 0;
            Interlocked.Exchange(ref _playbackMaxDecodeUtcUnixMs, 0);
            Interlocked.Exchange(ref _playbackMaxDecodePositionMs, 0);
            Volatile.Write(ref _playbackMaxDecodePhase, string.Empty);
        }
    }
}
