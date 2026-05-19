using System;
using System.IO;
using System.Threading;

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

}
