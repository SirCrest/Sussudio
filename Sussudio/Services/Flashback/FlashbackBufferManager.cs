using System;
using System.Collections.Generic;
using System.IO;
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
internal sealed partial class FlashbackBufferManager : IDisposable
{
    private const string RecoveryPreserveMarkerFileName = ".flashback-recovery-preserve";
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
    /// The frame rate the encoder was configured with — ground truth for playback pacing.
    /// Set once when the encoder starts; avoids relying on TS container metadata which
    /// can report doubled rates (e.g. 240 for 120fps content).
    /// </summary>
    public double EncodeFrameRate { get; set; }

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

}
