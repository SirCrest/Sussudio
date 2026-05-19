using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
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

                EvictOldestSegments();
            }
        }
    }
}
