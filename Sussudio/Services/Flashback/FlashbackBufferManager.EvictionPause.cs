using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
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
}
