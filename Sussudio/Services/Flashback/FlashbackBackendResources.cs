using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackProducerDetachRequest(
    UnifiedVideoCapture? VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    string WarningToken,
    bool DetachMicrophoneWriter);

internal readonly record struct FlashbackProducerAttachRequest(
    UnifiedVideoCapture VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    string Reason);

internal readonly record struct FlashbackPreviewBackendStartRequest(
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
    Action<Task, FlashbackBufferManager?, FlashbackExporter?, string, bool> ScheduleDeferredCleanup,
    CancellationToken CancellationToken);

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

/// <summary>
/// Authoritative ownership record for the preview-owned Flashback backend.
/// CaptureService remains the transition coordinator; this aggregate keeps the
/// sink, buffer, exporter, playback controller, and settings snapshot together.
/// </summary>
internal sealed partial class FlashbackBackendResources
{
    public FlashbackBufferManager? BufferManager { get; set; }

    public FlashbackEncoderSink? Sink { get; set; }

    public FlashbackExporter? Exporter { get; set; }

    public FlashbackPlaybackController? PlaybackController { get; set; }

    public CaptureSettings? SettingsSnapshot { get; set; }

    public bool PreserveSegmentsAfterFailedRecordingFinalize { get; private set; }

    public bool HasAnyResource =>
        BufferManager != null ||
        Sink != null ||
        Exporter != null ||
        PlaybackController != null;

    public void Install(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink sink,
        FlashbackExporter exporter,
        FlashbackPlaybackController? playbackController,
        CaptureSettings? settingsSnapshot)
    {
        BufferManager = bufferManager;
        Sink = sink;
        Exporter = exporter;
        PlaybackController = playbackController;
        SettingsSnapshot = settingsSnapshot;
    }

    public void ClearRecoveryPreserve()
    {
        PreserveSegmentsAfterFailedRecordingFinalize = false;
    }

    public bool ResolveSegmentPurge(bool requested, string reason)
    {
        if (!requested)
        {
            return false;
        }

        if (!PreserveSegmentsAfterFailedRecordingFinalize)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_SEGMENT_PURGE_BLOCKED reason={reason}");
        return false;
    }

    public void PreserveRecoverySegments(string reason)
    {
        PreserveSegmentsAfterFailedRecordingFinalize = true;
        Logger.Log($"FLASHBACK_RECOVERY_PRESERVE reason={reason}");
        BufferManager?.MarkSessionPreservedForRecovery();
    }

    public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(
        FlashbackPreviewBackendStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.VideoCapture);
        ArgumentNullException.ThrowIfNull(request.Settings);
        ArgumentNullException.ThrowIfNull(request.CreateSessionContext);
        ArgumentNullException.ThrowIfNull(request.FatalErrorCallback);
        ArgumentNullException.ThrowIfNull(request.FrameEncodedHandler);
        ArgumentNullException.ThrowIfNull(request.ScheduleDeferredCleanup);

