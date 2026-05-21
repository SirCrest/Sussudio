using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackBufferCyclePlaybackState(
    TimeSpan? InPoint,
    TimeSpan? OutPoint,
    TimeSpan? InPointFilePts,
    TimeSpan? OutPointFilePts);

internal sealed partial class FlashbackBackendResources
{
    private FlashbackBufferCyclePlaybackState DisposePlaybackForBufferCycle(bool preserveSegments)
    {
        var oldPlaybackController = TakePlaybackController();
        var preservedPlaybackState = new FlashbackBufferCyclePlaybackState(
            preserveSegments ? oldPlaybackController?.InPoint : null,
            preserveSegments ? oldPlaybackController?.OutPoint : null,
            preserveSegments ? oldPlaybackController?.InPointFilePts : null,
            preserveSegments ? oldPlaybackController?.OutPointFilePts : null);

        if (oldPlaybackController != null)
        {
            try
            {
                oldPlaybackController.GoLive();
                oldPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        return preservedPlaybackState;
    }

    private void DetachOldSinkProducersForBufferCycle(
        FlashbackBufferCycleRequest request,
        FlashbackEncoderSink oldSink)
    {
        DetachProducers(
            new FlashbackProducerDetachRequest(
                request.VideoCapture,
                request.AudioCapture,
                request.MicrophoneCapture,
                "FLASHBACK_CYCLE_DETACH_WARN",
                DetachMicrophoneWriter: true));
        oldSink.FrameEncoded -= request.FrameEncodedHandler;
    }

    private static async Task StopAndDisposeOldSinkForBufferCycleAsync(
        FlashbackEncoderSink oldSink,
        CancellationToken requestCancellationToken,
        CancellationToken committedCycleToken)
    {
        try
        {
            await oldSink.StopAsync(committedCycleToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (requestCancellationToken.IsCancellationRequested)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED type={ex.GetType().Name} msg={ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        try
        {
            await oldSink.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private async Task<bool> TryStartReplacementSinkForBufferCycleAsync(
        FlashbackBufferManager bufferManager,
        FlashbackBufferCycleRequest request,
        FlashbackBufferCyclePlaybackState preservedPlaybackState,
        CancellationToken committedCycleToken)
    {
        var newSink = new FlashbackEncoderSink(bufferManager);
        newSink.SetFatalErrorCallback(request.FatalErrorCallback);
        try
        {
            // When preserving DVR history (no purge), continue PTS from where
            // the old sink left off so new segments don't overlap existing ones.
            var ptsOffset = request.PurgeSegments ? TimeSpan.Zero : bufferManager.LatestPts;
            await newSink.StartAsync(
                request.CreateSessionContext(),
                committedCycleToken,
                ptsBaseOffset: ptsOffset).ConfigureAwait(false);

            newSink.FrameEncoded += request.FrameEncodedHandler;
            Sink = newSink;
            SettingsSnapshot = request.SettingsSnapshot;
            request.ClearLastFailure();
            AttachProducers(
                new FlashbackProducerAttachRequest(
                    request.VideoCapture,
                    request.AudioCapture,
                    request.MicrophoneCapture,
                    "buffer_cycle"));

            var playbackController = new FlashbackPlaybackController(bufferManager)
            {
                GpuDecodeEnabled = request.Settings.FlashbackGpuDecode
            };
            playbackController.RestoreInOutPoints(
                preservedPlaybackState.InPoint,
                preservedPlaybackState.OutPoint,
                preservedPlaybackState.InPointFilePts,
                preservedPlaybackState.OutPointFilePts);
            if (request.PreviewFrameSink != null)
            {
                playbackController.Initialize(
                    request.PreviewFrameSink,
                    request.VideoCapture,
                    request.AudioPlayback,
                    request.AudioCapture);
            }

            PlaybackController = playbackController;

            Logger.Log($"FLASHBACK_BUFFER_CYCLE_OK mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}' - falling back to full teardown");
            await CleanupFailedReplacementSinkForBufferCycleAsync(newSink, request).ConfigureAwait(false);
            ClearSinkAndSettings();
            return false;
        }
    }

    private static async Task CleanupFailedReplacementSinkForBufferCycleAsync(
        FlashbackEncoderSink newSink,
        FlashbackBufferCycleRequest request)
    {
        try { newSink.FrameEncoded -= request.FrameEncodedHandler; }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { request.VideoCapture.SetFlashbackSink(null); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { request.AudioCapture?.DetachFlashbackSink(); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_AUDIO_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { request.MicrophoneCapture?.SetAudioWriter(null); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_MIC_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { await newSink.DisposeAsync().ConfigureAwait(false); }
        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
    }
}
