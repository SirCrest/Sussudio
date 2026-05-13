using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback frame-rate, snap policy, and cadence accounting ---

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

        var fps = _bufferManager.EncodeFrameRate;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        fps = Math.Min(fps, MaxPlaybackFrameRate);
        var backoff = TimeSpan.FromSeconds(1.0 / fps);
        if (latestPts - frozenValidStart <= backoff)
        {
            return latestPts;
        }

        return latestPts - backoff;
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
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private void UpdateDecoderHwAccel(FlashbackDecoder decoder)
    {
        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
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
}
