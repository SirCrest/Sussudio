using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Standard LibAv recording stop/finalize path: stop capture fan-out, drain and
// dispose the recording sink, clean idle preview resources, and publish outcome.
public partial class CaptureService
{
    private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        var detachedBackend = _recordingBackend.DetachLibAvBackend();
        var sink = detachedBackend.Sink;
        var libAvSink = detachedBackend.LibAvSink;
        var recordingContext = detachedBackend.Context;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
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

        if (sink != null)
        {
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

        }

        var libAvFinalAudioCounters = libAvSink != null
            ? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, libAvSink, _activeRecordingSettings))
            : RecordingAudioIntegrityCounterSnapshot.Disabled;

        if (!_isVideoPreviewActive)
        {
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
                    if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
                    {
                        _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
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
        }

        var wasapiAudioCaptureFault = _previewAudioGraph.ConsumeCaptureFault();
        if (wasapiAudioCaptureFault.Faulted && cancellationException == null && result.Succeeded)
        {
            var statusMessage = string.IsNullOrWhiteSpace(wasapiAudioCaptureFault.Message)
                ? "Recording failed (WASAPI audio capture faulted)."
                : $"Recording failed (WASAPI audio capture faulted: {wasapiAudioCaptureFault.Message})";
            Logger.Log($"RECORDING_AUDIO_FAULT status='{statusMessage}'");
            result = FinalizeResult.Failure(result.OutputPath, statusMessage);
        }

        if (libAvSink != null)
        {
            CaptureEncoderRuntimeTelemetry(libAvSink);
            _lastRecordingIntegrity = BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: false,
                finalizeSucceeded: result.Succeeded,
                finalizeStatus: result.StatusMessage,
                completedUtc: DateTimeOffset.UtcNow,
                sourceFrames: recordingFramesDeliveredToBoundary,
                acceptedFrames: recordingFramesAcceptedByBoundary,
                counters: GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(libAvSink)),
                audioCounters: libAvFinalAudioCounters);
            _recordingIntegrityCounterBaseline = null;
            _recordingIntegrityAudioBaseline = null;
            LogRecordingIntegritySummary(_lastRecordingIntegrity);
        }

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingBackend.ClearContextAndSettings();
        _mfConvertersDisabled = false;

        cancellationException = await RestoreLibAvPreviewFeaturesAfterRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);

        PublishRecordingFinalizedOutcome(result, updateOutputPath: true);

        if (cancellationException != null)
        {
            throw cancellationException;
        }

        return result;
    }
}
