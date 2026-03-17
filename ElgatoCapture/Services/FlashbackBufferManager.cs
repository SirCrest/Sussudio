using System;
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
    private string? _activeFilePath;
    private long _latestPtsTicks;
    private long _validStartPtsTicks;
    private long _totalDiskBytes;
    private bool _disposed;
    private volatile bool _evictionPaused;
    private TimeSpan _recordingStartPts;
    private TimeSpan _recordingEndPts;

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

    public long TotalDiskBytes
    {
        get
        {
            lock (_indexLock)
            {
                return _totalDiskBytes;
            }
        }
    }

    public TimeSpan LatestPts
    {
        get { return TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)); }
    }

    public bool EvictionPaused => _evictionPaused;

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
            _evictionPaused = true;
            _recordingStartPts = TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks));
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_PAUSED start_pts_ms={(long)_recordingStartPts.TotalMilliseconds}");
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
            _evictionPaused = false;
            Logger.Log($"FLASHBACK_BUFFER_EVICTION_RESUMED start_pts_ms={(long)_recordingStartPts.TotalMilliseconds} end_pts_ms={(long)_recordingEndPts.TotalMilliseconds} range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");
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
                return string.IsNullOrWhiteSpace(_activeFilePath) ? 0 : 1;
            }
        }
    }

    public string? ActiveFilePath
    {
        get
        {
            lock (_indexLock)
            {
                return _activeFilePath;
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

            // Clean up stale files from previous sessions
            var staleDeleted = 0;
            foreach (var filePath in Directory.EnumerateFiles(_options.TempDirectory, "fb_*.*", SearchOption.TopDirectoryOnly))
            {
                if (TryDeleteFile(filePath))
                {
                    staleDeleted++;
                }
            }

            _activeFilePath = null;
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
            _sessionId = sessionId;

            Logger.Log(
                $"FLASHBACK_BUFFER_INIT session='{sessionId}' temp_dir='{_options.TempDirectory}' stale_deleted={staleDeleted}");
        }
    }

    /// <summary>
    /// Gets the single .ts file path for this session.
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

            if (_activeFilePath == null)
            {
                _activeFilePath = Path.Combine(_options.TempDirectory, $"fb_{_sessionId}.ts");
            }

            return _activeFilePath;
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

        // Evict: advance valid start if buffer exceeds max duration (skip while recording)
        if (!_evictionPaused)
        {
            var maxTicks = _options.BufferDuration.Ticks;
            var startTicks = Interlocked.Read(ref _validStartPtsTicks);
            var duration = ptsTicks - startTicks;
            if (duration > maxTicks)
            {
                Interlocked.CompareExchange(ref _validStartPtsTicks, ptsTicks - maxTicks, startTicks);
            }
        }
    }

    /// <summary>
    /// Updates the tracked disk bytes (called periodically by the encoder).
    /// </summary>
    public void UpdateDiskBytes(long bytes)
    {
        lock (_indexLock)
        {
            _totalDiskBytes = bytes;

            if (!_evictionPaused && _totalDiskBytes > _options.MaxDiskBytes)
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
            }
        }
    }

    public void PurgeAllSegments()
    {
        lock (_indexLock)
        {
            if (_activeFilePath != null)
            {
                TryDeleteFile(_activeFilePath);
                _activeFilePath = null;
            }
            Interlocked.Exchange(ref _latestPtsTicks, 0);
            Interlocked.Exchange(ref _validStartPtsTicks, 0);
            _totalDiskBytes = 0;
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
            Logger.Log($"FLASHBACK_BUFFER_DISPOSE file={_activeFilePath != null}");
            PurgeAllSegments();
        }
    }

    private bool TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

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
