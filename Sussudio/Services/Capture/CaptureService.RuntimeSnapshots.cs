using System;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

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
}
