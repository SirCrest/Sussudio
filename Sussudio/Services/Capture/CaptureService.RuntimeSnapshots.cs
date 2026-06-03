using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Runtime snapshot projection consumed by UI, automation, and verification.
// Keep this path read-only so frequent polling cannot mutate capture behavior.
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

    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var sessionGeneration = CaptureSnapshotProducerEpoch();
        var latestSourceTelemetry = _latestSourceTelemetry;
        var sink = _recordingBackend.LibAvSink;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var wasapiCapture = _previewAudioGraph.ProgramCapture;
        var wasapiPlayback = _previewAudioGraph.Playback;
        var ingestAudio = CaptureRuntimeIngestAudioSnapshotFields(
            unifiedVideoCapture,
            sink,
            wasapiCapture,
            wasapiPlayback,
            _isVideoPreviewActive,
            _isRecording);
        var requestedSettings = _recordingBackend.SettingsSnapshot ?? _currentSettings;
        var hdrPipeline = CaptureRuntimeHdrPipelineSnapshotFields(
            requestedSettings,
            _activeVideoInputPixelFormat,
            _recordingBackend.Context,
            _isRecording,
            latestSourceTelemetry,
            _mfConvertersDisabled);
        var hdrRequested = hdrPipeline.HdrRequested;
        var sourceTelemetry = CaptureRuntimeSourceTelemetrySnapshotFields(
            requestedSettings,
            latestSourceTelemetry,
            _actualWidth,
            _actualHeight,
            _actualFrameRate,
            hdrRequested);
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var hdrWarmup = CaptureRuntimeHdrWarmupSnapshotFields(
            hdrPipeline,
            _isRecording,
            observedTelemetry);
        var readerTransport = CaptureRuntimeReaderTransportSnapshotFields(
            requestedSettings,
            hdrRequested,
            unifiedVideoCapture,
            _isVideoPreviewActive,
            _isRecording,
            _videoPipeline.PreviewFrameSink,
            _actualPixelFormat,
            _lastMfSourceReaderNegotiatedFormat);
        var recordingIntegrity = CaptureRuntimeRecordingIntegritySnapshotFields(
            ResolveRecordingIntegritySummary(unifiedVideoCapture, sink, _flashbackBackend.Sink));
        var (runtimeAvSyncDriftMs, runtimeAvSyncDriftRate) = ComputeAvSyncDrift();
        var (runtimeAvSyncEncoderDriftMs, runtimeAvSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();

        return CaptureRuntimeSnapshotAssembler.Build(new CaptureRuntimeSnapshotAssemblyFields
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            CaptureSessionEpoch = sessionGeneration,
            SourceTelemetryEpoch = latestSourceTelemetry.TelemetryEpoch,
            IsInitialized = _isInitialized,
            IsRecording = _isRecording,
            IsAudioPreviewActive = _isAudioPreviewActive,
            SessionState = CurrentSessionState,
            IngestAudio = ingestAudio,
            ReaderTransport = readerTransport,
            HdrPipeline = hdrPipeline,
            HdrWarmup = hdrWarmup,
            SourceTelemetry = sourceTelemetry,
            ObservedTelemetry = observedTelemetry,
            RecordingIntegrity = recordingIntegrity,
            CurrentDeviceId = _currentDevice?.Id,
            CurrentDeviceName = _currentDevice?.Name,
            ActiveAudioDeviceId = _audioDeviceId,
            ActiveAudioDeviceName = _audioDeviceName,
            RequestedSettings = requestedSettings,
            RequestedFrameRateArg = ResolveRequestedFrameRateArg(requestedSettings, _actualFrameRateArg),
            ActualWidth = _actualWidth,
            ActualHeight = _actualHeight,
            ActualFrameRate = _actualFrameRate,
            ActualFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RecordingBackend = ResolveRecordingBackendName(),
            LastOutputPath = _lastOutputPath,
            LastFinalizeStatus = _lastFinalizeStatus,
            LastFinalizeUtc = _lastFinalizeUtc,
            LastPreservedArtifacts = _lastPreservedArtifacts,
            FlashbackExportOutputPath = _flashbackExportOutputPath,
            FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),
            FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(requestedSettings, unifiedVideoCapture),
            RuntimeAvSyncDriftMs = runtimeAvSyncDriftMs,
            RuntimeAvSyncDriftRateMsPerSec = runtimeAvSyncDriftRate,
            RuntimeAvSyncEncoderDriftMs = runtimeAvSyncEncoderDriftMs,
            RuntimeAvSyncEncoderCorrectionSamples = runtimeAvSyncEncoderCorrectionSamples
        });
    }

    private RuntimeIngestAudioSnapshotFields CaptureRuntimeIngestAudioSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture,
        LibAvRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        WasapiAudioPlayback? wasapiPlayback,
        bool videoPreviewActive,
        bool recordingActive)
    {
        var (wasapiCaptureCallbackAvgMs, wasapiCaptureCallbackMaxMs) =
            wasapiCapture?.GetCaptureCallbackIntervalSnapshot() ?? (0d, 0d);

        return new RuntimeIngestAudioSnapshotFields
        {
            AudioReaderActive = wasapiCapture?.IsCapturing ?? false,
            AudioFramesArrived = wasapiCapture?.AudioFramesArrived ?? 0,
            AudioFramesWrittenToSink = wasapiCapture?.AudioFramesWrittenToSink ?? 0,
            VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive),
            IngestVideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            IngestVideoFramesWrittenToSink = unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
            IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoIngestErrorCount = unifiedVideoCapture?.VideoFramesDropped ?? 0,
            MfSourceReaderFramesDelivered = unifiedVideoCapture?.VideoFramesArrived ?? _lastMfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = unifiedVideoCapture?.VideoFramesDropped ?? _lastMfSourceReaderFramesDropped,
            SourceReaderReadOutstanding = unifiedVideoCapture?.SourceReaderReadOutstanding ?? false,
            SourceReaderReadOutstandingMs = unifiedVideoCapture?.SourceReaderReadOutstandingMs ?? 0,
            SourceReaderLastFrameTickMs = unifiedVideoCapture?.SourceReaderLastFrameTickMs ?? 0,
            SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0,
            WasapiCaptureCallbackCount = wasapiCapture?.CaptureCallbackCount ?? 0,
            WasapiCaptureCallbackAvgIntervalMs = wasapiCaptureCallbackAvgMs,
            WasapiCaptureCallbackMaxIntervalMs = wasapiCaptureCallbackMaxMs,
            WasapiCaptureCallbackSevereGapCount = wasapiCapture?.CaptureCallbackSevereGapCount ?? 0,
            WasapiCaptureAudioDiscontinuityCount = wasapiCapture?.AudioDataDiscontinuityCount ?? 0,
            WasapiCaptureAudioTimestampErrorCount = wasapiCapture?.AudioTimestampErrorCount ?? 0,
            WasapiCaptureAudioGlitchCount = wasapiCapture?.AudioGlitchCount ?? 0,
            WasapiCaptureCallbackSilenceCount = wasapiCapture?.CaptureCallbackSilenceCount ?? 0,
            WasapiCaptureLastCallbackTickMs = wasapiCapture?.LastCaptureCallbackTickMs ?? 0,
            WasapiCaptureAudioLevelEventsFired = wasapiCapture?.AudioLevelEventsFired ?? 0,
            WasapiCaptureAudioLevelLastFireTickMs = wasapiCapture?.AudioLevelEventsLastFireTickMs ?? 0,
            WasapiPlaybackRenderCallbackCount = wasapiPlayback?.RenderCallbackCount ?? 0,
            WasapiPlaybackRenderSilenceCount = wasapiPlayback?.RenderSilenceCount ?? 0,
            WasapiPlaybackQueueDepth = wasapiPlayback?.PlaybackQueueDepth ?? 0,
            WasapiPlaybackQueueDropCount = wasapiPlayback?.PlaybackQueueDropCount ?? 0,
            WasapiPlaybackQueueDurationMs = wasapiPlayback?.PlaybackQueueDurationMs ?? 0,
            WasapiPlaybackActiveChunkDurationMs = wasapiPlayback?.PlaybackActiveChunkDurationMs ?? 0,
            WasapiPlaybackEndpointQueuedDurationMs = wasapiPlayback?.PlaybackEndpointQueuedDurationMs ?? 0,
            WasapiPlaybackBufferedDurationMs = wasapiPlayback?.PlaybackBufferedDurationMs ?? 0,
            WasapiPlaybackStreamLatencyMs = wasapiPlayback?.PlaybackStreamLatencyMs ?? 0,
            WasapiPlaybackLastRenderTickMs = wasapiPlayback?.LastRenderCallbackTickMs ?? 0,
            WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0,
            WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0,
            WasapiPlaybackOutputPeak = wasapiPlayback?.LastOutputPeak ?? 0,
            WasapiPlaybackOutputRms = wasapiPlayback?.LastOutputRms ?? 0,
            WasapiPlaybackOutputLevelLastTickMs = wasapiPlayback?.LastOutputLevelTickMs ?? 0
        };
    }

    private static RuntimeReaderTransportSnapshotFields CaptureRuntimeReaderTransportSnapshotFields(
        CaptureSettings? requestedSettings,
        bool hdrRequested,
        UnifiedVideoCapture? unifiedVideoCapture,
        bool videoPreviewActive,
        bool recordingActive,
        IPreviewFrameSink? previewFrameSink,
        string? actualPixelFormat,
        string? lastMfSourceReaderNegotiatedFormat)
    {
        var requestedReaderSubtype = !string.IsNullOrWhiteSpace(requestedSettings?.RequestedPixelFormat)
            ? requestedSettings!.RequestedPixelFormat
            : hdrRequested
                ? "P010"
                : "NV12";
        var mfSourceReaderNegotiatedFormat = unifiedVideoCapture?.NegotiatedFormat ?? lastMfSourceReaderNegotiatedFormat;
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
        var readerSourceStreamType = (recordingActive || videoPreviewActive) && unifiedVideoCapture != null
            ? "MfSourceReader"
            : null;
        var frameLedger = unifiedVideoCapture?.GetFrameLedgerSummary() ?? FrameLedgerSummary.Empty;

        return new RuntimeReaderTransportSnapshotFields
        {
            MemoryPreference = unifiedVideoCapture?.D3DManager != null ? "Gpu" : "Cpu",
            VideoRequestedSubtype = requestedReaderSubtype ?? "unknown",
            VideoNegotiatedSubtype = videoNegotiatedSubtype,
            FrameLedgerCapacity = frameLedger.Capacity,
            FrameLedgerEventCount = frameLedger.TotalEventsRecorded,
            FrameLedgerDroppedEventCount = frameLedger.EventsDroppedByRetention,
            FrameLedgerRecentEvents = frameLedger.RecentEvents,
            PreviewColorMetadata = (previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? "None",
            MfSourceReaderNegotiatedFormat = mfSourceReaderNegotiatedFormat,
            RequestedReaderSubtype = requestedReaderSubtype,
            ReaderSourceStreamType = readerSourceStreamType,
            ReaderSourceSubtype = actualPixelFormat
        };
    }

    private static RuntimeHdrPipelineSnapshotFields CaptureRuntimeHdrPipelineSnapshotFields(
        CaptureSettings? requestedSettings,
        string? encoderInputPixelFormat,
        RecordingContext? recordingContext,
        bool recordingActive,
        SourceSignalTelemetrySnapshot sourceTelemetry,
        bool mfConvertersDisabled)
    {
        var hdrRequested = requestedSettings?.HdrEnabled == true &&
                           requestedSettings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        var requestedPipelineMode = hdrRequested ? "HDR10-PQ" : "SDR";
        var encoderOutputPixelFormat = ResolveEncoderOutputPixelFormat(recordingContext, requestedSettings);
        var encoderVideoCodec = ResolveEncoderCodecName(requestedSettings);
        var encoderVideoProfile = ResolveEncoderVideoProfile(recordingContext, requestedSettings);
        bool? encoderTenBitPipelineConfirmed = recordingActive
            ? recordingContext?.HdrPipelineActive == true
            : null;
        var negotiatedMediaSubtypeToken = string.Equals(encoderInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase)
            ? "P010|MFVideoFormat_P010"
            : "NV12";
        var activePipelineMode = recordingActive
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
        var pipelineModeStatus = recordingActive
            ? (pipelineModeMatched ? "Active" : "Violation")
            : "Ready";
        var pipelineModeReason = pipelineModeMatched
            ? string.Empty
            : $"Requested pipeline '{requestedPipelineMode}', but active encoder ingress is '{activePipelineMode}' " +
              $"(pixel-format={encoderInputPixelFormat ?? "unknown"}).";
        var hdrOutputActive = recordingActive &&
                              string.Equals(
                                  activePipelineMode,
                                  "HDR10-PQ",
                                  StringComparison.OrdinalIgnoreCase);
        var hdrAutoDowngraded = hdrRequested && recordingActive && !pipelineModeMatched;

        return new RuntimeHdrPipelineSnapshotFields
        {
            HdrRequested = hdrRequested,
            EncoderInputPixelFormat = encoderInputPixelFormat,
            EncoderOutputPixelFormat = encoderOutputPixelFormat,
            EncoderVideoCodec = encoderVideoCodec,
            EncoderVideoProfile = encoderVideoProfile,
            EncoderTenBitPipelineConfirmed = encoderTenBitPipelineConfirmed,
            MfConvertersDisabled = mfConvertersDisabled,
            NegotiatedMediaSubtypeToken = negotiatedMediaSubtypeToken,
            HdrOutputActive = hdrOutputActive,
            HdrActivationReason = hdrOutputActive
                ? "P010 pipeline is active."
                : hdrRequested
                    ? (recordingActive
                        ? "HDR requested but the active recording pipeline is not in HDR mode."
                        : "HDR requested and waiting for recording start.")
                    : "HDR not requested.",
            HdrRuntimeState = hdrOutputActive
                ? "Active"
                : hdrRequested
                    ? (recordingActive ? "Violation" : "Ready")
                    : "Inactive",
            HdrReadinessReason = hdrOutputActive
                ? string.Empty
                : hdrRequested
                    ? (recordingActive
                        ? pipelineModeReason
                        : "HDR requested and will activate when recording starts.")
                    : string.Empty,
            HdrAutoDowngraded = hdrAutoDowngraded,
            HdrAutoDowngradeReason = hdrAutoDowngraded
                ? pipelineModeReason
                : string.Empty,
            HdrDowngradeCode = hdrAutoDowngraded ? "encoder-input-not-p010" : string.Empty,
            HdrRequestedButSourceNot10Bit = hdrRequested && sourceTelemetry.IsHdr == false,
            RequestedPipelineMode = requestedPipelineMode,
            ActivePipelineMode = activePipelineMode,
            PipelineModeMatched = pipelineModeMatched,
            PipelineModeStatus = pipelineModeStatus,
            PipelineModeReason = pipelineModeReason
        };
    }

    private static RuntimeHdrWarmupSnapshotFields CaptureRuntimeHdrWarmupSnapshotFields(
        RuntimeHdrPipelineSnapshotFields hdrPipeline,
        bool recordingActive,
        ObservedFrameSnapshotFields observedTelemetry)
    {
        var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
        var observedNonP010FrameCount =
            observedTelemetry.ObservedNv12FrameCount +
            observedTelemetry.ObservedOtherFrameCount;

        return new RuntimeHdrWarmupSnapshotFields
        {
            State = ResolveHdrWarmupState(
                hdrPipeline.HdrRequested,
                hdrPipeline.HdrOutputActive,
                recordingActive,
                observedP010FrameCount),
            RequiredP010Frames = hdrPipeline.HdrRequested ? 1 : 0,
            AllowedNonP010Frames = hdrPipeline.HdrRequested ? 2 : 0,
            ObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),
            ObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount))
        };
    }

    private static string ResolveHdrWarmupState(
        bool hdrRequested,
        bool hdrOutputActive,
        bool isRecording,
        long observedP010Frames)
    {
        if (!hdrRequested)
        {
            return "NotRequested";
        }

        if (hdrOutputActive)
        {
            return "Satisfied";
        }

        if (observedP010Frames > 0)
        {
            return isRecording ? "Partial" : "Pending";
        }

        return isRecording ? "Degraded" : "Pending";
    }

    private static string ResolveSourceFrameRateOrigin(SourceSignalTelemetrySnapshot telemetry)
    {
        if (!telemetry.FrameRateExact.HasValue || telemetry.FrameRateExact.Value <= 0)
        {
            return "Unknown";
        }

        return telemetry.Origin switch
        {
            SourceTelemetryOrigin.DeviceFormatFallback => "SourceTelemetry(DeviceFormatFallback)",
            SourceTelemetryOrigin.NativeXu => "SourceTelemetry(NativeXu)",
            _ => "SourceTelemetry"
        };
    }

    private static (string Status, string Reason) ResolveTelemetryAlignment(
        CaptureSettings? requestedSettings,
        SourceSignalTelemetrySnapshot telemetry,
        uint? actualWidth,
        uint? actualHeight,
        double? actualFrameRate,
        bool hdrRequested)
    {
        if (telemetry.Availability is SourceTelemetryAvailability.Unknown or SourceTelemetryAvailability.Unavailable)
        {
            return ("Unavailable", telemetry.DiagnosticSummary ?? "Source telemetry unavailable.");
        }

        var expectedWidth = (int?)(requestedSettings?.Width ?? actualWidth);
        var expectedHeight = (int?)(requestedSettings?.Height ?? actualHeight);
        var expectedFrameRate = requestedSettings?.FrameRate ?? actualFrameRate;
        var mismatches = new List<string>();

        if (!telemetry.Width.HasValue || !telemetry.Height.HasValue || !telemetry.FrameRateExact.HasValue)
        {
            return ("Inconclusive", "Telemetry did not include full mode dimensions and frame rate.");
        }

        if (expectedWidth.HasValue && telemetry.Width.Value != expectedWidth.Value)
        {
            mismatches.Add($"width expected {expectedWidth.Value}, observed {telemetry.Width.Value}");
        }

        if (expectedHeight.HasValue && telemetry.Height.Value != expectedHeight.Value)
        {
            mismatches.Add($"height expected {expectedHeight.Value}, observed {telemetry.Height.Value}");
        }

        if (expectedFrameRate.HasValue && Math.Abs(telemetry.FrameRateExact.Value - expectedFrameRate.Value) > 0.75)
        {
            mismatches.Add($"fps expected {expectedFrameRate.Value:0.###}, observed {telemetry.FrameRateExact.Value:0.###}");
        }

        var sourceHdrExpectedSdrCapture = telemetry.IsHdr == true && !hdrRequested;
        if (telemetry.IsHdr.HasValue && telemetry.IsHdr.Value != hdrRequested && !sourceHdrExpectedSdrCapture)
        {
            mismatches.Add($"hdr expected {hdrRequested}, observed {telemetry.IsHdr.Value}");
        }

        if (mismatches.Count == 0)
        {
            if (sourceHdrExpectedSdrCapture)
            {
                return ("Aligned", "Source is HDR, but SDR capture was requested.");
            }

            return ("Aligned", "Source telemetry matches requested capture settings.");
        }

        return ("Mismatch", string.Join("; ", mismatches));
    }

    private static RuntimeSourceTelemetrySnapshotFields CaptureRuntimeSourceTelemetrySnapshotFields(
        CaptureSettings? requestedSettings,
        SourceSignalTelemetrySnapshot telemetry,
        uint? actualWidth,
        uint? actualHeight,
        double? actualFrameRate,
        bool hdrRequested)
    {
        var telemetryTimestampUtc = telemetry.TimestampUtc;
        var telemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(telemetryTimestampUtc, DateTimeOffset.UtcNow);
        var suppressedReason = ResolveSourceTelemetrySuppressedReason(telemetry);
        var suppressed = !string.IsNullOrWhiteSpace(suppressedReason);
        var (alignmentStatus, alignmentReason) = ResolveTelemetryAlignment(
            requestedSettings,
            telemetry,
            actualWidth,
            actualHeight,
            actualFrameRate,
            hdrRequested);

        return new RuntimeSourceTelemetrySnapshotFields
        {
            DetectedSourceFrameRate = telemetry.FrameRateExact,
            DetectedSourceFrameRateArg = telemetry.FrameRateArg,
            SourceFrameRateOrigin = ResolveSourceFrameRateOrigin(telemetry),
            SourceWidth = telemetry.Width,
            SourceHeight = telemetry.Height,
            SourceIsHdr = telemetry.IsHdr,
            SourceVideoFormat = telemetry.VideoFormat,
            SourceColorimetry = telemetry.Colorimetry,
            SourceQuantization = telemetry.Quantization,
            SourceHdrTransferFunction = telemetry.HdrTransferFunction,
            SourceHdrTransferCode = telemetry.HdrTransferCode,
            SourceFirmware = telemetry.Firmware,
            SourceAudioFormat = telemetry.AudioFormat,
            SourceAudioSampleRate = telemetry.AudioSampleRate,
            SourceInputSource = telemetry.InputSource,
            SourceUsbHostProtocol = telemetry.UsbHostProtocol,
            SourceHdcpMode = telemetry.HdcpMode,
            SourceHdcpVersion = telemetry.HdcpVersion,
            SourceRxTxHdcpVersion = telemetry.RxTxHdcpVersion,
            SourceRawTimingHex = telemetry.RawTimingHex,
            Availability = telemetry.Availability.ToString(),
            OriginDetail = telemetry.OriginDetail,
            Confidence = telemetry.Confidence.ToString(),
            DiagnosticSummary = telemetry.DiagnosticSummary,
            Details = telemetry.DetailEntries,
            TimestampUtc = telemetryTimestampUtc,
            AgeSeconds = telemetryAgeSeconds,
            Backend = ResolveSourceTelemetryBackend(telemetry),
            Suppressed = suppressed,
            SuppressedReason = suppressedReason,
            CircuitState = ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed),
            AlignmentStatus = alignmentStatus,
            AlignmentReason = alignmentReason
        };
    }

    private static RuntimeRecordingIntegritySnapshotFields CaptureRuntimeRecordingIntegritySnapshotFields(
        RecordingIntegritySummary recordingIntegrity)
    {
        return new RuntimeRecordingIntegritySnapshotFields
        {
            Status = recordingIntegrity.Status,
            Complete = recordingIntegrity.Complete,
            Backend = recordingIntegrity.Backend,
            CompletedUtc = recordingIntegrity.CompletedUtc,
            SourceFrames = recordingIntegrity.SourceFrames,
            AcceptedFrames = recordingIntegrity.AcceptedFrames,
            PipelineDroppedFrames = recordingIntegrity.PipelineDroppedFrames,
            QueueDroppedFrames = recordingIntegrity.QueueDroppedFrames,
            SubmittedFrames = recordingIntegrity.SubmittedFrames,
            EncodedFrames = recordingIntegrity.EncodedFrames,
            PacketsWritten = recordingIntegrity.PacketsWritten,
            EncoderDroppedFrames = recordingIntegrity.EncoderDroppedFrames,
            SequenceGaps = recordingIntegrity.SequenceGaps,
            QueueMaxDepth = recordingIntegrity.QueueMaxDepth,
            QueueOldestFrameAgeMs = recordingIntegrity.QueueOldestFrameAgeMs,
            BackpressureWaitMs = recordingIntegrity.BackpressureWaitMs,
            BackpressureEvents = recordingIntegrity.BackpressureEvents,
            BackpressureMaxWaitMs = recordingIntegrity.BackpressureMaxWaitMs,
            AudioStatus = recordingIntegrity.AudioStatus,
            AudioEnabled = recordingIntegrity.AudioEnabled,
            AudioCaptureActive = recordingIntegrity.AudioCaptureActive,
            AudioFramesArrived = recordingIntegrity.AudioFramesArrived,
            AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,
            AudioSamplesEncoded = recordingIntegrity.AudioSamplesEncoded,
            AudioDropEvents = recordingIntegrity.AudioDropEvents,
            AudioDiscontinuities = recordingIntegrity.AudioDiscontinuities,
            AudioTimestampErrors = recordingIntegrity.AudioTimestampErrors,
            AudioCallbackGaps = recordingIntegrity.AudioCallbackGaps,
            AvSyncDriftMs = recordingIntegrity.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = recordingIntegrity.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = recordingIntegrity.EncoderAvSyncCorrectionSamples,
            Reason = recordingIntegrity.Reason
        };
    }

    private sealed class CaptureRuntimeSnapshotAssemblyFields
    {
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
        public long CaptureSessionEpoch { get; init; }
        public long SourceTelemetryEpoch { get; init; }
        public bool IsInitialized { get; init; }
        public bool IsRecording { get; init; }
        public bool IsAudioPreviewActive { get; init; }
        public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;
        public RuntimeIngestAudioSnapshotFields IngestAudio { get; init; } = new();
        public RuntimeReaderTransportSnapshotFields ReaderTransport { get; init; } = new();
        public RuntimeHdrPipelineSnapshotFields HdrPipeline { get; init; } = new();
        public RuntimeHdrWarmupSnapshotFields HdrWarmup { get; init; } = new();
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

    // Pure runtime snapshot DTO construction from already-sampled field groups.
    private static class CaptureRuntimeSnapshotAssembler
    {
        public static CaptureRuntimeSnapshot Build(CaptureRuntimeSnapshotAssemblyFields fields)
        {
            var requestedSettings = fields.RequestedSettings;
            var ingestAudio = fields.IngestAudio;
            var readerTransport = fields.ReaderTransport;
            var hdrPipeline = fields.HdrPipeline;
            var hdrWarmup = fields.HdrWarmup;
            var sourceTelemetry = fields.SourceTelemetry;
            var observedTelemetry = fields.ObservedTelemetry;
            var recordingIntegrity = fields.RecordingIntegrity;
            var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
            var observedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount;
            var observedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount;
            var observedP010BitDepthSampleCount = observedTelemetry.ObservedP010BitDepthSampleCount;
            var observedP010Low2BitNonZeroPercent = observedTelemetry.ObservedP010Low2BitNonZeroPercent;
            var observedP010Likely8BitUpscaled = observedTelemetry.ObservedP010Likely8BitUpscaled;

            return new CaptureRuntimeSnapshot
            {
                TimestampUtc = fields.TimestampUtc,
                CaptureSessionEpoch = fields.CaptureSessionEpoch,
                SourceTelemetryEpoch = fields.SourceTelemetryEpoch,
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
                HdrWarmupState = hdrWarmup.State,
                HdrWarmupRequiredP010Frames = hdrWarmup.RequiredP010Frames,
                HdrWarmupAllowedNonP010Frames = hdrWarmup.AllowedNonP010Frames,
                HdrWarmupObservedP010Frames = hdrWarmup.ObservedP010Frames,
                HdrWarmupObservedNonP010Frames = hdrWarmup.ObservedNonP010Frames,
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

    private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;
    private const int PreviewFrameCaptureRendererPollMs = 50;

    private string ResolveRecordingBackendName()
    {
        if (IsFlashbackRecordingBackendOwnedByRecording())
            return "Flashback";
        return _isRecording && _recordingBackend.LibAvSink != null ? "LibAv" : "None";
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        // CaptureHealthSnapshot inherits from CaptureDiagnosticsSnapshot,
        // so the full snapshot satisfies the diagnostics contract directly.
        return GetHealthSnapshot();
    }

    public VideoSourceProbeResult ProbeVideoSource()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture == null)
        {
            return new VideoSourceProbeResult
            {
                SessionActive = false,
                MemoryPreference = "Unknown"
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        var fps = Math.Round(unifiedVideoCapture.Fps, 3);
        return new VideoSourceProbeResult
        {
            SessionActive = true,
            MemoryPreference = unifiedVideoCapture.IsP010 ? "Auto" : "Cpu",
            CurrentSubtype = subtype,
            CurrentWidth = unifiedVideoCapture.Width,
            CurrentHeight = unifiedVideoCapture.Height,
            CurrentFrameRate = fps,
            P010Available = unifiedVideoCapture.IsP010,
            Nv12Available = !unifiedVideoCapture.IsP010,
            SupportedSubtypes = new[] { subtype },
            TotalFormatCount = 1,
            Formats = new[]
            {
                new VideoSourceFormatEntry
                {
                    Subtype = subtype,
                    Width = unifiedVideoCapture.Width,
                    Height = unifiedVideoCapture.Height,
                    FrameRate = fps,
                    Summary = $"{subtype} {unifiedVideoCapture.Width}x{unifiedVideoCapture.Height}@{fps:0.###}"
                }
            }
        };
    }

    public PreviewColorProbeResult ProbePreviewColor()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
        var d3dInputColor = d3dSink?.InputColorSpaceLabel ?? "None";
        var d3dOutputColor = d3dSink?.OutputColorSpaceLabel ?? "None";
        if (unifiedVideoCapture == null)
        {
            return new PreviewColorProbeResult
            {
                SessionActive = false,
                D3DInputColorSpace = d3dInputColor,
                D3DOutputColorSpace = d3dOutputColor
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        return new PreviewColorProbeResult
        {
            SessionActive = true,
            RendererMode = d3dSink?.RendererMode ?? "None",
            NegotiatedSubtype = subtype,
            SourceWidth = unifiedVideoCapture.Width,
            SourceHeight = unifiedVideoCapture.Height,
            SourceFrameRate = Math.Round(unifiedVideoCapture.Fps, 3),
            D3DInputColorSpace = d3dInputColor,
            D3DOutputColorSpace = d3dOutputColor
        };
    }

    public async Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var waitStartedAt = Stopwatch.GetTimestamp();
        while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)
        {
            var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
            if (d3dSink is { IsRendering: true })
            {
                return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
            }

            if (Stopwatch.GetElapsedTime(waitStartedAt).TotalMilliseconds >= PreviewFrameCaptureRendererWaitTimeoutMs)
            {
                break;
            }

            try
            {
                await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = "No active preview renderer."
        };
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }

    public RecordingStats GetRecordingStats()
    {
        var snapshotUtc = DateTimeOffset.UtcNow;
        var captureSessionEpoch = CaptureSnapshotProducerEpoch();

        RecordingStats BuildStats(
            long videoBytes,
            long audioBytes,
            bool isFlashbackEstimate = false,
            bool isFailure = false)
            => new(
                videoBytes,
                audioBytes,
                isFlashbackEstimate,
                isFailure,
                snapshotUtc,
                captureSessionEpoch);

        try
        {
            if (_isRecording && _recordingBackend.LibAvSink != null)
            {
                return BuildStats(_recordingBackend.LibAvSink.OutputBytes, 0);
            }

            // Flashback recording: the output file doesn't exist until export-on-stop.
            // Report estimated size from the flashback buffer bytes written since recording start.
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                var bufferManager = _flashbackBackend.BufferManager;
                if (bufferManager != null)
                {
                    return BuildStats(bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes, 0, isFlashbackEstimate: true);
                }
            }

            var path = _recordingBackend.Context?.VideoOutputPath ?? _lastOutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return BuildStats(0, 0);
            }

            try
            {
                return BuildStats(new FileInfo(path).Length, 0);
            }
            catch (FileNotFoundException)
            {
                return BuildStats(0, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"GetRecordingStats failed: {ex.Message}");
            return BuildStats(0, 0, isFailure: true);
        }
    }

    private static string? ResolveEncoderCodecName(CaptureSettings? settings)
        => settings == null ? null : MediaFormat.MapNvencCodecName(settings.Format);

    private static string? ResolveEncoderOutputPixelFormat(RecordingContext? context, CaptureSettings? settings)
    {
        if (context?.HdrPipelineActive == true)
        {
            return "yuv420p10le";
        }

        return settings == null ? null : "yuv420p";
    }

    private static string? ResolveEncoderVideoProfile(RecordingContext? context, CaptureSettings? settings)
    {
        if (settings == null)
        {
            return null;
        }

        if (context?.HdrPipelineActive == true)
        {
            return "main10";
        }

        return settings.Format switch
        {
            RecordingFormat.H264Mp4 => "high",
            _ => "main"
        };
    }

    private static string? ResolveRequestedFrameRateArg(CaptureSettings? settings, string? fallbackArg)
    {
        if (!string.IsNullOrWhiteSpace(settings?.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg;
        }

        if (settings?.RequestedFrameRateNumerator is uint numerator &&
            settings.RequestedFrameRateDenominator is uint denominator &&
            numerator > 0 &&
            denominator > 0)
        {
            return $"{numerator}/{denominator}";
        }

        return fallbackArg;
    }

    private ObservedFrameSnapshotFields ResolveObservedFrameTelemetry()
    {
        var expectedFormat = _recordingBackend.Context?.HdrPipelineActive == true ? "P010" : _recordingBackend.Context != null ? "NV12" : null;
        var firstObserved = _firstObservedFramePixelFormat ?? expectedFormat;
        var latestObserved = _latestObservedFramePixelFormat ?? expectedFormat;
        var latestSurface = _latestObservedSurfaceFormat ?? latestObserved;

        return new ObservedFrameSnapshotFields(
            FirstObservedFramePixelFormat: firstObserved,
            LatestObservedFramePixelFormat: latestObserved,
            LatestObservedSurfaceFormat: latestSurface,
            ObservedP010FrameCount: Math.Max(0, Interlocked.Read(ref _observedP010FrameCount)),
            ObservedNv12FrameCount: Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount)),
            ObservedOtherFrameCount: Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount)),
            ObservedP010BitDepthSampleCount: 0,
            ObservedP010Low2BitNonZeroPercent: 0,
            ObservedP010Likely8BitUpscaled: null);
    }

    private static string ResolveSourceTelemetryBackend(SourceSignalTelemetrySnapshot telemetry)
        => telemetry.Origin switch
        {
            SourceTelemetryOrigin.DeviceFormatFallback => "DeviceFormatFallback",
            SourceTelemetryOrigin.NativeXu => "NativeXu",
            _ => "Unknown"
        };

    private static string? ResolveSourceTelemetrySuppressedReason(SourceSignalTelemetrySnapshot telemetry)
    {
        if (string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary))
        {
            return null;
        }

        if (telemetry.DiagnosticSummary.Contains("suppressed", StringComparison.OrdinalIgnoreCase) ||
            telemetry.DiagnosticSummary.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            return telemetry.DiagnosticSummary;
        }

        return null;
    }

    private static string ResolveSourceTelemetryCircuitState(
        SourceTelemetryAvailability availability,
        bool telemetrySuppressed)
    {
        if (telemetrySuppressed)
        {
            return "Open";
        }

        return availability switch
        {
            SourceTelemetryAvailability.Unavailable => "Open",
            SourceTelemetryAvailability.Stale => "Open",
            _ => "Closed"
        };
    }

    private void ResetAvSyncDriftBaseline()
    {
        _avSyncBaselineDriftMs = double.NaN;
    }

    private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        var wasapiCapture = _previewAudioGraph.ProgramCapture;
        if (unifiedVideoCapture == null || wasapiCapture == null)
        {
            return (null, null);
        }

        var videoFrames = unifiedVideoCapture.VideoFramesArrived;
        var audioFrames = wasapiCapture.AudioFramesArrived;
        var negotiatedFps = unifiedVideoCapture.Fps;

        if (videoFrames <= 0 || audioFrames <= 0 || negotiatedFps <= 0)
        {
            return (null, null);
        }

        var rawDriftMs = (audioFrames / 48000.0 - videoFrames / negotiatedFps) * 1000.0;

        if (double.IsNaN(_avSyncBaselineDriftMs))
        {
            _avSyncBaselineDriftMs = rawDriftMs;
            _avSyncPrevDriftMs = 0.0;
            _avSyncPrevDriftTick = Environment.TickCount64;
            return (0.0, 0.0);
        }

        var correctedDrift = rawDriftMs - _avSyncBaselineDriftMs;
        var now = Environment.TickCount64;
        var elapsedMs = now - _avSyncPrevDriftTick;

        if (elapsedMs >= 5000)
        {
            var elapsedSec = elapsedMs / 1000.0;
            _avSyncDriftRateMsPerSec = (correctedDrift - _avSyncPrevDriftMs) / elapsedSec;
            _avSyncPrevDriftMs = correctedDrift;
            _avSyncPrevDriftTick = now;
        }

        return (correctedDrift, _avSyncDriftRateMsPerSec);
    }

    private (double? EncoderDriftMs, long? EncoderCorrectionSamples) GetEncoderAvSyncDrift()
    {
        var sink = _recordingBackend.LibAvSink;
        if (sink != null && sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))
        {
            return (driftMs, correctionSamples);
        }

        return (null, null);
    }

    private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()
    {
        var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();
        var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();

        return new AvSyncHealthSnapshotFields(
            captureDriftMs,
            captureDriftRateMsPerSec,
            encoderDriftMs,
            encoderCorrectionSamples);
    }

    private double _avSyncBaselineDriftMs = double.NaN;
    private double _avSyncPrevDriftMs;
    private long _avSyncPrevDriftTick;
    private double _avSyncDriftRateMsPerSec;

    private readonly record struct ObservedFrameSnapshotFields(
        string? FirstObservedFramePixelFormat,
        string? LatestObservedFramePixelFormat,
        string? LatestObservedSurfaceFormat,
        long ObservedP010FrameCount,
        long ObservedNv12FrameCount,
        long ObservedOtherFrameCount,
        long ObservedP010BitDepthSampleCount,
        double ObservedP010Low2BitNonZeroPercent,
        bool? ObservedP010Likely8BitUpscaled);

    private readonly record struct AvSyncHealthSnapshotFields(
        double? CaptureDriftMs,
        double? CaptureDriftRateMsPerSec,
        double? EncoderDriftMs,
        long? EncoderCorrectionSamples);

    public SourceSignalTelemetrySnapshot GetLatestSourceTelemetrySnapshot() => _latestSourceTelemetry;

    private Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken)
        => RefreshSourceTelemetryAsync(cancellationToken, Volatile.Read(ref _telemetryPollGeneration));

    private async Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken, long pollGeneration)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fallback = BuildFallbackTelemetry();
        SourceSignalTelemetrySnapshot telemetry;
        try
        {
            telemetry = await _sourceTelemetryProvider
                .ReadAsync(_currentDevice, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"Source telemetry read failed: {ex.Message}");
            telemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("source-telemetry-exception", ex.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (pollGeneration != Volatile.Read(ref _telemetryPollGeneration))
        {
            return;
        }

        var telemetryEpoch = Interlocked.Increment(ref _sourceTelemetryEpoch);
        _latestSourceTelemetry = MergeTelemetryWithFallback(telemetry, fallback) with { TelemetryEpoch = telemetryEpoch };
        SourceTelemetryUpdated?.Invoke(this, _latestSourceTelemetry);
    }

    private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()
    {
        return new SourceSignalTelemetrySnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            TelemetryEpoch = Volatile.Read(ref _sourceTelemetryEpoch),
            Availability = SourceTelemetryAvailability.Inconclusive,
            Origin = SourceTelemetryOrigin.DeviceFormatFallback,
            OriginDetail = "CaptureSettingsFallback",
            Confidence = SourceTelemetryConfidence.Low,
            Width = (int?)_actualWidth ?? (int?)_currentSettings?.Width,
            Height = (int?)_actualHeight ?? (int?)_currentSettings?.Height,
            FrameRateExact = _actualFrameRate ?? _currentSettings?.FrameRate,
            FrameRateArg = _actualFrameRateArg ?? _currentSettings?.RequestedFrameRateArg,
            IsHdr = null,
            DiagnosticSummary = "Using capture-format fallback telemetry."
        };
    }

    private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(
        SourceSignalTelemetrySnapshot telemetry,
        SourceSignalTelemetrySnapshot fallback)
    {
        return telemetry with
        {
            Width = telemetry.Width ?? fallback.Width,
            Height = telemetry.Height ?? fallback.Height,
            FrameRateExact = telemetry.FrameRateExact ?? fallback.FrameRateExact,
            FrameRateArg = telemetry.FrameRateArg ?? fallback.FrameRateArg,
            IsHdr = telemetry.IsHdr ?? fallback.IsHdr,
            Origin = telemetry.Origin == SourceTelemetryOrigin.Unknown
                ? fallback.Origin
                : telemetry.Origin,
            OriginDetail = string.IsNullOrWhiteSpace(telemetry.OriginDetail) ||
                           string.Equals(telemetry.OriginDetail, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? fallback.OriginDetail
                : telemetry.OriginDetail,
            Confidence = telemetry.Confidence == SourceTelemetryConfidence.Unknown
                ? fallback.Confidence
                : telemetry.Confidence,
            VideoFormat = telemetry.VideoFormat ?? fallback.VideoFormat,
            Colorimetry = telemetry.Colorimetry ?? fallback.Colorimetry,
            Quantization = telemetry.Quantization ?? fallback.Quantization,
            HdrTransferFunction = telemetry.HdrTransferFunction ?? fallback.HdrTransferFunction,
            HdrTransferCode = telemetry.HdrTransferCode ?? fallback.HdrTransferCode,
            Firmware = telemetry.Firmware ?? fallback.Firmware,
            AudioFormat = telemetry.AudioFormat ?? fallback.AudioFormat,
            AudioSampleRate = telemetry.AudioSampleRate ?? fallback.AudioSampleRate,
            InputSource = telemetry.InputSource ?? fallback.InputSource,
            UsbHostProtocol = telemetry.UsbHostProtocol ?? fallback.UsbHostProtocol,
            HdcpMode = telemetry.HdcpMode ?? fallback.HdcpMode,
            HdcpVersion = telemetry.HdcpVersion ?? fallback.HdcpVersion,
            RxTxHdcpVersion = telemetry.RxTxHdcpVersion ?? fallback.RxTxHdcpVersion,
            RawTimingHex = telemetry.RawTimingHex ?? fallback.RawTimingHex,
            DetailEntries = telemetry.DetailEntries.Count > 0
                ? telemetry.DetailEntries
                : fallback.DetailEntries,
            DiagnosticSummary = string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary)
                ? fallback.DiagnosticSummary
                : telemetry.DiagnosticSummary
        };
    }

    private void ResetObservedPixelTelemetry()
    {
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        _latestObservedSurfaceFormat = null;
        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);
    }

    private static string? NormalizeObservedPixelFormat(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return null;
        }

        if (pixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        if (pixelFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase))
        {
            return "NV12";
        }

        return pixelFormat.Trim().ToUpperInvariant();
    }

    private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)
    {
        var normalizedFormat = NormalizeObservedPixelFormat(pixelFormat);
        if (string.IsNullOrWhiteSpace(normalizedFormat))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_firstObservedFramePixelFormat))
        {
            _firstObservedFramePixelFormat = normalizedFormat;
        }

        _latestObservedFramePixelFormat = normalizedFormat;
        _latestObservedSurfaceFormat = normalizedFormat;

        if (!incrementAsFrame)
        {
            return;
        }

        if (string.Equals(normalizedFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedP010FrameCount);
        }
        else if (string.Equals(normalizedFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedNv12FrameCount);
        }
        else
        {
            Interlocked.Increment(ref _observedOtherFrameCount);
        }
    }

    private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)
    {
        if (sink == null)
        {
            return;
        }

        Interlocked.Exchange(ref _videoFramesDropped, sink.DroppedVideoFrames);
    }

    /// <summary>
    /// When the driver reports integer frame rates (for example 120/1 for MJPG)
    /// but source telemetry confirms NTSC timing (for example vfreq=11987 is
    /// about 119.88fps), override the actual frame rate to the correct NTSC
    /// rational. This affects recording metadata, cadence tracking, and UI display.
    /// </summary>
    private void TryCorrectFrameRateFromTelemetry()
    {
        if (_actualFrameRateDenominator is not null and not 1)
            return; // Already fractional; no correction needed.

        var telemetry = _latestSourceTelemetry;
        if (!telemetry.HasFrameRate || !telemetry.FrameRateExact.HasValue)
            return;

        var telemetryFps = telemetry.FrameRateExact.Value;
        var friendlyBucket = (int)Math.Round(_actualFrameRate ?? 0, MidpointRounding.AwayFromZero);
        if (friendlyBucket <= 0)
            return;

        var expectedNtscFps = friendlyBucket * 1000.0 / 1001.0;
        if (Math.Abs(telemetryFps - expectedNtscFps) > 0.15)
            return;

        var ntscNumerator = (uint)(friendlyBucket * 1000);
        const uint ntscDenominator = 1001;
        var correctedFps = (double)ntscNumerator / ntscDenominator;

        Logger.Log(
            $"FRAMERATE_NTSC_CORRECTION driver={_actualFrameRateNumerator}/{_actualFrameRateDenominator} " +
            $"telemetry={telemetryFps:0.###} corrected={ntscNumerator}/{ntscDenominator} ({correctedFps:0.######})");

        _actualFrameRate = correctedFps;
        _actualFrameRateNumerator = ntscNumerator;
        _actualFrameRateDenominator = ntscDenominator;
        _actualFrameRateArg = $"{ntscNumerator}/{ntscDenominator}";
    }

    private static string ResolveFrameRateArg(CaptureSettings settings, double fallbackFrameRate)
    {
        if (!string.IsNullOrWhiteSpace(settings.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg!;
        }

        if (settings.RequestedFrameRateNumerator.HasValue &&
            settings.RequestedFrameRateDenominator.HasValue &&
            settings.RequestedFrameRateNumerator.Value > 0 &&
            settings.RequestedFrameRateDenominator.Value > 0)
        {
            return $"{settings.RequestedFrameRateNumerator.Value}/{settings.RequestedFrameRateDenominator.Value}";
        }

        return fallbackFrameRate > 0
            ? fallbackFrameRate.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "60";
    }

    private void StartTelemetryPoll()
    {
        lock (_telemetryPollSync)
        {
            var previousTask = _telemetryPollTask;
            StopTelemetryPollLocked();
            if (previousTask != null && !previousTask.IsCompleted)
            {
                var deferredGeneration = Volatile.Read(ref _telemetryPollGeneration);
                Logger.Log("Telemetry poll start deferred until canceled poll exits");
                _telemetryPollTask = Task.Run(async () =>
                {
                    try
                    {
                        await previousTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected while draining a canceled poll.
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Telemetry poll drain failed before restart: {ex.Message}");
                    }

                    lock (_telemetryPollSync)
                    {
                        if (deferredGeneration == Volatile.Read(ref _telemetryPollGeneration))
                        {
                            StartTelemetryPollCoreLocked();
                        }
                    }
                });
                return;
            }

            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCore()
    {
        lock (_telemetryPollSync)
        {
            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCoreLocked()
    {
        var generation = Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = new CancellationTokenSource();
        _telemetryPollCts = cts;
        _telemetryPollTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TelemetryPollIntervalMs, cts.Token).ConfigureAwait(false);
                    await RefreshSourceTelemetryAsync(cts.Token, generation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Telemetry poll cycle failed: {ex.Message}");
                }
            }
        }, cts.Token);
    }

    private void StopTelemetryPoll()
    {
        lock (_telemetryPollSync)
        {
            StopTelemetryPollLocked();
        }
    }

    private void StopTelemetryPollLocked()
    {
        Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = _telemetryPollCts;
        _telemetryPollCts = null;
        cts?.Cancel();
        if (_telemetryPollTask?.IsCompleted == true)
        {
            _telemetryPollTask = null;
        }
        // Do not dispose the CTS here; the poll task may still be checking
        // the token between Cancel and its own exit. Let GC finalize instead of
        // risking ObjectDisposedException in the poll loop's Task.Delay.
    }

    private async Task StopTelemetryPollAsync()
    {
        Task? task;
        lock (_telemetryPollSync)
        {
            task = _telemetryPollTask;
            StopTelemetryPollLocked();
        }
        if (task == null || task.IsCompleted)
        {
            return;
        }

        try
        {
            await task.WaitAsync(TimeSpan.FromMilliseconds(TelemetryPollStopDrainTimeoutMs)).ConfigureAwait(false);
            lock (_telemetryPollSync)
            {
                if (ReferenceEquals(_telemetryPollTask, task))
                {
                    _telemetryPollTask = null;
                }
            }
        }
        catch (TimeoutException)
        {
            Logger.Log($"Telemetry poll drain timed out after {TelemetryPollStopDrainTimeoutMs}ms");
        }
        catch (OperationCanceledException)
        {
            // Expected when the poll loop observes cancellation.
        }
    }

}

// Single policy gate for enabling HDR output. Environment overrides live beside
// the HDR runtime projection so capture setup and UI readiness stay consistent.
internal static class HdrOutputPolicy
{
    public static bool IsEnabled(CaptureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var hdrRequested = settings.HdrEnabled && settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        if (!hdrRequested)
        {
            return false;
        }

        if (EnvironmentHelpers.TryGetBoolFromEnv("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", out var forceOff) && forceOff)
        {
            Logger.Log("HDR output requested but SUSSUDIO_HDR_OUTPUT_FORCE_OFF disables the HDR pipeline.");
            return false;
        }

        return true;
    }
}
