using System;
using System.IO;
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
    private TimeSpan _latestPts;
    private TimeSpan _validStartPts;
    private long _totalDiskBytes;
    private bool _disposed;

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
            lock (_indexLock)
            {
                var duration = _latestPts - _validStartPts;
                return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
            }
        }
    }

    public TimeSpan ValidStartPts
    {
        get
        {
            lock (_indexLock)
            {
                return _validStartPts;
            }
        }
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
            _latestPts = TimeSpan.Zero;
            _validStartPts = TimeSpan.Zero;
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
        lock (_indexLock)
        {
            if (pts > _latestPts)
            {
                _latestPts = pts;
            }

            // Evict: advance valid start if buffer exceeds max duration
            var duration = _latestPts - _validStartPts;
            if (duration > _options.BufferDuration)
            {
                _validStartPts = _latestPts - _options.BufferDuration;
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
            _latestPts = TimeSpan.Zero;
            _validStartPts = TimeSpan.Zero;
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
