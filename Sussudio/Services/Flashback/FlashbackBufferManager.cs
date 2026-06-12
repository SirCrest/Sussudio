using System;
using System.Collections.Concurrent;
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
/// Owns disk retention and recovery markers; it does not control live capture.
/// </summary>
internal sealed class FlashbackBufferManager : IDisposable
{
    private const string RecoveryPreserveMarkerFileName = ".flashback-recovery-preserve";
    private const string RetiredSessionMarkerFileName = ".flashback-retired-session";
    private const int EvictedSegmentDeleteMaxAttempts = 3;
    private const int EvictedSegmentDeleteRetryDelayMs = 250;
    private readonly object _indexLock = new();
    private readonly FlashbackBufferOptions _options;
    private string? _sessionId;
    private string? _sessionDirectory;
    private string? _segmentExtension;
    private string? _activeSegmentPath;
    private long _activeSegmentStartPtsTicks = -1;
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
    private bool _retireSessionForStartupCleanup;
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

    private readonly record struct PendingEvictedSegmentDelete(
        string FullPath,
        string SessionRoot,
        long SizeBytes,
        string Reason);

    private static List<PendingEvictedSegmentDelete> EnsurePendingEvictionDeleteList(
        ref List<PendingEvictedSegmentDelete>? pendingDeletes)
        => pendingDeletes ??= new List<PendingEvictedSegmentDelete>();

    // Evicted segments whose async delete kept failing (typically the playback
    // decoder still holds the file open). Keyed by full path; retried on later
    // eviction passes and swept once more at dispose. Bounded by segment count.
    private readonly ConcurrentDictionary<string, PendingEvictedSegmentDelete> _parkedEvictionDeletes =
        new(StringComparer.OrdinalIgnoreCase);

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

