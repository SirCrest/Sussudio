using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)
        => StopVideoPreviewCoreAsync(teardownPipeline: false, cancellationToken);

    public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopVideoPreviewCoreAsync(teardownPipeline: true, cancellationToken);

    private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();

            var commitStoppedState = false;
            Exception? stopFailure = null;
            try
            {
                // Invariant: preview lifecycle must not affect the recording/flashback pipeline.
                // Keep the capture + flashback backend alive across preview toggles unless the
                // caller explicitly requests a full teardown (reinit, shutdown, settings change).
                var keepPipelineAlive = !teardownPipeline &&
                    (_isRecording || (_flashbackEnabled && _flashbackBackend.Sink != null));

                if (keepPipelineAlive)
                {
                    Logger.Log($"PREVIEW_STOP keep_pipeline_alive=1 recording={_isRecording} flashback_alive={_flashbackBackend.Sink != null}");
                    _videoPipeline.Capture?.SetPreviewSink(null);
                }
                else
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }

                commitStoppedState = true;
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopFailure = ex;
                commitStoppedState = true;
                throw;
            }
            finally
            {
                if (commitStoppedState)
                {
                    _isVideoPreviewActive = false;
                    if (!_isRecording)
                    {
                        try
                        {
                            await StopTelemetryPollAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex) when (stopFailure != null)
                        {
                            Logger.Log($"PREVIEW_STOP_TELEMETRY_WARN type={ex.GetType().Name} msg='{ex.Message}'");
                        }
                    }
                }
            }

            StatusChanged?.Invoke(this, "Preview stopped");
        }, cancellationToken);

    private async Task DisposePreviewPipelineAsync(
        CancellationToken transitionToken,
        bool purgeFlashbackSegments)
    {
        _recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();

        var unifiedVideoCapture = _videoPipeline.TakeCapture();
        var videoCaptureCleanupDeferred = false;
        if (unifiedVideoCapture != null)
        {
            CacheMjpegTimingMetrics(unifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            DetachUnifiedVideoCapture(unifiedVideoCapture);
            try
            {
                unifiedVideoCapture.SetPreviewSink(null);
                unifiedVideoCapture.SetFlashbackSink(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"PREVIEW_PIPELINE_VIDEO_DETACH_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }

            if (_recordingBackend.PendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
            {
                _recordingBackend.PendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                    pendingLibAvDrainTask,
                    unifiedVideoCapture,
                    reason: "dispose_preview_pipeline_after_deferred_recording");
                videoCaptureCleanupDeferred = true;
            }
            else
            {
                Logger.Log("PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE");
                await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
            }
        }

        await DisposeFlashbackPreviewBackendAsync(
                transitionToken,
                purgeSegments: _flashbackBackend.ResolveSegmentPurge(
                    purgeFlashbackSegments,
                    "preview_pipeline_dispose"))
            .ConfigureAwait(false);

        if (unifiedVideoCapture != null && !videoCaptureCleanupDeferred)
        {
            await unifiedVideoCapture.DisposeForPreviewReinitAsync().ConfigureAwait(false);
        }

        var capture = _previewAudioGraph.ProgramCapture;
        _previewAudioGraph.ProgramCapture = null;
        _previewAudioGraph.DetachCapture(
            capture,
            OnWasapiAudioLevelUpdated,
            OnWasapiCaptureFailed,
            _flashbackBackend.PlaybackController);
        if (capture != null)
        {
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
    }
}
