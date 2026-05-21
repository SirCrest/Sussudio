using System;
using System.Collections.Generic;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
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
