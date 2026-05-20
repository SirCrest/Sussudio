using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private long _playbackAudioMasterDelayDoubles;
    private long _playbackAudioMasterDelayShrinks;

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
        // If the audio clock hasn't been updated in >200ms, WASAPI is likely underrunning -
        // fall through to wall-clock pacing instead of extrapolating against a stale sample.
        if (TryGetFreshAudioMasterClock(out var extrapolatedAudioTicks, out var wallElapsedTicks, out var hasAudioClockSample))
        {
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
        var fallbackReason = hasAudioClockSample ? "stale-clock" : "unavailable";
        RecordAudioMasterFallback(fallbackReason, 0, hasAudioClockSample ? wallElapsedTicks : 0);
        WallClockPace(pacingStopwatch, frameDuration);
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
