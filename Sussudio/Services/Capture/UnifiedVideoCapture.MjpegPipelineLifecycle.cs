using System;
using System.Threading;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
    private static bool ShouldUseExternalMjpegDecode(
        bool useMjpegHighFrameRateMode,
        bool requireP010,
        string? requestedPixelFormat)
    {
        // 4K120 MJPEG is compressed on the USB wire. In that mode the source
        // reader must hand compressed samples to our decoder instead of trying
        // to expose D3D textures directly from Media Foundation.
        return useMjpegHighFrameRateMode &&
            !requireP010 &&
            string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
    }

    private ParallelMjpegDecodePipeline? CreateExternalMjpegPipelineIfNeeded(
        bool useExternalMjpegDecode,
        int mjpegDecoderCount,
        int width,
        int height)
    {
        if (!useExternalMjpegDecode)
        {
            return null;
        }

        ParallelMjpegDecodePipeline? mjpegPipeline = null;
        try
        {
            mjpegPipeline = new ParallelMjpegDecodePipeline(
                mjpegDecoderCount,
                width,
                height,
                OnMjpegPipelineFrameEmitted,
                OnMjpegPipelineFatalError,
                OnMjpegPipelinePreviewFrameDecoded);
            return mjpegPipeline;
        }
        catch (Exception ex)
        {
            mjpegPipeline?.Dispose();
            Logger.Log($"SW_MJPEG_PIPELINE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            throw new InvalidOperationException(
                $"CPU MJPEG decode pipeline failed to initialize: {ex.Message}", ex);
        }
    }

    private void InstallMjpegPreviewJitterBuffer(double fps)
    {
        Volatile.Write(
            ref _mjpegPreviewJitterBuffer,
            new MjpegPreviewJitterBuffer(
                fps,
                () => Volatile.Read(ref _previewSink),
                () => _previewSuppressed,
                previewFrameProbe: null,
                targetDepth: 3));
    }

    private void StopAndDisposeMjpegPipeline(ParallelMjpegDecodePipeline mjpegPipelineToStop)
    {
        var jitterBuffer = Interlocked.Exchange(ref _mjpegPreviewJitterBuffer, null);
        jitterBuffer?.Dispose();

        if (!mjpegPipelineToStop.TryStop(TimeSpan.FromSeconds(5), out var failureReason))
        {
            var stopException = new InvalidOperationException(
                $"CPU MJPEG pipeline stop did not quiesce cleanly: {failureReason ?? "unknown"}");
            SignalFatalError(
                stopException,
                $"UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='{failureReason ?? "unknown"}'");
            throw stopException;
        }

        lock (_sync)
        {
            if (ReferenceEquals(_mjpegPipeline, mjpegPipelineToStop))
            {
                _mjpegPipeline = null;
            }
        }

        mjpegPipelineToStop.Dispose();
    }

    private static void DisposeMjpegPipelineResources(
        ParallelMjpegDecodePipeline? mjpegPipeline,
        MjpegPreviewJitterBuffer? jitterBuffer)
    {
        mjpegPipeline?.Dispose();
        jitterBuffer?.Dispose();
    }

    private void OnMjpegPipelineFatalError(Exception ex)
    {
        SignalFatalError(
            ex,
            $"UNIFIED_VIDEO_FATAL_MJPEG_ERROR type={ex.GetType().Name} msg={ex.Message}");
    }
}
