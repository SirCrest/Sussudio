using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Runtime snapshot projection consumed by UI, automation, and verification.
// Keep this path read-only so frequent polling cannot mutate capture behavior.
public partial class CaptureService
{
    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var sink = _recordingBackend.LibAvSink;
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
            _previewFrameSink,
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
}
