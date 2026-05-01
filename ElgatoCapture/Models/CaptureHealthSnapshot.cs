using System;
using System.Collections.Generic;

namespace ElgatoCapture.Models;

public sealed record MjpegDecoderHealthSnapshot(
    int WorkerIndex,
    int SampleCount,
    double AvgMs,
    double P95Ms,
    double MaxMs);

/// <summary>
/// Full health snapshot extending <see cref="CaptureDiagnosticsSnapshot"/> with
/// flashback playback/encoder detail, source signal metadata, and AV-sync data.
/// </summary>
public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot
{
    // ── Flashback detail (beyond base counts) ──────────────────────────
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public string? EncoderCodecName { get; init; }
    public uint EncoderTargetBitRate { get; init; }
    public int EncoderWidth { get; init; }
    public int EncoderHeight { get; init; }
    public double EncoderFrameRate { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public long FlashbackPlaybackPositionMs { get; init; }
    public string FlashbackDecoderHwAccel { get; init; } = "N/A";
    public long FlashbackPlaybackFrameCount { get; init; }
    public long FlashbackPlaybackLateFrames { get; init; }
    public long FlashbackPlaybackDroppedFrames { get; init; }
    public long FlashbackPlaybackSegmentSwitches { get; init; }
    public long FlashbackPlaybackFmp4Reopens { get; init; }
    public long FlashbackPlaybackWriteHeadWaits { get; init; }
    public long FlashbackPlaybackNearLiveSnaps { get; init; }
    public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
    public long FlashbackPlaybackSubmitFailures { get; init; }
    public long FlashbackPlaybackLastSegmentSwitchUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastFmp4ReopenUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackAvgFrameMs { get; init; }
    public int FlashbackPlaybackCadenceSampleCount { get; init; }
    public double FlashbackPlaybackP95FrameMs { get; init; }
    public double FlashbackPlaybackP99FrameMs { get; init; }
    public double FlashbackPlaybackMaxFrameMs { get; init; }
    public long FlashbackPlaybackSlowFrames { get; init; }
    public double FlashbackPlaybackSlowFramePercent { get; init; }
    public double FlashbackPlaybackOnePercentLowFps { get; init; }
    public int FlashbackPlaybackDecodeSampleCount { get; init; }
    public double FlashbackPlaybackDecodeAvgMs { get; init; }
    public double FlashbackPlaybackDecodeP95Ms { get; init; }
    public double FlashbackPlaybackDecodeP99Ms { get; init; }
    public double FlashbackPlaybackDecodeMaxMs { get; init; }
    public double FlashbackAvDriftMs { get; init; }
    public bool FlashbackPlaybackThreadAlive { get; init; }
    public long FlashbackPlaybackCommandsEnqueued { get; init; }
    public long FlashbackPlaybackCommandsProcessed { get; init; }
    public long FlashbackPlaybackCommandsDropped { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReady { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalesced { get; init; }
    public int FlashbackPlaybackCommandQueueCapacity { get; init; }
    public int FlashbackPlaybackPendingCommands { get; init; }
    public int FlashbackPlaybackMaxPendingCommands { get; init; }
    public long FlashbackPlaybackLastCommandQueueLatencyMs { get; init; }
    public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
    public string FlashbackPlaybackLastCommandQueued { get; init; } = "None";
    public string FlashbackPlaybackLastCommandProcessed { get; init; } = "None";
    public long FlashbackPlaybackLastCommandQueuedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandProcessedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;

    // ── Export ──────────────────────────────────────────────────────────
    public bool FlashbackExportActive { get; init; }
    public long FlashbackExportId { get; init; }
    public string FlashbackExportStatus { get; init; } = "NotStarted";
    public string FlashbackExportOutputPath { get; init; } = string.Empty;
    public long FlashbackExportStartedUtcUnixMs { get; init; }
    public long FlashbackExportLastProgressUtcUnixMs { get; init; }
    public long FlashbackExportCompletedUtcUnixMs { get; init; }
    public long FlashbackExportElapsedMs { get; init; }
    public long FlashbackExportLastProgressAgeMs { get; init; }
    public long FlashbackExportOutputBytes { get; init; }
    public double FlashbackExportThroughputBytesPerSec { get; init; }
    public int FlashbackExportSegmentsProcessed { get; init; }
    public int FlashbackExportTotalSegments { get; init; }
    public double FlashbackExportPercent { get; init; }
    public long FlashbackExportInPointMs { get; init; }
    public long FlashbackExportOutPointMs { get; init; }
    public string FlashbackExportMessage { get; init; } = string.Empty;
    public string FlashbackExportFailureKind { get; init; } = string.Empty;
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }

    // ── Source signal extended metadata ─────────────────────────────────
    public string? SourceVideoFormat { get; init; }
    public string? SourceColorimetry { get; init; }
    public string? SourceQuantization { get; init; }
    public string? SourceHdrTransferFunction { get; init; }
    public int? SourceHdrTransferCode { get; init; }
    public string? SourceFirmware { get; init; }
    public string? SourceAudioFormat { get; init; }
    public string? SourceAudioSampleRate { get; init; }
    public string? SourceInputSource { get; init; }
    public string? SourceUsbHostProtocol { get; init; }
    public string? SourceHdcpMode { get; init; }
    public string? SourceHdcpVersion { get; init; }
    public string? SourceRxTxHdcpVersion { get; init; }
    public string? SourceRawTimingHex { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();

    // ── Queue age / enqueue tracking ───────────────────────────────────
    public long LastVideoEnqueueAgeMs { get; init; }
    public long LastVideoWriteAgeMs { get; init; }

    // ── AV Sync diagnostics ────────────────────────────────────────────
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }
}
