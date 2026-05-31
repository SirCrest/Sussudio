using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),
            Video = BuildRecordingIntegrityVideoProjection(captureRuntime),
            Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),
            Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),
            AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)
        };

    private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(
        RecordingIntegrityProjection recordingIntegrity)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),
            Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),
            Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),
            Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),
            AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)
        };

    private readonly record struct RecordingIntegrityProjection
    {
        public RecordingIntegritySummaryProjection Summary { get; init; }
        public RecordingIntegrityVideoProjection Video { get; init; }
        public RecordingIntegrityBackpressureProjection Backpressure { get; init; }
        public RecordingIntegrityAudioProjection Audio { get; init; }
        public RecordingIntegrityAvSyncProjection AvSync { get; init; }
    }

    private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Status = captureRuntime.RecordingIntegrityStatus,
            Complete = captureRuntime.RecordingIntegrityComplete,
            Backend = captureRuntime.RecordingIntegrityBackend,
            CompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            Reason = captureRuntime.RecordingIntegrityReason
        };

    private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(
        RecordingIntegritySummaryProjection summary)
        => new()
        {
            Status = summary.Status,
            Complete = summary.Complete,
            Backend = summary.Backend,
            CompletedUtc = summary.CompletedUtc,
            Reason = summary.Reason
        };

    private readonly record struct RecordingIntegritySummaryProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryFlattenedProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            SourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            AcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            PipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            QueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            SubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            PacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            EncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps
        };

    private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(
        RecordingIntegrityVideoProjection video)
        => new()
        {
            SourceFrames = video.SourceFrames,
            AcceptedFrames = video.AcceptedFrames,
            PipelineDroppedFrames = video.PipelineDroppedFrames,
            QueueDroppedFrames = video.QueueDroppedFrames,
            SubmittedFrames = video.SubmittedFrames,
            EncodedFrames = video.EncodedFrames,
            PacketsWritten = video.PacketsWritten,
            EncoderDroppedFrames = video.EncoderDroppedFrames,
            SequenceGaps = video.SequenceGaps
        };

    private readonly record struct RecordingIntegrityVideoProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }

    private readonly record struct RecordingIntegrityVideoFlattenedProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }

    private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            QueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            BackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            BackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs
        };

    private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(
        RecordingIntegrityBackpressureProjection backpressure)
        => new()
        {
            QueueMaxDepth = backpressure.QueueMaxDepth,
            QueueOldestFrameAgeMs = backpressure.QueueOldestFrameAgeMs,
            BackpressureWaitMs = backpressure.BackpressureWaitMs,
            BackpressureEvents = backpressure.BackpressureEvents,
            BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs
        };

    private readonly record struct RecordingIntegrityBackpressureProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private readonly record struct RecordingIntegrityBackpressureFlattenedProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            AudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            AudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            AudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            AudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            AudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            AudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            AudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps
        };

    private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(
        RecordingIntegrityAudioProjection audio)
        => new()
        {
            AudioStatus = audio.AudioStatus,
            AudioEnabled = audio.AudioEnabled,
            AudioCaptureActive = audio.AudioCaptureActive,
            AudioFramesArrived = audio.AudioFramesArrived,
            AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audio.AudioSamplesEncoded,
            AudioDropEvents = audio.AudioDropEvents,
            AudioDiscontinuities = audio.AudioDiscontinuities,
            AudioTimestampErrors = audio.AudioTimestampErrors,
            AudioCallbackGaps = audio.AudioCallbackGaps
        };

    private readonly record struct RecordingIntegrityAudioProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }

    private readonly record struct RecordingIntegrityAudioFlattenedProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }

    private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            AvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples
        };

    private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(
        RecordingIntegrityAvSyncProjection avSync)
        => new()
        {
            AvSyncDriftMs = avSync.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = avSync.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityAvSyncFlattenedProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityFlattenedProjection
    {
        public RecordingIntegritySummaryFlattenedProjection Summary { get; init; }
        public RecordingIntegrityVideoFlattenedProjection Video { get; init; }
        public RecordingIntegrityBackpressureFlattenedProjection Backpressure { get; init; }
        public RecordingIntegrityAudioFlattenedProjection Audio { get; init; }
        public RecordingIntegrityAvSyncFlattenedProjection AvSync { get; init; }
    }

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

    private static RecordingOutputProjection BuildRecordingOutputProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        RecordingStats recordingStats,
        bool recordingFileGrowing,
        LastOutputProbe lastOutput,
        RecordingVerificationResult? lastVerification)
        => new()
        {
            OutputPath = viewModelSnapshot.OutputPath,
            RecordingTime = viewModelSnapshot.RecordingTime,
            RecordingSizeInfo = viewModelSnapshot.RecordingSizeInfo,
            RecordingBitrateInfo = viewModelSnapshot.RecordingBitrateInfo,
            RecordingVideoBytes = recordingStats.VideoBytes,
            RecordingAudioBytes = recordingStats.AudioBytes,
            RecordingTotalBytes = recordingStats.TotalBytes,
            RecordingFileGrowing = recordingFileGrowing,
            LastOutputPath = captureRuntime.LastOutputPath,
            LastFinalizeStatus = captureRuntime.LastFinalizeStatus,
            LastFinalizeUtc = captureRuntime.LastFinalizeUtc,
            LastOutputExists = lastOutput.Exists,
            LastOutputSizeBytes = lastOutput.SizeBytes,
            LastVerification = lastVerification
        };

    private readonly record struct RecordingOutputProjection
    {
        public string OutputPath { get; init; }
        public string RecordingTime { get; init; }
        public string RecordingSizeInfo { get; init; }
        public string RecordingBitrateInfo { get; init; }
        public long RecordingVideoBytes { get; init; }
        public long RecordingAudioBytes { get; init; }
        public long RecordingTotalBytes { get; init; }
        public bool RecordingFileGrowing { get; init; }
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; }
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public bool LastOutputExists { get; init; }
        public long? LastOutputSizeBytes { get; init; }
        public RecordingVerificationResult? LastVerification { get; init; }
    }

    private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Backend = captureRuntime.RecordingBackend,
            AudioPathMode = captureRuntime.AudioPathMode,
            MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)
        };

    private static string ResolveMuxResult(bool? muxSucceeded)
        => muxSucceeded.HasValue
            ? (muxSucceeded.Value ? "Succeeded" : "Failed")
            : "NotAttempted";

    private readonly record struct RecordingBackendProjection
    {
        public string Backend { get; init; }
        public string AudioPathMode { get; init; }
        public string MuxResult { get; init; }
    }

    private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(
        RecordingBackendProjection recordingBackend,
        RecordingOutputProjection recordingOutput)
        => new()
        {
            Backend = recordingBackend.Backend,
            AudioPathMode = recordingBackend.AudioPathMode,
            MuxResult = recordingBackend.MuxResult,
            OutputPath = recordingOutput.OutputPath,
            RecordingTime = recordingOutput.RecordingTime,
            RecordingSizeInfo = recordingOutput.RecordingSizeInfo,
            RecordingBitrateInfo = recordingOutput.RecordingBitrateInfo,
            RecordingVideoBytes = recordingOutput.RecordingVideoBytes,
            RecordingAudioBytes = recordingOutput.RecordingAudioBytes,
            RecordingTotalBytes = recordingOutput.RecordingTotalBytes,
            RecordingFileGrowing = recordingOutput.RecordingFileGrowing,
            LastOutputPath = recordingOutput.LastOutputPath,
            LastFinalizeStatus = recordingOutput.LastFinalizeStatus,
            LastFinalizeUtc = recordingOutput.LastFinalizeUtc,
            LastOutputExists = recordingOutput.LastOutputExists,
            LastOutputSizeBytes = recordingOutput.LastOutputSizeBytes,
            LastVerification = recordingOutput.LastVerification
        };

    private readonly record struct RecordingOutputFlattenedProjection
    {
        public string Backend { get; init; }
        public string AudioPathMode { get; init; }
        public string MuxResult { get; init; }
        public string OutputPath { get; init; }
        public string RecordingTime { get; init; }
        public string RecordingSizeInfo { get; init; }
        public string RecordingBitrateInfo { get; init; }
        public long RecordingVideoBytes { get; init; }
        public long RecordingAudioBytes { get; init; }
        public long RecordingTotalBytes { get; init; }
        public bool RecordingFileGrowing { get; init; }
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; }
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public bool LastOutputExists { get; init; }
        public long? LastOutputSizeBytes { get; init; }
        public RecordingVerificationResult? LastVerification { get; init; }
    }

    private static AudioAndIngestProjection BuildAudioAndIngestProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        AudioSignalState audioSignal)
        => new()
        {
            Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),
            Ingest = BuildCaptureIngestProjection(captureRuntime),
            Wasapi = BuildWasapiAudioProjection(captureRuntime)
        };

    private readonly record struct AudioAndIngestProjection
    {
        public AudioSignalProjection Signal { get; init; }
        public CaptureIngestProjection Ingest { get; init; }
        public WasapiAudioProjection Wasapi { get; init; }
    }

    private static AudioSignalProjection BuildAudioSignalProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        AudioSignalState audioSignal)
        => new()
        {
            Peak = viewModelSnapshot.AudioPeak,
            Clipping = viewModelSnapshot.AudioClipping,
            SignalPresent = audioSignal.SignalPresent,
            MutedSuspected = audioSignal.MutedSuspected
        };

    private readonly record struct AudioSignalProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }

    private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(
        AudioSignalProjection signal)
        => new()
        {
            Peak = signal.Peak,
            Clipping = signal.Clipping,
            SignalPresent = signal.SignalPresent,
            MutedSuspected = signal.MutedSuspected
        };

    private readonly record struct AudioSignalFlattenedProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }

    private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            VideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            VideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            LastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth
        };

    private readonly record struct CaptureIngestProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public bool SourceReaderReadOutstanding { get; init; }
        public long SourceReaderReadOutstandingMs { get; init; }
        public long SourceReaderLastFrameTickMs { get; init; }
        public int SourceReaderFrameChannelDepth { get; init; }
    }

    private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            AudioReaderActive = ingest.AudioReaderActive,
            AudioFramesArrived = ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,
            VideoReaderActive = ingest.VideoReaderActive,
            VideoFramesArrived = ingest.VideoFramesArrived,
            VideoFramesWrittenToSink = ingest.VideoFramesWrittenToSink,
            LastVideoFrameAgeMs = ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = ingest.VideoIngestErrorCount
        };

    private readonly record struct CaptureIngestFlattenedProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
    }

    private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            FramesDelivered = ingest.MfSourceReaderFramesDelivered,
            FramesDropped = ingest.MfSourceReaderFramesDropped,
            NegotiatedFormat = ingest.MfSourceReaderNegotiatedFormat,
            ReadOutstanding = ingest.SourceReaderReadOutstanding,
            ReadOutstandingMs = ingest.SourceReaderReadOutstandingMs,
            LastFrameTickMs = ingest.SourceReaderLastFrameTickMs,
            FrameChannelDepth = ingest.SourceReaderFrameChannelDepth
        };

    private readonly record struct SourceReaderFlattenedProjection
    {
        public long FramesDelivered { get; init; }
        public long FramesDropped { get; init; }
        public string? NegotiatedFormat { get; init; }
        public bool ReadOutstanding { get; init; }
        public long ReadOutstandingMs { get; init; }
        public long LastFrameTickMs { get; init; }
        public int FrameChannelDepth { get; init; }
    }

    private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            CaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            CaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            CaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            CaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            CaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            CaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            CaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            CaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            CaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            PlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            PlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            PlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            PlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            PlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            PlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            PlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            PlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            PlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs
        };

    private readonly record struct WasapiAudioProjection
    {
        public long CaptureCallbackCount { get; init; }
        public double CaptureCallbackAvgIntervalMs { get; init; }
        public double CaptureCallbackMaxIntervalMs { get; init; }
        public long CaptureCallbackSevereGapCount { get; init; }
        public long CaptureAudioDiscontinuityCount { get; init; }
        public long CaptureAudioTimestampErrorCount { get; init; }
        public long CaptureAudioGlitchCount { get; init; }
        public int CaptureCallbackSilenceCount { get; init; }
        public long CaptureLastCallbackTickMs { get; init; }
        public long CaptureAudioLevelEventsFired { get; init; }
        public long CaptureAudioLevelLastFireTickMs { get; init; }
        public long PlaybackRenderCallbackCount { get; init; }
        public int PlaybackRenderSilenceCount { get; init; }
        public int PlaybackQueueDepth { get; init; }
        public int PlaybackQueueDropCount { get; init; }
        public double PlaybackQueueDurationMs { get; init; }
        public double PlaybackActiveChunkDurationMs { get; init; }
        public double PlaybackEndpointQueuedDurationMs { get; init; }
        public double PlaybackBufferedDurationMs { get; init; }
        public double PlaybackStreamLatencyMs { get; init; }
        public long PlaybackLastRenderTickMs { get; init; }
    }

    private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            CallbackCount = wasapi.CaptureCallbackCount,
            CallbackAvgIntervalMs = wasapi.CaptureCallbackAvgIntervalMs,
            CallbackMaxIntervalMs = wasapi.CaptureCallbackMaxIntervalMs,
            CallbackSevereGapCount = wasapi.CaptureCallbackSevereGapCount,
            AudioDiscontinuityCount = wasapi.CaptureAudioDiscontinuityCount,
            AudioTimestampErrorCount = wasapi.CaptureAudioTimestampErrorCount,
            AudioGlitchCount = wasapi.CaptureAudioGlitchCount,
            CallbackSilenceCount = wasapi.CaptureCallbackSilenceCount,
            LastCallbackTickMs = wasapi.CaptureLastCallbackTickMs,
            AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,
            AudioLevelLastFireTickMs = wasapi.CaptureAudioLevelLastFireTickMs
        };

    private readonly record struct WasapiCaptureFlattenedProjection
    {
        public long CallbackCount { get; init; }
        public double CallbackAvgIntervalMs { get; init; }
        public double CallbackMaxIntervalMs { get; init; }
        public long CallbackSevereGapCount { get; init; }
        public long AudioDiscontinuityCount { get; init; }
        public long AudioTimestampErrorCount { get; init; }
        public long AudioGlitchCount { get; init; }
        public int CallbackSilenceCount { get; init; }
        public long LastCallbackTickMs { get; init; }
        public long AudioLevelEventsFired { get; init; }
        public long AudioLevelLastFireTickMs { get; init; }
    }

    private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            RenderCallbackCount = wasapi.PlaybackRenderCallbackCount,
            RenderSilenceCount = wasapi.PlaybackRenderSilenceCount,
            QueueDepth = wasapi.PlaybackQueueDepth,
            QueueDropCount = wasapi.PlaybackQueueDropCount,
            QueueDurationMs = wasapi.PlaybackQueueDurationMs,
            ActiveChunkDurationMs = wasapi.PlaybackActiveChunkDurationMs,
            EndpointQueuedDurationMs = wasapi.PlaybackEndpointQueuedDurationMs,
            BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,
            StreamLatencyMs = wasapi.PlaybackStreamLatencyMs,
            LastRenderTickMs = wasapi.PlaybackLastRenderTickMs
        };

    private readonly record struct WasapiPlaybackFlattenedProjection
    {
        public long RenderCallbackCount { get; init; }
        public int RenderSilenceCount { get; init; }
        public int QueueDepth { get; init; }
        public int QueueDropCount { get; init; }
        public double QueueDurationMs { get; init; }
        public double ActiveChunkDurationMs { get; init; }
        public double EndpointQueuedDurationMs { get; init; }
        public double BufferedDurationMs { get; init; }
        public double StreamLatencyMs { get; init; }
        public long LastRenderTickMs { get; init; }
    }

    private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)
        => new()
        {
            QueueSaturated = health.AudioDropsQueueSaturated,
            BacklogEviction = health.AudioDropsBacklogEviction,
            ChunksDropped = health.AudioChunksDropped,
            QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            QueueDropsFileWriter = health.AudioChunksDropped
        };

    private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(AudioDropsProjection audioDrops)
        => new()
        {
            QueueSaturated = audioDrops.QueueSaturated,
            BacklogEviction = audioDrops.BacklogEviction,
            ChunksDropped = audioDrops.ChunksDropped,
            QueueDropsRealtime = audioDrops.QueueDropsRealtime,
            QueueDropsFileWriter = audioDrops.QueueDropsFileWriter
        };

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private readonly record struct AudioDropsFlattenedProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),
            Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),
            SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),
            WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),
            WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)
        };

    private readonly record struct AudioAndIngestFlattenedProjection
    {
        public AudioSignalFlattenedProjection Signal { get; init; }
        public CaptureIngestFlattenedProjection Ingest { get; init; }
        public SourceReaderFlattenedProjection SourceReader { get; init; }
        public WasapiCaptureFlattenedProjection WasapiCapture { get; init; }
        public WasapiPlaybackFlattenedProjection WasapiPlayback { get; init; }
    }
}
