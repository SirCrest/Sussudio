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
}
