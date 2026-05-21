using System;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBackendResources
{
    private async Task RollBackPreviewBackendStartAsync(
        FlashbackPreviewBackendStartRequest request,
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink flashbackSink,
        FlashbackExporter flashbackExporter,
        FlashbackPlaybackController? playbackController)
    {
        flashbackSink.FrameEncoded -= request.FrameEncodedHandler;
        DetachProducers(
            new FlashbackProducerDetachRequest(
                request.VideoCapture,
                request.AudioCapture,
                request.MicrophoneCapture,
                "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN",
                DetachMicrophoneWriter: true));
        try { playbackController?.Dispose(); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
        try { await flashbackSink.DisposeAsync().ConfigureAwait(false); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_SINK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

        var sinkCompletionTask = flashbackSink.EncodingCompletionTask;
        var cleanupBufferManager = bufferManager;
        var cleanupExporter = flashbackExporter;
        if (!sinkCompletionTask.IsCompleted)
        {
            request.ScheduleDeferredCleanup(
                sinkCompletionTask,
                cleanupBufferManager,
                cleanupExporter,
                "preview_init_rollback",
                true);
            cleanupBufferManager = null;
            cleanupExporter = null;
        }

        try { cleanupExporter?.Dispose(); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_EXPORTER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

        try { cleanupBufferManager?.PurgeAllSegments(); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

        try { cleanupBufferManager?.Dispose(); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_BUFFER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

        Clear();
    }
}
