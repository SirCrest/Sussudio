using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public string OutputPath { get; init; } = string.Empty;
    public string RecordingTime { get; init; } = string.Empty;
    public string RecordingSizeInfo { get; init; } = string.Empty;
    public string RecordingBitrateInfo { get; init; } = string.Empty;

    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";
    public string RecordingIntegrityStatus { get; init; } = "NotStarted";
    public bool RecordingIntegrityComplete { get; init; }
    public string RecordingIntegrityBackend { get; init; } = "None";
    public DateTimeOffset? RecordingIntegrityCompletedUtc { get; init; }
    public long RecordingIntegritySourceFrames { get; init; }
    public long RecordingIntegrityAcceptedFrames { get; init; }
    public long RecordingIntegrityPipelineDroppedFrames { get; init; }
    public long RecordingIntegrityQueueDroppedFrames { get; init; }
    public long RecordingIntegritySubmittedFrames { get; init; }
    public long RecordingIntegrityEncodedFrames { get; init; }
    public long RecordingIntegrityPacketsWritten { get; init; }
    public long RecordingIntegrityEncoderDroppedFrames { get; init; }
    public long RecordingIntegritySequenceGaps { get; init; }
    public int RecordingIntegrityQueueMaxDepth { get; init; }
    public long RecordingIntegrityQueueOldestFrameAgeMs { get; init; }
    public long RecordingIntegrityBackpressureWaitMs { get; init; }
    public long RecordingIntegrityBackpressureEvents { get; init; }
    public long RecordingIntegrityBackpressureMaxWaitMs { get; init; }
    public string RecordingIntegrityAudioStatus { get; init; } = "Disabled";
    public bool RecordingIntegrityAudioEnabled { get; init; }
    public bool RecordingIntegrityAudioCaptureActive { get; init; }
    public long RecordingIntegrityAudioFramesArrived { get; init; }
    public long RecordingIntegrityAudioFramesWrittenToSink { get; init; }
    public long RecordingIntegrityAudioSamplesEncoded { get; init; }
    public long RecordingIntegrityAudioDropEvents { get; init; }
    public long RecordingIntegrityAudioDiscontinuities { get; init; }
    public long RecordingIntegrityAudioTimestampErrors { get; init; }
    public long RecordingIntegrityAudioCallbackGaps { get; init; }
    public double? RecordingIntegrityAvSyncDriftMs { get; init; }
    public double? RecordingIntegrityAvSyncDriftRateMsPerSec { get; init; }
    public double? RecordingIntegrityEncoderAvSyncDriftMs { get; init; }
    public long? RecordingIntegrityEncoderAvSyncCorrectionSamples { get; init; }
    public string RecordingIntegrityReason { get; init; } = "No recording has completed.";

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
    public bool RecordingEncodingFailed { get; init; }
    public string? RecordingEncodingFailureType { get; init; }
    public string? RecordingEncodingFailureMessage { get; init; }
    public int RecordingVideoQueueCapacity { get; init; }
    public int RecordingVideoQueueMaxDepth { get; init; }
    public long RecordingVideoFramesSubmittedToEncoder { get; init; }
    public long RecordingVideoEncoderPts { get; init; }
    public long RecordingVideoEncoderPacketsWritten { get; init; }
    public long RecordingVideoEncoderDroppedFrames { get; init; }
    public long RecordingVideoSequenceGaps { get; init; }
    public long RecordingVideoQueueOldestFrameAgeMs { get; init; }
    public long RecordingVideoQueueLastLatencyMs { get; init; }
    public int RecordingVideoQueueLatencySampleCount { get; init; }
    public double RecordingVideoQueueLatencyAvgMs { get; init; }
    public double RecordingVideoQueueLatencyP95Ms { get; init; }
    public double RecordingVideoQueueLatencyP99Ms { get; init; }
    public double RecordingVideoQueueLatencyMaxMs { get; init; }
    public long RecordingVideoBackpressureWaitMs { get; init; }
    public long RecordingVideoBackpressureEvents { get; init; }
    public long RecordingVideoBackpressureLastWaitMs { get; init; }
    public long RecordingVideoBackpressureMaxWaitMs { get; init; }
    public int RecordingGpuQueueDepth { get; init; }
    public int RecordingGpuQueueCapacity { get; init; }
    public int RecordingGpuQueueMaxDepth { get; init; }
    public long RecordingGpuFramesEnqueued { get; init; }
    public long RecordingGpuFramesDropped { get; init; }
    public int RecordingCudaQueueDepth { get; init; }
    public int RecordingCudaQueueCapacity { get; init; }
    public int RecordingCudaQueueMaxDepth { get; init; }
    public long RecordingCudaFramesEnqueued { get; init; }
    public long RecordingCudaFramesDropped { get; init; }
    public bool FlashbackEncodingFailed { get; init; }
    public string? FlashbackEncodingFailureType { get; init; }
    public string? FlashbackEncodingFailureMessage { get; init; }
    public bool FatalCleanupInProgress { get; init; }
    public bool FlashbackCleanupInProgress { get; init; }
    public bool FlashbackForceRotateActive { get; init; }
    public bool FlashbackForceRotateRequested { get; init; }
    public bool FlashbackForceRotateDraining { get; init; }
    public int FlashbackVideoQueueCapacity { get; init; }
    public int FlashbackVideoQueueMaxDepth { get; init; }
    public long FlashbackVideoFramesSubmittedToEncoder { get; init; }
    public long FlashbackVideoEncoderPts { get; init; }
    public long FlashbackVideoEncoderPacketsWritten { get; init; }
    public long FlashbackVideoEncoderDroppedFrames { get; init; }
    public long FlashbackVideoSequenceGaps { get; init; }
    public long FlashbackVideoQueueRejectedFrames { get; init; }
    public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
    public long FlashbackVideoQueueOldestFrameAgeMs { get; init; }
    public long FlashbackVideoQueueLastLatencyMs { get; init; }
    public int FlashbackVideoQueueLatencySampleCount { get; init; }
    public double FlashbackVideoQueueLatencyAvgMs { get; init; }
    public double FlashbackVideoQueueLatencyP95Ms { get; init; }
    public double FlashbackVideoQueueLatencyP99Ms { get; init; }
    public double FlashbackVideoQueueLatencyMaxMs { get; init; }
    public long FlashbackVideoBackpressureWaitMs { get; init; }
    public long FlashbackVideoBackpressureEvents { get; init; }
    public long FlashbackVideoBackpressureLastWaitMs { get; init; }
    public long FlashbackVideoBackpressureMaxWaitMs { get; init; }
    public int FlashbackGpuQueueDepth { get; init; }
    public int FlashbackGpuQueueCapacity { get; init; }
    public int FlashbackGpuQueueMaxDepth { get; init; }
    public long FlashbackGpuFramesEnqueued { get; init; }
    public long FlashbackGpuFramesDropped { get; init; }
    public long FlashbackGpuQueueRejectedFrames { get; init; }
    public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;
    public long AudioDropsQueueSaturated { get; init; }
    public long AudioDropsBacklogEviction { get; init; }
    public long AudioChunksDropped { get; init; }
    public long AudioQueueDropsRealtime { get; init; }
    public long AudioQueueDropsFileWriter { get; init; }

    public long RecordingVideoBytes { get; init; }
    public long RecordingAudioBytes { get; init; }
    public long RecordingTotalBytes { get; init; }
    public bool RecordingFileGrowing { get; init; }

    public string? LastOutputPath { get; init; }
    public string LastFinalizeStatus { get; init; } = "None";
    public DateTimeOffset? LastFinalizeUtc { get; init; }
    public bool LastOutputExists { get; init; }
    public long? LastOutputSizeBytes { get; init; }

    public RecordingVerificationResult? LastVerification { get; init; }
}
