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

    private const double FallbackPlaybackFrameRate = 60.0;
    private const double MaxPlaybackFrameRate = 1000.0;
    private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;
    private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;
    private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);

    // Keep the previous D3D11VA frame alive until the renderer has had a later
    // submit to copy from; CPU frames follow the same ownership path.
    private DecodedVideoFrame _previousHeldFrame;
    private bool _hasPreviousHeldFrame;

    private long _lastPlaybackCadencePtsTicks = -1;
    private long _playbackPtsCadenceMismatchCount;
    private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;
    private double _lastPlaybackPtsCadenceDeltaMs;
    private double _lastPlaybackPtsCadenceExpectedMs;

    public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);
    public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);
    public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;
    public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;

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

    // --- Playback frame-rate, near-live timing, and cadence accounting ---

    private TimeSpan ResolveContinuousPlaybackNearLiveSnapThreshold()
    {
        var fps = _playbackTargetFps;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = _bufferManager.EncodeFrameRate;
        }

        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        var framesThreshold = TimeSpan.FromSeconds(ContinuousPlaybackNearLiveSnapFrames / Math.Min(fps, MaxPlaybackFrameRate));
        return framesThreshold > ContinuousPlaybackNearLiveSnapMinimum
            ? framesThreshold
            : ContinuousPlaybackNearLiveSnapMinimum;
    }

    private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= frozenValidStart)
        {
            return frozenValidStart;
        }

        return ClampPlaybackTargetToMinimumLiveLead(latestPts, frozenValidStart, "pause_from_live");
    }

    private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)
    {
        // The encode rate is authoritative when present. Decoder/container metadata
        // can be wrong, and invalid floating-point values must never tear down playback.
        var fps = ResolvePlaybackFrameRate(decoder);
        _playbackTargetFps = fps;
        return TimeSpan.FromSeconds(1.0 / fps);
    }

    private double ResolvePlaybackFrameRate(FlashbackDecoder decoder)
    {
        var fps = _bufferManager.EncodeFrameRate;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = decoder.FrameRate;
        }

        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        fps = Math.Min(fps, MaxPlaybackFrameRate);
        return fps;
    }

    private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        if (!ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
        {
            UpdateDecoderHwAccel(decoder);
            return false;
        }

        SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, operation);
        return true;
    }

    private bool ShouldSnapLiveForSoftwarePlaybackBudget(
        FlashbackDecoder decoder,
        out double fps,
        out double pixelRate)
    {
        UpdateDecoderHwAccel(decoder);
        fps = ResolvePlaybackFrameRate(decoder);
        pixelRate = Math.Max(0, decoder.VideoWidth) * (double)Math.Max(0, decoder.VideoHeight) * fps;
        return GpuDecodeEnabled &&
               !decoder.IsD3D11HwAccelerated &&
               pixelRate > MaxContinuousSoftwarePlaybackPixelRate;
    }

    private void SnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out var fps, out var pixelRate);
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        RecordPlaybackDroppedFrame("software_decode_over_budget");
        var pos = PlaybackPosition;
        SetLastCommandFailure($"software_decode_over_budget:{operation}{FormatCommandDetail(position: pos)}");
        Logger.Log(
            $"FLASHBACK_PLAYBACK_SOFTWARE_DECODE_SNAP_TO_LIVE op={operation} width={decoder.VideoWidth} height={decoder.VideoHeight} fps={fps:F2} pixel_rate={pixelRate:F0} max_pixel_rate={MaxContinuousSoftwarePlaybackPixelRate:F0}");
        RestoreLiveAfterSoftwarePlaybackBudgetSnap(decoder, ref fileOpen, operation);
    }

    private void UpdateDecoderHwAccel(FlashbackDecoder decoder)
    {
        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
    }

    private void UpdateCadenceMetrics(Stopwatch pacingStopwatch, double expectedFrameMs)
    {
        var frameNum = Interlocked.Increment(ref _playbackFrameCount);
        var intervalMs = pacingStopwatch.Elapsed.TotalMilliseconds;
        pacingStopwatch.Restart();
        TrackPlaybackCadence(intervalMs, expectedFrameMs);

        if (frameNum % 60 == 0)
        {
            // Rolling window over the cadence ring (~2 s at 120 fps) so transient dips
            // are not smoothed away by the cumulative average over a long session.
            double sumMs;
            int count;
            lock (_playbackCadenceLock)
            {
                count = _playbackFrameIntervalCount;
                sumMs = 0;
                for (var i = 0; i < count; i++)
                {
                    sumMs += _playbackFrameIntervalsMs[i];
                }
            }

            if (count > 0 && sumMs > 0)
            {
                _playbackAvgFrameMs = sumMs / count;
                _playbackObservedFps = count * 1000.0 / sumMs;
            }
        }
    }

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
        const double FrameSkipThresholdMs = 250.0;
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

    // --- Continuous playback segment-edge handling ---

    private bool HandleEndOfSegment(
        FlashbackDecoder decoder,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        // Use absolute PTS to measure distance from live edge.
        // PlaybackPosition uses frozenValidStart (captured at scrub time) while
        // BufferedDuration uses the current ValidStartPts (moves as segments are
        // evicted). Mixing these coordinate systems causes a permanently negative
        // gap once eviction advances ValidStartPts past frozenValidStart.
        var latestAbsPts = _bufferManager.LatestPts;
        var lastFrameAbsPts = TimeSpan.FromTicks(Interlocked.Read(ref _lastVideoPtsTicks));
        // Fallback: if no frame was decoded yet, estimate from PlaybackPosition
        if (lastFrameAbsPts == TimeSpan.Zero)
            lastFrameAbsPts = SaturatingAdd(PlaybackPosition, frozenValidStart);
        var gapFromLive = SaturatingSubtract(latestAbsPts, lastFrameAbsPts).TotalMilliseconds;
        var pos = PlaybackPosition;
        var currentOpenFilePath = _currentOpenFilePath;

        if (IsActiveFmp4Segment(currentOpenFilePath) &&
            CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false))
        {
            pacingStopwatch.Restart();
            return false;
        }

        if (gapFromLive > 2000)
        {
            if (TrySwitchToNextSegment(
                    decoder,
                    pacingStopwatch,
                    currentOpenFilePath,
                    lastFrameAbsPts,
                    pos,
                    frozenValidStart,
                    ref fileOpen,
                    cancellationToken,
                    out var segmentSwitchResult))
            {
                return segmentSwitchResult;
            }

            // Active fMP4 segment: the demuxer cached the file structure at open
            // time and won't see new fragments without re-opening. Close and re-open
            // the same file, then seek to where playback left off.
            // Only for fMP4; .ts handles appended data via eof_reached reset.
            if (IsActiveFmp4Segment(currentOpenFilePath) && currentOpenFilePath != null)
            {
                return HandleActiveFmp4ReopenAtSegmentEdge(
                    decoder,
                    pacingStopwatch,
                    currentOpenFilePath,
                    pos,
                    lastFrameAbsPts,
                    ref fileOpen,
                    cancellationToken);
            }
        }

        if (commandChannel.Reader.TryPeek(out _) || _disposedFlag != 0)
        {
            pacingStopwatch.Restart();
            return true;
        }

        Interlocked.Increment(ref _playbackWriteHeadWaits);
        Interlocked.Exchange(ref _lastWriteHeadWaitGapMs, Math.Max(0, (long)gapFromLive));
        Logger.Log($"FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT gapFromLive_ms={gapFromLive:F0} pos_ms={(long)pos.TotalMilliseconds} lastFrameAbsPts_ms={(long)lastFrameAbsPts.TotalMilliseconds} latestPts_ms={(long)latestAbsPts.TotalMilliseconds}");
        if (cancellationToken.WaitHandle.WaitOne(50))
        {
            return false;
        }

        pacingStopwatch.Restart();
        return true;
    }

    private bool TrySwitchToNextSegment(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string? currentOpenFilePath,
        TimeSpan lastFrameAbsPts,
        TimeSpan pos,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken,
        out bool playbackContinues)
    {
        playbackContinues = false;
        var nextFile = currentOpenFilePath != null
            ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
            : null;
        if (nextFile == null || IsSamePlaybackPath(nextFile, currentOpenFilePath))
        {
            return false;
        }

        var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);
        if (currentOpenFilePath != null &&
            nextSegmentStart.HasValue &&
            nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250))
        {
            if (TryReopenCurrentFmp4BeforeSegmentSwitch(
                decoder,
                pacingStopwatch,
                currentOpenFilePath,
                pos,
                lastFrameAbsPts,
                nextSegmentStart.Value,
                ref fileOpen,
                cancellationToken))
            {
                playbackContinues = true;
                return true;
            }
        }

        Interlocked.Increment(ref _playbackSegmentSwitches);
        Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} next='{System.IO.Path.GetFileName(nextFile)}'");
        try
        {
            decoder.CloseFile();
            fileOpen = false;
            decoder.OpenFile(nextFile);
            fileOpen = true;
            _currentOpenFilePath = nextFile;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            // Gate audio at last played position, not seek target - audio between
            // the last played sample and the seek point would otherwise be dropped,
            // causing an audible gap at segment boundaries.
            var audioGate = Interlocked.Read(ref _lastAudioPtsTicks);
            decoder.AudioChunkCallback = null;
            var segSwitchTarget = SaturatingAdd(pos, frozenValidStart);
            if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)
                segSwitchTarget = nextSegmentStart.Value;
            cancellationToken.ThrowIfCancellationRequested();
            if (!SeekToWithCapTelemetry(decoder, segSwitchTarget, "segment_switch", cancellationToken))
            {
                SetReopenFailure("segment_switch", "seek_failed", segSwitchTarget);
                Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL path='{nextFile}' offset_ms={(long)segSwitchTarget.TotalMilliseconds}");
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "segment_switch_seek_failed");
                return true;
            }
            RestoreAudioCallback(decoder, audioGate);
            ResetPlaybackPtsCadenceBaseline();
            pacingStopwatch.Restart();
            playbackContinues = true;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'");
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return true;
        }
    }

    private bool TryReopenCurrentFmp4BeforeSegmentSwitch(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string currentOpenFilePath,
        TimeSpan playbackPosition,
        TimeSpan lastFrameAbsPts,
        TimeSpan nextSegmentStart,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _playbackFmp4Reopens);
        Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH pos_ms={(long)playbackPosition.TotalMilliseconds} resumePts_ms={(long)lastFrameAbsPts.TotalMilliseconds} nextStart_ms={(long)nextSegmentStart.TotalMilliseconds}");
        try
        {
            ReopenDecoderPlaybackFile(
                decoder,
                currentOpenFilePath,
                ref fileOpen,
                updateCurrentOpenPath: false,
                closeOnlyWhenOpen: false);
            var preReopenLastAudioPts = SuppressAudioForFmp4Reopen(decoder);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, lastFrameAbsPts, "fmp4_reopen_before_segment_switch", cancellationToken))
            {
                RestoreAudioAfterFmp4Reopen(decoder, lastFrameAbsPts, preReopenLastAudioPts);
                pacingStopwatch.Restart();
                return true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
        }

        return false;
    }

    private bool HandleActiveFmp4ReopenAtSegmentEdge(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        string currentOpenFilePath,
        TimeSpan playbackPosition,
        TimeSpan lastFrameAbsPts,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _playbackFmp4Reopens);
        Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var resumeTarget = lastFrameAbsPts;
        var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);
        if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)
            resumeTarget = currentSegmentStart.Value;
        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN pos_ms={(long)playbackPosition.TotalMilliseconds} resumePts_ms={(long)resumeTarget.TotalMilliseconds}");
        try
        {
            ReopenDecoderPlaybackFile(
                decoder,
                currentOpenFilePath,
                ref fileOpen,
                updateCurrentOpenPath: false,
                closeOnlyWhenOpen: false);
            var preReopenLastAudioPts = SuppressAudioForFmp4Reopen(decoder);
            cancellationToken.ThrowIfCancellationRequested();
            if (!SeekToWithCapTelemetry(decoder, resumeTarget, "fmp4_reopen", cancellationToken))
            {
                SetReopenFailure("fmp4_reopen", "seek_failed", resumeTarget);
                Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL path='{currentOpenFilePath}' offset_ms={(long)resumeTarget.TotalMilliseconds}");
                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "fmp4_reopen_seek_failed");
                return false;
            }
            RestoreAudioAfterFmp4Reopen(decoder, resumeTarget, preReopenLastAudioPts);
            pacingStopwatch.Restart();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return false;
        }
    }

    private long SuppressAudioForFmp4Reopen(FlashbackDecoder decoder)
    {
        var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
        Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
        decoder.AudioChunkCallback = null;
        return preReopenLastAudioPts;
    }

    private void RestoreAudioAfterFmp4Reopen(
        FlashbackDecoder decoder,
        TimeSpan resumeTarget,
        long preReopenLastAudioPts)
    {
        // Gate audio at the post-seek video PTS (seek target), not at
        // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
        // using it suppresses audio if the seek lands earlier, or creates
        // a gap if it lands later, causing WASAPI underruns and A/V desync.
        var audioGateTicks = resumeTarget.Ticks;
        Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)resumeTarget.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)resumeTarget.TotalMilliseconds}");
        RestoreAudioCallback(decoder, audioGateTicks);
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
