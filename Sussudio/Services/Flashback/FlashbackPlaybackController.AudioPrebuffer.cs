using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Audio prebuffer ---

    private const double PlaybackAudioPrebufferTargetMs = 180.0;
    private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;
    private const int PlaybackAudioPrebufferTimeoutMs = 1000;
    private const int PlaybackAudioPrebufferRetryDelayMs = 20;
    private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;

    private void PrimePlaybackAudioBuffer(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        ref bool fileOpen,
        TimeSpan resumeTarget,
        string operation,
        CancellationToken cancellationToken,
        bool logResult = true)
    {
        var audioPlayback = _audioPlayback;
        if (audioPlayback == null)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        var decodedFrames = 0;
        var timedOut = false;
        var reachedEnd = false;
        var eofRetries = 0;
        var skippedForSoftwareBudget = false;
        var discarded = false;
        var rewound = false;
        var prebufferReleasedFrames = 0;

        while (decodedFrames < PlaybackAudioPrebufferDecodeFrameBudget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (audioPlayback.PlaybackBufferedDurationMs >= PlaybackAudioPrebufferTargetMs)
            {
                break;
            }

            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (elapsedMs >= PlaybackAudioPrebufferTimeoutMs)
            {
                timedOut = true;
                break;
            }

            if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
            {
                skippedForSoftwareBudget = true;
                break;
            }

            if (!TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken))
            {
                reachedEnd = true;
                var waitMs = Math.Min(
                    PlaybackAudioPrebufferRetryDelayMs,
                    Math.Max(1, PlaybackAudioPrebufferTimeoutMs - (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds));
                eofRetries++;
                if (cancellationToken.WaitHandle.WaitOne(waitMs))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                }

                continue;
            }

            decodedFrames++;
            ReleaseHeldFrameBestEffort(frame, $"audio_prebuffer_{operation}");
            prebufferReleasedFrames++;

            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= PlaybackAudioPrebufferTimeoutMs)
            {
                timedOut = true;
                break;
            }
        }

        var bufferedMs = audioPlayback.PlaybackBufferedDurationMs;
        if (bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs)
        {
            ClearPrebufferedFrames(prebufferedFrames, $"prebuffer_discard_{operation}");
            try
            {
                audioPlayback.Flush();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=prebuffer_discard_flush operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            }

            bufferedMs = audioPlayback.PlaybackBufferedDurationMs;
            discarded = true;
        }

        if (decodedFrames > 0)
        {
            rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, cancellationToken);
        }

        if (logResult || timedOut || reachedEnd || skippedForSoftwareBudget)
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER operation={operation} frames={decodedFrames} released_frames={prebufferReleasedFrames} buffered_ms={bufferedMs:F1} target_ms={PlaybackAudioPrebufferTargetMs:F1} discard_threshold_ms={PlaybackAudioPrebufferDiscardThresholdMs:F1} elapsed_ms={Stopwatch.GetElapsedTime(start).TotalMilliseconds:F1} timed_out={timedOut} eos={reachedEnd} eof_retries={eofRetries} software_budget={skippedForSoftwareBudget} discarded={discarded} rewound={rewound}");
        }
    }

    private bool TryRewindPlaybackAudioPrebuffer(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan resumeTarget,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            decoder.AudioChunkCallback = null;
            cancellationToken.ThrowIfCancellationRequested();
            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, resumeTarget, $"prebuffer_discard_{operation}", cancellationToken))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND_FAIL operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds}");
                RestoreAudioCallback(decoder, resumeTarget.Ticks);
                return false;
            }

            RestoreAudioCallback(decoder, resumeTarget.Ticks);
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds}");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=prebuffer_rewind operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds} type={ex.GetType().Name} msg='{ex.Message}'");
            RestoreAudioCallback(decoder, resumeTarget.Ticks);
            return false;
        }
    }
}
