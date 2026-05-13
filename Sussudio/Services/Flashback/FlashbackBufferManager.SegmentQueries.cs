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
    /// For compatibility: single file means 1 "segment" when active, 0 otherwise.
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
            {
                return seg.Path;
            }
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

                    return _activeSegmentPath != null && File.Exists(_activeSegmentPath)
                        ? _activeSegmentPath
                        : null;
                }
            }

            if (IsSameSegmentPath(_activeSegmentPath, currentPath))
                return _activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null;

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

            if (_activeSegmentPath != null && File.Exists(_activeSegmentPath))
            {
                var activeStartPts = GetActiveSegmentStartPts();
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
}
