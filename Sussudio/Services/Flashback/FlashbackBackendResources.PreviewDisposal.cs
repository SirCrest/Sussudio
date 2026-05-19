using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackPreviewBackendDisposalRequest(
    UnifiedVideoCapture? VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    EventHandler<long> FrameEncodedHandler,
    Func<Task<bool>> AcquireExportOperationLockAsync,
    Action<string> ReleaseExportOperationLock,
    bool PurgeSegments,
    bool DetachMicrophoneWriter,
    bool ExportOperationLockAlreadyHeld,
    CancellationToken CancellationToken);

internal sealed partial class FlashbackBackendResources
{
    public async Task DisposePreviewBackendAsync(
        FlashbackPreviewBackendDisposalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.FrameEncodedHandler);
        ArgumentNullException.ThrowIfNull(request.AcquireExportOperationLockAsync);
        ArgumentNullException.ThrowIfNull(request.ReleaseExportOperationLock);

        var flashbackSink = Sink;
        var flashbackBufferManager = BufferManager;
        var flashbackExporter = Exporter;
        var flashbackPlaybackController = TakePlaybackController();

        // Do NOT null the sink/buffer/exporter fields yet; the encoding loop may still be running
        // and code that checks the live sink must see a consistent state until the sink is drained.

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

        // Detach feeds first so new frames cannot enter the sink during teardown.
        DetachProducers(
            new FlashbackProducerDetachRequest(
                request.VideoCapture,
                request.AudioCapture,
                request.MicrophoneCapture,
                "FLASHBACK_PREVIEW_DETACH_WARN",
                request.DetachMicrophoneWriter));

        Task sinkCompletionTask = Task.CompletedTask;
        if (flashbackSink != null)
        {
            flashbackSink.FrameEncoded -= request.FrameEncodedHandler;
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

        // Now that the sink is fully stopped and disposed, clear the aggregate.
        // Any concurrent reader sees either the old valid resources or null, not
        // a half-disposed backend.
        Clear();

        if (!sinkCompletionTask.IsCompleted)
        {
            ScheduleDeferredArtifactCleanup(
                sinkCompletionTask,
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                    request.PurgeSegments),
                request.AcquireExportOperationLockAsync,
                request.ReleaseExportOperationLock);
            flashbackBufferManager = null;
            flashbackExporter = null;
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        if (request.PurgeSegments)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        var cleanupCompleted = await CleanupArtifactsAfterExportAsync(
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                    request.PurgeSegments),
                "preview_backend_dispose",
                request.AcquireExportOperationLockAsync,
                request.ReleaseExportOperationLock,
                request.ExportOperationLockAlreadyHeld)
            .ConfigureAwait(false);

        if (!cleanupCompleted)
        {
            ScheduleDeferredArtifactCleanup(
                Task.Delay(TimeSpan.FromSeconds(1)),
                new FlashbackBackendArtifactCleanupRequest(
                    flashbackBufferManager,
                    flashbackExporter,
                    request.PurgeSegments ? "preview_backend_dispose_purge_retry" : "preview_backend_dispose_retry",
                    request.PurgeSegments),
                request.AcquireExportOperationLockAsync,
                request.ReleaseExportOperationLock);
        }

        Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_OK purge={request.PurgeSegments}");
    }
}
