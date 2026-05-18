using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(
        FinalizeResult result,
        string fallbackOutputPath,
        CancellationToken cancellationToken)
    {
        OperationCanceledException? cancellationException = null;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var recordingFramesDeliveredToBoundary = 0L;
        var recordingFramesAcceptedByBoundary = 0L;
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video recording stop failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video recording stop failed: {ex.Message}");
                }
            }
            finally
            {
                // Keep SkipCpuReadback=true - preview uses GPU textures, not CPU bytes.
                // Lock2D is never needed while D3D shared device is active.
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            recordingFramesDeliveredToBoundary = unifiedVideoCapture.RecordingFramesDelivered;
            recordingFramesAcceptedByBoundary = unifiedVideoCapture.VideoFramesWrittenToSink;
            Logger.Log(
                "VIDEO_DIAG mf_source_reader " +
                $"frames_delivered={_lastMfSourceReaderFramesDelivered} " +
                $"frames_dropped={_lastMfSourceReaderFramesDropped} " +
                $"negotiated_format='{_lastMfSourceReaderNegotiatedFormat ?? "unknown"}'");
            Logger.Log(
                "VIDEO_DIAG recording_pipeline " +
                $"source_frames_during_recording={recordingFramesDeliveredToBoundary} " +
                $"frames_enqueued_to_encoder={recordingFramesAcceptedByBoundary} " +
                $"pipeline_drops={recordingFramesDeliveredToBoundary - recordingFramesAcceptedByBoundary}");
        }

        return new LibAvVideoBoundaryStopResult(
            result,
            cancellationException,
            recordingFramesDeliveredToBoundary,
            recordingFramesAcceptedByBoundary);
    }

    private async Task DetachLibAvRecordingAudioBeforeSinkStopAsync()
    {
        if (_wasapiAudioCapture != null)
        {
            try
            {
                _wasapiAudioCapture.DetachRecordingSink();
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio recording sink detach failed: {ex.Message}");
            }
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
    }

    private async Task<LibAvFinalizeStepResult> StopAndDisposeLibAvSinkForFinalizeAsync(
        IRecordingSink? sink,
        LibAvRecordingSink? libAvSink,
        FinalizeResult result,
        string fallbackOutputPath,
        bool emergency,
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        if (sink == null)
        {
            return new LibAvFinalizeStepResult(result, cancellationException);
        }

        try
        {
            // Use the typed LibAvRecordingSink reference (when available) so the
            // emergency flag can select EmergencyStopTimeoutMs (5s) vs the public
            // StopAsync's 30s budget. The plain IRecordingSink overload is the
            // fallback for non-LibAv sinks (unused in practice but kept for safety).
            var sinkResult = libAvSink != null
                ? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)
                : await sink.StopAsync(cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                result = sinkResult;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException = new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"Recording sink stop failed: {ex.Message}");
            if (result.Succeeded)
            {
                result = FinalizeResult.Failure(fallbackOutputPath, $"Recording stop failed: {ex.Message}");
            }
        }
        finally
        {
            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
                if (libAvSink != null)
                {
                    var libAvDrainTask = libAvSink.EncodingCompletionTask;
                    if (!libAvDrainTask.IsCompleted)
                    {
                        _pendingLibAvDrainTask = libAvDrainTask;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording sink dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording dispose failed: {ex.Message}");
                }
            }
        }

        return new LibAvFinalizeStepResult(result, cancellationException);
    }

    private async Task<LibAvFinalizeStepResult> DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(
        FinalizeResult result,
        string fallbackOutputPath,
        OperationCanceledException? cancellationException)
    {
        if (_isVideoPreviewActive)
        {
            return new LibAvFinalizeStepResult(result, cancellationException);
        }

        var unifiedVideoCapture = _videoPipeline.TakeCapture();
        if (unifiedVideoCapture != null)
        {
            try
            {
                CacheMjpegTimingMetrics(unifiedVideoCapture);
                DetachUnifiedVideoCapture(unifiedVideoCapture);
                if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
                {
                    _pendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        pendingLibAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_stop_deferred_drain");
                }
                else
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video capture dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video capture dispose failed: {ex.Message}");
                }
            }
        }

        var capture = _wasapiAudioCapture;
        _wasapiAudioCapture = null;
        _previewAudioGraph.DetachCapture(
            capture,
            OnWasapiAudioLevelUpdated,
            OnWasapiCaptureFailed,
            _flashbackPlaybackController);
        if (capture != null)
        {
            try
            {
                await capture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording WASAPI capture dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording WASAPI capture dispose failed: {ex.Message}");
                }
            }
        }

        return new LibAvFinalizeStepResult(result, cancellationException);
    }

    private readonly record struct LibAvFinalizeStepResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException);

    private readonly record struct LibAvVideoBoundaryStopResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException,
        long RecordingFramesDeliveredToBoundary,
        long RecordingFramesAcceptedByBoundary);
}
