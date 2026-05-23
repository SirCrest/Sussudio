using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // Keep the previous D3D11VA frame alive until the renderer has had a later
    // submit to copy from; CPU frames follow the same ownership path.
    private DecodedVideoFrame _previousHeldFrame;
    private bool _hasPreviousHeldFrame;

    private void ReleasePreviousHeldFrame()
    {
        if (_hasPreviousHeldFrame)
        {
            ReleaseHeldFrameBestEffort(_previousHeldFrame, "previous_frame");
            _previousHeldFrame = default;
            _hasPreviousHeldFrame = false;
        }
    }

    private void HoldSubmittedFrame(DecodedVideoFrame frame)
    {
        ReleasePreviousHeldFrame();
        _previousHeldFrame = frame;
        _hasPreviousHeldFrame = true;
    }

    private void ReleasePlaybackFrameForLive(string operation)
    {
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);

        if (_hasPreviousHeldFrame)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        }

        ReleasePreviousHeldFrame();
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            FlashbackDecoder.ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private bool TryReadNextPlaybackFrame(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        out DecodedVideoFrame frame,
        CancellationToken cancellationToken)
    {
        if (prebufferedFrames.Count > 0)
        {
            frame = prebufferedFrames.Dequeue();
            return true;
        }

        return TryDecodeNextVideoFrameWithMetrics(decoder, out frame, cancellationToken);
    }

    private void ClearPrebufferedFrames(Queue<DecodedVideoFrame> prebufferedFrames, string operation)
    {
        if (prebufferedFrames.Count == 0)
        {
            return;
        }

        var released = 0;
        while (prebufferedFrames.Count > 0)
        {
            var frame = prebufferedFrames.Dequeue();
            ReleaseHeldFrameBestEffort(frame, $"prebuffer_{operation}");
            released++;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_CLEAR operation={operation} frames={released}");
    }

    private bool TryResolveAudioDriftFrameSkip(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken,
        ref DecodedVideoFrame videoFrame,
        out bool continuePlayback)
    {
        continuePlayback = true;

        // Frame skip: when video falls significantly behind audio, decode-and-discard
        // frames to catch up rather than falling further behind. This handles codecs
        // whose decode time exceeds the frame interval (e.g. AV1 at 4K@120fps where
        // each decode takes ~25ms but frame interval is 8.33ms).
        //
        // The drift recompute MUST re-sync the audio clock each iteration: a single
        // skip can take ~25ms, during which the WASAPI render thread has likely
        // advanced _audioClockPtsTicks. Extrapolating from the original capture
        // diverges from the actual audio clock the longer the loop runs and can
        // either exit early (false-recovered) or burn the full skip cap unnecessarily.
        const double FrameSkipThresholdMs = 500.0;
        const int MaxSkipFrames = 30; // cap to prevent infinite skip loops
        if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) ||
            driftMs >= -FrameSkipThresholdMs)
        {
            return false;
        }

        var skipped = 0;
        while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (commandChannel.Reader.TryPeek(out _))
            {
                ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip_command_pending");
                Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped} drift_ms={driftMs:F1}");
                continuePlayback = true;
                return true;
            }

            // Release the frame without displaying it.
            ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip");
            RecordPlaybackDroppedFrame("av_sync_skip");
            skipped++;

            if (!TryReadNextPlaybackFrame(decoder, prebufferedFrames, out videoFrame, cancellationToken))
            {
                // EOS during skip - log partial progress so the diagnostic gap
                // doesn't hide a long catch-up burst that the user may notice.
                if (skipped > 0)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped} drift_at_eos_ms={driftMs:F1}");
                }

                continuePlayback = HandleEndOfSegment(
                    decoder,
                    commandChannel,
                    pacingStopwatch,
                    frozenValidStart,
                    ref fileOpen,
                    cancellationToken);
                return true;
            }

            if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
            {
                if (skipped > 0)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped} drift_at_budget_ms={driftMs:F1}");
                }

                ReleaseHeldFrameBestEffort(videoFrame, "software_decode_over_budget");
                SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "playback_skip");
                continuePlayback = false;
                return true;
            }

            // Recompute with a freshly-sampled audio clock; if WASAPI is now
            // stale or unavailable, exit the skip loop to avoid extrapolating
            // off a stale reference for the rest of the catch-up.
            if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))
            {
                break;
            }
        }

        if (skipped > 0)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP count={skipped} drift_after_ms={driftMs:F1}");
        }

        return false;
    }
}
