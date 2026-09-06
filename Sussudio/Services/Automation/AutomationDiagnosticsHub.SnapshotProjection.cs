using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using static Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder;
using FlashbackExportLastResultProjection = Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder.FlashbackExportLastResultProjection;
using FlashbackExportProjection = Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder.FlashbackExportProjection;
using FlashbackPlaybackProjection = Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder.FlashbackPlaybackProjection;
using FlashbackRecordingProjection = Sussudio.Services.Automation.AutomationSnapshotFlashbackProjectionBuilder.FlashbackRecordingProjection;

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
        var projections = BuildAutomationSnapshotProjectionSet(
            viewModelSnapshot,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            snapshotCollection,
            performance,
            diagnostic,
            previewPacingClassification,
            previewHdrState,
            audioSignal,
            recordingFileGrowing,
            hdrTruthVerdict,
            lastOutput,
            processResources,
            lastVerification,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return BuildAutomationSnapshotFromProjections(projections);
    }

    private AutomationSnapshotProjectionSet BuildAutomationSnapshotProjectionSet(
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
        var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);
        var audioDrops = BuildAudioDropsProjection(health);
        var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);
        var userSettings = BuildUserSettingsProjection(viewModelSnapshot);
        var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);
        var captureFormat = BuildCaptureFormatProjection(captureRuntime);
        var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);
        var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);
        var recordingOutput = BuildRecordingOutputProjection(
            viewModelSnapshot,
            captureRuntime,
            recordingStats,
            recordingFileGrowing,
            lastOutput,
            lastVerification);
        var processResourceProjection = BuildProcessResourceProjection(processResources);
        var avSync = BuildAvSyncProjection(captureRuntime);
        var captureTransport = BuildCaptureTransportProjection(captureRuntime);
        var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);
        var recordingBackend = BuildRecordingBackendProjection(captureRuntime);
        var recordingPipeline = BuildRecordingPipelineProjection(health);
        var captureCadence = BuildCaptureCadenceProjection(health);
        var visualCadence = BuildVisualCadenceProjection(health);
        var mjpeg = BuildMjpegProjection(health);
        var previewD3D = BuildPreviewD3DProjection(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);
        var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);
        var flashbackExport = BuildFlashbackExportProjection(health);
        var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);
        var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);
        var flashbackPlayback = BuildFlashbackPlaybackProjection(health);

        return new AutomationSnapshotProjectionSet(
            snapshotStatus,
            snapshotEvaluation,
            audioAndIngest,
            audioDrops,
            captureCommands,
            userSettings,
            recordingIntegrity,
            captureFormat,
            sourceSignal,
            sourceTelemetry,
            recordingOutput,
            processResourceProjection,
            avSync,
            captureTransport,
            previewSummary,
            recordingBackend,
            recordingPipeline,
            captureCadence,
            visualCadence,
            mjpeg,
            previewD3D,
            hdrPipeline,
            flashbackExport,
            flashbackExportLastResult,
            flashbackRecording,
            flashbackPlayback);
    }

    private readonly record struct AutomationSnapshotProjectionSet(
        SnapshotStatusProjection SnapshotStatus,
        SnapshotEvaluationProjection SnapshotEvaluation,
        AudioAndIngestProjection AudioAndIngest,
        AudioDropsProjection AudioDrops,
        CaptureCommandProjection CaptureCommands,
        UserSettingsProjection UserSettings,
        RecordingIntegrityProjection RecordingIntegrity,
        CaptureFormatProjection CaptureFormat,
        SourceSignalProjection SourceSignal,
        SourceTelemetryProjection SourceTelemetry,
        RecordingOutputProjection RecordingOutput,
        ProcessResourceProjection ProcessResourceProjection,
        AvSyncProjection AvSync,
        CaptureTransportProjection CaptureTransport,
        PreviewRuntimeProjection PreviewSummary,
        RecordingBackendProjection RecordingBackend,
        RecordingPipelineProjection RecordingPipeline,
        CaptureCadenceProjection CaptureCadence,
        VisualCadenceProjection VisualCadence,
        MjpegProjection Mjpeg,
        PreviewD3DProjection PreviewD3D,
        HdrPipelineProjection HdrPipeline,
        FlashbackExportProjection FlashbackExport,
        FlashbackExportLastResultProjection FlashbackExportLastResult,
        FlashbackRecordingProjection FlashbackRecording,
        FlashbackPlaybackProjection FlashbackPlayback);

    private static AutomationSnapshot BuildAutomationSnapshotFromProjections(
        AutomationSnapshotProjectionSet projections)
    {
        var snapshotStatus = projections.SnapshotStatus;
        var snapshotEvaluation = projections.SnapshotEvaluation;
        var captureCommands = projections.CaptureCommands;
        var userSettings = projections.UserSettings;
        var sourceSignal = projections.SourceSignal;
        var sourceTelemetry = projections.SourceTelemetry;
        var hdrPipeline = projections.HdrPipeline;
        var recordingOutput = projections.RecordingOutput;
        var audioAndIngest = projections.AudioAndIngest;
        var captureTransport = projections.CaptureTransport;
        var previewSummary = projections.PreviewSummary;
        var recordingPipeline = projections.RecordingPipeline;
        var recordingBackend = projections.RecordingBackend;
        var recordingIntegrity = projections.RecordingIntegrity;
        var captureFormat = projections.CaptureFormat;
        var previewD3D = projections.PreviewD3D;
        var flashbackRecording = projections.FlashbackRecording;
        var audioDrops = projections.AudioDrops;
        var captureCadence = projections.CaptureCadence;
        var mjpeg = projections.Mjpeg;
        var visualCadence = projections.VisualCadence;
        var processResourceProjection = projections.ProcessResourceProjection;
        var avSync = projections.AvSync;
        var flashbackPlayback = projections.FlashbackPlayback;
        var flashbackExport = projections.FlashbackExport;
        var flashbackExportLastResult = projections.FlashbackExportLastResult;

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
            CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,
            CaptureCommandCommandsCompleted = captureCommands.CommandsCompleted,
            CaptureCommandCommandsFailed = captureCommands.CommandsFailed,
            CaptureCommandCommandsCanceled = captureCommands.CommandsCanceled,
            CaptureCommandCommandsCoalesced = captureCommands.CommandsCoalesced,
            CaptureCommandPendingCommands = captureCommands.PendingCommands,
            CaptureCommandMaxPendingCommands = captureCommands.MaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = captureCommands.OldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = captureCommands.LastQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,
            CaptureCommandLastCommand = captureCommands.LastCommand,
            CaptureCommandLastOutcome = captureCommands.LastOutcome,
            CaptureCommandLastCorrelationId = captureCommands.LastCorrelationId,
            CaptureCommandLastError = captureCommands.LastError,
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
            OutputPath = recordingOutput.OutputPath,
            RecordingTime = recordingOutput.RecordingTime,
            RecordingSizeInfo = recordingOutput.RecordingSizeInfo,
            RecordingBitrateInfo = recordingOutput.RecordingBitrateInfo,
            AudioPeak = audioAndIngest.Signal.Peak,
            AudioClipping = audioAndIngest.Signal.Clipping,
            AudioSignalPresent = audioAndIngest.Signal.SignalPresent,
            AudioMutedSuspected = audioAndIngest.Signal.MutedSuspected,
            AudioReaderActive = audioAndIngest.Ingest.AudioReaderActive,
            AudioFramesArrived = audioAndIngest.Ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = audioAndIngest.Ingest.AudioFramesWrittenToSink,
            VideoReaderActive = audioAndIngest.Ingest.VideoReaderActive,
            IngestVideoFramesArrived = audioAndIngest.Ingest.VideoFramesArrived,
            IngestVideoFramesWrittenToSink = audioAndIngest.Ingest.VideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = audioAndIngest.Ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = audioAndIngest.Ingest.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = audioAndIngest.Ingest.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = audioAndIngest.Ingest.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = audioAndIngest.Ingest.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = audioAndIngest.Ingest.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = audioAndIngest.Ingest.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = audioAndIngest.Ingest.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = audioAndIngest.Ingest.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = audioAndIngest.Wasapi.CaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = audioAndIngest.Wasapi.CaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = audioAndIngest.Wasapi.CaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = audioAndIngest.Wasapi.CaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = audioAndIngest.Wasapi.CaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = audioAndIngest.Wasapi.CaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = audioAndIngest.Wasapi.CaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = audioAndIngest.Wasapi.CaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = audioAndIngest.Wasapi.CaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = audioAndIngest.Wasapi.CaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = audioAndIngest.Wasapi.CaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = audioAndIngest.Wasapi.PlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = audioAndIngest.Wasapi.PlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = audioAndIngest.Wasapi.PlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = audioAndIngest.Wasapi.PlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = audioAndIngest.Wasapi.PlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = audioAndIngest.Wasapi.PlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = audioAndIngest.Wasapi.PlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = audioAndIngest.Wasapi.PlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = audioAndIngest.Wasapi.PlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = audioAndIngest.Wasapi.PlaybackLastRenderTickMs,
            AudioBufferHealthStatus = audioAndIngest.Wasapi.BufferHealthStatus,
            AudioBufferHealthReason = audioAndIngest.Wasapi.BufferHealthReason,
            AudioBufferUnderrunDetected = audioAndIngest.Wasapi.BufferUnderrunDetected,
            AudioBufferOverrunDetected = audioAndIngest.Wasapi.BufferOverrunDetected,
            AudioBufferUnderrunEvents = audioAndIngest.Wasapi.BufferUnderrunEvents,
            AudioBufferOverrunEvents = audioAndIngest.Wasapi.BufferOverrunEvents,
            MemoryPreference = captureTransport.MemoryPreference,
            VideoRequestedSubtype = captureTransport.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureTransport.FrameLedgerCapacity,
            FrameLedgerEventCount = captureTransport.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureTransport.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,
            PreviewAdapterColorMetadata = previewSummary.Color.AdapterColorMetadata,
            EncoderVideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,
            EncoderVideoFramesEncoded = recordingPipeline.Encoder.VideoFramesEncoded,
            EncoderLastEnqueueAgeMs = recordingPipeline.Encoder.LastEnqueueAgeMs,
            EncoderLastWriteAgeMs = recordingPipeline.Encoder.LastWriteAgeMs,
            RecordingBackend = recordingBackend.Backend,
            AudioPathMode = recordingBackend.AudioPathMode,
            MuxResult = recordingBackend.MuxResult,
            RecordingIntegrityStatus = recordingIntegrity.Summary.Status,
            RecordingIntegrityComplete = recordingIntegrity.Summary.Complete,
            RecordingIntegrityBackend = recordingIntegrity.Summary.Backend,
            RecordingIntegrityCompletedUtc = recordingIntegrity.Summary.CompletedUtc,
            RecordingIntegritySourceFrames = recordingIntegrity.Video.SourceFrames,
            RecordingIntegrityAcceptedFrames = recordingIntegrity.Video.AcceptedFrames,
            RecordingIntegrityPipelineDroppedFrames = recordingIntegrity.Video.PipelineDroppedFrames,
            RecordingIntegrityQueueDroppedFrames = recordingIntegrity.Video.QueueDroppedFrames,
            RecordingIntegritySubmittedFrames = recordingIntegrity.Video.SubmittedFrames,
            RecordingIntegrityEncodedFrames = recordingIntegrity.Video.EncodedFrames,
            RecordingIntegrityPacketsWritten = recordingIntegrity.Video.PacketsWritten,
            RecordingIntegrityEncoderDroppedFrames = recordingIntegrity.Video.EncoderDroppedFrames,
            RecordingIntegritySequenceGaps = recordingIntegrity.Video.SequenceGaps,
            RecordingIntegrityQueueMaxDepth = recordingIntegrity.Backpressure.QueueMaxDepth,
            RecordingIntegrityQueueOldestFrameAgeMs = recordingIntegrity.Backpressure.QueueOldestFrameAgeMs,
            RecordingIntegrityBackpressureWaitMs = recordingIntegrity.Backpressure.BackpressureWaitMs,
            RecordingIntegrityBackpressureEvents = recordingIntegrity.Backpressure.BackpressureEvents,
            RecordingIntegrityBackpressureMaxWaitMs = recordingIntegrity.Backpressure.BackpressureMaxWaitMs,
            RecordingIntegrityAudioStatus = recordingIntegrity.Audio.AudioStatus,
            RecordingIntegrityAudioEnabled = recordingIntegrity.Audio.AudioEnabled,
            RecordingIntegrityAudioCaptureActive = recordingIntegrity.Audio.AudioCaptureActive,
            RecordingIntegrityAudioFramesArrived = recordingIntegrity.Audio.AudioFramesArrived,
            RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.Audio.AudioFramesWrittenToSink,
            RecordingIntegrityAudioSamplesEncoded = recordingIntegrity.Audio.AudioSamplesEncoded,
            RecordingIntegrityAudioDropEvents = recordingIntegrity.Audio.AudioDropEvents,
            RecordingIntegrityAudioDiscontinuities = recordingIntegrity.Audio.AudioDiscontinuities,
            RecordingIntegrityAudioTimestampErrors = recordingIntegrity.Audio.AudioTimestampErrors,
            RecordingIntegrityAudioCallbackGaps = recordingIntegrity.Audio.AudioCallbackGaps,
            RecordingIntegrityAvSyncDriftMs = recordingIntegrity.AvSync.AvSyncDriftMs,
            RecordingIntegrityAvSyncDriftRateMsPerSec = recordingIntegrity.AvSync.AvSyncDriftRateMsPerSec,
            RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.AvSync.EncoderAvSyncDriftMs,
            RecordingIntegrityEncoderAvSyncCorrectionSamples = recordingIntegrity.AvSync.EncoderAvSyncCorrectionSamples,
            RecordingIntegrityReason = recordingIntegrity.Summary.Reason,
            RequestedWidth = captureFormat.Requested.Width,
            RequestedHeight = captureFormat.Requested.Height,
            RequestedFrameRate = captureFormat.Requested.FrameRate,
            RequestedFrameRateArg = captureFormat.Requested.FrameRateArg,
            RequestedFrameRateNumerator = captureFormat.Requested.FrameRateNumerator,
            RequestedFrameRateDenominator = captureFormat.Requested.FrameRateDenominator,
            RequestedPixelFormat = captureFormat.Requested.PixelFormat,
            RequestedFormat = captureFormat.Requested.Format,
            RequestedQuality = captureFormat.Requested.Quality,
            RequestedHdrEnabled = captureFormat.Requested.HdrEnabled,
            RequestedHdrMasteringMetadata = captureFormat.Requested.HdrMasteringMetadata,
            RequestedAudioEnabled = captureFormat.Requested.AudioEnabled,
            HdrActivationReason = captureFormat.HdrRequest.ActivationReason,
            HdrAutoDowngraded = captureFormat.HdrRequest.AutoDowngraded,
            HdrAutoDowngradeReason = captureFormat.HdrRequest.AutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureFormat.HdrRequest.RequestedButSourceNot10Bit,
            ActualWidth = captureFormat.Actual.Width,
            ActualHeight = captureFormat.Actual.Height,
            ActualFrameRate = captureFormat.Actual.FrameRate,
            ActualFrameRateArg = captureFormat.Actual.FrameRateArg,
            NegotiatedWidth = captureFormat.Negotiated.Width,
            NegotiatedHeight = captureFormat.Negotiated.Height,
            NegotiatedFrameRate = captureFormat.Negotiated.FrameRate,
            NegotiatedFrameRateArg = captureFormat.Negotiated.FrameRateArg,
            NegotiatedFrameRateNumerator = captureFormat.Negotiated.FrameRateNumerator,
            NegotiatedFrameRateDenominator = captureFormat.Negotiated.FrameRateDenominator,
            NegotiatedPixelFormat = captureFormat.Negotiated.PixelFormat,
            RequestedReaderSubtype = captureFormat.ReaderObservation.RequestedReaderSubtype,
            ReaderSourceStreamType = captureFormat.ReaderObservation.ReaderSourceStreamType,
            ReaderSourceSubtype = captureFormat.ReaderObservation.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureFormat.ReaderObservation.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureFormat.ReaderObservation.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureFormat.ReaderObservation.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureFormat.ReaderObservation.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureFormat.ReaderObservation.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureFormat.ReaderObservation.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureFormat.ReaderObservation.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureFormat.ReaderObservation.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureFormat.ReaderObservation.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureFormat.Encoder.InputPixelFormat,
            EncoderOutputPixelFormat = captureFormat.Encoder.OutputPixelFormat,
            EncoderVideoCodec = captureFormat.Encoder.VideoCodec,
            EncoderVideoProfile = captureFormat.Encoder.VideoProfile,
            EncoderTenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken,
            PreviewFramesArrived = previewSummary.Frame.FramesArrived,
            PreviewFramesDisplayed = previewSummary.Frame.FramesDisplayed,
            PreviewFramesDropped = previewSummary.Frame.FramesDropped,
            PreviewCadenceSampleCount = previewSummary.Cadence.SampleCount,
            PreviewCadenceObservedFps = previewSummary.Cadence.ObservedFps,
            PreviewCadenceExpectedIntervalMs = previewSummary.Cadence.ExpectedIntervalMs,
            PreviewCadenceAverageIntervalMs = previewSummary.Cadence.AverageIntervalMs,
            PreviewCadenceP95IntervalMs = previewSummary.Cadence.P95IntervalMs,
            PreviewCadenceP99IntervalMs = previewSummary.Cadence.P99IntervalMs,
            PreviewCadenceMaxIntervalMs = previewSummary.Cadence.MaxIntervalMs,
            PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,
            PreviewCadenceFivePercentLowFps = previewSummary.Cadence.FivePercentLowFps,
            PreviewCadenceSampleDurationMs = previewSummary.Cadence.SampleDurationMs,
            PreviewCadenceRecentIntervalsMs = previewSummary.Cadence.RecentIntervalsMs,
            PreviewCadenceJitterStdDevMs = previewSummary.Cadence.JitterStdDevMs,
            PreviewCadenceSlowFrameCount = previewSummary.Cadence.SlowFrameCount,
            PreviewCadenceSlowFramePercent = previewSummary.Cadence.SlowFramePercent,
            PreviewGpuActive = previewSummary.Surface.GpuActive,
            PreviewPlaceholderVisible = previewSummary.Surface.PlaceholderVisible,
            PreviewGpuElementVisible = previewSummary.Surface.GpuElementVisible,
            PreviewCpuElementVisible = previewSummary.Surface.CpuElementVisible,
            PreviewRendererAttached = previewSummary.Surface.RendererAttached,
            PreviewStartupState = previewSummary.Startup.State,
            PreviewAttemptId = previewSummary.Startup.AttemptId,
            PreviewStartupElapsedMs = previewSummary.Startup.ElapsedMs,
            PreviewStartupTimeoutMs = previewSummary.Startup.TimeoutMs,
            PreviewGpuSignalMediaOpened = previewSummary.Startup.GpuSignalMediaOpened,
            PreviewGpuSignalFirstFrame = previewSummary.Startup.GpuSignalFirstFrame,
            PreviewGpuSignalPlaybackAdvancing = previewSummary.Startup.GpuSignalPlaybackAdvancing,
            PreviewStartupRequiredSignals = previewSummary.Startup.RequiredSignals,
            PreviewStartupReceivedSignals = previewSummary.Startup.ReceivedSignals,
            PreviewStartupStrategy = previewSummary.Startup.Strategy,
            PreviewStartupMissingSignals = previewSummary.Startup.MissingSignals,
            PreviewRecoveryAttemptCount = previewSummary.Startup.RecoveryAttemptCount,
            PreviewLastFailureReason = previewSummary.Startup.LastFailureReason,
            PreviewFirstVisualConfirmed = previewSummary.Startup.FirstVisualConfirmed,
            PreviewBlankSuspected = previewSummary.Startup.BlankSuspected,
            PreviewStalled = previewSummary.Startup.Stalled,
            PreviewRendererMode = previewSummary.Startup.RendererMode,
            PreviewD3DPresentSyncInterval = previewD3D.PresentSyncInterval,
            PreviewD3DMaxFrameLatency = previewD3D.MaxFrameLatency,
            PreviewD3DSwapChainBufferCount = previewD3D.SwapChainBufferCount,
            PreviewD3DSwapChainAddress = previewD3D.SwapChainAddress,
            PreviewD3DFramesSubmitted = previewD3D.FramesSubmitted,
            PreviewD3DFramesRendered = previewD3D.FramesRendered,
            PreviewD3DFramesDropped = previewD3D.FramesDropped,
            PreviewD3DRenderThreadFailureCount = previewD3D.RenderThreadFailureCount,
            PreviewD3DLastRenderThreadFailureType = previewD3D.LastRenderThreadFailureType,
            PreviewD3DLastRenderThreadFailureMessage = previewD3D.LastRenderThreadFailureMessage,
            PreviewD3DLastRenderThreadFailureHResult = previewD3D.LastRenderThreadFailureHResult,
            PreviewD3DPendingFrameCount = previewD3D.PendingFrameCount,
            PreviewD3DInputColorSpace = previewD3D.InputColorSpace,
            PreviewD3DOutputColorSpace = previewD3D.OutputColorSpace,
            PreviewD3DCpuTimingSampleCount = previewD3D.CpuTiming.SampleCount,
            PreviewD3DInputUploadCpuAvgMs = previewD3D.CpuTiming.InputUploadAvgMs,
            PreviewD3DInputUploadCpuP95Ms = previewD3D.CpuTiming.InputUploadP95Ms,
            PreviewD3DInputUploadCpuP99Ms = previewD3D.CpuTiming.InputUploadP99Ms,
            PreviewD3DInputUploadCpuMaxMs = previewD3D.CpuTiming.InputUploadMaxMs,
            PreviewD3DRenderSubmitCpuAvgMs = previewD3D.CpuTiming.RenderSubmitAvgMs,
            PreviewD3DRenderSubmitCpuP95Ms = previewD3D.CpuTiming.RenderSubmitP95Ms,
            PreviewD3DRenderSubmitCpuP99Ms = previewD3D.CpuTiming.RenderSubmitP99Ms,
            PreviewD3DRenderSubmitCpuMaxMs = previewD3D.CpuTiming.RenderSubmitMaxMs,
            PreviewD3DPresentCallAvgMs = previewD3D.CpuTiming.PresentCallAvgMs,
            PreviewD3DPresentCallP95Ms = previewD3D.CpuTiming.PresentCallP95Ms,
            PreviewD3DPresentCallP99Ms = previewD3D.CpuTiming.PresentCallP99Ms,
            PreviewD3DPresentCallMaxMs = previewD3D.CpuTiming.PresentCallMaxMs,
            PreviewD3DTotalFrameCpuAvgMs = previewD3D.CpuTiming.TotalFrameAvgMs,
            PreviewD3DTotalFrameCpuP95Ms = previewD3D.CpuTiming.TotalFrameP95Ms,
            PreviewD3DTotalFrameCpuP99Ms = previewD3D.CpuTiming.TotalFrameP99Ms,
            PreviewD3DTotalFrameCpuMaxMs = previewD3D.CpuTiming.TotalFrameMaxMs,
            PreviewD3DPipelineLatencySampleCount = previewD3D.PipelineLatency.SampleCount,
            PreviewD3DPipelineLatencyAvgMs = previewD3D.PipelineLatency.AvgMs,
            PreviewD3DPipelineLatencyP95Ms = previewD3D.PipelineLatency.P95Ms,
            PreviewD3DPipelineLatencyP99Ms = previewD3D.PipelineLatency.P99Ms,
            PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,
            PreviewD3DFrameLatencyWaitEnabled = previewD3D.FrameLatencyWait.Enabled,
            PreviewD3DFrameLatencyWaitHandleActive = previewD3D.FrameLatencyWait.HandleActive,
            PreviewD3DFrameLatencyWaitCallCount = previewD3D.FrameLatencyWait.CallCount,
            PreviewD3DFrameLatencyWaitSignaledCount = previewD3D.FrameLatencyWait.SignaledCount,
            PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,
            PreviewD3DFrameLatencyWaitUnexpectedResultCount = previewD3D.FrameLatencyWait.UnexpectedResultCount,
            PreviewD3DFrameLatencyWaitLastResult = previewD3D.FrameLatencyWait.LastResult,
            PreviewD3DFrameLatencyWaitLastMs = previewD3D.FrameLatencyWait.LastMs,
            PreviewD3DFrameLatencyWaitSampleCount = previewD3D.FrameLatencyWait.SampleCount,
            PreviewD3DFrameLatencyWaitAvgMs = previewD3D.FrameLatencyWait.AvgMs,
            PreviewD3DFrameLatencyWaitP95Ms = previewD3D.FrameLatencyWait.P95Ms,
            PreviewD3DFrameLatencyWaitP99Ms = previewD3D.FrameLatencyWait.P99Ms,
            PreviewD3DFrameLatencyWaitMaxMs = previewD3D.FrameLatencyWait.MaxMs,
            PreviewD3DFrameStatsSampleCount = previewD3D.FrameStats.SampleCount,
            PreviewD3DFrameStatsSuccessCount = previewD3D.FrameStats.SuccessCount,
            PreviewD3DFrameStatsFailureCount = previewD3D.FrameStats.FailureCount,
            PreviewD3DFrameStatsLastError = previewD3D.FrameStats.LastError,
            PreviewD3DFrameStatsPresentCount = previewD3D.FrameStats.PresentCount,
            PreviewD3DFrameStatsPresentRefreshCount = previewD3D.FrameStats.PresentRefreshCount,
            PreviewD3DFrameStatsSyncRefreshCount = previewD3D.FrameStats.SyncRefreshCount,
            PreviewD3DFrameStatsSyncQpcTime = previewD3D.FrameStats.SyncQpcTime,
            PreviewD3DFrameStatsLastPresentDelta = previewD3D.FrameStats.LastPresentDelta,
            PreviewD3DFrameStatsLastPresentRefreshDelta = previewD3D.FrameStats.LastPresentRefreshDelta,
            PreviewD3DFrameStatsLastSyncRefreshDelta = previewD3D.FrameStats.LastSyncRefreshDelta,
            PreviewD3DFrameStatsMissedRefreshCount = previewD3D.FrameStats.MissedRefreshCount,
            PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,
            PreviewD3DFrameStatsRecentFailureCount = previewD3D.FrameStats.RecentFailureCount,
            PreviewD3DLastSubmittedPreviewPresentId = previewD3D.FrameFlow.LastSubmittedPreviewPresentId,
            PreviewD3DLastSubmittedSourceSequenceNumber = previewD3D.FrameFlow.LastSubmittedSourceSequenceNumber,
            PreviewD3DLastSubmittedSourcePtsTicks = previewD3D.FrameFlow.LastSubmittedSourcePtsTicks,
            PreviewD3DLastSubmittedQpc = previewD3D.FrameFlow.LastSubmittedQpc,
            PreviewD3DLastSubmittedUtcUnixMs = previewD3D.FrameFlow.LastSubmittedUtcUnixMs,
            PreviewD3DLastRenderedPreviewPresentId = previewD3D.FrameFlow.LastRenderedPreviewPresentId,
            PreviewD3DLastRenderedSourceSequenceNumber = previewD3D.FrameFlow.LastRenderedSourceSequenceNumber,
            PreviewD3DLastRenderedSourcePtsTicks = previewD3D.FrameFlow.LastRenderedSourcePtsTicks,
            PreviewD3DLastRenderedQpc = previewD3D.FrameFlow.LastRenderedQpc,
            PreviewD3DLastRenderedUtcUnixMs = previewD3D.FrameFlow.LastRenderedUtcUnixMs,
            PreviewD3DLastRenderedSchedulerToPresentMs = previewD3D.FrameFlow.LastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,
            PreviewD3DLastDroppedPreviewPresentId = previewD3D.FrameFlow.LastDroppedPreviewPresentId,
            PreviewD3DLastDroppedSourceSequenceNumber = previewD3D.FrameFlow.LastDroppedSourceSequenceNumber,
            PreviewD3DLastDroppedSourcePtsTicks = previewD3D.FrameFlow.LastDroppedSourcePtsTicks,
            PreviewD3DLastDroppedQpc = previewD3D.FrameFlow.LastDroppedQpc,
            PreviewD3DLastDroppedUtcUnixMs = previewD3D.FrameFlow.LastDroppedUtcUnixMs,
            PreviewD3DLastDropReason = previewD3D.FrameFlow.LastDropReason,
            PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,
            PreviewGpuPlaybackState = previewSummary.GpuPlayback.PlaybackState,
            PreviewGpuNaturalVideoWidth = previewSummary.GpuPlayback.NaturalVideoWidth,
            PreviewGpuNaturalVideoHeight = previewSummary.GpuPlayback.NaturalVideoHeight,
            PreviewGpuPositionMs = previewSummary.GpuPlayback.PositionMs,
            PreviewGpuPositionEventCount = previewSummary.GpuPlayback.PositionEventCount,
            PreviewHdrInputDetected = previewSummary.Color.HdrInputDetected,
            PreviewToneMapMode = previewSummary.Color.ToneMapMode,
            PreviewColorContext = previewSummary.Color.ColorContext,
            ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,
            FfmpegVideoQueueDepth = recordingPipeline.Ingest.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = recordingPipeline.Ingest.FfmpegAudioQueueDepth,
            VideoFramesArrived = recordingPipeline.Ingest.VideoFramesArrived,
            VideoFramesQueued = recordingPipeline.Ingest.VideoFramesQueued,
            VideoFramesDropped = recordingPipeline.Ingest.VideoFramesDropped,
            VideoFramesDroppedBacklog = recordingPipeline.Ingest.VideoFramesDroppedBacklog,
            VideoFramesConverted = recordingPipeline.Ingest.VideoFramesConverted,
            VideoFramesEnqueued = recordingPipeline.Ingest.VideoFramesEnqueued,
            VideoDropsQueueSaturated = recordingPipeline.Ingest.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction,
            RecordingEncodingFailed = recordingPipeline.Encoder.EncodingFailed,
            RecordingEncodingFailureType = recordingPipeline.Encoder.EncodingFailureType,
            RecordingEncodingFailureMessage = recordingPipeline.Encoder.EncodingFailureMessage,
            RecordingVideoQueueCapacity = recordingPipeline.VideoQueue.Capacity,
            RecordingVideoQueueMaxDepth = recordingPipeline.VideoQueue.MaxDepth,
            RecordingVideoFramesSubmittedToEncoder = recordingPipeline.VideoQueue.FramesSubmittedToEncoder,
            RecordingVideoEncoderPts = recordingPipeline.VideoQueue.EncoderPts,
            RecordingVideoEncoderPacketsWritten = recordingPipeline.VideoQueue.EncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = recordingPipeline.VideoQueue.EncoderDroppedFrames,
            RecordingVideoSequenceGaps = recordingPipeline.VideoQueue.SequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = recordingPipeline.VideoQueue.OldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = recordingPipeline.VideoQueue.LastLatencyMs,
            RecordingVideoQueueLatencySampleCount = recordingPipeline.VideoQueue.LatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = recordingPipeline.VideoQueue.LatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = recordingPipeline.VideoQueue.LatencyP95Ms,
            RecordingVideoQueueLatencyP99Ms = recordingPipeline.VideoQueue.LatencyP99Ms,
            RecordingVideoQueueLatencyMaxMs = recordingPipeline.VideoQueue.LatencyMaxMs,
            RecordingVideoBackpressureWaitMs = recordingPipeline.VideoQueue.BackpressureWaitMs,
            RecordingVideoBackpressureEvents = recordingPipeline.VideoQueue.BackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = recordingPipeline.VideoQueue.BackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs,
            RecordingGpuQueueDepth = recordingPipeline.HardwareQueues.GpuQueueDepth,
            RecordingGpuQueueCapacity = recordingPipeline.HardwareQueues.GpuQueueCapacity,
            RecordingGpuQueueMaxDepth = recordingPipeline.HardwareQueues.GpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,
            RecordingGpuFramesDropped = recordingPipeline.HardwareQueues.GpuFramesDropped,
            RecordingCudaQueueDepth = recordingPipeline.HardwareQueues.CudaQueueDepth,
            RecordingCudaQueueCapacity = recordingPipeline.HardwareQueues.CudaQueueCapacity,
            RecordingCudaQueueMaxDepth = recordingPipeline.HardwareQueues.CudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = recordingPipeline.HardwareQueues.CudaFramesEnqueued,
            RecordingCudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped,
            FlashbackEncodingFailed = flashbackRecording.EncodingFailed,
            FlashbackEncodingFailureType = flashbackRecording.EncodingFailureType,
            FlashbackEncodingFailureMessage = flashbackRecording.EncodingFailureMessage,
            FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress,
            FlashbackCleanupInProgress = flashbackRecording.CleanupInProgress,
            FlashbackForceRotateActive = flashbackRecording.ForceRotateActive,
            FlashbackForceRotateRequested = flashbackRecording.ForceRotateRequested,
            FlashbackForceRotateDraining = flashbackRecording.ForceRotateDraining,
            FlashbackTempDriveFreeBytes = flashbackRecording.StartupCache.TempDriveFreeBytes,
            FlashbackStartupCacheBudgetBytes = flashbackRecording.StartupCache.BudgetBytes,
            FlashbackStartupCacheBytes = flashbackRecording.StartupCache.Bytes,
            FlashbackStartupCacheSessionCount = flashbackRecording.StartupCache.SessionCount,
            FlashbackStartupCacheDeletedSessionCount = flashbackRecording.StartupCache.DeletedSessionCount,
            FlashbackStartupCacheFreedBytes = flashbackRecording.StartupCache.FreedBytes,
            FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,
            FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,
            FlashbackVideoQueueMaxDepth = flashbackRecording.Queues.VideoQueueMaxDepth,
            FlashbackVideoFramesSubmittedToEncoder = flashbackRecording.Queues.VideoFramesSubmittedToEncoder,
            FlashbackVideoEncoderPts = flashbackRecording.Queues.VideoEncoderPts,
            FlashbackVideoEncoderPacketsWritten = flashbackRecording.Queues.VideoEncoderPacketsWritten,
            FlashbackVideoEncoderDroppedFrames = flashbackRecording.Queues.VideoEncoderDroppedFrames,
            FlashbackVideoSequenceGaps = flashbackRecording.Queues.VideoSequenceGaps,
            FlashbackVideoQueueRejectedFrames = flashbackRecording.Queues.VideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = flashbackRecording.Queues.VideoQueueLastRejectReason,
            FlashbackVideoQueueOldestFrameAgeMs = flashbackRecording.Queues.VideoQueueOldestFrameAgeMs,
            FlashbackVideoQueueLastLatencyMs = flashbackRecording.Queues.VideoQueueLastLatencyMs,
            FlashbackVideoQueueLatencySampleCount = flashbackRecording.Queues.VideoQueueLatencySampleCount,
            FlashbackVideoQueueLatencyAvgMs = flashbackRecording.Queues.VideoQueueLatencyAvgMs,
            FlashbackVideoQueueLatencyP95Ms = flashbackRecording.Queues.VideoQueueLatencyP95Ms,
            FlashbackVideoQueueLatencyP99Ms = flashbackRecording.Queues.VideoQueueLatencyP99Ms,
            FlashbackVideoQueueLatencyMaxMs = flashbackRecording.Queues.VideoQueueLatencyMaxMs,
            FlashbackVideoBackpressureWaitMs = flashbackRecording.Queues.VideoBackpressureWaitMs,
            FlashbackVideoBackpressureEvents = flashbackRecording.Queues.VideoBackpressureEvents,
            FlashbackVideoBackpressureLastWaitMs = flashbackRecording.Queues.VideoBackpressureLastWaitMs,
            FlashbackVideoBackpressureMaxWaitMs = flashbackRecording.Queues.VideoBackpressureMaxWaitMs,
            FlashbackGpuQueueDepth = flashbackRecording.Queues.GpuQueueDepth,
            FlashbackGpuQueueCapacity = flashbackRecording.Queues.GpuQueueCapacity,
            FlashbackGpuQueueMaxDepth = flashbackRecording.Queues.GpuQueueMaxDepth,
            FlashbackGpuFramesEnqueued = flashbackRecording.Queues.GpuFramesEnqueued,
            FlashbackGpuFramesDropped = flashbackRecording.Queues.GpuFramesDropped,
            FlashbackGpuQueueRejectedFrames = flashbackRecording.Queues.GpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,
            AudioDropsQueueSaturated = audioDrops.QueueSaturated,
            AudioDropsBacklogEviction = audioDrops.BacklogEviction,
            AudioChunksDropped = audioDrops.ChunksDropped,
            AudioQueueDropsRealtime = audioDrops.QueueDropsRealtime,
            AudioQueueDropsFileWriter = audioDrops.QueueDropsFileWriter,
            EstimatedPipelineLatencyMs = previewSummary.Frame.EstimatedPipelineLatencyMs,
            ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,
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
            MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,
            MjpegDecodeAvgMs = mjpeg.Timing.DecodeAvgMs,
            MjpegDecodeP95Ms = mjpeg.Timing.DecodeP95Ms,
            MjpegDecodeMaxMs = mjpeg.Timing.DecodeMaxMs,
            MjpegInteropCopySampleCount = mjpeg.Timing.InteropCopySampleCount,
            MjpegInteropCopyAvgMs = mjpeg.Timing.InteropCopyAvgMs,
            MjpegInteropCopyP95Ms = mjpeg.Timing.InteropCopyP95Ms,
            MjpegInteropCopyMaxMs = mjpeg.Timing.InteropCopyMaxMs,
            MjpegCallbackSampleCount = mjpeg.Timing.CallbackSampleCount,
            MjpegCallbackAvgMs = mjpeg.Timing.CallbackAvgMs,
            MjpegCallbackP95Ms = mjpeg.Timing.CallbackP95Ms,
            MjpegCallbackMaxMs = mjpeg.Timing.CallbackMaxMs,
            MjpegDecoderCount = mjpeg.Timing.DecoderCount,
            MjpegReorderSampleCount = mjpeg.Timing.ReorderSampleCount,
            MjpegReorderAvgMs = mjpeg.Timing.ReorderAvgMs,
            MjpegReorderP95Ms = mjpeg.Timing.ReorderP95Ms,
            MjpegReorderMaxMs = mjpeg.Timing.ReorderMaxMs,
            MjpegPipelineSampleCount = mjpeg.Timing.PipelineSampleCount,
            MjpegPipelineAvgMs = mjpeg.Timing.PipelineAvgMs,
            MjpegPipelineP95Ms = mjpeg.Timing.PipelineP95Ms,
            MjpegPipelineMaxMs = mjpeg.Timing.PipelineMaxMs,
            MjpegTotalDecoded = mjpeg.TotalDecoded,
            MjpegTotalEmitted = mjpeg.TotalEmitted,
            MjpegTotalDropped = mjpeg.TotalDropped,
            MjpegCompressedFramesQueued = mjpeg.CompressedFramesQueued,
            MjpegCompressedFramesDequeued = mjpeg.CompressedFramesDequeued,
            MjpegCompressedDropsQueueFull = mjpeg.CompressedDropsQueueFull,
            MjpegCompressedDropsByteBudget = mjpeg.CompressedDropsByteBudget,
            MjpegCompressedDropsDisposed = mjpeg.CompressedDropsDisposed,
            MjpegDecodeFailures = mjpeg.DecodeFailures,
            MjpegReorderCollisions = mjpeg.ReorderCollisions,
            MjpegEmitFailures = mjpeg.EmitFailures,
            MjpegCompressedQueueDepth = mjpeg.CompressedQueueDepth,
            MjpegCompressedQueueBytes = mjpeg.CompressedQueueBytes,
            MjpegCompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,
            MjpegReorderSkips = mjpeg.ReorderSkips,
            MjpegReorderBufferDepth = mjpeg.ReorderBufferDepth,
            MjpegPeakReorderDepth = mjpeg.PeakReorderDepth,
            MjpegPeakCompressedQueueBytes = mjpeg.PeakCompressedQueueBytes,
            MjpegReorderRingForceDrops = mjpeg.ReorderRingForceDrops,
            MjpegPreviewJitterEnabled = mjpeg.PreviewJitter.Queue.Enabled,
            MjpegPreviewJitterTargetDepth = mjpeg.PreviewJitter.Queue.TargetDepth,
            MjpegPreviewJitterMaxDepth = mjpeg.PreviewJitter.Queue.MaxDepth,
            MjpegPreviewJitterQueueDepth = mjpeg.PreviewJitter.Queue.QueueDepth,
            MjpegPreviewJitterTotalQueued = mjpeg.PreviewJitter.Queue.TotalQueued,
            MjpegPreviewJitterTotalSubmitted = mjpeg.PreviewJitter.Queue.TotalSubmitted,
            MjpegPreviewJitterTotalDropped = mjpeg.PreviewJitter.Queue.TotalDropped,
            MjpegPreviewJitterUnderflowCount = mjpeg.PreviewJitter.Queue.UnderflowCount,
            MjpegPreviewJitterResumeReprimeCount = mjpeg.PreviewJitter.Queue.ResumeReprimeCount,
            MjpegPreviewJitterInputSampleCount = mjpeg.PreviewJitter.Timing.InputSampleCount,
            MjpegPreviewJitterInputAvgMs = mjpeg.PreviewJitter.Timing.InputAvgMs,
            MjpegPreviewJitterInputP95Ms = mjpeg.PreviewJitter.Timing.InputP95Ms,
            MjpegPreviewJitterInputMaxMs = mjpeg.PreviewJitter.Timing.InputMaxMs,
            MjpegPreviewJitterOutputSampleCount = mjpeg.PreviewJitter.Timing.OutputSampleCount,
            MjpegPreviewJitterOutputAvgMs = mjpeg.PreviewJitter.Timing.OutputAvgMs,
            MjpegPreviewJitterOutputP95Ms = mjpeg.PreviewJitter.Timing.OutputP95Ms,
            MjpegPreviewJitterOutputMaxMs = mjpeg.PreviewJitter.Timing.OutputMaxMs,
            MjpegPreviewJitterLatencySampleCount = mjpeg.PreviewJitter.Timing.LatencySampleCount,
            MjpegPreviewJitterLatencyAvgMs = mjpeg.PreviewJitter.Timing.LatencyAvgMs,
            MjpegPreviewJitterLatencyP95Ms = mjpeg.PreviewJitter.Timing.LatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = mjpeg.PreviewJitter.Timing.LatencyMaxMs,
            MjpegPreviewJitterDeadlineDropCount = mjpeg.PreviewJitter.Adaptive.DeadlineDropCount,
            MjpegPreviewJitterClearedDropCount = mjpeg.PreviewJitter.Adaptive.ClearedDropCount,
            MjpegPreviewJitterTargetIncreaseCount = mjpeg.PreviewJitter.Adaptive.TargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = mjpeg.PreviewJitter.Adaptive.TargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = mjpeg.PreviewJitter.Events.LastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = mjpeg.PreviewJitter.Events.LastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = mjpeg.PreviewJitter.Events.LastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = mjpeg.PreviewJitter.Events.LastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = mjpeg.PreviewJitter.Events.LastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = mjpeg.PreviewJitter.Events.LastDropQpc,
            MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.Events.LastDropReason,
            MjpegPreviewJitterLastUnderflowQpc = mjpeg.PreviewJitter.Events.LastUnderflowQpc,
            MjpegPreviewJitterLastUnderflowReason = mjpeg.PreviewJitter.Events.LastUnderflowReason,
            MjpegPreviewJitterLastUnderflowQueueDepth = mjpeg.PreviewJitter.Events.LastUnderflowQueueDepth,
            MjpegPreviewJitterLastUnderflowInputAgeMs = mjpeg.PreviewJitter.Events.LastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = mjpeg.PreviewJitter.Events.LastUnderflowOutputAgeMs,
            MjpegPreviewJitterLastScheduleLateMs = mjpeg.PreviewJitter.Events.LastScheduleLateMs,
            MjpegPreviewJitterMaxScheduleLateMs = mjpeg.PreviewJitter.Events.MaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = mjpeg.PreviewJitter.Events.ScheduleLateCount,
            MjpegPacketHashSampleCount = mjpeg.PacketHash.SampleCount,
            MjpegPacketHashUniqueFrameCount = mjpeg.PacketHash.UniqueFrameCount,
            MjpegPacketHashDuplicateFrameCount = mjpeg.PacketHash.DuplicateFrameCount,
            MjpegPacketHashLongestDuplicateRun = mjpeg.PacketHash.LongestDuplicateRun,
            MjpegPacketHashInputObservedFps = mjpeg.PacketHash.InputObservedFps,
            MjpegPacketHashUniqueObservedFps = mjpeg.PacketHash.UniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = mjpeg.PacketHash.DuplicateFramePercent,
            MjpegPacketHashLastHash = mjpeg.PacketHash.LastHash,
            MjpegPacketHashLastFrameDuplicate = mjpeg.PacketHash.LastFrameDuplicate,
            MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,
            MjpegPacketHashRecentInputIntervalsMs = mjpeg.PacketHash.RecentInputIntervalsMs,
            MjpegPacketHashRecentUniqueIntervalsMs = mjpeg.PacketHash.RecentUniqueIntervalsMs,
            MjpegPacketHashRecentDuplicateFlags = mjpeg.PacketHash.RecentDuplicateFlags,
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
            VisualCenterCadenceSampleCount = visualCadence.CenterSampleCount,
            VisualCenterCadenceChangedFrameCount = visualCadence.CenterChangedFrameCount,
            VisualCenterCadenceRepeatFrameCount = visualCadence.CenterRepeatFrameCount,
            VisualCenterCadenceLongestRepeatRun = visualCadence.CenterLongestRepeatRun,
            VisualCenterCadenceOutputObservedFps = visualCadence.CenterOutputObservedFps,
            VisualCenterCadenceChangeObservedFps = visualCadence.CenterChangeObservedFps,
            VisualCenterCadenceRepeatFramePercent = visualCadence.CenterRepeatFramePercent,
            VisualCenterCadenceLastDelta = visualCadence.CenterLastDelta,
            VisualCenterCadenceAverageDelta = visualCadence.CenterAverageDelta,
            VisualCenterCadenceP95Delta = visualCadence.CenterP95Delta,
            VisualCenterCadenceMotionScore = visualCadence.CenterMotionScore,
            VisualCenterCadenceMotionConfidence = visualCadence.CenterMotionConfidence,
            VisualCenterCadenceRecentOutputIntervalsMs = visualCadence.CenterRecentOutputIntervalsMs,
            VisualCenterCadenceRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs,
            MjpegPerDecoder = mjpeg.Timing.PerDecoder,
            RecordingVideoBytes = recordingOutput.RecordingVideoBytes,
            RecordingAudioBytes = recordingOutput.RecordingAudioBytes,
            RecordingTotalBytes = recordingOutput.RecordingTotalBytes,
            RecordingFileGrowing = recordingOutput.RecordingFileGrowing,
            LastOutputPath = recordingOutput.LastOutputPath,
            LastFinalizeStatus = recordingOutput.LastFinalizeStatus,
            LastFinalizeUtc = recordingOutput.LastFinalizeUtc,
            RecordingLifecyclePhase = recordingOutput.RecordingLifecyclePhase,
            RecordingFinalizeOutcome = recordingOutput.RecordingFinalizeOutcome,
            RecordingFinalizeFailureCode = recordingOutput.RecordingFinalizeFailureCode,
            RecordingFinalizationVerificationCompleted = recordingOutput.RecordingFinalizationVerificationCompleted,
            RecordingFinalizationCleanupPending = recordingOutput.RecordingFinalizationCleanupPending,
            RecordingFinalizationElapsedMs = recordingOutput.RecordingFinalizationElapsedMs,
            RecordingRecoveryPath = recordingOutput.RecordingRecoveryPath,
            RecordingRequestedTracks = recordingOutput.RecordingRequestedTracks,
            RecordingObservedTracks = recordingOutput.RecordingObservedTracks,
            LastPreservedArtifacts = recordingOutput.LastPreservedArtifacts,
            RecordingFinalizationProgressStage = recordingOutput.RecordingFinalizationProgressStage,
            LastRecordingFinalizationProgressUtc = recordingOutput.LastRecordingFinalizationProgressUtc,
            LastOutputExists = recordingOutput.LastOutputExists,
            LastOutputSizeBytes = recordingOutput.LastOutputSizeBytes,
            LastVerification = recordingOutput.LastVerification,
            HdrTruthVerdict = hdrPipeline.TruthVerdict,
            MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,
            MemoryPrivateBytesMb = processResourceProjection.MemoryPrivateBytesMb,
            MemoryManagedHeapMb = processResourceProjection.MemoryManagedHeapMb,
            MemoryTotalAllocatedMb = processResourceProjection.MemoryTotalAllocatedMb,
            ProcessCpuPercent = processResourceProjection.ProcessCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processResourceProjection.ProcessCpuTotalProcessorTimeMs,
            MemoryGcHeapSizeMb = processResourceProjection.MemoryGcHeapSizeMb,
            MemoryGcGen0Collections = processResourceProjection.MemoryGcGen0Collections,
            MemoryGcGen1Collections = processResourceProjection.MemoryGcGen1Collections,
            MemoryGcGen2Collections = processResourceProjection.MemoryGcGen2Collections,
            MemoryGcPauseTimePercent = processResourceProjection.MemoryGcPauseTimePercent,
            MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,
            ThreadPoolWorkerAvailable = processResourceProjection.ThreadPoolWorkerAvailable,
            ThreadPoolWorkerMax = processResourceProjection.ThreadPoolWorkerMax,
            ThreadPoolIoAvailable = processResourceProjection.ThreadPoolIoAvailable,
            ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,
            AvSyncCaptureDriftMs = avSync.CaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs = avSync.EncoderDriftMs,
            AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,
            FlashbackActive = flashbackRecording.Runtime.Active,
            FlashbackBufferedDurationMs = flashbackRecording.Runtime.BufferedDurationMs,
            FlashbackDiskBytes = flashbackRecording.Runtime.DiskBytes,
            FlashbackTotalBytesWritten = flashbackRecording.Runtime.TotalBytesWritten,
            FlashbackOutputBytes = flashbackRecording.Runtime.OutputBytes,
            FlashbackFilePath = flashbackRecording.Runtime.FilePath,
            FlashbackEncodedFrames = flashbackRecording.Runtime.EncodedFrames,
            FlashbackDroppedFrames = flashbackRecording.Runtime.DroppedFrames,
            FlashbackGpuEncoding = flashbackRecording.Runtime.GpuEncoding,
            FlashbackBackendSettingsStale = flashbackRecording.Backend.SettingsStale,
            FlashbackBackendSettingsStaleReason = flashbackRecording.Backend.SettingsStaleReason,
            FlashbackBackendActiveFormat = flashbackRecording.Backend.ActiveFormat,
            FlashbackBackendRequestedFormat = flashbackRecording.Backend.RequestedFormat,
            FlashbackBackendActivePreset = flashbackRecording.Backend.ActivePreset,
            FlashbackBackendRequestedPreset = flashbackRecording.Backend.RequestedPreset,
            FlashbackExportVerificationFormat = flashbackRecording.Backend.ExportVerificationFormat,
            FlashbackCodecDowngradeReason = flashbackRecording.Backend.CodecDowngradeReason,
            EncoderCodecName = flashbackRecording.Encoder.CodecName,
            EncoderTargetBitRate = flashbackRecording.Encoder.TargetBitRate,
            EncoderWidth = flashbackRecording.Encoder.Width,
            EncoderHeight = flashbackRecording.Encoder.Height,
            EncoderFrameRate = flashbackRecording.Encoder.FrameRate,
            EncoderFrameRateNumerator = flashbackRecording.Encoder.FrameRateNumerator,
            EncoderFrameRateDenominator = flashbackRecording.Encoder.FrameRateDenominator,
            FlashbackVideoQueueDepth = flashbackRecording.Queues.VideoQueueDepth,
            FlashbackAudioQueueDepth = flashbackRecording.Queues.AudioQueueDepth,
            FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,
            FlashbackPlaybackState = flashbackPlayback.State,
            FlashbackPlaybackPositionMs = flashbackPlayback.PositionMs,
            FlashbackDecoderHwAccel = flashbackPlayback.DecoderHwAccel,
            FlashbackPlaybackFrameCount = flashbackPlayback.FrameCount,
            FlashbackPlaybackLateFrames = flashbackPlayback.LateFrames,
            FlashbackPlaybackDroppedFrames = flashbackPlayback.DroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = flashbackPlayback.AudioMaster.DelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = flashbackPlayback.AudioMaster.DelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = flashbackPlayback.AudioMaster.UnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = flashbackPlayback.AudioMaster.StaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = flashbackPlayback.AudioMaster.DriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = flashbackPlayback.AudioMaster.LastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackDriftMs = flashbackPlayback.AudioMaster.LastFallbackDriftMs,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = flashbackPlayback.AudioMaster.LastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = flashbackPlayback.Timing.SegmentSwitches,
            FlashbackPlaybackFmp4Reopens = flashbackPlayback.Timing.Fmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = flashbackPlayback.Timing.WriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = flashbackPlayback.Timing.NearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = flashbackPlayback.Timing.DecodeErrorSnaps,
            FlashbackPlaybackSubmitFailures = flashbackPlayback.Timing.SubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = flashbackPlayback.Timing.LastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = flashbackPlayback.Timing.LastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = flashbackPlayback.Timing.LastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = flashbackPlayback.Timing.LastSubmitFailure,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = flashbackPlayback.Timing.LastSegmentSwitchUtcUnixMs,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = flashbackPlayback.Timing.LastFmp4ReopenUtcUnixMs,
            FlashbackPlaybackLastWriteHeadWaitGapMs = flashbackPlayback.Timing.LastWriteHeadWaitGapMs,
            FlashbackPlaybackTargetFps = flashbackPlayback.Timing.TargetFps,
            FlashbackPlaybackObservedFps = flashbackPlayback.Timing.ObservedFps,
            FlashbackPlaybackAvgFrameMs = flashbackPlayback.Timing.AvgFrameMs,
            FlashbackPlaybackCadenceSampleCount = flashbackPlayback.Timing.CadenceSampleCount,
            FlashbackPlaybackP95FrameMs = flashbackPlayback.Timing.P95FrameMs,
            FlashbackPlaybackP99FrameMs = flashbackPlayback.Timing.P99FrameMs,
            FlashbackPlaybackMaxFrameMs = flashbackPlayback.Timing.MaxFrameMs,
            FlashbackPlaybackSlowFrames = flashbackPlayback.Timing.SlowFrames,
            FlashbackPlaybackSlowFramePercent = flashbackPlayback.Timing.SlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = flashbackPlayback.Timing.OnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = flashbackPlayback.Timing.FivePercentLowFps,
            FlashbackPlaybackSampleDurationMs = flashbackPlayback.Timing.SampleDurationMs,
            FlashbackPlaybackRecentFrameIntervalsMs = flashbackPlayback.Timing.RecentFrameIntervalsMs,
            FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlayback.Timing.PtsCadenceMismatchCount,
            FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = flashbackPlayback.Timing.LastPtsCadenceMismatchUtcUnixMs,
            FlashbackPlaybackLastPtsCadenceDeltaMs = flashbackPlayback.Timing.LastPtsCadenceDeltaMs,
            FlashbackPlaybackLastPtsCadenceExpectedMs = flashbackPlayback.Timing.LastPtsCadenceExpectedMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = flashbackPlayback.Decode.SeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = flashbackPlayback.Decode.LastSeekHitForwardDecodeCap,
            FlashbackPlaybackDecodeSampleCount = flashbackPlayback.Decode.SampleCount,
            FlashbackPlaybackDecodeAvgMs = flashbackPlayback.Decode.AvgMs,
            FlashbackPlaybackDecodeP95Ms = flashbackPlayback.Decode.P95Ms,
            FlashbackPlaybackDecodeP99Ms = flashbackPlayback.Decode.P99Ms,
            FlashbackPlaybackDecodeMaxMs = flashbackPlayback.Decode.MaxMs,
            FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,
            FlashbackPlaybackMaxDecodeReceiveMs = flashbackPlayback.Decode.MaxReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = flashbackPlayback.Decode.MaxFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = flashbackPlayback.Decode.MaxReadMs,
            FlashbackPlaybackMaxDecodeSendMs = flashbackPlayback.Decode.MaxSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = flashbackPlayback.Decode.MaxAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = flashbackPlayback.Decode.MaxConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = flashbackPlayback.Decode.MaxUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = flashbackPlayback.Decode.MaxPositionMs,
            FlashbackAvDriftMs = flashbackPlayback.Timing.AvDriftMs,
            FlashbackPlaybackThreadAlive = flashbackPlayback.Commands.ThreadAlive,
            FlashbackPlaybackCommandsEnqueued = flashbackPlayback.Commands.Enqueued,
            FlashbackPlaybackCommandsProcessed = flashbackPlayback.Commands.Processed,
            FlashbackPlaybackCommandsDropped = flashbackPlayback.Commands.Dropped,
            FlashbackPlaybackCommandsSkippedNotReady = flashbackPlayback.Commands.SkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = flashbackPlayback.Commands.ScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = flashbackPlayback.Commands.SeekCommandsCoalesced,
            FlashbackPlaybackCommandQueueCapacity = flashbackPlayback.Commands.QueueCapacity,
            FlashbackPlaybackPendingCommands = flashbackPlayback.Commands.Pending,
            FlashbackPlaybackMaxPendingCommands = flashbackPlayback.Commands.MaxPending,
            FlashbackPlaybackLastCommandQueueLatencyMs = flashbackPlayback.Commands.LastQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyMs = flashbackPlayback.Commands.MaxQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = flashbackPlayback.Commands.MaxQueueLatencyCommand,
            FlashbackPlaybackLastCommandQueued = flashbackPlayback.Commands.LastQueued,
            FlashbackPlaybackLastCommandProcessed = flashbackPlayback.Commands.LastProcessed,
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = flashbackPlayback.Commands.LastQueuedUtcUnixMs,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = flashbackPlayback.Commands.LastProcessedUtcUnixMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = flashbackPlayback.Commands.LastFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,
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
            LastExportId = flashbackExportLastResult.LastExportId,
            LastExportPath = flashbackExportLastResult.LastExportPath,
            LastExportSuccess = flashbackExportLastResult.LastExportSuccess,
            LastExportMessage = flashbackExportLastResult.LastExportMessage
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

    private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)
        => new()
        {
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
            ThreadPoolIoMax = processResources.ThreadPoolIoMax
        };

    private readonly record struct ProcessResourceProjection
    {
        public double MemoryWorkingSetMb { get; init; }
        public double MemoryPrivateBytesMb { get; init; }
        public double MemoryManagedHeapMb { get; init; }
        public double MemoryTotalAllocatedMb { get; init; }
        public double ProcessCpuPercent { get; init; }
        public double ProcessCpuTotalProcessorTimeMs { get; init; }
        public double MemoryGcHeapSizeMb { get; init; }
        public int MemoryGcGen0Collections { get; init; }
        public int MemoryGcGen1Collections { get; init; }
        public int MemoryGcGen2Collections { get; init; }
        public double MemoryGcPauseTimePercent { get; init; }
        public double MemoryGcFragmentationPercent { get; init; }
        public int ThreadPoolWorkerAvailable { get; init; }
        public int ThreadPoolWorkerMax { get; init; }
        public int ThreadPoolIoAvailable { get; init; }
        public int ThreadPoolIoMax { get; init; }
    }

    private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,
            CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,
            EncoderDriftMs = captureRuntime.AvSyncEncoderDriftMs,
            EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples
        };

    private readonly record struct AvSyncProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }

    private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)
        => new()
        {
            ExpectedFrameRate = health.ExpectedFrameRate,
            SampleCount = health.CaptureCadenceSampleCount,
            ObservedFps = health.CaptureCadenceObservedFps,
            ExpectedIntervalMs = health.CaptureCadenceExpectedIntervalMs,
            AverageIntervalMs = health.CaptureCadenceAverageIntervalMs,
            P95IntervalMs = health.CaptureCadenceP95IntervalMs,
            P99IntervalMs = health.CaptureCadenceP99IntervalMs,
            MaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            OnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
            FivePercentLowFps = health.CaptureCadenceFivePercentLowFps,
            SampleDurationMs = health.CaptureCadenceSampleDurationMs,
            RecentIntervalsMs = health.CaptureCadenceRecentIntervalsMs,
            JitterStdDevMs = health.CaptureCadenceJitterStdDevMs,
            SevereGapCount = health.CaptureCadenceSevereGapCount,
            EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
            EstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent
        };

    private readonly record struct CaptureCadenceProjection
    {
        public double ExpectedFrameRate { get; init; }
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SevereGapCount { get; init; }
        public long EstimatedDroppedFrames { get; init; }
        public double EstimatedDropPercent { get; init; }
    }

    private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.VisualCadenceSampleCount,
            ChangedFrameCount = health.VisualCadenceChangedFrameCount,
            RepeatFrameCount = health.VisualCadenceRepeatFrameCount,
            LongestRepeatRun = health.VisualCadenceLongestRepeatRun,
            OutputObservedFps = health.VisualCadenceOutputObservedFps,
            ChangeObservedFps = health.VisualCadenceChangeObservedFps,
            RepeatFramePercent = health.VisualCadenceRepeatFramePercent,
            LastDelta = health.VisualCadenceLastDelta,
            AverageDelta = health.VisualCadenceAverageDelta,
            P95Delta = health.VisualCadenceP95Delta,
            MotionScore = health.VisualCadenceMotionScore,
            MotionConfidence = health.VisualCadenceMotionConfidence,
            RecentOutputIntervalsMs = health.VisualCadenceRecentOutputIntervalsMs,
            RecentChangeIntervalsMs = health.VisualCadenceRecentChangeIntervalsMs,
            CenterSampleCount = health.VisualCenterCadenceSampleCount,
            CenterChangedFrameCount = health.VisualCenterCadenceChangedFrameCount,
            CenterRepeatFrameCount = health.VisualCenterCadenceRepeatFrameCount,
            CenterLongestRepeatRun = health.VisualCenterCadenceLongestRepeatRun,
            CenterOutputObservedFps = health.VisualCenterCadenceOutputObservedFps,
            CenterChangeObservedFps = health.VisualCenterCadenceChangeObservedFps,
            CenterRepeatFramePercent = health.VisualCenterCadenceRepeatFramePercent,
            CenterLastDelta = health.VisualCenterCadenceLastDelta,
            CenterAverageDelta = health.VisualCenterCadenceAverageDelta,
            CenterP95Delta = health.VisualCenterCadenceP95Delta,
            CenterMotionScore = health.VisualCenterCadenceMotionScore,
            CenterMotionConfidence = health.VisualCenterCadenceMotionConfidence,
            CenterRecentOutputIntervalsMs = health.VisualCenterCadenceRecentOutputIntervalsMs,
            CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs
        };

    private readonly record struct VisualCadenceProjection
    {
        public int SampleCount { get; init; }
        public long ChangedFrameCount { get; init; }
        public long RepeatFrameCount { get; init; }
        public long LongestRepeatRun { get; init; }
        public double OutputObservedFps { get; init; }
        public double ChangeObservedFps { get; init; }
        public double RepeatFramePercent { get; init; }
        public double LastDelta { get; init; }
        public double AverageDelta { get; init; }
        public double P95Delta { get; init; }
        public double MotionScore { get; init; }
        public string MotionConfidence { get; init; }
        public double[] RecentOutputIntervalsMs { get; init; }
        public double[] RecentChangeIntervalsMs { get; init; }
        public int CenterSampleCount { get; init; }
        public long CenterChangedFrameCount { get; init; }
        public long CenterRepeatFrameCount { get; init; }
        public long CenterLongestRepeatRun { get; init; }
        public double CenterOutputObservedFps { get; init; }
        public double CenterChangeObservedFps { get; init; }
        public double CenterRepeatFramePercent { get; init; }
        public double CenterLastDelta { get; init; }
        public double CenterAverageDelta { get; init; }
        public double CenterP95Delta { get; init; }
        public double CenterMotionScore { get; init; }
        public string CenterMotionConfidence { get; init; }
        public double[] CenterRecentOutputIntervalsMs { get; init; }
        public double[] CenterRecentChangeIntervalsMs { get; init; }
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

    private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)
        => new()
        {
            CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,
            CommandsCompleted = viewModelSnapshot.CaptureCommandCommandsCompleted,
            CommandsFailed = viewModelSnapshot.CaptureCommandCommandsFailed,
            CommandsCanceled = viewModelSnapshot.CaptureCommandCommandsCanceled,
            CommandsCoalesced = viewModelSnapshot.CaptureCommandCommandsCoalesced,
            PendingCommands = viewModelSnapshot.CaptureCommandPendingCommands,
            MaxPendingCommands = viewModelSnapshot.CaptureCommandMaxPendingCommands,
            OldestPendingCommandAgeMs = viewModelSnapshot.CaptureCommandOldestPendingCommandAgeMs,
            LastQueueLatencyMs = viewModelSnapshot.CaptureCommandLastQueueLatencyMs,
            MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,
            LastCommand = viewModelSnapshot.CaptureCommandLastCommand,
            LastOutcome = viewModelSnapshot.CaptureCommandLastOutcome,
            LastCorrelationId = viewModelSnapshot.CaptureCommandLastCorrelationId,
            LastError = viewModelSnapshot.CaptureCommandLastError
        };

    private readonly record struct CaptureCommandProjection
    {
        public long CommandsEnqueued { get; init; }
        public long CommandsCompleted { get; init; }
        public long CommandsFailed { get; init; }
        public long CommandsCanceled { get; init; }
        public long CommandsCoalesced { get; init; }
        public int PendingCommands { get; init; }
        public int MaxPendingCommands { get; init; }
        public long OldestPendingCommandAgeMs { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string LastCommand { get; init; }
        public string LastOutcome { get; init; }
        public string LastCorrelationId { get; init; }
        public string LastError { get; init; }
    }

    private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Requested = BuildCaptureFormatRequestedProjection(captureRuntime),
            HdrRequest = BuildCaptureFormatHdrRequestProjection(captureRuntime),
            Actual = BuildCaptureFormatActualProjection(captureRuntime),
            Negotiated = BuildCaptureFormatNegotiatedProjection(captureRuntime),
            ReaderObservation = BuildCaptureFormatReaderObservationProjection(captureRuntime),
            Encoder = BuildCaptureFormatEncoderProjection(captureRuntime)
        };

    private readonly record struct CaptureFormatProjection
    {
        public CaptureFormatRequestedProjection Requested { get; init; }
        public CaptureFormatHdrRequestProjection HdrRequest { get; init; }
        public CaptureFormatActualProjection Actual { get; init; }
        public CaptureFormatNegotiatedProjection Negotiated { get; init; }
        public CaptureFormatReaderObservationProjection ReaderObservation { get; init; }
        public CaptureFormatEncoderProjection Encoder { get; init; }
    }

    private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.RequestedWidth,
            Height = captureRuntime.RequestedHeight,
            FrameRate = captureRuntime.RequestedFrameRate,
            FrameRateArg = captureRuntime.RequestedFrameRateArg,
            FrameRateNumerator = captureRuntime.RequestedFrameRateNumerator,
            FrameRateDenominator = captureRuntime.RequestedFrameRateDenominator,
            PixelFormat = captureRuntime.RequestedPixelFormat,
            Format = captureRuntime.RequestedFormat,
            Quality = captureRuntime.RequestedQuality,
            HdrEnabled = captureRuntime.RequestedHdrEnabled,
            HdrMasteringMetadata = captureRuntime.RequestedHdrMasteringMetadata,
            AudioEnabled = captureRuntime.RequestedAudioEnabled
        };

    private readonly record struct CaptureFormatRequestedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? Format { get; init; }
        public string? Quality { get; init; }
        public bool? HdrEnabled { get; init; }
        public bool? HdrMasteringMetadata { get; init; }
        public bool? AudioEnabled { get; init; }
    }

    private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            ActivationReason = captureRuntime.HdrActivationReason,
            AutoDowngraded = captureRuntime.HdrAutoDowngraded,
            AutoDowngradeReason = captureRuntime.HdrAutoDowngradeReason,
            RequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit
        };

    private readonly record struct CaptureFormatHdrRequestProjection
    {
        public string ActivationReason { get; init; }
        public bool AutoDowngraded { get; init; }
        public string AutoDowngradeReason { get; init; }
        public bool RequestedButSourceNot10Bit { get; init; }
    }

    private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.ActualWidth,
            Height = captureRuntime.ActualHeight,
            FrameRate = captureRuntime.ActualFrameRate,
            FrameRateArg = captureRuntime.ActualFrameRateArg
        };

    private readonly record struct CaptureFormatActualProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
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

    private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
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
            MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters
        };

    private readonly record struct CaptureFormatReaderObservationProjection
    {
        public string? RequestedReaderSubtype { get; init; }
        public string? ReaderSourceStreamType { get; init; }
        public string? ReaderSourceSubtype { get; init; }
        public string? FirstObservedFramePixelFormat { get; init; }
        public string? LatestObservedFramePixelFormat { get; init; }
        public string? LatestObservedSurfaceFormat { get; init; }
        public long ObservedP010FrameCount { get; init; }
        public long ObservedNv12FrameCount { get; init; }
        public long ObservedOtherFrameCount { get; init; }
        public long ObservedP010BitDepthSampleCount { get; init; }
        public double ObservedP010Low2BitNonZeroPercent { get; init; }
        public bool? ObservedP010Likely8BitUpscaled { get; init; }
        public bool? MfReadwriteDisableConverters { get; init; }
    }

    private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            InputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            OutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            VideoCodec = captureRuntime.EncoderVideoCodec,
            VideoProfile = captureRuntime.EncoderVideoProfile,
            TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed
        };

    private readonly record struct CaptureFormatEncoderProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }

    private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            MemoryPreference = captureRuntime.MemoryPreference,
            VideoRequestedSubtype = captureRuntime.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureRuntime.FrameLedgerCapacity,
            FrameLedgerEventCount = captureRuntime.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureRuntime.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents
        };

    private readonly record struct CaptureTransportProjection
    {
        public string MemoryPreference { get; init; }
        public string VideoRequestedSubtype { get; init; }
        public string VideoNegotiatedSubtype { get; init; }
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; }
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
            captureRuntime.FirstObservedFramePixelFormat ??
            captureRuntime.NegotiatedPixelFormat);
        var hasP010 = captureRuntime.ObservedP010FrameCount > 0 || string.Equals(observedFormatToken, "P010", StringComparison.OrdinalIgnoreCase);
        var hasNv12 = captureRuntime.ObservedNv12FrameCount > 0 || string.Equals(observedFormatToken, "NV12", StringComparison.OrdinalIgnoreCase);
        var pipelineFormat = hasP010
            ? "P010"
            : hasNv12
                ? "NV12"
                : observedFormatToken;

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
        if (!sourceHdr.HasValue)
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

    private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)
    {
        var timing = BuildMjpegTimingProjection(health);
        var previewJitter = BuildMjpegPreviewJitterProjection(health);
        var packetHash = BuildMjpegPacketHashProjection(health);

        return new()
        {
            Timing = timing,
            TotalDecoded = health.MjpegTotalDecoded,
            TotalEmitted = health.MjpegTotalEmitted,
            TotalDropped = health.MjpegTotalDropped,
            CompressedFramesQueued = health.MjpegCompressedFramesQueued,
            CompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            CompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            CompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            CompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            DecodeFailures = health.MjpegDecodeFailures,
            ReorderCollisions = health.MjpegReorderCollisions,
            EmitFailures = health.MjpegEmitFailures,
            CompressedQueueDepth = health.MjpegCompressedQueueDepth,
            CompressedQueueBytes = health.MjpegCompressedQueueBytes,
            CompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            ReorderSkips = health.MjpegReorderSkips,
            ReorderBufferDepth = health.MjpegReorderBufferDepth,
            PeakReorderDepth = health.MjpegPeakReorderDepth,
            PeakCompressedQueueBytes = health.MjpegPeakCompressedQueueBytes,
            ReorderRingForceDrops = health.MjpegReorderRingForceDrops,
            PreviewJitter = previewJitter,
            PacketHash = packetHash,
        };
    }

    private readonly record struct MjpegProjection
    {
        public MjpegTimingProjection Timing { get; init; }
        public long TotalDecoded { get; init; }
        public long TotalEmitted { get; init; }
        public long TotalDropped { get; init; }
        public long CompressedFramesQueued { get; init; }
        public long CompressedFramesDequeued { get; init; }
        public long CompressedDropsQueueFull { get; init; }
        public long CompressedDropsByteBudget { get; init; }
        public long CompressedDropsDisposed { get; init; }
        public long DecodeFailures { get; init; }
        public long ReorderCollisions { get; init; }
        public long EmitFailures { get; init; }
        public int CompressedQueueDepth { get; init; }
        public long CompressedQueueBytes { get; init; }
        public long CompressedQueueByteBudget { get; init; }
        public long ReorderSkips { get; init; }
        public int ReorderBufferDepth { get; init; }
        public int PeakReorderDepth { get; init; }
        public long PeakCompressedQueueBytes { get; init; }
        public long ReorderRingForceDrops { get; init; }
        public MjpegPreviewJitterProjection PreviewJitter { get; init; }
        public MjpegPacketHashProjection PacketHash { get; init; }
    }

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

    private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueProjection(health),
            Timing = BuildMjpegPreviewJitterTimingProjection(health),
            Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),
            Events = BuildMjpegPreviewJitterEventProjection(health)
        };

    private readonly record struct MjpegPreviewJitterProjection
    {
        public MjpegPreviewJitterQueueProjection Queue { get; init; }
        public MjpegPreviewJitterTimingProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventProjection Events { get; init; }
    }

    private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Enabled = health.MjpegPreviewJitterEnabled,
            TargetDepth = health.MjpegPreviewJitterTargetDepth,
            MaxDepth = health.MjpegPreviewJitterMaxDepth,
            QueueDepth = health.MjpegPreviewJitterQueueDepth,
            TotalQueued = health.MjpegPreviewJitterTotalQueued,
            TotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            TotalDropped = health.MjpegPreviewJitterTotalDropped,
            UnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }

    private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            InputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            InputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            InputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            InputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            OutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            OutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            OutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            OutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            LatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            LatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            LatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            ClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            TargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            LastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            LastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            LastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            LastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            LastDropReason = health.MjpegPreviewJitterLastDropReason,
            LastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            LastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            LastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            LastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }

    private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.MjpegPacketHashSampleCount,
            UniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            DuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            LongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            InputObservedFps = health.MjpegPacketHashInputObservedFps,
            UniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            DuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            LastHash = health.MjpegPacketHashLastHash,
            LastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            Pattern = health.MjpegPacketHashPattern,
            RecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            RecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags
        };

    private readonly record struct MjpegPacketHashProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }

    private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),
            Video = BuildRecordingIntegrityVideoProjection(captureRuntime),
            Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),
            Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),
            AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)
        };

    private readonly record struct RecordingIntegrityProjection
    {
        public RecordingIntegritySummaryProjection Summary { get; init; }
        public RecordingIntegrityVideoProjection Video { get; init; }
        public RecordingIntegrityBackpressureProjection Backpressure { get; init; }
        public RecordingIntegrityAudioProjection Audio { get; init; }
        public RecordingIntegrityAvSyncProjection AvSync { get; init; }
    }

    private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Status = captureRuntime.RecordingIntegrityStatus,
            Complete = captureRuntime.RecordingIntegrityComplete,
            Backend = captureRuntime.RecordingIntegrityBackend,
            CompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            Reason = captureRuntime.RecordingIntegrityReason
        };

    private readonly record struct RecordingIntegritySummaryProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            SourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            AcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            PipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            QueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            SubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            PacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            EncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps
        };

    private readonly record struct RecordingIntegrityVideoProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }

    private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            QueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            BackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            BackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs
        };

    private readonly record struct RecordingIntegrityBackpressureProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            AudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            AudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            AudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            AudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            AudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            AudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            AudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps
        };

    private readonly record struct RecordingIntegrityAudioProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }

    private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            AvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderProjection(health),
            Ingest = BuildRecordingPipelineIngestProjection(health),
            VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)
        };

    private readonly record struct RecordingPipelineProjection
    {
        public RecordingPipelineEncoderProjection Encoder { get; init; }
        public RecordingPipelineIngestProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }
    }

    private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(CaptureHealthSnapshot health)
        => new()
        {
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoFramesEncoded = health.VideoFramesConverted,
            LastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            LastWriteAgeMs = health.LastVideoWriteAgeMs,
            EncodingFailed = health.RecordingEncodingFailed,
            EncodingFailureType = health.RecordingEncodingFailureType,
            EncodingFailureMessage = health.RecordingEncodingFailureMessage
        };

    private readonly record struct RecordingPipelineEncoderProjection
    {
        public long VideoFramesEnqueued { get; init; }
        public long VideoFramesEncoded { get; init; }
        public long LastEnqueueAgeMs { get; init; }
        public long LastWriteAgeMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }

    private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(CaptureHealthSnapshot health)
        => new()
        {
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
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction
        };

    private readonly record struct RecordingPipelineIngestProjection
    {
        public int ConversionQueueDepth { get; init; }
        public int FfmpegVideoQueueDepth { get; init; }
        public int FfmpegAudioQueueDepth { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesQueued { get; init; }
        public long VideoFramesDropped { get; init; }
        public long VideoFramesDroppedBacklog { get; init; }
        public long VideoFramesConverted { get; init; }
        public long VideoFramesEnqueued { get; init; }
        public long VideoDropsQueueSaturated { get; init; }
        public long VideoDropsBacklogEviction { get; init; }
    }

    private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(CaptureHealthSnapshot health)
        => new()
        {
            Capacity = health.RecordingVideoQueueCapacity,
            MaxDepth = health.RecordingVideoQueueMaxDepth,
            FramesSubmittedToEncoder = health.RecordingVideoFramesSubmittedToEncoder,
            EncoderPts = health.RecordingVideoEncoderPts,
            EncoderPacketsWritten = health.RecordingVideoEncoderPacketsWritten,
            EncoderDroppedFrames = health.RecordingVideoEncoderDroppedFrames,
            SequenceGaps = health.RecordingVideoSequenceGaps,
            OldestFrameAgeMs = health.RecordingVideoQueueOldestFrameAgeMs,
            LastLatencyMs = health.RecordingVideoQueueLastLatencyMs,
            LatencySampleCount = health.RecordingVideoQueueLatencySampleCount,
            LatencyAvgMs = health.RecordingVideoQueueLatencyAvgMs,
            LatencyP95Ms = health.RecordingVideoQueueLatencyP95Ms,
            LatencyP99Ms = health.RecordingVideoQueueLatencyP99Ms,
            LatencyMaxMs = health.RecordingVideoQueueLatencyMaxMs,
            BackpressureWaitMs = health.RecordingVideoBackpressureWaitMs,
            BackpressureEvents = health.RecordingVideoBackpressureEvents,
            BackpressureLastWaitMs = health.RecordingVideoBackpressureLastWaitMs,
            BackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs
        };

    private readonly record struct RecordingPipelineVideoQueueProjection
    {
        public int Capacity { get; init; }
        public int MaxDepth { get; init; }
        public long FramesSubmittedToEncoder { get; init; }
        public long EncoderPts { get; init; }
        public long EncoderPacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public long OldestFrameAgeMs { get; init; }
        public long LastLatencyMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyP99Ms { get; init; }
        public double LatencyMaxMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureLastWaitMs { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(CaptureHealthSnapshot health)
        => new()
        {
            GpuQueueDepth = health.RecordingGpuQueueDepth,
            GpuQueueCapacity = health.RecordingGpuQueueCapacity,
            GpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            GpuFramesDropped = health.RecordingGpuFramesDropped,
            CudaQueueDepth = health.RecordingCudaQueueDepth,
            CudaQueueCapacity = health.RecordingCudaQueueCapacity,
            CudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            CudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            CudaFramesDropped = health.RecordingCudaFramesDropped
        };

    private readonly record struct RecordingPipelineHardwareQueuesProjection
    {
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public int CudaQueueDepth { get; init; }
        public int CudaQueueCapacity { get; init; }
        public int CudaQueueMaxDepth { get; init; }
        public long CudaFramesEnqueued { get; init; }
        public long CudaFramesDropped { get; init; }
    }

    private static RecordingOutputProjection BuildRecordingOutputProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        RecordingStats recordingStats,
        bool recordingFileGrowing,
        LastOutputProbe lastOutput,
        RecordingVerificationResult? lastVerification)
        => new()
        {
            OutputPath = viewModelSnapshot.OutputPath,
            RecordingTime = viewModelSnapshot.RecordingTime,
            RecordingSizeInfo = viewModelSnapshot.RecordingSizeInfo,
            RecordingBitrateInfo = viewModelSnapshot.RecordingBitrateInfo,
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
            LastVerification = lastVerification
        };

    private readonly record struct RecordingOutputProjection
    {
        public string OutputPath { get; init; }
        public string RecordingTime { get; init; }
        public string RecordingSizeInfo { get; init; }
        public string RecordingBitrateInfo { get; init; }
        public long RecordingVideoBytes { get; init; }
        public long RecordingAudioBytes { get; init; }
        public long RecordingTotalBytes { get; init; }
        public bool RecordingFileGrowing { get; init; }
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; }
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public string RecordingLifecyclePhase { get; init; }
        public string RecordingFinalizeOutcome { get; init; }
        public string RecordingFinalizeFailureCode { get; init; }
        public bool RecordingFinalizationVerificationCompleted { get; init; }
        public bool RecordingFinalizationCleanupPending { get; init; }
        public long RecordingFinalizationElapsedMs { get; init; }
        public string? RecordingRecoveryPath { get; init; }
        public IReadOnlyList<string> RecordingRequestedTracks { get; init; }
        public IReadOnlyList<string> RecordingObservedTracks { get; init; }
        public IReadOnlyList<string> LastPreservedArtifacts { get; init; }
        public string RecordingFinalizationProgressStage { get; init; }
        public DateTimeOffset? LastRecordingFinalizationProgressUtc { get; init; }
        public bool LastOutputExists { get; init; }
        public long? LastOutputSizeBytes { get; init; }
        public RecordingVerificationResult? LastVerification { get; init; }
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

    private static AudioAndIngestProjection BuildAudioAndIngestProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        AudioSignalState audioSignal)
        => new()
        {
            Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),
            Ingest = BuildCaptureIngestProjection(captureRuntime),
            Wasapi = BuildWasapiAudioProjection(captureRuntime)
        };

    private readonly record struct AudioAndIngestProjection
    {
        public AudioSignalProjection Signal { get; init; }
        public CaptureIngestProjection Ingest { get; init; }
        public WasapiAudioProjection Wasapi { get; init; }
    }

    private static AudioSignalProjection BuildAudioSignalProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        AudioSignalState audioSignal)
        => new()
        {
            Peak = viewModelSnapshot.AudioPeak,
            Clipping = viewModelSnapshot.AudioClipping,
            SignalPresent = audioSignal.SignalPresent,
            MutedSuspected = audioSignal.MutedSuspected
        };

    private readonly record struct AudioSignalProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }

    private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            VideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            VideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            LastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth
        };

    private readonly record struct CaptureIngestProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public bool SourceReaderReadOutstanding { get; init; }
        public long SourceReaderReadOutstandingMs { get; init; }
        public long SourceReaderLastFrameTickMs { get; init; }
        public int SourceReaderFrameChannelDepth { get; init; }
    }

    private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            CaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            CaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            CaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            CaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            CaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            CaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            CaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            CaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            CaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            PlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            PlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            PlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            PlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            PlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            PlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            PlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            PlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            PlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs,
            BufferHealthStatus = captureRuntime.AudioBufferHealthStatus,
            BufferHealthReason = captureRuntime.AudioBufferHealthReason,
            BufferUnderrunDetected = captureRuntime.AudioBufferUnderrunDetected,
            BufferOverrunDetected = captureRuntime.AudioBufferOverrunDetected,
            BufferUnderrunEvents = captureRuntime.AudioBufferUnderrunEvents,
            BufferOverrunEvents = captureRuntime.AudioBufferOverrunEvents
        };

    private readonly record struct WasapiAudioProjection
    {
        public long CaptureCallbackCount { get; init; }
        public double CaptureCallbackAvgIntervalMs { get; init; }
        public double CaptureCallbackMaxIntervalMs { get; init; }
        public long CaptureCallbackSevereGapCount { get; init; }
        public long CaptureAudioDiscontinuityCount { get; init; }
        public long CaptureAudioTimestampErrorCount { get; init; }
        public long CaptureAudioGlitchCount { get; init; }
        public int CaptureCallbackSilenceCount { get; init; }
        public long CaptureLastCallbackTickMs { get; init; }
        public long CaptureAudioLevelEventsFired { get; init; }
        public long CaptureAudioLevelLastFireTickMs { get; init; }
        public long PlaybackRenderCallbackCount { get; init; }
        public int PlaybackRenderSilenceCount { get; init; }
        public int PlaybackQueueDepth { get; init; }
        public int PlaybackQueueDropCount { get; init; }
        public double PlaybackQueueDurationMs { get; init; }
        public double PlaybackActiveChunkDurationMs { get; init; }
        public double PlaybackEndpointQueuedDurationMs { get; init; }
        public double PlaybackBufferedDurationMs { get; init; }
        public double PlaybackStreamLatencyMs { get; init; }
        public long PlaybackLastRenderTickMs { get; init; }
        public string BufferHealthStatus { get; init; }
        public string BufferHealthReason { get; init; }
        public bool BufferUnderrunDetected { get; init; }
        public bool BufferOverrunDetected { get; init; }
        public long BufferUnderrunEvents { get; init; }
        public long BufferOverrunEvents { get; init; }
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

    private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(
        PreviewRuntimeSnapshot previewRuntime,
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),
            Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),
            Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),
            Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),
            Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)
        };

    private readonly record struct PreviewRuntimeProjection
    {
        public PreviewRuntimeFrameProjection Frame { get; init; }
        public PreviewRuntimeCadenceProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceProjection Surface { get; init; }
        public PreviewRuntimeStartupProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorProjection Color { get; init; }
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

    private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.DisplayCadenceSampleCount,
            ObservedFps = previewRuntime.DisplayCadenceObservedFps,
            ExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            AverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            P95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            P99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            MaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            FivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            SampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
            JitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            SlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent
        };

    private readonly record struct PreviewRuntimeCadenceProjection
    {
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SlowFrameCount { get; init; }
        public double SlowFramePercent { get; init; }
    }

    private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            HdrInputDetected = previewHdrState.InputDetected,
            ToneMapMode = previewHdrState.ToneMapMode,
            ColorContext = captureRuntime.NegotiatedPixelFormat,
            AdapterColorMetadata = captureRuntime.PreviewColorMetadata
        };

    private readonly record struct PreviewRuntimeColorProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            GpuActive = previewRuntime.GpuActive,
            PlaceholderVisible = previewRuntime.PlaceholderVisible,
            GpuElementVisible = previewRuntime.GpuElementVisible,
            CpuElementVisible = previewRuntime.CpuElementVisible,
            RendererAttached = previewRuntime.RendererAttached
        };

    private readonly record struct PreviewRuntimeSurfaceProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
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

    private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            PlaybackState = previewRuntime.GpuPlaybackState,
            NaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            NaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            PositionMs = previewRuntime.GpuPositionMs,
            PositionEventCount = previewRuntime.GpuPositionEventCount
        };

    private readonly record struct PreviewRuntimeGpuPlaybackProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private static PreviewD3DProjection BuildPreviewD3DProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);
        var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);
        var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);
        var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);
        var frameStats = BuildPreviewD3DFrameStatsProjection(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return new()
        {
            PresentSyncInterval = previewRuntime.D3DPresentSyncInterval,
            MaxFrameLatency = previewRuntime.D3DMaxFrameLatency,
            SwapChainBufferCount = previewRuntime.D3DSwapChainBufferCount,
            SwapChainAddress = previewRuntime.D3DSwapChainAddress,
            FramesSubmitted = previewRuntime.D3DFramesSubmitted,
            FramesRendered = previewRuntime.D3DFramesRendered,
            FramesDropped = previewRuntime.D3DFramesDropped,
            RenderThreadFailureCount = previewRuntime.D3DRenderThreadFailureCount,
            LastRenderThreadFailureType = previewRuntime.D3DLastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewRuntime.D3DLastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewRuntime.D3DLastRenderThreadFailureHResult,
            PendingFrameCount = previewRuntime.D3DPendingFrameCount,
            InputColorSpace = previewRuntime.D3DInputColorSpace,
            OutputColorSpace = previewRuntime.D3DOutputColorSpace,
            CpuTiming = cpuTiming,
            FrameLatencyWait = frameLatencyWait,
            PipelineLatency = pipelineLatency,
            FrameStats = frameStats,
            FrameFlow = frameFlow
        };
    }

    private readonly record struct PreviewD3DProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingProjection CpuTiming { get; init; }
        public PreviewD3DFrameLatencyWaitProjection FrameLatencyWait { get; init; }
        public PreviewD3DPipelineLatencyProjection PipelineLatency { get; init; }
        public PreviewD3DFrameStatsProjection FrameStats { get; init; }
        public PreviewD3DFrameFlowProjection FrameFlow { get; init; }
    }

    private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DCpuTimingSampleCount,
            InputUploadAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            InputUploadP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            InputUploadMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            RenderSubmitAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            RenderSubmitP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            RenderSubmitP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            RenderSubmitMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            TotalFrameAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            TotalFrameP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            TotalFrameP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            TotalFrameMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadAvgMs { get; init; }
        public double InputUploadP95Ms { get; init; }
        public double InputUploadP99Ms { get; init; }
        public double InputUploadMaxMs { get; init; }
        public double RenderSubmitAvgMs { get; init; }
        public double RenderSubmitP95Ms { get; init; }
        public double RenderSubmitP99Ms { get; init; }
        public double RenderSubmitMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameAvgMs { get; init; }
        public double TotalFrameP95Ms { get; init; }
        public double TotalFrameP99Ms { get; init; }
        public double TotalFrameMaxMs { get; init; }
    }

    private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            AvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            P95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            P99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            MaxMs = previewRuntime.D3DPipelineLatencyMaxMs
        };

    private readonly record struct PreviewD3DPipelineLatencyProjection
    {
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }

    private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            HandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            CallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            SignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            UnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            LastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            LastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            SampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            AvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            P95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            P99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs
        };

    private readonly record struct PreviewD3DFrameLatencyWaitProjection
    {
        public bool Enabled { get; init; }
        public bool HandleActive { get; init; }
        public long CallCount { get; init; }
        public long SignaledCount { get; init; }
        public long TimeoutCount { get; init; }
        public long UnexpectedResultCount { get; init; }
        public uint LastResult { get; init; }
        public double LastMs { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }

    private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
        => new()
        {
            SampleCount = previewRuntime.D3DFrameStatsSampleCount,
            SuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            FailureCount = previewRuntime.D3DFrameStatsFailureCount,
            LastError = previewRuntime.D3DFrameStatsLastError,
            PresentCount = previewRuntime.D3DFrameStatsPresentCount,
            PresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            SyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            SyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            LastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            LastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            LastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            MissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            RecentMissedRefreshCount = recentD3DMissedRefreshes,
            RecentFailureCount = recentD3DStatsFailures
        };

    private readonly record struct PreviewD3DFrameStatsProjection
    {
        public long SampleCount { get; init; }
        public long SuccessCount { get; init; }
        public long FailureCount { get; init; }
        public string LastError { get; init; }
        public long PresentCount { get; init; }
        public long PresentRefreshCount { get; init; }
        public long SyncRefreshCount { get; init; }
        public long SyncQpcTime { get; init; }
        public long LastPresentDelta { get; init; }
        public long LastPresentRefreshDelta { get; init; }
        public long LastSyncRefreshDelta { get; init; }
        public long MissedRefreshCount { get; init; }
        public long RecentMissedRefreshCount { get; init; }
        public long RecentFailureCount { get; init; }
    }

    private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            LastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            LastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            LastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            LastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            LastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            LastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            LastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            LastDropReason = previewRuntime.D3DLastDropReason,
            RecentSlowFrames = previewRuntime.D3DRecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowProjection
    {
        public long LastSubmittedPreviewPresentId { get; init; }
        public long LastSubmittedSourceSequenceNumber { get; init; }
        public long LastSubmittedSourcePtsTicks { get; init; }
        public long LastSubmittedQpc { get; init; }
        public long LastSubmittedUtcUnixMs { get; init; }
        public long LastRenderedPreviewPresentId { get; init; }
        public long LastRenderedSourceSequenceNumber { get; init; }
        public long LastRenderedSourcePtsTicks { get; init; }
        public long LastRenderedQpc { get; init; }
        public long LastRenderedUtcUnixMs { get; init; }
        public double LastRenderedSchedulerToPresentMs { get; init; }
        public double LastRenderedPipelineLatencyMs { get; init; }
        public long LastDroppedPreviewPresentId { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDroppedSourcePtsTicks { get; init; }
        public long LastDroppedQpc { get; init; }
        public long LastDroppedUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }
    }

}
