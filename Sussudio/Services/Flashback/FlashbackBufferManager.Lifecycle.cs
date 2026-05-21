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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
