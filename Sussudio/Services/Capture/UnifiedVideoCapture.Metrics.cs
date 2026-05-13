using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
    public readonly record struct MjpegPipelineTimingMetrics(
        int DecodeSampleCount,
        double DecodeAvgMs,
        double DecodeP95Ms,
        double DecodeMaxMs,
        int InteropCopySampleCount,
        double InteropCopyAvgMs,
        double InteropCopyP95Ms,
        double InteropCopyMaxMs,
        int CallbackSampleCount,
        double CallbackAvgMs,
        double CallbackP95Ms,
        double CallbackMaxMs);

    public readonly record struct MjpegPipelineTimingSnapshot(
        MjpegPipelineTimingMetrics Summary,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? Details);

    public MfSourceReaderVideoCapture.SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        var capture = _capture;
        return capture?.GetSourceCadenceMetrics() ?? default;
    }

    public MjpegPipelineTimingMetrics GetMjpegPipelineTimingMetrics()
        => GetMjpegPipelineTimingSnapshot().Summary;

    public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()
    {
        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline == null)
        {
            return default;
        }

        var metrics = pipeline.GetTimingMetrics();
        return new MjpegPipelineTimingSnapshot(
            Summary: CreateMjpegPipelineTimingSummary(metrics),
            Details: metrics);
    }

    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetFullMjpegPipelineTimingMetrics()
    {
        return Volatile.Read(ref _mjpegPipeline)?.GetTimingMetrics();
    }

    private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary(ParallelMjpegDecodePipeline.PipelineTimingMetrics metrics)
    {
        return new MjpegPipelineTimingMetrics(
            DecodeSampleCount: metrics.DecodeSampleCount,
            DecodeAvgMs: metrics.DecodeAvgMs,
            DecodeP95Ms: metrics.DecodeP95Ms,
            DecodeMaxMs: metrics.DecodeMaxMs,
            InteropCopySampleCount: 0,
            InteropCopyAvgMs: 0,
            InteropCopyP95Ms: 0,
            InteropCopyMaxMs: 0,
            CallbackSampleCount: metrics.PipelineSampleCount,
            CallbackAvgMs: metrics.PipelineAvgMs,
            CallbackP95Ms: metrics.PipelineP95Ms,
            CallbackMaxMs: metrics.PipelineMaxMs);
    }

    public MjpegPreviewJitterBuffer.Metrics GetMjpegPreviewJitterMetrics()
    {
        return Volatile.Read(ref _mjpegPreviewJitterBuffer)?.GetMetrics() ?? default;
    }

    public VisualCadenceTracker.Metrics GetPreviewVisualCadenceMetrics()
    {
        return _visualCadenceTracker.GetMetrics();
    }

    public VisualCadenceTracker.Metrics GetPreviewVisualCenterCadenceMetrics()
    {
        return _visualCenterCadenceTracker.GetMetrics();
    }

    public FrameFingerprintCadenceTracker.Metrics GetMjpegPacketHashMetrics()
    {
        return Volatile.Read(ref _mjpegPipeline)?.GetPacketHashMetrics()
            ?? FrameFingerprintCadenceTracker.Empty;
    }

    public FrameLedgerSummary GetFrameLedgerSummary(int maxEvents = 64)
        => _frameLedger.GetSummary(maxEvents);
}
