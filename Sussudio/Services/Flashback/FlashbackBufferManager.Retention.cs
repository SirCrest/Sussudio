using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    public long MaxDiskBytes => _options.MaxDiskBytes;

    /// <summary>
    /// Deletes all completed segment files and clears the index.
    /// Called during sink cycling to prevent stale segments with overlapping PTS.
    /// </summary>
    public void PurgeCompletedSegments()
    {
        lock (_indexLock)
        {
            if (_disposed)
            {
                Logger.Log("FLASHBACK_PURGE_SKIP reason=disposed");
                return;
            }

            long freedBytes = 0;
            for (int i = _completedSegments.Count - 1; i >= 0; i--)
            {
                if (TryDeleteFile(_completedSegments[i].Path))
                {
                    freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);
                    _completedSegments.RemoveAt(i);
                }
            }

            var activeSegmentBytes = _activeSegmentPath != null
                ? Math.Max(0, _totalDiskBytes - _completedSegmentBytes)
                : 0;

            // Also delete the active segment file (it has stale data)
            if (_activeSegmentPath != null)
            {
                if (TryDeleteFile(_activeSegmentPath))
                {
                    freedBytes = AddNonNegativeSaturated(freedBytes, activeSegmentBytes);
                    _activeSegmentPath = null; // Force new path generation on next AcquireSegmentPath()
                    Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
                    _previousActiveSegmentBytes = 0;
                }
                else
                {
                    Logger.Log($"FLASHBACK_PURGE_ACTIVE_RETAINED path='{Path.GetFileName(_activeSegmentPath)}'");
                }
            }

            if (_completedSegments.Count == 0 && _activeSegmentPath == null)
            {
                // Full purge succeeded - reset all counters.
                _completedSegmentBytes = 0;
                _completedSegmentSequence = 0;
                Interlocked.Exchange(ref _validStartPtsTicks, 0);
                _totalDiskBytes = 0;
                _totalBytesWritten = 0;
                _previousActiveSegmentBytes = 0;
                Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
            }
            else
            {
                // Partial purge - adjust byte counters for what we freed.
                _completedSegmentBytes = GetCompletedSegmentBytesSaturated();
                var retainedActiveBytes = _activeSegmentPath != null ? activeSegmentBytes : 0;
                _totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);
                Logger.Log($"FLASHBACK_PURGE_PARTIAL freed={freedBytes} remaining_segments={_completedSegments.Count}");
            }
        }
    }

    public void PurgeAllSegments()
    {
        lock (_indexLock)
        {
            if (_disposed)
            {
                Logger.Log("FLASHBACK_BUFFER_PURGE_SKIP reason=disposed");
                return;
            }

            if (IsSessionPreservedForRecoveryUnsafe())
            {
                Logger.Log($"FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved dir='{_sessionDirectory}'");
                return;
            }

            PurgeAllSegmentsCore(); // return value unused here; FLASHBACK_BUFFER_PURGE log covers it
        }
    }

    /// <summary>
    /// Deletes all segment files and resets all buffer state. Must be called under <see cref="_indexLock"/>.
    /// Does not gate on <see cref="_disposed"/>; callers are responsible for that check.
    /// Returns the number of segments purged and total bytes freed.
    /// </summary>
    private (int Segments, long FreedBytes) PurgeAllSegmentsCore()
    {
        // Must be called under _indexLock.
        var segmentCount = _completedSegments.Count + (_activeSegmentPath != null ? 1 : 0);
        var activeBytes = _activeSegmentPath != null
            ? Math.Max(0, _totalDiskBytes - _completedSegmentBytes)
            : 0;
        long freedBytes = 0;

        // Delete completed segments
        for (int i = _completedSegments.Count - 1; i >= 0; i--)
        {
            if (TryDeleteFile(_completedSegments[i].Path))
            {
                freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);
                _completedSegments.RemoveAt(i);
            }
        }

        // Delete active segment
        if (_activeSegmentPath != null)
        {
            if (TryDeleteFile(_activeSegmentPath))
            {
                freedBytes = AddNonNegativeSaturated(freedBytes, activeBytes);
                _activeSegmentPath = null;
                Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
                _previousActiveSegmentBytes = 0;
            }
            else
            {
                Logger.Log($"FLASHBACK_BUFFER_PURGE_ACTIVE_RETAINED path='{Path.GetFileName(_activeSegmentPath)}'");
            }
        }

        _completedSegmentBytes = GetCompletedSegmentBytesSaturated();
        if (_completedSegments.Count == 0 && _activeSegmentPath == null)
        {
            _completedSegmentSequence = 0;
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
            _totalBytesWritten = 0;
            _previousActiveSegmentBytes = 0;
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
        }
        else
        {
            var retainedActiveBytes = _activeSegmentPath != null ? activeBytes : 0;
            _totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);
        }

        // Clear under _indexLock so this is serialized with PauseEviction/ResumeEviction.
        _evictionPauseCount = 0;
        Logger.Log($"FLASHBACK_BUFFER_PURGE segments={segmentCount} freed_bytes={freedBytes}");
        return (segmentCount, freedBytes);
    }

    private void EvictOldestSegments()
    {
        // Must be called under _indexLock
        if (IsSessionPreservedForRecoveryUnsafe())
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_SKIP reason=recovery_preserved dir='{_sessionDirectory}' segments={_completedSegments.Count}");
            return;
        }

        var validStart = TimeSpan.FromTicks(Interlocked.Read(ref _validStartPtsTicks));
        var evictedCount = 0;
        long evictedBytes = 0;

        // Remove segments that are entirely before the valid window
        while (_completedSegments.Count > 0)
        {
            var oldest = _completedSegments[0];
            if (oldest.EndPts <= validStart)
            {
                if (DeleteFileForEviction(oldest.Path, oldest.SizeBytes, "valid_window"))
                {
                    evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                    _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
                    _completedSegments.RemoveAt(0);
                    evictedCount++;
                }
                else
                {
                    break; // Can't delete - file is locked, stop evicting
                }
            }
            else
            {
                break;
            }
        }

        // Also evict if total disk bytes exceed limit
        while (_completedSegments.Count > 0 && _completedSegmentBytes > _options.MaxDiskBytes)
        {
            var oldest = _completedSegments[0];
            if (DeleteFileForEviction(oldest.Path, oldest.SizeBytes, "disk_budget"))
            {
                evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
                _completedSegments.RemoveAt(0);
                evictedCount++;
            }
            else
            {
                break; // Can't delete - file is locked, stop evicting
            }
        }

        if (evictedCount > 0)
        {
            _totalDiskBytes = SubtractNonNegative(_totalDiskBytes, evictedBytes);
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_EVICT count={evictedCount} evicted_bytes={evictedBytes} remaining_segments={_completedSegments.Count}");
        }
    }

    private bool DeleteFileForEviction(string filePath, long sizeBytes, string reason)
    {
        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_DELETE_SKIP reason=no_session path='{filePath}'");
            return false;
        }

        if (!IsPathInSessionDirectory(filePath))
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_DELETE_SKIP reason=outside_session path='{filePath}'");
            return false;
        }

        string sessionRoot;
        string fullPath;
        try
        {
            sessionRoot = FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator(Path.GetFullPath(_sessionDirectory));
            fullPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_DELETE_SKIP reason=path_error path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }

        return DeleteEvictedFile(fullPath, sessionRoot, sizeBytes, reason);
    }

    private static bool DeleteEvictedFile(string fullPath, string sessionRoot, long sizeBytes, string reason)
    {
        if (!FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot))
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_DELETE_SKIP reason=outside_session path='{fullPath}'");
            return false;
        }

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            File.Delete(fullPath);
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_BUFFER_SEGMENT_EVICT_DELETED reason={reason} " +
                $"path='{Path.GetFileName(fullPath)}' size_bytes={Math.Max(0, sizeBytes)} elapsed_ms={elapsedMs:0.###}");
            return true;
        }
        catch (Exception ex)
        {
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_BUFFER_EVICT_DELETE_WARN reason={reason} path='{fullPath}' " +
                $"type={ex.GetType().Name} msg='{ex.Message}' elapsed_ms={elapsedMs:0.###}");
            return false;
        }
    }

    private bool TryDeleteFile(string filePath)
    {
        if (!IsPathInSessionDirectory(filePath))
        {
            Logger.Log($"FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session path='{filePath}'");
            return false;
        }

        try
        {
            File.Delete(filePath); // No-op if file doesn't exist (.NET 8+)
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_DELETE_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }
}
