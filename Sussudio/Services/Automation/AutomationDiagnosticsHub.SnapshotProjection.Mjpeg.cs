using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)
        => new()
        {
            DecodeSampleCount = health.MjpegDecodeSampleCount,
            DecodeAvgMs = health.MjpegDecodeAvgMs,
            DecodeP95Ms = health.MjpegDecodeP95Ms,
            DecodeMaxMs = health.MjpegDecodeMaxMs,
            InteropCopySampleCount = health.MjpegInteropCopySampleCount,
            InteropCopyAvgMs = health.MjpegInteropCopyAvgMs,
            InteropCopyP95Ms = health.MjpegInteropCopyP95Ms,
            InteropCopyMaxMs = health.MjpegInteropCopyMaxMs,
            CallbackSampleCount = health.MjpegCallbackSampleCount,
            CallbackAvgMs = health.MjpegCallbackAvgMs,
            CallbackP95Ms = health.MjpegCallbackP95Ms,
            CallbackMaxMs = health.MjpegCallbackMaxMs,
            DecoderCount = health.MjpegDecoderCount,
            ReorderSampleCount = health.MjpegReorderSampleCount,
            ReorderAvgMs = health.MjpegReorderAvgMs,
            ReorderP95Ms = health.MjpegReorderP95Ms,
            ReorderMaxMs = health.MjpegReorderMaxMs,
            PipelineSampleCount = health.MjpegPipelineSampleCount,
            PipelineAvgMs = health.MjpegPipelineAvgMs,
            PipelineP95Ms = health.MjpegPipelineP95Ms,
            PipelineMaxMs = health.MjpegPipelineMaxMs,
            TotalDecoded = health.MjpegTotalDecoded,
            TotalEmitted = health.MjpegTotalEmitted,
            TotalDropped = health.MjpegTotalDropped,
            CompressedFramesQueued = health.MjpegCompressedFramesQueued,
            CompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            CompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            CompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            CompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            DecodeFailures = health.MjpegDecodeFailures,
            ReorderCollisions = health.MjpegReorderCollisions,
            EmitFailures = health.MjpegEmitFailures,
            CompressedQueueDepth = health.MjpegCompressedQueueDepth,
            CompressedQueueBytes = health.MjpegCompressedQueueBytes,
            CompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            ReorderSkips = health.MjpegReorderSkips,
            ReorderBufferDepth = health.MjpegReorderBufferDepth,
            PreviewJitterEnabled = health.MjpegPreviewJitterEnabled,
            PreviewJitterTargetDepth = health.MjpegPreviewJitterTargetDepth,
            PreviewJitterMaxDepth = health.MjpegPreviewJitterMaxDepth,
            PreviewJitterQueueDepth = health.MjpegPreviewJitterQueueDepth,
            PreviewJitterTotalQueued = health.MjpegPreviewJitterTotalQueued,
            PreviewJitterTotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            PreviewJitterTotalDropped = health.MjpegPreviewJitterTotalDropped,
            PreviewJitterUnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            PreviewJitterResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount,
            PreviewJitterInputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            PreviewJitterInputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            PreviewJitterInputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            PreviewJitterInputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            PreviewJitterOutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            PreviewJitterOutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            PreviewJitterOutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            PreviewJitterOutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            PreviewJitterLatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            PreviewJitterLatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            PreviewJitterLatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            PreviewJitterLatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs,
            PreviewJitterDeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            PreviewJitterClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            PreviewJitterTargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            PreviewJitterTargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount,
            PreviewJitterLastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            PreviewJitterLastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            PreviewJitterLastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            PreviewJitterLastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            PreviewJitterLastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            PreviewJitterLastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            PreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,
            PreviewJitterLastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            PreviewJitterLastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            PreviewJitterLastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            PreviewJitterLastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            PreviewJitterLastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            PreviewJitterLastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            PreviewJitterMaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            PreviewJitterScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount,
            PacketHashSampleCount = health.MjpegPacketHashSampleCount,
            PacketHashUniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            PacketHashDuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            PacketHashLongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            PacketHashInputObservedFps = health.MjpegPacketHashInputObservedFps,
            PacketHashUniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            PacketHashDuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            PacketHashLastHash = health.MjpegPacketHashLastHash,
            PacketHashLastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            PacketHashPattern = health.MjpegPacketHashPattern,
            PacketHashRecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            PacketHashRecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            PacketHashRecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags,
            PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderAutomationSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderAutomationSnapshot>()
        };

    private readonly record struct MjpegProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public long TotalDecoded { get; init; }
        public long TotalEmitted { get; init; }
        public long TotalDropped { get; init; }
        public long CompressedFramesQueued { get; init; }
        public long CompressedFramesDequeued { get; init; }
        public long CompressedDropsQueueFull { get; init; }
        public long CompressedDropsByteBudget { get; init; }
        public long CompressedDropsDisposed { get; init; }
        public long DecodeFailures { get; init; }
        public long ReorderCollisions { get; init; }
        public long EmitFailures { get; init; }
        public int CompressedQueueDepth { get; init; }
        public long CompressedQueueBytes { get; init; }
        public long CompressedQueueByteBudget { get; init; }
        public long ReorderSkips { get; init; }
        public int ReorderBufferDepth { get; init; }
        public bool PreviewJitterEnabled { get; init; }
        public int PreviewJitterTargetDepth { get; init; }
        public int PreviewJitterMaxDepth { get; init; }
        public int PreviewJitterQueueDepth { get; init; }
        public long PreviewJitterTotalQueued { get; init; }
        public long PreviewJitterTotalSubmitted { get; init; }
        public long PreviewJitterTotalDropped { get; init; }
        public long PreviewJitterUnderflowCount { get; init; }
        public long PreviewJitterResumeReprimeCount { get; init; }
        public int PreviewJitterInputSampleCount { get; init; }
        public double PreviewJitterInputAvgMs { get; init; }
        public double PreviewJitterInputP95Ms { get; init; }
        public double PreviewJitterInputMaxMs { get; init; }
        public int PreviewJitterOutputSampleCount { get; init; }
        public double PreviewJitterOutputAvgMs { get; init; }
        public double PreviewJitterOutputP95Ms { get; init; }
        public double PreviewJitterOutputMaxMs { get; init; }
        public int PreviewJitterLatencySampleCount { get; init; }
        public double PreviewJitterLatencyAvgMs { get; init; }
        public double PreviewJitterLatencyP95Ms { get; init; }
        public double PreviewJitterLatencyMaxMs { get; init; }
        public long PreviewJitterDeadlineDropCount { get; init; }
        public long PreviewJitterClearedDropCount { get; init; }
        public long PreviewJitterTargetIncreaseCount { get; init; }
        public long PreviewJitterTargetDecreaseCount { get; init; }
        public long PreviewJitterLastSelectedPreviewPresentId { get; init; }
        public long PreviewJitterLastSelectedSourceSequenceNumber { get; init; }
        public long PreviewJitterLastSelectedQpc { get; init; }
        public double PreviewJitterLastSelectedSourceLatencyMs { get; init; }
        public long PreviewJitterLastDroppedSourceSequenceNumber { get; init; }
        public long PreviewJitterLastDropQpc { get; init; }
        public string PreviewJitterLastDropReason { get; init; }
        public long PreviewJitterLastUnderflowQpc { get; init; }
        public string PreviewJitterLastUnderflowReason { get; init; }
        public int PreviewJitterLastUnderflowQueueDepth { get; init; }
        public double PreviewJitterLastUnderflowInputAgeMs { get; init; }
        public double PreviewJitterLastUnderflowOutputAgeMs { get; init; }
        public double PreviewJitterLastScheduleLateMs { get; init; }
        public double PreviewJitterMaxScheduleLateMs { get; init; }
        public long PreviewJitterScheduleLateCount { get; init; }
        public int PacketHashSampleCount { get; init; }
        public long PacketHashUniqueFrameCount { get; init; }
        public long PacketHashDuplicateFrameCount { get; init; }
        public long PacketHashLongestDuplicateRun { get; init; }
        public double PacketHashInputObservedFps { get; init; }
        public double PacketHashUniqueObservedFps { get; init; }
        public double PacketHashDuplicateFramePercent { get; init; }
        public string PacketHashLastHash { get; init; }
        public bool PacketHashLastFrameDuplicate { get; init; }
        public string PacketHashPattern { get; init; }
        public double[] PacketHashRecentInputIntervalsMs { get; init; }
        public double[] PacketHashRecentUniqueIntervalsMs { get; init; }
        public int[] PacketHashRecentDuplicateFlags { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }
}
