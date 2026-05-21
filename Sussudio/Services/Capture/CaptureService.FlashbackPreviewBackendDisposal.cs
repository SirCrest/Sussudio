using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task DisposeFlashbackPreviewBackendAsync(
        CancellationToken cancellationToken,
        bool purgeSegments = true,
        bool detachMicrophoneWriter = true)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var exportOperationLockHeld = false;
        try
        {
            await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            exportOperationLockHeld = true;

            var effectivePurgeSegments = _flashbackBackend.ResolveSegmentPurge(
                purgeSegments,
                "preview_backend_dispose");
            await DisposeFlashbackPreviewBackendCoreAsync(
                    cancellationToken,
                    CreateFlashbackPreviewBackendDisposalRequest(
                        effectivePurgeSegments,
                        detachMicrophoneWriter,
                        exportOperationLockAlreadyHeld: true,
                        cancellationToken))
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_preview_backend_dispose");
        }
    }

    private async Task DisposeFlashbackPreviewBackendCoreAsync(
        CancellationToken cancellationToken,
        FlashbackPreviewBackendDisposalRequest request)
    {
        await _flashbackBackend.DisposePreviewBackendAsync(request).ConfigureAwait(false);
    }

    private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest(
        bool purgeSegments,
        bool detachMicrophoneWriter,
        bool exportOperationLockAlreadyHeld,
        CancellationToken cancellationToken)
        => new FlashbackPreviewBackendDisposalRequest(
            _videoPipeline.Capture,
            _previewAudioGraph.ProgramCapture,
            _previewAudioGraph.MicrophoneCapture,
            OnFlashbackFrameEncoded,
            WaitForFlashbackBackendCleanupExportLockAsync,
            ReleaseFlashbackBackendCleanupExportLock,
            purgeSegments,
            detachMicrophoneWriter,
            exportOperationLockAlreadyHeld,
            cancellationToken);
}
