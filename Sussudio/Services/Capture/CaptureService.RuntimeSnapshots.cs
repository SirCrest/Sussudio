using System;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Runtime snapshot projection consumed by UI, automation, and verification.
// Keep this path read-only so frequent polling cannot mutate capture behavior.
public partial class CaptureService
{
    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var wasapiCapture = _wasapiAudioCapture;
        var wasapiPlayback = _wasapiAudioPlayback;
        var ingestAudio = CaptureRuntimeIngestAudioSnapshotFields(
            unifiedVideoCapture,
            sink,
            wasapiCapture,
            wasapiPlayback,
            _isVideoPreviewActive,
            _isRecording);
        var requestedSettings = _activeRecordingSettings ?? _currentSettings;
        var hdrRequested = requestedSettings?.HdrEnabled == true &&
                           requestedSettings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        var requestedPipelineMode = hdrRequested ? "HDR10-PQ" : "SDR";
        var encoderInputPixelFormat = _activeVideoInputPixelFormat;
        var encoderOutputPixelFormat = ResolveEncoderOutputPixelFormat(_recordingContext, requestedSettings);
        var encoderVideoCodec = ResolveEncoderCodecName(requestedSettings);
        var encoderVideoProfile = ResolveEncoderVideoProfile(_recordingContext, requestedSettings);
        bool? encoderTenBitPipelineConfirmed = _isRecording
            ? _recordingContext?.HdrPipelineActive == true
            : null;
        var mfConvertersDisabled = _mfConvertersDisabled;
        var negotiatedMediaSubtypeToken = string.Equals(encoderInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase)
            ? "P010|MFVideoFormat_P010"
            : "NV12";
        var activePipelineMode = _isRecording
            ? (string.Equals(
                encoderInputPixelFormat,
                "p010le",
                StringComparison.OrdinalIgnoreCase)
                ? "HDR10-PQ"
                : "SDR")
            : requestedPipelineMode;
        var pipelineModeMatched = string.Equals(
            requestedPipelineMode,
            activePipelineMode,
            StringComparison.OrdinalIgnoreCase);
        var pipelineModeStatus = _isRecording
            ? (pipelineModeMatched ? "Active" : "Violation")
            : "Ready";
        var pipelineModeReason = pipelineModeMatched
            ? string.Empty
            : $"Requested pipeline '{requestedPipelineMode}', but active encoder ingress is '{activePipelineMode}' " +
              $"(pixel-format={encoderInputPixelFormat ?? "unknown"}).";
        var hdrOutputActive = _isRecording &&
                              string.Equals(
                                  activePipelineMode,
                                  "HDR10-PQ",
                                  StringComparison.OrdinalIgnoreCase);
        var hdrRequestedButSourceNot10Bit = hdrRequested && _latestSourceTelemetry.IsHdr == false;
        var hdrAutoDowngraded = hdrRequested && _isRecording && !pipelineModeMatched;
        var hdrAutoDowngradeReason = hdrAutoDowngraded
            ? pipelineModeReason
            : string.Empty;
        var hdrDowngradeCode = hdrAutoDowngraded ? "encoder-input-not-p010" : string.Empty;
        var hdrRuntimeState = hdrOutputActive
            ? "Active"
            : hdrRequested
                ? (_isRecording ? "Violation" : "Ready")
                : "Inactive";
        var hdrReadinessReason = hdrOutputActive
            ? string.Empty
            : hdrRequested
                ? (_isRecording
                    ? pipelineModeReason
                    : "HDR requested and will activate when recording starts.")
                : string.Empty;
        var hdrActivationReason = hdrOutputActive
            ? "P010 pipeline is active."
            : hdrRequested
                ? (_isRecording
                    ? "HDR requested but the active recording pipeline is not in HDR mode."
                    : "HDR requested and waiting for recording start.")
                : "HDR not requested.";
        var sourceTelemetryTimestampUtc = _latestSourceTelemetry.TimestampUtc;
        var sourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(sourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
        var sourceTelemetryBackend = ResolveSourceTelemetryBackend(_latestSourceTelemetry);
        var sourceTelemetrySuppressedReason = ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry);
        var sourceTelemetrySuppressed = !string.IsNullOrWhiteSpace(sourceTelemetrySuppressedReason);
        var sourceTelemetryCircuitState = ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability, sourceTelemetrySuppressed);
        var sourceFrameRateOrigin = ResolveSourceFrameRateOrigin(_latestSourceTelemetry);
        var (telemetryAlignmentStatus, telemetryAlignmentReason) = ResolveTelemetryAlignment(
            requestedSettings,
            _latestSourceTelemetry,
            _actualWidth,
            _actualHeight,
            _actualFrameRate,
            hdrRequested);
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
        var observedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount;
        var observedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount;
        var observedP010BitDepthSampleCount = observedTelemetry.ObservedP010BitDepthSampleCount;
        var observedP010Low2BitNonZeroPercent = observedTelemetry.ObservedP010Low2BitNonZeroPercent;
        var observedP010Likely8BitUpscaled = observedTelemetry.ObservedP010Likely8BitUpscaled;
        var observedNonP010FrameCount = observedNv12FrameCount + observedOtherFrameCount;
        var hdrWarmupState = ResolveHdrWarmupState(
            hdrRequested,
            hdrOutputActive,
            _isRecording,
            observedP010FrameCount);
        var requestedReaderSubtype = !string.IsNullOrWhiteSpace(requestedSettings?.RequestedPixelFormat)
            ? requestedSettings!.RequestedPixelFormat
            : hdrRequested
                ? "P010"
                : "NV12";
        var mfSourceReaderNegotiatedFormat = unifiedVideoCapture?.NegotiatedFormat ?? _lastMfSourceReaderNegotiatedFormat;
        var negotiatedSubtypeFromSourceReader =
            !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
            mfSourceReaderNegotiatedFormat.Contains("P010", StringComparison.OrdinalIgnoreCase)
                ? "P010"
                : !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
                  mfSourceReaderNegotiatedFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase)
                    ? "NV12"
                    : "unknown";
        var videoNegotiatedSubtype = unifiedVideoCapture != null
            ? (unifiedVideoCapture.IsHighFrameRateMjpegMode ? "MJPG"
                : unifiedVideoCapture.IsP010 ? "P010" : "NV12")
            : negotiatedSubtypeFromSourceReader;
        var readerSourceStreamType = (_isRecording || _isVideoPreviewActive) && unifiedVideoCapture != null
            ? "MfSourceReader"
            : null;
        var previewColorMetadata = (_previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? "None";
        var frameLedger = unifiedVideoCapture?.GetFrameLedgerSummary() ?? FrameLedgerSummary.Empty;
        const bool muxAttempted = false;
        bool? muxSucceeded = null;
        var recordingIntegrity = ResolveRecordingIntegritySummary(unifiedVideoCapture, sink, _flashbackSink);
        var (runtimeAvSyncDriftMs, runtimeAvSyncDriftRate) = ComputeAvSyncDrift();
        var (runtimeAvSyncEncoderDriftMs, runtimeAvSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = _isInitialized,
            IsRecording = _isRecording,
            IsAudioPreviewActive = _isAudioPreviewActive,
            AudioReaderActive = ingestAudio.AudioReaderActive,
            AudioFramesArrived = ingestAudio.AudioFramesArrived,
            AudioFramesWrittenToSink = ingestAudio.AudioFramesWrittenToSink,
            VideoReaderActive = ingestAudio.VideoReaderActive,
            IngestVideoFramesArrived = ingestAudio.IngestVideoFramesArrived,
            IngestVideoFramesWrittenToSink = ingestAudio.IngestVideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = ingestAudio.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = ingestAudio.VideoIngestErrorCount,
            MemoryPreference = ingestAudio.MemoryPreference,
            VideoRequestedSubtype = requestedReaderSubtype ?? "unknown",
            VideoNegotiatedSubtype = videoNegotiatedSubtype,
            FrameLedgerCapacity = frameLedger.Capacity,
            FrameLedgerEventCount = frameLedger.TotalEventsRecorded,
            FrameLedgerDroppedEventCount = frameLedger.EventsDroppedByRetention,
            FrameLedgerRecentEvents = frameLedger.RecentEvents,
            PreviewColorMetadata = previewColorMetadata,
            MfSourceReaderFramesDelivered = ingestAudio.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = ingestAudio.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = mfSourceReaderNegotiatedFormat,
            SessionState = _sessionState,
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
            CurrentDeviceId = _currentDevice?.Id,
            CurrentDeviceName = _currentDevice?.Name,
            ActiveAudioDeviceId = _audioDeviceId,
            ActiveAudioDeviceName = _audioDeviceName,
            RequestedWidth = requestedSettings?.Width,
            RequestedHeight = requestedSettings?.Height,
            RequestedFrameRate = requestedSettings?.FrameRate,
            RequestedFrameRateArg = ResolveRequestedFrameRateArg(requestedSettings, _actualFrameRateArg),
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
            HdrOutputActive = hdrOutputActive,
            HdrActivationReason = hdrActivationReason,
            HdrRuntimeState = hdrRuntimeState,
            HdrReadinessReason = hdrReadinessReason,
            HdrWarmupState = hdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrRequested ? 1 : 0,
            HdrWarmupAllowedNonP010Frames = hdrRequested ? 2 : 0,
            HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),
            HdrWarmupObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount)),
            HdrAutoDowngraded = hdrAutoDowngraded,
            HdrAutoDowngradeReason = hdrAutoDowngradeReason,
            HdrDowngradeCode = hdrDowngradeCode,
            HdrRequestedButSourceNot10Bit = hdrRequestedButSourceNot10Bit,
            RequestedPipelineMode = requestedPipelineMode,
            ActivePipelineMode = activePipelineMode,
            PipelineModeMatched = pipelineModeMatched,
            PipelineModeStatus = pipelineModeStatus,
            PipelineModeReason = pipelineModeReason,
            RequestedOutputPath = requestedSettings?.OutputPath,
            ActualWidth = _actualWidth,
            ActualHeight = _actualHeight,
            ActualFrameRate = _actualFrameRate,
            ActualFrameRateArg = _actualFrameRateArg,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = requestedReaderSubtype,
            ReaderSourceStreamType = readerSourceStreamType,
            ReaderSourceSubtype = _actualPixelFormat,
            FirstObservedFramePixelFormat = observedTelemetry.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = observedTelemetry.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = observedTelemetry.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = observedP010FrameCount,
            ObservedNv12FrameCount = observedNv12FrameCount,
            ObservedOtherFrameCount = observedOtherFrameCount,
            ObservedP010BitDepthSampleCount = observedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = observedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = observedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = encoderInputPixelFormat,
            EncoderOutputPixelFormat = encoderOutputPixelFormat,
            EncoderVideoCodec = encoderVideoCodec,
            EncoderVideoProfile = encoderVideoProfile,
            EncoderTenBitPipelineConfirmed = encoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = mfConvertersDisabled,
            NegotiatedMediaSubtypeToken = negotiatedMediaSubtypeToken,
            DetectedSourceFrameRate = _latestSourceTelemetry.FrameRateExact,
            DetectedSourceFrameRateArg = _latestSourceTelemetry.FrameRateArg,
            SourceFrameRateOrigin = sourceFrameRateOrigin,
            SourceWidth = _latestSourceTelemetry.Width,
            SourceHeight = _latestSourceTelemetry.Height,
            SourceIsHdr = _latestSourceTelemetry.IsHdr,
            SourceVideoFormat = _latestSourceTelemetry.VideoFormat,
            SourceColorimetry = _latestSourceTelemetry.Colorimetry,
            SourceQuantization = _latestSourceTelemetry.Quantization,
            SourceHdrTransferFunction = _latestSourceTelemetry.HdrTransferFunction,
            SourceHdrTransferCode = _latestSourceTelemetry.HdrTransferCode,
            SourceFirmware = _latestSourceTelemetry.Firmware,
            SourceAudioFormat = _latestSourceTelemetry.AudioFormat,
            SourceAudioSampleRate = _latestSourceTelemetry.AudioSampleRate,
            SourceInputSource = _latestSourceTelemetry.InputSource,
            SourceUsbHostProtocol = _latestSourceTelemetry.UsbHostProtocol,
            SourceHdcpMode = _latestSourceTelemetry.HdcpMode,
            SourceHdcpVersion = _latestSourceTelemetry.HdcpVersion,
            SourceRxTxHdcpVersion = _latestSourceTelemetry.RxTxHdcpVersion,
            SourceRawTimingHex = _latestSourceTelemetry.RawTimingHex,
            RecordingBackend = ResolveRecordingBackendName(),
            AudioPathMode = requestedSettings?.AudioPathMode.ToString() ?? "None",
            MuxAttempted = muxAttempted,
            MuxSucceeded = muxSucceeded,
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
            LastOutputPath = _lastOutputPath,
            LastFinalizeStatus = _lastFinalizeStatus,
            LastFinalizeUtc = _lastFinalizeUtc,
            LastPreservedArtifacts = _lastPreservedArtifacts,
            FlashbackExportOutputPath = _flashbackExportOutputPath,
            FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),
            FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(requestedSettings, unifiedVideoCapture),
            SourceTelemetryAvailability = _latestSourceTelemetry.Availability.ToString(),
            SourceTelemetryOriginDetail = _latestSourceTelemetry.OriginDetail,
            SourceTelemetryConfidence = _latestSourceTelemetry.Confidence.ToString(),
            SourceTelemetryDiagnosticSummary = _latestSourceTelemetry.DiagnosticSummary,
            SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,
            SourceTelemetryTimestampUtc = sourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = sourceTelemetryAgeSeconds,
            SourceTelemetryBackend = sourceTelemetryBackend,
            SourceTelemetrySuppressed = sourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = sourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = sourceTelemetryCircuitState,
            TelemetryAlignmentStatus = telemetryAlignmentStatus,
            TelemetryAlignmentReason = telemetryAlignmentReason,
            AvSyncCaptureDriftMs = runtimeAvSyncDriftMs,
            AvSyncCaptureDriftRateMsPerSec = runtimeAvSyncDriftRate,
            AvSyncEncoderDriftMs = runtimeAvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples = runtimeAvSyncEncoderCorrectionSamples
        };
    }
}
