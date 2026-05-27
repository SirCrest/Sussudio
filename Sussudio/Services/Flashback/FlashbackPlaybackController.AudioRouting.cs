using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Audio routing and prebuffer ---

    private const double PlaybackAudioPrebufferTargetMs = 180.0;
    private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;
    private const int PlaybackAudioPrebufferTimeoutMs = 1000;
    private const int PlaybackAudioPrebufferRetryDelayMs = 20;
    private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;

    private void ApplyAudioRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        switch (_state)
        {
            case FlashbackPlaybackState.Live:
                RestoreLiveAudio();
                break;
            case FlashbackPlaybackState.Playing:
                SuppressLiveAudio();
                SafeResumeRendering(operation);
                break;
            case FlashbackPlaybackState.Paused:
            case FlashbackPlaybackState.Scrubbing:
                SuppressLiveAudio();
                SafePauseRendering(operation);
                break;
        }
    }

    private void ApplyPreviewRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        if (_state == FlashbackPlaybackState.Live)
        {
            SafeResumePreviewSubmission(operation);
        }
        else
        {
            SafeSuppressPreviewSubmission(operation);
        }
    }

    private void SuppressLiveAudio()
    {
        try
        {
            _audioCapture?.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=suppress_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeFlushPlayback("suppress_live_audio");
    }

    private void RestoreLiveAudio()
    {
        SafeFlushPlayback("restore_live_audio");
        // Reconnect audio feed before resuming rendering to avoid silence/stutter.
        try
        {
            if (_audioCapture != null && _audioPlayback != null)
            {
                _audioCapture.SetPlayback(_audioPlayback);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=restore_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeResumeRendering("restore_live_audio");
    }

    private void SafeSuppressPreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.SuppressPreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.ResumePreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafePauseRendering(string operation)
    {
        try
        {
            _audioPlayback?.PauseRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumeRendering(string operation)
    {
        try
        {
            _audioPlayback?.ResumeRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePlaybackRendering(string operation)
    {
        try
        {
            _audioPlayback?.ResumeRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume_playback operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeFlushPlayback(string operation)
    {
        try
        {
            _audioPlayback?.Flush();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

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

    private void RestoreAudioCallback(FlashbackDecoder decoder, long audioStartGateTicks = 0)
    {
        // Audio start gate: drop any audio chunk with PTS before this value.
        // This filters stale audio from keyframe-to-target decode after a seek.
        var videoPtsGate = audioStartGateTicks > 0
            ? audioStartGateTicks
            : Interlocked.Read(ref _lastVideoPtsTicks);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);

        if (_audioPlayback == null)
        {
            decoder.AudioChunkCallback = null;
            return;
        }

        decoder.AudioChunkCallback = chunk =>
        {
            var pb = _audioPlayback;
            if (pb == null)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_missing_audio_sink");
                return;
            }

            if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason} pts_ms={(long)chunk.Pts.TotalMilliseconds} valid_bytes={chunk.ValidLength} buffer_bytes={chunk.Samples?.Length ?? 0}");
                ReturnPlaybackAudioChunkBestEffort(chunk, $"playback_audio_{invalidReason}");
                return;
            }

            // Skip invalid or non-monotonic PTS (L8 fix).
            var prevPts = Interlocked.Read(ref _lastAudioPtsTicks);
            if (chunk.Pts.Ticks <= 0 || chunk.Pts.Ticks < prevPts)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_non_monotonic_pts");
                return;
            }

            // Skip audio from the keyframe-to-target forward decode after a seek.
            if (videoPtsGate > 0 && chunk.Pts.Ticks < videoPtsGate)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_before_gate");
                return;
            }

            Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
            pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
        };
    }

    private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)
    {
        if (chunk.Samples == null)
        {
            reason = "null_samples";
            return false;
        }

        if (chunk.ValidLength <= 0)
        {
            reason = "invalid_length";
            return false;
        }

        if (chunk.ValidLength > chunk.Samples.Length)
        {
            reason = "length_exceeds_buffer";
            return false;
        }

        const int playbackAudioBlockAlign = 2 * sizeof(float);
        if (chunk.ValidLength % playbackAudioBlockAlign != 0)
        {
            reason = "unaligned_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)
    {
        try
        {
            if (chunk.Samples is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(chunk.Samples);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    // Last sampled audio rendering PTS plus the wall-clock anchor used for
    // extrapolated audio-master pacing between WASAPI render callbacks.
    private long _audioClockPtsTicks;
    private long _audioClockWallTicks;

    private long _playbackAudioMasterDelayDoubles;
    private long _playbackAudioMasterDelayShrinks;
    private long _playbackAudioMasterFallbacks;
    private long _playbackAudioMasterUnavailableFallbacks;
    private long _playbackAudioMasterStaleFallbacks;
    private long _playbackAudioMasterDriftOutlierFallbacks;
    private string _playbackAudioMasterLastFallbackReason = string.Empty;
    private double _playbackAudioMasterLastFallbackDriftMs;
    private double _playbackAudioMasterLastFallbackClockAgeMs;
    private string _pendingAudioMasterFallbackReason = string.Empty;
    private double _pendingAudioMasterFallbackDriftMs;
    private long _pendingAudioMasterFallbackClockAgeTicks;

    private const long AudioMasterClockStaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;

    public long PlaybackAudioMasterDelayDoubles => Interlocked.Read(ref _playbackAudioMasterDelayDoubles);
    public long PlaybackAudioMasterDelayShrinks => Interlocked.Read(ref _playbackAudioMasterDelayShrinks);
    public long PlaybackAudioMasterFallbacks => Interlocked.Read(ref _playbackAudioMasterFallbacks);
    public long PlaybackAudioMasterUnavailableFallbacks => Interlocked.Read(ref _playbackAudioMasterUnavailableFallbacks);
    public long PlaybackAudioMasterStaleFallbacks => Interlocked.Read(ref _playbackAudioMasterStaleFallbacks);
    public long PlaybackAudioMasterDriftOutlierFallbacks => Interlocked.Read(ref _playbackAudioMasterDriftOutlierFallbacks);
    public string PlaybackAudioMasterLastFallbackReason => Volatile.Read(ref _playbackAudioMasterLastFallbackReason);
    public double PlaybackAudioMasterLastFallbackDriftMs => _playbackAudioMasterLastFallbackDriftMs;
    public double PlaybackAudioMasterLastFallbackClockAgeMs => _playbackAudioMasterLastFallbackClockAgeMs;

    /// <summary>
    /// Audio-video drift in milliseconds. Positive = audio ahead, negative = audio behind.
    /// Uses the PTS of the chunk WASAPI is currently rendering (not just enqueued).
    /// </summary>
    public double AvDriftMs
    {
        get
        {
            var renderingPts = _audioPlayback?.RenderingPtsTicks ?? 0;
            var videoPts = Interlocked.Read(ref _lastVideoPtsTicks);
            if (renderingPts == 0 || videoPts == 0) return 0;
            return TimeSpan.FromTicks(renderingPts - videoPts).TotalMilliseconds;
        }
    }

    // --- Audio-master playback pacing ---

    private void RefreshAudioMasterClock()
    {
        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }
    }

    private bool TryGetFreshAudioMasterClock(
        out long extrapolatedAudioTicks,
        out long wallElapsedTicks,
        out bool hasAudioClockSample)
    {
        RefreshAudioMasterClock();

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        hasAudioClockSample = audioClockPts > 0;
        extrapolatedAudioTicks = 0;
        wallElapsedTicks = 0;
        if (!hasAudioClockSample)
        {
            return false;
        }

        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
        if (wallElapsedTicks > AudioMasterClockStaleThresholdTicks)
        {
            return false;
        }

        extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
        return true;
    }

    /// <summary>
    /// Re-syncs the cached audio clock from WASAPI (matching the resync done by
    /// <see cref="PaceFrameInterval"/>) and returns the extrapolated drift in
    /// milliseconds (positive = video ahead of audio). Returns false if the audio clock
    /// is unavailable, has never been sampled, or is stale (>200ms since last update) -
    /// callers must fall back to wall-clock pacing in that case.
    /// </summary>
    private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)
    {
        driftMs = 0;
        if (!TryGetFreshAudioMasterClock(out var extrapolatedAudioTicks, out _, out _))
        {
            return false;
        }

        driftMs = (videoPtsTicks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
        return true;
    }

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
}
