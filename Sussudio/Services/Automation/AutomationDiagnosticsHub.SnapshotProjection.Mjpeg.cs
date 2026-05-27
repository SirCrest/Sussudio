using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)
    {
        var timing = BuildMjpegTimingProjection(health);
        var previewJitter = BuildMjpegPreviewJitterProjection(health);
        var packetHash = BuildMjpegPacketHashProjection(health);

        return new()
        {
            Timing = timing,
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
            PreviewJitter = previewJitter,
            PacketHash = packetHash,
        };
    }

    private readonly record struct MjpegProjection
    {
        public MjpegTimingProjection Timing { get; init; }
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
        public MjpegPreviewJitterProjection PreviewJitter { get; init; }
        public MjpegPacketHashProjection PacketHash { get; init; }
    }

    private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)
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

    private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(
        MjpegTimingProjection timing)
        => new()
        {
            DecodeSampleCount = timing.DecodeSampleCount,
            DecodeAvgMs = timing.DecodeAvgMs,
            DecodeP95Ms = timing.DecodeP95Ms,
            DecodeMaxMs = timing.DecodeMaxMs,
            InteropCopySampleCount = timing.InteropCopySampleCount,
            InteropCopyAvgMs = timing.InteropCopyAvgMs,
            InteropCopyP95Ms = timing.InteropCopyP95Ms,
            InteropCopyMaxMs = timing.InteropCopyMaxMs,
            CallbackSampleCount = timing.CallbackSampleCount,
            CallbackAvgMs = timing.CallbackAvgMs,
            CallbackP95Ms = timing.CallbackP95Ms,
            CallbackMaxMs = timing.CallbackMaxMs,
            DecoderCount = timing.DecoderCount,
            ReorderSampleCount = timing.ReorderSampleCount,
            ReorderAvgMs = timing.ReorderAvgMs,
            ReorderP95Ms = timing.ReorderP95Ms,
            ReorderMaxMs = timing.ReorderMaxMs,
            PipelineSampleCount = timing.PipelineSampleCount,
            PipelineAvgMs = timing.PipelineAvgMs,
            PipelineP95Ms = timing.PipelineP95Ms,
            PipelineMaxMs = timing.PipelineMaxMs,
            PerDecoder = timing.PerDecoder
        };

    private readonly record struct MjpegTimingProjection
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
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private readonly record struct MjpegTimingFlattenedProjection
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
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueProjection(health),
            Timing = BuildMjpegPreviewJitterTimingProjection(health),
            Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),
            Events = BuildMjpegPreviewJitterEventProjection(health)
        };

    private readonly record struct MjpegPreviewJitterProjection
    {
        public MjpegPreviewJitterQueueProjection Queue { get; init; }
        public MjpegPreviewJitterTimingProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventProjection Events { get; init; }
    }

    private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Enabled = health.MjpegPreviewJitterEnabled,
            TargetDepth = health.MjpegPreviewJitterTargetDepth,
            MaxDepth = health.MjpegPreviewJitterMaxDepth,
            QueueDepth = health.MjpegPreviewJitterQueueDepth,
            TotalQueued = health.MjpegPreviewJitterTotalQueued,
            TotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            TotalDropped = health.MjpegPreviewJitterTotalDropped,
            UnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }

    private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(
        MjpegPreviewJitterQueueProjection queue)
        => new()
        {
            Enabled = queue.Enabled,
            TargetDepth = queue.TargetDepth,
            MaxDepth = queue.MaxDepth,
            QueueDepth = queue.QueueDepth,
            TotalQueued = queue.TotalQueued,
            TotalSubmitted = queue.TotalSubmitted,
            TotalDropped = queue.TotalDropped,
            UnderflowCount = queue.UnderflowCount,
            ResumeReprimeCount = queue.ResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueFlattenedProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }

    private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            InputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            InputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            InputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            InputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            OutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            OutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            OutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            OutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            LatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            LatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            LatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(
        MjpegPreviewJitterTimingProjection timing)
        => new()
        {
            InputSampleCount = timing.InputSampleCount,
            InputAvgMs = timing.InputAvgMs,
            InputP95Ms = timing.InputP95Ms,
            InputMaxMs = timing.InputMaxMs,
            OutputSampleCount = timing.OutputSampleCount,
            OutputAvgMs = timing.OutputAvgMs,
            OutputP95Ms = timing.OutputP95Ms,
            OutputMaxMs = timing.OutputMaxMs,
            LatencySampleCount = timing.LatencySampleCount,
            LatencyAvgMs = timing.LatencyAvgMs,
            LatencyP95Ms = timing.LatencyP95Ms,
            LatencyMaxMs = timing.LatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingFlattenedProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            ClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            TargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(
        MjpegPreviewJitterAdaptiveProjection adaptive)
        => new()
        {
            DeadlineDropCount = adaptive.DeadlineDropCount,
            ClearedDropCount = adaptive.ClearedDropCount,
            TargetIncreaseCount = adaptive.TargetIncreaseCount,
            TargetDecreaseCount = adaptive.TargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveFlattenedProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            LastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            LastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            LastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            LastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            LastDropReason = health.MjpegPreviewJitterLastDropReason,
            LastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            LastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            LastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            LastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }

    private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(
        MjpegPreviewJitterEventProjection events)
        => new()
        {
            LastSelectedPreviewPresentId = events.LastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = events.LastSelectedSourceSequenceNumber,
            LastSelectedQpc = events.LastSelectedQpc,
            LastSelectedSourceLatencyMs = events.LastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = events.LastDroppedSourceSequenceNumber,
            LastDropQpc = events.LastDropQpc,
            LastDropReason = events.LastDropReason,
            LastUnderflowQpc = events.LastUnderflowQpc,
            LastUnderflowReason = events.LastUnderflowReason,
            LastUnderflowQueueDepth = events.LastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = events.LastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = events.LastUnderflowOutputAgeMs,
            LastScheduleLateMs = events.LastScheduleLateMs,
            MaxScheduleLateMs = events.MaxScheduleLateMs,
            ScheduleLateCount = events.ScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventFlattenedProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }

    private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(
        MjpegPreviewJitterProjection previewJitter)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),
            Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),
            Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),
            Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)
        };

    private readonly record struct MjpegPreviewJitterFlattenedProjection
    {
        public MjpegPreviewJitterQueueFlattenedProjection Queue { get; init; }
        public MjpegPreviewJitterTimingFlattenedProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveFlattenedProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventFlattenedProjection Events { get; init; }
    }

    private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.MjpegPacketHashSampleCount,
            UniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            DuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            LongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            InputObservedFps = health.MjpegPacketHashInputObservedFps,
            UniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            DuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            LastHash = health.MjpegPacketHashLastHash,
            LastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            Pattern = health.MjpegPacketHashPattern,
            RecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            RecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags
        };

    private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(
        MjpegPacketHashProjection packetHash)
        => new()
        {
            SampleCount = packetHash.SampleCount,
            UniqueFrameCount = packetHash.UniqueFrameCount,
            DuplicateFrameCount = packetHash.DuplicateFrameCount,
            LongestDuplicateRun = packetHash.LongestDuplicateRun,
            InputObservedFps = packetHash.InputObservedFps,
            UniqueObservedFps = packetHash.UniqueObservedFps,
            DuplicateFramePercent = packetHash.DuplicateFramePercent,
            LastHash = packetHash.LastHash,
            LastFrameDuplicate = packetHash.LastFrameDuplicate,
            Pattern = packetHash.Pattern,
            RecentInputIntervalsMs = packetHash.RecentInputIntervalsMs,
            RecentUniqueIntervalsMs = packetHash.RecentUniqueIntervalsMs,
            RecentDuplicateFlags = packetHash.RecentDuplicateFlags
        };

    private readonly record struct MjpegPacketHashProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }

    private readonly record struct MjpegPacketHashFlattenedProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }

    private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(MjpegProjection mjpeg)
    {
        return new()
        {
            TotalDecoded = mjpeg.TotalDecoded,
            TotalEmitted = mjpeg.TotalEmitted,
            TotalDropped = mjpeg.TotalDropped,
            CompressedFramesQueued = mjpeg.CompressedFramesQueued,
            CompressedFramesDequeued = mjpeg.CompressedFramesDequeued,
            CompressedDropsQueueFull = mjpeg.CompressedDropsQueueFull,
            CompressedDropsByteBudget = mjpeg.CompressedDropsByteBudget,
            CompressedDropsDisposed = mjpeg.CompressedDropsDisposed,
            DecodeFailures = mjpeg.DecodeFailures,
            ReorderCollisions = mjpeg.ReorderCollisions,
            EmitFailures = mjpeg.EmitFailures,
            CompressedQueueDepth = mjpeg.CompressedQueueDepth,
            CompressedQueueBytes = mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,
            ReorderSkips = mjpeg.ReorderSkips,
            ReorderBufferDepth = mjpeg.ReorderBufferDepth,
        };
    }

    private readonly record struct MjpegFlattenedProjection
    {
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
    }
}
