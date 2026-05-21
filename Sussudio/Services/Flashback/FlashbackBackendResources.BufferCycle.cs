using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackBufferCycleRequest(
    UnifiedVideoCapture VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    WasapiAudioPlayback? AudioPlayback,
    IPreviewFrameSink? PreviewFrameSink,
    CaptureSettings Settings,
    CaptureSettings SettingsSnapshot,
    Func<FlashbackSessionContext> CreateSessionContext,
    Action<Exception> FatalErrorCallback,
    EventHandler<long> FrameEncodedHandler,
    Action ClearLastFailure,
    Action<Task, FlashbackBackendArtifactCleanupRequest> ScheduleDeferredCleanup,
    bool PurgeSegments,
    CancellationToken CancellationToken);

internal enum FlashbackBufferCycleOutcome
{
    SinkOnly,
    DeferredFullRebuild,
    PurgeFallbackRebuild,
    FallbackFullRebuild
}

internal readonly record struct FlashbackBufferCycleResult(FlashbackBufferCycleOutcome Outcome);

internal sealed partial class FlashbackBackendResources
{
    public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(
        FlashbackBufferCycleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.VideoCapture);
        ArgumentNullException.ThrowIfNull(request.Settings);
        ArgumentNullException.ThrowIfNull(request.CreateSessionContext);
        ArgumentNullException.ThrowIfNull(request.FatalErrorCallback);
        ArgumentNullException.ThrowIfNull(request.FrameEncodedHandler);
        ArgumentNullException.ThrowIfNull(request.ClearLastFailure);
        ArgumentNullException.ThrowIfNull(request.ScheduleDeferredCleanup);

        var bufferManager = BufferManager
            ?? throw new InvalidOperationException("Flashback buffer manager is not active.");
        var oldSink = Sink
            ?? throw new InvalidOperationException("Flashback encoder sink is not active.");

        var preservedPlaybackState = DisposePlaybackForBufferCycle(
            preserveSegments: !request.PurgeSegments);
        DetachOldSinkProducersForBufferCycle(request, oldSink);
        var committedCycleToken = CancellationToken.None;

        await StopAndDisposeOldSinkForBufferCycleAsync(
            oldSink,
            request.CancellationToken,
            committedCycleToken).ConfigureAwait(false);

        // From this point on the old sink is no longer a usable backend. Keep
        // cancellation deferred until a replacement is attached or teardown is complete.
        ClearSinkAndSettings();

        var oldSinkCompletionTask = oldSink.EncodingCompletionTask;
        if (!oldSinkCompletionTask.IsCompleted)
        {
            Logger.Log("FLASHBACK_CYCLE_DISPOSE_DEFERRED - falling back to full teardown");
            var oldExporter = Exporter;

            Clear();

            request.ScheduleDeferredCleanup(
                oldSinkCompletionTask,
                new FlashbackBackendArtifactCleanupRequest(
                    bufferManager,
                    oldExporter,
                    "buffer_cycle_deferred_cleanup",
                    request.PurgeSegments));

            return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.DeferredFullRebuild);
        }

        // When the codec/format changed, purge stale segments (incompatible with
        // new encoder) and reset PTS so the new encoder starts fresh from 0.
        // After stop-recording, keep everything - segments, PTS range, and
        // buffer state - so the user can immediately scrub/export DVR history.
        if (request.PurgeSegments)
        {
            bufferManager.ResetLatestPts();
            bufferManager.PurgeCompletedSegments();

            // If some segments couldn't be deleted (e.g., playback has files locked),
            // fall back to full teardown to avoid mixed-codec segments in the buffer.
            if (bufferManager.SegmentCount > 0)
            {
                Logger.Log($"FLASHBACK_CYCLE_PURGE_INCOMPLETE remaining={bufferManager.SegmentCount} - falling back to full teardown");
                return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.PurgeFallbackRebuild);
            }
        }

        // Ensure the new sink gets a fresh segment file (not the old sink's active path).
        bufferManager.FinalizeActiveSegmentForCycle();

        if (!await TryStartReplacementSinkForBufferCycleAsync(
            bufferManager,
            request,
            preservedPlaybackState,
            committedCycleToken).ConfigureAwait(false))
        {
            return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.FallbackFullRebuild);
        }

        if (request.CancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.SinkOnly);
    }
}
