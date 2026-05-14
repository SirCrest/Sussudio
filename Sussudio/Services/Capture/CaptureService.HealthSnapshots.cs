using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Health snapshot projection for diagnostics and automation health checks.
// Keep this read-only; lifecycle mutations belong in coordinator/transition paths.
public partial class CaptureService
{
    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var sink = _libavSink;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var fbSink = _flashbackSink;
        var bufMgr = _flashbackBufferManager;
        var fbPlayback = _flashbackPlaybackController;
        var fatalCleanupInProgress = Volatile.Read(ref _fatalCleanupInProgress) != 0;
        var flashbackCleanupInProgress = Volatile.Read(ref _flashbackCleanupInProgress) != 0;
        var observedTelemetry = ResolveObservedFrameTelemetry();
        var videoFramesDropped = sink?.DroppedVideoFrames ?? Interlocked.Read(ref _videoFramesDropped);
        var sourceTelemetrySuppressedReason = ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry);
        var sourceTelemetrySuppressed = !string.IsNullOrWhiteSpace(sourceTelemetrySuppressedReason);
        var sourceCadence = unifiedVideoCapture?.GetSourceCadenceMetrics()
            ?? default(MfSourceReaderVideoCapture.SourceCadenceMetrics);
        var mjpegTimingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();
        var mjpegTiming = mjpegTimingSnapshot?.Summary ?? _lastMjpegPipelineTimingMetrics;
        var mjpegFullTiming = mjpegTimingSnapshot?.Details ?? _lastFullMjpegPipelineTimingMetrics;
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
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) emptyVideoQueueLatencyMetrics = default;
        var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics ?? emptyVideoQueueLatencyMetrics;
        var activeRecordingVideoQueueLatencyMetrics = sink?.VideoQueueLatencyMetrics ??
                                                      (flashbackIsRecordingBackend
                                                          ? flashbackVideoQueueLatencyMetrics
                                                          : emptyVideoQueueLatencyMetrics);
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
        var flashbackExport = CaptureFlashbackExportHealthSnapshotFields();
        var flashbackBackendSettings = _flashbackBackendSettings;

        var snapshotUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var flashbackExportElapsedMs = ComputeFlashbackExportElapsedMs(
            flashbackExport.Active,
            flashbackExport.StartedUtcUnixMs,
            flashbackExport.CompletedUtcUnixMs,
            snapshotUtcUnixMs);
        var flashbackExportLastProgressAgeMs = ComputeFlashbackExportLastProgressAgeMs(
            flashbackExport.Active,
            flashbackExport.StartedUtcUnixMs,
            flashbackExport.LastProgressUtcUnixMs,
            snapshotUtcUnixMs);
        var flashbackExportOutputBytes = GetFileLengthOrZero(
            !string.IsNullOrWhiteSpace(flashbackExport.OutputPath)
                ? flashbackExport.OutputPath
                : flashbackExport.LastResult?.OutputPath);
        var flashbackExportThroughputBytesPerSec = flashbackExportElapsedMs > 0
            ? flashbackExportOutputBytes / (flashbackExportElapsedMs / 1000.0)
            : 0;
        var flashbackBackendSettingsStaleReason = fbSink == null
            ? string.Empty
            : ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, _currentSettings);
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
            FlashbackBackendSettingsStale = !string.IsNullOrEmpty(flashbackBackendSettingsStaleReason),
            FlashbackBackendSettingsStaleReason = flashbackBackendSettingsStaleReason,
            FlashbackBackendActiveFormat = flashbackBackendSettings?.Format.ToString() ?? string.Empty,
            FlashbackBackendRequestedFormat = _currentSettings?.Format.ToString() ?? string.Empty,
            FlashbackBackendActivePreset = flashbackBackendSettings?.NvencPreset.ToString() ?? string.Empty,
            FlashbackBackendRequestedPreset = _currentSettings?.NvencPreset.ToString() ?? string.Empty,
            EncoderCodecName = fbSink?.CodecName,
            EncoderTargetBitRate = fbSink?.TargetBitRate ?? 0,
            EncoderWidth = fbSink?.EncoderWidth ?? 0,
            EncoderHeight = fbSink?.EncoderHeight ?? 0,
            EncoderFrameRate = fbSink?.EncoderFrameRate ?? 0,
            EncoderFrameRateNumerator = fbSink?.EncoderFrameRateNumerator,
            EncoderFrameRateDenominator = fbSink?.EncoderFrameRateDenominator,
            FlashbackVideoQueueDepth = fbSink?.VideoQueueCount ?? 0,
            FlashbackAudioQueueDepth = fbSink?.AudioQueueCount ?? 0,
            FlashbackAudioQueueCapacity = fbSink?.AudioQueueCapacityPackets ?? 0,
            FlashbackPlaybackState = fbPlayback?.State.ToString() ?? "N/A",
            FlashbackPlaybackPositionMs = (long)(fbPlayback?.PlaybackPosition.TotalMilliseconds ?? 0),
            FlashbackDecoderHwAccel = fbPlayback?.DecoderHwAccel ?? "N/A",
            FlashbackPlaybackFrameCount = fbPlayback?.PlaybackFrameCount ?? 0,
            FlashbackPlaybackLateFrames = fbPlayback?.PlaybackLateFrames ?? 0,
            FlashbackPlaybackDroppedFrames = fbPlayback?.PlaybackDroppedFrames ?? 0,
            FlashbackPlaybackAudioMasterDelayDoubles = fbPlayback?.PlaybackAudioMasterDelayDoubles ?? 0,
            FlashbackPlaybackAudioMasterDelayShrinks = fbPlayback?.PlaybackAudioMasterDelayShrinks ?? 0,
            FlashbackPlaybackAudioMasterFallbacks = fbPlayback?.PlaybackAudioMasterFallbacks ?? 0,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = fbPlayback?.PlaybackAudioMasterUnavailableFallbacks ?? 0,
            FlashbackPlaybackAudioMasterStaleFallbacks = fbPlayback?.PlaybackAudioMasterStaleFallbacks ?? 0,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = fbPlayback?.PlaybackAudioMasterDriftOutlierFallbacks ?? 0,
            FlashbackPlaybackAudioMasterLastFallbackReason = fbPlayback?.PlaybackAudioMasterLastFallbackReason ?? string.Empty,
            FlashbackPlaybackAudioMasterLastFallbackDriftMs = fbPlayback?.PlaybackAudioMasterLastFallbackDriftMs ?? 0,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = fbPlayback?.PlaybackAudioMasterLastFallbackClockAgeMs ?? 0,
            FlashbackPlaybackSegmentSwitches = fbPlayback?.PlaybackSegmentSwitches ?? 0,
            FlashbackPlaybackFmp4Reopens = fbPlayback?.PlaybackFmp4Reopens ?? 0,
            FlashbackPlaybackWriteHeadWaits = fbPlayback?.PlaybackWriteHeadWaits ?? 0,
            FlashbackPlaybackNearLiveSnaps = fbPlayback?.PlaybackNearLiveSnaps ?? 0,
            FlashbackPlaybackDecodeErrorSnaps = fbPlayback?.PlaybackDecodeErrorSnaps ?? 0,
            FlashbackPlaybackSubmitFailures = fbPlayback?.PlaybackSubmitFailures ?? 0,
            FlashbackPlaybackLastDropUtcUnixMs = fbPlayback?.LastPlaybackDropUtcUnixMs ?? 0,
            FlashbackPlaybackLastDropReason = fbPlayback?.LastPlaybackDropReason ?? string.Empty,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = fbPlayback?.LastSubmitFailureUtcUnixMs ?? 0,
            FlashbackPlaybackLastSubmitFailure = fbPlayback?.LastSubmitFailure ?? string.Empty,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = fbPlayback?.LastSegmentSwitchUtcUnixMs ?? 0,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = fbPlayback?.LastFmp4ReopenUtcUnixMs ?? 0,
            FlashbackPlaybackLastWriteHeadWaitGapMs = fbPlayback?.LastWriteHeadWaitGapMs ?? 0,
            FlashbackPlaybackTargetFps = fbPlayback?.PlaybackTargetFps ?? 0,
            FlashbackPlaybackObservedFps = fbPlayback?.PlaybackObservedFps ?? 0,
            FlashbackPlaybackAvgFrameMs = fbPlayback?.PlaybackAvgFrameMs ?? 0,
            FlashbackPlaybackCadenceSampleCount = playbackCadence.SampleCount,
            FlashbackPlaybackP95FrameMs = playbackCadence.P95FrameMs,
            FlashbackPlaybackP99FrameMs = playbackCadence.P99FrameMs,
            FlashbackPlaybackMaxFrameMs = playbackCadence.MaxFrameMs,
            FlashbackPlaybackSlowFrames = playbackCadence.SlowFrameCount,
            FlashbackPlaybackSlowFramePercent = playbackCadence.SlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = playbackCadence.OnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = playbackCadence.FivePercentLowFps,
            FlashbackPlaybackSampleDurationMs = playbackCadence.SampleDurationMs,
            FlashbackPlaybackRecentFrameIntervalsMs = playbackCadence.RecentFrameIntervalsMs,
            FlashbackPlaybackPtsCadenceMismatchCount = fbPlayback?.PlaybackPtsCadenceMismatchCount ?? 0,
            FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = fbPlayback?.LastPlaybackPtsCadenceMismatchUtcUnixMs ?? 0,
            FlashbackPlaybackLastPtsCadenceDeltaMs = fbPlayback?.LastPlaybackPtsCadenceDeltaMs ?? 0,
            FlashbackPlaybackLastPtsCadenceExpectedMs = fbPlayback?.LastPlaybackPtsCadenceExpectedMs ?? 0,
            FlashbackPlaybackSeekForwardDecodeCapHits = fbPlayback?.PlaybackSeekForwardDecodeCapHits ?? 0,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = fbPlayback?.LastPlaybackSeekHitForwardDecodeCap ?? false,
            FlashbackPlaybackDecodeSampleCount = playbackDecode.SampleCount,
            FlashbackPlaybackDecodeAvgMs = playbackDecode.AvgMs,
            FlashbackPlaybackDecodeP95Ms = playbackDecode.P95Ms,
            FlashbackPlaybackDecodeP99Ms = playbackDecode.P99Ms,
            FlashbackPlaybackDecodeMaxMs = playbackDecode.MaxMs,
            FlashbackPlaybackMaxDecodePhase = fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty,
            FlashbackPlaybackMaxDecodeReceiveMs = fbPlayback?.PlaybackMaxDecodeReceiveMs ?? 0,
            FlashbackPlaybackMaxDecodeFeedMs = fbPlayback?.PlaybackMaxDecodeFeedMs ?? 0,
            FlashbackPlaybackMaxDecodeReadMs = fbPlayback?.PlaybackMaxDecodeReadMs ?? 0,
            FlashbackPlaybackMaxDecodeSendMs = fbPlayback?.PlaybackMaxDecodeSendMs ?? 0,
            FlashbackPlaybackMaxDecodeAudioMs = fbPlayback?.PlaybackMaxDecodeAudioMs ?? 0,
            FlashbackPlaybackMaxDecodeConvertMs = fbPlayback?.PlaybackMaxDecodeConvertMs ?? 0,
            FlashbackPlaybackMaxDecodeUtcUnixMs = fbPlayback?.PlaybackMaxDecodeUtcUnixMs ?? 0,
            FlashbackPlaybackMaxDecodePositionMs = fbPlayback?.PlaybackMaxDecodePositionMs ?? 0,
            FlashbackAvDriftMs = fbPlayback?.AvDriftMs ?? 0,
            FlashbackPlaybackThreadAlive = fbPlayback?.PlaybackThreadAlive ?? false,
            FlashbackPlaybackCommandsEnqueued = fbPlayback?.CommandsEnqueued ?? 0,
            FlashbackPlaybackCommandsProcessed = fbPlayback?.CommandsProcessed ?? 0,
            FlashbackPlaybackCommandsDropped = fbPlayback?.CommandsDropped ?? 0,
            FlashbackPlaybackCommandsSkippedNotReady = fbPlayback?.CommandsSkippedNotReady ?? 0,
            FlashbackPlaybackScrubUpdatesCoalesced = fbPlayback?.ScrubUpdatesCoalesced ?? 0,
            FlashbackPlaybackSeekCommandsCoalesced = fbPlayback?.SeekCommandsCoalesced ?? 0,
            FlashbackPlaybackCommandQueueCapacity = fbPlayback?.CommandQueueCapacityCommands ?? 0,
            FlashbackPlaybackPendingCommands = fbPlayback?.PendingCommands ?? 0,
            FlashbackPlaybackMaxPendingCommands = fbPlayback?.MaxPendingCommands ?? 0,
            FlashbackPlaybackLastCommandQueueLatencyMs = fbPlayback?.LastCommandQueueLatencyMs ?? 0,
            FlashbackPlaybackMaxCommandQueueLatencyMs = fbPlayback?.MaxCommandQueueLatencyMs ?? 0,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = fbPlayback?.MaxCommandQueueLatencyCommand ?? "None",
            FlashbackPlaybackLastCommandQueued = fbPlayback?.LastCommandQueued ?? "None",
            FlashbackPlaybackLastCommandProcessed = fbPlayback?.LastCommandProcessed ?? "None",
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = fbPlayback?.LastCommandQueuedUtcUnixMs ?? 0,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = fbPlayback?.LastCommandProcessedUtcUnixMs ?? 0,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = fbPlayback?.LastCommandFailureUtcUnixMs ?? 0,
            FlashbackPlaybackLastCommandFailure = fbPlayback?.LastCommandFailure ?? string.Empty,
            FlashbackExportActive = flashbackExport.Active,
            FlashbackExportId = flashbackExport.Id,
            FlashbackExportStatus = flashbackExport.Status,
            FlashbackExportOutputPath = flashbackExport.OutputPath,
            FlashbackExportStartedUtcUnixMs = flashbackExport.StartedUtcUnixMs,
            FlashbackExportLastProgressUtcUnixMs = flashbackExport.LastProgressUtcUnixMs,
            FlashbackExportCompletedUtcUnixMs = flashbackExport.CompletedUtcUnixMs,
            FlashbackExportElapsedMs = flashbackExportElapsedMs,
            FlashbackExportLastProgressAgeMs = flashbackExportLastProgressAgeMs,
            FlashbackExportOutputBytes = flashbackExportOutputBytes,
            FlashbackExportThroughputBytesPerSec = flashbackExportThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = flashbackExport.SegmentsProcessed,
            FlashbackExportTotalSegments = flashbackExport.TotalSegments,
            FlashbackExportPercent = flashbackExport.Percent,
            FlashbackExportInPointMs = flashbackExport.InPointMs,
            FlashbackExportOutPointMs = flashbackExport.OutPointMs,
            FlashbackExportMessage = flashbackExport.Message,
            FlashbackExportFailureKind = flashbackExport.FailureKind,
            FlashbackExportForceRotateFallbacks = flashbackExport.ForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = flashbackExport.LastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = flashbackExport.LastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = flashbackExport.LastForceRotateFallbackOutPointMs,
            // Surface the silent codec/preset substitution alongside the existing
            // export status so automation, the verifier, and (eventually) the UI
            // can show what was actually encoded vs what the user requested.
            FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(_currentSettings, unifiedVideoCapture),
            FlashbackCodecDowngradeReason = ResolveFlashbackCodecDowngradeReason(_currentSettings, unifiedVideoCapture),
            LastExportId = flashbackExport.LastResultId,
            LastExportPath = flashbackExport.LastResult?.OutputPath,
            LastExportSuccess = flashbackExport.LastResult?.Succeeded,
            LastExportMessage = flashbackExport.LastResult?.StatusMessage,
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
            RecordingVideoQueueLatencySampleCount = activeRecordingVideoQueueLatencyMetrics.SampleCount,
            RecordingVideoQueueLatencyAvgMs = activeRecordingVideoQueueLatencyMetrics.AverageMs,
            RecordingVideoQueueLatencyP95Ms = activeRecordingVideoQueueLatencyMetrics.P95Ms,
            RecordingVideoQueueLatencyP99Ms = activeRecordingVideoQueueLatencyMetrics.P99Ms,
            RecordingVideoQueueLatencyMaxMs = activeRecordingVideoQueueLatencyMetrics.MaxMs,
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
            FatalCleanupInProgress = fatalCleanupInProgress,
            FlashbackCleanupInProgress = flashbackCleanupInProgress,
            FlashbackForceRotateActive = fbSink?.IsForceRotateActive ?? false,
            FlashbackForceRotateRequested = fbSink?.IsForceRotateRequested ?? false,
            FlashbackForceRotateDraining = fbSink?.IsForceRotateDraining ?? false,
            FlashbackVideoQueueCapacity = fbSink?.VideoQueueCapacityFrames ?? 0,
            FlashbackVideoQueueMaxDepth = fbSink?.VideoQueueMaxDepth ?? 0,
            FlashbackVideoFramesSubmittedToEncoder = fbSink?.VideoFramesSubmittedToEncoder ?? 0,
            FlashbackVideoEncoderPts = fbSink?.VideoEncoderPts ?? 0,
            FlashbackVideoEncoderPacketsWritten = fbSink?.VideoEncoderPacketsWritten ?? 0,
            FlashbackVideoEncoderDroppedFrames = fbSink?.VideoEncoderDroppedFrames ?? 0,
            FlashbackVideoSequenceGaps = fbSink?.VideoSequenceGaps ?? 0,
            FlashbackVideoQueueRejectedFrames = fbSink?.VideoQueueRejectedFrames ?? 0,
            FlashbackVideoQueueLastRejectReason = fbSink?.LastVideoQueueRejectReason ?? string.Empty,
            FlashbackVideoQueueOldestFrameAgeMs = fbSink?.VideoQueueOldestFrameAgeMs ?? 0,
            FlashbackVideoQueueLastLatencyMs = fbSink?.LastVideoQueueLatencyMs ?? 0,
            FlashbackVideoQueueLatencySampleCount = flashbackVideoQueueLatencyMetrics.SampleCount,
            FlashbackVideoQueueLatencyAvgMs = flashbackVideoQueueLatencyMetrics.AverageMs,
            FlashbackVideoQueueLatencyP95Ms = flashbackVideoQueueLatencyMetrics.P95Ms,
            FlashbackVideoQueueLatencyP99Ms = flashbackVideoQueueLatencyMetrics.P99Ms,
            FlashbackVideoQueueLatencyMaxMs = flashbackVideoQueueLatencyMetrics.MaxMs,
            FlashbackVideoBackpressureWaitMs = fbSink?.VideoBackpressureWaitMs ?? 0,
            FlashbackVideoBackpressureEvents = fbSink?.VideoBackpressureEvents ?? 0,
            FlashbackVideoBackpressureLastWaitMs = fbSink?.LastVideoBackpressureWaitMs ?? 0,
            FlashbackVideoBackpressureMaxWaitMs = fbSink?.MaxVideoBackpressureWaitMs ?? 0,
            FlashbackGpuQueueDepth = fbSink?.GpuQueueCount ?? 0,
            FlashbackGpuQueueCapacity = fbSink?.GpuQueueCapacityFrames ?? 0,
            FlashbackGpuQueueMaxDepth = fbSink?.GpuQueueMaxDepth ?? 0,
            FlashbackGpuFramesEnqueued = fbSink?.GpuFramesEnqueued ?? 0,
            FlashbackGpuFramesDropped = fbSink?.GpuFramesDropped ?? 0,
            FlashbackGpuQueueRejectedFrames = fbSink?.GpuQueueRejectedFrames ?? 0,
            FlashbackGpuQueueLastRejectReason = fbSink?.LastGpuQueueRejectReason ?? string.Empty,
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
            CaptureCadenceFivePercentLowFps = sourceCadence.FivePercentLowFps,
            CaptureCadenceSampleDurationMs = sourceCadence.SampleDurationMs,
            CaptureCadenceRecentIntervalsMs = sourceCadence.RecentIntervalsMs,
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
            MjpegPreviewJitterResumeReprimeCount = mjpegPreviewJitter.ResumeReprimeCount,
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
            MjpegPreviewJitterClearedDropCount = mjpegPreviewJitter.ClearedDropCount,
            MjpegPreviewJitterTargetIncreaseCount = mjpegPreviewJitter.TargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = mjpegPreviewJitter.TargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = mjpegPreviewJitter.LastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = mjpegPreviewJitter.LastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = mjpegPreviewJitter.LastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = mjpegPreviewJitter.LastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = mjpegPreviewJitter.LastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = mjpegPreviewJitter.LastDropQpc,
            MjpegPreviewJitterLastDropReason = mjpegPreviewJitter.LastDropReason ?? string.Empty,
            MjpegPreviewJitterLastUnderflowQpc = mjpegPreviewJitter.LastUnderflowQpc,
            MjpegPreviewJitterLastUnderflowReason = mjpegPreviewJitter.LastUnderflowReason ?? string.Empty,
            MjpegPreviewJitterLastUnderflowQueueDepth = mjpegPreviewJitter.LastUnderflowQueueDepth,
            MjpegPreviewJitterLastUnderflowInputAgeMs = mjpegPreviewJitter.LastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = mjpegPreviewJitter.LastUnderflowOutputAgeMs,
            MjpegPreviewJitterLastScheduleLateMs = mjpegPreviewJitter.LastScheduleLateMs,
            MjpegPreviewJitterMaxScheduleLateMs = mjpegPreviewJitter.MaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = mjpegPreviewJitter.ScheduleLateCount,
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
