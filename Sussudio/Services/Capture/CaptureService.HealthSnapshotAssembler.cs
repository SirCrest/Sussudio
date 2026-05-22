using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Health snapshot DTO construction from already-sampled field groups.
public partial class CaptureService
{
    private readonly record struct CaptureHealthSnapshotAssemblyFields
    {
        public CaptureSessionState SessionState { get; init; }

        public bool IsRecording { get; init; }

        public string RecordingBackend { get; init; }

        public long RecordingElapsedMs { get; init; }

        public double ExpectedFrameRate { get; init; }

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

        public string? FlashbackExportVerificationFormat { get; init; }

        public string? FlashbackCodecDowngradeReason { get; init; }

        public long LastFrameArrivalMs { get; init; }

        public long VideoFramesArrived { get; init; }

        public long LastVideoEnqueueAgeMs { get; init; }

        public long LastVideoWriteAgeMs { get; init; }

        public bool FatalCleanupInProgress { get; init; }

        public bool FlashbackCleanupInProgress { get; init; }

        public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }

        public SourceTelemetryHealthSnapshotFields SourceTelemetry { get; init; }

        public CaptureCadenceHealthSnapshotFields CaptureCadence { get; init; }

        public MjpegHealthSnapshotFields MjpegHealth { get; init; }

        public AvSyncHealthSnapshotFields AvSyncHealth { get; init; }

        public RecordingHealthSnapshotFields RecordingHealth { get; init; }

        public FlashbackQueueHealthSnapshotFields FlashbackQueues { get; init; }

        public long SnapshotUtcUnixMs { get; init; }

        public FlashbackExportHealthSnapshotFields FlashbackExport { get; init; }

        public FlashbackBufferHealthSnapshotFields FlashbackBuffer { get; init; }

