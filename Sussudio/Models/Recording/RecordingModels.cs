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

    /// <summary>
    /// Maps a <see cref="RecordingFormat"/> to the corresponding NVENC encoder codec name
    /// (e.g. "hevc_nvenc", "av1_nvenc", "h264_nvenc").
    /// </summary>
    public static string MapNvencCodecName(RecordingFormat format)
    {
        return format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => "av1_nvenc",
            _ => "h264_nvenc"
        };
    }
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

public readonly struct RecordingStats
{
    public RecordingStats(
        long videoBytes,
        long audioBytes,
        bool isFlashbackEstimate = false,
        bool isFailure = false)
        : this(videoBytes, audioBytes, isFlashbackEstimate, isFailure, default, 0)
    {
    }

    public RecordingStats(
        long videoBytes,
        long audioBytes,
        bool isFlashbackEstimate,
        bool isFailure,
        DateTimeOffset timestampUtc,
        long captureSessionEpoch)
    {
        VideoBytes = videoBytes;
        AudioBytes = audioBytes;
        IsFlashbackEstimate = isFlashbackEstimate;
        IsFailure = isFailure;
        TimestampUtc = timestampUtc == default ? DateTimeOffset.UtcNow : timestampUtc;
        CaptureSessionEpoch = captureSessionEpoch;
    }

    public DateTimeOffset TimestampUtc { get; }
    public long CaptureSessionEpoch { get; }
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
