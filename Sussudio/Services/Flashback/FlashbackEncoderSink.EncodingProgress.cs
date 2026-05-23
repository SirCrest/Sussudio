using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void OnVideoFrameEncoded()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
        var encoded = Interlocked.Increment(ref _encodedVideoFrames);

        var pts = ResolveEncoderPts();
        if (pts > TimeSpan.Zero)
        {
            _bufferManager.UpdateLatestPts(pts);

            // Segment rotation happens on the encoding thread, so no extra lock is needed here.
            if (_segmentDuration > TimeSpan.Zero && pts - _segmentStartPts >= _segmentDuration)
            {
                _ = RotateSegment(pts);
            }
        }

        // Refresh disk bytes ~4 Hz so the monotonic counter stays current for UI
        // bitrate sampling; the prior frame-count gate plateaued for ~5 s at 60 fps.
        var nowMs = Environment.TickCount64;
        if (nowMs - _lastDiskBytesUpdateMs >= 250)
        {
            _lastDiskBytesUpdateMs = nowMs;
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
        }

        // NOTE: This event fires on the encoding background thread, NOT the UI thread.
        // Handlers must marshal to DispatcherQueue if they need to update UI state.
        if (!_disposed && Volatile.Read(ref _recordingActive) == 1)
        {
            try
            {
                FrameEncoded?.Invoke(this, encoded);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private TimeSpan ResolveEncoderPts()
    {
        var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);
        var seconds = _encoder.NextVideoPts / frameRate;
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            return _ptsBaseOffset;
        }

        if (seconds >= TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.MaxValue;
        }

        var delta = TimeSpan.FromSeconds(seconds);
        return _ptsBaseOffset > TimeSpan.MaxValue - delta
            ? TimeSpan.MaxValue
            : _ptsBaseOffset + delta;
    }

    private bool RotateSegment(TimeSpan currentPts)
    {
        string? completedPath = null;
        string? newPath = null;
        var encoderRotated = false;
        try
        {
            completedPath = _tsFilePath;
            var completedStartPts = _segmentStartPts;
            newPath = _bufferManager.GenerateSegmentPath();

            // RotateOutput flushes encoder queues, writes trailer, then resets
            // TotalBytesWritten to 0 for the new segment. PreviousTotalBytes
            // in the result includes all drain/trailer bytes.
            var result = _encoder.RotateOutput(newPath);
            var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));
            encoderRotated = true;

            _segmentStartPts = currentPts;
            _tsFilePath = newPath;
            _bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);
            Interlocked.Exchange(ref _segmentStartBytes, _encoder.TotalBytesWritten);

            _bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);

            // Update disk bytes tracking.
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
            _lastDiskBytesUpdateMs = Environment.TickCount64;

            Logger.Log(
                $"FLASHBACK_SINK_ROTATE new_segment='{Path.GetFileName(newPath)}' " +
                $"prev_bytes={segmentBytes} " +
                $"segment_start_ms={(long)currentPts.TotalMilliseconds}");
            return true;
        }
        catch (Exception ex)
        {
            if (newPath != null && !encoderRotated)
            {
                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);
            }

            Interlocked.Increment(ref _segmentRotationFailures);

            // Register the segment that was open before the rotation attempt so its
            // data remains visible in the buffer index even though rotation failed.
            if (completedPath != null)
            {
                try
                {
                    var failPts = ResolveEncoderPts();
                    if (failPts > _segmentStartPts)
                    {
                        var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);
                        Logger.Log(
                            $"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED " +
                            $"path='{completedPath}' frames={_encoder.VideoPacketsWritten} " +
                            $"start_ms={(long)_segmentStartPts.TotalMilliseconds} end_ms={(long)failPts.TotalMilliseconds}");
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                }
            }

            // Advance _segmentStartPts to prevent infinite retry on every frame.
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }
}
