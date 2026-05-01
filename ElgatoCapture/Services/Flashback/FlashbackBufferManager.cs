using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;

namespace ElgatoCapture.Services.Flashback;

/// <summary>
/// Manages a single MPEG-TS flashback buffer file.
/// Tracks buffered duration via PTS updates from the encoder.
/// </summary>
internal sealed class FlashbackBufferManager : IDisposable
{
    private static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);
    private const int MaxStaleSessionDirectoryScansPerInit = 64;
    private const int MaxStaleSessionDirectoriesPerInit = 16;
    private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;
    private const int MaxStartupCacheSessionDirectoriesPerInit = 32;
    private const long StartupCacheBudgetMultiplier = 2;
    private const int MaxStaleRootSegmentFileScansPerInit = 512;
    private const int MaxStaleRootSegmentFilesPerInit = 128;
    private const string RecoveryPreserveMarkerFileName = ".flashback-recovery-preserve";
    private readonly object _indexLock = new();
    private readonly FlashbackBufferOptions _options;
    private string? _sessionId;
    private string? _sessionDirectory;
    private string? _segmentExtension;
    private string? _activeSegmentPath;
    private long _latestPtsTicks;
    private long _validStartPtsTicks;
    private long _totalDiskBytes;
    private long _totalBytesWritten;
    private long _startupCacheBudgetBytes;
    private long _startupCacheBytes;
    private long _startupCacheFreedBytes;
    private int _startupCacheSessionCount;
    private int _startupCacheDeletedSessionCount;
    private bool _disposed;
    private int _evictionPauseCount;
    private TimeSpan _recordingStartPts;
    private TimeSpan _recordingEndPts;
    private readonly List<CompletedSegment> _completedSegments = new();
    private int _nextSegmentIndex;
    private long _completedSegmentBytes; // running total — avoids iterating the list
    private long _previousActiveSegmentBytes; // for monotonic written counter

    private record CompletedSegment(string Path, int SequenceNumber, TimeSpan StartPts, TimeSpan EndPts, long SizeBytes);
    private record StartupCacheCandidate(string Path, DateTime LastActivityUtc, long SizeBytes);
    private record StartupCacheCleanupResult(
        long BudgetBytes,
        long RemainingBytes,
        int SessionCount,
        int DeletedSessionCount,
        long FreedBytes);

    public FlashbackBufferManager(FlashbackBufferOptions? options = null)
    {
        _options = options ?? new FlashbackBufferOptions();
    }

    public FlashbackBufferOptions Options => _options;

    public string? SessionId
    {
        get
        {
            lock (_indexLock)
            {
                return _sessionId;
            }
        }
    }

    public string TempDirectory => _options.TempDirectory;

    public bool IsInitialized
    {
        get
        {
            lock (_indexLock)
            {
                return !string.IsNullOrWhiteSpace(_sessionId);
            }
        }
    }

    public TimeSpan BufferedDuration
    {
        get
        {
            var latest = Interlocked.Read(ref _latestPtsTicks);
            var start = Interlocked.Read(ref _validStartPtsTicks);
            var duration = latest - start;
            return duration > 0 ? TimeSpan.FromTicks(duration) : TimeSpan.Zero;
        }
    }

    public TimeSpan ValidStartPts
    {
        get { return TimeSpan.FromTicks(Interlocked.Read(ref _validStartPtsTicks)); }
    }

    public long TotalDiskBytes => Volatile.Read(ref _totalDiskBytes);

    /// <summary>Monotonic counter of all bytes written (never decreases on eviction).</summary>
    public long TotalBytesWritten => Volatile.Read(ref _totalBytesWritten);
    public long StartupCacheBudgetBytes => Volatile.Read(ref _startupCacheBudgetBytes);
    public long StartupCacheBytes => Volatile.Read(ref _startupCacheBytes);
    public int StartupCacheSessionCount => Volatile.Read(ref _startupCacheSessionCount);
    public int StartupCacheDeletedSessionCount => Volatile.Read(ref _startupCacheDeletedSessionCount);
    public long StartupCacheFreedBytes => Volatile.Read(ref _startupCacheFreedBytes);
    public bool StartupCacheOverBudget
    {
        get
        {
            var budgetBytes = StartupCacheBudgetBytes;
            return budgetBytes > 0 && StartupCacheBytes > budgetBytes;
        }
    }

    public long TempDriveAvailableFreeBytes => TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);

    public TimeSpan LatestPts
    {
        get { return TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)); }
    }

    public bool EvictionPaused => Volatile.Read(ref _evictionPauseCount) > 0;

    public void MarkSessionPreservedForRecovery()
    {
        lock (_indexLock)
        {
            if (string.IsNullOrWhiteSpace(_sessionDirectory))
            {
                return;
            }

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

    /// <summary>
    /// Resets the latest PTS tracker to zero.  Called during sink cycling so
    /// the new encoder's PTS (starting from 0) can advance <see cref="_latestPtsTicks"/>
    /// naturally without being blocked by the monotonic guard.
    /// </summary>
    public void ResetLatestPts()
    {
        Interlocked.Exchange(ref _latestPtsTicks, 0);
    }

    /// <summary>
    /// Prepares the buffer for a sink-only cycle (new encoder, same buffer).
    /// Nulls the active segment path so the next <see cref="GetFilePath"/> generates
    /// a fresh file instead of reusing/overwriting the previous sink's segment.
    /// </summary>
    public void FinalizeActiveSegmentForCycle()
    {
        lock (_indexLock)
        {
            _activeSegmentPath = null;
            _previousActiveSegmentBytes = 0;
        }
    }

    /// <summary>
    /// Deletes all completed segment files and clears the index.
    /// Called during sink cycling to prevent stale segments with overlapping PTS.
    /// </summary>
    public void PurgeCompletedSegments()
    {
        lock (_indexLock)
        {
            long freedBytes = 0;
            for (int i = _completedSegments.Count - 1; i >= 0; i--)
            {
                if (TryDeleteFile(_completedSegments[i].Path))
                {
                    freedBytes += _completedSegments[i].SizeBytes;
                    _completedSegments.RemoveAt(i);
                }
            }

            // Also delete the active segment file (it has stale data)
            if (_activeSegmentPath != null && TryDeleteFile(_activeSegmentPath))
                _activeSegmentPath = null; // Force new path generation on next GetFilePath()

            if (_completedSegments.Count == 0)
            {
                // Full purge succeeded — reset all counters
                _completedSegmentBytes = 0;
                Interlocked.Exchange(ref _validStartPtsTicks, 0);
                _totalDiskBytes = 0;
                _totalBytesWritten = 0;
                _previousActiveSegmentBytes = 0;
            }
            else
            {
                // Partial purge — adjust byte counters for what we freed
                _completedSegmentBytes -= freedBytes;
                _totalDiskBytes -= freedBytes;
                Logger.Log($"FLASHBACK_PURGE_PARTIAL freed={freedBytes} remaining_segments={_completedSegments.Count}");
            }
        }
    }

    public long MaxDiskBytes => _options.MaxDiskBytes;

    /// <summary>
    /// The frame rate the encoder was configured with — ground truth for playback pacing.
    /// Set once when the encoder starts; avoids relying on TS container metadata which
    /// can report doubled rates (e.g. 240 for 120fps content).
    /// </summary>
    public double EncodeFrameRate { get; set; }

    /// <summary>
    /// True when eviction is paused (recording/export) and total disk usage exceeds the limit.
    /// The UI polls this to show a disk warning InfoBar.
    /// </summary>
    public bool IsDiskWarningActive
    {
        get
        {
            if (Volatile.Read(ref _evictionPauseCount) <= 0) return false;
            var totalBytes = Volatile.Read(ref _totalDiskBytes);
            return totalBytes > _options.MaxDiskBytes;
        }
    }

    public TimeSpan RecordingStartPts
    {
        get { lock (_indexLock) { return _recordingStartPts; } }
    }

    public TimeSpan RecordingEndPts
    {
        get { lock (_indexLock) { return _recordingEndPts; } }
    }

    /// <summary>
    /// Pauses eviction and marks the recording start PTS.
    /// While paused, the .ts file grows without evicting old frames.
    /// </summary>
    public void PauseEviction()
    {
        lock (_indexLock)
        {
            var newCount = Interlocked.Increment(ref _evictionPauseCount);
            if (newCount == 1)
            {
                // First pause — capture the recording start boundary.
                // Nested pauses (e.g. export during recording) must not overwrite this.
                _recordingStartPts = TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks));
            }
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_PAUSED count={newCount} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds}");
        }
    }

    /// <summary>
    /// Resumes eviction and captures the recording end PTS.
    /// Returns the (startPts, endPts) range for export.
    /// </summary>
    public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()
    {
        lock (_indexLock)
        {
            var currentCount = Volatile.Read(ref _evictionPauseCount);
            if (currentCount <= 0)
            {
                if (currentCount < 0)
                {
                    Interlocked.Exchange(ref _evictionPauseCount, 0);
                }

                Logger.Log($"FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED count={currentCount} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds} end_pts_ms={(long)_recordingEndPts.TotalMilliseconds}");
                return (_recordingStartPts, _recordingEndPts);
            }

            var newCount = Interlocked.Decrement(ref _evictionPauseCount);
            // Only capture end PTS on the final resume (outermost pause/resume pair).
            // With nested pauses (e.g. export during recording), an inner resume must
            // not overwrite the end PTS — the outermost resume captures the true range.
            if (newCount == 0)
            {
                _recordingEndPts = TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks));
            }
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_RESUMED count={Volatile.Read(ref _evictionPauseCount)} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds} end_pts_ms={(long)_recordingEndPts.TotalMilliseconds} range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");
            return (_recordingStartPts, _recordingEndPts);
        }
    }

    /// <summary>
    /// For compatibility — single file means 1 "segment" when active, 0 otherwise.
    /// </summary>
    public int SegmentCount
    {
        get
        {
            lock (_indexLock)
            {
                return _completedSegments.Count + (_activeSegmentPath != null ? 1 : 0);
            }
        }
    }

    public string? ActiveFilePath
    {
        get
        {
            lock (_indexLock)
            {
                return _activeSegmentPath;
            }
        }
    }

    /// <summary>Sets the file extension for new segments (e.g. ".ts" or ".mp4").</summary>
    public void SetSegmentExtension(string extension) => _segmentExtension = extension;

    public void Initialize(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        lock (_indexLock)
        {
            ThrowIfDisposed();

            Directory.CreateDirectory(_options.TempDirectory);
            var sessionDirectory = Path.Combine(_options.TempDirectory, sessionId);
            Directory.CreateDirectory(sessionDirectory);

            // Clean up orphaned export temp files from previous sessions
            FlashbackExporter.CleanupOrphanedTempFiles(_options.TempDirectory);
            CleanupStaleRootSegmentFiles(_options.TempDirectory);
            CleanupStaleSessionDirectories(_options.TempDirectory, sessionDirectory);
            var cacheCleanup = CleanupSessionCacheBudget(
                _options.TempDirectory,
                sessionDirectory,
                CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));

            _activeSegmentPath = null;
            _completedSegments.Clear();
            _completedSegmentBytes = 0;
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
            _previousActiveSegmentBytes = 0;
            Interlocked.Exchange(ref _evictionPauseCount, 0);
            _sessionId = sessionId;
            _sessionDirectory = sessionDirectory;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{_options.TempDirectory}' session_dir='{sessionDirectory}'");
        }
    }

    /// <summary>
    /// Gets the active .ts segment path for this session. Generates the first segment on first call.
    /// </summary>
    public string GetFilePath()
    {
        lock (_indexLock)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                throw new InvalidOperationException("Flashback buffer manager has not been initialized.");
            }

            if (_activeSegmentPath == null)
            {
                return GenerateSegmentPath();
            }

            return _activeSegmentPath;
        }
    }

    public string GenerateSegmentPath()
    {
        lock (_indexLock)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(_sessionId))
                throw new InvalidOperationException("Flashback buffer manager has not been initialized.");
            if (string.IsNullOrWhiteSpace(_sessionDirectory))
                throw new InvalidOperationException("Flashback buffer manager session directory has not been initialized.");

            var ext = _segmentExtension ?? ".mp4";
            var path = Path.Combine(_sessionDirectory, $"fb_{_sessionId}_{_nextSegmentIndex:D4}{ext}");
            _nextSegmentIndex++;
            _activeSegmentPath = path;
            return path;
        }
    }

    private static void CleanupStaleSessionDirectories(string tempDirectory, string currentSessionDirectory)
    {
        try
        {
            var currentFullPath = Path.GetFullPath(currentSessionDirectory);
            var nowUtc = DateTime.UtcNow;
            var scannedCount = 0;
            var deletedCount = 0;
            long freedBytes = 0;

            foreach (var directory in Directory.EnumerateDirectories(tempDirectory))
            {
                if (scannedCount >= MaxStaleSessionDirectoryScansPerInit ||
                    deletedCount >= MaxStaleSessionDirectoriesPerInit)
                {
                    break;
                }

                scannedCount++;

                var fullPath = Path.GetFullPath(directory);
                if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var info = new DirectoryInfo(fullPath);
                if (!info.Exists)
                {
                    continue;
                }

                if (File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName)))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_PRESERVE_SKIP dir='{fullPath}'");
                    continue;
                }

                var latestActivityUtc = info.LastWriteTimeUtc;
                long directoryBytes = 0;
                var looksLikeFlashbackSession = false;
                foreach (var file in info.EnumerateFiles("fb_*", SearchOption.TopDirectoryOnly))
                {
                    looksLikeFlashbackSession = true;
                    latestActivityUtc = latestActivityUtc > file.LastWriteTimeUtc
                        ? latestActivityUtc
                        : file.LastWriteTimeUtc;
                    directoryBytes += file.Length;
                }

                if (!looksLikeFlashbackSession && info.EnumerateFileSystemInfos().Any())
                {
                    continue;
                }

                if (nowUtc - latestActivityUtc < StaleSessionMinAge)
                {
                    continue;
                }

                try
                {
                    Directory.Delete(fullPath, recursive: true);
                    deletedCount++;
                    freedBytes += directoryBytes;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_DELETE_WARN dir='{fullPath}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_STALE_SESSION_CLEANUP deleted={deletedCount} freed_bytes={freedBytes}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_STALE_SESSION_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static long CalculateStartupTempCacheBudgetBytes(long sessionMaxDiskBytes)
    {
        if (sessionMaxDiskBytes <= 0)
        {
            return 0;
        }

        return sessionMaxDiskBytes > long.MaxValue / StartupCacheBudgetMultiplier
            ? long.MaxValue
            : sessionMaxDiskBytes * StartupCacheBudgetMultiplier;
    }

    private static StartupCacheCleanupResult CleanupSessionCacheBudget(string tempDirectory, string currentSessionDirectory, long maxCacheBytes)
    {
        if (maxCacheBytes <= 0)
        {
            return new StartupCacheCleanupResult(0, 0, 0, 0, 0);
        }

        try
        {
            var currentFullPath = Path.GetFullPath(currentSessionDirectory);
            var candidates = new List<StartupCacheCandidate>();
            var scannedCount = 0;
            var deletedCount = 0;
            var sessionCount = 0;
            long freedBytes = 0;
            long totalCacheBytes = TryGetFlashbackSessionDirectoryStats(
                currentFullPath,
                out _,
                out var currentBytes,
                out _)
                ? currentBytes
                : 0;
            if (currentBytes > 0)
            {
                sessionCount++;
            }

            foreach (var directory in Directory.EnumerateDirectories(tempDirectory))
            {
                if (scannedCount >= MaxStartupCacheSessionDirectoryScansPerInit)
                {
                    break;
                }

                scannedCount++;

                var fullPath = Path.GetFullPath(directory);
                if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName)))
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP dir='{fullPath}'");
                    continue;
                }

                if (!TryGetFlashbackSessionDirectoryStats(fullPath, out var latestActivityUtc, out var directoryBytes, out var hasFiles))
                {
                    continue;
                }

                if (!hasFiles || directoryBytes <= 0)
                {
                    continue;
                }

                totalCacheBytes += directoryBytes;
                sessionCount++;
                candidates.Add(new StartupCacheCandidate(fullPath, latestActivityUtc, directoryBytes));
            }

            if (totalCacheBytes <= maxCacheBytes)
            {
                return new StartupCacheCleanupResult(maxCacheBytes, totalCacheBytes, sessionCount, 0, 0);
            }

            foreach (var candidate in candidates.OrderBy(candidate => candidate.LastActivityUtc))
            {
                if (deletedCount >= MaxStartupCacheSessionDirectoriesPerInit || totalCacheBytes <= maxCacheBytes)
                {
                    break;
                }

                try
                {
                    Directory.Delete(candidate.Path, recursive: true);
                    deletedCount++;
                    freedBytes += candidate.SizeBytes;
                    totalCacheBytes -= candidate.SizeBytes;
                    sessionCount = Math.Max(0, sessionCount - 1);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_DELETE_WARN dir='{candidate.Path}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_CACHE_BUDGET_CLEANUP deleted={deletedCount} freed_bytes={freedBytes} remaining_bytes={totalCacheBytes} budget_bytes={maxCacheBytes}");
            }

            return new StartupCacheCleanupResult(maxCacheBytes, totalCacheBytes, sessionCount, deletedCount, freedBytes);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CACHE_BUDGET_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
            return new StartupCacheCleanupResult(maxCacheBytes, 0, 0, 0, 0);
        }
    }

    private static long TryGetTempDriveAvailableFreeBytes(string tempDirectory)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(tempDirectory));
            if (string.IsNullOrWhiteSpace(root))
            {
                return -1;
            }

            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_TEMP_DRIVE_FREE_SPACE_WARN dir='{tempDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            return -1;
        }
    }

    private static bool TryGetFlashbackSessionDirectoryStats(
        string fullPath,
        out DateTime latestActivityUtc,
        out long directoryBytes,
        out bool hasFiles)
    {
        latestActivityUtc = DateTime.MinValue;
        directoryBytes = 0;
        hasFiles = false;

        try
        {
            var info = new DirectoryInfo(fullPath);
            if (!info.Exists)
            {
                return false;
            }

            latestActivityUtc = info.LastWriteTimeUtc;
            var looksLikeFlashbackSession = false;
            foreach (var file in info.EnumerateFiles("fb_*", SearchOption.TopDirectoryOnly))
            {
                looksLikeFlashbackSession = true;
                hasFiles = true;
                latestActivityUtc = latestActivityUtc > file.LastWriteTimeUtc
                    ? latestActivityUtc
                    : file.LastWriteTimeUtc;
                directoryBytes += file.Length;
            }

            if (!looksLikeFlashbackSession && info.EnumerateFileSystemInfos().Any())
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SESSION_STATS_WARN dir='{fullPath}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }

    private static void CleanupStaleRootSegmentFiles(string tempDirectory)
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var scannedCount = 0;
            var deletedCount = 0;
            long freedBytes = 0;

            foreach (var filePath in Directory.EnumerateFiles(tempDirectory, "fb_*", SearchOption.TopDirectoryOnly))
            {
                if (scannedCount >= MaxStaleRootSegmentFileScansPerInit ||
                    deletedCount >= MaxStaleRootSegmentFilesPerInit)
                {
                    break;
                }

                scannedCount++;

                var info = new FileInfo(filePath);
                if (!info.Exists || nowUtc - info.LastWriteTimeUtc < StaleSessionMinAge)
                {
                    continue;
                }

                try
                {
                    var length = info.Length;
                    info.Delete();
                    deletedCount++;
                    freedBytes += length;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_DELETE_WARN file='{filePath}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_CLEANUP deleted={deletedCount} freed_bytes={freedBytes}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    public void OnSegmentCompleted(string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        lock (_indexLock)
        {
            var sequenceNumber = _completedSegments.Count;
            _completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, sizeBytes));
            _completedSegmentBytes += sizeBytes;
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_COMPLETE seq={sequenceNumber} path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} size_bytes={sizeBytes}");

            if (!(Volatile.Read(ref _evictionPauseCount) > 0))
            {
                EvictOldestSegments();
            }
        }
    }

    /// <summary>
    /// Returns the segment file path containing the given absolute PTS, or the active segment
    /// as fallback. Used by the playback controller to open the correct file for seeking.
    /// </summary>
    public string? GetSegmentFileForPosition(TimeSpan absolutePts)
    {
        lock (_indexLock)
        {
            // Search completed segments (they have known PTS ranges)
            foreach (var seg in _completedSegments)
            {
                if (absolutePts >= seg.StartPts && absolutePts < seg.EndPts)
                    return seg.Path;
            }
            // Fall back to active segment (contains the most recent, still-growing data)
            return _activeSegmentPath;
        }
    }

    /// <summary>
    /// Returns a validated segment file path for the given position. Unlike GetSegmentFileForPosition,
    /// this checks that the file still exists (hasn't been evicted between lookup and open).
    /// If the target segment was evicted, falls back to the oldest available segment.
    /// </summary>
    public string? GetValidSegmentFileForPosition(TimeSpan absolutePts)
    {
        lock (_indexLock)
        {
            var path = GetSegmentFileForPositionCore(absolutePts);
            if (path != null && File.Exists(path))
                return path;
            // If the target segment was evicted, return the oldest valid segment
            if (_completedSegments.Count > 0 && File.Exists(_completedSegments[0].Path))
                return _completedSegments[0].Path;
            return _activeSegmentPath;
        }
    }

    /// <summary>
    /// Core lookup without lock — caller must hold _indexLock.
    /// Uses binary search since completed segments are sorted by StartPts.
    /// </summary>
    private string? GetSegmentFileForPositionCore(TimeSpan absolutePts)
    {
        var segments = _completedSegments;
        var count = segments.Count;
        if (count == 0)
            return _activeSegmentPath;

        // Binary search: find the last segment whose StartPts <= absolutePts
        int lo = 0, hi = count - 1, best = -1;
        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            if (segments[mid].StartPts <= absolutePts)
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (best >= 0 && absolutePts < segments[best].EndPts)
            return segments[best].Path;

        return _activeSegmentPath;
    }

    /// <summary>
    /// Returns the path of the segment immediately after the given one, or the active
    /// segment path if currentPath is the last completed segment. If currentPath was
    /// evicted or is unknown, returns the oldest available segment instead of blindly
    /// jumping to the active segment.
    /// </summary>
    public string? GetNextSegmentFile(string currentPath)
    {
        lock (_indexLock)
        {
            for (int i = 0; i < _completedSegments.Count; i++)
            {
                if (_completedSegments[i].Path == currentPath)
                {
                    // Found current — return next if it exists
                    if (i + 1 < _completedSegments.Count)
                        return _completedSegments[i + 1].Path;
                    // Current is the last completed — fall back to active
                    return _activeSegmentPath;
                }
            }
            if (string.Equals(_activeSegmentPath, currentPath, StringComparison.OrdinalIgnoreCase))
                return _activeSegmentPath;
            // currentPath not found (evicted or unknown)
            // Return the oldest available segment, or active if none
            if (_completedSegments.Count > 0)
                return _completedSegments[0].Path;
            return _activeSegmentPath;
        }
    }

    public TimeSpan? GetSegmentStartPts(string path)
    {
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (string.Equals(seg.Path, path, StringComparison.OrdinalIgnoreCase))
                    return seg.StartPts;
            }

            if (string.Equals(_activeSegmentPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return _completedSegments.Count > 0
                    ? _completedSegments[^1].EndPts
                    : _recordingStartPts;
            }

            return null;
        }
    }

    public IReadOnlyList<string> GetValidSegmentPaths(TimeSpan inPoint, TimeSpan outPoint)
    {
        lock (_indexLock)
        {
            var paths = new List<string>();
            foreach (var seg in _completedSegments)
            {
                // Segment overlaps [inPoint, outPoint] if seg.Start < outPoint AND seg.End > inPoint
                if (seg.StartPts < outPoint && seg.EndPts > inPoint)
                {
                    paths.Add(seg.Path);
                }
            }
            // Do NOT include the active segment — it's still being written to and may not
            // have valid headers yet. After ForceRotateForExport, all relevant data is
            // in the completed segments.
            return paths;
        }
    }

    public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()
    {
        lock (_indexLock)
        {
            var result = new List<FlashbackSegmentInfo>(_completedSegments.Count + 1);
            foreach (var seg in _completedSegments)
            {
                result.Add(new FlashbackSegmentInfo
                {
                    Path = seg.Path,
                    SequenceNumber = seg.SequenceNumber,
                    StartPtsMs = (long)seg.StartPts.TotalMilliseconds,
                    EndPtsMs = (long)seg.EndPts.TotalMilliseconds,
                    SizeBytes = seg.SizeBytes,
                    IsActive = false
                });
            }
            if (_activeSegmentPath != null)
            {
                var activeStartPts = _completedSegments.Count > 0
                    ? _completedSegments[^1].EndPts
                    : _recordingStartPts;
                result.Add(new FlashbackSegmentInfo
                {
                    Path = _activeSegmentPath,
                    SequenceNumber = _nextSegmentIndex,
                    StartPtsMs = (long)activeStartPts.TotalMilliseconds,
                    EndPtsMs = (long)TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)).TotalMilliseconds,
                    SizeBytes = _totalDiskBytes - _completedSegmentBytes,
                    IsActive = true
                });
            }
            return result;
        }
    }

    /// <summary>
    /// Called by the encoder on each video frame to update the latest PTS.
    /// </summary>
    public void UpdateLatestPts(TimeSpan pts)
    {
        var ptsTicks = pts.Ticks;
        // Atomic monotonic update
        long current;
        do
        {
            current = Interlocked.Read(ref _latestPtsTicks);
            if (ptsTicks <= current) return;
        } while (Interlocked.CompareExchange(ref _latestPtsTicks, ptsTicks, current) != current);

        // Advance valid start if buffer exceeds max duration (skip while recording).
        if (!(Volatile.Read(ref _evictionPauseCount) > 0))
        {
            var maxTicks = _options.BufferDuration.Ticks;
            var startTicks = Interlocked.Read(ref _validStartPtsTicks);
            var duration = ptsTicks - startTicks;
            if (duration > maxTicks)
            {
                // Double-check under lock to close the TOCTOU window where PauseEviction()
                // could increment _evictionPauseCount between our volatile read and the CAS.
                lock (_indexLock)
                {
                    if (!(Volatile.Read(ref _evictionPauseCount) > 0))
                    {
                        startTicks = Interlocked.Read(ref _validStartPtsTicks);
                        Interlocked.CompareExchange(ref _validStartPtsTicks, ptsTicks - maxTicks, startTicks);

                        // Only run segment eviction if there are segments that might be evictable.
                        // This avoids taking the eviction path on every frame at 120fps.
                        if (_completedSegments.Count > 0 &&
                            _completedSegments[0].EndPts.Ticks <= Interlocked.Read(ref _validStartPtsTicks))
                        {
                            EvictOldestSegments();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the tracked disk bytes. <paramref name="activeSegmentBytes"/> is the current
    /// active segment's byte count (encoder resets this on rotation). Total disk usage is
    /// computed as completed segment bytes + active segment bytes.
    /// </summary>
    public void UpdateDiskBytes(long activeSegmentBytes)
    {
        lock (_indexLock)
        {
            // Track monotonic bytes written: when active segment grows, add the delta.
            // On rotation activeSegmentBytes resets to 0 — the completed segment's bytes
            // were already added to _completedSegmentBytes via CompleteSegment, so we just
            // reset the previous-active tracker.
            if (activeSegmentBytes >= _previousActiveSegmentBytes)
                Interlocked.Add(ref _totalBytesWritten, activeSegmentBytes - _previousActiveSegmentBytes);
            _previousActiveSegmentBytes = activeSegmentBytes;

            _totalDiskBytes = _completedSegmentBytes + activeSegmentBytes;

            if (!(Volatile.Read(ref _evictionPauseCount) > 0) && _totalDiskBytes > _options.MaxDiskBytes)
            {
                var excessBytes = _totalDiskBytes - _options.MaxDiskBytes;
                var latestTicks = Interlocked.Read(ref _latestPtsTicks);
                var startTicks = Interlocked.Read(ref _validStartPtsTicks);
                var totalDuration = latestTicks - startTicks;

                if (totalDuration > 0)
                {
                    var bytesPerTick = (double)_totalDiskBytes / totalDuration;
                    var evictTicks = (long)(excessBytes / bytesPerTick);
                    var newStart = startTicks + evictTicks;
                    if (newStart > latestTicks) newStart = latestTicks;
                    Interlocked.Exchange(ref _validStartPtsTicks, newStart);

                    Logger.Log(
                        $"FLASHBACK_BUFFER_DISK_EVICT excess_bytes={excessBytes} evicted_seconds={TimeSpan.FromTicks(evictTicks).TotalSeconds:F2}");
                }

                EvictOldestSegments();
            }
        }
    }

    public void PurgeAllSegments()
    {
        lock (_indexLock)
        {
            // Delete completed segments
            foreach (var seg in _completedSegments)
            {
                TryDeleteFile(seg.Path);
            }
            _completedSegments.Clear();
            _completedSegmentBytes = 0;

            // Delete active segment
            if (_activeSegmentPath != null)
            {
                TryDeleteFile(_activeSegmentPath);
                _activeSegmentPath = null;
            }
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
            _totalBytesWritten = 0;
            _previousActiveSegmentBytes = 0;
            Interlocked.Exchange(ref _evictionPauseCount, 0);
            Logger.Log("FLASHBACK_BUFFER_PURGE");
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

            _disposed = true;
            Logger.Log($"FLASHBACK_BUFFER_DISPOSE file={_activeSegmentPath != null} segments={_completedSegments.Count}");
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

    private void EvictOldestSegments()
    {
        // Must be called under _indexLock
        var validStart = TimeSpan.FromTicks(Interlocked.Read(ref _validStartPtsTicks));
        var evictedCount = 0;
        long evictedBytes = 0;

        // Remove segments that are entirely before the valid window
        while (_completedSegments.Count > 0)
        {
            var oldest = _completedSegments[0];
            if (oldest.EndPts <= validStart)
            {
                if (TryDeleteFile(oldest.Path))
                {
                    evictedBytes += oldest.SizeBytes;
                    _completedSegmentBytes -= oldest.SizeBytes;
                    _completedSegments.RemoveAt(0);
                    evictedCount++;
                }
                else
                {
                    break; // Can't delete — file is locked, stop evicting
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
            if (TryDeleteFile(oldest.Path))
            {
                evictedBytes += oldest.SizeBytes;
                _completedSegmentBytes -= oldest.SizeBytes;
                _completedSegments.RemoveAt(0);
                evictedCount++;
            }
            else
            {
                break; // Can't delete — file is locked, stop evicting
            }
        }

        if (evictedCount > 0)
        {
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_EVICT count={evictedCount} evicted_bytes={evictedBytes} remaining_segments={_completedSegments.Count}");
        }
    }

    private bool TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath); // No-op if file doesn't exist (.NET 8+)
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_DELETE_WARN path='{filePath}' msg='{ex.Message}'");
            return false;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
