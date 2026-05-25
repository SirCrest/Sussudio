using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Continuous playback frame loop ---

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

    // --- Preview frame submission ---

    private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)
    {
        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink == null)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:missing_preview_sink");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_missing_preview_sink");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason=missing_preview_sink");
            return false;
        }

        if (!TryValidatePreviewFrame(frame, out var skipReason))
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:{skipReason}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_{skipReason}");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
            return false;
        }

        try
        {
            var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);
            var countForPresentCadence = string.Equals(operation, "playback", StringComparison.Ordinal);
            SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);
            HoldSubmittedFrame(frame);
            ClearLastSubmitFailure();
            return true;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:submit_fail:{ex.GetType().Name}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_submit_fail");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_FAIL op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    /// <summary>
    /// Submits a decoded frame to the preview renderer - GPU texture or raw CPU data.
    /// </summary>
    private static void SubmitFrame(
        IPreviewFrameSink previewSink,
        DecodedVideoFrame frame,
        long previewPresentId,
        bool countForPresentCadence)
    {
        var submitTick = Stopwatch.GetTimestamp();
        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                Logger.Log("FLASHBACK_PLAYBACK_SUBMIT_SKIP reason=null_texture");
                return;
            }
            previewSink.SubmitTexture(
                frame.TexturePtr, frame.SubresourceIndex,
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
        }
        else
        {
            previewSink.SubmitRawFrame(
                frame.Data, frame.DataLength,
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
        }
    }

    private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)
    {
        if (frame.Width <= 0 || frame.Height <= 0 || (frame.Width & 1) != 0 || (frame.Height & 1) != 0)
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                reason = "null_texture";
                return false;
            }

            if (frame.SubresourceIndex < 0)
            {
                reason = "invalid_subresource";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        if (frame.Data == IntPtr.Zero)
        {
            reason = "null_data";
            return false;
        }

        if (frame.DataLength <= 0)
        {
            reason = "invalid_data_length";
            return false;
        }

        if (!TryCalculatePreviewFrameBytes(frame.Width, frame.Height, frame.IsHdr, out var expectedBytes))
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.DataLength < expectedBytes)
        {
            reason = "short_data_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)
    {
        bytes = 0;
        if (width <= 0 || height <= 0 || (width & 1) != 0 || (height & 1) != 0)
        {
            return false;
        }

        var pixels = (long)width * height;
        var calculated = isHdr
            ? pixels * 3
            : pixels + width * (long)(height / 2);
        if (calculated <= 0 || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
    }

    private bool SeekAndDisplayKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan bufferPosition,
        TimeSpan validStartPts,
        CommandKind kind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Suppress audio delivery during scrub - prevents audio accumulation
        // in the WASAPI queue. Audio callback is re-enabled on Play/EndScrub.
        decoder.AudioChunkCallback = null;
        SafeFlushPlayback("seek_display_keyframe");

        bufferPosition = ClampPosition(bufferPosition, validStartPts);

        if (!decoder.IsOpen)
        {
            // No file - use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, "no_file", bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_NO_FILE pos_ms={(long)bufferPosition.TotalMilliseconds}");
            return false;
        }

        try
        {
            // Map buffer position to file PTS (offset by frozen valid start)
            var filePts = SaturatingAdd(bufferPosition, validStartPts);
            cancellationToken.ThrowIfCancellationRequested();

            // Clamp to current valid range: if eviction advanced ValidStartPts past
            // frozenValidStart, positions near the left edge map to evicted data.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (filePts < currentValidStart)
            {
                filePts = currentValidStart;
                bufferPosition = SaturatingSubtract(filePts, validStartPts);
                if (bufferPosition < TimeSpan.Zero) bufferPosition = TimeSpan.Zero;
            }

            if (!decoder.SeekToKeyframe(filePts, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Active fMP4 segment: demuxer caches fragment index at open time.
                // New fragments written since open aren't visible - reopen and retry.
                // Only for fMP4; .ts handles appended data via eof_reached reset.
                if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
                {
                    if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, "seek_keyframe"))
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}");
                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, "seek_keyframe", cancellationToken))
                            goto seekSuccess;
                    }
                }

                PlaybackPosition = bufferPosition;
                SetSeekDisplayFailure(kind, "seek_failed", bufferPosition);
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return false;
            }
            seekSuccess:
            cancellationToken.ThrowIfCancellationRequested();

            var gotFrame = TryDecodeAndDisplaySeekFrame(
                decoder,
                ref fileOpen,
                kind,
                bufferPosition,
                validStartPts,
                ref filePts,
                cancellationToken);

            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_OK pos_ms={(long)PlaybackPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds} got_frame={gotFrame}");
            return gotFrame;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // On error, use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'");
            return false;
        }
    }

    private bool TryDecodeAndDisplaySeekFrame(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        CommandKind kind,
        TimeSpan bufferPosition,
        TimeSpan validStartPts,
        ref TimeSpan filePts,
        CancellationToken cancellationToken)
    {
        var gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken);
        var frameOwned = gotFrame;
        try
        {
            if (!gotFrame &&
                TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $"seek_display:{kind}", out var adjacentFilePts, cancellationToken))
            {
                filePts = adjacentFilePts;
                cancellationToken.ThrowIfCancellationRequested();
                gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out frame, cancellationToken);
                frameOwned = gotFrame;
            }

            if (gotFrame)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var submitted = TrySubmitAndHoldFrame(frame, "seek");
                frameOwned = false;
                if (!submitted)
                {
                    PlaybackPosition = bufferPosition;
                    SetSeekDisplayFailure(kind, "submit_failed", bufferPosition);
                    return false;
                }
                Interlocked.Exchange(ref _lastVideoPtsTicks, frame.Pts.Ticks);

                var actualPosition = SaturatingSubtract(frame.Pts, validStartPts);
                if (actualPosition < TimeSpan.Zero) actualPosition = TimeSpan.Zero;
                PlaybackPosition = actualPosition;
            }
            else
            {
                PlaybackPosition = bufferPosition;
                RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);
            }
        }
        finally
        {
            if (frameOwned)
            {
                ReleaseHeldFrameBestEffort(frame, "seek_cancelled");
            }
        }

        return gotFrame;
    }

    private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        RecordPlaybackDroppedFrame("seek_display_no_frame");
        SetSeekDisplayFailure(kind, "no_frame", bufferPosition);
        Logger.Log(
            $"FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE kind={kind} pos_ms={(long)bufferPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds}");
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

    private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);

    private void SnapToLiveOnError(FlashbackDecoder decoder, Exception ex, ref bool fileOpen)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        var pos = PlaybackPosition;
        var bufDur = _bufferManager.BufferedDuration;
        var gapMs = SaturatingSubtract(bufDur, pos).TotalMilliseconds;
        SetLastCommandFailure($"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}' pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK {ex.StackTrace?.Replace("\r\n", " | ")}");
        RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);
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
            RestoreLiveAfterNearLiveSnap(decoder, ref fileOpen);
            return true;
        }
        return false;
    }

    private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, operation, resumeRendering: true);

    private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, operation, resumeRendering: true);

    private void RestoreLiveAfterPlaybackDecodeError(FlashbackDecoder decoder, ref bool fileOpen)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, "decode_error", resumeRendering: false);

    private void RestoreLiveAfterNearLiveSnap(FlashbackDecoder decoder, ref bool fileOpen)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, "near_live", resumeRendering: false);

    private void RestoreLiveAfterSoftwarePlaybackBudgetSnap(FlashbackDecoder decoder, ref bool fileOpen, string operation)
        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, operation, resumeRendering: true);

    private void RestoreLiveAfterDecoderPlaybackFailure(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        string operation,
        bool resumeRendering)
    {
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        if (resumeRendering)
        {
            SafeResumeRendering(operation);
        }

        SetState(FlashbackPlaybackState.Live);
    }
}
