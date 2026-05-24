using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
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
    /// For compatibility: single file means 1 "segment" when active, 0 otherwise.
    /// </summary>
    public int SegmentCount
    {
        get
        {
            lock (_indexLock)
            {
                return _completedSegments.Count(seg => File.Exists(seg.Path)) +
                    (TryGetExistingActiveSegmentPath(out _) ? 1 : 0);
            }
        }
    }

    public string? ActiveFilePath
    {
        get
        {
            lock (_indexLock)
            {
                return TryGetExistingActiveSegmentPath(out var activePath)
                    ? activePath
                    : null;
            }
        }
    }

    private bool TryGetExistingActiveSegmentPath(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? path)
    {
        path = _activeSegmentPath;
        return path != null && File.Exists(path);
    }

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

            if (TryGetExistingActiveSegmentPath(out var activePath))
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

            return result;
        }
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
                    ?? (TryGetExistingActiveSegmentPath(out var activePath) ? activePath : null);
            }

            if (TryGetExistingActiveSegmentPath(out var existingActivePath))
            {
                return existingActivePath;
            }

            return GetOldestExistingSegmentPath();
        }
    }

    private string? GetOldestExistingSegmentPath()
    {
        foreach (var seg in _completedSegments)
        {
            if (File.Exists(seg.Path))
            {
                return seg.Path;
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
        lock (_indexLock)
        {
            for (int i = 0; i < _completedSegments.Count; i++)
            {
                if (IsSameSegmentPath(_completedSegments[i].Path, currentPath))
                {
                    for (var nextIndex = i + 1; nextIndex < _completedSegments.Count; nextIndex++)
                    {
                        var nextPath = _completedSegments[nextIndex].Path;
                        if (File.Exists(nextPath))
                            return nextPath;
                    }

                    return TryGetExistingActiveSegmentPath(out var activePath)
                        ? activePath
                        : null;
                }
            }

            if (IsSameSegmentPath(_activeSegmentPath, currentPath))
                return TryGetExistingActiveSegmentPath(out var activePath) ? activePath : null;

            return GetOldestExistingSegmentPath()
                ?? (TryGetExistingActiveSegmentPath(out var fallbackActivePath) ? fallbackActivePath : null);
        }
    }

    public TimeSpan? GetSegmentStartPts(string path)
    {
        lock (_indexLock)
        {
            foreach (var seg in _completedSegments)
            {
                if (IsSameSegmentPath(seg.Path, path) && File.Exists(seg.Path))
                {
                    return seg.StartPts;
                }
            }

            if (IsSameSegmentPath(_activeSegmentPath, path) &&
                _activeSegmentPath != null &&
                File.Exists(_activeSegmentPath))
            {
                return GetActiveSegmentStartPts();
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
                if (seg.StartPts < outPoint && seg.EndPts > inPoint && File.Exists(seg.Path))
                {
                    paths.Add(seg.Path);
                }
            }

            // Do not include the active segment. It is still being written to and may not
            // have valid headers yet. After ForceRotateForExport, all relevant data is
            // in the completed segments.
            return paths;
        }
    }
}