    public long TempDriveAvailableFreeBytes => FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);

    public TimeSpan LatestPts
    {
        get { return TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)); }
    }

    public bool EvictionPaused => Volatile.Read(ref _evictionPauseCount) > 0;

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
        {
            return 0;
        }

        if (startTicks < 0 && latestTicks > long.MaxValue + startTicks)
        {
            return long.MaxValue;
        }

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
        {
            return 0;
        }

        return value >= long.MaxValue ? long.MaxValue : (long)value;
    }

    /// <summary>
    /// Resets the latest PTS tracker to zero. Called during sink cycling so
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
    /// Nulls the active segment path so the next <see cref="AcquireSegmentPath"/> generates
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
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, -1);
            _previousActiveSegmentBytes = 0;
        }
    }

    /// <summary>
    /// The frame rate the encoder was configured with - ground truth for playback pacing.
    /// Set once when the encoder starts; avoids relying on TS container metadata which
    /// can report doubled rates (e.g. 240 for 120fps content).
    /// </summary>
    public double EncodeFrameRate { get; set; }

    /// <summary>
    /// Called by the encoder on each video frame to update the latest PTS.
    /// </summary>
    public void UpdateLatestPts(TimeSpan pts)
    {
        if (_disposed)
        {
            return;
        }

        List<PendingEvictedSegmentDelete>? pendingDeletes = null;
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
                            EvictOldestSegmentsLocked(EnsurePendingEvictionDeleteList(ref pendingDeletes));
                        }
                    }
                }
            }
        }

        QueueEvictedSegmentDeletes(pendingDeletes);
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

        List<PendingEvictedSegmentDelete>? pendingDeletes = null;
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
            // On rotation activeSegmentBytes resets to 0; the completed segment's bytes
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

                EvictOldestSegmentsLocked(EnsurePendingEvictionDeleteList(ref pendingDeletes));
            }
        }

        QueueEvictedSegmentDeletes(pendingDeletes);
    }

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

            // Clean up orphaned export temp files from previous sessions.
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
            _retireSessionForStartupCleanup = false;
            _sessionId = sessionId;
            _sessionDirectory = sessionDirectory;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{tempDirectory}' session_dir='{sessionDirectory}'");
        }
    }

    public void Dispose()
    {
        // Final chance for parked eviction deletes before the session directory
        // teardown below; must stay outside _indexLock.
        SweepParkedEvictionDeletesBestEffort("dispose");

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

            if (IsSessionRetiredForStartupCleanupUnsafe())
            {
                _disposed = true;
                Logger.Log($"FLASHBACK_BUFFER_DISPOSE_RETIRE_SESSION dir='{_sessionDirectory}' segments={_completedSegments.Count} active_file={_activeSegmentPath != null}");
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

    public void MarkSessionRetiredForStartupCleanup(string reason)
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

            if (_completedSegments.Count == 0 && string.IsNullOrWhiteSpace(_activeSegmentPath))
            {
                Logger.Log($"FLASHBACK_RETIRE_MARKER_SKIP reason='{reason}' cause=no_segments dir='{_sessionDirectory}'");
                return;
            }

            _retireSessionForStartupCleanup = true;

            try
            {
                Directory.CreateDirectory(_sessionDirectory);
                var markerPath = Path.Combine(_sessionDirectory, RetiredSessionMarkerFileName);
                File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
                Logger.Log($"FLASHBACK_RETIRE_MARKER reason='{reason}' dir='{_sessionDirectory}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_RETIRE_MARKER_WARN reason='{reason}' dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
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

    private bool IsSessionRetiredForStartupCleanupUnsafe()
    {
        if (_retireSessionForStartupCleanup)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            return File.Exists(Path.Combine(_sessionDirectory, RetiredSessionMarkerFileName));
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_RETIRE_MARKER_CHECK_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
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

            // Also delete the active segment file (it has stale data).
            if (_activeSegmentPath != null)
            {
                if (TryDeleteFile(_activeSegmentPath))
                {
                    freedBytes = AddNonNegativeSaturated(freedBytes, activeSegmentBytes);
                    _activeSegmentPath = null;
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

            PurgeAllSegmentsCore();
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

        // Delete completed segments.
        for (int i = _completedSegments.Count - 1; i >= 0; i--)
        {
            if (TryDeleteFile(_completedSegments[i].Path))
            {
                freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);
                _completedSegments.RemoveAt(i);
            }
        }

        // Delete active segment.
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

    /// <summary>
    /// Returns the active .ts segment path, generating a new one on first call
    /// after <see cref="StartCaptureAsync"/> or after segment rotation. The
    /// "Acquire" prefix flags the side effect (segment-index increment and
    /// active-path assignment); callers that just want to peek at the current
    /// path without creating one should add a peek API rather than reuse this.
    /// </summary>
    public string AcquireSegmentPath()
        => AcquireSegmentPath(out _);

    public string AcquireSegmentPath(out bool generated)
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
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, GetDefaultActiveSegmentStartPts().Ticks);
            return path;
        }
    }

    public void MarkActiveSegmentStart(string path, TimeSpan startPts)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (_indexLock)
        {
            if (_disposed ||
                _activeSegmentPath == null ||
                !IsSameSegmentPath(_activeSegmentPath, path))
            {
                return;
            }

            var safeStartPts = startPts < TimeSpan.Zero ? TimeSpan.Zero : startPts;
            Interlocked.Exchange(ref _activeSegmentStartPtsTicks, safeStartPts.Ticks);
            Logger.Log(
                $"FLASHBACK_BUFFER_ACTIVE_SEGMENT_START path='{Path.GetFileName(path)}' " +
                $"start_ms={(long)safeStartPts.TotalMilliseconds}");
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
                Interlocked.Exchange(ref _activeSegmentStartPtsTicks, restoreActivePath == null ? -1 : GetDefaultActiveSegmentStartPts().Ticks);
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
        List<PendingEvictedSegmentDelete>? pendingDeletes = null;
        try
        {
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
                    if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment, ref pendingDeletes))
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
                    EvictOldestSegmentsLocked(EnsurePendingEvictionDeleteList(ref pendingDeletes));
                }
            }
        }
        finally
        {
            QueueEvictedSegmentDeletes(pendingDeletes);
        }
    }

    private bool TryExtendCompletedSegment(
        int existingIndex,
        string path,
        TimeSpan startPts,
        TimeSpan endPts,
        long sizeBytes,
        bool pathIsActiveSegment,
        ref List<PendingEvictedSegmentDelete>? pendingDeletes)
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
            EvictOldestSegmentsLocked(EnsurePendingEvictionDeleteList(ref pendingDeletes));
        }

        return true;
    }

    /// <summary>
    /// For compatibility: single file means 1 "segment" when active, 0 otherwise.
    /// </summary>
    public int SegmentCount
    {
        get
        {
            List<string> completedPaths;
            string? activePath;
            lock (_indexLock)
            {
                completedPaths = _completedSegments.Select(seg => seg.Path).ToList();
                activePath = _activeSegmentPath;
            }

            return completedPaths.Count(File.Exists) +
                (IsExistingPath(activePath) ? 1 : 0);
        }
    }

    public string? ActiveFilePath
    {
        get
        {
            string? activePath;
            lock (_indexLock)
            {
                activePath = _activeSegmentPath;
            }

            return IsExistingPath(activePath)
                ? activePath
                : null;
        }
    }

    private static bool IsExistingPath([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? path)
        => path != null && File.Exists(path);

    private TimeSpan GetActiveSegmentStartPts()
    {
        var activeStartTicks = Interlocked.Read(ref _activeSegmentStartPtsTicks);
        return activeStartTicks >= 0
            ? TimeSpan.FromTicks(activeStartTicks)
            : GetDefaultActiveSegmentStartPts();
    }

    private TimeSpan GetDefaultActiveSegmentStartPts()
        => _completedSegments.Count > 0
            ? _completedSegments[^1].EndPts
            : _recordingStartPts;

    public IReadOnlyList<FlashbackSegmentInfo> GetSegmentInfoList()
    {
        List<FlashbackSegmentInfo> result;
        lock (_indexLock)
        {
            result = new List<FlashbackSegmentInfo>(_completedSegments.Count + 1);
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

            if (_activeSegmentPath is { } activePath)
            {
                var activeStartPts = GetActiveSegmentStartPts();
                var activeEndPts = TimeSpan.FromTicks(Math.Max(activeStartPts.Ticks, Interlocked.Read(ref _latestPtsTicks)));
                var activeSizeBytes = Math.Max(0, _totalDiskBytes - _completedSegmentBytes);
                result.Add(new FlashbackSegmentInfo
                {
                    Path = activePath,
                    SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),
                    StartPtsMs = (long)activeStartPts.TotalMilliseconds,
                    EndPtsMs = (long)activeEndPts.TotalMilliseconds,
                    SizeBytes = activeSizeBytes,
                    IsActive = true
                });
            }
        }

        return result
            .Where(segment => File.Exists(segment.Path))
            .ToList();
    }

    private bool IsPathInSessionDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            var sessionRoot = FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator(Path.GetFullPath(_sessionDirectory));
            var fullPath = Path.GetFullPath(path);
            return FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_PATH_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
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
        string? targetPath = null;
        var beforeFirstCompletedSegment = false;
        List<string> completedPaths;
        string? activePath;
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (absolutePts >= seg.StartPts && absolutePts < seg.EndPts)
                {
                    targetPath = seg.Path;
                    break;
                }
            }

            beforeFirstCompletedSegment = _completedSegments.Count > 0 && absolutePts < _completedSegments[0].StartPts;
            completedPaths = _completedSegments.Select(seg => seg.Path).ToList();
            activePath = _activeSegmentPath;
        }

        if (IsExistingPath(targetPath))
        {
            return targetPath;
        }

        if (beforeFirstCompletedSegment)
        {
            return GetOldestExistingSegmentPath(completedPaths)
                ?? (IsExistingPath(activePath) ? activePath : null);
        }

        if (IsExistingPath(activePath))
        {
            return activePath;
        }

        return GetOldestExistingSegmentPath(completedPaths);
    }

    private static string? GetOldestExistingSegmentPath(IEnumerable<string> completedPaths)
    {
        foreach (var path in completedPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the path of the segment immediately after the given one, or the active
    /// segment path if currentPath is the last completed segment. If currentPath was
    /// evicted or is unknown, returns the oldest available segment instead of blindly
    /// jumping to the active segment.
    /// </summary>
    public string? GetNextSegmentFile(string currentPath)
    {
        List<string> completedPaths;
        string? activePath;
        lock (_indexLock)
        {
            completedPaths = _completedSegments.Select(seg => seg.Path).ToList();
            activePath = _activeSegmentPath;
        }

        for (int i = 0; i < completedPaths.Count; i++)
        {
            if (IsSameSegmentPath(completedPaths[i], currentPath))
            {
                for (var nextIndex = i + 1; nextIndex < completedPaths.Count; nextIndex++)
                {
                    var nextPath = completedPaths[nextIndex];
                    if (File.Exists(nextPath))
                        return nextPath;
                }

                return IsExistingPath(activePath) ? activePath : null;
            }
        }

        if (IsSameSegmentPath(activePath, currentPath))
            return IsExistingPath(activePath) ? activePath : null;

        return GetOldestExistingSegmentPath(completedPaths)
            ?? (IsExistingPath(activePath) ? activePath : null);
    }

    public TimeSpan? GetSegmentStartPts(string path)
    {
        string? matchedPath = null;
        TimeSpan? matchedStart = null;
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (IsSameSegmentPath(seg.Path, path))
                {
                    matchedPath = seg.Path;
                    matchedStart = seg.StartPts;
                    break;
                }
            }

            if (matchedStart == null &&
                IsSameSegmentPath(_activeSegmentPath, path) &&
                _activeSegmentPath != null)
            {
                matchedPath = _activeSegmentPath;
                matchedStart = GetActiveSegmentStartPts();
            }
        }

        return matchedStart.HasValue && IsExistingPath(matchedPath)
            ? matchedStart
            : null;
    }

    public IReadOnlyList<string> GetValidSegmentPaths(TimeSpan inPoint, TimeSpan outPoint)
    {
        List<string> paths;
        lock (_indexLock)
        {
            if (outPoint <= inPoint)
            {
                return Array.Empty<string>();
            }

            paths = new List<string>();
            foreach (var seg in _completedSegments)
            {
                if (seg.StartPts < outPoint && seg.EndPts > inPoint)
                {
                    paths.Add(seg.Path);
                }
            }
        }

        // Do not include the active segment. It is still being written to and may not
        // have valid headers yet. After ForceRotateForExport, all relevant data is
        // in the completed segments.
        return paths
            .Where(File.Exists)
            .ToList();
    }

    public long MaxDiskBytes => _options.MaxDiskBytes;

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
                // First pause captures the recording start boundary.
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
            // not overwrite the end PTS; the outermost resume captures the true range.
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

    private void EvictOldestSegments()
    {
        List<PendingEvictedSegmentDelete>? pendingDeletes = null;
        lock (_indexLock)
        {
            EvictOldestSegmentsLocked(EnsurePendingEvictionDeleteList(ref pendingDeletes));
        }

        QueueEvictedSegmentDeletes(pendingDeletes);
    }

    private void EvictOldestSegmentsLocked(List<PendingEvictedSegmentDelete> pendingDeletes)
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
                if (TryCreatePendingEvictionDelete(oldest.Path, oldest.SizeBytes, "valid_window", out var pendingDelete))
                {
                    pendingDeletes.Add(pendingDelete);
                    evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                    _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
                    _completedSegments.RemoveAt(0);
                    evictedCount++;
                }
                else
                {
                    break; // Can't safely unlink this segment, stop evicting.
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
            if (TryCreatePendingEvictionDelete(oldest.Path, oldest.SizeBytes, "disk_budget", out var pendingDelete))
            {
                pendingDeletes.Add(pendingDelete);
                evictedBytes = AddNonNegativeSaturated(evictedBytes, oldest.SizeBytes);
                _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
                _completedSegments.RemoveAt(0);
                evictedCount++;
            }
            else
            {
                break; // Can't safely unlink this segment, stop evicting.
            }
        }

        if (evictedCount > 0)
        {
            _totalDiskBytes = SubtractNonNegative(_totalDiskBytes, evictedBytes);
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_EVICT count={evictedCount} evicted_bytes={evictedBytes} remaining_segments={_completedSegments.Count}");
        }
    }

    private void QueueEvictedSegmentDeletes(List<PendingEvictedSegmentDelete>? pendingDeletes)
    {
        // A non-null list means an eviction pass ran; use it as the hook to
        // retry parked deletes even when this pass produced no new ones.
        if (pendingDeletes == null)
        {
            return;
        }

        foreach (var pendingDelete in pendingDeletes)
        {
            QueueEvictedSegmentDelete(pendingDelete, parkedRetry: false);
        }

        RetryParkedEvictionDeletes();
    }

    private void QueueEvictedSegmentDelete(PendingEvictedSegmentDelete pendingDelete, bool parkedRetry)
    {
        ThreadPool.QueueUserWorkItem(
            static state =>
            {
                var (manager, delete, isParkedRetry) =
                    ((FlashbackBufferManager, PendingEvictedSegmentDelete, bool))state!;
                manager.DeleteEvictedFileWithRetry(delete, isParkedRetry);
            },
            (this, pendingDelete, parkedRetry));
    }

    private void RetryParkedEvictionDeletes()
    {
        foreach (var parkedPath in _parkedEvictionDeletes.Keys)
        {
            if (_parkedEvictionDeletes.TryRemove(parkedPath, out var parkedDelete))
            {
                QueueEvictedSegmentDelete(parkedDelete, parkedRetry: true);
            }
        }
    }

    /// <summary>
    /// Final synchronous best-effort pass over parked eviction deletes. Runs
    /// outside <see cref="_indexLock"/>; files that still cannot be deleted are
    /// logged as abandoned and left for startup cache cleanup.
    /// </summary>
    private void SweepParkedEvictionDeletesBestEffort(string operation)
    {
        foreach (var parkedPath in _parkedEvictionDeletes.Keys)
        {
            if (!_parkedEvictionDeletes.TryRemove(parkedPath, out var parkedDelete))
            {
                continue;
            }

            if (DeleteEvictedFile(
                    parkedDelete.FullPath,
                    parkedDelete.SessionRoot,
                    parkedDelete.SizeBytes,
                    parkedDelete.Reason,
                    EvictedSegmentDeleteMaxAttempts + 1))
            {
                continue;
            }

            Logger.Log(
                $"FLASHBACK_BUFFER_EVICT_DELETE_ABANDONED op={operation} reason={parkedDelete.Reason} " +
                $"path='{parkedDelete.FullPath}' attempts={EvictedSegmentDeleteMaxAttempts + 1}");
        }
    }

    private bool TryCreatePendingEvictionDelete(
        string filePath,
        long sizeBytes,
        string reason,
        out PendingEvictedSegmentDelete pendingDelete)
    {
        pendingDelete = default;
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

        if (!FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot))
        {
            Logger.Log($"FLASHBACK_BUFFER_EVICT_DELETE_SKIP reason=outside_session path='{fullPath}'");
            return false;
        }

        pendingDelete = new PendingEvictedSegmentDelete(
            fullPath,
            sessionRoot,
            Math.Max(0, sizeBytes),
            reason);
        return true;
    }

    private void DeleteEvictedFileWithRetry(PendingEvictedSegmentDelete pendingDelete, bool parkedRetry)
    {
        for (var attempt = 1; attempt <= EvictedSegmentDeleteMaxAttempts; attempt++)
        {
            if (DeleteEvictedFile(
                    pendingDelete.FullPath,
                    pendingDelete.SessionRoot,
                    pendingDelete.SizeBytes,
                    pendingDelete.Reason,
                    attempt))
            {
                if (parkedRetry)
                {
                    Logger.Log(
                        $"FLASHBACK_BUFFER_EVICT_DELETE_PARKED_RECOVERED reason={pendingDelete.Reason} " +
                        $"path='{pendingDelete.FullPath}' attempt={attempt}");
                }

                return;
            }

            if (attempt < EvictedSegmentDeleteMaxAttempts)
            {
                Thread.Sleep(EvictedSegmentDeleteRetryDelayMs);
            }
        }

        // Park instead of abandoning: the file is already unlinked from the
        // index, so without a later retry it would be orphaned until the next
        // app start (e.g. the decoder holds the oldest segment during a pause).
        _parkedEvictionDeletes[pendingDelete.FullPath] = pendingDelete;
        Logger.Log(
            $"FLASHBACK_BUFFER_EVICT_DELETE_PARKED reason={pendingDelete.Reason} " +
            $"path='{pendingDelete.FullPath}' attempts={EvictedSegmentDeleteMaxAttempts} " +
            $"parked_total={_parkedEvictionDeletes.Count}");
    }

    private static bool DeleteEvictedFile(string fullPath, string sessionRoot, long sizeBytes, string reason, int attempt)
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
                $"path='{Path.GetFileName(fullPath)}' size_bytes={Math.Max(0, sizeBytes)} " +
                $"attempt={attempt} elapsed_ms={elapsedMs:0.###}");
            return true;
        }
        catch (Exception ex)
        {
            var elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_BUFFER_EVICT_DELETE_WARN reason={reason} path='{fullPath}' " +
                $"type={ex.GetType().Name} msg='{ex.Message}' attempt={attempt} elapsed_ms={elapsedMs:0.###}");
            return false;
        }
    }

}
