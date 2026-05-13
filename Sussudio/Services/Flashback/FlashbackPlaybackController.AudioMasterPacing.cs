using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Audio-master playback pacing ---

    /// <summary>
    /// Audio-master pacing. Video and audio are decoded from the same interleaved
    /// container on the same thread - their PTS are the source of truth.
    /// Without suppression, audio and video start at the same file position after
    /// seek, so the initial offset should be near-zero. This method corrects any
    /// drift that develops over time (hardware clock vs decode rate).
    /// Falls back to wall-clock pacing when audio is unavailable.
    /// </summary>
    private void PaceFrameInterval(Stopwatch pacingStopwatch, TimeSpan frameDuration, long videoPtsTicks)
    {
        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;

        // Update audio clock extrapolation state when WASAPI reports a new PTS.
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        var wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);

        // If the audio clock hasn't been updated in >200ms, WASAPI is likely underrunning -
        // fall through to wall-clock pacing instead of extrapolating against a stale sample.
        const long StaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;
        if (audioClockPts > 0 && wallElapsedTicks <= StaleThresholdTicks)
        {
            // Extrapolate: audioClock = lastSampledPts + wallElapsedSinceSample.
            var extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;

            // diff > 0 = video ahead of audio, < 0 = video behind.
            var diffTicks = videoPtsTicks - extrapolatedAudioTicks;
            var diffMs = diffTicks / (double)TimeSpan.TicksPerMillisecond;
            var nominalDelayMs = frameDuration.TotalMilliseconds;

            // At HFR, per-frame corrections are very visible. Short fMP4
            // fragments keep audio close, so tolerate sub-100ms drift and only
            // correct when sync moves outside that band.
            const double syncThresholdMs = 100.0;
            const double MaxAudioMasterCorrectionMs = 250.0;

            if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)
            {
                // WASAPI render PTS can lag decoded video by the endpoint buffer/device
                // latency after resume. Do not let that stale clock halve video cadence.
                RecordAudioMasterFallback("drift-outlier", diffMs, wallElapsedTicks);
                WallClockPace(pacingStopwatch, frameDuration);
                return;
            }

            ClearPendingAudioMasterFallback();

            double adjustedDelayMs;
            if (diffMs > syncThresholdMs)
            {
                // Video ahead: add a tiny correction without tanking HFR cadence.
                Interlocked.Increment(ref _playbackAudioMasterDelayDoubles);
                var correctionMs = Math.Min(diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));
                adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);
            }
            else if (diffMs < -syncThresholdMs)
            {
                // Video behind: shave a tiny correction without creating bursts.
                Interlocked.Increment(ref _playbackAudioMasterDelayShrinks);
                var correctionMs = Math.Min(-diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));
                adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));
                if (adjustedDelayMs <= 0)
                {
                    Interlocked.Increment(ref _playbackLateFrames);
                }
            }
            else
            {
                // Within threshold - smooth wall-clock cadence.
                adjustedDelayMs = nominalDelayMs;
            }

            if (adjustedDelayMs > 0)
            {
                var targetTicks = (long)(adjustedDelayMs / 1000.0 * Stopwatch.Frequency);
                var remaining = targetTicks - pacingStopwatch.ElapsedTicks;
                if (remaining > 0)
                {
                    var spinThresholdTicks = 2L * Stopwatch.Frequency / 1000;
                    if (remaining > spinThresholdTicks)
                    {
                        var sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);
                        if (sleepMs > 0)
                        {
                            Thread.Sleep(sleepMs);
                        }
                    }

                    while (pacingStopwatch.ElapsedTicks < targetTicks)
                    {
                        Thread.SpinWait(1);
                    }
                }
            }

            return;
        }

        // Fallback: no audio clock available - pure wall-clock pacing.
        var fallbackReason = audioClockPts <= 0 ? "unavailable" : "stale-clock";
        RecordAudioMasterFallback(fallbackReason, 0, audioClockPts <= 0 ? 0 : wallElapsedTicks);
        WallClockPace(pacingStopwatch, frameDuration);
    }

    private void RecordAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)
    {
        if (!IsTransientAudioMasterFallbackCandidate(reason))
        {
            CommitPendingAudioMasterFallback();
            CommitAudioMasterFallback(reason, driftMs, clockAgeTicks);
            return;
        }

        if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))
        {
            _pendingAudioMasterFallbackReason = reason;
            _pendingAudioMasterFallbackDriftMs = driftMs;
            _pendingAudioMasterFallbackClockAgeTicks = clockAgeTicks;
            return;
        }

        CommitPendingAudioMasterFallback();
        CommitAudioMasterFallback(reason, driftMs, clockAgeTicks);
    }

    private static bool IsTransientAudioMasterFallbackCandidate(string reason)
        => string.Equals(reason, "unavailable", StringComparison.Ordinal) ||
           string.Equals(reason, "stale-clock", StringComparison.Ordinal) ||
           string.Equals(reason, "drift-outlier", StringComparison.Ordinal);

    private void ClearPendingAudioMasterFallback()
    {
        _pendingAudioMasterFallbackReason = string.Empty;
        _pendingAudioMasterFallbackDriftMs = 0;
        _pendingAudioMasterFallbackClockAgeTicks = 0;
    }

    private void CommitPendingAudioMasterFallback()
    {
        if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))
        {
            return;
        }

        CommitAudioMasterFallback(
            _pendingAudioMasterFallbackReason,
            _pendingAudioMasterFallbackDriftMs,
            _pendingAudioMasterFallbackClockAgeTicks);
        ClearPendingAudioMasterFallback();
    }

    private void CommitAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)
    {
        Interlocked.Increment(ref _playbackAudioMasterFallbacks);
        switch (reason)
        {
            case "unavailable":
                Interlocked.Increment(ref _playbackAudioMasterUnavailableFallbacks);
                break;
            case "stale-clock":
                Interlocked.Increment(ref _playbackAudioMasterStaleFallbacks);
                break;
            case "drift-outlier":
                Interlocked.Increment(ref _playbackAudioMasterDriftOutlierFallbacks);
                break;
        }

        Volatile.Write(ref _playbackAudioMasterLastFallbackReason, reason);
        _playbackAudioMasterLastFallbackDriftMs = driftMs;
        _playbackAudioMasterLastFallbackClockAgeMs = clockAgeTicks <= 0
            ? 0
            : clockAgeTicks / (double)TimeSpan.TicksPerMillisecond;
    }

    /// <summary>
    /// Re-syncs the cached audio clock from WASAPI (matching the resync done at the top
    /// of <see cref="PaceFrameInterval"/>) and returns the extrapolated drift in
    /// milliseconds (positive = video ahead of audio). Returns false if the audio clock
    /// is unavailable, has never been sampled, or is stale (>200ms since last update) -
    /// callers must fall back to wall-clock pacing in that case.
    /// </summary>
    private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)
    {
        driftMs = 0;

        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        if (audioClockPts <= 0)
        {
            return false;
        }

        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        var wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
        const long StaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;
        if (wallElapsedTicks > StaleThresholdTicks)
        {
            return false;
        }

        var extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
        driftMs = (videoPtsTicks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
        return true;
    }

    private void WallClockPace(Stopwatch pacingStopwatch, TimeSpan frameDuration)
    {
        var targetTicks = (long)(frameDuration.TotalSeconds * Stopwatch.Frequency);
        var remaining = targetTicks - pacingStopwatch.ElapsedTicks;
        if (remaining > 0)
        {
            var spinThresholdTicks = 2L * Stopwatch.Frequency / 1000;
            if (remaining > spinThresholdTicks)
            {
                var sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }
            }

            while (pacingStopwatch.ElapsedTicks < targetTicks)
            {
                Thread.SpinWait(1);
            }
        }
        else
        {
            Interlocked.Increment(ref _playbackLateFrames);
        }
    }
}
