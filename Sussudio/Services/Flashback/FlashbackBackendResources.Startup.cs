using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

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

internal sealed partial class FlashbackBackendResources
{
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
}
