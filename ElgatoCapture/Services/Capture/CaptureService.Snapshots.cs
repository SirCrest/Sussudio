using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Capture;

public partial class CaptureService
{
    private const double RecordingIntegrityAvSyncDriftWarningMs = 500.0;
    private const long RecordingIntegrityAudioBoundaryToleranceFrames = 960;

    private string ResolveRecordingBackendName()
    {
        if (IsFlashbackRecordingBackendOwnedByRecording())
            return "Flashback";
        return _isRecording && _libavSink != null ? "LibAv" : "None";
    }

    public RecordingStats GetRecordingStats()
    {
        try
        {
            if (_isRecording && _libavSink != null)
            {
                return new RecordingStats(_libavSink.OutputBytes, 0);
            }

            // Flashback recording: the output file doesn't exist until export-on-stop.
            // Report estimated size from the flashback buffer bytes written since recording start.
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                var bufferManager = _flashbackBufferManager;
                if (bufferManager != null)
                {
                    return new RecordingStats(bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes, 0, isFlashbackEstimate: true);
                }
            }

            var path = _recordingContext?.VideoOutputPath ?? _lastOutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return new RecordingStats(0, 0);
            }

            try
            {
                return new RecordingStats(new FileInfo(path).Length, 0);
            }
            catch (FileNotFoundException)
            {
                return new RecordingStats(0, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"GetRecordingStats failed: {ex.Message}");
            return new RecordingStats(0, 0);
        }
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        // CaptureHealthSnapshot inherits from CaptureDiagnosticsSnapshot,
        // so the full snapshot satisfies the diagnostics contract directly.
        return GetHealthSnapshot();
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

    private static long ComputeFlashbackExportElapsedMs(
        bool active,
        long startedUtcUnixMs,
        long completedUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (startedUtcUnixMs <= 0)
        {
            return 0;
        }

        var endUtcUnixMs = active
            ? nowUtcUnixMs
            : completedUtcUnixMs > 0
                ? completedUtcUnixMs
                : nowUtcUnixMs;

        return Math.Max(0, endUtcUnixMs - startedUtcUnixMs);
    }

    private static long ComputeFlashbackExportLastProgressAgeMs(
        bool active,
        long startedUtcUnixMs,
        long lastProgressUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (!active)
        {
            return 0;
        }

        var referenceUtcUnixMs = lastProgressUtcUnixMs > 0
            ? lastProgressUtcUnixMs
            : startedUtcUnixMs;

        return referenceUtcUnixMs > 0
            ? Math.Max(0, nowUtcUnixMs - referenceUtcUnixMs)
            : 0;
    }

