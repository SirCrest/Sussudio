using System.Threading;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
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
}
