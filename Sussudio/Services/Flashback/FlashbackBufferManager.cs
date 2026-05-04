using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

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
    private volatile bool _disposed;
    private bool _preserveSessionForRecovery;
    private int _evictionPauseCount;
    private TimeSpan _recordingStartPts;
    private TimeSpan _recordingEndPts;
    private readonly List<CompletedSegment> _completedSegments = new();
    private int _nextSegmentIndex;
    private int _completedSegmentSequence;
    private long _completedSegmentBytes; // running total — avoids iterating the list
    private long _previousActiveSegmentBytes; // for monotonic written counter

    private record CompletedSegment(string Path, int SequenceNumber, TimeSpan StartPts, TimeSpan EndPts, long SizeBytes)
    {
        public bool AllowSamePathExtension { get; init; }
    }
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
            var duration = NonNegativeDeltaTicks(latest, start);
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

    /// <summary>
    /// Resets the latest PTS tracker to zero.  Called during sink cycling so
    /// the new encoder's PTS (starting from 0) can advance <see cref="_latestPtsTicks"/>
    /// naturally without being blocked by the monotonic guard.
    /// </summary>
    public void ResetLatestPts()
    {
        if (_disposed)
        {
            return;
        }

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
            if (_disposed)
            {
                return;
            }

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
                    _activeSegmentPath = null; // Force new path generation on next GetFilePath()
                    _previousActiveSegmentBytes = 0;
                }
                else
                {
                    Logger.Log($"FLASHBACK_PURGE_ACTIVE_RETAINED path='{Path.GetFileName(_activeSegmentPath)}'");
                }
            }

            if (_completedSegments.Count == 0 && _activeSegmentPath == null)
            {
                // Full purge succeeded — reset all counters
                _completedSegmentBytes = 0;
                _completedSegmentSequence = 0;
                Interlocked.Exchange(ref _validStartPtsTicks, 0);
                _totalDiskBytes = 0;
                _totalBytesWritten = 0;
                _previousActiveSegmentBytes = 0;
            }
            else
            {
                // Partial purge — adjust byte counters for what we freed
                _completedSegmentBytes = GetCompletedSegmentBytesSaturated();
                var retainedActiveBytes = _activeSegmentPath != null ? activeSegmentBytes : 0;
                _totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);
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
            if (_disposed)
            {
                return;
            }

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
            if (_disposed)
            {
                return (_recordingStartPts, ClampEndPtsToStart(_recordingStartPts, _recordingEndPts));
            }

            var currentCount = Volatile.Read(ref _evictionPauseCount);
            if (currentCount <= 0)
            {
                if (currentCount < 0)
                {
                    Interlocked.Exchange(ref _evictionPauseCount, 0);
                }

                var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);
                Logger.Log($"FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED count={currentCount} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds} end_pts_ms={(long)unbalancedEndPts.TotalMilliseconds}");
                return (_recordingStartPts, unbalancedEndPts);
            }

            var newCount = Interlocked.Decrement(ref _evictionPauseCount);
            // Only capture end PTS on the final resume (outermost pause/resume pair).
            // With nested pauses (e.g. export during recording), an inner resume must
            // not overwrite the end PTS — the outermost resume captures the true range.
            if (newCount == 0)
            {
                _recordingEndPts = ClampEndPtsToStart(
                    _recordingStartPts,
                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));
            }
            var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_RESUMED count={Volatile.Read(ref _evictionPauseCount)} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds} end_pts_ms={(long)_recordingEndPts.TotalMilliseconds} range_s={rangeSeconds:F1}");
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
                return _completedSegments.Count(seg => File.Exists(seg.Path)) +
                    (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? 1 : 0);
            }
        }
    }

    public string? ActiveFilePath
    {
        get
        {
            lock (_indexLock)
            {
                return _activeSegmentPath != null && File.Exists(_activeSegmentPath)
                    ? _activeSegmentPath
                    : null;
            }
        }
    }

    /// <summary>Sets the file extension for new segments (e.g. ".ts" or ".mp4").</summary>
    public void SetSegmentExtension(string extension)
    {
        var normalizedExtension = NormalizeSegmentExtension(extension);
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
            var sessionDirectory = BuildSessionDirectory(tempDirectory, sessionId);
            Directory.CreateDirectory(sessionDirectory);

            // Clean up orphaned export temp files from previous sessions
            FlashbackExporter.CleanupOrphanedTempFiles(tempDirectory);
            CleanupStaleRootSegmentFiles(tempDirectory);
            CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);
            var cacheCleanup = CleanupSessionCacheBudget(
                tempDirectory,
                sessionDirectory,
                CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));

            _activeSegmentPath = null;
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
            Interlocked.Exchange(ref _evictionPauseCount, 0);
            _preserveSessionForRecovery = false;
            _sessionId = sessionId;
            _sessionDirectory = sessionDirectory;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{tempDirectory}' session_dir='{sessionDirectory}'");
        }
    }

    /// <summary>
    /// Gets the active .ts segment path for this session. Generates the first segment on first call.
    /// </summary>
    public string GetFilePath()
        => GetFilePath(out _);

    public string GetFilePath(out bool generated)
    {
        lock (_indexLock)
        {
            ThrowIfDisposed();
            generated = false;

            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                throw new InvalidOperationException("Flashback buffer manager has not been initialized.");
            }

            if (_activeSegmentPath == null)
            {
                generated = true;
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

    public void AbandonGeneratedSegmentPath(string generatedPath, string? restoreActivePath)
    {
        if (string.IsNullOrWhiteSpace(generatedPath))
        {
            return;
        }

        lock (_indexLock)
        {
            if (_disposed)
            {
                return;
            }

            if (IsSameSegmentPath(_activeSegmentPath, generatedPath))
            {
                _activeSegmentPath = restoreActivePath;
                if (_nextSegmentIndex > 0)
                {
                    _nextSegmentIndex--;
                }
            }

            if (!IsSameSegmentPath(generatedPath, restoreActivePath))
            {
                TryDeleteFile(generatedPath);
            }
        }
    }

    private static void CleanupStaleSessionDirectories(string tempDirectory, string currentSessionDirectory)
    {
        try
        {
            var tempRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
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
                if (!IsPathUnderDirectory(fullPath, tempRoot))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=outside_temp dir='{fullPath}'");
                    continue;
                }

                if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var info = new DirectoryInfo(fullPath);
                if (!info.Exists)
                {
                    continue;
                }

                if (IsReparsePoint(info))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=reparse_point dir='{fullPath}'");
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
                    directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);
                }

                if (!looksLikeFlashbackSession)
                {
                    if (info.EnumerateFileSystemInfos().Any())
                    {
                        continue;
                    }

                    if (!IsPlausibleFlashbackSessionDirectoryName(info.Name))
                    {
                        Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir dir='{fullPath}'");
                        continue;
                    }
                }

                if (nowUtc - latestActivityUtc < StaleSessionMinAge)
                {
                    continue;
                }

                try
                {
                    Directory.Delete(fullPath, recursive: true);
                    deletedCount++;
                    freedBytes = AddNonNegativeSaturated(freedBytes, directoryBytes);
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
            var tempRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
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
                if (!IsPathUnderDirectory(fullPath, tempRoot))
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp dir='{fullPath}'");
                    continue;
                }

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

                totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);
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
                    freedBytes = AddNonNegativeSaturated(freedBytes, candidate.SizeBytes);
                    totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);
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

            if (IsReparsePoint(info))
            {
                Logger.Log($"FLASHBACK_SESSION_STATS_SKIP reason=reparse_point dir='{fullPath}'");
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
                directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);
            }

            if (!looksLikeFlashbackSession)
            {
                if (info.EnumerateFileSystemInfos().Any())
                {
                    return false;
                }

                return IsPlausibleFlashbackSessionDirectoryName(info.Name);
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
                    freedBytes = AddNonNegativeSaturated(freedBytes, length);
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

    private static string BuildSessionDirectory(string tempDirectory, string sessionId)
    {
        if (Path.IsPathRooted(sessionId) ||
            sessionId.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            sessionId.IndexOf(Path.AltDirectorySeparatorChar) >= 0 ||
            sessionId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Session id must be a simple file-name component.", nameof(sessionId));
        }

        var tempRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
        var sessionDirectory = Path.GetFullPath(Path.Combine(tempRoot, sessionId));
        if (!IsPathUnderDirectory(sessionDirectory, tempRoot))
        {
            throw new ArgumentException("Session id must resolve inside the flashback temp directory.", nameof(sessionId));
        }

        return sessionDirectory;
    }

    private static string NormalizeSegmentExtension(string extension)
    {
        if (string.Equals(extension, ".ts", StringComparison.OrdinalIgnoreCase))
        {
            return ".ts";
        }

        if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return ".mp4";
        }

        throw new ArgumentException("Flashback segment extension must be .ts or .mp4.", nameof(extension));
    }

    private static string EnsureTrailingDirectorySeparator(string path)
        => Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    private static bool IsPlausibleFlashbackSessionDirectoryName(string name)
    {
        if (name.Length == 32)
        {
            return IsLowerHexString(name);
        }

        var underscore = name.IndexOf('_');
        return underscore > 0 &&
               underscore < name.Length - 1 &&
               IsLowerHexString(name.AsSpan(0, underscore)) &&
               name.AsSpan(underscore + 1).Length == 32 &&
               IsLowerHexString(name.AsSpan(underscore + 1));
    }

    private static bool IsLowerHexString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!IsLowerHexDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLowerHexDigit(char value)
        => value is >= '0' and <= '9' or >= 'a' and <= 'f';

    private static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)
        => fullPath.StartsWith(fullDirectoryRoot, StringComparison.OrdinalIgnoreCase);

    private static bool IsReparsePoint(FileSystemInfo info)
        => (info.Attributes & FileAttributes.ReparsePoint) != 0;

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

    /// <summary>
    /// Returns an existing segment file path containing the given absolute PTS, or the active segment
    /// as fallback when it exists.
    /// </summary>
    public string? GetSegmentFileForPosition(TimeSpan absolutePts)
        => GetValidSegmentFileForPosition(absolutePts);

    /// <summary>
    /// Returns a validated segment file path for the given position.
    /// This checks that the file still exists (hasn't been evicted between lookup and open).
    /// If the target segment was evicted, falls back to the oldest available segment.
    /// </summary>
    public string? GetValidSegmentFileForPosition(TimeSpan absolutePts)
    {
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (absolutePts >= seg.StartPts && absolutePts < seg.EndPts)
                {
                    return File.Exists(seg.Path)
                        ? seg.Path
                        : GetOldestExistingSegmentPath();
                }
            }

            if (_completedSegments.Count > 0 && absolutePts < _completedSegments[0].StartPts)
            {
                return GetOldestExistingSegmentPath()
                    ?? (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null);
            }

            if (_activeSegmentPath != null && File.Exists(_activeSegmentPath))
            {
                return _activeSegmentPath;
            }

            return GetOldestExistingSegmentPath();
        }
    }

    private string? GetOldestExistingSegmentPath()
    {
        foreach (var seg in _completedSegments)
        {
            if (File.Exists(seg.Path))
                return seg.Path;
        }

        return null;
    }

    private bool IsPathInSessionDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            var sessionRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(_sessionDirectory));
            var fullPath = Path.GetFullPath(path);
            return IsPathUnderDirectory(fullPath, sessionRoot);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_PATH_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
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
                if (IsSameSegmentPath(_completedSegments[i].Path, currentPath))
                {
                    // Found current — return next if it exists
                    for (var nextIndex = i + 1; nextIndex < _completedSegments.Count; nextIndex++)
                    {
                        var nextPath = _completedSegments[nextIndex].Path;
                        if (File.Exists(nextPath))
                            return nextPath;
                    }
                    // Current is the last completed — fall back to active
                    return _activeSegmentPath != null && File.Exists(_activeSegmentPath)
                        ? _activeSegmentPath
                        : null;
                }
            }
            if (IsSameSegmentPath(_activeSegmentPath, currentPath))
                return _activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null;
            // currentPath not found (evicted or unknown)
            // Return the oldest available segment, or active if none
            return GetOldestExistingSegmentPath()
                ?? (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null);
        }
    }

    public TimeSpan? GetSegmentStartPts(string path)
    {
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (IsSameSegmentPath(seg.Path, path) && File.Exists(seg.Path))
                    return seg.StartPts;
            }

            if (IsSameSegmentPath(_activeSegmentPath, path) &&
                _activeSegmentPath != null &&
                File.Exists(_activeSegmentPath))
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
            if (outPoint <= inPoint)
            {
                return Array.Empty<string>();
            }

            var paths = new List<string>();
            foreach (var seg in _completedSegments)
            {
                // Segment overlaps [inPoint, outPoint] if seg.Start < outPoint AND seg.End > inPoint
                if (seg.StartPts < outPoint && seg.EndPts > inPoint && File.Exists(seg.Path))
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
                if (!File.Exists(seg.Path))
                {
                    continue;
                }

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
            if (_activeSegmentPath != null && File.Exists(_activeSegmentPath))
            {
                var activeStartPts = _completedSegments.Count > 0
                    ? _completedSegments[^1].EndPts
                    : _recordingStartPts;
                var activeEndPts = TimeSpan.FromTicks(Math.Max(activeStartPts.Ticks, Interlocked.Read(ref _latestPtsTicks)));
                var activeSizeBytes = Math.Max(0, _totalDiskBytes - _completedSegmentBytes);
                result.Add(new FlashbackSegmentInfo
                {
                    Path = _activeSegmentPath,
                    SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),
                    StartPtsMs = (long)activeStartPts.TotalMilliseconds,
                    EndPtsMs = (long)activeEndPts.TotalMilliseconds,
                    SizeBytes = activeSizeBytes,
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
        if (_disposed)
        {
            return;
        }

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
            var maxTicks = Math.Max(0, _options.BufferDuration.Ticks);
            var startTicks = Interlocked.Read(ref _validStartPtsTicks);
            var duration = NonNegativeDeltaTicks(ptsTicks, startTicks);
            if (duration > maxTicks)
            {
                // Double-check under lock to close the TOCTOU window where PauseEviction()
                // could increment _evictionPauseCount between our volatile read and the CAS.
                lock (_indexLock)
                {
                    if (!(Volatile.Read(ref _evictionPauseCount) > 0))
                    {
                        startTicks = Interlocked.Read(ref _validStartPtsTicks);
                        var newStartTicks = Math.Max(0, ptsTicks - maxTicks);
                        Interlocked.CompareExchange(ref _validStartPtsTicks, newStartTicks, startTicks);

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
        if (_disposed)
        {
            return;
        }

        lock (_indexLock)
        {
            if (_disposed)
            {
                return;
            }

            var safeActiveSegmentBytes = Math.Max(0, activeSegmentBytes);
            var accountedActiveSegmentBytes = safeActiveSegmentBytes;
            if (_activeSegmentPath != null &&
                _completedSegments.Count > 0 &&
                IsSameSegmentPath(_completedSegments[^1].Path, _activeSegmentPath))
            {
                accountedActiveSegmentBytes = SubtractNonNegative(safeActiveSegmentBytes, _completedSegments[^1].SizeBytes);
            }

            // Track monotonic bytes written: when active segment grows, add the delta.
            // On rotation activeSegmentBytes resets to 0 — the completed segment's bytes
            // were already added to _completedSegmentBytes via CompleteSegment, so we just
            // reset the previous-active tracker.
            if (safeActiveSegmentBytes >= _previousActiveSegmentBytes)
                Interlocked.Add(ref _totalBytesWritten, safeActiveSegmentBytes - _previousActiveSegmentBytes);
            _previousActiveSegmentBytes = safeActiveSegmentBytes;

            _totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, accountedActiveSegmentBytes);

            if (!(Volatile.Read(ref _evictionPauseCount) > 0) && _totalDiskBytes > _options.MaxDiskBytes)
            {
                var excessBytes = _totalDiskBytes - _options.MaxDiskBytes;
                var latestTicks = Interlocked.Read(ref _latestPtsTicks);
                var startTicks = Interlocked.Read(ref _validStartPtsTicks);
                var totalDuration = NonNegativeDeltaTicks(latestTicks, startTicks);

                if (totalDuration > 0)
                {
                    var bytesPerTick = (double)_totalDiskBytes / totalDuration;
                    var evictTicks = ToNonNegativeLongSaturated(excessBytes / bytesPerTick);
                    var newStart = AddNonNegativeSaturated(Math.Max(0, startTicks), evictTicks);
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
    /// Does not gate on <see cref="_disposed"/> — callers are responsible for that check.
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

    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    private static long SubtractNonNegative(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left <= right ? 0 : left - right;
    }

    private long GetCompletedSegmentBytesSaturated()
    {
        // Must be called under _indexLock.
        long total = 0;
        foreach (var segment in _completedSegments)
        {
            total = AddNonNegativeSaturated(total, segment.SizeBytes);
        }

        return total;
    }

    private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)
    {
        if (latestTicks <= startTicks)
            return 0;

        if (startTicks < 0 && latestTicks > long.MaxValue + startTicks)
            return long.MaxValue;

        return latestTicks - startTicks;
    }

    private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)
        => endPts < startPts ? startPts : endPts;

    private static bool IsSameSegmentPath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static long ToNonNegativeLongSaturated(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
            return 0;

        return value >= long.MaxValue ? long.MaxValue : (long)value;
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
                    evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                    _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
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
                evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
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
            _totalDiskBytes = SubtractNonNegative(_totalDiskBytes, evictedBytes);
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_EVICT count={evictedCount} evicted_bytes={evictedBytes} remaining_segments={_completedSegments.Count}");
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
