using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)
    {
        return StopCoreAsync(cancellationToken);
    }

    private async Task<FinalizeResult> StopCoreAsync(CancellationToken cancellationToken)
    {
        var outputPath = _recordingOutputPath ?? _tsFilePath ?? string.Empty;
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

        if (_encodingTask != null)
        {
            var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(StopTimeoutMs, cancellationToken)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _encodingTask))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var timeoutFailure = new TimeoutException("Flashback encode drain timed out while stopping.");
                lock (_sync)
                {
                    _encodingFailure ??= timeoutFailure;
                }
                CancelEncodingCts("stop_timeout");
                CompletePendingForceRotateWithEmptyResult();
                Logger.Log("FLASHBACK_SINK_STOP_DRAIN_TIMEOUT");
                return FinalizeResult.Failure(outputPath, "Stopped (flashback encode drain timed out)");
            }

            try
            {
                await _encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure ??= ex;
            }
        }

        if (_encodingFailure != null)
        {
            Logger.Log($"FLASHBACK_SINK_STOP_FAIL type={_encodingFailure.GetType().Name} msg={_encodingFailure.Message}");
            return FinalizeResult.Failure(outputPath, $"Stopped (flashback encode failed: {_encodingFailure.Message})");
        }

        Logger.Log(
            $"FLASHBACK_SINK_STOP output='{outputPath}' frames={EncodedVideoFrames} dropped={DroppedVideoFrames} " +
            $"audio_samples={AudioSamplesReceived}");
        return FinalizeResult.Success(outputPath, "Stopped");
    }
}