        public FlashbackPlaybackHealthSnapshotFields FlashbackPlayback { get; init; }
    }

    private static class CaptureHealthSnapshotAssembler
    {
        public static CaptureHealthSnapshot Build(CaptureHealthSnapshotAssemblyFields fields)
        {
            var fatalCleanupInProgress = fields.FatalCleanupInProgress;
            var flashbackCleanupInProgress = fields.FlashbackCleanupInProgress;
            var observedTelemetry = fields.ObservedTelemetry;
            var sourceTelemetry = fields.SourceTelemetry;
            var captureCadence = fields.CaptureCadence;
            var mjpegHealth = fields.MjpegHealth;
            var avSyncHealth = fields.AvSyncHealth;
            var recordingHealth = fields.RecordingHealth;
            var flashbackQueues = fields.FlashbackQueues;
            var snapshotUtcUnixMs = fields.SnapshotUtcUnixMs;
            var flashbackExport = fields.FlashbackExport;
            var flashbackBuffer = fields.FlashbackBuffer;
            var flashbackPlayback = fields.FlashbackPlayback;

            return new CaptureHealthSnapshot
            {
                TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(snapshotUtcUnixMs),
                SessionState = fields.SessionState,
                IsRecording = fields.IsRecording,
                RecordingBackend = fields.RecordingBackend,
                FlashbackActive = flashbackBuffer.Active,
                FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,
                FlashbackSegmentCount = flashbackBuffer.SegmentCount,
                FlashbackDiskBytes = flashbackBuffer.DiskBytes,
                FlashbackTotalBytesWritten = flashbackBuffer.TotalBytesWritten,
                FlashbackTempDriveFreeBytes = flashbackBuffer.TempDriveFreeBytes,
                FlashbackStartupCacheBudgetBytes = flashbackBuffer.StartupCacheBudgetBytes,
                FlashbackStartupCacheBytes = flashbackBuffer.StartupCacheBytes,
                FlashbackStartupCacheSessionCount = flashbackBuffer.StartupCacheSessionCount,
                FlashbackStartupCacheDeletedSessionCount = flashbackBuffer.StartupCacheDeletedSessionCount,
                FlashbackStartupCacheFreedBytes = flashbackBuffer.StartupCacheFreedBytes,
                FlashbackStartupCacheOverBudget = flashbackBuffer.StartupCacheOverBudget,
                FlashbackOutputBytes = flashbackBuffer.OutputBytes,
                FlashbackFilePath = flashbackBuffer.FilePath,
                FlashbackEncodedFrames = flashbackBuffer.EncodedFrames,
                FlashbackDroppedFrames = flashbackBuffer.DroppedFrames,
                FlashbackGpuEncoding = flashbackBuffer.GpuEncoding,
                FlashbackBackendSettingsStale = flashbackBuffer.BackendSettingsStale,
                FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,
                FlashbackBackendActiveFormat = flashbackBuffer.BackendActiveFormat,
                FlashbackBackendRequestedFormat = flashbackBuffer.BackendRequestedFormat,
                FlashbackBackendActivePreset = flashbackBuffer.BackendActivePreset,
                FlashbackBackendRequestedPreset = flashbackBuffer.BackendRequestedPreset,
                EncoderCodecName = flashbackBuffer.EncoderCodecName,
                EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,
                EncoderWidth = flashbackBuffer.EncoderWidth,
                EncoderHeight = flashbackBuffer.EncoderHeight,
                EncoderFrameRate = flashbackBuffer.EncoderFrameRate,
                EncoderFrameRateNumerator = flashbackBuffer.EncoderFrameRateNumerator,
                EncoderFrameRateDenominator = flashbackBuffer.EncoderFrameRateDenominator,
                FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,
                FlashbackAudioQueueDepth = flashbackQueues.AudioQueueDepth,
                FlashbackAudioQueueCapacity = flashbackQueues.AudioQueueCapacity,
                FlashbackPlaybackState = flashbackPlayback.State,
                FlashbackPlaybackPositionMs = flashbackPlayback.PositionMs,
                FlashbackDecoderHwAccel = flashbackPlayback.DecoderHwAccel,
                FlashbackPlaybackFrameCount = flashbackPlayback.FrameCount,
                FlashbackPlaybackLateFrames = flashbackPlayback.LateFrames,
                FlashbackPlaybackDroppedFrames = flashbackPlayback.DroppedFrames,
                FlashbackPlaybackAudioMasterDelayDoubles = flashbackPlayback.AudioMasterDelayDoubles,
                FlashbackPlaybackAudioMasterDelayShrinks = flashbackPlayback.AudioMasterDelayShrinks,
                FlashbackPlaybackAudioMasterFallbacks = flashbackPlayback.AudioMasterFallbacks,
                FlashbackPlaybackAudioMasterUnavailableFallbacks = flashbackPlayback.AudioMasterUnavailableFallbacks,
                FlashbackPlaybackAudioMasterStaleFallbacks = flashbackPlayback.AudioMasterStaleFallbacks,
                FlashbackPlaybackAudioMasterDriftOutlierFallbacks = flashbackPlayback.AudioMasterDriftOutlierFallbacks,
                FlashbackPlaybackAudioMasterLastFallbackReason = flashbackPlayback.AudioMasterLastFallbackReason,
                FlashbackPlaybackAudioMasterLastFallbackDriftMs = flashbackPlayback.AudioMasterLastFallbackDriftMs,
                FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = flashbackPlayback.AudioMasterLastFallbackClockAgeMs,
                FlashbackPlaybackSegmentSwitches = flashbackPlayback.SegmentSwitches,
                FlashbackPlaybackFmp4Reopens = flashbackPlayback.Fmp4Reopens,
                FlashbackPlaybackWriteHeadWaits = flashbackPlayback.WriteHeadWaits,
                FlashbackPlaybackNearLiveSnaps = flashbackPlayback.NearLiveSnaps,
                FlashbackPlaybackDecodeErrorSnaps = flashbackPlayback.DecodeErrorSnaps,
                FlashbackPlaybackSubmitFailures = flashbackPlayback.SubmitFailures,
                FlashbackPlaybackLastDropUtcUnixMs = flashbackPlayback.LastDropUtcUnixMs,
                FlashbackPlaybackLastDropReason = flashbackPlayback.LastDropReason,
                FlashbackPlaybackLastSubmitFailureUtcUnixMs = flashbackPlayback.LastSubmitFailureUtcUnixMs,
                FlashbackPlaybackLastSubmitFailure = flashbackPlayback.LastSubmitFailure,
                FlashbackPlaybackLastSegmentSwitchUtcUnixMs = flashbackPlayback.LastSegmentSwitchUtcUnixMs,
                FlashbackPlaybackLastFmp4ReopenUtcUnixMs = flashbackPlayback.LastFmp4ReopenUtcUnixMs,
                FlashbackPlaybackLastWriteHeadWaitGapMs = flashbackPlayback.LastWriteHeadWaitGapMs,
                FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,
                FlashbackPlaybackObservedFps = flashbackPlayback.ObservedFps,
                FlashbackPlaybackAvgFrameMs = flashbackPlayback.AvgFrameMs,
                FlashbackPlaybackCadenceSampleCount = flashbackPlayback.CadenceSampleCount,
                FlashbackPlaybackP95FrameMs = flashbackPlayback.P95FrameMs,
                FlashbackPlaybackP99FrameMs = flashbackPlayback.P99FrameMs,
                FlashbackPlaybackMaxFrameMs = flashbackPlayback.MaxFrameMs,
                FlashbackPlaybackSlowFrames = flashbackPlayback.SlowFrames,
                FlashbackPlaybackSlowFramePercent = flashbackPlayback.SlowFramePercent,
                FlashbackPlaybackOnePercentLowFps = flashbackPlayback.OnePercentLowFps,
                FlashbackPlaybackFivePercentLowFps = flashbackPlayback.FivePercentLowFps,
                FlashbackPlaybackSampleDurationMs = flashbackPlayback.SampleDurationMs,
                FlashbackPlaybackRecentFrameIntervalsMs = flashbackPlayback.RecentFrameIntervalsMs,
                FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlayback.PtsCadenceMismatchCount,
                FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = flashbackPlayback.LastPtsCadenceMismatchUtcUnixMs,
                FlashbackPlaybackLastPtsCadenceDeltaMs = flashbackPlayback.LastPtsCadenceDeltaMs,
                FlashbackPlaybackLastPtsCadenceExpectedMs = flashbackPlayback.LastPtsCadenceExpectedMs,
                FlashbackPlaybackSeekForwardDecodeCapHits = flashbackPlayback.SeekForwardDecodeCapHits,
                FlashbackPlaybackLastSeekHitForwardDecodeCap = flashbackPlayback.LastSeekHitForwardDecodeCap,
                FlashbackPlaybackDecodeSampleCount = flashbackPlayback.DecodeSampleCount,
                FlashbackPlaybackDecodeAvgMs = flashbackPlayback.DecodeAvgMs,
                FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,
                FlashbackPlaybackDecodeP99Ms = flashbackPlayback.DecodeP99Ms,
                FlashbackPlaybackDecodeMaxMs = flashbackPlayback.DecodeMaxMs,
                FlashbackPlaybackMaxDecodePhase = flashbackPlayback.MaxDecodePhase,
                FlashbackPlaybackMaxDecodeReceiveMs = flashbackPlayback.MaxDecodeReceiveMs,
                FlashbackPlaybackMaxDecodeFeedMs = flashbackPlayback.MaxDecodeFeedMs,
                FlashbackPlaybackMaxDecodeReadMs = flashbackPlayback.MaxDecodeReadMs,
                FlashbackPlaybackMaxDecodeSendMs = flashbackPlayback.MaxDecodeSendMs,
                FlashbackPlaybackMaxDecodeAudioMs = flashbackPlayback.MaxDecodeAudioMs,
                FlashbackPlaybackMaxDecodeConvertMs = flashbackPlayback.MaxDecodeConvertMs,
                FlashbackPlaybackMaxDecodeUtcUnixMs = flashbackPlayback.MaxDecodeUtcUnixMs,
                FlashbackPlaybackMaxDecodePositionMs = flashbackPlayback.MaxDecodePositionMs,
                FlashbackAvDriftMs = flashbackPlayback.AvDriftMs,
                FlashbackPlaybackThreadAlive = flashbackPlayback.ThreadAlive,
                FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued,
                FlashbackPlaybackCommandsProcessed = flashbackPlayback.CommandsProcessed,
                FlashbackPlaybackCommandsDropped = flashbackPlayback.CommandsDropped,
                FlashbackPlaybackCommandsSkippedNotReady = flashbackPlayback.CommandsSkippedNotReady,
                FlashbackPlaybackScrubUpdatesCoalesced = flashbackPlayback.ScrubUpdatesCoalesced,
                FlashbackPlaybackSeekCommandsCoalesced = flashbackPlayback.SeekCommandsCoalesced,
                FlashbackPlaybackCommandQueueCapacity = flashbackPlayback.CommandQueueCapacity,
                FlashbackPlaybackPendingCommands = flashbackPlayback.PendingCommands,
                FlashbackPlaybackMaxPendingCommands = flashbackPlayback.MaxPendingCommands,
                FlashbackPlaybackLastCommandQueueLatencyMs = flashbackPlayback.LastCommandQueueLatencyMs,
                FlashbackPlaybackMaxCommandQueueLatencyMs = flashbackPlayback.MaxCommandQueueLatencyMs,
                FlashbackPlaybackMaxCommandQueueLatencyCommand = flashbackPlayback.MaxCommandQueueLatencyCommand,
                FlashbackPlaybackLastCommandQueued = flashbackPlayback.LastCommandQueued,
                FlashbackPlaybackLastCommandProcessed = flashbackPlayback.LastCommandProcessed,
                FlashbackPlaybackLastCommandQueuedUtcUnixMs = flashbackPlayback.LastCommandQueuedUtcUnixMs,
                FlashbackPlaybackLastCommandProcessedUtcUnixMs = flashbackPlayback.LastCommandProcessedUtcUnixMs,
                FlashbackPlaybackLastCommandFailureUtcUnixMs = flashbackPlayback.LastCommandFailureUtcUnixMs,
                FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,
                FlashbackExportActive = flashbackExport.Active,
                FlashbackExportId = flashbackExport.Id,
                FlashbackExportStatus = flashbackExport.Status,
                FlashbackExportOutputPath = flashbackExport.OutputPath,
                FlashbackExportStartedUtcUnixMs = flashbackExport.StartedUtcUnixMs,
                FlashbackExportLastProgressUtcUnixMs = flashbackExport.LastProgressUtcUnixMs,
                FlashbackExportCompletedUtcUnixMs = flashbackExport.CompletedUtcUnixMs,
                FlashbackExportElapsedMs = flashbackExport.ElapsedMs,
                FlashbackExportLastProgressAgeMs = flashbackExport.LastProgressAgeMs,
                FlashbackExportOutputBytes = flashbackExport.OutputBytes,
                FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,
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
                FlashbackExportVerificationFormat = fields.FlashbackExportVerificationFormat,
                FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,
                LastExportId = flashbackExport.LastResultId,
                LastExportPath = flashbackExport.LastResult?.OutputPath,
                LastExportSuccess = flashbackExport.LastResult?.Succeeded,
                LastExportMessage = flashbackExport.LastResult?.StatusMessage,
                RecordingElapsedMs = fields.RecordingElapsedMs,
                ExpectedFrameRate = fields.ExpectedFrameRate,
                NegotiatedWidth = fields.NegotiatedWidth,
                NegotiatedHeight = fields.NegotiatedHeight,
                NegotiatedFrameRate = fields.NegotiatedFrameRate,
                NegotiatedFrameRateArg = fields.NegotiatedFrameRateArg,
                NegotiatedFrameRateNumerator = fields.NegotiatedFrameRateNumerator,
                NegotiatedFrameRateDenominator = fields.NegotiatedFrameRateDenominator,
                NegotiatedPixelFormat = fields.NegotiatedPixelFormat,
                RequestedReaderSubtype = fields.RequestedReaderSubtype,
                ReaderSourceStreamType = fields.ReaderSourceStreamType,
                ReaderSourceSubtype = fields.ReaderSourceSubtype,
                FirstObservedFramePixelFormat = observedTelemetry.FirstObservedFramePixelFormat,
                LatestObservedFramePixelFormat = observedTelemetry.LatestObservedFramePixelFormat,
                ObservedP010FrameCount = observedTelemetry.ObservedP010FrameCount,
                ObservedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount,
                ObservedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount,
                SourceTelemetryAvailability = sourceTelemetry.Availability,
                SourceTelemetryOrigin = sourceTelemetry.Origin,
                SourceTelemetryConfidence = sourceTelemetry.Confidence,
                SourceTelemetryOriginDetail = sourceTelemetry.OriginDetail,
                SourceTelemetryDiagnosticSummary = sourceTelemetry.DiagnosticSummary,
                SourceTelemetryTimestampUtc = sourceTelemetry.TimestampUtc,
                SourceWidth = sourceTelemetry.Width,
                SourceHeight = sourceTelemetry.Height,
                SourceFrameRateExact = sourceTelemetry.FrameRateExact,
                SourceFrameRateArg = sourceTelemetry.FrameRateArg,
                SourceIsHdr = sourceTelemetry.IsHdr,
                SourceVideoFormat = sourceTelemetry.VideoFormat,
                SourceColorimetry = sourceTelemetry.Colorimetry,
                SourceQuantization = sourceTelemetry.Quantization,
                SourceHdrTransferFunction = sourceTelemetry.HdrTransferFunction,
                SourceHdrTransferCode = sourceTelemetry.HdrTransferCode,
                SourceFirmware = sourceTelemetry.Firmware,
                SourceAudioFormat = sourceTelemetry.AudioFormat,
                SourceAudioSampleRate = sourceTelemetry.AudioSampleRate,
                SourceInputSource = sourceTelemetry.InputSource,
                SourceUsbHostProtocol = sourceTelemetry.UsbHostProtocol,
                SourceHdcpMode = sourceTelemetry.HdcpMode,
                SourceHdcpVersion = sourceTelemetry.HdcpVersion,
                SourceRxTxHdcpVersion = sourceTelemetry.RxTxHdcpVersion,
                SourceRawTimingHex = sourceTelemetry.RawTimingHex,
                SourceTelemetryDetails = sourceTelemetry.Details,
                SourceTelemetryBackend = sourceTelemetry.Backend,
                SourceTelemetrySuppressedReason = sourceTelemetry.SuppressedReason,
                SourceTelemetrySuppressed = sourceTelemetry.Suppressed,
                SourceTelemetryCircuitState = sourceTelemetry.CircuitState,
                LastFrameArrivalMs = fields.LastFrameArrivalMs,
                VideoFramesArrived = fields.VideoFramesArrived,
                VideoFramesQueued = recordingHealth.VideoQueueDepth,
                VideoFramesDropped = recordingHealth.DroppedFrames,
                VideoFramesDroppedBacklog = recordingHealth.VideoDropsBacklogEviction,
                VideoFramesConverted = recordingHealth.EncodedVideoFrames,
                VideoDropsQueueSaturated = recordingHealth.VideoDropsQueueSaturated,
                VideoDropsBacklogEviction = recordingHealth.VideoDropsBacklogEviction,
                RecordingEncodingFailed = recordingHealth.EncodingFailed,
                RecordingEncodingFailureType = recordingHealth.FailureType,
                RecordingEncodingFailureMessage = recordingHealth.FailureMessage,
                RecordingVideoQueueCapacity = recordingHealth.VideoQueueCapacity,
                RecordingVideoQueueMaxDepth = recordingHealth.VideoQueueMaxDepth,
                RecordingVideoFramesSubmittedToEncoder = recordingHealth.VideoFramesSubmitted,
                RecordingVideoEncoderPts = recordingHealth.VideoEncoderPts,
                RecordingVideoEncoderPacketsWritten = recordingHealth.VideoEncoderPacketsWritten,
                RecordingVideoEncoderDroppedFrames = recordingHealth.VideoEncoderDroppedFrames,
                RecordingVideoSequenceGaps = recordingHealth.VideoSequenceGaps,
                RecordingVideoQueueOldestFrameAgeMs = recordingHealth.VideoQueueOldestFrameAgeMs,
                RecordingVideoQueueLastLatencyMs = recordingHealth.VideoQueueLastLatencyMs,
                RecordingVideoQueueLatencySampleCount = recordingHealth.VideoQueueLatencyMetrics.SampleCount,
                RecordingVideoQueueLatencyAvgMs = recordingHealth.VideoQueueLatencyMetrics.AverageMs,
                RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms,
                RecordingVideoQueueLatencyP99Ms = recordingHealth.VideoQueueLatencyMetrics.P99Ms,
                RecordingVideoQueueLatencyMaxMs = recordingHealth.VideoQueueLatencyMetrics.MaxMs,
                RecordingVideoBackpressureWaitMs = recordingHealth.VideoBackpressureWaitMs,
                RecordingVideoBackpressureEvents = recordingHealth.VideoBackpressureEvents,
                RecordingVideoBackpressureLastWaitMs = recordingHealth.VideoBackpressureLastWaitMs,
                RecordingVideoBackpressureMaxWaitMs = recordingHealth.VideoBackpressureMaxWaitMs,
                RecordingGpuQueueDepth = recordingHealth.GpuQueueDepth,
                RecordingGpuQueueCapacity = recordingHealth.GpuQueueCapacity,
                RecordingGpuQueueMaxDepth = recordingHealth.GpuQueueMaxDepth,
                RecordingGpuFramesEnqueued = recordingHealth.GpuFramesEnqueued,
                RecordingGpuFramesDropped = recordingHealth.GpuFramesDropped,
                RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth,
                RecordingCudaQueueCapacity = recordingHealth.CudaQueueCapacity,
                RecordingCudaQueueMaxDepth = recordingHealth.CudaQueueMaxDepth,
                RecordingCudaFramesEnqueued = recordingHealth.CudaFramesEnqueued,
                RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped,
                FlashbackEncodingFailed = recordingHealth.FlashbackEncodingFailed,
                FlashbackEncodingFailureType = recordingHealth.FlashbackFailureType,
                FlashbackEncodingFailureMessage = recordingHealth.FlashbackFailureMessage,
                FatalCleanupInProgress = fatalCleanupInProgress,
                FlashbackCleanupInProgress = flashbackCleanupInProgress,
                FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,
                FlashbackForceRotateRequested = flashbackQueues.ForceRotateRequested,
                FlashbackForceRotateDraining = flashbackQueues.ForceRotateDraining,
                FlashbackVideoQueueCapacity = flashbackQueues.VideoQueueCapacity,
                FlashbackVideoQueueMaxDepth = flashbackQueues.VideoQueueMaxDepth,
                FlashbackVideoFramesSubmittedToEncoder = flashbackQueues.VideoFramesSubmittedToEncoder,
                FlashbackVideoEncoderPts = flashbackQueues.VideoEncoderPts,
                FlashbackVideoEncoderPacketsWritten = flashbackQueues.VideoEncoderPacketsWritten,
                FlashbackVideoEncoderDroppedFrames = flashbackQueues.VideoEncoderDroppedFrames,
                FlashbackVideoSequenceGaps = flashbackQueues.VideoSequenceGaps,
                FlashbackVideoQueueRejectedFrames = flashbackQueues.VideoQueueRejectedFrames,
                FlashbackVideoQueueLastRejectReason = flashbackQueues.VideoQueueLastRejectReason,
                FlashbackVideoQueueOldestFrameAgeMs = flashbackQueues.VideoQueueOldestFrameAgeMs,
                FlashbackVideoQueueLastLatencyMs = flashbackQueues.VideoQueueLastLatencyMs,
                FlashbackVideoQueueLatencySampleCount = flashbackQueues.VideoQueueLatencyMetrics.SampleCount,
                FlashbackVideoQueueLatencyAvgMs = flashbackQueues.VideoQueueLatencyMetrics.AverageMs,
                FlashbackVideoQueueLatencyP95Ms = flashbackQueues.VideoQueueLatencyMetrics.P95Ms,
                FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,
                FlashbackVideoQueueLatencyMaxMs = flashbackQueues.VideoQueueLatencyMetrics.MaxMs,
                FlashbackVideoBackpressureWaitMs = flashbackQueues.VideoBackpressureWaitMs,
                FlashbackVideoBackpressureEvents = flashbackQueues.VideoBackpressureEvents,
                FlashbackVideoBackpressureLastWaitMs = flashbackQueues.VideoBackpressureLastWaitMs,
                FlashbackVideoBackpressureMaxWaitMs = flashbackQueues.VideoBackpressureMaxWaitMs,
                FlashbackGpuQueueDepth = flashbackQueues.GpuQueueDepth,
                FlashbackGpuQueueCapacity = flashbackQueues.GpuQueueCapacity,
                FlashbackGpuQueueMaxDepth = flashbackQueues.GpuQueueMaxDepth,
                FlashbackGpuFramesEnqueued = flashbackQueues.GpuFramesEnqueued,
                FlashbackGpuFramesDropped = flashbackQueues.GpuFramesDropped,
                FlashbackGpuQueueRejectedFrames = flashbackQueues.GpuQueueRejectedFrames,
                FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,
                AudioDropsQueueSaturated = recordingHealth.AudioDropsQueueSaturated,
                AudioDropsBacklogEviction = recordingHealth.AudioDropsBacklogEviction,
                AudioChunksDropped = recordingHealth.AudioDropsQueueSaturated + recordingHealth.AudioDropsBacklogEviction,
                ConversionQueueDepth = 0,
                FfmpegVideoQueueDepth = recordingHealth.VideoQueueDepth,
                FfmpegAudioQueueDepth = recordingHealth.AudioQueueDepth,
                VideoFramesEnqueued = recordingHealth.VideoFramesEnqueued,
                LastVideoEnqueueAgeMs = fields.LastVideoEnqueueAgeMs,
                LastVideoWriteAgeMs = fields.LastVideoWriteAgeMs,
                CaptureCadenceSampleCount = captureCadence.SampleCount,
                CaptureCadenceObservedFps = captureCadence.ObservedFps,
                CaptureCadenceExpectedIntervalMs = captureCadence.ExpectedIntervalMs,
                CaptureCadenceAverageIntervalMs = captureCadence.AverageIntervalMs,
                CaptureCadenceP95IntervalMs = captureCadence.P95IntervalMs,
                CaptureCadenceP99IntervalMs = captureCadence.P99IntervalMs,
                CaptureCadenceMaxIntervalMs = captureCadence.MaxIntervalMs,
                CaptureCadenceOnePercentLowFps = captureCadence.OnePercentLowFps,
                CaptureCadenceFivePercentLowFps = captureCadence.FivePercentLowFps,
                CaptureCadenceSampleDurationMs = captureCadence.SampleDurationMs,
                CaptureCadenceRecentIntervalsMs = captureCadence.RecentIntervalsMs,
                CaptureCadenceJitterStdDevMs = captureCadence.JitterStdDevMs,
                CaptureCadenceSevereGapCount = captureCadence.SevereGapCount,
                CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,
                CaptureCadenceEstimatedDropPercent = captureCadence.EstimatedDropPercent,
                MjpegDecodeSampleCount = mjpegHealth.Timing.DecodeSampleCount,
                MjpegDecodeAvgMs = mjpegHealth.Timing.DecodeAvgMs,
                MjpegDecodeP95Ms = mjpegHealth.Timing.DecodeP95Ms,
                MjpegDecodeMaxMs = mjpegHealth.Timing.DecodeMaxMs,
                MjpegInteropCopySampleCount = mjpegHealth.Timing.InteropCopySampleCount,
                MjpegInteropCopyAvgMs = mjpegHealth.Timing.InteropCopyAvgMs,
                MjpegInteropCopyP95Ms = mjpegHealth.Timing.InteropCopyP95Ms,
                MjpegInteropCopyMaxMs = mjpegHealth.Timing.InteropCopyMaxMs,
                MjpegCallbackSampleCount = mjpegHealth.Timing.CallbackSampleCount,
                MjpegCallbackAvgMs = mjpegHealth.Timing.CallbackAvgMs,
                MjpegCallbackP95Ms = mjpegHealth.Timing.CallbackP95Ms,
                MjpegCallbackMaxMs = mjpegHealth.Timing.CallbackMaxMs,
                MjpegDecoderCount = mjpegHealth.FullTiming?.DecoderCount ?? 0,
                MjpegReorderSampleCount = mjpegHealth.FullTiming?.ReorderSampleCount ?? 0,
                MjpegReorderAvgMs = mjpegHealth.FullTiming?.ReorderAvgMs ?? 0,
                MjpegReorderP95Ms = mjpegHealth.FullTiming?.ReorderP95Ms ?? 0,
                MjpegReorderMaxMs = mjpegHealth.FullTiming?.ReorderMaxMs ?? 0,
                MjpegPipelineSampleCount = mjpegHealth.FullTiming?.PipelineSampleCount ?? 0,
                MjpegPipelineAvgMs = mjpegHealth.FullTiming?.PipelineAvgMs ?? 0,
                MjpegPipelineP95Ms = mjpegHealth.FullTiming?.PipelineP95Ms ?? 0,
                MjpegPipelineMaxMs = mjpegHealth.FullTiming?.PipelineMaxMs ?? 0,
                MjpegTotalDecoded = mjpegHealth.FullTiming?.TotalDecoded ?? 0,
                MjpegTotalEmitted = mjpegHealth.FullTiming?.TotalEmitted ?? 0,
                MjpegTotalDropped = mjpegHealth.FullTiming?.TotalDropped ?? 0,
                MjpegCompressedFramesQueued = mjpegHealth.FullTiming?.CompressedFramesQueued ?? 0,
                MjpegCompressedFramesDequeued = mjpegHealth.FullTiming?.CompressedFramesDequeued ?? 0,
                MjpegCompressedDropsQueueFull = mjpegHealth.FullTiming?.CompressedDropsQueueFull ?? 0,
                MjpegCompressedDropsByteBudget = mjpegHealth.FullTiming?.CompressedDropsByteBudget ?? 0,
                MjpegCompressedDropsDisposed = mjpegHealth.FullTiming?.CompressedDropsDisposed ?? 0,
                MjpegDecodeFailures = mjpegHealth.FullTiming?.DecodeFailures ?? 0,
                MjpegReorderCollisions = mjpegHealth.FullTiming?.ReorderCollisions ?? 0,
                MjpegEmitFailures = mjpegHealth.FullTiming?.EmitFailures ?? 0,
                MjpegCompressedQueueDepth = mjpegHealth.FullTiming?.CompressedQueueDepth ?? 0,
                MjpegCompressedQueueBytes = mjpegHealth.FullTiming?.CompressedQueueBytes ?? 0,
                MjpegCompressedQueueByteBudget = mjpegHealth.FullTiming?.CompressedQueueByteBudget ?? 0,
                MjpegReorderSkips = mjpegHealth.FullTiming?.ReorderSkips ?? 0,
                MjpegReorderBufferDepth = mjpegHealth.FullTiming?.ReorderBufferDepth ?? 0,
                MjpegPreviewJitterEnabled = mjpegHealth.PreviewJitter.Enabled,
                MjpegPreviewJitterTargetDepth = mjpegHealth.PreviewJitter.TargetDepth,
                MjpegPreviewJitterMaxDepth = mjpegHealth.PreviewJitter.MaxDepth,
                MjpegPreviewJitterQueueDepth = mjpegHealth.PreviewJitter.QueueDepth,
                MjpegPreviewJitterTotalQueued = mjpegHealth.PreviewJitter.TotalQueued,
                MjpegPreviewJitterTotalSubmitted = mjpegHealth.PreviewJitter.TotalSubmitted,
                MjpegPreviewJitterTotalDropped = mjpegHealth.PreviewJitter.TotalDropped,
                MjpegPreviewJitterUnderflowCount = mjpegHealth.PreviewJitter.UnderflowCount,
                MjpegPreviewJitterResumeReprimeCount = mjpegHealth.PreviewJitter.ResumeReprimeCount,
                MjpegPreviewJitterInputSampleCount = mjpegHealth.PreviewJitter.InputIntervalSampleCount,
                MjpegPreviewJitterInputAvgMs = mjpegHealth.PreviewJitter.InputIntervalAvgMs,
                MjpegPreviewJitterInputP95Ms = mjpegHealth.PreviewJitter.InputIntervalP95Ms,
                MjpegPreviewJitterInputMaxMs = mjpegHealth.PreviewJitter.InputIntervalMaxMs,
                MjpegPreviewJitterOutputSampleCount = mjpegHealth.PreviewJitter.OutputIntervalSampleCount,
                MjpegPreviewJitterOutputAvgMs = mjpegHealth.PreviewJitter.OutputIntervalAvgMs,
                MjpegPreviewJitterOutputP95Ms = mjpegHealth.PreviewJitter.OutputIntervalP95Ms,
                MjpegPreviewJitterOutputMaxMs = mjpegHealth.PreviewJitter.OutputIntervalMaxMs,
                MjpegPreviewJitterLatencySampleCount = mjpegHealth.PreviewJitter.QueueLatencySampleCount,
                MjpegPreviewJitterLatencyAvgMs = mjpegHealth.PreviewJitter.QueueLatencyAvgMs,
                MjpegPreviewJitterLatencyP95Ms = mjpegHealth.PreviewJitter.QueueLatencyP95Ms,
                MjpegPreviewJitterLatencyMaxMs = mjpegHealth.PreviewJitter.QueueLatencyMaxMs,
                MjpegPreviewJitterDeadlineDropCount = mjpegHealth.PreviewJitter.DeadlineDropCount,
                MjpegPreviewJitterClearedDropCount = mjpegHealth.PreviewJitter.ClearedDropCount,
                MjpegPreviewJitterTargetIncreaseCount = mjpegHealth.PreviewJitter.TargetIncreaseCount,
                MjpegPreviewJitterTargetDecreaseCount = mjpegHealth.PreviewJitter.TargetDecreaseCount,
                MjpegPreviewJitterLastSelectedPreviewPresentId = mjpegHealth.PreviewJitter.LastSelectedPreviewPresentId,
                MjpegPreviewJitterLastSelectedSourceSequenceNumber = mjpegHealth.PreviewJitter.LastSelectedSourceSequenceNumber,
                MjpegPreviewJitterLastSelectedQpc = mjpegHealth.PreviewJitter.LastSelectedQpc,
                MjpegPreviewJitterLastSelectedSourceLatencyMs = mjpegHealth.PreviewJitter.LastSelectedSourceLatencyMs,
                MjpegPreviewJitterLastDroppedSourceSequenceNumber = mjpegHealth.PreviewJitter.LastDroppedSourceSequenceNumber,
                MjpegPreviewJitterLastDropQpc = mjpegHealth.PreviewJitter.LastDropQpc,
                MjpegPreviewJitterLastDropReason = mjpegHealth.PreviewJitter.LastDropReason ?? string.Empty,
                MjpegPreviewJitterLastUnderflowQpc = mjpegHealth.PreviewJitter.LastUnderflowQpc,
                MjpegPreviewJitterLastUnderflowReason = mjpegHealth.PreviewJitter.LastUnderflowReason ?? string.Empty,
                MjpegPreviewJitterLastUnderflowQueueDepth = mjpegHealth.PreviewJitter.LastUnderflowQueueDepth,
                MjpegPreviewJitterLastUnderflowInputAgeMs = mjpegHealth.PreviewJitter.LastUnderflowInputAgeMs,
                MjpegPreviewJitterLastUnderflowOutputAgeMs = mjpegHealth.PreviewJitter.LastUnderflowOutputAgeMs,
                MjpegPreviewJitterLastScheduleLateMs = mjpegHealth.PreviewJitter.LastScheduleLateMs,
                MjpegPreviewJitterMaxScheduleLateMs = mjpegHealth.PreviewJitter.MaxScheduleLateMs,
                MjpegPreviewJitterScheduleLateCount = mjpegHealth.PreviewJitter.ScheduleLateCount,
                MjpegPacketHashSampleCount = mjpegHealth.PacketHash.SampleCount,
                MjpegPacketHashUniqueFrameCount = mjpegHealth.PacketHash.UniqueFrameCount,
                MjpegPacketHashDuplicateFrameCount = mjpegHealth.PacketHash.DuplicateFrameCount,
                MjpegPacketHashLongestDuplicateRun = mjpegHealth.PacketHash.LongestDuplicateRun,
                MjpegPacketHashInputObservedFps = mjpegHealth.PacketHash.InputObservedFps,
                MjpegPacketHashUniqueObservedFps = mjpegHealth.PacketHash.UniqueObservedFps,
                MjpegPacketHashDuplicateFramePercent = mjpegHealth.PacketHash.DuplicateFramePercent,
                MjpegPacketHashLastHash = mjpegHealth.PacketHash.LastHash,
                MjpegPacketHashLastFrameDuplicate = mjpegHealth.PacketHash.LastFrameDuplicate,
                MjpegPacketHashPattern = mjpegHealth.PacketHash.Pattern,
                MjpegPacketHashRecentInputIntervalsMs = mjpegHealth.PacketHash.RecentInputIntervalsMs,
                MjpegPacketHashRecentUniqueIntervalsMs = mjpegHealth.PacketHash.RecentUniqueIntervalsMs,
                MjpegPacketHashRecentDuplicateFlags = mjpegHealth.PacketHash.RecentDuplicateFlags,
                VisualCadenceSampleCount = mjpegHealth.VisualCadence.SampleCount,
                VisualCadenceChangedFrameCount = mjpegHealth.VisualCadence.ChangedFrameCount,
                VisualCadenceRepeatFrameCount = mjpegHealth.VisualCadence.RepeatFrameCount,
                VisualCadenceLongestRepeatRun = mjpegHealth.VisualCadence.LongestRepeatRun,
                VisualCadenceOutputObservedFps = mjpegHealth.VisualCadence.OutputObservedFps,
                VisualCadenceChangeObservedFps = mjpegHealth.VisualCadence.ChangeObservedFps,
                VisualCadenceRepeatFramePercent = mjpegHealth.VisualCadence.RepeatFramePercent,
                VisualCadenceLastDelta = mjpegHealth.VisualCadence.LastDelta,
                VisualCadenceAverageDelta = mjpegHealth.VisualCadence.AverageDelta,
                VisualCadenceP95Delta = mjpegHealth.VisualCadence.P95Delta,
                VisualCadenceMotionScore = mjpegHealth.VisualCadence.MotionScore,
                VisualCadenceMotionConfidence = mjpegHealth.VisualCadence.MotionConfidence,
                VisualCadenceRecentOutputIntervalsMs = mjpegHealth.VisualCadence.RecentOutputIntervalsMs,
                VisualCadenceRecentChangeIntervalsMs = mjpegHealth.VisualCadence.RecentChangeIntervalsMs,
                VisualCenterCadenceSampleCount = mjpegHealth.VisualCenterCadence.SampleCount,
                VisualCenterCadenceChangedFrameCount = mjpegHealth.VisualCenterCadence.ChangedFrameCount,
                VisualCenterCadenceRepeatFrameCount = mjpegHealth.VisualCenterCadence.RepeatFrameCount,
                VisualCenterCadenceLongestRepeatRun = mjpegHealth.VisualCenterCadence.LongestRepeatRun,
                VisualCenterCadenceOutputObservedFps = mjpegHealth.VisualCenterCadence.OutputObservedFps,
                VisualCenterCadenceChangeObservedFps = mjpegHealth.VisualCenterCadence.ChangeObservedFps,
                VisualCenterCadenceRepeatFramePercent = mjpegHealth.VisualCenterCadence.RepeatFramePercent,
                VisualCenterCadenceLastDelta = mjpegHealth.VisualCenterCadence.LastDelta,
                VisualCenterCadenceAverageDelta = mjpegHealth.VisualCenterCadence.AverageDelta,
                VisualCenterCadenceP95Delta = mjpegHealth.VisualCenterCadence.P95Delta,
                VisualCenterCadenceMotionScore = mjpegHealth.VisualCenterCadence.MotionScore,
                VisualCenterCadenceMotionConfidence = mjpegHealth.VisualCenterCadence.MotionConfidence,
                VisualCenterCadenceRecentOutputIntervalsMs = mjpegHealth.VisualCenterCadence.RecentOutputIntervalsMs,
                VisualCenterCadenceRecentChangeIntervalsMs = mjpegHealth.VisualCenterCadence.RecentChangeIntervalsMs,
                MjpegPerDecoder = mjpegHealth.PerDecoder,
                AvSyncCaptureDriftMs = avSyncHealth.CaptureDriftMs,
                AvSyncCaptureDriftRateMsPerSec = avSyncHealth.CaptureDriftRateMsPerSec,
                AvSyncEncoderDriftMs = avSyncHealth.EncoderDriftMs,
                AvSyncEncoderCorrectionSamples = avSyncHealth.EncoderCorrectionSamples
            };
        }
    }

}
