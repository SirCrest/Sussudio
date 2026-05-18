using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    public long MaxDiskBytes => _options.MaxDiskBytes;

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

}
