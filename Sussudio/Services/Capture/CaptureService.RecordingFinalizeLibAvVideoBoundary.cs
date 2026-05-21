using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(
        FinalizeResult result,
        string fallbackOutputPath,
        CancellationToken cancellationToken)
    {
        OperationCanceledException? cancellationException = null;
        var unifiedVideoCapture = _videoPipeline.Capture;
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

    private readonly record struct LibAvVideoBoundaryStopResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException,
        long RecordingFramesDeliveredToBoundary,
        long RecordingFramesAcceptedByBoundary);
}
