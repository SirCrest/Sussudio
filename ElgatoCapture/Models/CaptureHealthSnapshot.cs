using System;
using System.Collections.Generic;

namespace ElgatoCapture.Models;

public sealed record MjpegDecoderHealthSnapshot(
    int WorkerIndex,
    int SampleCount,
    double AvgMs,
    double P95Ms,
    double MaxMs);

public sealed class CaptureHealthSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";
    public bool FlashbackActive { get; init; }
    public long FlashbackBufferedDurationMs { get; init; }
    public int FlashbackSegmentCount { get; init; }
    public long FlashbackDiskBytes { get; init; }
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public long FlashbackPlaybackPositionMs { get; init; }
    public string FlashbackDecoderHwAccel { get; init; } = "N/A";
    public long FlashbackPlaybackFrameCount { get; init; }
    public long FlashbackPlaybackLateFrames { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackAvgFrameMs { get; init; }

    public long RecordingElapsedMs { get; init; }
    public long LastFrameArrivalMs { get; init; }
    public long EstimatedPipelineLatencyMs { get; init; }
    public double ExpectedFrameRate { get; init; }
    public uint? NegotiatedWidth { get; init; }
    public uint? NegotiatedHeight { get; init; }
    public double? NegotiatedFrameRate { get; init; }
    public string? NegotiatedFrameRateArg { get; init; }
    public uint? NegotiatedFrameRateNumerator { get; init; }
    public uint? NegotiatedFrameRateDenominator { get; init; }
    public string? NegotiatedPixelFormat { get; init; }
    public string? RequestedReaderSubtype { get; init; }
    public string? ReaderSourceStreamType { get; init; }
    public string? ReaderSourceSubtype { get; init; }
    public string? FirstObservedFramePixelFormat { get; init; }
    public string? LatestObservedFramePixelFormat { get; init; }
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;
    public SourceTelemetryOrigin SourceTelemetryOrigin { get; init; } = SourceTelemetryOrigin.Unknown;
    public SourceTelemetryConfidence SourceTelemetryConfidence { get; init; } = SourceTelemetryConfidence.Unknown;
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public double? SourceFrameRateExact { get; init; }
    public string? SourceFrameRateArg { get; init; }
    public bool? SourceIsHdr { get; init; }
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
    public bool HdrAutoDowngraded { get; init; }
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
    public int CaptureCadenceSampleCount { get; init; }
    public double CaptureCadenceObservedFps { get; init; }
    public double CaptureCadenceExpectedIntervalMs { get; init; }
    public double CaptureCadenceAverageIntervalMs { get; init; }
    public double CaptureCadenceP95IntervalMs { get; init; }
    public double CaptureCadenceMaxIntervalMs { get; init; }
    public double CaptureCadenceJitterStdDevMs { get; init; }
    public long CaptureCadenceSevereGapCount { get; init; }
    public long CaptureCadenceEstimatedDroppedFrames { get; init; }
    public double CaptureCadenceEstimatedDropPercent { get; init; }
    public int MjpegDecodeSampleCount { get; init; }
    public double MjpegDecodeAvgMs { get; init; }
    public double MjpegDecodeP95Ms { get; init; }
    public double MjpegDecodeMaxMs { get; init; }
    public int MjpegInteropCopySampleCount { get; init; }
    public double MjpegInteropCopyAvgMs { get; init; }
    public double MjpegInteropCopyP95Ms { get; init; }
    public double MjpegInteropCopyMaxMs { get; init; }
    public int MjpegCallbackSampleCount { get; init; }
    public double MjpegCallbackAvgMs { get; init; }
    public double MjpegCallbackP95Ms { get; init; }
    public double MjpegCallbackMaxMs { get; init; }
    public int MjpegDecoderCount { get; init; }
    public int MjpegReorderSampleCount { get; init; }
    public double MjpegReorderAvgMs { get; init; }
    public double MjpegReorderP95Ms { get; init; }
    public double MjpegReorderMaxMs { get; init; }
    public int MjpegPipelineSampleCount { get; init; }
    public double MjpegPipelineAvgMs { get; init; }
    public double MjpegPipelineP95Ms { get; init; }
    public double MjpegPipelineMaxMs { get; init; }
    public long MjpegTotalDecoded { get; init; }
    public long MjpegTotalEmitted { get; init; }
    public long MjpegTotalDropped { get; init; }
    public long MjpegReorderSkips { get; init; }
    public int MjpegReorderBufferDepth { get; init; }
    public MjpegDecoderHealthSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderHealthSnapshot>();

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
    public long AudioDropsQueueSaturated { get; init; }
    public long AudioDropsBacklogEviction { get; init; }
    public long LastVideoEnqueueAgeMs { get; init; }
    public long LastVideoWriteAgeMs { get; init; }

    public long AudioChunksDropped { get; init; }

    // AV Sync diagnostics
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }
}