        var bufferMinutes = request.Settings.FlashbackBufferMinutes > 0
            ? request.Settings.FlashbackBufferMinutes
            : 5;
        var bufferDuration = TimeSpan.FromMinutes(bufferMinutes);
        // Segment duration must be shorter than buffer duration so completed segments
        // can be evicted. Use half the buffer, clamped to [0.5, 5] minutes.
        // - Lower bound 0.5min: for 1-min buffer, ensures at least 1 completed segment
        //   exists before the buffer fills (2 segments x 0.5min = 1min).
        // - Upper bound 5min: for large buffers (15-30min), keeps eviction granular
        //   so users don't lose 15min of history in one eviction step.
        var segmentDuration = TimeSpan.FromMinutes(Math.Clamp(bufferMinutes / 2.0, 0.5, 5.0));
        var bufferManager = new FlashbackBufferManager(new FlashbackBufferOptions
        {
            BufferDuration = bufferDuration,
            SegmentDuration = segmentDuration
        });
        bufferManager.Initialize(Guid.NewGuid().ToString("N"));
        var flashbackSink = new FlashbackEncoderSink(bufferManager);
        flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback);
        var flashbackExporter = new FlashbackExporter();
        FlashbackPlaybackController? playbackController = null;

        try
        {
            await flashbackSink.StartAsync(
                request.CreateSessionContext(),
                request.CancellationToken).ConfigureAwait(false);
            flashbackSink.FrameEncoded += request.FrameEncodedHandler;

            playbackController = new FlashbackPlaybackController(bufferManager)
            {
                GpuDecodeEnabled = request.Settings.FlashbackGpuDecode
            };

            Install(
                bufferManager,
                flashbackSink,
                flashbackExporter,
                playbackController,
                request.SettingsSnapshot);
            AttachProducers(
                new FlashbackProducerAttachRequest(
                    request.VideoCapture,
                    request.AudioCapture,
                    request.MicrophoneCapture,
                    "preview_backend_start"));

            if (request.PreviewFrameSink != null)
            {
                playbackController.Initialize(
                    request.PreviewFrameSink,
                    request.VideoCapture,
                    request.AudioPlayback,
                    request.AudioCapture);
            }

            ClearRecoveryPreserve();
            Logger.Log($"FLASHBACK_PREVIEW_INIT_OK session='{bufferManager.SessionId}' controller_initialized={playbackController.IsInitialized}");
            return playbackController;
        }
        catch
        {
            await RollBackPreviewBackendStartAsync(
                    request,
                    bufferManager,
                    flashbackSink,
                    flashbackExporter,
                    playbackController)
                .ConfigureAwait(false);
            throw;
        }
    }

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

        // Close playback before cycling the sink so active decoders release segment files.
        var oldPlaybackController = TakePlaybackController();
        var preservedInPoint = !request.PurgeSegments ? oldPlaybackController?.InPoint : null;
        var preservedOutPoint = !request.PurgeSegments ? oldPlaybackController?.OutPoint : null;
        var preservedInPointFilePts = !request.PurgeSegments ? oldPlaybackController?.InPointFilePts : null;
        var preservedOutPointFilePts = !request.PurgeSegments ? oldPlaybackController?.OutPointFilePts : null;
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

        DetachProducers(
            new FlashbackProducerDetachRequest(
                request.VideoCapture,
                request.AudioCapture,
                request.MicrophoneCapture,
                "FLASHBACK_CYCLE_DETACH_WARN",
                DetachMicrophoneWriter: true));
        oldSink.FrameEncoded -= request.FrameEncodedHandler;
        var committedCycleToken = CancellationToken.None;

        // Stop and dispose the old sink (leaves buffer manager and segments intact).
        try
        {
            await oldSink.StopAsync(committedCycleToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (request.CancellationToken.IsCancellationRequested)
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

        // Create and start a new encoder sink on the same buffer manager.
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
                preservedInPoint,
                preservedOutPoint,
                preservedInPointFilePts,
                preservedOutPointFilePts);
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
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}' - falling back to full teardown");
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
            ClearSinkAndSettings();
            return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.FallbackFullRebuild);
        }

        if (request.CancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        return new FlashbackBufferCycleResult(FlashbackBufferCycleOutcome.SinkOnly);
    }

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

    public async Task<FinalizeResult> FinalizeRecordingAsync(
        string outputPath,
        Action<FlashbackEncoderSink>? captureBoundarySnapshot,
        Func<TimeSpan, TimeSpan, string, CancellationToken, Task<FinalizeResult>> exportRecordingAsync,
        Action<FlashbackBufferManager?, string> resumeEvictionBestEffort,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exportRecordingAsync);
        ArgumentNullException.ThrowIfNull(resumeEvictionBestEffort);

        var flashbackSink = Sink
            ?? throw new InvalidOperationException("Flashback recording backend is not active.");
        var bufferManager = BufferManager;
        var outerPauseApplied = false;
        try
        {
            bufferManager?.PauseEviction();
            outerPauseApplied = bufferManager != null;

            var endResult = await flashbackSink.EndRecordingAsync(cancellationToken).ConfigureAwait(false);
            captureBoundarySnapshot?.Invoke(flashbackSink);
            if (!endResult.Succeeded)
            {
                return endResult;
            }

            var startPts = flashbackSink.LastRecordingStartPts;
            var endPts = flashbackSink.LastRecordingEndPts;
            var exportResult = await exportRecordingAsync(startPts, endPts, outputPath, cancellationToken)
                .ConfigureAwait(false);

            exportResult = PreserveEndArtifactsOnFailure(exportResult, endResult);
            if (exportResult.Succeeded)
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_OK output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_FAIL output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }

            return exportResult;
        }
        finally
        {
            if (outerPauseApplied)
            {
                resumeEvictionBestEffort(bufferManager, "flashback_recording_finalize");
            }
        }
    }

    private static FinalizeResult PreserveEndArtifactsOnFailure(
        FinalizeResult exportResult,
        FinalizeResult endResult)
    {
        if (exportResult.Succeeded || endResult.PreservedArtifacts.Count == 0)
        {
            return exportResult;
        }

        return FinalizeResult.Failure(
            exportResult.OutputPath,
            exportResult.StatusMessage,
            exportResult.PreservedArtifacts.Concat(endResult.PreservedArtifacts));
    }

    public FlashbackPlaybackController? TakePlaybackController()
    {
        var playbackController = PlaybackController;
        PlaybackController = null;
        return playbackController;
    }

    public void AttachProducers(FlashbackProducerAttachRequest request)
    {
        var flashbackSink = Sink;
        if (flashbackSink == null)
        {
            return;
        }

        request.VideoCapture.SetFlashbackSink(flashbackSink);
        AttachAudioProducer(request.AudioCapture, flashbackSink, request.Reason);
        AttachMicrophoneProducer(request.MicrophoneCapture, flashbackSink, request.Reason);
    }

    public void DetachProducers(FlashbackProducerDetachRequest request)
    {
        if (request.DetachMicrophoneWriter)
        {
            try { request.MicrophoneCapture?.SetAudioWriter(null); }
            catch (Exception ex) { Logger.Log($"{request.WarningToken} target=microphone type={ex.GetType().Name} msg={ex.Message}"); }
        }

        try { request.AudioCapture?.DetachFlashbackSink(); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=audio type={ex.GetType().Name} msg={ex.Message}"); }

        try { request.VideoCapture?.SetFlashbackSink(null); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=video type={ex.GetType().Name} msg={ex.Message}"); }
    }

    private static void AttachAudioProducer(
        WasapiAudioCapture? audioCapture,
        FlashbackEncoderSink flashbackSink,
        string reason)
    {
        if (audioCapture == null)
        {
            return;
        }

        if (!flashbackSink.AudioEnabled)
        {
            Logger.Log($"FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
            return;
        }

        audioCapture.AttachFlashbackSink(flashbackSink);
        Logger.Log($"FLASHBACK_AUDIO_ATTACH_OK reason='{reason}'");
    }

    private static void AttachMicrophoneProducer(
        WasapiAudioCapture? microphoneCapture,
        FlashbackEncoderSink flashbackSink,
        string reason)
    {
        if (microphoneCapture == null || !flashbackSink.MicrophoneEnabled)
        {
            return;
        }

        microphoneCapture.SetAudioWriter(samples => flashbackSink.WriteMicrophoneAudioAsync(samples));
        Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
    }

    public void ClearSinkAndSettings()
    {
        Sink = null;
        SettingsSnapshot = null;
    }

    public void Clear()
    {
        BufferManager = null;
        Sink = null;
        Exporter = null;
        PlaybackController = null;
        SettingsSnapshot = null;
    }
}
