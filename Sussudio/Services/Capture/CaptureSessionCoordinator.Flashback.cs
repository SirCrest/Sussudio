using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(settings, ct),
            cancellationToken,
            propagateCancellationToOperation: true);
    }

    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackRecordingFormat,
            ct => _captureService.UpdateRecordingFormatAsync(format, ct),
            cancellationToken);

    public Task CycleFlashbackEncoderSettingsAsync(
        VideoQuality? quality = null,
        double? customBitrateMbps = null,
        string? nvencPreset = null,
        string? splitEncodeMode = null,
        CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.CycleFlashbackEncoderSettings,
            ct => _captureService.CycleFlashbackEncoderSettingsAsync(quality, customBitrateMbps, nvencPreset, splitEncodeMode, ct),
            cancellationToken,
            coalesceLatest: true);

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.SetFlashbackEnabled,
            ct => _captureService.SetFlashbackEnabledAsync(enabled, ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task UpdateFlashbackSettingsAsync(int bufferMinutes, bool gpuDecode, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackSettings,
            ct => _captureService.UpdateFlashbackSettingsAsync(bufferMinutes, gpuDecode, ct),
            cancellationToken);

    internal bool IsFlashbackActive => _captureService.IsFlashbackActive;

    internal long FlashbackTotalBytesWritten => _captureService.FlashbackTotalBytesWritten;

    internal FlashbackBufferStatus GetFlashbackBufferStatus()
    {
        ThrowIfDisposed();
        var bufferManager = _captureService.FlashbackBufferManager;
        if (bufferManager == null || !_captureService.IsFlashbackActive)
        {
            return FlashbackBufferStatus.Inactive;
        }

        return new FlashbackBufferStatus(
            true,
            bufferManager.Options.BufferDuration,
            bufferManager.BufferedDuration,
            _captureService.FlashbackDiskBytes,
            bufferManager.IsDiskWarningActive);
    }

    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
    {
        ThrowIfDisposed();
        var controller = _captureService.FlashbackPlaybackController;
        return controller == null || controller.IsDisposed
            ? FlashbackPlaybackSnapshot.Inactive(
                _lastFlashbackCommandRejection,
                Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs))
            : new FlashbackPlaybackSnapshot(
                true,
                controller.State,
                controller.PlaybackPosition,
                controller.GapFromLive,
                controller.InPoint,
                controller.OutPoint,
                controller.InPointFilePts,
                controller.OutPointFilePts,
                controller.PlaybackThreadAlive,
                controller.PendingCommands,
                controller.LastCommandFailure,
                controller.LastCommandFailureUtcUnixMs);
    }

    internal Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken,
        TimeSpan? inPointFilePts = null,
        TimeSpan? outPointFilePts = null,
        bool force = false)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackRangeAsync(
            inPoint,
            outPoint,
            outputPath,
            progress,
            cancellationToken,
            inPointFilePts,
            outPointFilePts,
            force);
    }

    internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds,
        string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken,
        bool force = false)
    {
        ThrowIfDisposed();
        return _captureService.ExportFlashbackLastNSecondsAsync(seconds, outputPath, progress, cancellationToken, force);
    }

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        ThrowIfDisposed();
        return _captureService.GetFlashbackSegments();
    }

    internal bool FlashbackBeginScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)) return false;
        return controller.BeginScrub(position);
    }

    internal bool FlashbackSeek(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackSeek), out var controller)) return false;
        return controller.Seek(position);
    }

    internal bool FlashbackUpdateScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)) return false;
        return controller.UpdateScrub(position);
    }

    internal bool FlashbackEndScrub()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)) return false;
        return controller.EndScrub();
    }

    internal bool FlashbackEndScrubAt(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackEndScrubAt), out var controller)) return false;
        return controller.EndScrubAt(position);
    }

    internal bool FlashbackPlay()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPlay), out var controller)) return false;
        return controller.Play();
    }

    internal bool FlashbackPause()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPause), out var controller)) return false;
        return controller.Pause();
    }

    internal bool FlashbackGoLive()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)) return false;
        return controller.GoLive();
    }

    internal bool FlashbackNudge(TimeSpan delta)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackNudge), out var controller)) return false;
        return controller.NudgePosition(delta);
    }

    internal TimeSpan? FlashbackSetInPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetInPoint), out var controller)
            ? controller.SetInPoint()
            : null;
    }

    internal TimeSpan? FlashbackSetInPointAt(TimeSpan position)
    {
        return TryGetActiveFlashback(nameof(FlashbackSetInPointAt), out var controller)
            ? controller.SetInPointAt(position)
            : null;
    }

    internal TimeSpan? FlashbackSetOutPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetOutPoint), out var controller)
            ? controller.SetOutPoint()
            : null;
    }

    internal TimeSpan? FlashbackSetOutPointAt(TimeSpan position)
    {
        return TryGetActiveFlashback(nameof(FlashbackSetOutPointAt), out var controller)
            ? controller.SetOutPointAt(position)
            : null;
    }

    internal bool FlashbackClearInOutPoints()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)) return false;
        controller.ClearInOutPoints();
        return true;
    }

    private bool TryGetActiveFlashback(
        string command,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FlashbackPlaybackController? controller)
    {
        ThrowIfDisposed();
        controller = _captureService.FlashbackPlaybackController;
        if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })
        {
            return true;
        }

        var reason = controller == null
            ? "missing_controller"
            : controller.IsDisposed
                ? "disposed"
                : !controller.IsInitialized
                ? "not_initialized"
                : $"state_{controller.State}";
        _lastFlashbackCommandRejection = $"{reason}:{command}";
        Interlocked.Exchange(ref _lastFlashbackCommandRejectionUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}");
        return false;
    }
}
