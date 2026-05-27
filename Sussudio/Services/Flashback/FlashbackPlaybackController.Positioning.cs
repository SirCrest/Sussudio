using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Decoder file lifecycle ---

    private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);

    private string? _currentOpenFilePath;

    private FlashbackDecoder CreateDecoder()
    {
        var useGpu = GpuDecodeEnabled;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CREATE gpu={useGpu}");
        var decoder = new FlashbackDecoder();

        // Get D3D11 device pointers for GPU-direct decode (skip if GPU decode disabled).
        var devicePtr = IntPtr.Zero;
        var contextPtr = IntPtr.Zero;
        if (useGpu)
        {
            var d3dManager = _videoCapture?.D3DManager;
            devicePtr = d3dManager?.Device?.NativePointer ?? IntPtr.Zero;
            contextPtr = d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero;
        }
        decoder.Initialize(devicePtr, contextPtr);

        RestoreAudioCallback(decoder);
        return decoder;
    }

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan? targetPts = null)
    {
        // Determine which segment file contains the target position.
        var filePath = targetPts.HasValue
            ? _bufferManager.GetValidSegmentFileForPosition(targetPts.Value)
            : _bufferManager.ActiveFilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Logger.Log("FLASHBACK_PLAYBACK_NO_FILE");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_no_file");
            }

            fileOpen = false;
            _currentOpenFilePath = null;
            _decoderHwAccel = "N/A";
            return;
        }

        // If already open on the correct file, nothing to do.
        if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))
            return;

        try
        {
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open");
                fileOpen = false;
                _currentOpenFilePath = null;
                _decoderHwAccel = "N/A";
            }

            decoder.OpenFile(filePath);
            fileOpen = true;
            _currentOpenFilePath = filePath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN path='{filePath}' hw_accel={_decoderHwAccel}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_error");
            }
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
        }
    }

    private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)
        => fileOpen && decoder.IsOpen;

    private bool TrySeekWithActiveFmp4Reopen(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
        {
            return true;
        }

        // Active fMP4 segment: demuxer fragment index is stale. Reopen and retry.
        // MPEG-TS handles appended data via eof_reached reset and does not need reopening.
        if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
        {
            if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))
            {
                SetReopenFailure(reason, "near_live", seekTarget);
                return false;
            }

            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);
        }

        if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))
        {
            return true;
        }

        SetReopenFailure(reason, "seek_failed", seekTarget);
        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
        return false;
    }

    private bool TryReopenCurrentFileAndSeek(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                currentPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private bool TryReopenCurrentFileAndSeekKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                currentPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (decoder.SeekToKeyframe(seekTarget, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "keyframe_seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= TimeSpan.Zero)
        {
            return false;
        }

        var distanceFromLive = seekTarget >= latestPts
            ? TimeSpan.Zero
            : latestPts - seekTarget;
        if (distanceFromLive > ActiveFmp4ReopenNearLiveGuard)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE reason={reason} target_ms={(long)seekTarget.TotalMilliseconds} latest_ms={(long)latestPts.TotalMilliseconds} distance_ms={(long)distanceFromLive.TotalMilliseconds} guard_ms={(long)ActiveFmp4ReopenNearLiveGuard.TotalMilliseconds}");
        return true;
    }

    private bool TrySeekAdjacentSegmentStart(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        out TimeSpan effectiveSeekTarget,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        effectiveSeekTarget = seekTarget;
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        var nextPath = _bufferManager.GetNextSegmentFile(currentPath);
        if (string.IsNullOrWhiteSpace(nextPath) || IsSamePlaybackPath(nextPath, currentPath))
        {
            return false;
        }

        var nextStart = _bufferManager.GetSegmentStartPts(nextPath);
        if (!nextStart.HasValue)
        {
            return false;
        }

        var targetGap = (nextStart.Value - seekTarget).Duration();
        if (targetGap > AdjacentSegmentSeekFallbackWindow)
        {
            return false;
        }

        effectiveSeekTarget = seekTarget < nextStart.Value ? nextStart.Value : seekTarget;
        try
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK reason={reason} " +
                $"from='{System.IO.Path.GetFileName(currentPath)}' next='{System.IO.Path.GetFileName(nextPath)}' " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} effective_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                nextPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, effectiveSeekTarget, reason, cancellationToken))
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                ResetPlaybackPtsCadenceBaseline();
                return true;
            }

            SetReopenFailure(reason, "adjacent_seek_failed", effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL reason={reason} path='{nextPath}' offset_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR reason={reason} path='{nextPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private void ReopenDecoderPlaybackFile(
        FlashbackDecoder decoder,
        string path,
        ref bool fileOpen,
        bool updateCurrentOpenPath,
        bool closeOnlyWhenOpen)
    {
        if (!closeOnlyWhenOpen || decoder.IsOpen)
        {
            decoder.CloseFile();
        }

        fileOpen = false;
        decoder.OpenFile(path);
        fileOpen = true;
        if (updateCurrentOpenPath)
        {
            _currentOpenFilePath = path;
        }

        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
    }

    private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)
    {
        _decoderHwAccel = "N/A";
        fileOpen = false;
        _currentOpenFilePath = null;
    }

    private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)
    {
        try
        {
            if (decoder.IsOpen) decoder.CloseFile();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)
    {
        var cleanupStarted = Stopwatch.GetTimestamp();
        var wasOpen = decoder?.IsOpen ?? false;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP was_open={wasOpen}");
        var releaseStarted = Stopwatch.GetTimestamp();
        ReleasePreviousHeldFrame();
        var releaseMs = Stopwatch.GetElapsedTime(releaseStarted).TotalMilliseconds;
        var closeMs = 0d;
        var disposeMs = 0d;
        if (decoder != null)
        {
            var decoderToDispose = decoder;
            decoder = null;
            try
            {
                if (decoderToDispose.IsOpen)
                {
                    var closeStarted = Stopwatch.GetTimestamp();
                    decoderToDispose.CloseFile();
                    closeMs = Stopwatch.GetElapsedTime(closeStarted).TotalMilliseconds;
                }
            }
            catch (Exception ex)
            {
                closeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close type={ex.GetType().Name} msg='{ex.Message}'");
            }

            try
            {
                var disposeStarted = Stopwatch.GetTimestamp();
                decoderToDispose.Dispose();
                disposeMs = Stopwatch.GetElapsedTime(disposeStarted).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                disposeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        var totalMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
        Logger.Log(
            $"FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen} " +
            $"release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
    }

    // --- In/Out points and playback position mapping ---
    // --- In/Out points ---

    private long _inPointTicks = long.MinValue;
    private long _outPointTicks = long.MinValue;
    private long _inPointFilePtsTicks = long.MinValue;
    private long _outPointFilePtsTicks = long.MinValue;

    public TimeSpan? InPoint
    {
        get
        {
            var t = Interlocked.Read(ref _inPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? OutPoint
    {
        get
        {
            var t = Interlocked.Read(ref _outPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? InPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _inPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan? OutPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _outPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan SetInPoint() => SetInPointAt(null);

    /// <summary>
    /// Pin the in-point at an explicit user-intended position rather than the
    /// controller's last decoded keyframe. The UI should pass the position the
    /// user is visually pointing at (its FlashbackPlaybackPosition), which during
    /// scrubbing is the user's drag target rather than the keyframe-snapped
    /// PlaybackPosition the controller publishes after each decode. Without this
    /// overload, mid-GOP "click In" landed on the prior keyframe and the marker
    /// could appear hundreds of milliseconds before where the playhead sat.
    /// </summary>
    public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);

    private TimeSpan SetInPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetInPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        InPoint = pos;
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && outTicks <= pos.Ticks)
        {
            OutPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_IN pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public TimeSpan SetOutPoint() => SetOutPointAt(null);

    /// <summary>
    /// Pin the out-point at an explicit user-intended position. See
    /// <see cref="SetInPointAt(TimeSpan)"/> for the rationale: the UI's visual
    /// playhead and the controller's keyframe-snapped PlaybackPosition can
    /// differ by hundreds of milliseconds during scrubbing.
    /// </summary>
    public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);

    private TimeSpan SetOutPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetOutPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        OutPoint = pos;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        if (inTicks != long.MinValue && inTicks >= pos.Ticks)
        {
            InPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_IN invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_OUT pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public void ClearInOutPoints()
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:ClearInOutPoints");
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
            return;
        }

        InPoint = null;
        OutPoint = null;
        ClearLastCommandFailure();
        Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT");
    }

    public void RestoreInOutPoints(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        InPoint = inPoint;
        OutPoint = outPoint;

        if (inPoint.HasValue && inPointFilePts.HasValue && inPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);
        }

        if (outPoint.HasValue && outPointFilePts.HasValue && outPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);
        }
    }

    private bool CheckOutPoint(TimeSpan position, Stopwatch pacingStopwatch)
    {
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && position >= TimeSpan.FromTicks(outTicks))
        {
            Logger.Log($"FLASHBACK_PLAYBACK_HIT_OUTPOINT pos_ms={(long)position.TotalMilliseconds}");
            SafePauseRendering("out_point");
            pacingStopwatch.Stop();
            SetState(FlashbackPlaybackState.Paused);
            return true;
        }

        return false;
    }

    private TimeSpan NormalizeMarkerPosition(TimeSpan position)
    {
        if (position <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var bufferDuration = _bufferManager.BufferedDuration;
        return position > bufferDuration ? bufferDuration : position;
    }

    /// <summary>
    /// Returns true if the given path is the active fMP4 segment. The reopen-and-retry
    /// workaround only applies to fMP4 (fragment index goes stale); transport streams
    /// handle appended data via eof_reached reset and don't need reopening.
    /// </summary>
    private bool IsActiveFmp4Segment(string? path)
        => path != null
        && IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)
        && path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);

    private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);

    /// <summary>
    /// Clamp a scrub/seek position to the currently usable buffer range, optionally
    /// account for segment eviction that has happened since a scrub session captured
    /// its frozen reference. Without the eviction adjustment, a long-held scrub at
    /// position 0 maps via SaturatingAdd(pos, frozenValidStart) to a file PTS that
    /// has been evicted - EnsureFileOpen fails and the user gets a sudden snap-to-
    /// live instead of clamping to the new oldest available position.
    /// </summary>
    private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)
    {
        var bufferDuration = _bufferManager.BufferedDuration;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        var min = inTicks == long.MinValue ? TimeSpan.Zero : TimeSpan.FromTicks(inTicks);
        var outTicks = Interlocked.Read(ref _outPointTicks);
        var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);
        if (max > bufferDuration) max = bufferDuration;
        if (frozenValidStart.HasValue)
        {
            // Eviction may have advanced ValidStartPts past the scrub session's
            // captured reference. Positions in the evicted gap (in scrub coords)
            // would resolve to file PTS values whose segments no longer exist.
            // Promote min so those positions clamp up to the new oldest valid
            // position rather than failing the file lookup downstream.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (currentValidStart > frozenValidStart.Value)
            {
                var evictedDelta = currentValidStart - frozenValidStart.Value;
                if (evictedDelta > min)
                {
                    min = evictedDelta;
                }
            }
        }
        if (min > max) min = max;
        if (position < min) return min;
        if (position > max) return max;
        return position;
    }

    private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks + rightTicks);
    }

    private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks - rightTicks);
    }

    private static bool IsSamePlaybackPath(string? left, string? right)
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
            Logger.Log($"FLASHBACK_PLAYBACK_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
