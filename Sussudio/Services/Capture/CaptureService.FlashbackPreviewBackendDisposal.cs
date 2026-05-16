using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private readonly record struct FlashbackPreviewBackendDisposalRequest(
        bool PurgeSegments,
        bool DetachMicrophoneWriter,
        bool ExportOperationLockAlreadyHeld);

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
                    new FlashbackPreviewBackendDisposalRequest(
                        effectivePurgeSegments,
                        detachMicrophoneWriter,
                        ExportOperationLockAlreadyHeld: true))
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
        var flashbackSink = _flashbackSink;
        var flashbackBufferManager = _flashbackBufferManager;
        var flashbackExporter = _flashbackExporter;
        var flashbackPlaybackController = _flashbackBackend.TakePlaybackController();

        // Do NOT null the sink/buffer/exporter fields yet; the encoding loop may still be running
        // and code that checks _flashbackSink (e.g. IsFlashbackActive) must see
        // a consistent state until the sink is fully drained and stopped.

        if (flashbackPlaybackController != null)
        {
            try
            {
                flashbackPlaybackController.GoLive();
                flashbackPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Detach feeds first — stops new frames from entering the sink
        _flashbackBackend.DetachProducers(
            new FlashbackProducerDetachRequest(
                _unifiedVideoCapture,
                _wasapiAudioCapture,
                _microphoneCapture,
                "FLASHBACK_PREVIEW_DETACH_WARN",
                request.DetachMicrophoneWriter));

        Task sinkCompletionTask = Task.CompletedTask;
        if (flashbackSink != null)
        {
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try
            {
                // Once feeds are detached, finish the bounded sink drain even if the
                // caller cancels so service fields never point at a half-torn backend.
                await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            try
            {
                await flashbackSink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            sinkCompletionTask = flashbackSink.EncodingCompletionTask;
        }

        // Now that the sink is fully stopped and disposed, clear the fields.
        // Any concurrent reader of _flashbackSink sees either the old (valid)
        // value or null — never a half-disposed object.
        _flashbackBackend.Clear();

        if (!sinkCompletionTask.IsCompleted)
        {
            ScheduleDeferredFlashbackBackendCleanup(
                sinkCompletionTask,
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                    request.PurgeSegments));
            flashbackBufferManager = null;
            flashbackExporter = null;
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (request.PurgeSegments)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        var cleanupCompleted = await CleanupFlashbackBackendArtifactsAfterExportAsync(
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                    request.PurgeSegments),
                "preview_backend_dispose",
                request.ExportOperationLockAlreadyHeld)
            .ConfigureAwait(false);

        if (!cleanupCompleted)
        {
            ScheduleDeferredFlashbackBackendCleanup(
                Task.Delay(TimeSpan.FromSeconds(1)),
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge_retry" : "preview_backend_dispose_retry",
                    request.PurgeSegments));
        }

        Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_OK purge={request.PurgeSegments}");
    }
}
