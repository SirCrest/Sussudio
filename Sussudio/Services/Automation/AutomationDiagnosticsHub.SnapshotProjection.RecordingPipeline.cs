using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderProjection(health),
            Ingest = BuildRecordingPipelineIngestProjection(health),
            VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)
        };

    private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),
            Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),
            VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)
        };

    private readonly record struct RecordingPipelineProjection
    {
        public RecordingPipelineEncoderProjection Encoder { get; init; }
        public RecordingPipelineIngestProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }
    }

    private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(CaptureHealthSnapshot health)
        => new()
        {
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoFramesEncoded = health.VideoFramesConverted,
            LastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            LastWriteAgeMs = health.LastVideoWriteAgeMs,
            EncodingFailed = health.RecordingEncodingFailed,
            EncodingFailureType = health.RecordingEncodingFailureType,
            EncodingFailureMessage = health.RecordingEncodingFailureMessage
        };

    private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,
            VideoFramesEncoded = recordingPipeline.Encoder.VideoFramesEncoded,
            LastEnqueueAgeMs = recordingPipeline.Encoder.LastEnqueueAgeMs,
            LastWriteAgeMs = recordingPipeline.Encoder.LastWriteAgeMs,
            EncodingFailed = recordingPipeline.Encoder.EncodingFailed,
            EncodingFailureType = recordingPipeline.Encoder.EncodingFailureType,
            EncodingFailureMessage = recordingPipeline.Encoder.EncodingFailureMessage
        };

    private readonly record struct RecordingPipelineEncoderProjection
    {
        public long VideoFramesEnqueued { get; init; }
        public long VideoFramesEncoded { get; init; }
        public long LastEnqueueAgeMs { get; init; }
        public long LastWriteAgeMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }

    private readonly record struct RecordingPipelineEncoderFlattenedProjection
    {
        public long VideoFramesEnqueued { get; init; }
        public long VideoFramesEncoded { get; init; }
        public long LastEnqueueAgeMs { get; init; }
        public long LastWriteAgeMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }

    private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(CaptureHealthSnapshot health)
        => new()
        {
            ConversionQueueDepth = health.ConversionQueueDepth,
            FfmpegVideoQueueDepth = health.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = health.FfmpegAudioQueueDepth,
            VideoFramesArrived = health.VideoFramesArrived,
            VideoFramesQueued = health.VideoFramesQueued,
            VideoFramesDropped = health.VideoFramesDropped,
            VideoFramesDroppedBacklog = health.VideoFramesDroppedBacklog,
            VideoFramesConverted = health.VideoFramesConverted,
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoDropsQueueSaturated = health.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction
        };

    private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,
            FfmpegVideoQueueDepth = recordingPipeline.Ingest.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = recordingPipeline.Ingest.FfmpegAudioQueueDepth,
            VideoFramesArrived = recordingPipeline.Ingest.VideoFramesArrived,
            VideoFramesQueued = recordingPipeline.Ingest.VideoFramesQueued,
            VideoFramesDropped = recordingPipeline.Ingest.VideoFramesDropped,
            VideoFramesDroppedBacklog = recordingPipeline.Ingest.VideoFramesDroppedBacklog,
            VideoFramesConverted = recordingPipeline.Ingest.VideoFramesConverted,
            VideoFramesEnqueued = recordingPipeline.Ingest.VideoFramesEnqueued,
            VideoDropsQueueSaturated = recordingPipeline.Ingest.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction
        };

    private readonly record struct RecordingPipelineIngestProjection
    {
        public int ConversionQueueDepth { get; init; }
        public int FfmpegVideoQueueDepth { get; init; }
        public int FfmpegAudioQueueDepth { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesQueued { get; init; }
        public long VideoFramesDropped { get; init; }
        public long VideoFramesDroppedBacklog { get; init; }
        public long VideoFramesConverted { get; init; }
        public long VideoFramesEnqueued { get; init; }
        public long VideoDropsQueueSaturated { get; init; }
        public long VideoDropsBacklogEviction { get; init; }
    }

    private readonly record struct RecordingPipelineIngestFlattenedProjection
    {
        public int ConversionQueueDepth { get; init; }
        public int FfmpegVideoQueueDepth { get; init; }
        public int FfmpegAudioQueueDepth { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesQueued { get; init; }
        public long VideoFramesDropped { get; init; }
        public long VideoFramesDroppedBacklog { get; init; }
        public long VideoFramesConverted { get; init; }
        public long VideoFramesEnqueued { get; init; }
        public long VideoDropsQueueSaturated { get; init; }
        public long VideoDropsBacklogEviction { get; init; }
    }

    private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(CaptureHealthSnapshot health)
        => new()
        {
            Capacity = health.RecordingVideoQueueCapacity,
            MaxDepth = health.RecordingVideoQueueMaxDepth,
            FramesSubmittedToEncoder = health.RecordingVideoFramesSubmittedToEncoder,
            EncoderPts = health.RecordingVideoEncoderPts,
            EncoderPacketsWritten = health.RecordingVideoEncoderPacketsWritten,
            EncoderDroppedFrames = health.RecordingVideoEncoderDroppedFrames,
            SequenceGaps = health.RecordingVideoSequenceGaps,
            OldestFrameAgeMs = health.RecordingVideoQueueOldestFrameAgeMs,
            LastLatencyMs = health.RecordingVideoQueueLastLatencyMs,
            LatencySampleCount = health.RecordingVideoQueueLatencySampleCount,
            LatencyAvgMs = health.RecordingVideoQueueLatencyAvgMs,
            LatencyP95Ms = health.RecordingVideoQueueLatencyP95Ms,
            LatencyP99Ms = health.RecordingVideoQueueLatencyP99Ms,
            LatencyMaxMs = health.RecordingVideoQueueLatencyMaxMs,
            BackpressureWaitMs = health.RecordingVideoBackpressureWaitMs,
            BackpressureEvents = health.RecordingVideoBackpressureEvents,
            BackpressureLastWaitMs = health.RecordingVideoBackpressureLastWaitMs,
            BackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs
        };

    private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Capacity = recordingPipeline.VideoQueue.Capacity,
            MaxDepth = recordingPipeline.VideoQueue.MaxDepth,
            FramesSubmittedToEncoder = recordingPipeline.VideoQueue.FramesSubmittedToEncoder,
            EncoderPts = recordingPipeline.VideoQueue.EncoderPts,
            EncoderPacketsWritten = recordingPipeline.VideoQueue.EncoderPacketsWritten,
            EncoderDroppedFrames = recordingPipeline.VideoQueue.EncoderDroppedFrames,
            SequenceGaps = recordingPipeline.VideoQueue.SequenceGaps,
            OldestFrameAgeMs = recordingPipeline.VideoQueue.OldestFrameAgeMs,
            LastLatencyMs = recordingPipeline.VideoQueue.LastLatencyMs,
            LatencySampleCount = recordingPipeline.VideoQueue.LatencySampleCount,
            LatencyAvgMs = recordingPipeline.VideoQueue.LatencyAvgMs,
            LatencyP95Ms = recordingPipeline.VideoQueue.LatencyP95Ms,
            LatencyP99Ms = recordingPipeline.VideoQueue.LatencyP99Ms,
            LatencyMaxMs = recordingPipeline.VideoQueue.LatencyMaxMs,
            BackpressureWaitMs = recordingPipeline.VideoQueue.BackpressureWaitMs,
            BackpressureEvents = recordingPipeline.VideoQueue.BackpressureEvents,
            BackpressureLastWaitMs = recordingPipeline.VideoQueue.BackpressureLastWaitMs,
            BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs
        };

    private readonly record struct RecordingPipelineVideoQueueProjection
    {
        public int Capacity { get; init; }
        public int MaxDepth { get; init; }
        public long FramesSubmittedToEncoder { get; init; }
        public long EncoderPts { get; init; }
        public long EncoderPacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public long OldestFrameAgeMs { get; init; }
        public long LastLatencyMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyP99Ms { get; init; }
        public double LatencyMaxMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureLastWaitMs { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private readonly record struct RecordingPipelineVideoQueueFlattenedProjection
    {
        public int Capacity { get; init; }
        public int MaxDepth { get; init; }
        public long FramesSubmittedToEncoder { get; init; }
        public long EncoderPts { get; init; }
        public long EncoderPacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public long OldestFrameAgeMs { get; init; }
        public long LastLatencyMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyP99Ms { get; init; }
        public double LatencyMaxMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureLastWaitMs { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(CaptureHealthSnapshot health)
        => new()
        {
            GpuQueueDepth = health.RecordingGpuQueueDepth,
            GpuQueueCapacity = health.RecordingGpuQueueCapacity,
            GpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            GpuFramesDropped = health.RecordingGpuFramesDropped,
            CudaQueueDepth = health.RecordingCudaQueueDepth,
            CudaQueueCapacity = health.RecordingCudaQueueCapacity,
            CudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            CudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            CudaFramesDropped = health.RecordingCudaFramesDropped
        };

    private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            GpuQueueDepth = recordingPipeline.HardwareQueues.GpuQueueDepth,
            GpuQueueCapacity = recordingPipeline.HardwareQueues.GpuQueueCapacity,
            GpuQueueMaxDepth = recordingPipeline.HardwareQueues.GpuQueueMaxDepth,
            GpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,
            GpuFramesDropped = recordingPipeline.HardwareQueues.GpuFramesDropped,
            CudaQueueDepth = recordingPipeline.HardwareQueues.CudaQueueDepth,
            CudaQueueCapacity = recordingPipeline.HardwareQueues.CudaQueueCapacity,
            CudaQueueMaxDepth = recordingPipeline.HardwareQueues.CudaQueueMaxDepth,
            CudaFramesEnqueued = recordingPipeline.HardwareQueues.CudaFramesEnqueued,
            CudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped
        };

    private readonly record struct RecordingPipelineHardwareQueuesProjection
    {
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public int CudaQueueDepth { get; init; }
        public int CudaQueueCapacity { get; init; }
        public int CudaQueueMaxDepth { get; init; }
        public long CudaFramesEnqueued { get; init; }
        public long CudaFramesDropped { get; init; }
    }

    private readonly record struct RecordingPipelineHardwareQueuesFlattenedProjection
    {
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public int CudaQueueDepth { get; init; }
        public int CudaQueueCapacity { get; init; }
        public int CudaQueueMaxDepth { get; init; }
        public long CudaFramesEnqueued { get; init; }
        public long CudaFramesDropped { get; init; }
    }

    private readonly record struct RecordingPipelineFlattenedProjection
    {
        public RecordingPipelineEncoderFlattenedProjection Encoder { get; init; }
        public RecordingPipelineIngestFlattenedProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueFlattenedProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesFlattenedProjection HardwareQueues { get; init; }
    }
}
