using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
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

            // Update disk bytes tracking
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

            // Advance _segmentStartPts to prevent infinite retry on every frame
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }
}
