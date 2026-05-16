using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    // Keeps the 30s StopTimeoutMs drain budget so saturated 4K 60fps queues can drain
    // cleanly without triggering the fix #11 emergency-flush fallback.
    public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
        => StopCoreAsync(emergency: false, cancellationToken);

    // Internal overload used by the emergency-stop path (CaptureService.StopRecordingAsync
    // when called from CaptureSessionCoordinator.StopRecordingForEmergencyAsync).
    // Uses EmergencyStopTimeoutMs (5s) so the encode-drain fits inside App.xaml.cs's 8s
    // emergency-stop wrapper (fix #12).
    internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)
        => StopCoreAsync(emergency, cancellationToken);

    private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)
    {
        var context = _context;
        var outputPath = context?.FinalOutputPath ?? OutputPath;

        if (_disposed)
        {
            return FinalizeResult.Success(outputPath, "Stopped");
        }

        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CompleteWriter(_cudaQueue);

        if (_encodingTask != null)
        {
            var drainTimeoutMs = emergency ? EmergencyStopTimeoutMs : StopTimeoutMs;
            var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(drainTimeoutMs, cancellationToken)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _encodingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Cancel the encoding loop so it stops processing new frames and
                // exits via OperationCanceledException - this must happen before
                // FlushAndClose so the two don't race on _encoder state.
                _cts?.Cancel();

                // Give the encoding task a brief window to unblock from its
                // cancellation-token wait and exit cleanly. DisposeTimeoutMs (1 s)
                // is sufficient when the encoding loop is parked at its token-aware
                // wait site (_workAvailable.Wait), but the loop does NOT poll
                // cancellation inside the inner drain loops. If it's wedged in a
                // native libav call (avcodec_send_frame, av_interleaved_write_frame),
                // the grace expires while the loop is still touching _encoder /
                // _videoCodecCtx / _formatCtx. Flushing concurrently in that case
                // races on unsynchronized native state and can corrupt the file or
                // raise an SEH access violation that managed `catch` cannot intercept.
                // Gate the flush on _encodingTask having actually completed; if it
                // hasn't, skip the flush and accept a cleanly-truncated output.
                var graceResult = await Task.WhenAny(_encodingTask, Task.Delay(DisposeTimeoutMs)).ConfigureAwait(false);
                if (ReferenceEquals(graceResult, _encodingTask))
                {
                    // Encoder loop has exited - safe to flush. Wrap in try/catch
                    // since FlushAndClose can itself throw if the encoder is in a
                    // broken state; in that case the file is still truncated but
                    // the error has been surfaced to the caller via Failure below.
                    try
                    {
                        _encoder.FlushAndClose();
                    }
                    catch (Exception flushEx)
                    {
                        Logger.Log($"LIBAV_SINK_STOP_DRAIN_FLUSH_FAIL type={flushEx.GetType().Name} msg={flushEx.Message}");
                    }
                }
                else
                {
                    Logger.Log("LIBAV_SINK_STOP_DRAIN_FLUSH_SKIPPED reason=encoder_task_still_running");
                }

                return FinalizeResult.Failure(outputPath, "Stopped (libav encode drain timed out; emergency flush attempted)");
            }

            try
            {
                await _encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure = ex;
            }
        }
        else
        {
            _encoder.FlushAndClose();
        }

        if (_encodingFailure != null)
        {
            Logger.Log($"LIBAV_SINK_STOP_FAIL type={_encodingFailure.GetType().Name} msg={_encodingFailure.Message}");
            return FinalizeResult.Failure(outputPath, $"Stopped (libav encode failed: {_encodingFailure.Message})");
        }

        if (!TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure))
        {
            Logger.Log($"LIBAV_SINK_STOP_OUTPUT_INVALID output='{outputPath}' reason='{outputFailure}'");
            return FinalizeResult.Failure(outputPath, $"Stopped (output file invalid: {outputFailure})");
        }

        if (context?.HdrPipelineActive == true)
        {
            var (validationSucceeded, validationDetail) = await HdrValidationRunner
                .RunAsync(context, outputPath, cancellationToken)
                .ConfigureAwait(false);

            if (!validationSucceeded)
            {
                if (validationDetail.Contains("validator-script-missing", StringComparison.Ordinal))
                {
                    Logger.Log($"HDR validation skipped (script not found): {validationDetail}");
                }
                else
                {
                    return FinalizeResult.Failure(
                        outputPath,
                        $"Stopped (hdr validation failed: {validationDetail})",
                        new[] { outputPath });
                }
            }
        }

        Logger.Log(
            $"LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes} frames={EncodedVideoFrames} dropped={DroppedVideoFrames} audio_samples={AudioSamplesReceived} mic_samples={MicrophoneSamplesReceived}");
        return FinalizeResult.Success(outputPath, "Stopped");
    }
}
