using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

/// <summary>
/// Manages a single MPEG-TS flashback buffer file.
/// Tracks buffered duration via PTS updates from the encoder.
/// </summary>
internal sealed class FlashbackBufferManager : IDisposable
{
    private readonly object _indexLock = new();
    private readonly FlashbackBufferOptions _options;
    private string? _sessionId;
    private string? _activeSegmentPath;
    private long _latestPtsTicks;
    private long _validStartPtsTicks;
    private long _totalDiskBytes;
    private bool _disposed;
    private int _evictionPauseCount;
    private TimeSpan _recordingStartPts;
    private TimeSpan _recordingEndPts;
    private readonly List<CompletedSegment> _completedSegments = new();
    private int _nextSegmentIndex;
    private long _completedSegmentBytes; // running total — avoids iterating the list

    private record CompletedSegment(string Path, int SequenceNumber, TimeSpan StartPts, TimeSpan EndPts, long SizeBytes);

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

    public TimeSpan LatestPts
    {
        get { return TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)); }
    }

    public bool EvictionPaused => Volatile.Read(ref _evictionPauseCount) > 0;

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
            Interlocked.Increment(ref _evictionPauseCount);
            _recordingStartPts = TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks));
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_PAUSED count={Volatile.Read(ref _evictionPauseCount)} start_pts_ms={(long)_recordingStartPts.TotalMilliseconds}");
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
            _recordingEndPts = TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks));
            var newCount = Interlocked.Decrement(ref _evictionPauseCount);
            if (newCount < 0)
            {
                Interlocked.CompareExchange(ref _evictionPauseCount, 0, newCount);
                System.Diagnostics.Debug.WriteLine($"FLASHBACK_BUFFER: WARNING — eviction pause count underflowed to {newCount}, clamped to 0");
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

            // Clean up orphaned export temp files from previous sessions
            FlashbackExporter.CleanupOrphanedTempFiles(_options.TempDirectory);

            // Clean up stale files from previous sessions
            var staleDeleted = 0;
            foreach (var filePath in Directory.EnumerateFiles(_options.TempDirectory, "fb_*.*", SearchOption.TopDirectoryOnly))
            {
                if (TryDeleteFile(filePath))
                {
                    staleDeleted++;
                }
            }

            _activeSegmentPath = null;
            _completedSegments.Clear();
            _completedSegmentBytes = 0;
            _nextSegmentIndex = 0;
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
            Interlocked.Exchange(ref _evictionPauseCount, 0);
            _sessionId = sessionId;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{_options.TempDirectory}' stale_deleted={staleDeleted}");
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

            var path = Path.Combine(_options.TempDirectory, $"fb_{_sessionId}_{_nextSegmentIndex:D4}.ts");
            _nextSegmentIndex++;
            _activeSegmentPath = path;
            return path;
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
    /// </summary>
    private string? GetSegmentFileForPositionCore(TimeSpan absolutePts)
    {
        foreach (var seg in _completedSegments)
        {
            if (absolutePts >= seg.StartPts && absolutePts < seg.EndPts)
                return seg.Path;
        }
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
            // currentPath not found (evicted or unknown)
            // Return the oldest available segment, or active if none
            if (_completedSegments.Count > 0)
                return _completedSegments[0].Path;
            return _activeSegmentPath;
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
            PurgeAllSegments();
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
