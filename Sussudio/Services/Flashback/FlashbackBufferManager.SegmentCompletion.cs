using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        if (_disposed)
        {
            Logger.Log("FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            Logger.Log("FLASHBACK_BUFFER_SEGMENT_SKIP reason=empty_path");
            return;
        }

        if (endPts <= startPts)
        {
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_SKIP reason=invalid_range path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}");
            return;
        }

        var safeSizeBytes = Math.Max(0, sizeBytes);
        lock (_indexLock)
        {
            if (_disposed)
            {
                Logger.Log("FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
                return;
            }

            if (!IsPathInSessionDirectory(path))
            {
                Logger.Log($"FLASHBACK_BUFFER_SEGMENT_SKIP reason=outside_session path='{Path.GetFileName(path)}'");
                return;
            }

            if (!File.Exists(path))
            {
                Logger.Log($"FLASHBACK_BUFFER_SEGMENT_SKIP reason=missing_file path='{Path.GetFileName(path)}'");
                return;
            }

            var pathIsActiveSegment = IsSameSegmentPath(_activeSegmentPath, path);
            var existingIndex = _completedSegments.FindIndex(seg => IsSameSegmentPath(seg.Path, path));
            if (existingIndex >= 0)
            {
                if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment))
                {
                    Logger.Log($"FLASHBACK_BUFFER_SEGMENT_SKIP reason=duplicate_path path='{Path.GetFileName(path)}'");
                }

                return;
            }

            if (_completedSegments.Count > 0 && startPts < _completedSegments[^1].EndPts)
            {
                var previous = _completedSegments[^1];
                Logger.Log(
                    $"FLASHBACK_BUFFER_SEGMENT_SKIP reason=non_monotonic path='{Path.GetFileName(path)}' " +
                    $"start_ms={(long)startPts.TotalMilliseconds} previous_end_ms={(long)previous.EndPts.TotalMilliseconds}");
                return;
            }

            if (safeSizeBytes >= _previousActiveSegmentBytes)
            {
                Interlocked.Add(ref _totalBytesWritten, safeSizeBytes - _previousActiveSegmentBytes);
            }

            var sequenceNumber = _completedSegmentSequence++;
            _completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, safeSizeBytes)
            {
                AllowSamePathExtension = pathIsActiveSegment
            });
            _completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, safeSizeBytes);
            _previousActiveSegmentBytes = pathIsActiveSegment ? safeSizeBytes : 0;
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_COMPLETE seq={sequenceNumber} path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} size_bytes={safeSizeBytes}");

            if (!(Volatile.Read(ref _evictionPauseCount) > 0))
            {
                EvictOldestSegments();
            }
        }
    }

    private bool TryExtendCompletedSegment(
        int existingIndex,
        string path,
        TimeSpan startPts,
        TimeSpan endPts,
        long sizeBytes,
        bool pathIsActiveSegment)
    {
        if (existingIndex != _completedSegments.Count - 1)
        {
            return false;
        }

        var existing = _completedSegments[existingIndex];
        var extendedStartPts = startPts < existing.StartPts ? startPts : existing.StartPts;
        var extendedEndPts = endPts > existing.EndPts ? endPts : existing.EndPts;
        var extendedSizeBytes = Math.Max(existing.SizeBytes, sizeBytes);
        if (extendedStartPts == existing.StartPts &&
            extendedEndPts == existing.EndPts &&
            extendedSizeBytes == existing.SizeBytes)
        {
            return false;
        }

        if (!pathIsActiveSegment && !existing.AllowSamePathExtension)
        {
            return false;
        }

        if (extendedSizeBytes >= _previousActiveSegmentBytes)
        {
            Interlocked.Add(ref _totalBytesWritten, extendedSizeBytes - _previousActiveSegmentBytes);
        }

        var sizeDeltaBytes = SubtractNonNegative(extendedSizeBytes, existing.SizeBytes);
        _completedSegments[existingIndex] = existing with
        {
            StartPts = extendedStartPts,
            EndPts = extendedEndPts,
            SizeBytes = extendedSizeBytes,
            AllowSamePathExtension = pathIsActiveSegment
        };
        _completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, sizeDeltaBytes);
        _previousActiveSegmentBytes = pathIsActiveSegment ? extendedSizeBytes : 0;
        _totalDiskBytes = Math.Max(_totalDiskBytes, _completedSegmentBytes);
        Logger.Log(
            $"FLASHBACK_BUFFER_SEGMENT_EXTEND seq={existing.SequenceNumber} path='{Path.GetFileName(path)}' " +
            $"start_ms={(long)extendedStartPts.TotalMilliseconds} end_ms={(long)extendedEndPts.TotalMilliseconds} " +
            $"size_bytes={extendedSizeBytes} size_delta_bytes={sizeDeltaBytes}");

        if (!(Volatile.Read(ref _evictionPauseCount) > 0))
        {
            EvictOldestSegments();
        }

        return true;
    }
}
