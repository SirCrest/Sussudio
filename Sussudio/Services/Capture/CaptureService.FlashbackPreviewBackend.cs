using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task EnsureFlashbackPreviewBackendAsync(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_flashbackEnabled || _flashbackSink != null)
            return;

        // Cache AV1 NVENC availability on first flashback init (async-safe here)
        if (!_hasAv1Nvenc)
        {
            try
            {
                var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync().ConfigureAwait(false);
                _hasAv1Nvenc = support.HasAv1Nvenc;
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_ENCODER_SUPPORT_PROBE_WARN type={ex.GetType().Name} msg={ex.Message}");
                // Assume unavailable — will fall back to HEVC.
            }
        }

        var bufferMinutes = settings.FlashbackBufferMinutes > 0 ? settings.FlashbackBufferMinutes : 5;
        var bufferDuration = TimeSpan.FromMinutes(bufferMinutes);
        // Segment duration must be shorter than buffer duration so completed segments
        // can be evicted. Use half the buffer, clamped to [0.5, 5] minutes.
        // - Lower bound 0.5min: for 1-min buffer, ensures at least 1 completed segment
        //   exists before the buffer fills (2 segments × 0.5min = 1min).
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
        flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
        var flashbackExporter = new FlashbackExporter();
        FlashbackPlaybackController? playbackController = null;

        try
        {
            // Wait until both video and audio are confirmed flowing before starting
            // the encoder. This eliminates the startup transient where audio PTS races
            // ahead of video PTS (~840ms) because WASAPI starts before the source reader.
            var deadline = Environment.TickCount64 + 5000;
            while (Environment.TickCount64 < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var videoReady = unifiedVideoCapture.VideoFramesArrived > 0;
                var audioReady = _wasapiAudioCapture == null || _wasapiAudioCapture.CaptureCallbackCount > 0;
                if (videoReady && audioReady)
                    break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            Logger.Log(
                $"FLASHBACK_PREVIEW_READINESS video_frames={unifiedVideoCapture.VideoFramesArrived} " +
                $"audio_callbacks={_wasapiAudioCapture?.CaptureCallbackCount ?? -1}");

            await flashbackSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, settings),
                cancellationToken).ConfigureAwait(false);
            flashbackSink.FrameEncoded += OnFlashbackFrameEncoded;
            _flashbackBackend.Install(
                bufferManager,
                flashbackSink,
                flashbackExporter,
                playbackController: null,
                settingsSnapshot: null);
            _flashbackBackend.AttachProducers(
                new FlashbackProducerAttachRequest(
                    unifiedVideoCapture,
                    _wasapiAudioCapture,
                    _microphoneCapture,
                    "preview_backend_start"));

            // Create playback controller for timeline scrubbing/playback
            playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = settings.FlashbackGpuDecode;
            if (_previewFrameSink != null && unifiedVideoCapture != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;
            _flashbackBackendSettings = CloneCaptureSettings(settings);
            _flashbackBackend.ClearRecoveryPreserve();
            ClearLastFlashbackFailure();

            Logger.Log($"FLASHBACK_PREVIEW_INIT_OK session='{bufferManager.SessionId}' controller_initialized={playbackController.IsInitialized}");
        }
        catch (Exception ex)
        {
            var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "FLASHBACK_PREVIEW_INIT_CANCELLED"
                : "FLASHBACK_PREVIEW_INIT_FAIL";
            Logger.Log($"{failureToken} type={ex.GetType().Name} error='{ex.Message}'");
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            _flashbackBackend.DetachProducers(
                new FlashbackProducerDetachRequest(
                    unifiedVideoCapture,
                    _wasapiAudioCapture,
                    _microphoneCapture,
                    "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN",
                    DetachMicrophoneWriter: true));
            try { (playbackController ?? _flashbackPlaybackController)?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            try { await flashbackSink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_SINK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            var sinkCompletionTask = flashbackSink.EncodingCompletionTask;
            if (!sinkCompletionTask.IsCompleted)
            {
                ScheduleDeferredFlashbackBackendCleanup(
                    sinkCompletionTask,
                    new FlashbackBackendArtifactCleanupRequest(
                        bufferManager,
                        flashbackExporter,
                        "preview_init_rollback",
                        PurgeSegments: true));
                bufferManager = null;
                flashbackExporter = null;
            }

            try { flashbackExporter?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_EXPORTER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.PurgeAllSegments(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_BUFFER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            _flashbackBackend.Clear();

            throw;
        }
    }
}
