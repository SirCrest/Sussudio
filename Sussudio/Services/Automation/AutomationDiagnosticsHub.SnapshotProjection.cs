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
        var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);
        var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);
        var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);
        var audioDrops = BuildAudioDropsProjection(health);
        var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);
        var userSettings = BuildUserSettingsProjection(viewModelSnapshot);
        var recordingSettings = BuildRecordingSettingsProjection(userSettings);
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
            recordingSettings,
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
        RecordingSettingsProjection RecordingSettings,
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
        var flattened = BuildAutomationSnapshotFlattenedProjectionSet(projections);
        return BuildAutomationSnapshotFromFlattenedProjections(flattened);
    }

    private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(
        AutomationSnapshotProjectionSet projections)
    {
        var snapshotStatus = projections.SnapshotStatus;
        var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);
        var snapshotEvaluation = projections.SnapshotEvaluation;
        var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);
        var audioAndIngest = projections.AudioAndIngest;
        var audioAndIngestFlattening = BuildAudioAndIngestFlattenedProjection(audioAndIngest);
        var audioDrops = projections.AudioDrops;
        var audioDropsFlattening = BuildAudioDropsFlattenedProjection(audioDrops);
        var captureCommands = projections.CaptureCommands;
        var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);
        var userSettings = projections.UserSettings;
        var recordingSettings = projections.RecordingSettings;
        var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);
        var recordingIntegrity = projections.RecordingIntegrity;
        var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);
        var captureFormat = projections.CaptureFormat;
        var sourceSignal = projections.SourceSignal;
        var sourceTelemetry = projections.SourceTelemetry;
        var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);
        var recordingOutput = projections.RecordingOutput;
        var processResourceProjection = projections.ProcessResourceProjection;
        var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);
        var avSync = projections.AvSync;
        var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);
        var captureTransport = projections.CaptureTransport;
        var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);
        var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);
        var previewSummary = projections.PreviewSummary;
        var recordingBackend = projections.RecordingBackend;
        var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);
        var recordingPipeline = projections.RecordingPipeline;
        var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);
        var captureCadence = projections.CaptureCadence;
        var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);
        var visualCadence = projections.VisualCadence;
        var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);
        var mjpeg = projections.Mjpeg;
        var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);
        var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);
        var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);
        var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);
        var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);
        var previewD3D = projections.PreviewD3D;
        var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);
        var hdrPipeline = projections.HdrPipeline;
        var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);
        var flashbackExport = projections.FlashbackExport;
        var flashbackExportLastResult = projections.FlashbackExportLastResult;
        var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(
            flashbackExport,
            flashbackExportLastResult);
        var flashbackRecording = projections.FlashbackRecording;
        var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);
        var flashbackPlayback = projections.FlashbackPlayback;
        var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);

        return new AutomationSnapshotFlattenedProjectionSet(
            snapshotStatusFlattening,
            snapshotEvaluationFlattening,
            audioAndIngestFlattening,
            audioDropsFlattening,
            captureCommandFlattening,
            settingsFlattening,
            recordingIntegrityFlattening,
            sourceFlattening,
            processResourceFlattening,
            avSyncFlattening,
            captureTransportFlattening,
            captureFormatFlattening,
            recordingOutputFlattening,
            recordingPipelineFlattening,
            captureCadenceFlattening,
            visualCadenceFlattening,
            mjpegFlattening,
            mjpegTimingFlattening,
            mjpegPreviewJitterFlattening,
            mjpegPacketHashFlattening,
            previewRuntimeFlattening,
            previewD3DFlattening,
            hdrPipelineFlattening,
            flashbackExportFlattening,
            flashbackRecordingFlattening,
            flashbackPlaybackFlattening);
    }

    private readonly record struct AutomationSnapshotFlattenedProjectionSet(
        SnapshotStatusFlattenedProjection SnapshotStatus,
        SnapshotEvaluationFlattenedProjection SnapshotEvaluation,
        AudioAndIngestFlattenedProjection AudioAndIngest,
        AudioDropsFlattenedProjection AudioDrops,
        CaptureCommandFlattenedProjection CaptureCommand,
        SettingsFlattenedProjection Settings,
        RecordingIntegrityFlattenedProjection RecordingIntegrity,
        SourceFlattenedProjection Source,
        ProcessResourceFlattenedProjection ProcessResource,
        AvSyncFlattenedProjection AvSync,
        CaptureTransportFlattenedProjection CaptureTransport,
        CaptureFormatFlattenedProjection CaptureFormat,
        RecordingOutputFlattenedProjection RecordingOutput,
        RecordingPipelineFlattenedProjection RecordingPipeline,
        CaptureCadenceFlattenedProjection CaptureCadence,
        VisualCadenceFlattenedProjection VisualCadence,
        MjpegFlattenedProjection Mjpeg,
        MjpegTimingFlattenedProjection MjpegTiming,
        MjpegPreviewJitterFlattenedProjection MjpegPreviewJitter,
        MjpegPacketHashFlattenedProjection MjpegPacketHash,
        PreviewRuntimeFlattenedProjection PreviewRuntime,
        PreviewD3DFlattenedProjection PreviewD3D,
        HdrPipelineFlattenedProjection HdrPipeline,
        FlashbackExportFlattenedProjection FlashbackExport,
        FlashbackRecordingFlattenedProjection FlashbackRecording,
        FlashbackPlaybackFlattenedProjection FlashbackPlayback);

    private SnapshotStatusProjection BuildSnapshotStatusProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            TimestampUtc = DateTimeOffset.UtcNow,
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

    private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(
        SnapshotStatusProjection snapshotStatus)
        => new()
        {
            TimestampUtc = snapshotStatus.TimestampUtc,
            IsInitialized = snapshotStatus.IsInitialized,
            IsPreviewing = snapshotStatus.IsPreviewing,
            IsRecording = snapshotStatus.IsRecording,
            VerificationInProgress = snapshotStatus.VerificationInProgress,
            IsAudioEnabled = snapshotStatus.IsAudioEnabled,
            IsAudioPreviewEnabled = snapshotStatus.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = snapshotStatus.IsCustomAudioInputEnabled,
            SessionState = snapshotStatus.SessionState,
            StatusText = snapshotStatus.StatusText
        };

    private readonly record struct SnapshotStatusFlattenedProjection
    {
        public DateTimeOffset TimestampUtc { get; init; }
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

    private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(
        SnapshotEvaluationProjection snapshotEvaluation)
        => new()
        {
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
            PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,
            PerformanceThresholdCaptureP95Multiplier = snapshotEvaluation.PerformanceThresholdCaptureP95Multiplier,
            PerformanceThresholdPreviewSlowPercent = snapshotEvaluation.PerformanceThresholdPreviewSlowPercent,
            PerformanceThresholdVerificationDropPercent = snapshotEvaluation.PerformanceThresholdVerificationDropPercent
        };

    private readonly record struct SnapshotEvaluationFlattenedProjection
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

    private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)
        => new()
        {
            SelectedRecordingFormat = userSettings.SelectedRecordingFormat,
            SelectedQuality = userSettings.SelectedQuality,
            SelectedPreset = userSettings.SelectedPreset,
            SelectedSplitEncodeMode = userSettings.SelectedSplitEncodeMode,
            SelectedVideoFormat = userSettings.SelectedVideoFormat,
            CustomBitrateMbps = userSettings.CustomBitrateMbps
        };

    private readonly record struct RecordingSettingsProjection
    {
        public string SelectedRecordingFormat { get; init; }
        public string SelectedQuality { get; init; }
        public string SelectedPreset { get; init; }
        public string SelectedSplitEncodeMode { get; init; }
        public string SelectedVideoFormat { get; init; }
        public double CustomBitrateMbps { get; init; }
    }

    private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(
        UserSettingsProjection userSettings,
        RecordingSettingsProjection recordingSettings)
        => new()
        {
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
            SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,
            SelectedQuality = recordingSettings.SelectedQuality,
            SelectedPreset = recordingSettings.SelectedPreset,
            SelectedSplitEncodeMode = recordingSettings.SelectedSplitEncodeMode,
            SelectedVideoFormat = recordingSettings.SelectedVideoFormat,
            CustomBitrateMbps = recordingSettings.CustomBitrateMbps,
            PreviewVolumePercent = userSettings.PreviewVolumePercent,
            IsStatsVisible = userSettings.IsStatsVisible
        };

    private readonly record struct SettingsFlattenedProjection
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

    private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(
        ProcessResourceProjection processResourceProjection)
        => new()
        {
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
            ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax
        };

    private readonly record struct ProcessResourceFlattenedProjection
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

    private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)
        => new()
        {
            CaptureDriftMs = avSync.CaptureDriftMs,
            CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,
            EncoderDriftMs = avSync.EncoderDriftMs,
            EncoderCorrectionSamples = avSync.EncoderCorrectionSamples
        };

    private readonly record struct AvSyncProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }

    private readonly record struct AvSyncFlattenedProjection
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

    private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(
        CaptureCadenceProjection captureCadence)
        => new()
        {
            ExpectedFrameRate = captureCadence.ExpectedFrameRate,
            SampleCount = captureCadence.SampleCount,
            ObservedFps = captureCadence.ObservedFps,
            ExpectedIntervalMs = captureCadence.ExpectedIntervalMs,
            AverageIntervalMs = captureCadence.AverageIntervalMs,
            P95IntervalMs = captureCadence.P95IntervalMs,
            P99IntervalMs = captureCadence.P99IntervalMs,
            MaxIntervalMs = captureCadence.MaxIntervalMs,
            OnePercentLowFps = captureCadence.OnePercentLowFps,
            FivePercentLowFps = captureCadence.FivePercentLowFps,
            SampleDurationMs = captureCadence.SampleDurationMs,
            RecentIntervalsMs = captureCadence.RecentIntervalsMs,
            JitterStdDevMs = captureCadence.JitterStdDevMs,
            SevereGapCount = captureCadence.SevereGapCount,
            EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,
            EstimatedDropPercent = captureCadence.EstimatedDropPercent
        };

    private readonly record struct CaptureCadenceFlattenedProjection
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

    private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(
        VisualCadenceProjection visualCadence)
        => new()
        {
            SampleCount = visualCadence.SampleCount,
            ChangedFrameCount = visualCadence.ChangedFrameCount,
            RepeatFrameCount = visualCadence.RepeatFrameCount,
            LongestRepeatRun = visualCadence.LongestRepeatRun,
            OutputObservedFps = visualCadence.OutputObservedFps,
            ChangeObservedFps = visualCadence.ChangeObservedFps,
            RepeatFramePercent = visualCadence.RepeatFramePercent,
            LastDelta = visualCadence.LastDelta,
            AverageDelta = visualCadence.AverageDelta,
            P95Delta = visualCadence.P95Delta,
            MotionScore = visualCadence.MotionScore,
            MotionConfidence = visualCadence.MotionConfidence,
            RecentOutputIntervalsMs = visualCadence.RecentOutputIntervalsMs,
            RecentChangeIntervalsMs = visualCadence.RecentChangeIntervalsMs,
            CenterSampleCount = visualCadence.CenterSampleCount,
            CenterChangedFrameCount = visualCadence.CenterChangedFrameCount,
            CenterRepeatFrameCount = visualCadence.CenterRepeatFrameCount,
            CenterLongestRepeatRun = visualCadence.CenterLongestRepeatRun,
            CenterOutputObservedFps = visualCadence.CenterOutputObservedFps,
            CenterChangeObservedFps = visualCadence.CenterChangeObservedFps,
            CenterRepeatFramePercent = visualCadence.CenterRepeatFramePercent,
            CenterLastDelta = visualCadence.CenterLastDelta,
            CenterAverageDelta = visualCadence.CenterAverageDelta,
            CenterP95Delta = visualCadence.CenterP95Delta,
            CenterMotionScore = visualCadence.CenterMotionScore,
            CenterMotionConfidence = visualCadence.CenterMotionConfidence,
            CenterRecentOutputIntervalsMs = visualCadence.CenterRecentOutputIntervalsMs,
            CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs
        };

    private readonly record struct VisualCadenceFlattenedProjection
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

    private static SourceFlattenedProjection BuildSourceFlattenedProjection(
        SourceSignalProjection sourceSignal,
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
            Signal = BuildSourceSignalFlattenedProjection(sourceSignal),
            Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)
        };

    private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(
        SourceSignalProjection sourceSignal)
        => new()
        {
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
            SourceRawTimingHex = sourceSignal.RawTimingHex
        };

    private readonly record struct SourceSignalFlattenedProjection
    {
        public double? DetectedSourceFrameRate { get; init; }
        public string? DetectedSourceFrameRateArg { get; init; }
        public string SourceFrameRateOrigin { get; init; }
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
    }

    private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
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
            SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText
        };

    private readonly record struct SourceTelemetryFlattenedProjection
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

    private readonly record struct SourceFlattenedProjection
    {
        public SourceSignalFlattenedProjection Signal { get; init; }
        public SourceTelemetryFlattenedProjection Telemetry { get; init; }
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

    private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(
        CaptureCommandProjection captureCommands)
        => new()
        {
            CommandsEnqueued = captureCommands.CommandsEnqueued,
            CommandsCompleted = captureCommands.CommandsCompleted,
            CommandsFailed = captureCommands.CommandsFailed,
            CommandsCanceled = captureCommands.CommandsCanceled,
            CommandsCoalesced = captureCommands.CommandsCoalesced,
            PendingCommands = captureCommands.PendingCommands,
            MaxPendingCommands = captureCommands.MaxPendingCommands,
            OldestPendingCommandAgeMs = captureCommands.OldestPendingCommandAgeMs,
            LastQueueLatencyMs = captureCommands.LastQueueLatencyMs,
            MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,
            LastCommand = captureCommands.LastCommand,
            LastOutcome = captureCommands.LastOutcome,
            LastCorrelationId = captureCommands.LastCorrelationId,
            LastError = captureCommands.LastError
        };

    private readonly record struct CaptureCommandFlattenedProjection
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

    private static AutomationSnapshot BuildAutomationSnapshotFromFlattenedProjections(
        AutomationSnapshotFlattenedProjectionSet flattened)
    {
        var snapshotStatusFlattening = flattened.SnapshotStatus;
        var snapshotEvaluationFlattening = flattened.SnapshotEvaluation;
        var audioAndIngestFlattening = flattened.AudioAndIngest;
        var audioDropsFlattening = flattened.AudioDrops;
        var captureCommandFlattening = flattened.CaptureCommand;
        var settingsFlattening = flattened.Settings;
        var recordingIntegrityFlattening = flattened.RecordingIntegrity;
        var sourceFlattening = flattened.Source;
        var processResourceFlattening = flattened.ProcessResource;
        var avSyncFlattening = flattened.AvSync;
        var captureTransportFlattening = flattened.CaptureTransport;
        var captureFormatFlattening = flattened.CaptureFormat;
        var recordingOutputFlattening = flattened.RecordingOutput;
        var recordingPipelineFlattening = flattened.RecordingPipeline;
        var captureCadenceFlattening = flattened.CaptureCadence;
        var visualCadenceFlattening = flattened.VisualCadence;
        var mjpegFlattening = flattened.Mjpeg;
        var mjpegTimingFlattening = flattened.MjpegTiming;
        var mjpegPreviewJitterFlattening = flattened.MjpegPreviewJitter;
        var mjpegPacketHashFlattening = flattened.MjpegPacketHash;
        var previewRuntimeFlattening = flattened.PreviewRuntime;
        var previewD3DFlattening = flattened.PreviewD3D;
        var hdrPipelineFlattening = flattened.HdrPipeline;
        var flashbackExportFlattening = flattened.FlashbackExport;
        var flashbackRecordingFlattening = flattened.FlashbackRecording;
        var flashbackPlaybackFlattening = flattened.FlashbackPlayback;

        return new AutomationSnapshot
        {
            TimestampUtc = snapshotStatusFlattening.TimestampUtc,
            IsInitialized = snapshotStatusFlattening.IsInitialized,
            IsPreviewing = snapshotStatusFlattening.IsPreviewing,
            IsRecording = snapshotStatusFlattening.IsRecording,
            VerificationInProgress = snapshotStatusFlattening.VerificationInProgress,
            IsAudioEnabled = snapshotStatusFlattening.IsAudioEnabled,
            IsAudioPreviewEnabled = snapshotStatusFlattening.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = snapshotStatusFlattening.IsCustomAudioInputEnabled,
            SessionState = snapshotStatusFlattening.SessionState,
            StatusText = snapshotStatusFlattening.StatusText,
            PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,
            PerformancePerfectionMet = snapshotEvaluationFlattening.PerformancePerfectionMet,
            PerformanceSummary = snapshotEvaluationFlattening.PerformanceSummary,
            DiagnosticHealthStatus = snapshotEvaluationFlattening.DiagnosticHealthStatus,
            DiagnosticLikelyStage = snapshotEvaluationFlattening.DiagnosticLikelyStage,
            DiagnosticSummary = snapshotEvaluationFlattening.DiagnosticSummary,
            DiagnosticEvidence = snapshotEvaluationFlattening.DiagnosticEvidence,
            DiagnosticSourceLane = snapshotEvaluationFlattening.DiagnosticSourceLane,
            DiagnosticDecodeLane = snapshotEvaluationFlattening.DiagnosticDecodeLane,
            DiagnosticPreviewLane = snapshotEvaluationFlattening.DiagnosticPreviewLane,
            DiagnosticRenderLane = snapshotEvaluationFlattening.DiagnosticRenderLane,
            DiagnosticPresentLane = snapshotEvaluationFlattening.DiagnosticPresentLane,
            DiagnosticRecordingLane = snapshotEvaluationFlattening.DiagnosticRecordingLane,
            DiagnosticAudioLane = snapshotEvaluationFlattening.DiagnosticAudioLane,
            PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage,
            PreviewPacingSlowStageConfidence = snapshotEvaluationFlattening.PreviewPacingSlowStageConfidence,
            PreviewPacingSlowStageEvidence = snapshotEvaluationFlattening.PreviewPacingSlowStageEvidence,
            CaptureCommandCommandsEnqueued = captureCommandFlattening.CommandsEnqueued,
            CaptureCommandCommandsCompleted = captureCommandFlattening.CommandsCompleted,
            CaptureCommandCommandsFailed = captureCommandFlattening.CommandsFailed,
            CaptureCommandCommandsCanceled = captureCommandFlattening.CommandsCanceled,
            CaptureCommandCommandsCoalesced = captureCommandFlattening.CommandsCoalesced,
            CaptureCommandPendingCommands = captureCommandFlattening.PendingCommands,
            CaptureCommandMaxPendingCommands = captureCommandFlattening.MaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = captureCommandFlattening.OldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = captureCommandFlattening.LastQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = captureCommandFlattening.MaxQueueLatencyMs,
            CaptureCommandLastCommand = captureCommandFlattening.LastCommand,
            CaptureCommandLastOutcome = captureCommandFlattening.LastOutcome,
            CaptureCommandLastCorrelationId = captureCommandFlattening.LastCorrelationId,
            CaptureCommandLastError = captureCommandFlattening.LastError,
            PerformanceThresholdCaptureDropPercent = snapshotEvaluationFlattening.PerformanceThresholdCaptureDropPercent,
            PerformanceThresholdCaptureP95Multiplier = snapshotEvaluationFlattening.PerformanceThresholdCaptureP95Multiplier,
            PerformanceThresholdPreviewSlowPercent = snapshotEvaluationFlattening.PerformanceThresholdPreviewSlowPercent,
            PerformanceThresholdVerificationDropPercent = snapshotEvaluationFlattening.PerformanceThresholdVerificationDropPercent,
            SelectedDeviceId = settingsFlattening.SelectedDeviceId,
            SelectedDeviceName = settingsFlattening.SelectedDeviceName,
            SelectedAudioInputDeviceId = settingsFlattening.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = settingsFlattening.SelectedAudioInputDeviceName,
            SelectedResolution = settingsFlattening.SelectedResolution,
            SelectedFrameRate = settingsFlattening.SelectedFrameRate,
            SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,
            SelectedExactFrameRate = settingsFlattening.SelectedExactFrameRate,
            SelectedExactFrameRateArg = settingsFlattening.SelectedExactFrameRateArg,
            DisabledResolutionReason = settingsFlattening.DisabledResolutionReason,
            DisabledFrameRateReason = settingsFlattening.DisabledFrameRateReason,
            DetectedSourceFrameRate = sourceFlattening.Signal.DetectedSourceFrameRate,
            DetectedSourceFrameRateArg = sourceFlattening.Signal.DetectedSourceFrameRateArg,
            SourceFrameRateOrigin = sourceFlattening.Signal.SourceFrameRateOrigin,
            SourceWidth = sourceFlattening.Signal.SourceWidth,
            SourceHeight = sourceFlattening.Signal.SourceHeight,
            SourceIsHdr = sourceFlattening.Signal.SourceIsHdr,
            SourceVideoFormat = sourceFlattening.Signal.SourceVideoFormat,
            SourceColorimetry = sourceFlattening.Signal.SourceColorimetry,
            SourceQuantization = sourceFlattening.Signal.SourceQuantization,
            SourceHdrTransferFunction = sourceFlattening.Signal.SourceHdrTransferFunction,
            SourceHdrTransferCode = sourceFlattening.Signal.SourceHdrTransferCode,
            SourceFirmware = sourceFlattening.Signal.SourceFirmware,
            SourceAudioFormat = sourceFlattening.Signal.SourceAudioFormat,
            SourceAudioSampleRate = sourceFlattening.Signal.SourceAudioSampleRate,
            SourceInputSource = sourceFlattening.Signal.SourceInputSource,
            SourceUsbHostProtocol = sourceFlattening.Signal.SourceUsbHostProtocol,
            SourceHdcpMode = sourceFlattening.Signal.SourceHdcpMode,
            SourceHdcpVersion = sourceFlattening.Signal.SourceHdcpVersion,
            SourceRxTxHdcpVersion = sourceFlattening.Signal.SourceRxTxHdcpVersion,
            SourceRawTimingHex = sourceFlattening.Signal.SourceRawTimingHex,
            SourceTelemetryAvailability = sourceFlattening.Telemetry.SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = sourceFlattening.Telemetry.SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = sourceFlattening.Telemetry.SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = sourceFlattening.Telemetry.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = sourceFlattening.Telemetry.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = sourceFlattening.Telemetry.SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = sourceFlattening.Telemetry.SourceTelemetryAgeSeconds,
            SourceTelemetryBackend = sourceFlattening.Telemetry.SourceTelemetryBackend,
            SourceTelemetrySuppressed = sourceFlattening.Telemetry.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = sourceFlattening.Telemetry.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = sourceFlattening.Telemetry.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = sourceFlattening.Telemetry.SourceTelemetrySummaryText,
            SourceTargetSummaryText = sourceFlattening.Telemetry.SourceTargetSummaryText,
            SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,
            SelectedQuality = settingsFlattening.SelectedQuality,
            SelectedPreset = settingsFlattening.SelectedPreset,
            SelectedSplitEncodeMode = settingsFlattening.SelectedSplitEncodeMode,
            SelectedVideoFormat = settingsFlattening.SelectedVideoFormat,
            CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,
            PreviewVolumePercent = settingsFlattening.PreviewVolumePercent,
            IsStatsVisible = settingsFlattening.IsStatsVisible,
            IsHdrAvailable = hdrPipelineFlattening.IsHdrAvailable,
            IsHdrEnabled = hdrPipelineFlattening.IsHdrEnabled,
            HdrOutputActive = hdrPipelineFlattening.HdrOutputActive,
            HdrRuntimeState = hdrPipelineFlattening.HdrRuntimeState,
            HdrReadinessReason = hdrPipelineFlattening.HdrReadinessReason,
            HdrWarmupState = hdrPipelineFlattening.HdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrPipelineFlattening.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = hdrPipelineFlattening.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = hdrPipelineFlattening.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = hdrPipelineFlattening.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = hdrPipelineFlattening.HdrDowngradeCode,
            RequestedPipelineMode = hdrPipelineFlattening.RequestedPipelineMode,
            ActivePipelineMode = hdrPipelineFlattening.ActivePipelineMode,
            PipelineModeMatched = hdrPipelineFlattening.PipelineModeMatched,
            PipelineModeStatus = hdrPipelineFlattening.PipelineModeStatus,
            PipelineModeReason = hdrPipelineFlattening.PipelineModeReason,
            TelemetryAlignmentStatus = hdrPipelineFlattening.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = hdrPipelineFlattening.TelemetryAlignmentReason,
            OutputPath = recordingOutputFlattening.OutputPath,
            RecordingTime = recordingOutputFlattening.RecordingTime,
            RecordingSizeInfo = recordingOutputFlattening.RecordingSizeInfo,
            RecordingBitrateInfo = recordingOutputFlattening.RecordingBitrateInfo,
            AudioPeak = audioAndIngestFlattening.Signal.Peak,
            AudioClipping = audioAndIngestFlattening.Signal.Clipping,
            AudioSignalPresent = audioAndIngestFlattening.Signal.SignalPresent,
            AudioMutedSuspected = audioAndIngestFlattening.Signal.MutedSuspected,
            AudioReaderActive = audioAndIngestFlattening.Ingest.AudioReaderActive,
            AudioFramesArrived = audioAndIngestFlattening.Ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = audioAndIngestFlattening.Ingest.AudioFramesWrittenToSink,
            VideoReaderActive = audioAndIngestFlattening.Ingest.VideoReaderActive,
            IngestVideoFramesArrived = audioAndIngestFlattening.Ingest.VideoFramesArrived,
            IngestVideoFramesWrittenToSink = audioAndIngestFlattening.Ingest.VideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = audioAndIngestFlattening.Ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = audioAndIngestFlattening.Ingest.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = audioAndIngestFlattening.SourceReader.FramesDelivered,
            MfSourceReaderFramesDropped = audioAndIngestFlattening.SourceReader.FramesDropped,
            MfSourceReaderNegotiatedFormat = audioAndIngestFlattening.SourceReader.NegotiatedFormat,
            SourceReaderReadOutstanding = audioAndIngestFlattening.SourceReader.ReadOutstanding,
            SourceReaderReadOutstandingMs = audioAndIngestFlattening.SourceReader.ReadOutstandingMs,
            SourceReaderLastFrameTickMs = audioAndIngestFlattening.SourceReader.LastFrameTickMs,
            SourceReaderFrameChannelDepth = audioAndIngestFlattening.SourceReader.FrameChannelDepth,
            WasapiCaptureCallbackCount = audioAndIngestFlattening.WasapiCapture.CallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = audioAndIngestFlattening.WasapiCapture.CallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = audioAndIngestFlattening.WasapiCapture.CallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = audioAndIngestFlattening.WasapiCapture.CallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = audioAndIngestFlattening.WasapiCapture.AudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = audioAndIngestFlattening.WasapiCapture.AudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = audioAndIngestFlattening.WasapiCapture.AudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = audioAndIngestFlattening.WasapiCapture.CallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = audioAndIngestFlattening.WasapiCapture.LastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = audioAndIngestFlattening.WasapiCapture.AudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = audioAndIngestFlattening.WasapiCapture.AudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = audioAndIngestFlattening.WasapiPlayback.RenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = audioAndIngestFlattening.WasapiPlayback.RenderSilenceCount,
            WasapiPlaybackQueueDepth = audioAndIngestFlattening.WasapiPlayback.QueueDepth,
            WasapiPlaybackQueueDropCount = audioAndIngestFlattening.WasapiPlayback.QueueDropCount,
            WasapiPlaybackQueueDurationMs = audioAndIngestFlattening.WasapiPlayback.QueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = audioAndIngestFlattening.WasapiPlayback.ActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = audioAndIngestFlattening.WasapiPlayback.EndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = audioAndIngestFlattening.WasapiPlayback.BufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = audioAndIngestFlattening.WasapiPlayback.StreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = audioAndIngestFlattening.WasapiPlayback.LastRenderTickMs,
            MemoryPreference = captureTransportFlattening.MemoryPreference,
            VideoRequestedSubtype = captureTransportFlattening.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureTransportFlattening.FrameLedgerCapacity,
            FrameLedgerEventCount = captureTransportFlattening.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureTransportFlattening.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,
            PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,
            EncoderVideoFramesEnqueued = recordingPipelineFlattening.Encoder.VideoFramesEnqueued,
            EncoderVideoFramesEncoded = recordingPipelineFlattening.Encoder.VideoFramesEncoded,
            EncoderLastEnqueueAgeMs = recordingPipelineFlattening.Encoder.LastEnqueueAgeMs,
            EncoderLastWriteAgeMs = recordingPipelineFlattening.Encoder.LastWriteAgeMs,
            RecordingBackend = recordingOutputFlattening.Backend,
            AudioPathMode = recordingOutputFlattening.AudioPathMode,
            MuxResult = recordingOutputFlattening.MuxResult,
            RecordingIntegrityStatus = recordingIntegrityFlattening.Summary.Status,
            RecordingIntegrityComplete = recordingIntegrityFlattening.Summary.Complete,
            RecordingIntegrityBackend = recordingIntegrityFlattening.Summary.Backend,
            RecordingIntegrityCompletedUtc = recordingIntegrityFlattening.Summary.CompletedUtc,
            RecordingIntegritySourceFrames = recordingIntegrityFlattening.Video.SourceFrames,
            RecordingIntegrityAcceptedFrames = recordingIntegrityFlattening.Video.AcceptedFrames,
            RecordingIntegrityPipelineDroppedFrames = recordingIntegrityFlattening.Video.PipelineDroppedFrames,
            RecordingIntegrityQueueDroppedFrames = recordingIntegrityFlattening.Video.QueueDroppedFrames,
            RecordingIntegritySubmittedFrames = recordingIntegrityFlattening.Video.SubmittedFrames,
            RecordingIntegrityEncodedFrames = recordingIntegrityFlattening.Video.EncodedFrames,
            RecordingIntegrityPacketsWritten = recordingIntegrityFlattening.Video.PacketsWritten,
            RecordingIntegrityEncoderDroppedFrames = recordingIntegrityFlattening.Video.EncoderDroppedFrames,
            RecordingIntegritySequenceGaps = recordingIntegrityFlattening.Video.SequenceGaps,
            RecordingIntegrityQueueMaxDepth = recordingIntegrityFlattening.Backpressure.QueueMaxDepth,
            RecordingIntegrityQueueOldestFrameAgeMs = recordingIntegrityFlattening.Backpressure.QueueOldestFrameAgeMs,
            RecordingIntegrityBackpressureWaitMs = recordingIntegrityFlattening.Backpressure.BackpressureWaitMs,
            RecordingIntegrityBackpressureEvents = recordingIntegrityFlattening.Backpressure.BackpressureEvents,
            RecordingIntegrityBackpressureMaxWaitMs = recordingIntegrityFlattening.Backpressure.BackpressureMaxWaitMs,
            RecordingIntegrityAudioStatus = recordingIntegrityFlattening.Audio.AudioStatus,
            RecordingIntegrityAudioEnabled = recordingIntegrityFlattening.Audio.AudioEnabled,
            RecordingIntegrityAudioCaptureActive = recordingIntegrityFlattening.Audio.AudioCaptureActive,
            RecordingIntegrityAudioFramesArrived = recordingIntegrityFlattening.Audio.AudioFramesArrived,
            RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.Audio.AudioFramesWrittenToSink,
            RecordingIntegrityAudioSamplesEncoded = recordingIntegrityFlattening.Audio.AudioSamplesEncoded,
            RecordingIntegrityAudioDropEvents = recordingIntegrityFlattening.Audio.AudioDropEvents,
            RecordingIntegrityAudioDiscontinuities = recordingIntegrityFlattening.Audio.AudioDiscontinuities,
            RecordingIntegrityAudioTimestampErrors = recordingIntegrityFlattening.Audio.AudioTimestampErrors,
            RecordingIntegrityAudioCallbackGaps = recordingIntegrityFlattening.Audio.AudioCallbackGaps,
            RecordingIntegrityAvSyncDriftMs = recordingIntegrityFlattening.AvSync.AvSyncDriftMs,
            RecordingIntegrityAvSyncDriftRateMsPerSec = recordingIntegrityFlattening.AvSync.AvSyncDriftRateMsPerSec,
            RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.AvSync.EncoderAvSyncDriftMs,
            RecordingIntegrityEncoderAvSyncCorrectionSamples = recordingIntegrityFlattening.AvSync.EncoderAvSyncCorrectionSamples,
            RecordingIntegrityReason = recordingIntegrityFlattening.Summary.Reason,
            RequestedWidth = captureFormatFlattening.Requested.Width,
            RequestedHeight = captureFormatFlattening.Requested.Height,
            RequestedFrameRate = captureFormatFlattening.Requested.FrameRate,
            RequestedFrameRateArg = captureFormatFlattening.Requested.FrameRateArg,
            RequestedFrameRateNumerator = captureFormatFlattening.Requested.FrameRateNumerator,
            RequestedFrameRateDenominator = captureFormatFlattening.Requested.FrameRateDenominator,
            RequestedPixelFormat = captureFormatFlattening.Requested.PixelFormat,
            RequestedFormat = captureFormatFlattening.Requested.Format,
            RequestedQuality = captureFormatFlattening.Requested.Quality,
            RequestedHdrEnabled = captureFormatFlattening.Requested.HdrEnabled,
            RequestedHdrMasteringMetadata = captureFormatFlattening.Requested.HdrMasteringMetadata,
            RequestedAudioEnabled = captureFormatFlattening.Requested.AudioEnabled,
            HdrActivationReason = captureFormatFlattening.HdrRequest.ActivationReason,
            HdrAutoDowngraded = captureFormatFlattening.HdrRequest.AutoDowngraded,
            HdrAutoDowngradeReason = captureFormatFlattening.HdrRequest.AutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureFormatFlattening.HdrRequest.RequestedButSourceNot10Bit,
            ActualWidth = captureFormatFlattening.Actual.Width,
            ActualHeight = captureFormatFlattening.Actual.Height,
            ActualFrameRate = captureFormatFlattening.Actual.FrameRate,
            ActualFrameRateArg = captureFormatFlattening.Actual.FrameRateArg,
            NegotiatedWidth = captureFormatFlattening.Negotiated.Width,
            NegotiatedHeight = captureFormatFlattening.Negotiated.Height,
            NegotiatedFrameRate = captureFormatFlattening.Negotiated.FrameRate,
            NegotiatedFrameRateArg = captureFormatFlattening.Negotiated.FrameRateArg,
            NegotiatedFrameRateNumerator = captureFormatFlattening.Negotiated.FrameRateNumerator,
            NegotiatedFrameRateDenominator = captureFormatFlattening.Negotiated.FrameRateDenominator,
            NegotiatedPixelFormat = captureFormatFlattening.Negotiated.PixelFormat,
            RequestedReaderSubtype = captureFormatFlattening.ReaderObservation.RequestedReaderSubtype,
            ReaderSourceStreamType = captureFormatFlattening.ReaderObservation.ReaderSourceStreamType,
            ReaderSourceSubtype = captureFormatFlattening.ReaderObservation.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureFormatFlattening.ReaderObservation.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureFormatFlattening.ReaderObservation.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureFormatFlattening.ReaderObservation.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureFormatFlattening.ReaderObservation.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureFormatFlattening.ReaderObservation.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureFormatFlattening.ReaderObservation.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureFormatFlattening.ReaderObservation.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureFormatFlattening.Encoder.InputPixelFormat,
            EncoderOutputPixelFormat = captureFormatFlattening.Encoder.OutputPixelFormat,
            EncoderVideoCodec = captureFormatFlattening.Encoder.VideoCodec,
            EncoderVideoProfile = captureFormatFlattening.Encoder.VideoProfile,
            EncoderTenBitPipelineConfirmed = captureFormatFlattening.Encoder.TenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureFormatFlattening.ReaderObservation.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = captureFormatFlattening.Negotiated.MediaSubtypeToken,
            PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,
            PreviewFramesDisplayed = previewRuntimeFlattening.Frame.FramesDisplayed,
            PreviewFramesDropped = previewRuntimeFlattening.Frame.FramesDropped,
            PreviewCadenceSampleCount = previewRuntimeFlattening.Cadence.SampleCount,
            PreviewCadenceObservedFps = previewRuntimeFlattening.Cadence.ObservedFps,
            PreviewCadenceExpectedIntervalMs = previewRuntimeFlattening.Cadence.ExpectedIntervalMs,
            PreviewCadenceAverageIntervalMs = previewRuntimeFlattening.Cadence.AverageIntervalMs,
            PreviewCadenceP95IntervalMs = previewRuntimeFlattening.Cadence.P95IntervalMs,
            PreviewCadenceP99IntervalMs = previewRuntimeFlattening.Cadence.P99IntervalMs,
            PreviewCadenceMaxIntervalMs = previewRuntimeFlattening.Cadence.MaxIntervalMs,
            PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,
            PreviewCadenceFivePercentLowFps = previewRuntimeFlattening.Cadence.FivePercentLowFps,
            PreviewCadenceSampleDurationMs = previewRuntimeFlattening.Cadence.SampleDurationMs,
            PreviewCadenceRecentIntervalsMs = previewRuntimeFlattening.Cadence.RecentIntervalsMs,
            PreviewCadenceJitterStdDevMs = previewRuntimeFlattening.Cadence.JitterStdDevMs,
            PreviewCadenceSlowFrameCount = previewRuntimeFlattening.Cadence.SlowFrameCount,
            PreviewCadenceSlowFramePercent = previewRuntimeFlattening.Cadence.SlowFramePercent,
            PreviewGpuActive = previewRuntimeFlattening.Surface.GpuActive,
            PreviewPlaceholderVisible = previewRuntimeFlattening.Surface.PlaceholderVisible,
            PreviewGpuElementVisible = previewRuntimeFlattening.Surface.GpuElementVisible,
            PreviewCpuElementVisible = previewRuntimeFlattening.Surface.CpuElementVisible,
            PreviewRendererAttached = previewRuntimeFlattening.Surface.RendererAttached,
            PreviewStartupState = previewRuntimeFlattening.Startup.State,
            PreviewAttemptId = previewRuntimeFlattening.Startup.AttemptId,
            PreviewStartupElapsedMs = previewRuntimeFlattening.Startup.ElapsedMs,
            PreviewStartupTimeoutMs = previewRuntimeFlattening.Startup.TimeoutMs,
            PreviewGpuSignalMediaOpened = previewRuntimeFlattening.Startup.GpuSignalMediaOpened,
            PreviewGpuSignalFirstFrame = previewRuntimeFlattening.Startup.GpuSignalFirstFrame,
            PreviewGpuSignalPlaybackAdvancing = previewRuntimeFlattening.Startup.GpuSignalPlaybackAdvancing,
            PreviewStartupRequiredSignals = previewRuntimeFlattening.Startup.RequiredSignals,
            PreviewStartupReceivedSignals = previewRuntimeFlattening.Startup.ReceivedSignals,
            PreviewStartupStrategy = previewRuntimeFlattening.Startup.Strategy,
            PreviewStartupMissingSignals = previewRuntimeFlattening.Startup.MissingSignals,
            PreviewRecoveryAttemptCount = previewRuntimeFlattening.Startup.RecoveryAttemptCount,
            PreviewLastFailureReason = previewRuntimeFlattening.Startup.LastFailureReason,
            PreviewFirstVisualConfirmed = previewRuntimeFlattening.Startup.FirstVisualConfirmed,
            PreviewBlankSuspected = previewRuntimeFlattening.Startup.BlankSuspected,
            PreviewStalled = previewRuntimeFlattening.Startup.Stalled,
            PreviewRendererMode = previewRuntimeFlattening.Startup.RendererMode,
            PreviewD3DPresentSyncInterval = previewD3DFlattening.PresentSyncInterval,
            PreviewD3DMaxFrameLatency = previewD3DFlattening.MaxFrameLatency,
            PreviewD3DSwapChainBufferCount = previewD3DFlattening.SwapChainBufferCount,
            PreviewD3DSwapChainAddress = previewD3DFlattening.SwapChainAddress,
            PreviewD3DFramesSubmitted = previewD3DFlattening.FramesSubmitted,
            PreviewD3DFramesRendered = previewD3DFlattening.FramesRendered,
            PreviewD3DFramesDropped = previewD3DFlattening.FramesDropped,
            PreviewD3DRenderThreadFailureCount = previewD3DFlattening.RenderThreadFailureCount,
            PreviewD3DLastRenderThreadFailureType = previewD3DFlattening.LastRenderThreadFailureType,
            PreviewD3DLastRenderThreadFailureMessage = previewD3DFlattening.LastRenderThreadFailureMessage,
            PreviewD3DLastRenderThreadFailureHResult = previewD3DFlattening.LastRenderThreadFailureHResult,
            PreviewD3DPendingFrameCount = previewD3DFlattening.PendingFrameCount,
            PreviewD3DInputColorSpace = previewD3DFlattening.InputColorSpace,
            PreviewD3DOutputColorSpace = previewD3DFlattening.OutputColorSpace,
            PreviewD3DCpuTimingSampleCount = previewD3DFlattening.CpuTiming.SampleCount,
            PreviewD3DInputUploadCpuAvgMs = previewD3DFlattening.CpuTiming.InputUploadCpuAvgMs,
            PreviewD3DInputUploadCpuP95Ms = previewD3DFlattening.CpuTiming.InputUploadCpuP95Ms,
            PreviewD3DInputUploadCpuP99Ms = previewD3DFlattening.CpuTiming.InputUploadCpuP99Ms,
            PreviewD3DInputUploadCpuMaxMs = previewD3DFlattening.CpuTiming.InputUploadCpuMaxMs,
            PreviewD3DRenderSubmitCpuAvgMs = previewD3DFlattening.CpuTiming.RenderSubmitCpuAvgMs,
            PreviewD3DRenderSubmitCpuP95Ms = previewD3DFlattening.CpuTiming.RenderSubmitCpuP95Ms,
            PreviewD3DRenderSubmitCpuP99Ms = previewD3DFlattening.CpuTiming.RenderSubmitCpuP99Ms,
            PreviewD3DRenderSubmitCpuMaxMs = previewD3DFlattening.CpuTiming.RenderSubmitCpuMaxMs,
            PreviewD3DPresentCallAvgMs = previewD3DFlattening.CpuTiming.PresentCallAvgMs,
            PreviewD3DPresentCallP95Ms = previewD3DFlattening.CpuTiming.PresentCallP95Ms,
            PreviewD3DPresentCallP99Ms = previewD3DFlattening.CpuTiming.PresentCallP99Ms,
            PreviewD3DPresentCallMaxMs = previewD3DFlattening.CpuTiming.PresentCallMaxMs,
            PreviewD3DTotalFrameCpuAvgMs = previewD3DFlattening.CpuTiming.TotalFrameCpuAvgMs,
            PreviewD3DTotalFrameCpuP95Ms = previewD3DFlattening.CpuTiming.TotalFrameCpuP95Ms,
            PreviewD3DTotalFrameCpuP99Ms = previewD3DFlattening.CpuTiming.TotalFrameCpuP99Ms,
            PreviewD3DTotalFrameCpuMaxMs = previewD3DFlattening.CpuTiming.TotalFrameCpuMaxMs,
            PreviewD3DPipelineLatencySampleCount = previewD3DFlattening.LatencyAndStats.PipelineLatencySampleCount,
            PreviewD3DPipelineLatencyAvgMs = previewD3DFlattening.LatencyAndStats.PipelineLatencyAvgMs,
            PreviewD3DPipelineLatencyP95Ms = previewD3DFlattening.LatencyAndStats.PipelineLatencyP95Ms,
            PreviewD3DPipelineLatencyP99Ms = previewD3DFlattening.LatencyAndStats.PipelineLatencyP99Ms,
            PreviewD3DPipelineLatencyMaxMs = previewD3DFlattening.LatencyAndStats.PipelineLatencyMaxMs,
            PreviewD3DFrameLatencyWaitEnabled = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitEnabled,
            PreviewD3DFrameLatencyWaitHandleActive = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitHandleActive,
            PreviewD3DFrameLatencyWaitCallCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitCallCount,
            PreviewD3DFrameLatencyWaitSignaledCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitSignaledCount,
            PreviewD3DFrameLatencyWaitTimeoutCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitTimeoutCount,
            PreviewD3DFrameLatencyWaitUnexpectedResultCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitUnexpectedResultCount,
            PreviewD3DFrameLatencyWaitLastResult = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitLastResult,
            PreviewD3DFrameLatencyWaitLastMs = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitLastMs,
            PreviewD3DFrameLatencyWaitSampleCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitSampleCount,
            PreviewD3DFrameLatencyWaitAvgMs = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitAvgMs,
            PreviewD3DFrameLatencyWaitP95Ms = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitP95Ms,
            PreviewD3DFrameLatencyWaitP99Ms = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitP99Ms,
            PreviewD3DFrameLatencyWaitMaxMs = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitMaxMs,
            PreviewD3DFrameStatsSampleCount = previewD3DFlattening.LatencyAndStats.FrameStatsSampleCount,
            PreviewD3DFrameStatsSuccessCount = previewD3DFlattening.LatencyAndStats.FrameStatsSuccessCount,
            PreviewD3DFrameStatsFailureCount = previewD3DFlattening.LatencyAndStats.FrameStatsFailureCount,
            PreviewD3DFrameStatsLastError = previewD3DFlattening.LatencyAndStats.FrameStatsLastError,
            PreviewD3DFrameStatsPresentCount = previewD3DFlattening.LatencyAndStats.FrameStatsPresentCount,
            PreviewD3DFrameStatsPresentRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsPresentRefreshCount,
            PreviewD3DFrameStatsSyncRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsSyncRefreshCount,
            PreviewD3DFrameStatsSyncQpcTime = previewD3DFlattening.LatencyAndStats.FrameStatsSyncQpcTime,
            PreviewD3DFrameStatsLastPresentDelta = previewD3DFlattening.LatencyAndStats.FrameStatsLastPresentDelta,
            PreviewD3DFrameStatsLastPresentRefreshDelta = previewD3DFlattening.LatencyAndStats.FrameStatsLastPresentRefreshDelta,
            PreviewD3DFrameStatsLastSyncRefreshDelta = previewD3DFlattening.LatencyAndStats.FrameStatsLastSyncRefreshDelta,
            PreviewD3DFrameStatsMissedRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsMissedRefreshCount,
            PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsRecentMissedRefreshCount,
            PreviewD3DFrameStatsRecentFailureCount = previewD3DFlattening.LatencyAndStats.FrameStatsRecentFailureCount,
            PreviewD3DLastSubmittedPreviewPresentId = previewD3DFlattening.FrameFlow.LastSubmittedPreviewPresentId,
            PreviewD3DLastSubmittedSourceSequenceNumber = previewD3DFlattening.FrameFlow.LastSubmittedSourceSequenceNumber,
            PreviewD3DLastSubmittedSourcePtsTicks = previewD3DFlattening.FrameFlow.LastSubmittedSourcePtsTicks,
            PreviewD3DLastSubmittedQpc = previewD3DFlattening.FrameFlow.LastSubmittedQpc,
            PreviewD3DLastSubmittedUtcUnixMs = previewD3DFlattening.FrameFlow.LastSubmittedUtcUnixMs,
            PreviewD3DLastRenderedPreviewPresentId = previewD3DFlattening.FrameFlow.LastRenderedPreviewPresentId,
            PreviewD3DLastRenderedSourceSequenceNumber = previewD3DFlattening.FrameFlow.LastRenderedSourceSequenceNumber,
            PreviewD3DLastRenderedSourcePtsTicks = previewD3DFlattening.FrameFlow.LastRenderedSourcePtsTicks,
            PreviewD3DLastRenderedQpc = previewD3DFlattening.FrameFlow.LastRenderedQpc,
            PreviewD3DLastRenderedUtcUnixMs = previewD3DFlattening.FrameFlow.LastRenderedUtcUnixMs,
            PreviewD3DLastRenderedSchedulerToPresentMs = previewD3DFlattening.FrameFlow.LastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = previewD3DFlattening.FrameFlow.LastRenderedPipelineLatencyMs,
            PreviewD3DLastDroppedPreviewPresentId = previewD3DFlattening.FrameFlow.LastDroppedPreviewPresentId,
            PreviewD3DLastDroppedSourceSequenceNumber = previewD3DFlattening.FrameFlow.LastDroppedSourceSequenceNumber,
            PreviewD3DLastDroppedSourcePtsTicks = previewD3DFlattening.FrameFlow.LastDroppedSourcePtsTicks,
            PreviewD3DLastDroppedQpc = previewD3DFlattening.FrameFlow.LastDroppedQpc,
            PreviewD3DLastDroppedUtcUnixMs = previewD3DFlattening.FrameFlow.LastDroppedUtcUnixMs,
            PreviewD3DLastDropReason = previewD3DFlattening.FrameFlow.LastDropReason,
            PreviewD3DRecentSlowFrames = previewD3DFlattening.FrameFlow.RecentSlowFrames,
            PreviewGpuPlaybackState = previewRuntimeFlattening.GpuPlayback.PlaybackState,
            PreviewGpuNaturalVideoWidth = previewRuntimeFlattening.GpuPlayback.NaturalVideoWidth,
            PreviewGpuNaturalVideoHeight = previewRuntimeFlattening.GpuPlayback.NaturalVideoHeight,
            PreviewGpuPositionMs = previewRuntimeFlattening.GpuPlayback.PositionMs,
            PreviewGpuPositionEventCount = previewRuntimeFlattening.GpuPlayback.PositionEventCount,
            PreviewHdrInputDetected = previewRuntimeFlattening.Color.HdrInputDetected,
            PreviewToneMapMode = previewRuntimeFlattening.Color.ToneMapMode,
            PreviewColorContext = previewRuntimeFlattening.Color.ColorContext,
            ConversionQueueDepth = recordingPipelineFlattening.Ingest.ConversionQueueDepth,
            FfmpegVideoQueueDepth = recordingPipelineFlattening.Ingest.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = recordingPipelineFlattening.Ingest.FfmpegAudioQueueDepth,
            VideoFramesArrived = recordingPipelineFlattening.Ingest.VideoFramesArrived,
            VideoFramesQueued = recordingPipelineFlattening.Ingest.VideoFramesQueued,
            VideoFramesDropped = recordingPipelineFlattening.Ingest.VideoFramesDropped,
            VideoFramesDroppedBacklog = recordingPipelineFlattening.Ingest.VideoFramesDroppedBacklog,
            VideoFramesConverted = recordingPipelineFlattening.Ingest.VideoFramesConverted,
            VideoFramesEnqueued = recordingPipelineFlattening.Ingest.VideoFramesEnqueued,
            VideoDropsQueueSaturated = recordingPipelineFlattening.Ingest.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = recordingPipelineFlattening.Ingest.VideoDropsBacklogEviction,
            RecordingEncodingFailed = recordingPipelineFlattening.Encoder.EncodingFailed,
            RecordingEncodingFailureType = recordingPipelineFlattening.Encoder.EncodingFailureType,
            RecordingEncodingFailureMessage = recordingPipelineFlattening.Encoder.EncodingFailureMessage,
            RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,
            RecordingVideoQueueMaxDepth = recordingPipelineFlattening.VideoQueue.MaxDepth,
            RecordingVideoFramesSubmittedToEncoder = recordingPipelineFlattening.VideoQueue.FramesSubmittedToEncoder,
            RecordingVideoEncoderPts = recordingPipelineFlattening.VideoQueue.EncoderPts,
            RecordingVideoEncoderPacketsWritten = recordingPipelineFlattening.VideoQueue.EncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = recordingPipelineFlattening.VideoQueue.EncoderDroppedFrames,
            RecordingVideoSequenceGaps = recordingPipelineFlattening.VideoQueue.SequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = recordingPipelineFlattening.VideoQueue.OldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = recordingPipelineFlattening.VideoQueue.LastLatencyMs,
            RecordingVideoQueueLatencySampleCount = recordingPipelineFlattening.VideoQueue.LatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = recordingPipelineFlattening.VideoQueue.LatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = recordingPipelineFlattening.VideoQueue.LatencyP95Ms,
            RecordingVideoQueueLatencyP99Ms = recordingPipelineFlattening.VideoQueue.LatencyP99Ms,
            RecordingVideoQueueLatencyMaxMs = recordingPipelineFlattening.VideoQueue.LatencyMaxMs,
            RecordingVideoBackpressureWaitMs = recordingPipelineFlattening.VideoQueue.BackpressureWaitMs,
            RecordingVideoBackpressureEvents = recordingPipelineFlattening.VideoQueue.BackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = recordingPipelineFlattening.VideoQueue.BackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = recordingPipelineFlattening.VideoQueue.BackpressureMaxWaitMs,
            RecordingGpuQueueDepth = recordingPipelineFlattening.HardwareQueues.GpuQueueDepth,
            RecordingGpuQueueCapacity = recordingPipelineFlattening.HardwareQueues.GpuQueueCapacity,
            RecordingGpuQueueMaxDepth = recordingPipelineFlattening.HardwareQueues.GpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = recordingPipelineFlattening.HardwareQueues.GpuFramesEnqueued,
            RecordingGpuFramesDropped = recordingPipelineFlattening.HardwareQueues.GpuFramesDropped,
            RecordingCudaQueueDepth = recordingPipelineFlattening.HardwareQueues.CudaQueueDepth,
            RecordingCudaQueueCapacity = recordingPipelineFlattening.HardwareQueues.CudaQueueCapacity,
            RecordingCudaQueueMaxDepth = recordingPipelineFlattening.HardwareQueues.CudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = recordingPipelineFlattening.HardwareQueues.CudaFramesEnqueued,
            RecordingCudaFramesDropped = recordingPipelineFlattening.HardwareQueues.CudaFramesDropped,
            FlashbackEncodingFailed = flashbackRecordingFlattening.EncodingFailed,
            FlashbackEncodingFailureType = flashbackRecordingFlattening.EncodingFailureType,
            FlashbackEncodingFailureMessage = flashbackRecordingFlattening.EncodingFailureMessage,
            FatalCleanupInProgress = flashbackRecordingFlattening.FatalCleanupInProgress,
            FlashbackCleanupInProgress = flashbackRecordingFlattening.CleanupInProgress,
            FlashbackForceRotateActive = flashbackRecordingFlattening.ForceRotateActive,
            FlashbackForceRotateRequested = flashbackRecordingFlattening.ForceRotateRequested,
            FlashbackForceRotateDraining = flashbackRecordingFlattening.ForceRotateDraining,
            FlashbackTempDriveFreeBytes = flashbackRecordingFlattening.StartupCache.TempDriveFreeBytes,
            FlashbackStartupCacheBudgetBytes = flashbackRecordingFlattening.StartupCache.BudgetBytes,
            FlashbackStartupCacheBytes = flashbackRecordingFlattening.StartupCache.Bytes,
            FlashbackStartupCacheSessionCount = flashbackRecordingFlattening.StartupCache.SessionCount,
            FlashbackStartupCacheDeletedSessionCount = flashbackRecordingFlattening.StartupCache.DeletedSessionCount,
            FlashbackStartupCacheFreedBytes = flashbackRecordingFlattening.StartupCache.FreedBytes,
            FlashbackStartupCacheOverBudget = flashbackRecordingFlattening.StartupCache.OverBudget,
            FlashbackVideoQueueCapacity = flashbackRecordingFlattening.Queues.VideoQueueCapacity,
            FlashbackVideoQueueMaxDepth = flashbackRecordingFlattening.Queues.VideoQueueMaxDepth,
            FlashbackVideoFramesSubmittedToEncoder = flashbackRecordingFlattening.Queues.VideoFramesSubmittedToEncoder,
            FlashbackVideoEncoderPts = flashbackRecordingFlattening.Queues.VideoEncoderPts,
            FlashbackVideoEncoderPacketsWritten = flashbackRecordingFlattening.Queues.VideoEncoderPacketsWritten,
            FlashbackVideoEncoderDroppedFrames = flashbackRecordingFlattening.Queues.VideoEncoderDroppedFrames,
            FlashbackVideoSequenceGaps = flashbackRecordingFlattening.Queues.VideoSequenceGaps,
            FlashbackVideoQueueRejectedFrames = flashbackRecordingFlattening.Queues.VideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = flashbackRecordingFlattening.Queues.VideoQueueLastRejectReason,
            FlashbackVideoQueueOldestFrameAgeMs = flashbackRecordingFlattening.Queues.VideoQueueOldestFrameAgeMs,
            FlashbackVideoQueueLastLatencyMs = flashbackRecordingFlattening.Queues.VideoQueueLastLatencyMs,
            FlashbackVideoQueueLatencySampleCount = flashbackRecordingFlattening.Queues.VideoQueueLatencySampleCount,
            FlashbackVideoQueueLatencyAvgMs = flashbackRecordingFlattening.Queues.VideoQueueLatencyAvgMs,
            FlashbackVideoQueueLatencyP95Ms = flashbackRecordingFlattening.Queues.VideoQueueLatencyP95Ms,
            FlashbackVideoQueueLatencyP99Ms = flashbackRecordingFlattening.Queues.VideoQueueLatencyP99Ms,
            FlashbackVideoQueueLatencyMaxMs = flashbackRecordingFlattening.Queues.VideoQueueLatencyMaxMs,
            FlashbackVideoBackpressureWaitMs = flashbackRecordingFlattening.Queues.VideoBackpressureWaitMs,
            FlashbackVideoBackpressureEvents = flashbackRecordingFlattening.Queues.VideoBackpressureEvents,
            FlashbackVideoBackpressureLastWaitMs = flashbackRecordingFlattening.Queues.VideoBackpressureLastWaitMs,
            FlashbackVideoBackpressureMaxWaitMs = flashbackRecordingFlattening.Queues.VideoBackpressureMaxWaitMs,
            FlashbackGpuQueueDepth = flashbackRecordingFlattening.Queues.GpuQueueDepth,
            FlashbackGpuQueueCapacity = flashbackRecordingFlattening.Queues.GpuQueueCapacity,
            FlashbackGpuQueueMaxDepth = flashbackRecordingFlattening.Queues.GpuQueueMaxDepth,
            FlashbackGpuFramesEnqueued = flashbackRecordingFlattening.Queues.GpuFramesEnqueued,
            FlashbackGpuFramesDropped = flashbackRecordingFlattening.Queues.GpuFramesDropped,
            FlashbackGpuQueueRejectedFrames = flashbackRecordingFlattening.Queues.GpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = flashbackRecordingFlattening.Queues.GpuQueueLastRejectReason,
            AudioDropsQueueSaturated = audioDropsFlattening.QueueSaturated,
            AudioDropsBacklogEviction = audioDropsFlattening.BacklogEviction,
            AudioChunksDropped = audioDropsFlattening.ChunksDropped,
            AudioQueueDropsRealtime = audioDropsFlattening.QueueDropsRealtime,
            AudioQueueDropsFileWriter = audioDropsFlattening.QueueDropsFileWriter,
            EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,
            ExpectedCaptureFrameRate = captureCadenceFlattening.ExpectedFrameRate,
            CaptureCadenceSampleCount = captureCadenceFlattening.SampleCount,
            CaptureCadenceObservedFps = captureCadenceFlattening.ObservedFps,
            CaptureCadenceExpectedIntervalMs = captureCadenceFlattening.ExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = captureCadenceFlattening.AverageIntervalMs,
            CaptureCadenceP95IntervalMs = captureCadenceFlattening.P95IntervalMs,
            CaptureCadenceP99IntervalMs = captureCadenceFlattening.P99IntervalMs,
            CaptureCadenceMaxIntervalMs = captureCadenceFlattening.MaxIntervalMs,
            CaptureCadenceOnePercentLowFps = captureCadenceFlattening.OnePercentLowFps,
            CaptureCadenceFivePercentLowFps = captureCadenceFlattening.FivePercentLowFps,
            CaptureCadenceSampleDurationMs = captureCadenceFlattening.SampleDurationMs,
            CaptureCadenceRecentIntervalsMs = captureCadenceFlattening.RecentIntervalsMs,
            CaptureCadenceJitterStdDevMs = captureCadenceFlattening.JitterStdDevMs,
            CaptureCadenceSevereGapCount = captureCadenceFlattening.SevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = captureCadenceFlattening.EstimatedDropPercent,
            MjpegDecodeSampleCount = mjpegTimingFlattening.DecodeSampleCount,
            MjpegDecodeAvgMs = mjpegTimingFlattening.DecodeAvgMs,
            MjpegDecodeP95Ms = mjpegTimingFlattening.DecodeP95Ms,
            MjpegDecodeMaxMs = mjpegTimingFlattening.DecodeMaxMs,
            MjpegInteropCopySampleCount = mjpegTimingFlattening.InteropCopySampleCount,
            MjpegInteropCopyAvgMs = mjpegTimingFlattening.InteropCopyAvgMs,
            MjpegInteropCopyP95Ms = mjpegTimingFlattening.InteropCopyP95Ms,
            MjpegInteropCopyMaxMs = mjpegTimingFlattening.InteropCopyMaxMs,
            MjpegCallbackSampleCount = mjpegTimingFlattening.CallbackSampleCount,
            MjpegCallbackAvgMs = mjpegTimingFlattening.CallbackAvgMs,
            MjpegCallbackP95Ms = mjpegTimingFlattening.CallbackP95Ms,
            MjpegCallbackMaxMs = mjpegTimingFlattening.CallbackMaxMs,
            MjpegDecoderCount = mjpegTimingFlattening.DecoderCount,
            MjpegReorderSampleCount = mjpegTimingFlattening.ReorderSampleCount,
            MjpegReorderAvgMs = mjpegTimingFlattening.ReorderAvgMs,
            MjpegReorderP95Ms = mjpegTimingFlattening.ReorderP95Ms,
            MjpegReorderMaxMs = mjpegTimingFlattening.ReorderMaxMs,
            MjpegPipelineSampleCount = mjpegTimingFlattening.PipelineSampleCount,
            MjpegPipelineAvgMs = mjpegTimingFlattening.PipelineAvgMs,
            MjpegPipelineP95Ms = mjpegTimingFlattening.PipelineP95Ms,
            MjpegPipelineMaxMs = mjpegTimingFlattening.PipelineMaxMs,
            MjpegTotalDecoded = mjpegFlattening.TotalDecoded,
            MjpegTotalEmitted = mjpegFlattening.TotalEmitted,
            MjpegTotalDropped = mjpegFlattening.TotalDropped,
            MjpegCompressedFramesQueued = mjpegFlattening.CompressedFramesQueued,
            MjpegCompressedFramesDequeued = mjpegFlattening.CompressedFramesDequeued,
            MjpegCompressedDropsQueueFull = mjpegFlattening.CompressedDropsQueueFull,
            MjpegCompressedDropsByteBudget = mjpegFlattening.CompressedDropsByteBudget,
            MjpegCompressedDropsDisposed = mjpegFlattening.CompressedDropsDisposed,
            MjpegDecodeFailures = mjpegFlattening.DecodeFailures,
            MjpegReorderCollisions = mjpegFlattening.ReorderCollisions,
            MjpegEmitFailures = mjpegFlattening.EmitFailures,
            MjpegCompressedQueueDepth = mjpegFlattening.CompressedQueueDepth,
            MjpegCompressedQueueBytes = mjpegFlattening.CompressedQueueBytes,
            MjpegCompressedQueueByteBudget = mjpegFlattening.CompressedQueueByteBudget,
            MjpegReorderSkips = mjpegFlattening.ReorderSkips,
            MjpegReorderBufferDepth = mjpegFlattening.ReorderBufferDepth,
            MjpegPreviewJitterEnabled = mjpegPreviewJitterFlattening.Queue.Enabled,
            MjpegPreviewJitterTargetDepth = mjpegPreviewJitterFlattening.Queue.TargetDepth,
            MjpegPreviewJitterMaxDepth = mjpegPreviewJitterFlattening.Queue.MaxDepth,
            MjpegPreviewJitterQueueDepth = mjpegPreviewJitterFlattening.Queue.QueueDepth,
            MjpegPreviewJitterTotalQueued = mjpegPreviewJitterFlattening.Queue.TotalQueued,
            MjpegPreviewJitterTotalSubmitted = mjpegPreviewJitterFlattening.Queue.TotalSubmitted,
            MjpegPreviewJitterTotalDropped = mjpegPreviewJitterFlattening.Queue.TotalDropped,
            MjpegPreviewJitterUnderflowCount = mjpegPreviewJitterFlattening.Queue.UnderflowCount,
            MjpegPreviewJitterResumeReprimeCount = mjpegPreviewJitterFlattening.Queue.ResumeReprimeCount,
            MjpegPreviewJitterInputSampleCount = mjpegPreviewJitterFlattening.Timing.InputSampleCount,
            MjpegPreviewJitterInputAvgMs = mjpegPreviewJitterFlattening.Timing.InputAvgMs,
            MjpegPreviewJitterInputP95Ms = mjpegPreviewJitterFlattening.Timing.InputP95Ms,
            MjpegPreviewJitterInputMaxMs = mjpegPreviewJitterFlattening.Timing.InputMaxMs,
            MjpegPreviewJitterOutputSampleCount = mjpegPreviewJitterFlattening.Timing.OutputSampleCount,
            MjpegPreviewJitterOutputAvgMs = mjpegPreviewJitterFlattening.Timing.OutputAvgMs,
            MjpegPreviewJitterOutputP95Ms = mjpegPreviewJitterFlattening.Timing.OutputP95Ms,
            MjpegPreviewJitterOutputMaxMs = mjpegPreviewJitterFlattening.Timing.OutputMaxMs,
            MjpegPreviewJitterLatencySampleCount = mjpegPreviewJitterFlattening.Timing.LatencySampleCount,
            MjpegPreviewJitterLatencyAvgMs = mjpegPreviewJitterFlattening.Timing.LatencyAvgMs,
            MjpegPreviewJitterLatencyP95Ms = mjpegPreviewJitterFlattening.Timing.LatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = mjpegPreviewJitterFlattening.Timing.LatencyMaxMs,
            MjpegPreviewJitterDeadlineDropCount = mjpegPreviewJitterFlattening.Adaptive.DeadlineDropCount,
            MjpegPreviewJitterClearedDropCount = mjpegPreviewJitterFlattening.Adaptive.ClearedDropCount,
            MjpegPreviewJitterTargetIncreaseCount = mjpegPreviewJitterFlattening.Adaptive.TargetIncreaseCount,
            MjpegPreviewJitterTargetDecreaseCount = mjpegPreviewJitterFlattening.Adaptive.TargetDecreaseCount,
            MjpegPreviewJitterLastSelectedPreviewPresentId = mjpegPreviewJitterFlattening.Events.LastSelectedPreviewPresentId,
            MjpegPreviewJitterLastSelectedSourceSequenceNumber = mjpegPreviewJitterFlattening.Events.LastSelectedSourceSequenceNumber,
            MjpegPreviewJitterLastSelectedQpc = mjpegPreviewJitterFlattening.Events.LastSelectedQpc,
            MjpegPreviewJitterLastSelectedSourceLatencyMs = mjpegPreviewJitterFlattening.Events.LastSelectedSourceLatencyMs,
            MjpegPreviewJitterLastDroppedSourceSequenceNumber = mjpegPreviewJitterFlattening.Events.LastDroppedSourceSequenceNumber,
            MjpegPreviewJitterLastDropQpc = mjpegPreviewJitterFlattening.Events.LastDropQpc,
            MjpegPreviewJitterLastDropReason = mjpegPreviewJitterFlattening.Events.LastDropReason,
            MjpegPreviewJitterLastUnderflowQpc = mjpegPreviewJitterFlattening.Events.LastUnderflowQpc,
            MjpegPreviewJitterLastUnderflowReason = mjpegPreviewJitterFlattening.Events.LastUnderflowReason,
            MjpegPreviewJitterLastUnderflowQueueDepth = mjpegPreviewJitterFlattening.Events.LastUnderflowQueueDepth,
            MjpegPreviewJitterLastUnderflowInputAgeMs = mjpegPreviewJitterFlattening.Events.LastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = mjpegPreviewJitterFlattening.Events.LastUnderflowOutputAgeMs,
            MjpegPreviewJitterLastScheduleLateMs = mjpegPreviewJitterFlattening.Events.LastScheduleLateMs,
            MjpegPreviewJitterMaxScheduleLateMs = mjpegPreviewJitterFlattening.Events.MaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = mjpegPreviewJitterFlattening.Events.ScheduleLateCount,
            MjpegPacketHashSampleCount = mjpegPacketHashFlattening.SampleCount,
            MjpegPacketHashUniqueFrameCount = mjpegPacketHashFlattening.UniqueFrameCount,
            MjpegPacketHashDuplicateFrameCount = mjpegPacketHashFlattening.DuplicateFrameCount,
            MjpegPacketHashLongestDuplicateRun = mjpegPacketHashFlattening.LongestDuplicateRun,
            MjpegPacketHashInputObservedFps = mjpegPacketHashFlattening.InputObservedFps,
            MjpegPacketHashUniqueObservedFps = mjpegPacketHashFlattening.UniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = mjpegPacketHashFlattening.DuplicateFramePercent,
            MjpegPacketHashLastHash = mjpegPacketHashFlattening.LastHash,
            MjpegPacketHashLastFrameDuplicate = mjpegPacketHashFlattening.LastFrameDuplicate,
            MjpegPacketHashPattern = mjpegPacketHashFlattening.Pattern,
            MjpegPacketHashRecentInputIntervalsMs = mjpegPacketHashFlattening.RecentInputIntervalsMs,
            MjpegPacketHashRecentUniqueIntervalsMs = mjpegPacketHashFlattening.RecentUniqueIntervalsMs,
            MjpegPacketHashRecentDuplicateFlags = mjpegPacketHashFlattening.RecentDuplicateFlags,
            VisualCadenceSampleCount = visualCadenceFlattening.SampleCount,
            VisualCadenceChangedFrameCount = visualCadenceFlattening.ChangedFrameCount,
            VisualCadenceRepeatFrameCount = visualCadenceFlattening.RepeatFrameCount,
            VisualCadenceLongestRepeatRun = visualCadenceFlattening.LongestRepeatRun,
            VisualCadenceOutputObservedFps = visualCadenceFlattening.OutputObservedFps,
            VisualCadenceChangeObservedFps = visualCadenceFlattening.ChangeObservedFps,
            VisualCadenceRepeatFramePercent = visualCadenceFlattening.RepeatFramePercent,
            VisualCadenceLastDelta = visualCadenceFlattening.LastDelta,
            VisualCadenceAverageDelta = visualCadenceFlattening.AverageDelta,
            VisualCadenceP95Delta = visualCadenceFlattening.P95Delta,
            VisualCadenceMotionScore = visualCadenceFlattening.MotionScore,
            VisualCadenceMotionConfidence = visualCadenceFlattening.MotionConfidence,
            VisualCadenceRecentOutputIntervalsMs = visualCadenceFlattening.RecentOutputIntervalsMs,
            VisualCadenceRecentChangeIntervalsMs = visualCadenceFlattening.RecentChangeIntervalsMs,
            VisualCenterCadenceSampleCount = visualCadenceFlattening.CenterSampleCount,
            VisualCenterCadenceChangedFrameCount = visualCadenceFlattening.CenterChangedFrameCount,
            VisualCenterCadenceRepeatFrameCount = visualCadenceFlattening.CenterRepeatFrameCount,
            VisualCenterCadenceLongestRepeatRun = visualCadenceFlattening.CenterLongestRepeatRun,
            VisualCenterCadenceOutputObservedFps = visualCadenceFlattening.CenterOutputObservedFps,
            VisualCenterCadenceChangeObservedFps = visualCadenceFlattening.CenterChangeObservedFps,
            VisualCenterCadenceRepeatFramePercent = visualCadenceFlattening.CenterRepeatFramePercent,
            VisualCenterCadenceLastDelta = visualCadenceFlattening.CenterLastDelta,
            VisualCenterCadenceAverageDelta = visualCadenceFlattening.CenterAverageDelta,
            VisualCenterCadenceP95Delta = visualCadenceFlattening.CenterP95Delta,
            VisualCenterCadenceMotionScore = visualCadenceFlattening.CenterMotionScore,
            VisualCenterCadenceMotionConfidence = visualCadenceFlattening.CenterMotionConfidence,
            VisualCenterCadenceRecentOutputIntervalsMs = visualCadenceFlattening.CenterRecentOutputIntervalsMs,
            VisualCenterCadenceRecentChangeIntervalsMs = visualCadenceFlattening.CenterRecentChangeIntervalsMs,
            MjpegPerDecoder = mjpegTimingFlattening.PerDecoder,
            RecordingVideoBytes = recordingOutputFlattening.RecordingVideoBytes,
            RecordingAudioBytes = recordingOutputFlattening.RecordingAudioBytes,
            RecordingTotalBytes = recordingOutputFlattening.RecordingTotalBytes,
            RecordingFileGrowing = recordingOutputFlattening.RecordingFileGrowing,
            LastOutputPath = recordingOutputFlattening.LastOutputPath,
            LastFinalizeStatus = recordingOutputFlattening.LastFinalizeStatus,
            LastFinalizeUtc = recordingOutputFlattening.LastFinalizeUtc,
            LastOutputExists = recordingOutputFlattening.LastOutputExists,
            LastOutputSizeBytes = recordingOutputFlattening.LastOutputSizeBytes,
            LastVerification = recordingOutputFlattening.LastVerification,
            HdrTruthVerdict = hdrPipelineFlattening.TruthVerdict,
            MemoryWorkingSetMb = processResourceFlattening.MemoryWorkingSetMb,
            MemoryPrivateBytesMb = processResourceFlattening.MemoryPrivateBytesMb,
            MemoryManagedHeapMb = processResourceFlattening.MemoryManagedHeapMb,
            MemoryTotalAllocatedMb = processResourceFlattening.MemoryTotalAllocatedMb,
            ProcessCpuPercent = processResourceFlattening.ProcessCpuPercent,
            ProcessCpuTotalProcessorTimeMs = processResourceFlattening.ProcessCpuTotalProcessorTimeMs,
            MemoryGcHeapSizeMb = processResourceFlattening.MemoryGcHeapSizeMb,
            MemoryGcGen0Collections = processResourceFlattening.MemoryGcGen0Collections,
            MemoryGcGen1Collections = processResourceFlattening.MemoryGcGen1Collections,
            MemoryGcGen2Collections = processResourceFlattening.MemoryGcGen2Collections,
            MemoryGcPauseTimePercent = processResourceFlattening.MemoryGcPauseTimePercent,
            MemoryGcFragmentationPercent = processResourceFlattening.MemoryGcFragmentationPercent,
            ThreadPoolWorkerAvailable = processResourceFlattening.ThreadPoolWorkerAvailable,
            ThreadPoolWorkerMax = processResourceFlattening.ThreadPoolWorkerMax,
            ThreadPoolIoAvailable = processResourceFlattening.ThreadPoolIoAvailable,
            ThreadPoolIoMax = processResourceFlattening.ThreadPoolIoMax,
            AvSyncCaptureDriftMs = avSyncFlattening.CaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec = avSyncFlattening.CaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs = avSyncFlattening.EncoderDriftMs,
            AvSyncEncoderCorrectionSamples = avSyncFlattening.EncoderCorrectionSamples,
            FlashbackActive = flashbackRecordingFlattening.Runtime.Active,
            FlashbackBufferedDurationMs = flashbackRecordingFlattening.Runtime.BufferedDurationMs,
            FlashbackDiskBytes = flashbackRecordingFlattening.Runtime.DiskBytes,
            FlashbackTotalBytesWritten = flashbackRecordingFlattening.Runtime.TotalBytesWritten,
            FlashbackOutputBytes = flashbackRecordingFlattening.Runtime.OutputBytes,
            FlashbackFilePath = flashbackRecordingFlattening.Runtime.FilePath,
            FlashbackEncodedFrames = flashbackRecordingFlattening.Runtime.EncodedFrames,
            FlashbackDroppedFrames = flashbackRecordingFlattening.Runtime.DroppedFrames,
            FlashbackGpuEncoding = flashbackRecordingFlattening.Runtime.GpuEncoding,
            FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,
            FlashbackBackendSettingsStaleReason = flashbackRecordingFlattening.Backend.SettingsStaleReason,
            FlashbackBackendActiveFormat = flashbackRecordingFlattening.Backend.ActiveFormat,
            FlashbackBackendRequestedFormat = flashbackRecordingFlattening.Backend.RequestedFormat,
            FlashbackBackendActivePreset = flashbackRecordingFlattening.Backend.ActivePreset,
            FlashbackBackendRequestedPreset = flashbackRecordingFlattening.Backend.RequestedPreset,
            FlashbackExportVerificationFormat = flashbackRecordingFlattening.Backend.ExportVerificationFormat,
            FlashbackCodecDowngradeReason = flashbackRecordingFlattening.Backend.CodecDowngradeReason,
            EncoderCodecName = flashbackRecordingFlattening.Encoder.CodecName,
            EncoderTargetBitRate = flashbackRecordingFlattening.Encoder.TargetBitRate,
            EncoderWidth = flashbackRecordingFlattening.Encoder.Width,
            EncoderHeight = flashbackRecordingFlattening.Encoder.Height,
            EncoderFrameRate = flashbackRecordingFlattening.Encoder.FrameRate,
            EncoderFrameRateNumerator = flashbackRecordingFlattening.Encoder.FrameRateNumerator,
            EncoderFrameRateDenominator = flashbackRecordingFlattening.Encoder.FrameRateDenominator,
            FlashbackVideoQueueDepth = flashbackRecordingFlattening.Queues.VideoQueueDepth,
            FlashbackAudioQueueDepth = flashbackRecordingFlattening.Queues.AudioQueueDepth,
            FlashbackAudioQueueCapacity = flashbackRecordingFlattening.Queues.AudioQueueCapacity,
            FlashbackPlaybackState = flashbackPlaybackFlattening.State,
            FlashbackPlaybackPositionMs = flashbackPlaybackFlattening.PositionMs,
            FlashbackDecoderHwAccel = flashbackPlaybackFlattening.DecoderHwAccel,
            FlashbackPlaybackFrameCount = flashbackPlaybackFlattening.FrameCount,
            FlashbackPlaybackLateFrames = flashbackPlaybackFlattening.LateFrames,
            FlashbackPlaybackDroppedFrames = flashbackPlaybackFlattening.DroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = flashbackPlaybackFlattening.AudioMaster.DelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = flashbackPlaybackFlattening.AudioMaster.DelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = flashbackPlaybackFlattening.AudioMaster.Fallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = flashbackPlaybackFlattening.AudioMaster.UnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = flashbackPlaybackFlattening.AudioMaster.StaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = flashbackPlaybackFlattening.AudioMaster.DriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = flashbackPlaybackFlattening.AudioMaster.LastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackDriftMs = flashbackPlaybackFlattening.AudioMaster.LastFallbackDriftMs,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = flashbackPlaybackFlattening.AudioMaster.LastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = flashbackPlaybackFlattening.Timing.SegmentSwitches,
            FlashbackPlaybackFmp4Reopens = flashbackPlaybackFlattening.Timing.Fmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = flashbackPlaybackFlattening.Timing.WriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = flashbackPlaybackFlattening.Timing.NearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = flashbackPlaybackFlattening.Timing.DecodeErrorSnaps,
            FlashbackPlaybackSubmitFailures = flashbackPlaybackFlattening.Timing.SubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = flashbackPlaybackFlattening.Timing.LastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = flashbackPlaybackFlattening.Timing.LastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = flashbackPlaybackFlattening.Timing.LastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = flashbackPlaybackFlattening.Timing.LastSubmitFailure,
            FlashbackPlaybackLastSegmentSwitchUtcUnixMs = flashbackPlaybackFlattening.Timing.LastSegmentSwitchUtcUnixMs,
            FlashbackPlaybackLastFmp4ReopenUtcUnixMs = flashbackPlaybackFlattening.Timing.LastFmp4ReopenUtcUnixMs,
            FlashbackPlaybackLastWriteHeadWaitGapMs = flashbackPlaybackFlattening.Timing.LastWriteHeadWaitGapMs,
            FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,
            FlashbackPlaybackObservedFps = flashbackPlaybackFlattening.Timing.ObservedFps,
            FlashbackPlaybackAvgFrameMs = flashbackPlaybackFlattening.Timing.AvgFrameMs,
            FlashbackPlaybackCadenceSampleCount = flashbackPlaybackFlattening.Timing.CadenceSampleCount,
            FlashbackPlaybackP95FrameMs = flashbackPlaybackFlattening.Timing.P95FrameMs,
            FlashbackPlaybackP99FrameMs = flashbackPlaybackFlattening.Timing.P99FrameMs,
            FlashbackPlaybackMaxFrameMs = flashbackPlaybackFlattening.Timing.MaxFrameMs,
            FlashbackPlaybackSlowFrames = flashbackPlaybackFlattening.Timing.SlowFrames,
            FlashbackPlaybackSlowFramePercent = flashbackPlaybackFlattening.Timing.SlowFramePercent,
            FlashbackPlaybackOnePercentLowFps = flashbackPlaybackFlattening.Timing.OnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = flashbackPlaybackFlattening.Timing.FivePercentLowFps,
            FlashbackPlaybackSampleDurationMs = flashbackPlaybackFlattening.Timing.SampleDurationMs,
            FlashbackPlaybackRecentFrameIntervalsMs = flashbackPlaybackFlattening.Timing.RecentFrameIntervalsMs,
            FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlaybackFlattening.Timing.PtsCadenceMismatchCount,
            FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs = flashbackPlaybackFlattening.Timing.LastPtsCadenceMismatchUtcUnixMs,
            FlashbackPlaybackLastPtsCadenceDeltaMs = flashbackPlaybackFlattening.Timing.LastPtsCadenceDeltaMs,
            FlashbackPlaybackLastPtsCadenceExpectedMs = flashbackPlaybackFlattening.Timing.LastPtsCadenceExpectedMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = flashbackPlaybackFlattening.Decode.SeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = flashbackPlaybackFlattening.Decode.LastSeekHitForwardDecodeCap,
            FlashbackPlaybackDecodeSampleCount = flashbackPlaybackFlattening.Decode.SampleCount,
            FlashbackPlaybackDecodeAvgMs = flashbackPlaybackFlattening.Decode.AvgMs,
            FlashbackPlaybackDecodeP95Ms = flashbackPlaybackFlattening.Decode.P95Ms,
            FlashbackPlaybackDecodeP99Ms = flashbackPlaybackFlattening.Decode.P99Ms,
            FlashbackPlaybackDecodeMaxMs = flashbackPlaybackFlattening.Decode.MaxMs,
            FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.Decode.MaxPhase,
            FlashbackPlaybackMaxDecodeReceiveMs = flashbackPlaybackFlattening.Decode.MaxReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = flashbackPlaybackFlattening.Decode.MaxFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = flashbackPlaybackFlattening.Decode.MaxReadMs,
            FlashbackPlaybackMaxDecodeSendMs = flashbackPlaybackFlattening.Decode.MaxSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = flashbackPlaybackFlattening.Decode.MaxAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = flashbackPlaybackFlattening.Decode.MaxConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = flashbackPlaybackFlattening.Decode.MaxUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = flashbackPlaybackFlattening.Decode.MaxPositionMs,
            FlashbackAvDriftMs = flashbackPlaybackFlattening.Timing.AvDriftMs,
            FlashbackPlaybackThreadAlive = flashbackPlaybackFlattening.Commands.ThreadAlive,
            FlashbackPlaybackCommandsEnqueued = flashbackPlaybackFlattening.Commands.Enqueued,
            FlashbackPlaybackCommandsProcessed = flashbackPlaybackFlattening.Commands.Processed,
            FlashbackPlaybackCommandsDropped = flashbackPlaybackFlattening.Commands.Dropped,
            FlashbackPlaybackCommandsSkippedNotReady = flashbackPlaybackFlattening.Commands.SkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = flashbackPlaybackFlattening.Commands.ScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = flashbackPlaybackFlattening.Commands.SeekCommandsCoalesced,
            FlashbackPlaybackCommandQueueCapacity = flashbackPlaybackFlattening.Commands.QueueCapacity,
            FlashbackPlaybackPendingCommands = flashbackPlaybackFlattening.Commands.Pending,
            FlashbackPlaybackMaxPendingCommands = flashbackPlaybackFlattening.Commands.MaxPending,
            FlashbackPlaybackLastCommandQueueLatencyMs = flashbackPlaybackFlattening.Commands.LastQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyMs = flashbackPlaybackFlattening.Commands.MaxQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = flashbackPlaybackFlattening.Commands.MaxQueueLatencyCommand,
            FlashbackPlaybackLastCommandQueued = flashbackPlaybackFlattening.Commands.LastQueued,
            FlashbackPlaybackLastCommandProcessed = flashbackPlaybackFlattening.Commands.LastProcessed,
            FlashbackPlaybackLastCommandQueuedUtcUnixMs = flashbackPlaybackFlattening.Commands.LastQueuedUtcUnixMs,
            FlashbackPlaybackLastCommandProcessedUtcUnixMs = flashbackPlaybackFlattening.Commands.LastProcessedUtcUnixMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = flashbackPlaybackFlattening.Commands.LastFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.Commands.LastFailure,
            FlashbackExportActive = flashbackExportFlattening.Active,
            FlashbackExportId = flashbackExportFlattening.Id,
            FlashbackExportStatus = flashbackExportFlattening.Status,
            FlashbackExportOutputPath = flashbackExportFlattening.OutputPath,
            FlashbackExportStartedUtcUnixMs = flashbackExportFlattening.StartedUtcUnixMs,
            FlashbackExportLastProgressUtcUnixMs = flashbackExportFlattening.LastProgressUtcUnixMs,
            FlashbackExportCompletedUtcUnixMs = flashbackExportFlattening.CompletedUtcUnixMs,
            FlashbackExportElapsedMs = flashbackExportFlattening.ElapsedMs,
            FlashbackExportLastProgressAgeMs = flashbackExportFlattening.LastProgressAgeMs,
            FlashbackExportOutputBytes = flashbackExportFlattening.OutputBytes,
            FlashbackExportThroughputBytesPerSec = flashbackExportFlattening.ThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = flashbackExportFlattening.SegmentsProcessed,
            FlashbackExportTotalSegments = flashbackExportFlattening.TotalSegments,
            FlashbackExportPercent = flashbackExportFlattening.Percent,
            FlashbackExportInPointMs = flashbackExportFlattening.InPointMs,
            FlashbackExportOutPointMs = flashbackExportFlattening.OutPointMs,
            FlashbackExportMessage = flashbackExportFlattening.Message,
            FlashbackExportFailureKind = flashbackExportFlattening.FailureKind,
            FlashbackExportForceRotateFallbacks = flashbackExportFlattening.ForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = flashbackExportFlattening.LastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = flashbackExportFlattening.LastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = flashbackExportFlattening.LastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = flashbackExportFlattening.LastForceRotateFallbackOutPointMs,
            LastExportId = flashbackExportFlattening.LastExportId,
            LastExportPath = flashbackExportFlattening.LastExportPath,
            LastExportSuccess = flashbackExportFlattening.LastExportSuccess,
            LastExportMessage = flashbackExportFlattening.LastExportMessage
        };
    }
}
