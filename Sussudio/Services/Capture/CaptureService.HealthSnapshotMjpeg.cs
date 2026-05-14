using System;
using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        var timingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();
        var fullTiming = timingSnapshot?.Details ?? _lastFullMjpegPipelineTimingMetrics;

        return new MjpegHealthSnapshotFields(
            timingSnapshot?.Summary ?? _lastMjpegPipelineTimingMetrics,
            fullTiming,
            unifiedVideoCapture?.GetMjpegPreviewJitterMetrics()
                ?? default(MjpegPreviewJitterBuffer.Metrics),
            unifiedVideoCapture?.GetPreviewVisualCadenceMetrics()
                ?? VisualCadenceTracker.Empty,
            unifiedVideoCapture?.GetPreviewVisualCenterCadenceMetrics()
                ?? VisualCadenceTracker.Empty,
            unifiedVideoCapture?.GetMjpegPacketHashMetrics()
                ?? FrameFingerprintCadenceTracker.Empty,
            BuildMjpegDecoderHealthSnapshots(fullTiming));
    }

    private static MjpegDecoderHealthSnapshot[] BuildMjpegDecoderHealthSnapshots(
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? fullTiming)
    {
        return fullTiming?.PerDecoder is { Length: > 0 } perDecoder
            ? Array.ConvertAll(
                perDecoder,
                worker => new MjpegDecoderHealthSnapshot(
                    worker.WorkerIndex,
                    worker.SampleCount,
                    worker.AvgMs,
                    worker.P95Ms,
                    worker.MaxMs))
            : Array.Empty<MjpegDecoderHealthSnapshot>();
    }

    private readonly record struct MjpegHealthSnapshotFields(
        UnifiedVideoCapture.MjpegPipelineTimingMetrics Timing,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? FullTiming,
        MjpegPreviewJitterBuffer.Metrics PreviewJitter,
        VisualCadenceTracker.Metrics VisualCadence,
        VisualCadenceTracker.Metrics VisualCenterCadence,
        FrameFingerprintCadenceTracker.Metrics PacketHash,
        MjpegDecoderHealthSnapshot[] PerDecoder);
}
