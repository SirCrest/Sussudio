using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
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
        if (!_flashbackEnabled || _flashbackBackend.Sink != null)
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
                // Assume unavailable - will fall back to HEVC.
            }
        }

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
                var audioReady = _previewAudioGraph.ProgramCapture == null || _previewAudioGraph.ProgramCapture.CaptureCallbackCount > 0;
                if (videoReady && audioReady)
                    break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            Logger.Log(
                $"FLASHBACK_PREVIEW_READINESS video_frames={unifiedVideoCapture.VideoFramesArrived} " +
                $"audio_callbacks={_previewAudioGraph.ProgramCapture?.CaptureCallbackCount ?? -1}");

            await _flashbackBackend.StartPreviewBackendAsync(
                    new FlashbackPreviewBackendStartRequest(
                        unifiedVideoCapture,
                        _previewAudioGraph.ProgramCapture,
                        _previewAudioGraph.MicrophoneCapture,
                        _previewAudioGraph.Playback,
                        _videoPipeline.PreviewFrameSink,
                        settings,
                        CloneCaptureSettings(settings),
                        () => CreateFlashbackSessionContext(unifiedVideoCapture, settings),
                        OnFlashbackBackendFatalError,
                        OnFlashbackFrameEncoded,
                        SchedulePreviewBackendStartDeferredCleanup,
                        cancellationToken))
                .ConfigureAwait(false);

            ClearLastFlashbackFailure();
        }
        catch (Exception ex)
        {
            var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "FLASHBACK_PREVIEW_INIT_CANCELLED"
                : "FLASHBACK_PREVIEW_INIT_FAIL";
            Logger.Log($"{failureToken} type={ex.GetType().Name} error='{ex.Message}'");

            throw;
        }
    }

    private void SchedulePreviewBackendStartDeferredCleanup(
        Task sinkCompletionTask,
        FlashbackBufferManager? bufferManager,
        FlashbackExporter? flashbackExporter,
        string reason,
        bool purgeSegments)
    {
        ScheduleDeferredFlashbackBackendCleanup(
            sinkCompletionTask,
            new FlashbackBackendArtifactCleanupRequest(
                bufferManager,
                flashbackExporter,
                reason,
                purgeSegments));
    }

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
