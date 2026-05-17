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
