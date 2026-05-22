using System;
using System.Collections.Generic;

namespace Sussudio.Models;

public sealed class CaptureRuntimeSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool IsInitialized { get; init; }
    public bool IsRecording { get; init; }
    public bool IsAudioPreviewActive { get; init; }
    public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;
    public string? CurrentDeviceId { get; init; }
    public string? CurrentDeviceName { get; init; }
    public string? ActiveAudioDeviceId { get; init; }
    public string? ActiveAudioDeviceName { get; init; }
    public string? RequestedOutputPath { get; init; }

    // Ingest and audio diagnostics
    public bool AudioReaderActive { get; init; }
    public long AudioFramesArrived { get; init; }
    public long AudioFramesWrittenToSink { get; init; }
    public bool VideoReaderActive { get; init; }
    public long IngestVideoFramesArrived { get; init; }
    public long IngestVideoFramesWrittenToSink { get; init; }
    public long IngestLastVideoFrameAgeMs { get; init; }
    public long VideoIngestErrorCount { get; init; }
    public bool SourceReaderReadOutstanding { get; init; }
    public long SourceReaderReadOutstandingMs { get; init; }
    public long SourceReaderLastFrameTickMs { get; init; }
    public int SourceReaderFrameChannelDepth { get; init; }
    public long WasapiCaptureCallbackCount { get; init; }
    public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
    public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
    public long WasapiCaptureCallbackSevereGapCount { get; init; }
    public long WasapiCaptureAudioDiscontinuityCount { get; init; }
    public long WasapiCaptureAudioTimestampErrorCount { get; init; }
    public long WasapiCaptureAudioGlitchCount { get; init; }
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public double WasapiPlaybackQueueDurationMs { get; init; }
    public double WasapiPlaybackActiveChunkDurationMs { get; init; }
    public double WasapiPlaybackEndpointQueuedDurationMs { get; init; }
    public double WasapiPlaybackBufferedDurationMs { get; init; }
    public double WasapiPlaybackStreamLatencyMs { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }
    public double WasapiPlaybackTargetVolumePercent { get; init; }
    public double WasapiPlaybackCurrentVolumePercent { get; init; }
    public double WasapiPlaybackOutputPeak { get; init; }
    public double WasapiPlaybackOutputRms { get; init; }
    public long WasapiPlaybackOutputLevelLastTickMs { get; init; }

    // Reader transport diagnostics
    public long MfSourceReaderFramesDelivered { get; init; }
    public long MfSourceReaderFramesDropped { get; init; }
    public string? MfSourceReaderNegotiatedFormat { get; init; }
    public string MemoryPreference { get; init; } = "Cpu";
    public string VideoRequestedSubtype { get; init; } = "unknown";
    public string VideoNegotiatedSubtype { get; init; } = "unknown";
    public int FrameLedgerCapacity { get; init; }
    public long FrameLedgerEventCount { get; init; }
    public long FrameLedgerDroppedEventCount { get; init; }
    public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();
    public string PreviewColorMetadata { get; init; } = "None";

    // Capture format diagnostics
    public uint? RequestedWidth { get; init; }
    public uint? RequestedHeight { get; init; }
    public double? RequestedFrameRate { get; init; }
    public string? RequestedFrameRateArg { get; init; }
    public uint? RequestedFrameRateNumerator { get; init; }
    public uint? RequestedFrameRateDenominator { get; init; }
    public string? RequestedPixelFormat { get; init; }
    public string? RequestedFormat { get; init; }
    public string? RequestedQuality { get; init; }
    public bool? RequestedAudioEnabled { get; init; }
    public uint? ActualWidth { get; init; }
    public uint? ActualHeight { get; init; }
    public double? ActualFrameRate { get; init; }
    public string? ActualFrameRateArg { get; init; }
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
    public string? LatestObservedSurfaceFormat { get; init; }
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public long ObservedP010BitDepthSampleCount { get; init; }
    public double ObservedP010Low2BitNonZeroPercent { get; init; }
    public bool? ObservedP010Likely8BitUpscaled { get; init; }
    public string? EncoderInputPixelFormat { get; init; }
    public string? EncoderOutputPixelFormat { get; init; }
    public string? EncoderVideoCodec { get; init; }
    public string? EncoderVideoProfile { get; init; }
    public bool? EncoderTenBitPipelineConfirmed { get; init; }
    public bool? MfReadwriteDisableConverters { get; init; }
    public string? NegotiatedMediaSubtypeToken { get; init; }

    // HDR pipeline diagnostics
    public bool? RequestedHdrEnabled { get; init; }
    public bool? RequestedHdrMasteringMetadata { get; init; }
    public bool HdrOutputActive { get; init; }
    public string HdrActivationReason { get; init; } = "Unknown";
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string HdrWarmupState { get; init; } = "NotStarted";
    public int HdrWarmupRequiredP010Frames { get; init; }
    public int HdrWarmupAllowedNonP010Frames { get; init; }
    public int HdrWarmupObservedP010Frames { get; init; }
    public int HdrWarmupObservedNonP010Frames { get; init; }
    public bool HdrAutoDowngraded { get; init; }
    public string HdrDowngradeCode { get; init; } = string.Empty;
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
    public bool HdrRequestedButSourceNot10Bit { get; init; }
    public string RequestedPipelineMode { get; init; } = "SDR";
    public string ActivePipelineMode { get; init; } = "SDR";
    public bool PipelineModeMatched { get; init; } = true;
    public string PipelineModeStatus { get; init; } = "Ready";
    public string PipelineModeReason { get; init; } = string.Empty;
    public string TelemetryAlignmentStatus { get; init; } = "Unknown";
    public string TelemetryAlignmentReason { get; init; } = string.Empty;

    // Source telemetry diagnostics
    public double? DetectedSourceFrameRate { get; init; }
    public string? DetectedSourceFrameRateArg { get; init; }
    public string SourceFrameRateOrigin { get; init; } = "Unknown";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
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
    public string SourceTelemetryAvailability { get; init; } = "Unknown";
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string SourceTelemetryConfidence { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public int? SourceTelemetryAgeSeconds { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";

    // A/V sync diagnostics
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }

    // Recording diagnostics
    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public bool MuxAttempted { get; init; }
    public bool? MuxSucceeded { get; init; }
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
    public string? LastOutputPath { get; init; }
    public string LastFinalizeStatus { get; init; } = "None";
    public DateTimeOffset? LastFinalizeUtc { get; init; }
    public IReadOnlyList<string> LastPreservedArtifacts { get; init; } = Array.Empty<string>();
    public string? FlashbackExportOutputPath { get; init; }
    public string? FlashbackExportVerificationFormat { get; init; }
    public string? FlashbackCodecDowngradeReason { get; init; }
}
