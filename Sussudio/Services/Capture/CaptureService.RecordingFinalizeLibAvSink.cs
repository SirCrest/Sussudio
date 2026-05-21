using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task DetachLibAvRecordingAudioBeforeSinkStopAsync()
    {
        if (_previewAudioGraph.ProgramCapture != null)
        {
            try
            {
                _previewAudioGraph.ProgramCapture.DetachRecordingSink();
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
                        _recordingBackend.PendingLibAvDrainTask = libAvDrainTask;
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

    private readonly record struct LibAvFinalizeStepResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException);
}