    private static long GetFileLengthOrZero(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    private (
        string? FirstObservedFramePixelFormat,
        string? LatestObservedFramePixelFormat,
        string? LatestObservedSurfaceFormat,
        long ObservedP010FrameCount,
        long ObservedNv12FrameCount,
        long ObservedOtherFrameCount,
        long ObservedP010BitDepthSampleCount,
        double ObservedP010Low2BitNonZeroPercent,
        bool? ObservedP010Likely8BitUpscaled)
        ResolveObservedFrameTelemetry()
    {
        var expectedFormat = _recordingContext?.HdrPipelineActive == true ? "P010" : _recordingContext != null ? "NV12" : null;
        var firstObserved = _firstObservedFramePixelFormat ?? expectedFormat;
        var latestObserved = _latestObservedFramePixelFormat ?? expectedFormat;
        var latestSurface = _latestObservedSurfaceFormat ?? latestObserved;

        return (
            firstObserved,
            latestObserved,
            latestSurface,
            Math.Max(0, Interlocked.Read(ref _observedP010FrameCount)),
            Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount)),
            Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount)),
            0,
            0,
            null);
    }

    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var wasapiCapture = _wasapiAudioCapture;
        var wasapiPlayback = _wasapiAudioPlayback;
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
        var sourceTelemetryAgeSeconds = ResolveTelemetryAgeSeconds(sourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
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
        var mfSourceReaderFramesDelivered = unifiedVideoCapture?.VideoFramesArrived ?? _lastMfSourceReaderFramesDelivered;
        var mfSourceReaderFramesDropped = unifiedVideoCapture?.VideoFramesDropped ?? _lastMfSourceReaderFramesDropped;
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
        var hasD3DManager = unifiedVideoCapture?.D3DManager != null;
        var memoryPreference = hasD3DManager ? "Gpu" : "Cpu";
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
            AudioReaderActive = wasapiCapture?.IsCapturing ?? false,
            AudioFramesArrived = wasapiCapture?.AudioFramesArrived ?? 0,
            AudioFramesWrittenToSink = wasapiCapture?.AudioFramesWrittenToSink ?? 0,
            VideoReaderActive = unifiedVideoCapture != null && (_isVideoPreviewActive || _isRecording),
            IngestVideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            IngestVideoFramesWrittenToSink = unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
            IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoIngestErrorCount = unifiedVideoCapture?.VideoFramesDropped ?? 0,
            MemoryPreference = memoryPreference,
            VideoRequestedSubtype = requestedReaderSubtype ?? "unknown",
            VideoNegotiatedSubtype = videoNegotiatedSubtype,
            FrameLedgerCapacity = frameLedger.Capacity,
            FrameLedgerEventCount = frameLedger.TotalEventsRecorded,
            FrameLedgerDroppedEventCount = frameLedger.EventsDroppedByRetention,
            FrameLedgerRecentEvents = frameLedger.RecentEvents,
            PreviewColorMetadata = previewColorMetadata,
            MfSourceReaderFramesDelivered = mfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = mfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = mfSourceReaderNegotiatedFormat,
            SessionState = _sessionState,
            SourceReaderReadOutstanding = unifiedVideoCapture?.SourceReaderReadOutstanding ?? false,
            SourceReaderReadOutstandingMs = unifiedVideoCapture?.SourceReaderReadOutstandingMs ?? 0,
            SourceReaderLastFrameTickMs = unifiedVideoCapture?.SourceReaderLastFrameTickMs ?? 0,
            SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0,
            WasapiCaptureCallbackCount = wasapiCapture?.CaptureCallbackCount ?? 0,
            WasapiCaptureCallbackAvgIntervalMs = wasapiCapture?.CaptureCallbackAvgIntervalMs ?? 0,
            WasapiCaptureCallbackMaxIntervalMs = wasapiCapture?.CaptureCallbackMaxIntervalMs ?? 0,
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
            WasapiPlaybackLastRenderTickMs = wasapiPlayback?.LastRenderCallbackTickMs ?? 0,
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

    private static int? ResolveTelemetryAgeSeconds(DateTimeOffset telemetryTimestampUtc, DateTimeOffset nowUtc)
    {
        var age = nowUtc - telemetryTimestampUtc;
        if (age < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Floor(age.TotalSeconds);
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }

    private static string ResolveSourceTelemetryBackend(SourceSignalTelemetrySnapshot telemetry)
        => telemetry.Origin switch
        {
            SourceTelemetryOrigin.DeviceFormatFallback => "DeviceFormatFallback",
            SourceTelemetryOrigin.NativeXu => "NativeXu",
            _ => "Unknown"
        };

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

        if (telemetry.IsHdr.HasValue && telemetry.IsHdr.Value != hdrRequested)
        {
            mismatches.Add($"hdr expected {hdrRequested}, observed {telemetry.IsHdr.Value}");
        }

        if (mismatches.Count == 0)
        {
            return ("Aligned", "Source telemetry matches requested capture settings.");
        }

        return ("Mismatch", string.Join("; ", mismatches));
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

    private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        var wasapiCapture = _wasapiAudioCapture;
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
        var sink = _libavSink;
        if (sink != null && sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))
        {
            return (driftMs, correctionSamples);
        }

        return (null, null);
    }

    private sealed record RecordingIntegrityCounterSnapshot(
        string Backend,
        long SubmittedFrames,
        long EncodedFrames,
        long PacketsWritten,
        long EncoderDroppedFrames,
        long QueueDroppedFrames,
        long SequenceGaps,
        int QueueMaxDepth,
        long QueueOldestFrameAgeMs,
        long BackpressureWaitMs,
        long BackpressureEvents,
        long BackpressureMaxWaitMs,
        bool EncodingFailed,
        string? EncodingFailureType,
        string? EncodingFailureMessage);

    private sealed record RecordingAudioIntegrityCounterSnapshot(
        bool AudioEnabled,
        bool AudioCaptureActive,
        long AudioFramesArrived,
        long AudioFramesWrittenToSink,
        long AudioSamplesEncoded,
        long AudioDropEvents,
        long AudioDiscontinuities,
        long AudioTimestampErrors,
        long AudioCallbackGaps,
        double? AvSyncDriftMs,
        double? AvSyncDriftRateMsPerSec,
        double? EncoderAvSyncDriftMs,
        long? EncoderAvSyncCorrectionSamples)
    {
        public static RecordingAudioIntegrityCounterSnapshot Disabled { get; } = new(
            AudioEnabled: false,
            AudioCaptureActive: false,
            AudioFramesArrived: 0,
            AudioFramesWrittenToSink: 0,
            AudioSamplesEncoded: 0,
            AudioDropEvents: 0,
            AudioDiscontinuities: 0,
            AudioTimestampErrors: 0,
            AudioCallbackGaps: 0,
            AvSyncDriftMs: null,
            AvSyncDriftRateMsPerSec: null,
            EncoderAvSyncDriftMs: null,
            EncoderAvSyncCorrectionSamples: null);
    }

    private RecordingIntegritySummary ResolveRecordingIntegritySummary(
        UnifiedVideoCapture? unifiedVideoCapture,
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink)
    {
        if (!_isRecording)
        {
            return _lastRecordingIntegrity;
        }

        if (IsFlashbackRecordingBackendOwnedByRecording() && fbSink != null)
        {
            var counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(fbSink, unifiedVideoCapture);
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, fbSink, _activeRecordingSettings));
            return BuildRecordingIntegritySummary(
                backend: "Flashback",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters);
        }

        if (sink != null)
        {
            var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, sink, _activeRecordingSettings));
            return BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters);
        }

        return new RecordingIntegritySummary
        {
            Status = "Active",
            Backend = ResolveRecordingBackendName(),
            Reason = "Recording active; recording boundary is still attaching."
        };
    }

    private static RecordingIntegritySummary BuildRecordingIntegritySummary(
        string backend,
        bool recordingActive,
        bool finalizeSucceeded,
        string finalizeStatus,
        DateTimeOffset? completedUtc,
        long sourceFrames,
        long acceptedFrames,
        RecordingIntegrityCounterSnapshot counters,
        RecordingAudioIntegrityCounterSnapshot? audioCounters = null)
    {
        audioCounters ??= RecordingAudioIntegrityCounterSnapshot.Disabled;
        sourceFrames = Math.Max(0, sourceFrames);
        acceptedFrames = Math.Max(0, acceptedFrames);
        var submittedFrames = Math.Max(0, counters.SubmittedFrames);
        var encodedFrames = Math.Max(0, counters.EncodedFrames);
        var packetsWritten = Math.Max(0, counters.PacketsWritten);
        var encoderDroppedFrames = Math.Max(0, counters.EncoderDroppedFrames);
        var queueDroppedFrames = Math.Max(0, counters.QueueDroppedFrames);
        var sequenceGaps = Math.Max(0, counters.SequenceGaps);
        var queueMaxDepth = Math.Max(0, counters.QueueMaxDepth);
        var queueOldestFrameAgeMs = Math.Max(0, counters.QueueOldestFrameAgeMs);
        var backpressureWaitMs = Math.Max(0, counters.BackpressureWaitMs);
        var backpressureEvents = Math.Max(0, counters.BackpressureEvents);
        var backpressureMaxWaitMs = Math.Max(0, counters.BackpressureMaxWaitMs);
        var audioFramesArrived = Math.Max(0, audioCounters.AudioFramesArrived);
        var audioFramesWrittenToSink = Math.Max(0, audioCounters.AudioFramesWrittenToSink);
        var audioSamplesEncoded = Math.Max(0, audioCounters.AudioSamplesEncoded);
        var audioDropEvents = Math.Max(0, audioCounters.AudioDropEvents);
        var audioDiscontinuities = Math.Max(0, audioCounters.AudioDiscontinuities);
        var audioTimestampErrors = Math.Max(0, audioCounters.AudioTimestampErrors);
        var audioCallbackGaps = Math.Max(0, audioCounters.AudioCallbackGaps);
        var pipelineDroppedFrames = Math.Max(0, sourceFrames - acceptedFrames);

        var reasons = new List<string>();
        if (!recordingActive && !finalizeSucceeded)
        {
            reasons.Add($"finalize='{finalizeStatus}'");
        }

        if (counters.EncodingFailed)
        {
            var failure = string.IsNullOrWhiteSpace(counters.EncodingFailureMessage)
                ? counters.EncodingFailureType ?? "unknown"
                : $"{counters.EncodingFailureType ?? "unknown"}: {counters.EncodingFailureMessage}";
            reasons.Add($"encoding={failure}");
        }

        if (pipelineDroppedFrames > 0)
        {
            reasons.Add($"pipeline_drops={pipelineDroppedFrames}");
        }

        if (queueDroppedFrames > 0)
        {
            reasons.Add($"queue_drops={queueDroppedFrames}");
        }

        if (encoderDroppedFrames > 0)
        {
            reasons.Add($"encoder_drops={encoderDroppedFrames}");
        }

        if (sequenceGaps > 0)
        {
            reasons.Add($"sequence_gaps={sequenceGaps}");
        }

        var audioStatus = "Disabled";
        if (audioCounters.AudioEnabled)
        {
            var audioFailed = false;
            var audioIncomplete = false;
            if (!audioCounters.AudioCaptureActive)
            {
                audioFailed = true;
                reasons.Add("audio_inactive");
            }

            if (audioFramesArrived <= 0)
            {
                audioFailed = true;
                reasons.Add("audio_no_frames");
            }

            var audioBoundaryDropFrames = audioFramesArrived > audioFramesWrittenToSink
                ? audioFramesArrived - audioFramesWrittenToSink
                : 0;
            if (audioBoundaryDropFrames > RecordingIntegrityAudioBoundaryToleranceFrames)
            {
                audioIncomplete = true;
                reasons.Add($"audio_boundary_drops={audioBoundaryDropFrames}");
            }

            if (audioSamplesEncoded <= 0)
            {
                audioFailed = true;
                reasons.Add("audio_sink_no_samples");
            }

            if (audioDropEvents > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_drops={audioDropEvents}");
            }

            if (audioDiscontinuities > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_discontinuities={audioDiscontinuities}");
            }

            if (audioTimestampErrors > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_timestamp_errors={audioTimestampErrors}");
            }

            if (audioCallbackGaps > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_callback_gaps={audioCallbackGaps}");
            }

            if (audioCounters.AvSyncDriftMs is { } captureDriftMs &&
                Math.Abs(captureDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
            {
                audioIncomplete = true;
                reasons.Add($"av_sync_drift_ms={FormatRecordingIntegrityDouble(captureDriftMs)}");
            }

            if (audioCounters.EncoderAvSyncDriftMs is { } encoderDriftMs &&
                Math.Abs(encoderDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
            {
                audioIncomplete = true;
                reasons.Add($"encoder_av_sync_drift_ms={FormatRecordingIntegrityDouble(encoderDriftMs)}");
            }

            audioStatus = audioFailed ? "Failed" : audioIncomplete ? "Incomplete" : "Clean";
        }

        var status = reasons.Count > 0
            ? (counters.EncodingFailed ||
               (!recordingActive && !finalizeSucceeded) ||
               string.Equals(audioStatus, "Failed", StringComparison.Ordinal)
                ? "Failed"
                : "Incomplete")
            : recordingActive ? "Active" : "Complete";
        var reason = reasons.Count > 0
            ? string.Join("; ", reasons)
            : recordingActive
                ? "Recording active; all delivered source frames have reached the recording boundary so far."
                : "Every delivered source frame reached the recording boundary.";

        return new RecordingIntegritySummary
        {
            Status = status,
            Complete = !recordingActive && string.Equals(status, "Complete", StringComparison.Ordinal),
            Backend = backend,
            CompletedUtc = completedUtc,
            SourceFrames = sourceFrames,
            AcceptedFrames = acceptedFrames,
            PipelineDroppedFrames = pipelineDroppedFrames,
            QueueDroppedFrames = queueDroppedFrames,
            SubmittedFrames = submittedFrames,
            EncodedFrames = encodedFrames,
            PacketsWritten = packetsWritten,
            EncoderDroppedFrames = encoderDroppedFrames,
            SequenceGaps = sequenceGaps,
            QueueMaxDepth = queueMaxDepth,
            QueueOldestFrameAgeMs = queueOldestFrameAgeMs,
            BackpressureWaitMs = backpressureWaitMs,
            BackpressureEvents = backpressureEvents,
            BackpressureMaxWaitMs = backpressureMaxWaitMs,
            AudioStatus = audioStatus,
            AudioEnabled = audioCounters.AudioEnabled,
            AudioCaptureActive = audioCounters.AudioCaptureActive,
            AudioFramesArrived = audioFramesArrived,
            AudioFramesWrittenToSink = audioFramesWrittenToSink,
            AudioSamplesEncoded = audioSamplesEncoded,
            AudioDropEvents = audioDropEvents,
            AudioDiscontinuities = audioDiscontinuities,
            AudioTimestampErrors = audioTimestampErrors,
            AudioCallbackGaps = audioCallbackGaps,
            AvSyncDriftMs = audioCounters.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioCounters.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioCounters.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioCounters.EncoderAvSyncCorrectionSamples,
            Reason = reason
        };
    }

    private static string FormatRecordingIntegrityDouble(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(RecordingIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityCounterBaseline;
        if (baseline == null ||
            !string.Equals(baseline.Backend, current.Backend, StringComparison.Ordinal))
        {
            return current;
        }

        return current with
        {
            SubmittedFrames = DeltaCounter(current.SubmittedFrames, baseline.SubmittedFrames),
            EncodedFrames = DeltaCounter(current.EncodedFrames, baseline.EncodedFrames),
            PacketsWritten = DeltaCounter(current.PacketsWritten, baseline.PacketsWritten),
            EncoderDroppedFrames = DeltaCounter(current.EncoderDroppedFrames, baseline.EncoderDroppedFrames),
            QueueDroppedFrames = DeltaCounter(current.QueueDroppedFrames, baseline.QueueDroppedFrames),
            SequenceGaps = DeltaCounter(current.SequenceGaps, baseline.SequenceGaps),
            BackpressureWaitMs = DeltaCounter(current.BackpressureWaitMs, baseline.BackpressureWaitMs),
            BackpressureEvents = DeltaCounter(current.BackpressureEvents, baseline.BackpressureEvents)
        };
    }

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(LibAvRecordingSink sink)
        => new(
            Backend: "LibAv",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped,
                sink.CudaFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, Math.Max(sink.GpuQueueMaxDepth, sink.CudaQueueMaxDepth)),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(FlashbackEncoderSink sink)
        => new(
            Backend: "Flashback",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, sink.GpuQueueMaxDepth),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(
        FlashbackEncoderSink sink,
        UnifiedVideoCapture? videoCapture)
    {
        var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
        return videoCapture == null
            ? counters
            : counters with { SequenceGaps = Math.Max(0, videoCapture.FlashbackRecordingSequenceGaps) };
    }

    private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(RecordingAudioIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityAudioBaseline;
        if (baseline == null)
        {
            return current;
        }

        return current with
        {
            AudioFramesArrived = DeltaCounter(current.AudioFramesArrived, baseline.AudioFramesArrived),
            AudioFramesWrittenToSink = DeltaCounter(current.AudioFramesWrittenToSink, baseline.AudioFramesWrittenToSink),
            AudioSamplesEncoded = DeltaCounter(current.AudioSamplesEncoded, baseline.AudioSamplesEncoded),
            AudioDropEvents = DeltaCounter(current.AudioDropEvents, baseline.AudioDropEvents),
            AudioDiscontinuities = DeltaCounter(current.AudioDiscontinuities, baseline.AudioDiscontinuities),
            AudioTimestampErrors = DeltaCounter(current.AudioTimestampErrors, baseline.AudioTimestampErrors),
            AudioCallbackGaps = DeltaCounter(current.AudioCallbackGaps, baseline.AudioCallbackGaps)
        };
    }

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        LibAvRecordingSink sink,
        CaptureSettings? settings)
        => CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction));

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        FlashbackEncoderSink sink,
        CaptureSettings? settings)
        => CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction));

    private RecordingAudioIntegrityCounterSnapshot CreateRecordingAudioCounters(
        WasapiAudioCapture? capture,
        CaptureSettings? settings,
        long audioFramesArrived,
        long audioFramesWrittenToSink,
        long audioSamplesEncoded,
        long audioDropEvents)
    {
        var audioEnabled = settings?.AudioEnabled == true;
        if (!audioEnabled)
        {
            return RecordingAudioIntegrityCounterSnapshot.Disabled;
        }

        var (avSyncDriftMs, avSyncDriftRateMsPerSec) = ComputeAvSyncDrift();
        var (encoderAvSyncDriftMs, encoderAvSyncCorrectionSamples) = GetEncoderAvSyncDrift();
        return new RecordingAudioIntegrityCounterSnapshot(
            AudioEnabled: true,
            AudioCaptureActive: capture?.IsCapturing == true,
            AudioFramesArrived: audioFramesArrived,
            AudioFramesWrittenToSink: audioFramesWrittenToSink,
            AudioSamplesEncoded: audioSamplesEncoded,
            AudioDropEvents: audioDropEvents,
            AudioDiscontinuities: capture?.AudioDataDiscontinuityCount ?? 0,
            AudioTimestampErrors: capture?.AudioTimestampErrorCount ?? 0,
            AudioCallbackGaps: capture?.CaptureCallbackSevereGapCount ?? 0,
            AvSyncDriftMs: avSyncDriftMs,
            AvSyncDriftRateMsPerSec: avSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs: encoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples: encoderAvSyncCorrectionSamples);
    }

    private static long DeltaCounter(long current, long baseline)
        => current >= baseline ? current - baseline : current;

    private static long SumNonNegative(long a, long b)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0);

    private static long SumNonNegative(long a, long b, long c)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0) + (c > 0 ? c : 0);

    private static void LogRecordingIntegritySummary(RecordingIntegritySummary summary)
    {
        Logger.Log(
            "RECORDING_INTEGRITY " +
            $"status={summary.Status} " +
            $"complete={summary.Complete} " +
            $"backend={summary.Backend} " +
            $"source_frames={summary.SourceFrames} " +
            $"accepted_frames={summary.AcceptedFrames} " +
            $"pipeline_drops={summary.PipelineDroppedFrames} " +
            $"queue_drops={summary.QueueDroppedFrames} " +
            $"submitted_frames={summary.SubmittedFrames} " +
            $"encoded_frames={summary.EncodedFrames} " +
            $"packets_written={summary.PacketsWritten} " +
            $"encoder_drops={summary.EncoderDroppedFrames} " +
            $"sequence_gaps={summary.SequenceGaps} " +
            $"queue_max_depth={summary.QueueMaxDepth} " +
            $"queue_oldest_age_ms={summary.QueueOldestFrameAgeMs} " +
            $"backpressure_wait_ms={summary.BackpressureWaitMs} " +
            $"backpressure_events={summary.BackpressureEvents} " +
            $"backpressure_max_wait_ms={summary.BackpressureMaxWaitMs} " +
            $"audio_status={summary.AudioStatus} " +
            $"audio_enabled={summary.AudioEnabled} " +
            $"audio_active={summary.AudioCaptureActive} " +
            $"audio_arrived={summary.AudioFramesArrived} " +
            $"audio_written={summary.AudioFramesWrittenToSink} " +
            $"audio_encoded={summary.AudioSamplesEncoded} " +
            $"audio_drops={summary.AudioDropEvents} " +
            $"audio_discontinuities={summary.AudioDiscontinuities} " +
            $"audio_timestamp_errors={summary.AudioTimestampErrors} " +
            $"audio_callback_gaps={summary.AudioCallbackGaps} " +
            $"av_drift_ms={summary.AvSyncDriftMs?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"encoder_av_drift_ms={summary.EncoderAvSyncDriftMs?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"reason='{summary.Reason.Replace("'", "\\'", StringComparison.Ordinal)}'");
    }

    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var fbSink = _flashbackSink;
        var bufMgr = _flashbackBufferManager;
        var fbPlayback = _flashbackPlaybackController;
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var videoFramesDropped = sink?.DroppedVideoFrames ?? Interlocked.Read(ref _videoFramesDropped);
        var sourceTelemetrySuppressedReason = ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry);
        var sourceTelemetrySuppressed = !string.IsNullOrWhiteSpace(sourceTelemetrySuppressedReason);
        var sourceCadence = unifiedVideoCapture?.GetSourceCadenceMetrics()
            ?? default(MfSourceReaderVideoCapture.SourceCadenceMetrics);
        var mjpegTiming = unifiedVideoCapture?.GetMjpegPipelineTimingMetrics()
            ?? _lastMjpegPipelineTimingMetrics;
        var mjpegFullTiming = unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics()
            ?? _lastFullMjpegPipelineTimingMetrics;
        var mjpegPreviewJitter = unifiedVideoCapture?.GetMjpegPreviewJitterMetrics()
            ?? default(MjpegPreviewJitterBuffer.Metrics);
        var visualCadence = unifiedVideoCapture?.GetPreviewVisualCadenceMetrics()
            ?? VisualCadenceTracker.Empty;
        var visualCenterCadence = unifiedVideoCapture?.GetPreviewVisualCenterCadenceMetrics()
            ?? VisualCadenceTracker.Empty;
        var mjpegPacketHash = unifiedVideoCapture?.GetMjpegPacketHashMetrics()
            ?? FrameFingerprintCadenceTracker.Empty;
        var (avSyncDriftMs, avSyncDriftRate) = ComputeAvSyncDrift();
        var (avSyncEncoderDriftMs, avSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();
        var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();
        var lastFailure = GetLastFailureTelemetry();
        var liveRecordingFailed = sink?.EncodingFailed == true ||
                                  (flashbackIsRecordingBackend && fbSink?.EncodingFailed == true);
        var activeRecordingEncodingFailed = liveRecordingFailed || lastFailure.RecordingFailed;
        var activeRecordingFailureType = sink?.EncodingFailureType ??
                                         (flashbackIsRecordingBackend ? fbSink?.EncodingFailureType : null) ??
                                         lastFailure.RecordingFailureType;
        var activeRecordingFailureMessage = sink?.EncodingFailureMessage ??
                                            (flashbackIsRecordingBackend ? fbSink?.EncodingFailureMessage : null) ??
                                            lastFailure.RecordingFailureMessage;
        var flashbackEncodingFailed = fbSink?.EncodingFailed == true || lastFailure.FlashbackFailed;
        var flashbackFailureType = fbSink?.EncodingFailureType ?? lastFailure.FlashbackFailureType;
        var flashbackFailureMessage = fbSink?.EncodingFailureMessage ?? lastFailure.FlashbackFailureMessage;
        var activeRecordingVideoQueueDepth = sink?.VideoQueueCount ??
                                             (flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0);
        var activeRecordingVideoQueueCapacity = sink?.VideoQueueCapacityFrames ??
                                                (flashbackIsRecordingBackend ? fbSink?.VideoQueueCapacityFrames ?? 0 : 0);
        var activeRecordingVideoQueueMaxDepth = sink?.VideoQueueMaxDepth ??
                                                (flashbackIsRecordingBackend ? fbSink?.VideoQueueMaxDepth ?? 0 : 0);
        var activeRecordingVideoFramesEnqueued = sink?.VideoFramesEnqueuedCount ??
                                                 (flashbackIsRecordingBackend ? fbSink?.VideoFramesEnqueuedCount ?? 0 : 0);
        var activeRecordingVideoFramesSubmitted = sink?.VideoFramesSubmittedToEncoder ??
                                                  (flashbackIsRecordingBackend ? fbSink?.VideoFramesSubmittedToEncoder ?? 0 : 0);
        var activeRecordingVideoEncoderPts = sink?.VideoEncoderPts ??
                                             (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPts ?? 0 : 0);
        var activeRecordingVideoEncoderPacketsWritten = sink?.VideoEncoderPacketsWritten ??
                                                        (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPacketsWritten ?? 0 : 0);
        var activeRecordingVideoEncoderDroppedFrames = sink?.VideoEncoderDroppedFrames ??
                                                       (flashbackIsRecordingBackend ? fbSink?.VideoEncoderDroppedFrames ?? 0 : 0);
        var activeRecordingVideoSequenceGaps = sink?.VideoSequenceGaps ??
                                               (flashbackIsRecordingBackend ? fbSink?.VideoSequenceGaps ?? 0 : 0);
        var activeRecordingVideoQueueOldestFrameAgeMs = sink?.VideoQueueOldestFrameAgeMs ??
                                                        (flashbackIsRecordingBackend ? fbSink?.VideoQueueOldestFrameAgeMs ?? 0 : 0);
        var activeRecordingVideoQueueLastLatencyMs = sink?.LastVideoQueueLatencyMs ??
                                                     (flashbackIsRecordingBackend ? fbSink?.LastVideoQueueLatencyMs ?? 0 : 0);
        var activeRecordingVideoQueueLatencySampleCount = sink?.VideoQueueLatencySampleCount ??
                                                          (flashbackIsRecordingBackend ? fbSink?.VideoQueueLatencySampleCount ?? 0 : 0);
        var activeRecordingVideoQueueLatencyAvgMs = sink?.VideoQueueLatencyAvgMs ??
                                                    (flashbackIsRecordingBackend ? fbSink?.VideoQueueLatencyAvgMs ?? 0 : 0);
        var activeRecordingVideoQueueLatencyP95Ms = sink?.VideoQueueLatencyP95Ms ??
                                                    (flashbackIsRecordingBackend ? fbSink?.VideoQueueLatencyP95Ms ?? 0 : 0);
        var activeRecordingVideoQueueLatencyMaxMs = sink?.VideoQueueLatencyMaxMs ??
                                                    (flashbackIsRecordingBackend ? fbSink?.VideoQueueLatencyMaxMs ?? 0 : 0);
        var activeRecordingVideoBackpressureWaitMs = sink?.VideoBackpressureWaitMs ??
                                                     (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureWaitMs ?? 0 : 0);
        var activeRecordingVideoBackpressureEvents = sink?.VideoBackpressureEvents ??
                                                     (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureEvents ?? 0 : 0);
        var activeRecordingVideoBackpressureLastWaitMs = sink?.LastVideoBackpressureWaitMs ??
                                                         (flashbackIsRecordingBackend ? fbSink?.LastVideoBackpressureWaitMs ?? 0 : 0);
        var activeRecordingVideoBackpressureMaxWaitMs = sink?.MaxVideoBackpressureWaitMs ??
                                                        (flashbackIsRecordingBackend ? fbSink?.MaxVideoBackpressureWaitMs ?? 0 : 0);
        var activeRecordingDroppedFrames = sink?.DroppedVideoFrames ??
                                           (flashbackIsRecordingBackend ? fbSink?.DroppedVideoFrames ?? 0 : Interlocked.Read(ref _videoFramesDropped));
        var activeRecordingVideoDropsQueueSaturated = sink?.VideoDropsQueueSaturated ??
                                                      (flashbackIsRecordingBackend ? fbSink?.VideoDropsQueueSaturated ?? 0 : 0);
        var activeRecordingVideoDropsBacklogEviction = sink?.VideoDropsBacklogEviction ??
                                                       (flashbackIsRecordingBackend ? fbSink?.VideoDropsBacklogEviction ?? 0 : 0);
        var activeRecordingAudioQueueDepth = sink?.AudioQueueCount ??
                                             (flashbackIsRecordingBackend ? fbSink?.AudioQueueCount ?? 0 : 0);
        var activeRecordingAudioDropsQueueSaturated = sink?.AudioDropsQueueSaturated ??
                                                      (flashbackIsRecordingBackend ? fbSink?.AudioDropsQueueSaturated ?? 0 : 0);
        var activeRecordingAudioDropsBacklogEviction = sink?.AudioDropsBacklogEviction ??
                                                       (flashbackIsRecordingBackend ? fbSink?.AudioDropsBacklogEviction ?? 0 : 0);
        var activeRecordingLastVideoEnqueueTick = sink?.LastVideoEnqueueTick ??
                                                  (flashbackIsRecordingBackend ? fbSink?.LastVideoEnqueueTick ?? 0 : 0);
        var activeRecordingLastVideoWriteTick = sink?.LastVideoWriteTick ??
                                                (flashbackIsRecordingBackend ? fbSink?.LastVideoWriteTick ?? 0 : 0);
        bool flashbackExportActive;
        long flashbackExportId;
        string flashbackExportStatus;
        string flashbackExportOutputPath;
        long flashbackExportStartedUtcUnixMs;
        long flashbackExportLastProgressUtcUnixMs;
        long flashbackExportCompletedUtcUnixMs;
        int flashbackExportSegmentsProcessed;
        int flashbackExportTotalSegments;
        double flashbackExportPercent;
        long flashbackExportInPointMs;
        long flashbackExportOutPointMs;
        string flashbackExportMessage;
        lock (_flashbackExportDiagnosticsLock)
        {
            flashbackExportActive = _flashbackExportActive;
            flashbackExportId = _flashbackExportId;
            flashbackExportStatus = _flashbackExportStatus;
            flashbackExportOutputPath = _flashbackExportOutputPath;
            flashbackExportStartedUtcUnixMs = _flashbackExportStartedUtcUnixMs;
            flashbackExportLastProgressUtcUnixMs = _flashbackExportLastProgressUtcUnixMs;
            flashbackExportCompletedUtcUnixMs = _flashbackExportCompletedUtcUnixMs;
            flashbackExportSegmentsProcessed = _flashbackExportSegmentsProcessed;
            flashbackExportTotalSegments = _flashbackExportTotalSegments;
            flashbackExportPercent = _flashbackExportPercent;
            flashbackExportInPointMs = _flashbackExportInPointMs;
            flashbackExportOutPointMs = _flashbackExportOutPointMs;
            flashbackExportMessage = _flashbackExportMessage;
        }

        var snapshotUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var flashbackExportElapsedMs = ComputeFlashbackExportElapsedMs(
            flashbackExportActive,
            flashbackExportStartedUtcUnixMs,
            flashbackExportCompletedUtcUnixMs,
            snapshotUtcUnixMs);
        var flashbackExportLastProgressAgeMs = ComputeFlashbackExportLastProgressAgeMs(
            flashbackExportActive,
            flashbackExportStartedUtcUnixMs,
            flashbackExportLastProgressUtcUnixMs,
            snapshotUtcUnixMs);
        var flashbackExportOutputBytes = GetFileLengthOrZero(
            !string.IsNullOrWhiteSpace(flashbackExportOutputPath)
                ? flashbackExportOutputPath
                : _lastExportResult?.OutputPath);
        var flashbackExportThroughputBytesPerSec = flashbackExportElapsedMs > 0
            ? flashbackExportOutputBytes / (flashbackExportElapsedMs / 1000.0)
            : 0;
        var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics() ?? default;
        var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics() ?? default;

        return new CaptureHealthSnapshot
        {
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(snapshotUtcUnixMs),
            SessionState = _sessionState,
            IsRecording = _isRecording,
            RecordingBackend = ResolveRecordingBackendName(),
            FlashbackActive = fbSink != null,
            FlashbackBufferedDurationMs = (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0),
            FlashbackSegmentCount = bufMgr?.SegmentCount ?? 0,
            FlashbackDiskBytes = bufMgr?.TotalDiskBytes ?? 0,
            FlashbackTotalBytesWritten = bufMgr?.TotalBytesWritten ?? 0,
            FlashbackTempDriveFreeBytes = bufMgr?.TempDriveAvailableFreeBytes ?? 0,
            FlashbackStartupCacheBudgetBytes = bufMgr?.StartupCacheBudgetBytes ?? 0,
            FlashbackStartupCacheBytes = bufMgr?.StartupCacheBytes ?? 0,
            FlashbackStartupCacheSessionCount = bufMgr?.StartupCacheSessionCount ?? 0,
            FlashbackStartupCacheDeletedSessionCount = bufMgr?.StartupCacheDeletedSessionCount ?? 0,
            FlashbackStartupCacheFreedBytes = bufMgr?.StartupCacheFreedBytes ?? 0,
            FlashbackStartupCacheOverBudget = bufMgr?.StartupCacheOverBudget ?? false,
            FlashbackOutputBytes = fbSink?.OutputBytes ?? 0,
            FlashbackFilePath = bufMgr?.ActiveFilePath,
            FlashbackEncodedFrames = fbSink?.EncodedVideoFrames ?? 0,
            FlashbackDroppedFrames = fbSink?.DroppedVideoFrames ?? 0,
            FlashbackGpuEncoding = fbSink?.GpuEncodingEnabled ?? false,
            EncoderCodecName = fbSink?.CodecName,
            EncoderTargetBitRate = fbSink?.TargetBitRate ?? 0,
            EncoderWidth = fbSink?.EncoderWidth ?? 0,
            EncoderHeight = fbSink?.EncoderHeight ?? 0,
            EncoderFrameRate = fbSink?.EncoderFrameRate ?? 0,
            FlashbackVideoQueueDepth = fbSink?.VideoQueueCount ?? 0,
            FlashbackAudioQueueDepth = fbSink?.AudioQueueCount ?? 0,
            FlashbackPlaybackState = fbPlayback?.State.ToString() ?? "N/A",
            FlashbackPlaybackPositionMs = (long)(fbPlayback?.PlaybackPosition.TotalMilliseconds ?? 0),
            FlashbackDecoderHwAccel = fbPlayback?.DecoderHwAccel ?? "N/A",
            FlashbackPlaybackFrameCount = fbPlayback?.PlaybackFrameCount ?? 0,
            FlashbackPlaybackLateFrames = fbPlayback?.PlaybackLateFrames ?? 0,
            FlashbackPlaybackDroppedFrames = fbPlayback?.PlaybackDroppedFrames ?? 0,
            FlashbackPlaybackSegmentSwitches = fbPlayback?.PlaybackSegmentSwitches ?? 0,
            FlashbackPlaybackFmp4Reopens = fbPlayback?.PlaybackFmp4Reopens ?? 0,
            FlashbackPlaybackWriteHeadWaits = fbPlayback?.PlaybackWriteHeadWaits ?? 0,
            FlashbackPlaybackNearLiveSnaps = fbPlayback?.PlaybackNearLiveSnaps ?? 0,
            FlashbackPlaybackDecodeErrorSnaps = fbPlayback?.PlaybackDecodeErrorSnaps ?? 0,
            FlashbackPlaybackSubmitFailures = fbPlayback?.PlaybackSubmitFailures ?? 0,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = fbPlayback?.LastSegmentSwitchUtcUnixMs ?? 0,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = fbPlayback?.LastFmp4ReopenUtcUnixMs ?? 0,
            FlashbackPlaybackLastWriteHeadWaitGapMs = fbPlayback?.LastWriteHeadWaitGapMs ?? 0,
            FlashbackPlaybackObservedFps = fbPlayback?.PlaybackObservedFps ?? 0,
            FlashbackPlaybackAvgFrameMs = fbPlayback?.PlaybackAvgFrameMs ?? 0,
            FlashbackPlaybackCadenceSampleCount = playbackCadence.SampleCount,
            FlashbackPlaybackP95FrameMs = playbackCadence.P95FrameMs,
            FlashbackPlaybackP99FrameMs = playbackCadence.P99FrameMs,
            FlashbackPlaybackMaxFrameMs = playbackCadence.MaxFrameMs,
            FlashbackPlaybackSlowFrames = playbackCadence.SlowFrameCount,
            FlashbackPlaybackSlowFramePercent = playbackCadence.SlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = playbackCadence.OnePercentLowFps,
            FlashbackPlaybackDecodeSampleCount = playbackDecode.SampleCount,
            FlashbackPlaybackDecodeAvgMs = playbackDecode.AvgMs,
            FlashbackPlaybackDecodeP95Ms = playbackDecode.P95Ms,
            FlashbackPlaybackDecodeP99Ms = playbackDecode.P99Ms,
            FlashbackPlaybackDecodeMaxMs = playbackDecode.MaxMs,
            FlashbackAvDriftMs = fbPlayback?.AvDriftMs ?? 0,
            FlashbackPlaybackThreadAlive = fbPlayback?.PlaybackThreadAlive ?? false,
            FlashbackPlaybackCommandsEnqueued = fbPlayback?.CommandsEnqueued ?? 0,
            FlashbackPlaybackCommandsProcessed = fbPlayback?.CommandsProcessed ?? 0,
            FlashbackPlaybackCommandsDropped = fbPlayback?.CommandsDropped ?? 0,
            FlashbackPlaybackCommandsSkippedNotReady = fbPlayback?.CommandsSkippedNotReady ?? 0,
            FlashbackPlaybackScrubUpdatesCoalesced = fbPlayback?.ScrubUpdatesCoalesced ?? 0,
            FlashbackPlaybackCommandQueueCapacity = fbPlayback?.CommandQueueCapacityCommands ?? 0,
            FlashbackPlaybackPendingCommands = fbPlayback?.PendingCommands ?? 0,
            FlashbackPlaybackMaxPendingCommands = fbPlayback?.MaxPendingCommands ?? 0,
            FlashbackPlaybackLastCommandQueueLatencyMs = fbPlayback?.LastCommandQueueLatencyMs ?? 0,
            FlashbackPlaybackMaxCommandQueueLatencyMs = fbPlayback?.MaxCommandQueueLatencyMs ?? 0,
            FlashbackPlaybackLastCommandQueued = fbPlayback?.LastCommandQueued ?? "None",
            FlashbackPlaybackLastCommandProcessed = fbPlayback?.LastCommandProcessed ?? "None",
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = fbPlayback?.LastCommandQueuedUtcUnixMs ?? 0,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = fbPlayback?.LastCommandProcessedUtcUnixMs ?? 0,
            FlashbackPlaybackLastCommandFailure = fbPlayback?.LastCommandFailure ?? string.Empty,
            FlashbackExportActive = flashbackExportActive,
            FlashbackExportId = flashbackExportId,
            FlashbackExportStatus = flashbackExportStatus,
            FlashbackExportOutputPath = flashbackExportOutputPath,
            FlashbackExportStartedUtcUnixMs = flashbackExportStartedUtcUnixMs,
            FlashbackExportLastProgressUtcUnixMs = flashbackExportLastProgressUtcUnixMs,
            FlashbackExportCompletedUtcUnixMs = flashbackExportCompletedUtcUnixMs,
            FlashbackExportElapsedMs = flashbackExportElapsedMs,
            FlashbackExportLastProgressAgeMs = flashbackExportLastProgressAgeMs,
            FlashbackExportOutputBytes = flashbackExportOutputBytes,
            FlashbackExportThroughputBytesPerSec = flashbackExportThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = flashbackExportSegmentsProcessed,
            FlashbackExportTotalSegments = flashbackExportTotalSegments,
            FlashbackExportPercent = flashbackExportPercent,
            FlashbackExportInPointMs = flashbackExportInPointMs,
            FlashbackExportOutPointMs = flashbackExportOutPointMs,
            FlashbackExportMessage = flashbackExportMessage,
            LastExportPath = _lastExportResult?.OutputPath,
            LastExportSuccess = _lastExportResult?.Succeeded,
            LastExportMessage = _lastExportResult?.StatusMessage,
            RecordingElapsedMs = _isRecording ? _recordingStopwatch.ElapsedMilliseconds : 0,
            ExpectedFrameRate = _actualFrameRate ?? _currentSettings?.FrameRate ?? 0,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = _currentSettings?.RequestedPixelFormat,
            ReaderSourceStreamType = (_isRecording || _isVideoPreviewActive) && unifiedVideoCapture != null
                ? "MfSourceReader"
                : null,
            ReaderSourceSubtype = _actualPixelFormat,
            FirstObservedFramePixelFormat = observedTelemetry.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = observedTelemetry.LatestObservedFramePixelFormat,
            ObservedP010FrameCount = observedTelemetry.ObservedP010FrameCount,
            ObservedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount,
            ObservedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount,
            SourceTelemetryAvailability = _latestSourceTelemetry.Availability,
            SourceTelemetryOrigin = _latestSourceTelemetry.Origin,
            SourceTelemetryConfidence = _latestSourceTelemetry.Confidence,
            SourceTelemetryOriginDetail = _latestSourceTelemetry.OriginDetail,
            SourceTelemetryDiagnosticSummary = _latestSourceTelemetry.DiagnosticSummary,
            SourceTelemetryTimestampUtc = _latestSourceTelemetry.TimestampUtc,
            SourceWidth = _latestSourceTelemetry.Width,
            SourceHeight = _latestSourceTelemetry.Height,
            SourceFrameRateExact = _latestSourceTelemetry.FrameRateExact,
            SourceFrameRateArg = _latestSourceTelemetry.FrameRateArg,
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
            SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,
            SourceTelemetryBackend = ResolveSourceTelemetryBackend(_latestSourceTelemetry),
            SourceTelemetrySuppressedReason = sourceTelemetrySuppressedReason,
            SourceTelemetrySuppressed = sourceTelemetrySuppressed,
            SourceTelemetryCircuitState = ResolveSourceTelemetryCircuitState(
                _latestSourceTelemetry.Availability,
                sourceTelemetrySuppressed),
            LastFrameArrivalMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            VideoFramesQueued = activeRecordingVideoQueueDepth,
            VideoFramesDropped = activeRecordingDroppedFrames,
            VideoFramesDroppedBacklog = activeRecordingVideoDropsBacklogEviction,
            VideoFramesConverted = sink?.EncodedVideoFrames ?? (flashbackIsRecordingBackend ? fbSink?.EncodedVideoFrames ?? 0 : 0),
            VideoDropsQueueSaturated = activeRecordingVideoDropsQueueSaturated,
            VideoDropsBacklogEviction = activeRecordingVideoDropsBacklogEviction,
            RecordingEncodingFailed = activeRecordingEncodingFailed,
            RecordingEncodingFailureType = activeRecordingFailureType,
            RecordingEncodingFailureMessage = activeRecordingFailureMessage,
            RecordingVideoQueueCapacity = activeRecordingVideoQueueCapacity,
            RecordingVideoQueueMaxDepth = activeRecordingVideoQueueMaxDepth,
            RecordingVideoFramesSubmittedToEncoder = activeRecordingVideoFramesSubmitted,
            RecordingVideoEncoderPts = activeRecordingVideoEncoderPts,
            RecordingVideoEncoderPacketsWritten = activeRecordingVideoEncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = activeRecordingVideoEncoderDroppedFrames,
            RecordingVideoSequenceGaps = activeRecordingVideoSequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = activeRecordingVideoQueueOldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = activeRecordingVideoQueueLastLatencyMs,
            RecordingVideoQueueLatencySampleCount = activeRecordingVideoQueueLatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = activeRecordingVideoQueueLatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = activeRecordingVideoQueueLatencyP95Ms,
            RecordingVideoQueueLatencyMaxMs = activeRecordingVideoQueueLatencyMaxMs,
            RecordingVideoBackpressureWaitMs = activeRecordingVideoBackpressureWaitMs,
            RecordingVideoBackpressureEvents = activeRecordingVideoBackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = activeRecordingVideoBackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = activeRecordingVideoBackpressureMaxWaitMs,
            RecordingGpuQueueDepth = sink?.GpuQueueCount ?? (flashbackIsRecordingBackend ? fbSink?.GpuQueueCount ?? 0 : 0),
            RecordingGpuQueueCapacity = sink?.GpuQueueCapacityFrames ?? (flashbackIsRecordingBackend ? fbSink?.GpuQueueCapacityFrames ?? 0 : 0),
            RecordingGpuQueueMaxDepth = sink?.GpuQueueMaxDepth ?? (flashbackIsRecordingBackend ? fbSink?.GpuQueueMaxDepth ?? 0 : 0),
            RecordingGpuFramesEnqueued = sink?.GpuFramesEnqueued ?? (flashbackIsRecordingBackend ? fbSink?.GpuFramesEnqueued ?? 0 : 0),
            RecordingGpuFramesDropped = sink?.GpuFramesDropped ?? (flashbackIsRecordingBackend ? fbSink?.GpuFramesDropped ?? 0 : 0),
            RecordingCudaQueueDepth = sink?.CudaQueueCount ?? 0,
            RecordingCudaQueueCapacity = sink?.CudaQueueCapacityFrames ?? 0,
            RecordingCudaQueueMaxDepth = sink?.CudaQueueMaxDepth ?? 0,
            RecordingCudaFramesEnqueued = sink?.CudaFramesEnqueued ?? 0,
            RecordingCudaFramesDropped = sink?.CudaFramesDropped ?? 0,
            FlashbackEncodingFailed = flashbackEncodingFailed,
            FlashbackEncodingFailureType = flashbackFailureType,
            FlashbackEncodingFailureMessage = flashbackFailureMessage,
            FlashbackForceRotateActive = fbSink?.IsForceRotateActive ?? false,
            FlashbackVideoQueueCapacity = fbSink?.VideoQueueCapacityFrames ?? 0,
            FlashbackVideoQueueMaxDepth = fbSink?.VideoQueueMaxDepth ?? 0,
            FlashbackVideoFramesSubmittedToEncoder = fbSink?.VideoFramesSubmittedToEncoder ?? 0,
            FlashbackVideoEncoderPts = fbSink?.VideoEncoderPts ?? 0,
            FlashbackVideoEncoderPacketsWritten = fbSink?.VideoEncoderPacketsWritten ?? 0,
            FlashbackVideoEncoderDroppedFrames = fbSink?.VideoEncoderDroppedFrames ?? 0,
            FlashbackVideoSequenceGaps = fbSink?.VideoSequenceGaps ?? 0,
            FlashbackVideoQueueOldestFrameAgeMs = fbSink?.VideoQueueOldestFrameAgeMs ?? 0,
            FlashbackVideoQueueLastLatencyMs = fbSink?.LastVideoQueueLatencyMs ?? 0,
            FlashbackVideoQueueLatencySampleCount = fbSink?.VideoQueueLatencySampleCount ?? 0,
            FlashbackVideoQueueLatencyAvgMs = fbSink?.VideoQueueLatencyAvgMs ?? 0,
            FlashbackVideoQueueLatencyP95Ms = fbSink?.VideoQueueLatencyP95Ms ?? 0,
            FlashbackVideoQueueLatencyMaxMs = fbSink?.VideoQueueLatencyMaxMs ?? 0,
            FlashbackVideoBackpressureWaitMs = fbSink?.VideoBackpressureWaitMs ?? 0,
            FlashbackVideoBackpressureEvents = fbSink?.VideoBackpressureEvents ?? 0,
            FlashbackVideoBackpressureLastWaitMs = fbSink?.LastVideoBackpressureWaitMs ?? 0,
            FlashbackVideoBackpressureMaxWaitMs = fbSink?.MaxVideoBackpressureWaitMs ?? 0,
            FlashbackGpuQueueDepth = fbSink?.GpuQueueCount ?? 0,
            FlashbackGpuQueueCapacity = fbSink?.GpuQueueCapacityFrames ?? 0,
            FlashbackGpuQueueMaxDepth = fbSink?.GpuQueueMaxDepth ?? 0,
            FlashbackGpuFramesEnqueued = fbSink?.GpuFramesEnqueued ?? 0,
            FlashbackGpuFramesDropped = fbSink?.GpuFramesDropped ?? 0,
            AudioDropsQueueSaturated = activeRecordingAudioDropsQueueSaturated,
            AudioDropsBacklogEviction = activeRecordingAudioDropsBacklogEviction,
            AudioChunksDropped = activeRecordingAudioDropsQueueSaturated + activeRecordingAudioDropsBacklogEviction,
            ConversionQueueDepth = 0,
            FfmpegVideoQueueDepth = activeRecordingVideoQueueDepth,
            FfmpegAudioQueueDepth = activeRecordingAudioQueueDepth,
            VideoFramesEnqueued = activeRecordingVideoFramesEnqueued,
            LastVideoEnqueueAgeMs = ComputeTickAge(activeRecordingLastVideoEnqueueTick),
            LastVideoWriteAgeMs = ComputeTickAge(activeRecordingLastVideoWriteTick),
            CaptureCadenceSampleCount = sourceCadence.SampleCount,
            CaptureCadenceObservedFps = sourceCadence.ObservedFps,
            CaptureCadenceExpectedIntervalMs = sourceCadence.ExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = sourceCadence.AverageIntervalMs,
            CaptureCadenceP95IntervalMs = sourceCadence.P95IntervalMs,
            CaptureCadenceP99IntervalMs = sourceCadence.P99IntervalMs,
            CaptureCadenceMaxIntervalMs = sourceCadence.MaxIntervalMs,
            CaptureCadenceOnePercentLowFps = sourceCadence.OnePercentLowFps,
            CaptureCadenceJitterStdDevMs = sourceCadence.JitterStdDevMs,
            CaptureCadenceSevereGapCount = sourceCadence.SevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = sourceCadence.EstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = sourceCadence.EstimatedDropPercent,
            MjpegDecodeSampleCount = mjpegTiming.DecodeSampleCount,
            MjpegDecodeAvgMs = mjpegTiming.DecodeAvgMs,
            MjpegDecodeP95Ms = mjpegTiming.DecodeP95Ms,
            MjpegDecodeMaxMs = mjpegTiming.DecodeMaxMs,
            MjpegInteropCopySampleCount = mjpegTiming.InteropCopySampleCount,
            MjpegInteropCopyAvgMs = mjpegTiming.InteropCopyAvgMs,
            MjpegInteropCopyP95Ms = mjpegTiming.InteropCopyP95Ms,
            MjpegInteropCopyMaxMs = mjpegTiming.InteropCopyMaxMs,
            MjpegCallbackSampleCount = mjpegTiming.CallbackSampleCount,
            MjpegCallbackAvgMs = mjpegTiming.CallbackAvgMs,
            MjpegCallbackP95Ms = mjpegTiming.CallbackP95Ms,
            MjpegCallbackMaxMs = mjpegTiming.CallbackMaxMs,
            MjpegDecoderCount = mjpegFullTiming?.DecoderCount ?? 0,
            MjpegReorderSampleCount = mjpegFullTiming?.ReorderSampleCount ?? 0,
            MjpegReorderAvgMs = mjpegFullTiming?.ReorderAvgMs ?? 0,
            MjpegReorderP95Ms = mjpegFullTiming?.ReorderP95Ms ?? 0,
            MjpegReorderMaxMs = mjpegFullTiming?.ReorderMaxMs ?? 0,
            MjpegPipelineSampleCount = mjpegFullTiming?.PipelineSampleCount ?? 0,
            MjpegPipelineAvgMs = mjpegFullTiming?.PipelineAvgMs ?? 0,
            MjpegPipelineP95Ms = mjpegFullTiming?.PipelineP95Ms ?? 0,
            MjpegPipelineMaxMs = mjpegFullTiming?.PipelineMaxMs ?? 0,
            MjpegTotalDecoded = mjpegFullTiming?.TotalDecoded ?? 0,
            MjpegTotalEmitted = mjpegFullTiming?.TotalEmitted ?? 0,
            MjpegTotalDropped = mjpegFullTiming?.TotalDropped ?? 0,
            MjpegCompressedFramesQueued = mjpegFullTiming?.CompressedFramesQueued ?? 0,
            MjpegCompressedFramesDequeued = mjpegFullTiming?.CompressedFramesDequeued ?? 0,
            MjpegCompressedDropsQueueFull = mjpegFullTiming?.CompressedDropsQueueFull ?? 0,
            MjpegCompressedDropsByteBudget = mjpegFullTiming?.CompressedDropsByteBudget ?? 0,
            MjpegCompressedDropsDisposed = mjpegFullTiming?.CompressedDropsDisposed ?? 0,
            MjpegDecodeFailures = mjpegFullTiming?.DecodeFailures ?? 0,
            MjpegReorderCollisions = mjpegFullTiming?.ReorderCollisions ?? 0,
            MjpegEmitFailures = mjpegFullTiming?.EmitFailures ?? 0,
            MjpegCompressedQueueDepth = mjpegFullTiming?.CompressedQueueDepth ?? 0,
            MjpegCompressedQueueBytes = mjpegFullTiming?.CompressedQueueBytes ?? 0,
            MjpegCompressedQueueByteBudget = mjpegFullTiming?.CompressedQueueByteBudget ?? 0,
            MjpegReorderSkips = mjpegFullTiming?.ReorderSkips ?? 0,
            MjpegReorderBufferDepth = mjpegFullTiming?.ReorderBufferDepth ?? 0,
            MjpegPreviewJitterEnabled = mjpegPreviewJitter.Enabled,
            MjpegPreviewJitterTargetDepth = mjpegPreviewJitter.TargetDepth,
            MjpegPreviewJitterMaxDepth = mjpegPreviewJitter.MaxDepth,
            MjpegPreviewJitterQueueDepth = mjpegPreviewJitter.QueueDepth,
            MjpegPreviewJitterTotalQueued = mjpegPreviewJitter.TotalQueued,
            MjpegPreviewJitterTotalSubmitted = mjpegPreviewJitter.TotalSubmitted,
            MjpegPreviewJitterTotalDropped = mjpegPreviewJitter.TotalDropped,
            MjpegPreviewJitterUnderflowCount = mjpegPreviewJitter.UnderflowCount,
            MjpegPreviewJitterInputSampleCount = mjpegPreviewJitter.InputIntervalSampleCount,
            MjpegPreviewJitterInputAvgMs = mjpegPreviewJitter.InputIntervalAvgMs,
            MjpegPreviewJitterInputP95Ms = mjpegPreviewJitter.InputIntervalP95Ms,
            MjpegPreviewJitterInputMaxMs = mjpegPreviewJitter.InputIntervalMaxMs,
            MjpegPreviewJitterOutputSampleCount = mjpegPreviewJitter.OutputIntervalSampleCount,
            MjpegPreviewJitterOutputAvgMs = mjpegPreviewJitter.OutputIntervalAvgMs,
            MjpegPreviewJitterOutputP95Ms = mjpegPreviewJitter.OutputIntervalP95Ms,
            MjpegPreviewJitterOutputMaxMs = mjpegPreviewJitter.OutputIntervalMaxMs,
            MjpegPreviewJitterLatencySampleCount = mjpegPreviewJitter.QueueLatencySampleCount,
            MjpegPreviewJitterLatencyAvgMs = mjpegPreviewJitter.QueueLatencyAvgMs,
            MjpegPreviewJitterLatencyP95Ms = mjpegPreviewJitter.QueueLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = mjpegPreviewJitter.QueueLatencyMaxMs,
            MjpegPreviewJitterDeadlineDropCount = mjpegPreviewJitter.DeadlineDropCount,
            MjpegPreviewJitterTargetIncreaseCount = mjpegPreviewJitter.TargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = mjpegPreviewJitter.TargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = mjpegPreviewJitter.LastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = mjpegPreviewJitter.LastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = mjpegPreviewJitter.LastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = mjpegPreviewJitter.LastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = mjpegPreviewJitter.LastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = mjpegPreviewJitter.LastDropQpc,
            MjpegPreviewJitterLastDropReason = mjpegPreviewJitter.LastDropReason ?? string.Empty,
            MjpegPacketHashSampleCount = mjpegPacketHash.SampleCount,
            MjpegPacketHashUniqueFrameCount = mjpegPacketHash.UniqueFrameCount,
            MjpegPacketHashDuplicateFrameCount = mjpegPacketHash.DuplicateFrameCount,
            MjpegPacketHashLongestDuplicateRun = mjpegPacketHash.LongestDuplicateRun,
            MjpegPacketHashInputObservedFps = mjpegPacketHash.InputObservedFps,
            MjpegPacketHashUniqueObservedFps = mjpegPacketHash.UniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = mjpegPacketHash.DuplicateFramePercent,
            MjpegPacketHashLastHash = mjpegPacketHash.LastHash,
            MjpegPacketHashLastFrameDuplicate = mjpegPacketHash.LastFrameDuplicate,
            MjpegPacketHashPattern = mjpegPacketHash.Pattern,
            MjpegPacketHashRecentInputIntervalsMs = mjpegPacketHash.RecentInputIntervalsMs,
            MjpegPacketHashRecentUniqueIntervalsMs = mjpegPacketHash.RecentUniqueIntervalsMs,
            MjpegPacketHashRecentDuplicateFlags = mjpegPacketHash.RecentDuplicateFlags,
            VisualCadenceSampleCount = visualCadence.SampleCount,
            VisualCadenceChangedFrameCount = visualCadence.ChangedFrameCount,
            VisualCadenceRepeatFrameCount = visualCadence.RepeatFrameCount,
            VisualCadenceLongestRepeatRun = visualCadence.LongestRepeatRun,
            VisualCadenceOutputObservedFps = visualCadence.OutputObservedFps,
            VisualCadenceChangeObservedFps = visualCadence.ChangeObservedFps,
            VisualCadenceRepeatFramePercent = visualCadence.RepeatFramePercent,
            VisualCadenceLastDelta = visualCadence.LastDelta,
            VisualCadenceAverageDelta = visualCadence.AverageDelta,
            VisualCadenceP95Delta = visualCadence.P95Delta,
            VisualCadenceMotionScore = visualCadence.MotionScore,
            VisualCadenceMotionConfidence = visualCadence.MotionConfidence,
            VisualCadenceRecentOutputIntervalsMs = visualCadence.RecentOutputIntervalsMs,
            VisualCadenceRecentChangeIntervalsMs = visualCadence.RecentChangeIntervalsMs,
            VisualCenterCadenceSampleCount = visualCenterCadence.SampleCount,
            VisualCenterCadenceChangedFrameCount = visualCenterCadence.ChangedFrameCount,
            VisualCenterCadenceRepeatFrameCount = visualCenterCadence.RepeatFrameCount,
            VisualCenterCadenceLongestRepeatRun = visualCenterCadence.LongestRepeatRun,
            VisualCenterCadenceOutputObservedFps = visualCenterCadence.OutputObservedFps,
            VisualCenterCadenceChangeObservedFps = visualCenterCadence.ChangeObservedFps,
            VisualCenterCadenceRepeatFramePercent = visualCenterCadence.RepeatFramePercent,
            VisualCenterCadenceLastDelta = visualCenterCadence.LastDelta,
            VisualCenterCadenceAverageDelta = visualCenterCadence.AverageDelta,
            VisualCenterCadenceP95Delta = visualCenterCadence.P95Delta,
            VisualCenterCadenceMotionScore = visualCenterCadence.MotionScore,
            VisualCenterCadenceMotionConfidence = visualCenterCadence.MotionConfidence,
            VisualCenterCadenceRecentOutputIntervalsMs = visualCenterCadence.RecentOutputIntervalsMs,
            VisualCenterCadenceRecentChangeIntervalsMs = visualCenterCadence.RecentChangeIntervalsMs,
            MjpegPerDecoder = mjpegFullTiming?.PerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderHealthSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderHealthSnapshot>(),
            AvSyncCaptureDriftMs = avSyncDriftMs,
            AvSyncCaptureDriftRateMsPerSec = avSyncDriftRate,
            AvSyncEncoderDriftMs = avSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples = avSyncEncoderCorrectionSamples
        };
    }
}
