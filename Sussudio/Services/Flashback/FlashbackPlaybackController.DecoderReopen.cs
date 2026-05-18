using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Decoder seek/reopen recovery ---

    private bool TrySeekWithActiveFmp4Reopen(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
        {
            return true;
        }

        // Active fMP4 segment: demuxer fragment index is stale. Reopen and retry.
        // MPEG-TS handles appended data via eof_reached reset and does not need reopening.
        if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
        {
            if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))
            {
                SetReopenFailure(reason, "near_live", seekTarget);
                return false;
            }

            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);
        }

        if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))
        {
            return true;
        }

        SetReopenFailure(reason, "seek_failed", seekTarget);
        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
        return false;
    }

    private bool TrySeekAdjacentSegmentStart(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        out TimeSpan effectiveSeekTarget,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        effectiveSeekTarget = seekTarget;
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        var nextPath = _bufferManager.GetNextSegmentFile(currentPath);
        if (string.IsNullOrWhiteSpace(nextPath) || IsSamePlaybackPath(nextPath, currentPath))
        {
            return false;
        }

        var nextStart = _bufferManager.GetSegmentStartPts(nextPath);
        if (!nextStart.HasValue)
        {
            return false;
        }

        var targetGap = (nextStart.Value - seekTarget).Duration();
        if (targetGap > AdjacentSegmentSeekFallbackWindow)
        {
            return false;
        }

        effectiveSeekTarget = seekTarget < nextStart.Value ? nextStart.Value : seekTarget;
        try
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK reason={reason} " +
                $"from='{System.IO.Path.GetFileName(currentPath)}' next='{System.IO.Path.GetFileName(nextPath)}' " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} effective_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(nextPath);
            fileOpen = true;
            _currentOpenFilePath = nextPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, effectiveSeekTarget, reason, cancellationToken))
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                ResetPlaybackPtsCadenceBaseline();
                return true;
            }

            SetReopenFailure(reason, "adjacent_seek_failed", effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL reason={reason} path='{nextPath}' offset_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR reason={reason} path='{nextPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
        }
    }

    private bool TryReopenCurrentFileAndSeek(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(currentPath);
            fileOpen = true;
            _currentOpenFilePath = currentPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
        }
    }

    private bool TryReopenCurrentFileAndSeekKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(currentPath);
            fileOpen = true;
            _currentOpenFilePath = currentPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            cancellationToken.ThrowIfCancellationRequested();
            if (decoder.SeekToKeyframe(seekTarget, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "keyframe_seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
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

    private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= TimeSpan.Zero)
        {
            return false;
        }

        var distanceFromLive = seekTarget >= latestPts
            ? TimeSpan.Zero
            : latestPts - seekTarget;
        if (distanceFromLive > ActiveFmp4ReopenNearLiveGuard)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE reason={reason} target_ms={(long)seekTarget.TotalMilliseconds} latest_ms={(long)latestPts.TotalMilliseconds} distance_ms={(long)distanceFromLive.TotalMilliseconds} guard_ms={(long)ActiveFmp4ReopenNearLiveGuard.TotalMilliseconds}");
        return true;
    }
}
