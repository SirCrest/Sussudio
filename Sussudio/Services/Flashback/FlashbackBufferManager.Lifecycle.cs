using System;
using System.IO;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    /// <summary>Sets the file extension for new segments (e.g. ".ts" or ".mp4").</summary>
    public void SetSegmentExtension(string extension)
    {
        var normalizedExtension = FlashbackSessionRecoveryScanner.NormalizeSegmentExtension(extension);
        lock (_indexLock)
        {
            ThrowIfDisposed();
            _segmentExtension = normalizedExtension;
        }
    }

    public void Initialize(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        lock (_indexLock)
        {
            ThrowIfDisposed();

            var tempDirectory = Path.GetFullPath(_options.TempDirectory);
            Directory.CreateDirectory(tempDirectory);
            var sessionDirectory = FlashbackSessionRecoveryScanner.BuildSessionDirectory(tempDirectory, sessionId);
            Directory.CreateDirectory(sessionDirectory);

            // Clean up orphaned export temp files from previous sessions
            FlashbackExporter.CleanupOrphanedTempFiles(tempDirectory);
            FlashbackStartupCacheCleanup.CleanupStaleRootSegmentFiles(tempDirectory);
            FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);
            var cacheCleanup = FlashbackStartupSessionCacheBudget.CleanupSessionCacheBudget(
                tempDirectory,
                sessionDirectory,
                FlashbackStartupSessionCacheBudget.CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));

            _activeSegmentPath = null;
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
            _completedSegments.Clear();
            _completedSegmentBytes = 0;
            _completedSegmentSequence = 0;
            _nextSegmentIndex = 0;
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
            _totalBytesWritten = 0;
            Interlocked.Exchange(ref _startupCacheBudgetBytes, cacheCleanup.BudgetBytes);
            Interlocked.Exchange(ref _startupCacheBytes, cacheCleanup.RemainingBytes);
            Interlocked.Exchange(ref _startupCacheSessionCount, cacheCleanup.SessionCount);
            Interlocked.Exchange(ref _startupCacheDeletedSessionCount, cacheCleanup.DeletedSessionCount);
            Interlocked.Exchange(ref _startupCacheFreedBytes, cacheCleanup.FreedBytes);
            _recordingStartPts = TimeSpan.Zero;
            _recordingEndPts = TimeSpan.Zero;
            _previousActiveSegmentBytes = 0;
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
            Interlocked.Exchange(ref _evictionPauseCount, 0);
            _preserveSessionForRecovery = false;
            _sessionId = sessionId;
            _sessionDirectory = sessionDirectory;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{tempDirectory}' session_dir='{sessionDirectory}'");
        }
    }

    public void Dispose()
    {
        lock (_indexLock)
        {
            if (_disposed)
            {
                return;
            }

            Logger.Log($"FLASHBACK_BUFFER_DISPOSE file={_activeSegmentPath != null} segments={_completedSegments.Count}");

            if (IsSessionPreservedForRecoveryUnsafe())
            {
                _disposed = true;
                Logger.Log($"FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY dir='{_sessionDirectory}' segments={_completedSegments.Count} active_file={_activeSegmentPath != null}");
                return;
            }

            // Purge segment files before marking disposed so PurgeAllSegmentsCore can
            // run fully. This ensures the session directory is empty before the
            // Directory.Delete call below, preventing orphaned multi-GB segment files.
            var (purgedSegments, purgedBytes) = PurgeAllSegmentsCore();
            Logger.Log($"FLASHBACK_BUFFER_DISPOSE_PURGE segments={purgedSegments} bytes={purgedBytes}");

            _disposed = true;

            if (!string.IsNullOrWhiteSpace(_sessionDirectory))
            {
                try
                {
                    Directory.Delete(_sessionDirectory, recursive: false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_BUFFER_SESSIONDIR_DELETE_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }
        }
    }

    public bool IsSessionPreservedForRecovery
    {
        get
        {
            lock (_indexLock)
            {
                return IsSessionPreservedForRecoveryUnsafe();
            }
        }
    }

    public void MarkSessionPreservedForRecovery()
    {
        lock (_indexLock)
        {
            if (_disposed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_sessionDirectory))
            {
                return;
            }

            _preserveSessionForRecovery = true;

            try
            {
                Directory.CreateDirectory(_sessionDirectory);
                var markerPath = Path.Combine(_sessionDirectory, RecoveryPreserveMarkerFileName);
                File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
                Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER dir='{_sessionDirectory}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private bool IsSessionPreservedForRecoveryUnsafe()
    {
        if (_preserveSessionForRecovery)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            return File.Exists(Path.Combine(_sessionDirectory, RecoveryPreserveMarkerFileName));
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER_CHECK_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            return true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

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
