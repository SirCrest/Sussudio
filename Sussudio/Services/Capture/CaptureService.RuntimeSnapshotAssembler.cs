using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Pure runtime snapshot DTO construction from already-sampled field groups.
public partial class CaptureService
{
    private static class CaptureRuntimeSnapshotAssembler
    {
        public static CaptureRuntimeSnapshot Build(CaptureRuntimeSnapshotAssemblyFields fields)
        {
            var requestedSettings = fields.RequestedSettings;
            var ingestAudio = fields.IngestAudio;
            var readerTransport = fields.ReaderTransport;
            var hdrPipeline = fields.HdrPipeline;
            var sourceTelemetry = fields.SourceTelemetry;
            var observedTelemetry = fields.ObservedTelemetry;
            var recordingIntegrity = fields.RecordingIntegrity;
            var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
            var observedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount;
            var observedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount;
            var observedP010BitDepthSampleCount = observedTelemetry.ObservedP010BitDepthSampleCount;
            var observedP010Low2BitNonZeroPercent = observedTelemetry.ObservedP010Low2BitNonZeroPercent;
            var observedP010Likely8BitUpscaled = observedTelemetry.ObservedP010Likely8BitUpscaled;
            var observedNonP010FrameCount = observedNv12FrameCount + observedOtherFrameCount;
            var hdrRequested = hdrPipeline.HdrRequested;
            var hdrWarmupState = ResolveHdrWarmupState(
                hdrRequested,
                hdrPipeline.HdrOutputActive,
                fields.IsRecording,
                observedP010FrameCount);

            return new CaptureRuntimeSnapshot
            {
                TimestampUtc = fields.TimestampUtc,
                IsInitialized = fields.IsInitialized,
                IsRecording = fields.IsRecording,
                IsAudioPreviewActive = fields.IsAudioPreviewActive,
                AudioReaderActive = ingestAudio.AudioReaderActive,
                AudioFramesArrived = ingestAudio.AudioFramesArrived,
                AudioFramesWrittenToSink = ingestAudio.AudioFramesWrittenToSink,
                VideoReaderActive = ingestAudio.VideoReaderActive,
                IngestVideoFramesArrived = ingestAudio.IngestVideoFramesArrived,
                IngestVideoFramesWrittenToSink = ingestAudio.IngestVideoFramesWrittenToSink,
                IngestLastVideoFrameAgeMs = ingestAudio.IngestLastVideoFrameAgeMs,
                VideoIngestErrorCount = ingestAudio.VideoIngestErrorCount,
                MemoryPreference = readerTransport.MemoryPreference,
                VideoRequestedSubtype = readerTransport.VideoRequestedSubtype,
                VideoNegotiatedSubtype = readerTransport.VideoNegotiatedSubtype,
                FrameLedgerCapacity = readerTransport.FrameLedgerCapacity,
                FrameLedgerEventCount = readerTransport.FrameLedgerEventCount,
                FrameLedgerDroppedEventCount = readerTransport.FrameLedgerDroppedEventCount,
                FrameLedgerRecentEvents = readerTransport.FrameLedgerRecentEvents,
                PreviewColorMetadata = readerTransport.PreviewColorMetadata,
                MfSourceReaderFramesDelivered = ingestAudio.MfSourceReaderFramesDelivered,
                MfSourceReaderFramesDropped = ingestAudio.MfSourceReaderFramesDropped,
                MfSourceReaderNegotiatedFormat = readerTransport.MfSourceReaderNegotiatedFormat,
                SessionState = fields.SessionState,
                SourceReaderReadOutstanding = ingestAudio.SourceReaderReadOutstanding,
                SourceReaderReadOutstandingMs = ingestAudio.SourceReaderReadOutstandingMs,
                SourceReaderLastFrameTickMs = ingestAudio.SourceReaderLastFrameTickMs,
                SourceReaderFrameChannelDepth = ingestAudio.SourceReaderFrameChannelDepth,
                WasapiCaptureCallbackCount = ingestAudio.WasapiCaptureCallbackCount,
                WasapiCaptureCallbackAvgIntervalMs = ingestAudio.WasapiCaptureCallbackAvgIntervalMs,
                WasapiCaptureCallbackMaxIntervalMs = ingestAudio.WasapiCaptureCallbackMaxIntervalMs,
                WasapiCaptureCallbackSevereGapCount = ingestAudio.WasapiCaptureCallbackSevereGapCount,
                WasapiCaptureAudioDiscontinuityCount = ingestAudio.WasapiCaptureAudioDiscontinuityCount,
                WasapiCaptureAudioTimestampErrorCount = ingestAudio.WasapiCaptureAudioTimestampErrorCount,
                WasapiCaptureAudioGlitchCount = ingestAudio.WasapiCaptureAudioGlitchCount,
                WasapiCaptureCallbackSilenceCount = ingestAudio.WasapiCaptureCallbackSilenceCount,
                WasapiCaptureLastCallbackTickMs = ingestAudio.WasapiCaptureLastCallbackTickMs,
                WasapiCaptureAudioLevelEventsFired = ingestAudio.WasapiCaptureAudioLevelEventsFired,
                WasapiCaptureAudioLevelLastFireTickMs = ingestAudio.WasapiCaptureAudioLevelLastFireTickMs,
                WasapiPlaybackRenderCallbackCount = ingestAudio.WasapiPlaybackRenderCallbackCount,
                WasapiPlaybackRenderSilenceCount = ingestAudio.WasapiPlaybackRenderSilenceCount,
                WasapiPlaybackQueueDepth = ingestAudio.WasapiPlaybackQueueDepth,
                WasapiPlaybackQueueDropCount = ingestAudio.WasapiPlaybackQueueDropCount,
                WasapiPlaybackQueueDurationMs = ingestAudio.WasapiPlaybackQueueDurationMs,
                WasapiPlaybackActiveChunkDurationMs = ingestAudio.WasapiPlaybackActiveChunkDurationMs,
                WasapiPlaybackEndpointQueuedDurationMs = ingestAudio.WasapiPlaybackEndpointQueuedDurationMs,
                WasapiPlaybackBufferedDurationMs = ingestAudio.WasapiPlaybackBufferedDurationMs,
                WasapiPlaybackStreamLatencyMs = ingestAudio.WasapiPlaybackStreamLatencyMs,
                WasapiPlaybackLastRenderTickMs = ingestAudio.WasapiPlaybackLastRenderTickMs,
                WasapiPlaybackTargetVolumePercent = ingestAudio.WasapiPlaybackTargetVolumePercent,
                WasapiPlaybackCurrentVolumePercent = ingestAudio.WasapiPlaybackCurrentVolumePercent,
                WasapiPlaybackOutputPeak = ingestAudio.WasapiPlaybackOutputPeak,
                WasapiPlaybackOutputRms = ingestAudio.WasapiPlaybackOutputRms,
                WasapiPlaybackOutputLevelLastTickMs = ingestAudio.WasapiPlaybackOutputLevelLastTickMs,
                CurrentDeviceId = fields.CurrentDeviceId,
                CurrentDeviceName = fields.CurrentDeviceName,
                ActiveAudioDeviceId = fields.ActiveAudioDeviceId,
                ActiveAudioDeviceName = fields.ActiveAudioDeviceName,
                RequestedWidth = requestedSettings?.Width,
                RequestedHeight = requestedSettings?.Height,
                RequestedFrameRate = requestedSettings?.FrameRate,
                RequestedFrameRateArg = fields.RequestedFrameRateArg,
                RequestedFrameRateNumerator = requestedSettings?.RequestedFrameRateNumerator,
                RequestedFrameRateDenominator = requestedSettings?.RequestedFrameRateDenominator,
                RequestedPixelFormat = requestedSettings?.RequestedPixelFormat,
                RequestedFormat = requestedSettings?.Format.ToString(),
                RequestedQuality = requestedSettings?.Quality.ToString(),
                RequestedAudioEnabled = requestedSettings?.AudioEnabled,
                RequestedHdrEnabled = requestedSettings?.HdrEnabled,
                RequestedHdrMasteringMetadata =
                    !string.IsNullOrWhiteSpace(requestedSettings?.HdrMasterDisplayMetadata) ||
                    ((requestedSettings?.HdrMaxCll ?? 0) > 0 && (requestedSettings?.HdrMaxFall ?? 0) > 0),
                HdrOutputActive = hdrPipeline.HdrOutputActive,
                HdrActivationReason = hdrPipeline.HdrActivationReason,
                HdrRuntimeState = hdrPipeline.HdrRuntimeState,
                HdrReadinessReason = hdrPipeline.HdrReadinessReason,
                HdrWarmupState = hdrWarmupState,
                HdrWarmupRequiredP010Frames = hdrRequested ? 1 : 0,
                HdrWarmupAllowedNonP010Frames = hdrRequested ? 2 : 0,
                HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),
                HdrWarmupObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount)),
                HdrAutoDowngraded = hdrPipeline.HdrAutoDowngraded,
                HdrAutoDowngradeReason = hdrPipeline.HdrAutoDowngradeReason,
                HdrDowngradeCode = hdrPipeline.HdrDowngradeCode,
                HdrRequestedButSourceNot10Bit = hdrPipeline.HdrRequestedButSourceNot10Bit,
                RequestedPipelineMode = hdrPipeline.RequestedPipelineMode,
                ActivePipelineMode = hdrPipeline.ActivePipelineMode,
                PipelineModeMatched = hdrPipeline.PipelineModeMatched,
                PipelineModeStatus = hdrPipeline.PipelineModeStatus,
                PipelineModeReason = hdrPipeline.PipelineModeReason,
                RequestedOutputPath = requestedSettings?.OutputPath,
                ActualWidth = fields.ActualWidth,
                ActualHeight = fields.ActualHeight,
                ActualFrameRate = fields.ActualFrameRate,
                ActualFrameRateArg = fields.ActualFrameRateArg,
                NegotiatedWidth = fields.ActualWidth,
                NegotiatedHeight = fields.ActualHeight,
                NegotiatedFrameRate = fields.ActualFrameRate,
                NegotiatedFrameRateArg = fields.ActualFrameRateArg,
                NegotiatedFrameRateNumerator = fields.NegotiatedFrameRateNumerator,
                NegotiatedFrameRateDenominator = fields.NegotiatedFrameRateDenominator,
                NegotiatedPixelFormat = fields.NegotiatedPixelFormat,
                RequestedReaderSubtype = readerTransport.RequestedReaderSubtype,
                ReaderSourceStreamType = readerTransport.ReaderSourceStreamType,
                ReaderSourceSubtype = readerTransport.ReaderSourceSubtype,
                FirstObservedFramePixelFormat = observedTelemetry.FirstObservedFramePixelFormat,
                LatestObservedFramePixelFormat = observedTelemetry.LatestObservedFramePixelFormat,
                LatestObservedSurfaceFormat = observedTelemetry.LatestObservedSurfaceFormat,
                ObservedP010FrameCount = observedP010FrameCount,
                ObservedNv12FrameCount = observedNv12FrameCount,
                ObservedOtherFrameCount = observedOtherFrameCount,
                ObservedP010BitDepthSampleCount = observedP010BitDepthSampleCount,
                ObservedP010Low2BitNonZeroPercent = observedP010Low2BitNonZeroPercent,
                ObservedP010Likely8BitUpscaled = observedP010Likely8BitUpscaled,
                EncoderInputPixelFormat = hdrPipeline.EncoderInputPixelFormat,
                EncoderOutputPixelFormat = hdrPipeline.EncoderOutputPixelFormat,
                EncoderVideoCodec = hdrPipeline.EncoderVideoCodec,
                EncoderVideoProfile = hdrPipeline.EncoderVideoProfile,
                EncoderTenBitPipelineConfirmed = hdrPipeline.EncoderTenBitPipelineConfirmed,
                MfReadwriteDisableConverters = hdrPipeline.MfConvertersDisabled,
                NegotiatedMediaSubtypeToken = hdrPipeline.NegotiatedMediaSubtypeToken,
                DetectedSourceFrameRate = sourceTelemetry.DetectedSourceFrameRate,
                DetectedSourceFrameRateArg = sourceTelemetry.DetectedSourceFrameRateArg,
                SourceFrameRateOrigin = sourceTelemetry.SourceFrameRateOrigin,
                SourceWidth = sourceTelemetry.SourceWidth,
                SourceHeight = sourceTelemetry.SourceHeight,
                SourceIsHdr = sourceTelemetry.SourceIsHdr,
                SourceVideoFormat = sourceTelemetry.SourceVideoFormat,
                SourceColorimetry = sourceTelemetry.SourceColorimetry,
                SourceQuantization = sourceTelemetry.SourceQuantization,
                SourceHdrTransferFunction = sourceTelemetry.SourceHdrTransferFunction,
                SourceHdrTransferCode = sourceTelemetry.SourceHdrTransferCode,
                SourceFirmware = sourceTelemetry.SourceFirmware,
                SourceAudioFormat = sourceTelemetry.SourceAudioFormat,
                SourceAudioSampleRate = sourceTelemetry.SourceAudioSampleRate,
                SourceInputSource = sourceTelemetry.SourceInputSource,
                SourceUsbHostProtocol = sourceTelemetry.SourceUsbHostProtocol,
                SourceHdcpMode = sourceTelemetry.SourceHdcpMode,
                SourceHdcpVersion = sourceTelemetry.SourceHdcpVersion,
                SourceRxTxHdcpVersion = sourceTelemetry.SourceRxTxHdcpVersion,
                SourceRawTimingHex = sourceTelemetry.SourceRawTimingHex,
                RecordingBackend = fields.RecordingBackend,
                AudioPathMode = requestedSettings?.AudioPathMode.ToString() ?? "None",
                MuxAttempted = false,
                MuxSucceeded = null,
                RecordingIntegrityStatus = recordingIntegrity.Status,
                RecordingIntegrityComplete = recordingIntegrity.Complete,
                RecordingIntegrityBackend = recordingIntegrity.Backend,
                RecordingIntegrityCompletedUtc = recordingIntegrity.CompletedUtc,
                RecordingIntegritySourceFrames = recordingIntegrity.SourceFrames,
                RecordingIntegrityAcceptedFrames = recordingIntegrity.AcceptedFrames,
                RecordingIntegrityPipelineDroppedFrames = recordingIntegrity.PipelineDroppedFrames,
                RecordingIntegrityQueueDroppedFrames = recordingIntegrity.QueueDroppedFrames,
                RecordingIntegritySubmittedFrames = recordingIntegrity.SubmittedFrames,
                RecordingIntegrityEncodedFrames = recordingIntegrity.EncodedFrames,
                RecordingIntegrityPacketsWritten = recordingIntegrity.PacketsWritten,
                RecordingIntegrityEncoderDroppedFrames = recordingIntegrity.EncoderDroppedFrames,
                RecordingIntegritySequenceGaps = recordingIntegrity.SequenceGaps,
                RecordingIntegrityQueueMaxDepth = recordingIntegrity.QueueMaxDepth,
                RecordingIntegrityQueueOldestFrameAgeMs = recordingIntegrity.QueueOldestFrameAgeMs,
                RecordingIntegrityBackpressureWaitMs = recordingIntegrity.BackpressureWaitMs,
                RecordingIntegrityBackpressureEvents = recordingIntegrity.BackpressureEvents,
                RecordingIntegrityBackpressureMaxWaitMs = recordingIntegrity.BackpressureMaxWaitMs,
                RecordingIntegrityAudioStatus = recordingIntegrity.AudioStatus,
                RecordingIntegrityAudioEnabled = recordingIntegrity.AudioEnabled,
                RecordingIntegrityAudioCaptureActive = recordingIntegrity.AudioCaptureActive,
                RecordingIntegrityAudioFramesArrived = recordingIntegrity.AudioFramesArrived,
                RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,
                RecordingIntegrityAudioSamplesEncoded = recordingIntegrity.AudioSamplesEncoded,
                RecordingIntegrityAudioDropEvents = recordingIntegrity.AudioDropEvents,
                RecordingIntegrityAudioDiscontinuities = recordingIntegrity.AudioDiscontinuities,
                RecordingIntegrityAudioTimestampErrors = recordingIntegrity.AudioTimestampErrors,
                RecordingIntegrityAudioCallbackGaps = recordingIntegrity.AudioCallbackGaps,
                RecordingIntegrityAvSyncDriftMs = recordingIntegrity.AvSyncDriftMs,
                RecordingIntegrityAvSyncDriftRateMsPerSec = recordingIntegrity.AvSyncDriftRateMsPerSec,
                RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,
                RecordingIntegrityEncoderAvSyncCorrectionSamples = recordingIntegrity.EncoderAvSyncCorrectionSamples,
                RecordingIntegrityReason = recordingIntegrity.Reason,
                LastOutputPath = fields.LastOutputPath,
                LastFinalizeStatus = fields.LastFinalizeStatus,
                LastFinalizeUtc = fields.LastFinalizeUtc,
                LastPreservedArtifacts = fields.LastPreservedArtifacts,
                FlashbackExportOutputPath = fields.FlashbackExportOutputPath,
                FlashbackExportVerificationFormat = fields.FlashbackExportVerificationFormat,
                FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,
                SourceTelemetryAvailability = sourceTelemetry.Availability,
                SourceTelemetryOriginDetail = sourceTelemetry.OriginDetail,
                SourceTelemetryConfidence = sourceTelemetry.Confidence,
                SourceTelemetryDiagnosticSummary = sourceTelemetry.DiagnosticSummary,
                SourceTelemetryDetails = sourceTelemetry.Details,
                SourceTelemetryTimestampUtc = sourceTelemetry.TimestampUtc,
                SourceTelemetryAgeSeconds = sourceTelemetry.AgeSeconds,
                SourceTelemetryBackend = sourceTelemetry.Backend,
                SourceTelemetrySuppressed = sourceTelemetry.Suppressed,
                SourceTelemetrySuppressedReason = sourceTelemetry.SuppressedReason,
                SourceTelemetryCircuitState = sourceTelemetry.CircuitState,
                TelemetryAlignmentStatus = sourceTelemetry.AlignmentStatus,
                TelemetryAlignmentReason = sourceTelemetry.AlignmentReason,
                AvSyncCaptureDriftMs = fields.RuntimeAvSyncDriftMs,
                AvSyncCaptureDriftRateMsPerSec = fields.RuntimeAvSyncDriftRateMsPerSec,
                AvSyncEncoderDriftMs = fields.RuntimeAvSyncEncoderDriftMs,
                AvSyncEncoderCorrectionSamples = fields.RuntimeAvSyncEncoderCorrectionSamples
            };
        }
    }

    private sealed class CaptureRuntimeSnapshotAssemblyFields
    {
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
        public bool IsInitialized { get; init; }
        public bool IsRecording { get; init; }
        public bool IsAudioPreviewActive { get; init; }
        public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;
        public RuntimeIngestAudioSnapshotFields IngestAudio { get; init; } = new();
        public RuntimeReaderTransportSnapshotFields ReaderTransport { get; init; } = new();
        public RuntimeHdrPipelineSnapshotFields HdrPipeline { get; init; } = new();
        public RuntimeSourceTelemetrySnapshotFields SourceTelemetry { get; init; } = new();
        public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }
        public RuntimeRecordingIntegritySnapshotFields RecordingIntegrity { get; init; } = new();
        public string? CurrentDeviceId { get; init; }
        public string? CurrentDeviceName { get; init; }
        public string? ActiveAudioDeviceId { get; init; }
        public string? ActiveAudioDeviceName { get; init; }
        public CaptureSettings? RequestedSettings { get; init; }
        public string? RequestedFrameRateArg { get; init; }
        public uint? ActualWidth { get; init; }
        public uint? ActualHeight { get; init; }
        public double? ActualFrameRate { get; init; }
        public string? ActualFrameRateArg { get; init; }
        public uint? NegotiatedFrameRateNumerator { get; init; }
        public uint? NegotiatedFrameRateDenominator { get; init; }
        public string? NegotiatedPixelFormat { get; init; }
        public string RecordingBackend { get; init; } = "None";
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; } = "None";
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public IReadOnlyList<string> LastPreservedArtifacts { get; init; } = Array.Empty<string>();
        public string? FlashbackExportOutputPath { get; init; }
        public string? FlashbackExportVerificationFormat { get; init; }
        public string? FlashbackCodecDowngradeReason { get; init; }
        public double? RuntimeAvSyncDriftMs { get; init; }
        public double? RuntimeAvSyncDriftRateMsPerSec { get; init; }
        public double? RuntimeAvSyncEncoderDriftMs { get; init; }
        public long? RuntimeAvSyncEncoderCorrectionSamples { get; init; }
    }
}
