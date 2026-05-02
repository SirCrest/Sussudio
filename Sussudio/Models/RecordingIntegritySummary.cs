using System;

namespace ElgatoCapture.Models;

public sealed record RecordingIntegritySummary
{
    public static RecordingIntegritySummary NotStarted { get; } = new()
    {
        Status = "NotStarted",
        Backend = "None",
        Reason = "No recording has completed."
    };

    public string Status { get; init; } = "NotStarted";
    public bool Complete { get; init; }
    public string Backend { get; init; } = "None";
    public DateTimeOffset? CompletedUtc { get; init; }
    public long SourceFrames { get; init; }
    public long AcceptedFrames { get; init; }
    public long PipelineDroppedFrames { get; init; }
    public long QueueDroppedFrames { get; init; }
    public long SubmittedFrames { get; init; }
    public long EncodedFrames { get; init; }
    public long PacketsWritten { get; init; }
    public long EncoderDroppedFrames { get; init; }
    public long SequenceGaps { get; init; }
    public int QueueMaxDepth { get; init; }
    public long QueueOldestFrameAgeMs { get; init; }
    public long BackpressureWaitMs { get; init; }
    public long BackpressureEvents { get; init; }
    public long BackpressureMaxWaitMs { get; init; }
    public string AudioStatus { get; init; } = "Disabled";
    public bool AudioEnabled { get; init; }
    public bool AudioCaptureActive { get; init; }
    public long AudioFramesArrived { get; init; }
    public long AudioFramesWrittenToSink { get; init; }
    public long AudioSamplesEncoded { get; init; }
    public long AudioDropEvents { get; init; }
    public long AudioDiscontinuities { get; init; }
    public long AudioTimestampErrors { get; init; }
    public long AudioCallbackGaps { get; init; }
    public double? AvSyncDriftMs { get; init; }
    public double? AvSyncDriftRateMsPerSec { get; init; }
    public double? EncoderAvSyncDriftMs { get; init; }
    public long? EncoderAvSyncCorrectionSamples { get; init; }
    public string Reason { get; init; } = string.Empty;
}
