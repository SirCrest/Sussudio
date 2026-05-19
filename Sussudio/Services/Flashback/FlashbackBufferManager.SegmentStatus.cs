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
}
