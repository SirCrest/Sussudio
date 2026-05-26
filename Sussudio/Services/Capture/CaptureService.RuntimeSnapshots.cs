using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

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
            _latestSourceTelemetry,
            _mfConvertersDisabled);
        var hdrRequested = hdrPipeline.HdrRequested;
        var sourceTelemetry = CaptureRuntimeSourceTelemetrySnapshotFields(
            requestedSettings,
            _latestSourceTelemetry,
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
