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
}
