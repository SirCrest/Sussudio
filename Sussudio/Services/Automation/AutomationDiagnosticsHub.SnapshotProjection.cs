using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private AutomationSnapshot BuildAutomationSnapshot(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health,
        RecordingStats recordingStats,
        PreviewRuntimeSnapshot previewRuntime,
        SnapshotCollectionStamp snapshotCollection,
        PerformanceEvaluation performance,
        DiagnosticEvaluation diagnostic,
        PreviewPacingClassification previewPacingClassification,
        PreviewHdrState previewHdrState,
        AudioSignalState audioSignal,
        bool recordingFileGrowing,
        HdrTruthVerdict hdrTruthVerdict,
        LastOutputProbe lastOutput,
        ProcessResourceSnapshot processResources,
        RecordingVerificationResult? lastVerification,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime, snapshotCollection);
        var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);
        var audioDrops = BuildAudioDropsProjection(health);
        var userSettings = BuildUserSettingsProjection(viewModelSnapshot);
        var negotiatedCaptureFormat = BuildCaptureFormatNegotiatedProjection(captureRuntime);
        var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);
        var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);
        var previewFrame = BuildPreviewRuntimeFrameProjection(previewRuntime);
        var previewStartup = BuildPreviewRuntimeStartupProjection(previewRuntime);
        var recordingBackend = BuildRecordingBackendProjection(captureRuntime);
        var mjpegTiming = BuildMjpegTimingProjection(health);
        var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);

        return new AutomationSnapshot
        {
            TimestampUtc = snapshotStatus.TimestampUtc,
            SnapshotCollectionEpoch = snapshotStatus.SnapshotCollectionEpoch,
            SnapshotCollectionStartedUtc = snapshotStatus.SnapshotCollectionStartedUtc,
            SnapshotCollectionCompletedUtc = snapshotStatus.SnapshotCollectionCompletedUtc,
            SnapshotCollectionDurationMs = snapshotStatus.SnapshotCollectionDurationMs,
            SnapshotMixedEpochs = snapshotStatus.SnapshotMixedEpochs,
            SnapshotMixedEpochReason = snapshotStatus.SnapshotMixedEpochReason,
            SnapshotViewModelEpoch = snapshotStatus.SnapshotViewModelEpoch,
            SnapshotCaptureRuntimeEpoch = snapshotStatus.SnapshotCaptureRuntimeEpoch,
            SnapshotCaptureHealthEpoch = snapshotStatus.SnapshotCaptureHealthEpoch,
            SnapshotRecordingStatsEpoch = snapshotStatus.SnapshotRecordingStatsEpoch,
            SnapshotPreviewRuntimeEpoch = snapshotStatus.SnapshotPreviewRuntimeEpoch,
            SnapshotOutputEpoch = snapshotStatus.SnapshotOutputEpoch,
            SnapshotSourceTelemetryEpoch = snapshotStatus.SnapshotSourceTelemetryEpoch,
            IsInitialized = snapshotStatus.IsInitialized,
            IsPreviewing = snapshotStatus.IsPreviewing,
            IsRecording = snapshotStatus.IsRecording,
            VerificationInProgress = snapshotStatus.VerificationInProgress,
            IsAudioEnabled = snapshotStatus.IsAudioEnabled,
            IsAudioPreviewEnabled = snapshotStatus.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = snapshotStatus.IsCustomAudioInputEnabled,
            SessionState = snapshotStatus.SessionState,
            StatusText = snapshotStatus.StatusText,
            PerformanceScore = snapshotEvaluation.PerformanceScore,
            PerformancePerfectionMet = snapshotEvaluation.PerformancePerfectionMet,
            PerformanceSummary = snapshotEvaluation.PerformanceSummary,
            DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,
            DiagnosticLikelyStage = snapshotEvaluation.DiagnosticLikelyStage,
            DiagnosticSummary = snapshotEvaluation.DiagnosticSummary,
            DiagnosticEvidence = snapshotEvaluation.DiagnosticEvidence,
            DiagnosticSourceLane = snapshotEvaluation.DiagnosticSourceLane,
            DiagnosticDecodeLane = snapshotEvaluation.DiagnosticDecodeLane,
            DiagnosticPreviewLane = snapshotEvaluation.DiagnosticPreviewLane,
            DiagnosticRenderLane = snapshotEvaluation.DiagnosticRenderLane,
            DiagnosticPresentLane = snapshotEvaluation.DiagnosticPresentLane,
            DiagnosticRecordingLane = snapshotEvaluation.DiagnosticRecordingLane,
            DiagnosticAudioLane = snapshotEvaluation.DiagnosticAudioLane,
            PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,
            PreviewPacingSlowStageConfidence = snapshotEvaluation.PreviewPacingSlowStageConfidence,
            PreviewPacingSlowStageEvidence = snapshotEvaluation.PreviewPacingSlowStageEvidence,
            CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,
            CaptureCommandCommandsCompleted = viewModelSnapshot.CaptureCommandCommandsCompleted,
            CaptureCommandCommandsFailed = viewModelSnapshot.CaptureCommandCommandsFailed,
            CaptureCommandCommandsCanceled = viewModelSnapshot.CaptureCommandCommandsCanceled,
            CaptureCommandCommandsCoalesced = viewModelSnapshot.CaptureCommandCommandsCoalesced,
            CaptureCommandPendingCommands = viewModelSnapshot.CaptureCommandPendingCommands,
            CaptureCommandMaxPendingCommands = viewModelSnapshot.CaptureCommandMaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = viewModelSnapshot.CaptureCommandOldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = viewModelSnapshot.CaptureCommandLastQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,
            CaptureCommandLastCommand = viewModelSnapshot.CaptureCommandLastCommand,
            CaptureCommandLastOutcome = viewModelSnapshot.CaptureCommandLastOutcome,
            CaptureCommandLastCorrelationId = viewModelSnapshot.CaptureCommandLastCorrelationId,
            CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,
            PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,
            PerformanceThresholdCaptureP95Multiplier = snapshotEvaluation.PerformanceThresholdCaptureP95Multiplier,
            PerformanceThresholdPreviewSlowPercent = snapshotEvaluation.PerformanceThresholdPreviewSlowPercent,
            PerformanceThresholdVerificationDropPercent = snapshotEvaluation.PerformanceThresholdVerificationDropPercent,
            SelectedDeviceId = userSettings.SelectedDeviceId,
            SelectedDeviceName = userSettings.SelectedDeviceName,
            SelectedAudioInputDeviceId = userSettings.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = userSettings.SelectedAudioInputDeviceName,
            SelectedResolution = userSettings.SelectedResolution,
            SelectedFrameRate = userSettings.SelectedFrameRate,
            SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,
            SelectedExactFrameRate = userSettings.SelectedExactFrameRate,
            SelectedExactFrameRateArg = userSettings.SelectedExactFrameRateArg,
            DisabledResolutionReason = userSettings.DisabledResolutionReason,
            DisabledFrameRateReason = userSettings.DisabledFrameRateReason,
            DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,
            DetectedSourceFrameRateArg = sourceSignal.DetectedFrameRateArg,
            SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,
            SourceWidth = sourceSignal.Width,
            SourceHeight = sourceSignal.Height,
            SourceIsHdr = sourceSignal.IsHdr,
            SourceVideoFormat = sourceSignal.VideoFormat,
            SourceColorimetry = sourceSignal.Colorimetry,
            SourceQuantization = sourceSignal.Quantization,
            SourceHdrTransferFunction = sourceSignal.HdrTransferFunction,
            SourceHdrTransferCode = sourceSignal.HdrTransferCode,
            SourceFirmware = sourceSignal.Firmware,
            SourceAudioFormat = sourceSignal.AudioFormat,
            SourceAudioSampleRate = sourceSignal.AudioSampleRate,
            SourceInputSource = sourceSignal.InputSource,
            SourceUsbHostProtocol = sourceSignal.UsbHostProtocol,
            SourceHdcpMode = sourceSignal.HdcpMode,
            SourceHdcpVersion = sourceSignal.HdcpVersion,
            SourceRxTxHdcpVersion = sourceSignal.RxTxHdcpVersion,
            SourceRawTimingHex = sourceSignal.RawTimingHex,
            SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = sourceTelemetry.SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = sourceTelemetry.SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = sourceTelemetry.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = sourceTelemetry.SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,
            SourceTelemetryBackend = sourceTelemetry.SourceTelemetryBackend,
            SourceTelemetrySuppressed = sourceTelemetry.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = sourceTelemetry.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = sourceTelemetry.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = sourceTelemetry.SourceTelemetrySummaryText,
            SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,
            SelectedRecordingFormat = userSettings.SelectedRecordingFormat,
            SelectedQuality = userSettings.SelectedQuality,
            SelectedPreset = userSettings.SelectedPreset,
            SelectedSplitEncodeMode = userSettings.SelectedSplitEncodeMode,
            SelectedVideoFormat = userSettings.SelectedVideoFormat,
            CustomBitrateMbps = userSettings.CustomBitrateMbps,
            PreviewVolumePercent = userSettings.PreviewVolumePercent,
            IsStatsVisible = userSettings.IsStatsVisible,
            IsHdrAvailable = hdrPipeline.IsHdrAvailable,
            IsHdrEnabled = hdrPipeline.IsHdrEnabled,
            HdrOutputActive = hdrPipeline.HdrOutputActive,
            HdrRuntimeState = hdrPipeline.HdrRuntimeState,
            HdrReadinessReason = hdrPipeline.HdrReadinessReason,
            HdrWarmupState = hdrPipeline.HdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrPipeline.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = hdrPipeline.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = hdrPipeline.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = hdrPipeline.HdrDowngradeCode,
            RequestedPipelineMode = hdrPipeline.RequestedPipelineMode,
            ActivePipelineMode = hdrPipeline.ActivePipelineMode,
            PipelineModeMatched = hdrPipeline.PipelineModeMatched,
            PipelineModeStatus = hdrPipeline.PipelineModeStatus,
            PipelineModeReason = hdrPipeline.PipelineModeReason,
            TelemetryAlignmentStatus = hdrPipeline.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,
            OutputPath = viewModelSnapshot.OutputPath,
            RecordingTime = viewModelSnapshot.RecordingTime,
            RecordingSizeInfo = viewModelSnapshot.RecordingSizeInfo,
            RecordingBitrateInfo = viewModelSnapshot.RecordingBitrateInfo,
            AudioPeak = viewModelSnapshot.AudioPeak,
            AudioClipping = viewModelSnapshot.AudioClipping,
            AudioSignalPresent = audioSignal.SignalPresent,
            AudioMutedSuspected = audioSignal.MutedSuspected,
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            IngestVideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            IngestVideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs,
            AudioBufferHealthStatus = captureRuntime.AudioBufferHealthStatus,
            AudioBufferHealthReason = captureRuntime.AudioBufferHealthReason,
            AudioBufferUnderrunDetected = captureRuntime.AudioBufferUnderrunDetected,
            AudioBufferOverrunDetected = captureRuntime.AudioBufferOverrunDetected,
            AudioBufferUnderrunEvents = captureRuntime.AudioBufferUnderrunEvents,
            AudioBufferOverrunEvents = captureRuntime.AudioBufferOverrunEvents,
            MemoryPreference = captureRuntime.MemoryPreference,
            VideoRequestedSubtype = captureRuntime.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureRuntime.FrameLedgerCapacity,
            FrameLedgerEventCount = captureRuntime.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureRuntime.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,
            PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,
            EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,
            EncoderVideoFramesEncoded = health.VideoFramesConverted,
            EncoderLastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            EncoderLastWriteAgeMs = health.LastVideoWriteAgeMs,
            RecordingBackend = recordingBackend.Backend,
            AudioPathMode = recordingBackend.AudioPathMode,
            MuxResult = recordingBackend.MuxResult,
            RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,
            RecordingIntegrityComplete = captureRuntime.RecordingIntegrityComplete,
            RecordingIntegrityBackend = captureRuntime.RecordingIntegrityBackend,
            RecordingIntegrityCompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            RecordingIntegritySourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            RecordingIntegrityAcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            RecordingIntegrityPipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            RecordingIntegrityQueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            RecordingIntegritySubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            RecordingIntegrityEncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            RecordingIntegrityPacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            RecordingIntegrityEncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            RecordingIntegritySequenceGaps = captureRuntime.RecordingIntegritySequenceGaps,
            RecordingIntegrityQueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            RecordingIntegrityQueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            RecordingIntegrityBackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            RecordingIntegrityBackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            RecordingIntegrityBackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs,
            RecordingIntegrityAudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            RecordingIntegrityAudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            RecordingIntegrityAudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            RecordingIntegrityAudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            RecordingIntegrityAudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            RecordingIntegrityAudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            RecordingIntegrityAudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            RecordingIntegrityAudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            RecordingIntegrityAudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps,
            RecordingIntegrityAvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            RecordingIntegrityAvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            RecordingIntegrityEncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples,
            RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,
            RequestedWidth = captureRuntime.RequestedWidth,
            RequestedHeight = captureRuntime.RequestedHeight,
            RequestedFrameRate = captureRuntime.RequestedFrameRate,
            RequestedFrameRateArg = captureRuntime.RequestedFrameRateArg,
            RequestedFrameRateNumerator = captureRuntime.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = captureRuntime.RequestedFrameRateDenominator,
            RequestedPixelFormat = captureRuntime.RequestedPixelFormat,
            RequestedFormat = captureRuntime.RequestedFormat,
            RequestedQuality = captureRuntime.RequestedQuality,
            RequestedHdrEnabled = captureRuntime.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = captureRuntime.RequestedHdrMasteringMetadata,
            RequestedAudioEnabled = captureRuntime.RequestedAudioEnabled,
            HdrActivationReason = captureRuntime.HdrActivationReason,
            HdrAutoDowngraded = captureRuntime.HdrAutoDowngraded,
            HdrAutoDowngradeReason = captureRuntime.HdrAutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit,
            ActualWidth = captureRuntime.ActualWidth,
            ActualHeight = captureRuntime.ActualHeight,
            ActualFrameRate = captureRuntime.ActualFrameRate,
            ActualFrameRateArg = captureRuntime.ActualFrameRateArg,
            NegotiatedWidth = negotiatedCaptureFormat.Width,
            NegotiatedHeight = negotiatedCaptureFormat.Height,
            NegotiatedFrameRate = negotiatedCaptureFormat.FrameRate,
            NegotiatedFrameRateArg = negotiatedCaptureFormat.FrameRateArg,
            NegotiatedFrameRateNumerator = negotiatedCaptureFormat.FrameRateNumerator,
            NegotiatedFrameRateDenominator = negotiatedCaptureFormat.FrameRateDenominator,
            NegotiatedPixelFormat = negotiatedCaptureFormat.PixelFormat,
            RequestedReaderSubtype = captureRuntime.RequestedReaderSubtype,
            ReaderSourceStreamType = captureRuntime.ReaderSourceStreamType,
            ReaderSourceSubtype = captureRuntime.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureRuntime.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureRuntime.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureRuntime.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureRuntime.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureRuntime.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureRuntime.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureRuntime.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureRuntime.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            EncoderOutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            EncoderVideoCodec = captureRuntime.EncoderVideoCodec,
            EncoderVideoProfile = captureRuntime.EncoderVideoProfile,
            EncoderTenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = negotiatedCaptureFormat.MediaSubtypeToken,
            PreviewFramesArrived = previewFrame.FramesArrived,
            PreviewFramesDisplayed = previewFrame.FramesDisplayed,
            PreviewFramesDropped = previewFrame.FramesDropped,
            PreviewCadenceSampleCount = previewRuntime.DisplayCadenceSampleCount,
            PreviewCadenceObservedFps = previewRuntime.DisplayCadenceObservedFps,
            PreviewCadenceExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            PreviewCadenceAverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            PreviewCadenceP95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            PreviewCadenceP99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            PreviewCadenceMaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            PreviewCadenceFivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            PreviewCadenceSampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            PreviewCadenceRecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
            PreviewCadenceJitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            PreviewCadenceSlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            PreviewCadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,
            PreviewGpuActive = previewRuntime.GpuActive,
            PreviewPlaceholderVisible = previewRuntime.PlaceholderVisible,
            PreviewGpuElementVisible = previewRuntime.GpuElementVisible,
            PreviewCpuElementVisible = previewRuntime.CpuElementVisible,
            PreviewRendererAttached = previewRuntime.RendererAttached,
            PreviewStartupState = previewStartup.State,
            PreviewAttemptId = previewStartup.AttemptId,
            PreviewStartupElapsedMs = previewStartup.ElapsedMs,
            PreviewStartupTimeoutMs = previewStartup.TimeoutMs,
            PreviewGpuSignalMediaOpened = previewStartup.GpuSignalMediaOpened,
            PreviewGpuSignalFirstFrame = previewStartup.GpuSignalFirstFrame,
            PreviewGpuSignalPlaybackAdvancing = previewStartup.GpuSignalPlaybackAdvancing,
            PreviewStartupRequiredSignals = previewStartup.RequiredSignals,
            PreviewStartupReceivedSignals = previewStartup.ReceivedSignals,
            PreviewStartupStrategy = previewStartup.Strategy,
            PreviewStartupMissingSignals = previewStartup.MissingSignals,
            PreviewRecoveryAttemptCount = previewStartup.RecoveryAttemptCount,
            PreviewLastFailureReason = previewStartup.LastFailureReason,
            PreviewFirstVisualConfirmed = previewStartup.FirstVisualConfirmed,
            PreviewBlankSuspected = previewStartup.BlankSuspected,
            PreviewStalled = previewStartup.Stalled,
            PreviewRendererMode = previewStartup.RendererMode,
            PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,
            PreviewD3DMaxFrameLatency = previewRuntime.D3DMaxFrameLatency,
            PreviewD3DSwapChainBufferCount = previewRuntime.D3DSwapChainBufferCount,
            PreviewD3DSwapChainAddress = previewRuntime.D3DSwapChainAddress,
            PreviewD3DFramesSubmitted = previewRuntime.D3DFramesSubmitted,
            PreviewD3DFramesRendered = previewRuntime.D3DFramesRendered,
            PreviewD3DFramesDropped = previewRuntime.D3DFramesDropped,
            PreviewD3DRenderThreadFailureCount = previewRuntime.D3DRenderThreadFailureCount,
            PreviewD3DLastRenderThreadFailureType = previewRuntime.D3DLastRenderThreadFailureType,
            PreviewD3DLastRenderThreadFailureMessage = previewRuntime.D3DLastRenderThreadFailureMessage,
            PreviewD3DLastRenderThreadFailureHResult = previewRuntime.D3DLastRenderThreadFailureHResult,
            PreviewD3DPendingFrameCount = previewRuntime.D3DPendingFrameCount,
            PreviewD3DInputColorSpace = previewRuntime.D3DInputColorSpace,
            PreviewD3DOutputColorSpace = previewRuntime.D3DOutputColorSpace,
            PreviewD3DCpuTimingSampleCount = previewRuntime.D3DCpuTimingSampleCount,
            PreviewD3DInputUploadCpuAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            PreviewD3DInputUploadCpuP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            PreviewD3DInputUploadCpuMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            PreviewD3DRenderSubmitCpuAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            PreviewD3DRenderSubmitCpuP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            PreviewD3DRenderSubmitCpuP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            PreviewD3DRenderSubmitCpuMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PreviewD3DPresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PreviewD3DPresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PreviewD3DPresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PreviewD3DPresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            PreviewD3DTotalFrameCpuAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            PreviewD3DTotalFrameCpuP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            PreviewD3DTotalFrameCpuP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            PreviewD3DTotalFrameCpuMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs,
            PreviewD3DPipelineLatencySampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            PreviewD3DPipelineLatencyAvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            PreviewD3DPipelineLatencyP95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            PreviewD3DPipelineLatencyP99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            PreviewD3DPipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,
            PreviewD3DFrameLatencyWaitEnabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            PreviewD3DFrameLatencyWaitHandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            PreviewD3DFrameLatencyWaitCallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            PreviewD3DFrameLatencyWaitSignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            PreviewD3DFrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            PreviewD3DFrameLatencyWaitUnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            PreviewD3DFrameLatencyWaitLastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            PreviewD3DFrameLatencyWaitLastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            PreviewD3DFrameLatencyWaitSampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            PreviewD3DFrameLatencyWaitAvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            PreviewD3DFrameLatencyWaitP95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            PreviewD3DFrameLatencyWaitP99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            PreviewD3DFrameLatencyWaitMaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs,
            PreviewD3DFrameStatsSampleCount = previewRuntime.D3DFrameStatsSampleCount,
            PreviewD3DFrameStatsSuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            PreviewD3DFrameStatsFailureCount = previewRuntime.D3DFrameStatsFailureCount,
            PreviewD3DFrameStatsLastError = previewRuntime.D3DFrameStatsLastError,
            PreviewD3DFrameStatsPresentCount = previewRuntime.D3DFrameStatsPresentCount,
            PreviewD3DFrameStatsPresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            PreviewD3DFrameStatsSyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            PreviewD3DFrameStatsSyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            PreviewD3DFrameStatsLastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            PreviewD3DFrameStatsLastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            PreviewD3DFrameStatsLastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            PreviewD3DFrameStatsMissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,
            PreviewD3DFrameStatsRecentFailureCount = recentD3DStatsFailures,
            PreviewD3DLastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            PreviewD3DLastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            PreviewD3DLastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            PreviewD3DLastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            PreviewD3DLastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            PreviewD3DLastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            PreviewD3DLastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            PreviewD3DLastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            PreviewD3DLastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            PreviewD3DLastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            PreviewD3DLastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            PreviewD3DLastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            PreviewD3DLastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            PreviewD3DLastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            PreviewD3DLastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            PreviewD3DLastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            PreviewD3DLastDropReason = previewRuntime.D3DLastDropReason,
            PreviewD3DRecentSlowFrames = previewRuntime.D3DRecentSlowFrames,
            PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,
            PreviewGpuNaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            PreviewGpuNaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            PreviewGpuPositionMs = previewRuntime.GpuPositionMs,
            PreviewGpuPositionEventCount = previewRuntime.GpuPositionEventCount,
            PreviewHdrInputDetected = previewHdrState.InputDetected,
            PreviewToneMapMode = previewHdrState.ToneMapMode,
            PreviewColorContext = captureRuntime.NegotiatedPixelFormat,
            ConversionQueueDepth = health.ConversionQueueDepth,
            FfmpegVideoQueueDepth = health.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = health.FfmpegAudioQueueDepth,
            VideoFramesArrived = health.VideoFramesArrived,
            VideoFramesQueued = health.VideoFramesQueued,
            VideoFramesDropped = health.VideoFramesDropped,
            VideoFramesDroppedBacklog = health.VideoFramesDroppedBacklog,
            VideoFramesConverted = health.VideoFramesConverted,
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoDropsQueueSaturated = health.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction,
            RecordingEncodingFailed = health.RecordingEncodingFailed,
            RecordingEncodingFailureType = health.RecordingEncodingFailureType,
            RecordingEncodingFailureMessage = health.RecordingEncodingFailureMessage,
            RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,
            RecordingVideoQueueMaxDepth = health.RecordingVideoQueueMaxDepth,
            RecordingVideoFramesSubmittedToEncoder = health.RecordingVideoFramesSubmittedToEncoder,
            RecordingVideoEncoderPts = health.RecordingVideoEncoderPts,
            RecordingVideoEncoderPacketsWritten = health.RecordingVideoEncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = health.RecordingVideoEncoderDroppedFrames,
            RecordingVideoSequenceGaps = health.RecordingVideoSequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = health.RecordingVideoQueueOldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = health.RecordingVideoQueueLastLatencyMs,
            RecordingVideoQueueLatencySampleCount = health.RecordingVideoQueueLatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = health.RecordingVideoQueueLatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = health.RecordingVideoQueueLatencyP95Ms,
            RecordingVideoQueueLatencyP99Ms = health.RecordingVideoQueueLatencyP99Ms,
            RecordingVideoQueueLatencyMaxMs = health.RecordingVideoQueueLatencyMaxMs,
            RecordingVideoBackpressureWaitMs = health.RecordingVideoBackpressureWaitMs,
            RecordingVideoBackpressureEvents = health.RecordingVideoBackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = health.RecordingVideoBackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs,
            RecordingGpuQueueDepth = health.RecordingGpuQueueDepth,
            RecordingGpuQueueCapacity = health.RecordingGpuQueueCapacity,
            RecordingGpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            RecordingGpuFramesDropped = health.RecordingGpuFramesDropped,
            RecordingCudaQueueDepth = health.RecordingCudaQueueDepth,
            RecordingCudaQueueCapacity = health.RecordingCudaQueueCapacity,
            RecordingCudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,
            FlashbackEncodingFailed = health.FlashbackEncodingFailed,
            FlashbackEncodingFailureType = health.FlashbackEncodingFailureType,
            FlashbackEncodingFailureMessage = health.FlashbackEncodingFailureMessage,
            FatalCleanupInProgress = health.FatalCleanupInProgress,
            FlashbackCleanupInProgress = health.FlashbackCleanupInProgress,
            FlashbackForceRotateActive = health.FlashbackForceRotateActive,
            FlashbackForceRotateRequested = health.FlashbackForceRotateRequested,
            FlashbackForceRotateDraining = health.FlashbackForceRotateDraining,
            FlashbackTempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,
            FlashbackStartupCacheBudgetBytes = health.FlashbackStartupCacheBudgetBytes,
            FlashbackStartupCacheBytes = health.FlashbackStartupCacheBytes,
            FlashbackStartupCacheSessionCount = health.FlashbackStartupCacheSessionCount,
            FlashbackStartupCacheDeletedSessionCount = health.FlashbackStartupCacheDeletedSessionCount,
            FlashbackStartupCacheFreedBytes = health.FlashbackStartupCacheFreedBytes,
            FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,
            FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            FlashbackVideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            FlashbackVideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            FlashbackVideoEncoderPts = health.FlashbackVideoEncoderPts,
            FlashbackVideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            FlashbackVideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            FlashbackVideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            FlashbackVideoQueueRejectedFrames = health.FlashbackVideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = health.FlashbackVideoQueueLastRejectReason,
            FlashbackVideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            FlashbackVideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            FlashbackVideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            FlashbackVideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            FlashbackVideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            FlashbackVideoQueueLatencyP99Ms = health.FlashbackVideoQueueLatencyP99Ms,
            FlashbackVideoQueueLatencyMaxMs = health.FlashbackVideoQueueLatencyMaxMs,
            FlashbackVideoBackpressureWaitMs = health.FlashbackVideoBackpressureWaitMs,
            FlashbackVideoBackpressureEvents = health.FlashbackVideoBackpressureEvents,
            FlashbackVideoBackpressureLastWaitMs = health.FlashbackVideoBackpressureLastWaitMs,
            FlashbackVideoBackpressureMaxWaitMs = health.FlashbackVideoBackpressureMaxWaitMs,
            FlashbackGpuQueueDepth = health.FlashbackGpuQueueDepth,
            FlashbackGpuQueueCapacity = health.FlashbackGpuQueueCapacity,
            FlashbackGpuQueueMaxDepth = health.FlashbackGpuQueueMaxDepth,
            FlashbackGpuFramesEnqueued = health.FlashbackGpuFramesEnqueued,
            FlashbackGpuFramesDropped = health.FlashbackGpuFramesDropped,
            FlashbackGpuQueueRejectedFrames = health.FlashbackGpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,
            AudioDropsQueueSaturated = audioDrops.QueueSaturated,
            AudioDropsBacklogEviction = audioDrops.BacklogEviction,
            AudioChunksDropped = audioDrops.ChunksDropped,
            AudioQueueDropsRealtime = audioDrops.QueueDropsRealtime,
            AudioQueueDropsFileWriter = audioDrops.QueueDropsFileWriter,
            EstimatedPipelineLatencyMs = previewFrame.EstimatedPipelineLatencyMs,
            ExpectedCaptureFrameRate = health.ExpectedFrameRate,
            CaptureCadenceSampleCount = health.CaptureCadenceSampleCount,
            CaptureCadenceObservedFps = health.CaptureCadenceObservedFps,
            CaptureCadenceExpectedIntervalMs = health.CaptureCadenceExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = health.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95IntervalMs = health.CaptureCadenceP95IntervalMs,
            CaptureCadenceP99IntervalMs = health.CaptureCadenceP99IntervalMs,
            CaptureCadenceMaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            CaptureCadenceOnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps = health.CaptureCadenceFivePercentLowFps,
            CaptureCadenceSampleDurationMs = health.CaptureCadenceSampleDurationMs,
            CaptureCadenceRecentIntervalsMs = health.CaptureCadenceRecentIntervalsMs,
            CaptureCadenceJitterStdDevMs = health.CaptureCadenceJitterStdDevMs,
            CaptureCadenceSevereGapCount = health.CaptureCadenceSevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
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
            MjpegDecoderCount = mjpegTiming.DecoderCount,
            MjpegReorderSampleCount = mjpegTiming.ReorderSampleCount,
            MjpegReorderAvgMs = mjpegTiming.ReorderAvgMs,
            MjpegReorderP95Ms = mjpegTiming.ReorderP95Ms,
            MjpegReorderMaxMs = mjpegTiming.ReorderMaxMs,
            MjpegPipelineSampleCount = mjpegTiming.PipelineSampleCount,
            MjpegPipelineAvgMs = mjpegTiming.PipelineAvgMs,
            MjpegPipelineP95Ms = mjpegTiming.PipelineP95Ms,
            MjpegPipelineMaxMs = mjpegTiming.PipelineMaxMs,
            MjpegTotalDecoded = health.MjpegTotalDecoded,
            MjpegTotalEmitted = health.MjpegTotalEmitted,
            MjpegTotalDropped = health.MjpegTotalDropped,
            MjpegCompressedFramesQueued = health.MjpegCompressedFramesQueued,
            MjpegCompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            MjpegCompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            MjpegCompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            MjpegCompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            MjpegDecodeFailures = health.MjpegDecodeFailures,
            MjpegReorderCollisions = health.MjpegReorderCollisions,
            MjpegEmitFailures = health.MjpegEmitFailures,
            MjpegCompressedQueueDepth = health.MjpegCompressedQueueDepth,
            MjpegCompressedQueueBytes = health.MjpegCompressedQueueBytes,
            MjpegCompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            MjpegReorderSkips = health.MjpegReorderSkips,
            MjpegReorderBufferDepth = health.MjpegReorderBufferDepth,
            MjpegPeakReorderDepth = health.MjpegPeakReorderDepth,
            MjpegPeakCompressedQueueBytes = health.MjpegPeakCompressedQueueBytes,
            MjpegReorderRingForceDrops = health.MjpegReorderRingForceDrops,
            MjpegPreviewJitterEnabled = health.MjpegPreviewJitterEnabled,
            MjpegPreviewJitterTargetDepth = health.MjpegPreviewJitterTargetDepth,
            MjpegPreviewJitterMaxDepth = health.MjpegPreviewJitterMaxDepth,
            MjpegPreviewJitterQueueDepth = health.MjpegPreviewJitterQueueDepth,
            MjpegPreviewJitterTotalQueued = health.MjpegPreviewJitterTotalQueued,
            MjpegPreviewJitterTotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            MjpegPreviewJitterTotalDropped = health.MjpegPreviewJitterTotalDropped,
            MjpegPreviewJitterUnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            MjpegPreviewJitterResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount,
            MjpegPreviewJitterInputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            MjpegPreviewJitterInputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            MjpegPreviewJitterInputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            MjpegPreviewJitterInputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            MjpegPreviewJitterOutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            MjpegPreviewJitterOutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            MjpegPreviewJitterOutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            MjpegPreviewJitterOutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            MjpegPreviewJitterLatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            MjpegPreviewJitterLatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            MjpegPreviewJitterLatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs,
            MjpegPreviewJitterDeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            MjpegPreviewJitterClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            MjpegPreviewJitterTargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,
            MjpegPreviewJitterLastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            MjpegPreviewJitterLastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            MjpegPreviewJitterLastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            MjpegPreviewJitterLastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            MjpegPreviewJitterLastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MjpegPreviewJitterMaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount,
            MjpegPacketHashSampleCount = health.MjpegPacketHashSampleCount,
            MjpegPacketHashUniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            MjpegPacketHashDuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            MjpegPacketHashLongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            MjpegPacketHashInputObservedFps = health.MjpegPacketHashInputObservedFps,
            MjpegPacketHashUniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            MjpegPacketHashLastHash = health.MjpegPacketHashLastHash,
            MjpegPacketHashLastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            MjpegPacketHashPattern = health.MjpegPacketHashPattern,
            MjpegPacketHashRecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            MjpegPacketHashRecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            MjpegPacketHashRecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags,
            VisualCadenceSampleCount = health.VisualCadenceSampleCount,
            VisualCadenceChangedFrameCount = health.VisualCadenceChangedFrameCount,
            VisualCadenceRepeatFrameCount = health.VisualCadenceRepeatFrameCount,
            VisualCadenceLongestRepeatRun = health.VisualCadenceLongestRepeatRun,
            VisualCadenceOutputObservedFps = health.VisualCadenceOutputObservedFps,
            VisualCadenceChangeObservedFps = health.VisualCadenceChangeObservedFps,
            VisualCadenceRepeatFramePercent = health.VisualCadenceRepeatFramePercent,
            VisualCadenceLastDelta = health.VisualCadenceLastDelta,
            VisualCadenceAverageDelta = health.VisualCadenceAverageDelta,
            VisualCadenceP95Delta = health.VisualCadenceP95Delta,
            VisualCadenceMotionScore = health.VisualCadenceMotionScore,
            VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,
            VisualCadenceRecentOutputIntervalsMs = health.VisualCadenceRecentOutputIntervalsMs,
            VisualCadenceRecentChangeIntervalsMs = health.VisualCadenceRecentChangeIntervalsMs,
            VisualCenterCadenceSampleCount = health.VisualCenterCadenceSampleCount,
            VisualCenterCadenceChangedFrameCount = health.VisualCenterCadenceChangedFrameCount,
            VisualCenterCadenceRepeatFrameCount = health.VisualCenterCadenceRepeatFrameCount,
            VisualCenterCadenceLongestRepeatRun = health.VisualCenterCadenceLongestRepeatRun,
            VisualCenterCadenceOutputObservedFps = health.VisualCenterCadenceOutputObservedFps,
            VisualCenterCadenceChangeObservedFps = health.VisualCenterCadenceChangeObservedFps,
            VisualCenterCadenceRepeatFramePercent = health.VisualCenterCadenceRepeatFramePercent,
            VisualCenterCadenceLastDelta = health.VisualCenterCadenceLastDelta,
            VisualCenterCadenceAverageDelta = health.VisualCenterCadenceAverageDelta,
            VisualCenterCadenceP95Delta = health.VisualCenterCadenceP95Delta,
            VisualCenterCadenceMotionScore = health.VisualCenterCadenceMotionScore,
            VisualCenterCadenceMotionConfidence = health.VisualCenterCadenceMotionConfidence,
            VisualCenterCadenceRecentOutputIntervalsMs = health.VisualCenterCadenceRecentOutputIntervalsMs,
            VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,
            MjpegPerDecoder = mjpegTiming.PerDecoder,
            RecordingVideoBytes = recordingStats.VideoBytes,
            RecordingAudioBytes = recordingStats.AudioBytes,
            RecordingTotalBytes = recordingStats.TotalBytes,
            RecordingFileGrowing = recordingFileGrowing,
            LastOutputPath = captureRuntime.LastOutputPath,
            LastFinalizeStatus = captureRuntime.LastFinalizeStatus,
            LastFinalizeUtc = captureRuntime.LastFinalizeUtc,
            RecordingLifecyclePhase = captureRuntime.RecordingLifecyclePhase,
            RecordingFinalizeOutcome = captureRuntime.RecordingFinalizeOutcome,
            RecordingFinalizeFailureCode = captureRuntime.RecordingFinalizeFailureCode,
            RecordingFinalizationVerificationCompleted = captureRuntime.RecordingFinalizationVerificationCompleted,
            RecordingFinalizationCleanupPending = captureRuntime.RecordingFinalizationCleanupPending,
            RecordingFinalizationElapsedMs = captureRuntime.RecordingFinalizationElapsedMs,
            RecordingRecoveryPath = captureRuntime.RecordingRecoveryPath,
            RecordingRequestedTracks = captureRuntime.RecordingRequestedTracks,
            RecordingObservedTracks = captureRuntime.RecordingObservedTracks,
            LastPreservedArtifacts = captureRuntime.LastPreservedArtifacts,
            RecordingFinalizationProgressStage = captureRuntime.RecordingFinalizationProgressStage,
            LastRecordingFinalizationProgressUtc = captureRuntime.LastRecordingFinalizationProgressUtc,
            LastOutputExists = lastOutput.Exists,
            LastOutputSizeBytes = lastOutput.SizeBytes,
            LastVerification = lastVerification,
            HdrTruthVerdict = hdrPipeline.TruthVerdict,
            MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,
            MemoryPrivateBytesMb = processResources.MemoryPrivateBytesMb,
            MemoryManagedHeapMb = processResources.MemoryManagedHeapMb,
            MemoryTotalAllocatedMb = processResources.MemoryTotalAllocatedMb,
            ProcessCpuPercent = processResources.ProcessCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processResources.ProcessCpuTotalProcessorTimeMs,
            MemoryGcHeapSizeMb = processResources.MemoryGcHeapSizeMb,
            MemoryGcGen0Collections = processResources.MemoryGcGen0Collections,
            MemoryGcGen1Collections = processResources.MemoryGcGen1Collections,
            MemoryGcGen2Collections = processResources.MemoryGcGen2Collections,
            MemoryGcPauseTimePercent = processResources.MemoryGcPauseTimePercent,
            MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,
            ThreadPoolWorkerAvailable = processResources.ThreadPoolWorkerAvailable,
            ThreadPoolWorkerMax = processResources.ThreadPoolWorkerMax,
            ThreadPoolIoAvailable = processResources.ThreadPoolIoAvailable,
            ThreadPoolIoMax = processResources.ThreadPoolIoMax,
            AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs = captureRuntime.AvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,
            FlashbackActive = health.FlashbackActive,
            FlashbackBufferedDurationMs = health.FlashbackBufferedDurationMs,
            FlashbackDiskBytes = health.FlashbackDiskBytes,
            FlashbackTotalBytesWritten = health.FlashbackTotalBytesWritten,
            FlashbackOutputBytes = health.FlashbackOutputBytes,
            FlashbackFilePath = health.FlashbackFilePath,
            FlashbackEncodedFrames = health.FlashbackEncodedFrames,
            FlashbackDroppedFrames = health.FlashbackDroppedFrames,
            FlashbackGpuEncoding = health.FlashbackGpuEncoding,
            FlashbackBackendSettingsStale = health.FlashbackBackendSettingsStale,
            FlashbackBackendSettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            FlashbackBackendActiveFormat = health.FlashbackBackendActiveFormat,
            FlashbackBackendRequestedFormat = health.FlashbackBackendRequestedFormat,
            FlashbackBackendActivePreset = health.FlashbackBackendActivePreset,
            FlashbackBackendRequestedPreset = health.FlashbackBackendRequestedPreset,
            FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            FlashbackCodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,
            EncoderCodecName = health.EncoderCodecName,
            EncoderTargetBitRate = health.EncoderTargetBitRate,
            EncoderWidth = health.EncoderWidth,
            EncoderHeight = health.EncoderHeight,
            EncoderFrameRate = health.EncoderFrameRate,
            EncoderFrameRateNumerator = health.EncoderFrameRateNumerator,
            EncoderFrameRateDenominator = health.EncoderFrameRateDenominator,
            FlashbackVideoQueueDepth = health.FlashbackVideoQueueDepth,
            FlashbackAudioQueueDepth = health.FlashbackAudioQueueDepth,
            FlashbackAudioQueueCapacity = health.FlashbackAudioQueueCapacity,
            FlashbackPlaybackState = health.FlashbackPlaybackState,
            FlashbackPlaybackPositionMs = health.FlashbackPlaybackPositionMs,
            FlashbackDecoderHwAccel = health.FlashbackDecoderHwAccel,
            FlashbackPlaybackFrameCount = health.FlashbackPlaybackFrameCount,
            FlashbackPlaybackLateFrames = health.FlashbackPlaybackLateFrames,
            FlashbackPlaybackDroppedFrames = health.FlashbackPlaybackDroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = health.FlashbackPlaybackSegmentSwitches,
            FlashbackPlaybackFmp4Reopens = health.FlashbackPlaybackFmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = health.FlashbackPlaybackWriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = health.FlashbackPlaybackNearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = health.FlashbackPlaybackDecodeErrorSnaps,
            FlashbackPlaybackSubmitFailures = health.FlashbackPlaybackSubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = health.FlashbackPlaybackLastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = health.FlashbackPlaybackLastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = health.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = health.FlashbackPlaybackLastSubmitFailure,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = health.FlashbackPlaybackLastSegmentSwitchUtcUnixMs,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = health.FlashbackPlaybackLastFmp4ReopenUtcUnixMs,
            FlashbackPlaybackLastWriteHeadWaitGapMs = health.FlashbackPlaybackLastWriteHeadWaitGapMs,
            FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,
            FlashbackPlaybackObservedFps = health.FlashbackPlaybackObservedFps,
            FlashbackPlaybackAvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            FlashbackPlaybackCadenceSampleCount = health.FlashbackPlaybackCadenceSampleCount,
            FlashbackPlaybackP95FrameMs = health.FlashbackPlaybackP95FrameMs,
            FlashbackPlaybackP99FrameMs = health.FlashbackPlaybackP99FrameMs,
            FlashbackPlaybackMaxFrameMs = health.FlashbackPlaybackMaxFrameMs,
            FlashbackPlaybackSlowFrames = health.FlashbackPlaybackSlowFrames,
            FlashbackPlaybackSlowFramePercent = health.FlashbackPlaybackSlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = health.FlashbackPlaybackOnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = health.FlashbackPlaybackFivePercentLowFps,
            FlashbackPlaybackSampleDurationMs = health.FlashbackPlaybackSampleDurationMs,
            FlashbackPlaybackRecentFrameIntervalsMs = health.FlashbackPlaybackRecentFrameIntervalsMs,
            FlashbackPlaybackPtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,
            FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = health.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs,
            FlashbackPlaybackLastPtsCadenceDeltaMs = health.FlashbackPlaybackLastPtsCadenceDeltaMs,
            FlashbackPlaybackLastPtsCadenceExpectedMs = health.FlashbackPlaybackLastPtsCadenceExpectedMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            FlashbackPlaybackDecodeSampleCount = health.FlashbackPlaybackDecodeSampleCount,
            FlashbackPlaybackDecodeAvgMs = health.FlashbackPlaybackDecodeAvgMs,
            FlashbackPlaybackDecodeP95Ms = health.FlashbackPlaybackDecodeP95Ms,
            FlashbackPlaybackDecodeP99Ms = health.FlashbackPlaybackDecodeP99Ms,
            FlashbackPlaybackDecodeMaxMs = health.FlashbackPlaybackDecodeMaxMs,
            FlashbackPlaybackMaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,
            FlashbackPlaybackMaxDecodeReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            FlashbackPlaybackMaxDecodeSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = health.FlashbackPlaybackMaxDecodePositionMs,
            FlashbackAvDriftMs = health.FlashbackAvDriftMs,
            FlashbackPlaybackThreadAlive = health.FlashbackPlaybackThreadAlive,
            FlashbackPlaybackCommandsEnqueued = health.FlashbackPlaybackCommandsEnqueued,
            FlashbackPlaybackCommandsProcessed = health.FlashbackPlaybackCommandsProcessed,
            FlashbackPlaybackCommandsDropped = health.FlashbackPlaybackCommandsDropped,
            FlashbackPlaybackCommandsSkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            FlashbackPlaybackCommandQueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            FlashbackPlaybackPendingCommands = health.FlashbackPlaybackPendingCommands,
            FlashbackPlaybackMaxPendingCommands = health.FlashbackPlaybackMaxPendingCommands,
            FlashbackPlaybackLastCommandQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            FlashbackPlaybackLastCommandQueued = health.FlashbackPlaybackLastCommandQueued,
            FlashbackPlaybackLastCommandProcessed = health.FlashbackPlaybackLastCommandProcessed,
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,
            FlashbackExportActive = health.FlashbackExportActive,
            FlashbackExportId = health.FlashbackExportId,
            FlashbackExportStatus = health.FlashbackExportStatus,
            FlashbackExportOutputPath = health.FlashbackExportOutputPath,
            FlashbackExportStartedUtcUnixMs = health.FlashbackExportStartedUtcUnixMs,
            FlashbackExportLastProgressUtcUnixMs = health.FlashbackExportLastProgressUtcUnixMs,
            FlashbackExportCompletedUtcUnixMs = health.FlashbackExportCompletedUtcUnixMs,
            FlashbackExportElapsedMs = health.FlashbackExportElapsedMs,
            FlashbackExportLastProgressAgeMs = health.FlashbackExportLastProgressAgeMs,
            FlashbackExportOutputBytes = health.FlashbackExportOutputBytes,
            FlashbackExportThroughputBytesPerSec = health.FlashbackExportThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = health.FlashbackExportSegmentsProcessed,
            FlashbackExportTotalSegments = health.FlashbackExportTotalSegments,
            FlashbackExportPercent = health.FlashbackExportPercent,
            FlashbackExportInPointMs = health.FlashbackExportInPointMs,
            FlashbackExportOutPointMs = health.FlashbackExportOutPointMs,
            FlashbackExportMessage = health.FlashbackExportMessage,
            FlashbackExportFailureKind = health.FlashbackExportFailureKind,
            FlashbackExportForceRotateFallbacks = health.FlashbackExportForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = health.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = health.FlashbackExportLastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = health.FlashbackExportLastForceRotateFallbackOutPointMs,
            LastExportId = health.LastExportId,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };
    }

    private SnapshotStatusProjection BuildSnapshotStatusProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        SnapshotCollectionStamp snapshotCollection)
        => new()
        {
            TimestampUtc = snapshotCollection.CompletedUtc,
            SnapshotCollectionEpoch = snapshotCollection.Epoch,
            SnapshotCollectionStartedUtc = snapshotCollection.StartedUtc,
            SnapshotCollectionCompletedUtc = snapshotCollection.CompletedUtc,
            SnapshotCollectionDurationMs = snapshotCollection.DurationMs,
            SnapshotMixedEpochs = snapshotCollection.Mixed,
            SnapshotMixedEpochReason = snapshotCollection.MixedReason,
            SnapshotViewModelEpoch = snapshotCollection.ViewModelEpoch,
            SnapshotCaptureRuntimeEpoch = snapshotCollection.CaptureRuntimeEpoch,
            SnapshotCaptureHealthEpoch = snapshotCollection.CaptureHealthEpoch,
            SnapshotRecordingStatsEpoch = snapshotCollection.RecordingStatsEpoch,
            SnapshotPreviewRuntimeEpoch = snapshotCollection.PreviewRuntimeEpoch,
            SnapshotOutputEpoch = snapshotCollection.OutputEpoch,
            SnapshotSourceTelemetryEpoch = snapshotCollection.SourceTelemetryEpoch,
            IsInitialized = viewModelSnapshot.IsInitialized,
            IsPreviewing = viewModelSnapshot.IsPreviewing,
            IsRecording = viewModelSnapshot.IsRecording,
            VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,
            IsAudioEnabled = viewModelSnapshot.IsAudioEnabled,
            IsAudioPreviewEnabled = viewModelSnapshot.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = viewModelSnapshot.IsCustomAudioInputEnabled,
            SessionState = captureRuntime.SessionState,
            StatusText = viewModelSnapshot.StatusText
        };

    private readonly record struct SnapshotStatusProjection
    {
        public DateTimeOffset TimestampUtc { get; init; }
        public long SnapshotCollectionEpoch { get; init; }
        public DateTimeOffset SnapshotCollectionStartedUtc { get; init; }
        public DateTimeOffset SnapshotCollectionCompletedUtc { get; init; }
        public long SnapshotCollectionDurationMs { get; init; }
        public bool SnapshotMixedEpochs { get; init; }
        public string SnapshotMixedEpochReason { get; init; }
        public long SnapshotViewModelEpoch { get; init; }
        public long SnapshotCaptureRuntimeEpoch { get; init; }
        public long SnapshotCaptureHealthEpoch { get; init; }
        public long SnapshotRecordingStatsEpoch { get; init; }
        public long SnapshotPreviewRuntimeEpoch { get; init; }
        public long SnapshotOutputEpoch { get; init; }
        public long SnapshotSourceTelemetryEpoch { get; init; }
        public bool IsInitialized { get; init; }
        public bool IsPreviewing { get; init; }
        public bool IsRecording { get; init; }
        public bool VerificationInProgress { get; init; }
        public bool IsAudioEnabled { get; init; }
        public bool IsAudioPreviewEnabled { get; init; }
        public bool IsCustomAudioInputEnabled { get; init; }
        public CaptureSessionState SessionState { get; init; }
        public string StatusText { get; init; }
    }

    private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(
        PerformanceEvaluation performance,
        DiagnosticEvaluation diagnostic,
        PreviewPacingClassification previewPacingClassification)
        => new()
        {
            PerformanceScore = performance.Score,
            PerformancePerfectionMet = performance.PerfectionMet,
            PerformanceSummary = performance.Summary,
            DiagnosticHealthStatus = diagnostic.HealthStatus,
            DiagnosticLikelyStage = diagnostic.LikelyStage,
            DiagnosticSummary = diagnostic.Summary,
            DiagnosticEvidence = diagnostic.Evidence,
            DiagnosticSourceLane = diagnostic.SourceLane,
            DiagnosticDecodeLane = diagnostic.DecodeLane,
            DiagnosticPreviewLane = diagnostic.PreviewLane,
            DiagnosticRenderLane = diagnostic.RenderLane,
            DiagnosticPresentLane = diagnostic.PresentLane,
            DiagnosticRecordingLane = diagnostic.RecordingLane,
            DiagnosticAudioLane = diagnostic.AudioLane,
            PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,
            PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence,
            PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence,
            PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,
            PerformanceThresholdCaptureP95Multiplier = _perfectionCaptureP95MultiplierThreshold,
            PerformanceThresholdPreviewSlowPercent = _perfectionPreviewSlowPercentThreshold,
            PerformanceThresholdVerificationDropPercent = _perfectionVerificationDropPercentThreshold
        };

    private readonly record struct SnapshotEvaluationProjection
    {
        public double PerformanceScore { get; init; }
        public bool PerformancePerfectionMet { get; init; }
        public string PerformanceSummary { get; init; }
        public string DiagnosticHealthStatus { get; init; }
        public string DiagnosticLikelyStage { get; init; }
        public string DiagnosticSummary { get; init; }
        public string DiagnosticEvidence { get; init; }
        public string DiagnosticSourceLane { get; init; }
        public string DiagnosticDecodeLane { get; init; }
        public string DiagnosticPreviewLane { get; init; }
        public string DiagnosticRenderLane { get; init; }
        public string DiagnosticPresentLane { get; init; }
        public string DiagnosticRecordingLane { get; init; }
        public string DiagnosticAudioLane { get; init; }
        public string PreviewPacingLikelySlowStage { get; init; }
        public string PreviewPacingSlowStageConfidence { get; init; }
        public string PreviewPacingSlowStageEvidence { get; init; }
        public double PerformanceThresholdCaptureDropPercent { get; init; }
        public double PerformanceThresholdCaptureP95Multiplier { get; init; }
        public double PerformanceThresholdPreviewSlowPercent { get; init; }
        public double PerformanceThresholdVerificationDropPercent { get; init; }
    }

    private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)
        => new()
        {
            SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,
            SelectedDeviceName = viewModelSnapshot.SelectedDeviceName,
            SelectedAudioInputDeviceId = viewModelSnapshot.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = viewModelSnapshot.SelectedAudioInputDeviceName,
            SelectedResolution = viewModelSnapshot.SelectedResolution,
            SelectedFrameRate = viewModelSnapshot.SelectedFrameRate,
            SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),
            SelectedExactFrameRate = viewModelSnapshot.SelectedExactFrameRate ?? viewModelSnapshot.SelectedFrameRate,
            SelectedExactFrameRateArg = viewModelSnapshot.SelectedExactFrameRateArg,
            DisabledResolutionReason = viewModelSnapshot.DisabledResolutionReason,
            DisabledFrameRateReason = viewModelSnapshot.DisabledFrameRateReason,
            SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,
            SelectedQuality = viewModelSnapshot.SelectedQuality,
            SelectedPreset = viewModelSnapshot.SelectedPreset,
            SelectedSplitEncodeMode = viewModelSnapshot.SelectedSplitEncodeMode,
            SelectedVideoFormat = viewModelSnapshot.SelectedVideoFormat,
            CustomBitrateMbps = viewModelSnapshot.CustomBitrateMbps,
            PreviewVolumePercent = viewModelSnapshot.PreviewVolumePercent,
            IsStatsVisible = viewModelSnapshot.IsStatsVisible
        };

    private readonly record struct UserSettingsProjection
    {
        public string? SelectedDeviceId { get; init; }
        public string? SelectedDeviceName { get; init; }
        public string? SelectedAudioInputDeviceId { get; init; }
        public string? SelectedAudioInputDeviceName { get; init; }
        public string? SelectedResolution { get; init; }
        public double SelectedFrameRate { get; init; }
        public double? SelectedFriendlyFrameRate { get; init; }
        public double? SelectedExactFrameRate { get; init; }
        public string? SelectedExactFrameRateArg { get; init; }
        public string? DisabledResolutionReason { get; init; }
        public string? DisabledFrameRateReason { get; init; }
        public string SelectedRecordingFormat { get; init; }
        public string SelectedQuality { get; init; }
        public string SelectedPreset { get; init; }
        public string SelectedSplitEncodeMode { get; init; }
        public string SelectedVideoFormat { get; init; }
        public double CustomBitrateMbps { get; init; }
        public double PreviewVolumePercent { get; init; }
        public bool IsStatsVisible { get; init; }
    }

    private static SourceSignalProjection BuildSourceSignalProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,
            DetectedFrameRateArg = viewModelSnapshot.DetectedSourceFrameRateArg ?? captureRuntime.DetectedSourceFrameRateArg,
            FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),
            Width = viewModelSnapshot.SourceWidth ?? captureRuntime.SourceWidth,
            Height = viewModelSnapshot.SourceHeight ?? captureRuntime.SourceHeight,
            IsHdr = viewModelSnapshot.SourceIsHdr ?? captureRuntime.SourceIsHdr,
            VideoFormat = captureRuntime.SourceVideoFormat,
            Colorimetry = captureRuntime.SourceColorimetry,
            Quantization = captureRuntime.SourceQuantization,
            HdrTransferFunction = captureRuntime.SourceHdrTransferFunction,
            HdrTransferCode = captureRuntime.SourceHdrTransferCode,
            Firmware = captureRuntime.SourceFirmware,
            AudioFormat = captureRuntime.SourceAudioFormat,
            AudioSampleRate = captureRuntime.SourceAudioSampleRate,
            InputSource = captureRuntime.SourceInputSource,
            UsbHostProtocol = captureRuntime.SourceUsbHostProtocol,
            HdcpMode = captureRuntime.SourceHdcpMode,
            HdcpVersion = captureRuntime.SourceHdcpVersion,
            RxTxHdcpVersion = captureRuntime.SourceRxTxHdcpVersion,
            RawTimingHex = captureRuntime.SourceRawTimingHex
        };

    private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)
        => !string.IsNullOrWhiteSpace(viewModelOrigin) &&
           !string.Equals(viewModelOrigin, "Unknown", StringComparison.OrdinalIgnoreCase)
            ? viewModelOrigin
            : runtimeOrigin;

    private readonly record struct SourceSignalProjection
    {
        public double? DetectedFrameRate { get; init; }
        public string? DetectedFrameRateArg { get; init; }
        public string FrameRateOrigin { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }
        public bool? IsHdr { get; init; }
        public string? VideoFormat { get; init; }
        public string? Colorimetry { get; init; }
        public string? Quantization { get; init; }
        public string? HdrTransferFunction { get; init; }
        public int? HdrTransferCode { get; init; }
        public string? Firmware { get; init; }
        public string? AudioFormat { get; init; }
        public string? AudioSampleRate { get; init; }
        public string? InputSource { get; init; }
        public string? UsbHostProtocol { get; init; }
        public string? HdcpMode { get; init; }
        public string? HdcpVersion { get; init; }
        public string? RxTxHdcpVersion { get; init; }
        public string? RawTimingHex { get; init; }
    }

    private static SourceTelemetryProjection BuildSourceTelemetryProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
    {
        var telemetryTimestampUtc = viewModelSnapshot.SourceTelemetryTimestampUtc ?? captureRuntime.SourceTelemetryTimestampUtc;

        return new()
        {
            SourceTelemetryAvailability = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryAvailability,
                captureRuntime.SourceTelemetryAvailability),
            SourceTelemetryOriginDetail = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryOriginDetail,
                captureRuntime.SourceTelemetryOriginDetail),
            SourceTelemetryConfidence = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryConfidence,
                captureRuntime.SourceTelemetryConfidence),
            SourceTelemetryDiagnosticSummary = viewModelSnapshot.SourceTelemetryDiagnosticSummary ?? captureRuntime.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = telemetryTimestampUtc,
            SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(
                viewModelSnapshot.SourceTelemetryAgeSeconds,
                telemetryTimestampUtc,
                DateTimeOffset.UtcNow),
            SourceTelemetryBackend = captureRuntime.SourceTelemetryBackend,
            SourceTelemetrySuppressed = captureRuntime.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = captureRuntime.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = captureRuntime.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = viewModelSnapshot.SourceTelemetrySummaryText,
            SourceTargetSummaryText = viewModelSnapshot.SourceTargetSummaryText
        };
    }

    private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)
        => !string.IsNullOrWhiteSpace(viewModelValue) &&
           !string.Equals(viewModelValue, "Unknown", StringComparison.OrdinalIgnoreCase)
            ? viewModelValue
            : runtimeValue;

    private readonly record struct SourceTelemetryProjection
    {
        public string SourceTelemetryAvailability { get; init; }
        public string SourceTelemetryOriginDetail { get; init; }
        public string SourceTelemetryConfidence { get; init; }
        public string? SourceTelemetryDiagnosticSummary { get; init; }
        public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; }
        public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
        public int? SourceTelemetryAgeSeconds { get; init; }
        public string SourceTelemetryBackend { get; init; }
        public bool SourceTelemetrySuppressed { get; init; }
        public string? SourceTelemetrySuppressedReason { get; init; }
        public string SourceTelemetryCircuitState { get; init; }
        public string SourceTelemetrySummaryText { get; init; }
        public string SourceTargetSummaryText { get; init; }
    }

    private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,
            Height = captureRuntime.NegotiatedHeight ?? captureRuntime.ActualHeight,
            FrameRate = captureRuntime.NegotiatedFrameRate ?? captureRuntime.ActualFrameRate,
            FrameRateArg = captureRuntime.NegotiatedFrameRateArg ?? captureRuntime.ActualFrameRateArg,
            FrameRateNumerator = captureRuntime.NegotiatedFrameRateNumerator,
            FrameRateDenominator = captureRuntime.NegotiatedFrameRateDenominator,
            PixelFormat = captureRuntime.NegotiatedPixelFormat,
            MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken
        };

    private readonly record struct CaptureFormatNegotiatedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? MediaSubtypeToken { get; init; }
    }

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

    private static PreviewHdrState BuildPreviewHdrState(
        CaptureRuntimeSnapshot captureRuntime,
        ViewModelRuntimeSnapshot viewModelSnapshot,
        PreviewRuntimeSnapshot previewRuntime)
    {
        var inputDetected =
            IsHdrSubtype(captureRuntime.NegotiatedPixelFormat) ||
            (captureRuntime.RequestedHdrEnabled ?? false) ||
            viewModelSnapshot.IsHdrEnabled;
        var toneMapMode = !inputDetected
            ? "None"
            : previewRuntime.GpuActive
                ? "Auto"
                : "Unavailable";

        return new PreviewHdrState(inputDetected, toneMapMode);
    }

    private static HdrTruthVerdict BuildHdrTruthVerdict(
        CaptureRuntimeSnapshot captureRuntime,
        bool hdrEnabledInUi,
        RecordingVerificationResult? lastVerification)
    {
        static string NormalizeFormatToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "unknown";
            }

            var value = text.Trim();
            if (value.Contains("P010", StringComparison.OrdinalIgnoreCase))
            {
                return "P010";
            }

            if (value.Contains("NV12", StringComparison.OrdinalIgnoreCase))
            {
                return "NV12";
            }

            return value.ToUpperInvariant();
        }

        var evidence = new List<string>(capacity: 8);
        var observedFormatToken = NormalizeFormatToken(
            captureRuntime.LatestObservedFramePixelFormat ??
            captureRuntime.FirstObservedFramePixelFormat);
        var hasP010 = captureRuntime.ObservedP010FrameCount > 0;
        var hasNv12 = captureRuntime.ObservedNv12FrameCount > 0;
        var hasObservedFormat = hasP010 || hasNv12 || captureRuntime.ObservedOtherFrameCount > 0;
        var pipelineFormat = hasP010
            ? "P010"
            : hasNv12
                ? "NV12"
                : hasObservedFormat ? observedFormatToken : "unknown";

        if (hasP010)
        {
            evidence.Add($"observed-p010-frames={captureRuntime.ObservedP010FrameCount}");
        }
        if (hasNv12)
        {
            evidence.Add($"observed-nv12-frames={captureRuntime.ObservedNv12FrameCount}");
        }

        string effectiveBitDepth;
        if (string.Equals(pipelineFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            effectiveBitDepth = "8bit-like";
        }
        else if (string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            if (captureRuntime.ObservedP010Likely8BitUpscaled == true)
            {
                effectiveBitDepth = "8bit-like";
                evidence.Add("p010-samples-look-upscaled-8bit=true");
            }
            else if (captureRuntime.ObservedP010BitDepthSampleCount > 0)
            {
                effectiveBitDepth = captureRuntime.ObservedP010Low2BitNonZeroPercent >= 0.50
                    ? "10bit"
                    : "8bit-like";
                evidence.Add(
                    $"p010-low2-nonzero-pct={captureRuntime.ObservedP010Low2BitNonZeroPercent:0.###} (samples={captureRuntime.ObservedP010BitDepthSampleCount})");
            }
            else
            {
                effectiveBitDepth = "unknown";
                evidence.Add("p010-bitdepth-samples=0");
            }
        }
        else
        {
            effectiveBitDepth = "unknown";
        }

        string metadataState;
        if (lastVerification is null)
        {
            metadataState = "unknown";
            evidence.Add("metadata=verification-not-run");
        }
        else if (lastVerification.HdrColorimetryValid == false)
        {
            metadataState = "invalid";
            evidence.Add("metadata=colorimetry-invalid");
        }
        else if (lastVerification.HdrMetadataPresent == true)
        {
            metadataState = "present-valid";
            evidence.Add("metadata=present-valid");
        }
        else if (lastVerification.HdrMetadataPresent == false)
        {
            metadataState = "missing";
            evidence.Add("metadata=missing");
        }
        else
        {
            metadataState = "unknown";
            evidence.Add("metadata=unknown");
        }

        var captureHdrLike =
            string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase);
        var sourceHdr = captureRuntime.SourceIsHdr;
        string sourceVsCaptureParity;
        if (!hasObservedFormat || !sourceHdr.HasValue)
        {
            sourceVsCaptureParity = "unknown";
        }
        else if (sourceHdr.Value == captureHdrLike)
        {
            sourceVsCaptureParity = "match";
        }
        else if (sourceHdr.Value && !captureHdrLike && !hdrEnabledInUi)
        {
            sourceVsCaptureParity = "expected-sdr-capture";
            evidence.Add("source-hdr=true, capture-hdr-like=false, hdr-requested=false");
        }
        else
        {
            sourceVsCaptureParity = "mismatch";
            evidence.Add($"source-hdr={sourceHdr.Value}, capture-hdr-like={captureHdrLike}");
        }

        var finalClassification = pipelineFormat switch
        {
            "NV12" => "sdr-8bit",
            "P010" when string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(metadataState, "present-valid", StringComparison.OrdinalIgnoreCase)
                => "true-hdr10",
            "P010" => "p010-sdr",
            _ => "inconclusive"
        };

        if (hdrEnabledInUi && string.Equals(finalClassification, "sdr-8bit", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add("hdr-enabled-ui-while-effective-path-is-sdr-8bit");
        }

        return new HdrTruthVerdict
        {
            PipelineFormat = pipelineFormat,
            EffectiveBitDepth = effectiveBitDepth,
            HdrMetadataState = metadataState,
            SourceVsCaptureParity = sourceVsCaptureParity,
            FinalClassification = finalClassification,
            Evidence = evidence
        };
    }

    private static HdrPipelineProjection BuildHdrPipelineProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        HdrTruthVerdict truthVerdict)
        => new()
        {
            IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,
            IsHdrEnabled = viewModelSnapshot.IsHdrEnabled,
            HdrOutputActive = captureRuntime.HdrOutputActive,
            HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),
            HdrReadinessReason = PreferViewModelHdrText(viewModelSnapshot.HdrReadinessReason, captureRuntime.HdrReadinessReason),
            HdrWarmupState = captureRuntime.HdrWarmupState,
            HdrWarmupRequiredP010Frames = captureRuntime.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = captureRuntime.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = captureRuntime.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = captureRuntime.HdrDowngradeCode,
            RequestedPipelineMode = captureRuntime.RequestedPipelineMode,
            ActivePipelineMode = captureRuntime.ActivePipelineMode,
            PipelineModeMatched = captureRuntime.PipelineModeMatched,
            PipelineModeStatus = captureRuntime.PipelineModeStatus,
            PipelineModeReason = captureRuntime.PipelineModeReason,
            TelemetryAlignmentStatus = captureRuntime.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,
            TruthVerdict = truthVerdict
        };

    private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)
        => !string.IsNullOrWhiteSpace(viewModelValue) ? viewModelValue : runtimeValue;

    private readonly record struct HdrPipelineProjection
    {
        public bool IsHdrAvailable { get; init; }
        public bool IsHdrEnabled { get; init; }
        public bool HdrOutputActive { get; init; }
        public string HdrRuntimeState { get; init; }
        public string HdrReadinessReason { get; init; }
        public string HdrWarmupState { get; init; }
        public int HdrWarmupRequiredP010Frames { get; init; }
        public int HdrWarmupAllowedNonP010Frames { get; init; }
        public int HdrWarmupObservedP010Frames { get; init; }
        public int HdrWarmupObservedNonP010Frames { get; init; }
        public string HdrDowngradeCode { get; init; }
        public string RequestedPipelineMode { get; init; }
        public string ActivePipelineMode { get; init; }
        public bool PipelineModeMatched { get; init; }
        public string PipelineModeStatus { get; init; }
        public string PipelineModeReason { get; init; }
        public string TelemetryAlignmentStatus { get; init; }
        public string TelemetryAlignmentReason { get; init; }
        public HdrTruthVerdict TruthVerdict { get; init; }
    }

    private readonly record struct PreviewHdrState(bool InputDetected, string ToneMapMode);

    private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
            DecodeSampleCount = health.MjpegDecodeSampleCount,
            DecodeAvgMs = health.MjpegDecodeAvgMs,
            DecodeP95Ms = health.MjpegDecodeP95Ms,
            DecodeMaxMs = health.MjpegDecodeMaxMs,
            InteropCopySampleCount = health.MjpegInteropCopySampleCount,
            InteropCopyAvgMs = health.MjpegInteropCopyAvgMs,
            InteropCopyP95Ms = health.MjpegInteropCopyP95Ms,
            InteropCopyMaxMs = health.MjpegInteropCopyMaxMs,
            CallbackSampleCount = health.MjpegCallbackSampleCount,
            CallbackAvgMs = health.MjpegCallbackAvgMs,
            CallbackP95Ms = health.MjpegCallbackP95Ms,
            CallbackMaxMs = health.MjpegCallbackMaxMs,
            DecoderCount = health.MjpegDecoderCount,
            ReorderSampleCount = health.MjpegReorderSampleCount,
            ReorderAvgMs = health.MjpegReorderAvgMs,
            ReorderP95Ms = health.MjpegReorderP95Ms,
            ReorderMaxMs = health.MjpegReorderMaxMs,
            PipelineSampleCount = health.MjpegPipelineSampleCount,
            PipelineAvgMs = health.MjpegPipelineAvgMs,
            PipelineP95Ms = health.MjpegPipelineP95Ms,
            PipelineMaxMs = health.MjpegPipelineMaxMs,
            PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderAutomationSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderAutomationSnapshot>()
        };

    private readonly record struct MjpegTimingProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Backend = captureRuntime.RecordingBackend,
            AudioPathMode = captureRuntime.AudioPathMode,
            MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)
        };

    private static string ResolveMuxResult(bool? muxSucceeded)
        => muxSucceeded.HasValue
            ? (muxSucceeded.Value ? "Succeeded" : "Failed")
            : "NotAttempted";

    private readonly record struct RecordingBackendProjection
    {
        public string Backend { get; init; }
        public string AudioPathMode { get; init; }
        public string MuxResult { get; init; }
    }

    private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)
        => new()
        {
            QueueSaturated = health.AudioDropsQueueSaturated,
            BacklogEviction = health.AudioDropsBacklogEviction,
            ChunksDropped = health.AudioChunksDropped,
            QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            QueueDropsFileWriter = health.AudioChunksDropped
        };

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            FramesArrived = previewRuntime.FramesArrived,
            FramesDisplayed = previewRuntime.FramesDisplayed,
            FramesDropped = previewRuntime.FramesDropped,
            EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs
        };

    private readonly record struct PreviewRuntimeFrameProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            State = previewRuntime.StartupState,
            AttemptId = previewRuntime.StartupAttemptId,
            ElapsedMs = previewRuntime.StartupElapsedMs,
            TimeoutMs = previewRuntime.StartupTimeoutMs,
            GpuSignalMediaOpened = previewRuntime.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame = previewRuntime.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = previewRuntime.StartupGpuSignalPlaybackAdvancing,
            RequiredSignals = previewRuntime.StartupRequiredSignals,
            ReceivedSignals = previewRuntime.StartupReceivedSignals,
            Strategy = previewRuntime.StartupStrategy.ToString(),
            MissingSignals = previewRuntime.StartupMissingSignals,
            RecoveryAttemptCount = previewRuntime.StartupRecoveryAttemptCount,
            LastFailureReason = previewRuntime.StartupLastFailureReason,
            FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,
            BlankSuspected = previewRuntime.BlankSuspected,
            Stalled = previewRuntime.StallSuspected,
            RendererMode = previewRuntime.RendererMode
        };

    private readonly record struct PreviewRuntimeStartupProjection
    {
        public string State { get; init; }
        public string? AttemptId { get; init; }
        public double? ElapsedMs { get; init; }
        public int TimeoutMs { get; init; }
        public bool GpuSignalMediaOpened { get; init; }
        public bool GpuSignalFirstFrame { get; init; }
        public bool GpuSignalPlaybackAdvancing { get; init; }
        public PreviewStartupSignalFlags RequiredSignals { get; init; }
        public PreviewStartupSignalFlags ReceivedSignals { get; init; }
        public string Strategy { get; init; }
        public string? MissingSignals { get; init; }
        public int RecoveryAttemptCount { get; init; }
        public string? LastFailureReason { get; init; }
        public bool FirstVisualConfirmed { get; init; }
        public bool BlankSuspected { get; init; }
        public bool Stalled { get; init; }
        public string RendererMode { get; init; }
    }
}
