using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Continuous playback loop and segment-edge handling ---

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
            if (TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) &&
                driftMs < -FrameSkipThresholdMs)
            {
                var skipped = 0;
                while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (commandChannel.Reader.TryPeek(out _))
                    {
                        ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip_command_pending");
                        Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped} drift_ms={driftMs:F1}");
                        return true;
                    }

                    // Release the frame without displaying it
                    ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip");
                    RecordPlaybackDroppedFrame("av_sync_skip");
                    skipped++;

                    if (!TryReadNextPlaybackFrame(decoder, prebufferedFrames, out videoFrame, cancellationToken))
                    {
                        // EOS during skip — log partial progress so the diagnostic gap
                        // doesn't hide a long catch-up burst that the user may notice.
                        if (skipped > 0)
                        {
                            Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped} drift_at_eos_ms={driftMs:F1}");
                        }
                        return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);
                    }
                    if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
                    {
                        if (skipped > 0)
                        {
                            Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped} drift_at_budget_ms={driftMs:F1}");
                        }
                        ReleaseHeldFrameBestEffort(videoFrame, "software_decode_over_budget");
                        SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "playback_skip");
                        return false;
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

            // Use the encoder's frame rate as ground truth — the buffer manager knows
            // exactly what rate we told NVENC to encode at. TS container metadata can
            // report doubled rates (e.g. 240 for 120fps) and the decoder's PTS calibration
            // needs ~10 frames to correct. The encode rate is authoritative from frame 1.
            frameDuration = ResolveFrameDuration(decoder);
            TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);

            PaceFrameInterval(pacingStopwatch, frameDuration, videoFrame.Pts.Ticks);
            UpdateCadenceMetrics(pacingStopwatch, frameDuration.TotalMilliseconds);

            // Log A/V drift every ~1 second for diagnostics
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
            var nextFile = currentOpenFilePath != null
                ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
                : null;
            if (nextFile != null && !IsSamePlaybackPath(nextFile, currentOpenFilePath))
            {
                var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);
                if (currentOpenFilePath != null &&
                    nextSegmentStart.HasValue &&
                    nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250))
                {
                    Interlocked.Increment(ref _playbackFmp4Reopens);
                    Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} resumePts_ms={(long)lastFrameAbsPts.TotalMilliseconds} nextStart_ms={(long)nextSegmentStart.Value.TotalMilliseconds}");
                    try
                    {
                        decoder.CloseFile();
                        fileOpen = false;
                        decoder.OpenFile(currentOpenFilePath);
                        fileOpen = true;
                        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
                        var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
                        Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
                        decoder.AudioChunkCallback = null;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (SeekToWithCapTelemetry(decoder, lastFrameAbsPts, "fmp4_reopen_before_segment_switch", cancellationToken))
                        {
                            // Gate audio at the post-seek video PTS (seek target), not at
                            // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
                            // using it suppresses audio if the seek lands earlier, or creates
                            // a gap if it lands later, causing WASAPI underruns and A/V desync.
                            var audioGateTicks = lastFrameAbsPts.Ticks;
                            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)lastFrameAbsPts.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)lastFrameAbsPts.TotalMilliseconds}");
                            RestoreAudioCallback(decoder, audioGateTicks);
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
                    // Gate audio at last played position, not seek target — audio between
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
                        return false;
                    }
                    RestoreAudioCallback(decoder, audioGate);
                    ResetPlaybackPtsCadenceBaseline();
                    pacingStopwatch.Restart();
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
                    return false;
                }
            }

            // Active fMP4 segment: the demuxer cached the file structure at open
            // time and won't see new fragments without re-opening. Close and re-open
            // the same file, then seek to where playback left off.
            // Only for fMP4; .ts handles appended data via eof_reached reset.
            if (IsActiveFmp4Segment(currentOpenFilePath) && currentOpenFilePath != null)
            {
                Interlocked.Increment(ref _playbackFmp4Reopens);
                Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var resumeTarget = lastFrameAbsPts;
                var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);
                if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)
                    resumeTarget = currentSegmentStart.Value;
                Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN pos_ms={(long)pos.TotalMilliseconds} resumePts_ms={(long)resumeTarget.TotalMilliseconds}");
                try
                {
                    decoder.CloseFile();
                    fileOpen = false;
                    decoder.OpenFile(currentOpenFilePath);
                    fileOpen = true;
                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
                    var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
                    Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
                    decoder.AudioChunkCallback = null;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!SeekToWithCapTelemetry(decoder, resumeTarget, "fmp4_reopen", cancellationToken))
                    {
                        SetReopenFailure("fmp4_reopen", "seek_failed", resumeTarget);
                        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL path='{currentOpenFilePath}' offset_ms={(long)resumeTarget.TotalMilliseconds}");
                        RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "fmp4_reopen_seek_failed");
                        return false;
                    }
                    // Gate audio at the post-seek video PTS (seek target), not at
                    // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
                    // using it suppresses audio if the seek lands earlier, or creates
                    // a gap if it lands later, causing WASAPI underruns and A/V desync.
                    var audioGateTicks = resumeTarget.Ticks;
                    Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)resumeTarget.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)resumeTarget.TotalMilliseconds}");
                    RestoreAudioCallback(decoder, audioGateTicks);
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
