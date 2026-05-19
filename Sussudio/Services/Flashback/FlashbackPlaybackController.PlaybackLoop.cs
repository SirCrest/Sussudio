using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Continuous playback frame loop ---

    /// <summary>
    /// Decodes and submits the next frame at real-time pace.
    /// Decode-first structure: do the work, then wait for the remainder of the frame interval.
    /// Uses sleep + spin-wait hybrid for sub-millisecond accuracy at 120fps.
    /// When the decoder can't keep up (drift > 200ms), skips frames without display
    /// to maintain audio synchronization.
    /// Returns true if still playing, false if transitioned to another state.
    /// </summary>
    private bool PaceAndDecodeFrame(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        ref TimeSpan frameDuration,
        ref bool fileOpen,
        TimeSpan frozenValidStart,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryReadNextPlaybackFrame(decoder, prebufferedFrames, out var videoFrame, cancellationToken))
            {
                return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);
            }
            if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
            {
                ReleaseHeldFrameBestEffort(videoFrame, "software_decode_over_budget");
                SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "playback_decode");
                return false;
            }

            if (TryResolveAudioDriftFrameSkip(
                    decoder,
                    prebufferedFrames,
                    commandChannel,
                    pacingStopwatch,
                    frozenValidStart,
                    ref fileOpen,
                    cancellationToken,
                    ref videoFrame,
                    out var frameSkipResult))
            {
                return frameSkipResult;
            }

            if (!TrySubmitAndHoldFrame(videoFrame, "playback"))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                RestoreLiveAfterPlaybackSubmitFailure(decoder, ref fileOpen, "playback_submit_failed");
                return false;
            }
            Interlocked.Exchange(ref _lastVideoPtsTicks, videoFrame.Pts.Ticks);

            var newPosition = SaturatingSubtract(videoFrame.Pts, frozenValidStart);
            if (newPosition < TimeSpan.Zero) newPosition = TimeSpan.Zero;
            PlaybackPosition = newPosition;

            if (CheckOutPoint(newPosition, pacingStopwatch))
                return false;

            if (CheckNearLiveEdge(decoder, videoFrame.Pts, newPosition, ref fileOpen))
                return false;

            // Use the encoder's frame rate as ground truth - the buffer manager knows
            // exactly what rate we told NVENC to encode at. TS container metadata can
            // report doubled rates (e.g. 240 for 120fps) and the decoder's PTS calibration
            // needs ~10 frames to correct. The encode rate is authoritative from frame 1.
            frameDuration = ResolveFrameDuration(decoder);
            TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);

            PaceFrameInterval(pacingStopwatch, frameDuration, videoFrame.Pts.Ticks);
            UpdateCadenceMetrics(pacingStopwatch, frameDuration.TotalMilliseconds);

            // Log A/V drift every ~1 second for diagnostics.
            if (_playbackFrameCount % 120 == 0)
            {
                var drift = AvDriftMs;
                var audioClock = Volatile.Read(ref _audioClockPtsTicks);
                Logger.Log($"FLASHBACK_AV_DRIFT frame={_playbackFrameCount} drift_ms={drift:F1} videoPts_ms={(long)videoFrame.Pts.TotalMilliseconds} audioClock_ms={audioClock / TimeSpan.TicksPerMillisecond}");
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return false;
        }
    }

    private void SnapToLiveOnError(FlashbackDecoder decoder, Exception ex, ref bool fileOpen)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        var pos = PlaybackPosition;
        var bufDur = _bufferManager.BufferedDuration;
        var gapMs = SaturatingSubtract(bufDur, pos).TotalMilliseconds;
        SetLastCommandFailure($"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}' pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK {ex.StackTrace?.Replace("\r\n", " | ")}");
        CloseDecoderFileBestEffort(decoder, "decode_error");
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        ReleasePlaybackFrameForLive("decode_error");
        RestoreLiveAudio();
        SafeResumePreviewSubmission("decode_error");
        SetState(FlashbackPlaybackState.Live);
    }

    private bool CheckNearLiveEdge(
        FlashbackDecoder decoder,
        TimeSpan absoluteFramePts,
        TimeSpan bufferPosition,
        ref bool fileOpen,
        bool requireFrameWarmup = true)
    {
        var absoluteLatestPts = _bufferManager.LatestPts;
        var gapFromLive = SaturatingSubtract(absoluteLatestPts, absoluteFramePts);
        var snapThreshold = requireFrameWarmup
            ? ResolveContinuousPlaybackNearLiveSnapThreshold()
            : RecoveryNearLiveSnapThreshold;
        if ((!requireFrameWarmup || Interlocked.Read(ref _playbackFrameCount) > 60) &&
            gapFromLive <= snapThreshold)
        {
            Interlocked.Increment(ref _playbackNearLiveSnaps);
            var gapMs = gapFromLive.TotalMilliseconds;
            Logger.Log($"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP pos_ms={(long)bufferPosition.TotalMilliseconds} framePts_ms={(long)absoluteFramePts.TotalMilliseconds} latestPts_ms={(long)absoluteLatestPts.TotalMilliseconds} gapFromLive_ms={gapMs:F0} threshold_ms={(long)snapThreshold.TotalMilliseconds} frameCount={_playbackFrameCount}");
            CloseDecoderFileBestEffort(decoder, "near_live");
            fileOpen = false;
            _currentOpenFilePath = null;
            _decoderHwAccel = "N/A";
            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
            ReleasePlaybackFrameForLive("near_live");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("near_live");
            SetState(FlashbackPlaybackState.Live);
            return true;
        }
        return false;
    }
}
