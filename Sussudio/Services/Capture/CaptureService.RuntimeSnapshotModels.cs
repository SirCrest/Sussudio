using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Private runtime snapshot handoff models shared by runtime projection partials
// and the final DTO assembler.
public partial class CaptureService
{
    private sealed class RuntimeIngestAudioSnapshotFields
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long IngestVideoFramesArrived { get; init; }
        public long IngestVideoFramesWrittenToSink { get; init; }
        public long IngestLastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
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
    }

    private sealed class RuntimeReaderTransportSnapshotFields
    {
        public string MemoryPreference { get; init; } = "Cpu";
        public string VideoRequestedSubtype { get; init; } = "unknown";
        public string VideoNegotiatedSubtype { get; init; } = "unknown";
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();
        public string PreviewColorMetadata { get; init; } = "None";
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public string? RequestedReaderSubtype { get; init; }
        public string? ReaderSourceStreamType { get; init; }
        public string? ReaderSourceSubtype { get; init; }
    }

    private sealed class RuntimeHdrPipelineSnapshotFields
    {
        public bool HdrRequested { get; init; }
        public string? EncoderInputPixelFormat { get; init; }
        public string? EncoderOutputPixelFormat { get; init; }
        public string? EncoderVideoCodec { get; init; }
        public string? EncoderVideoProfile { get; init; }
        public bool? EncoderTenBitPipelineConfirmed { get; init; }
        public bool MfConvertersDisabled { get; init; }
        public string NegotiatedMediaSubtypeToken { get; init; } = "NV12";
        public bool HdrOutputActive { get; init; }
        public string HdrActivationReason { get; init; } = "Unknown";
        public string HdrRuntimeState { get; init; } = "Inactive";
        public string HdrReadinessReason { get; init; } = string.Empty;
        public bool HdrAutoDowngraded { get; init; }
        public string HdrAutoDowngradeReason { get; init; } = string.Empty;
        public string HdrDowngradeCode { get; init; } = string.Empty;
        public bool HdrRequestedButSourceNot10Bit { get; init; }
        public string RequestedPipelineMode { get; init; } = "SDR";
        public string ActivePipelineMode { get; init; } = "SDR";
        public bool PipelineModeMatched { get; init; } = true;
        public string PipelineModeStatus { get; init; } = "Ready";
        public string PipelineModeReason { get; init; } = string.Empty;
    }

    private sealed class RuntimeHdrWarmupSnapshotFields
    {
        public string State { get; init; } = "NotRequested";
        public int RequiredP010Frames { get; init; }
        public int AllowedNonP010Frames { get; init; }
        public int ObservedP010Frames { get; init; }
        public int ObservedNonP010Frames { get; init; }
    }

    private sealed class RuntimeSourceTelemetrySnapshotFields
    {
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
        public string Availability { get; init; } = "Unknown";
        public string OriginDetail { get; init; } = "Unknown";
        public string Confidence { get; init; } = "Unknown";
        public string? DiagnosticSummary { get; init; }
        public IReadOnlyList<SourceTelemetryDetailEntry> Details { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();
        public DateTimeOffset? TimestampUtc { get; init; }
        public int? AgeSeconds { get; init; }
        public string Backend { get; init; } = "Unknown";
        public bool Suppressed { get; init; }
        public string? SuppressedReason { get; init; }
        public string CircuitState { get; init; } = "Closed";
        public string AlignmentStatus { get; init; } = "Unknown";
        public string AlignmentReason { get; init; } = string.Empty;
    }

    private sealed class RuntimeRecordingIntegritySnapshotFields
    {
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
        public string Reason { get; init; } = "No recording has completed.";
    }
}
