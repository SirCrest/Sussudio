using System;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    /// <summary>
    /// Seeks to the nearest keyframe at or before <paramref name="target"/>.
    /// Fast seek suitable for scrubbing.
    /// </summary>
    public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();

        var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);
        var result = ffmpeg.av_seek_frame(
            _formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);
        cancellationToken.ThrowIfCancellationRequested();

        if (result < 0)
        {
            var streamSeekResult = result;
            var timestampUs = ToAvTimeBaseTimestamp(target);
            result = ffmpeg.av_seek_frame(
                _formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);
            cancellationToken.ThrowIfCancellationRequested();

            if (result < 0)
            {
                Logger.Log(
                    $"FLASHBACK_DECODER_SEEK_WARN keyframe_seek_failed code={result} " +
                    $"stream_code={streamSeekResult} target_ms={(long)target.TotalMilliseconds} stream_ts={streamTimestamp}");
                return false;
            }

            Logger.Log(
                $"FLASHBACK_DECODER_SEEK_FALLBACK_OK target_ms={(long)target.TotalMilliseconds} " +
                $"stream_ts={streamTimestamp} us_ts={timestampUs}");
        }

        if (_videoCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_videoCodecCtx);
        }

        if (_audioCodecCtx != null)
        {
            ffmpeg.avcodec_flush_buffers(_audioCodecCtx);
        }

        // Clear any stashed pending frame - it's from before the seek point.
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "seek_keyframe_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        _suppressRecoverableSeekLogsForNextVideoFrame = true;

        Logger.Log(
            $"FLASHBACK_DECODER_SEEK_OK target_ms={(long)target.TotalMilliseconds} " +
            $"stream_index={_videoStreamIndex} stream_ts={streamTimestamp}");
        return true;
    }

    /// <summary>
    /// Seeks to the exact frame at <paramref name="target"/> by first seeking to the
    /// nearest preceding keyframe, then decoding forward until the target PTS is reached.
    /// </summary>
    public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)
    {
        ThrowIfNotOpen();
        cancellationToken.ThrowIfCancellationRequested();
        _lastSeekHitForwardDecodeCap = false;

        if (!SeekToKeyframe(target, cancellationToken))
        {
            return false;
        }

        // Decode forward until we reach (or pass) the target PTS.
        // Stash the target frame so the next TryDecodeNextVideoFrame() returns it
        // instead of skipping past it (fixes off-by-one on seek).
        // Cap at 960 frames (8s at 120fps) to prevent CPU saturation on scrub.
        const int maxForwardFrames = 960;
        var targetTicks = target.Ticks;
        DecodedVideoFrame? bestFrame = null;
        var bestFrameTransferred = false;
        try
        {
            for (var i = 0; i < maxForwardFrames; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))
                {
                    // Reached EOF before target - return best frame if we have one.
                    if (bestFrame != null)
                    {
                        _currentPosition = bestFrame.Value.Pts;
                        _pendingVideoFrame = bestFrame.Value;
                        _hasPendingVideoFrame = true;
                        bestFrameTransferred = true;
                        return true;
                    }

                    return false;
                }

                if (frame.Pts.Ticks >= targetTicks)
                {
                    _currentPosition = frame.Pts;
                    _pendingVideoFrame = frame;
                    _hasPendingVideoFrame = true;
                    if (bestFrame != null)
                    {
                        ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_replace_best");
                        bestFrame = null;
                    }

                    return true;
                }

                // Keep the closest frame in case we hit the limit.
                if (bestFrame != null) ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_superseded");
                bestFrame = frame;
            }

            // Hit frame limit - return the closest frame we decoded.
            if (bestFrame != null)
            {
                var bestMs = (long)bestFrame.Value.Pts.TotalMilliseconds;
                var targetMs = (long)target.TotalMilliseconds;
                var gapMs = targetMs - bestMs;
                // One frame interval in ms (guard against zero/negative frame rate)
                var frameIntervalMs = _frameRate > 0.0 ? (long)(1000.0 / _frameRate) : 0L;
                if (gapMs > frameIntervalMs)
                {
                    _lastSeekHitForwardDecodeCap = true;
                    Interlocked.Increment(ref _seekToCapHits);
                    Logger.Log($"FLASHBACK_DECODER_SEEK_CAP_HIT target_ms={targetMs} best_ms={bestMs} gap_ms={gapMs} frames_decoded={maxForwardFrames}");
                }
                else
                {
                    Logger.Log($"FLASHBACK_DECODER_SEEK_FRAME_LIMIT target_ms={targetMs} best_ms={bestMs} frames={maxForwardFrames}");
                }

                _currentPosition = bestFrame.Value.Pts;
                _pendingVideoFrame = bestFrame.Value;
                _hasPendingVideoFrame = true;
                bestFrameTransferred = true;
                return true;
            }

            return false;
        }
        finally
        {
            if (!bestFrameTransferred && bestFrame != null)
            {
                ReleaseHeldFrameBestEffort(bestFrame.Value, "seek_best_abandoned");
            }
        }
    }

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        var microseconds = value.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)microseconds;
    }

    private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)
    {
        if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return 0;
        }

        var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;
        if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)timestamp;
    }
}
