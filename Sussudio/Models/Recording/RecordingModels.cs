using System;

namespace Sussudio.Models;

// Cached FFmpeg encoder capability snapshot used to enable or disable recording
// format choices.
public sealed class EncoderSupport
{
    public bool HasH264Nvenc { get; init; }
    public bool HasHevcNvenc { get; init; }
    public bool HasAv1Nvenc { get; init; }

    public bool HasLibX264 { get; init; }
    public bool HasLibX265 { get; init; }
    public bool HasLibSvtAv1 { get; init; }
    public bool HasLibAomAv1 { get; init; }

    public bool HasH264 => HasH264Nvenc || HasLibX264;
    public bool HasHevc => HasHevcNvenc || HasLibX265;
    public bool HasAv1 => HasAv1Nvenc || HasLibSvtAv1 || HasLibAomAv1;

    public string? PreferredAv1Encoder
        => HasAv1Nvenc ? "av1_nvenc"
        : HasLibSvtAv1 ? "libsvtav1"
        : HasLibAomAv1 ? "libaom-av1"
        : null;

    public static EncoderSupport Empty { get; } = new();
}

// End-of-recording counter comparison used to explain whether the capture,
// encoder, and audio paths stayed continuous.
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

// Video queue behavior knob for recording pipelines that support latency
// constrained buffering.
public enum VideoFrameDropPolicy
{
    DropOldest,
    DropNewest
}

// Desired queue/latency policy for recording sinks. Active sinks must opt into
// these values explicitly; this model alone does not change queue behavior.
public sealed class RecordingPipelineOptions
{
    public int TargetVideoLatencyMs { get; set; } = 250;
    public int MinBufferedVideoFrames { get; set; } = 4;
    public int MaxBufferedVideoFrames { get; set; } = 30;
    public VideoFrameDropPolicy VideoDropPolicy { get; set; } = VideoFrameDropPolicy.DropOldest;

    public int ResolveVideoQueueCapacity(double frameRate)
    {
        var safeFrameRate = frameRate > 0 ? frameRate : 60;
        var byLatency = (int)Math.Ceiling((safeFrameRate * Math.Max(50, TargetVideoLatencyMs)) / 1000.0);
        var min = Math.Max(1, MinBufferedVideoFrames);
        var max = Math.Max(min, MaxBufferedVideoFrames);
        return Math.Clamp(byLatency, min, max);
    }
}

public readonly struct RecordingStats
{
    public RecordingStats(long videoBytes, long audioBytes, bool isFlashbackEstimate = false, bool isFailure = false)
    {
        VideoBytes = videoBytes;
        AudioBytes = audioBytes;
        IsFlashbackEstimate = isFlashbackEstimate;
        IsFailure = isFailure;
    }

    public long VideoBytes { get; }
    public long AudioBytes { get; }
    public long TotalBytes => VideoBytes + AudioBytes;

    /// <summary>
    /// True when the bytes come from the flashback buffer (estimated, not final file size).
    /// </summary>
    public bool IsFlashbackEstimate { get; }

    /// <summary>
    /// True when the snapshot couldn't be computed (exception caught). Distinguishes
    /// legitimate zero (no recording) from swallowed failure that previously read as zero.
    /// </summary>
    public bool IsFailure { get; }
}
