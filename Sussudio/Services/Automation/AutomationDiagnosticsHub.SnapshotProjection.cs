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

    private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),
            HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),
            Actual = BuildCaptureFormatActualFlattenedProjection(captureFormat),
            Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),
            ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),
            Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)
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

    private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Requested.Width,
            Height = captureFormat.Requested.Height,
            FrameRate = captureFormat.Requested.FrameRate,
            FrameRateArg = captureFormat.Requested.FrameRateArg,
            FrameRateNumerator = captureFormat.Requested.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Requested.FrameRateDenominator,
            PixelFormat = captureFormat.Requested.PixelFormat,
            Format = captureFormat.Requested.Format,
            Quality = captureFormat.Requested.Quality,
            HdrEnabled = captureFormat.Requested.HdrEnabled,
            HdrMasteringMetadata = captureFormat.Requested.HdrMasteringMetadata,
            AudioEnabled = captureFormat.Requested.AudioEnabled
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

    private readonly record struct CaptureFormatRequestedFlattenedProjection
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

    private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            ActivationReason = captureFormat.HdrRequest.ActivationReason,
            AutoDowngraded = captureFormat.HdrRequest.AutoDowngraded,
            AutoDowngradeReason = captureFormat.HdrRequest.AutoDowngradeReason,
            RequestedButSourceNot10Bit = captureFormat.HdrRequest.RequestedButSourceNot10Bit
        };

    private readonly record struct CaptureFormatHdrRequestProjection
    {
        public string ActivationReason { get; init; }
        public bool AutoDowngraded { get; init; }
        public string AutoDowngradeReason { get; init; }
        public bool RequestedButSourceNot10Bit { get; init; }
    }

    private readonly record struct CaptureFormatHdrRequestFlattenedProjection
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

    private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Actual.Width,
            Height = captureFormat.Actual.Height,
            FrameRate = captureFormat.Actual.FrameRate,
            FrameRateArg = captureFormat.Actual.FrameRateArg
        };

    private readonly record struct CaptureFormatActualProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
    }

    private readonly record struct CaptureFormatActualFlattenedProjection
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

    private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Negotiated.Width,
            Height = captureFormat.Negotiated.Height,
            FrameRate = captureFormat.Negotiated.FrameRate,
            FrameRateArg = captureFormat.Negotiated.FrameRateArg,
            FrameRateNumerator = captureFormat.Negotiated.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Negotiated.FrameRateDenominator,
            PixelFormat = captureFormat.Negotiated.PixelFormat,
            MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken
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

    private readonly record struct CaptureFormatNegotiatedFlattenedProjection
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

    private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
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
            MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters
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

    private readonly record struct CaptureFormatReaderObservationFlattenedProjection
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

    private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            InputPixelFormat = captureFormat.Encoder.InputPixelFormat,
            OutputPixelFormat = captureFormat.Encoder.OutputPixelFormat,
            VideoCodec = captureFormat.Encoder.VideoCodec,
            VideoProfile = captureFormat.Encoder.VideoProfile,
            TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed
        };

    private readonly record struct CaptureFormatEncoderProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }

    private readonly record struct CaptureFormatEncoderFlattenedProjection
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

    private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(
        CaptureTransportProjection captureTransport)
        => new()
        {
            MemoryPreference = captureTransport.MemoryPreference,
            VideoRequestedSubtype = captureTransport.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureTransport.FrameLedgerCapacity,
            FrameLedgerEventCount = captureTransport.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureTransport.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents
        };

    private readonly record struct CaptureTransportFlattenedProjection
    {
        public string MemoryPreference { get; init; }
        public string VideoRequestedSubtype { get; init; }
        public string VideoNegotiatedSubtype { get; init; }
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; }
    }

    private readonly record struct CaptureFormatFlattenedProjection
    {
        public CaptureFormatRequestedFlattenedProjection Requested { get; init; }
        public CaptureFormatHdrRequestFlattenedProjection HdrRequest { get; init; }
        public CaptureFormatActualFlattenedProjection Actual { get; init; }
        public CaptureFormatNegotiatedFlattenedProjection Negotiated { get; init; }
        public CaptureFormatReaderObservationFlattenedProjection ReaderObservation { get; init; }
        public CaptureFormatEncoderFlattenedProjection Encoder { get; init; }
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

    private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(
        HdrPipelineProjection hdrPipeline)
        => new()
        {
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
            TruthVerdict = hdrPipeline.TruthVerdict
        };

    private readonly record struct HdrPipelineFlattenedProjection
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

    private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(
        MjpegTimingProjection timing)
        => new()
        {
            DecodeSampleCount = timing.DecodeSampleCount,
            DecodeAvgMs = timing.DecodeAvgMs,
            DecodeP95Ms = timing.DecodeP95Ms,
            DecodeMaxMs = timing.DecodeMaxMs,
            InteropCopySampleCount = timing.InteropCopySampleCount,
            InteropCopyAvgMs = timing.InteropCopyAvgMs,
            InteropCopyP95Ms = timing.InteropCopyP95Ms,
            InteropCopyMaxMs = timing.InteropCopyMaxMs,
            CallbackSampleCount = timing.CallbackSampleCount,
            CallbackAvgMs = timing.CallbackAvgMs,
            CallbackP95Ms = timing.CallbackP95Ms,
            CallbackMaxMs = timing.CallbackMaxMs,
            DecoderCount = timing.DecoderCount,
            ReorderSampleCount = timing.ReorderSampleCount,
            ReorderAvgMs = timing.ReorderAvgMs,
            ReorderP95Ms = timing.ReorderP95Ms,
            ReorderMaxMs = timing.ReorderMaxMs,
            PipelineSampleCount = timing.PipelineSampleCount,
            PipelineAvgMs = timing.PipelineAvgMs,
            PipelineP95Ms = timing.PipelineP95Ms,
            PipelineMaxMs = timing.PipelineMaxMs,
            PerDecoder = timing.PerDecoder
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

    private readonly record struct MjpegTimingFlattenedProjection
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

    private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(
        MjpegPreviewJitterQueueProjection queue)
        => new()
        {
            Enabled = queue.Enabled,
            TargetDepth = queue.TargetDepth,
            MaxDepth = queue.MaxDepth,
            QueueDepth = queue.QueueDepth,
            TotalQueued = queue.TotalQueued,
            TotalSubmitted = queue.TotalSubmitted,
            TotalDropped = queue.TotalDropped,
            UnderflowCount = queue.UnderflowCount,
            ResumeReprimeCount = queue.ResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueFlattenedProjection
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

    private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(
        MjpegPreviewJitterTimingProjection timing)
        => new()
        {
            InputSampleCount = timing.InputSampleCount,
            InputAvgMs = timing.InputAvgMs,
            InputP95Ms = timing.InputP95Ms,
            InputMaxMs = timing.InputMaxMs,
            OutputSampleCount = timing.OutputSampleCount,
            OutputAvgMs = timing.OutputAvgMs,
            OutputP95Ms = timing.OutputP95Ms,
            OutputMaxMs = timing.OutputMaxMs,
            LatencySampleCount = timing.LatencySampleCount,
            LatencyAvgMs = timing.LatencyAvgMs,
            LatencyP95Ms = timing.LatencyP95Ms,
            LatencyMaxMs = timing.LatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingFlattenedProjection
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

    private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(
        MjpegPreviewJitterAdaptiveProjection adaptive)
        => new()
        {
            DeadlineDropCount = adaptive.DeadlineDropCount,
            ClearedDropCount = adaptive.ClearedDropCount,
            TargetIncreaseCount = adaptive.TargetIncreaseCount,
            TargetDecreaseCount = adaptive.TargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveFlattenedProjection
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

    private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(
        MjpegPreviewJitterEventProjection events)
        => new()
        {
            LastSelectedPreviewPresentId = events.LastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = events.LastSelectedSourceSequenceNumber,
            LastSelectedQpc = events.LastSelectedQpc,
            LastSelectedSourceLatencyMs = events.LastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = events.LastDroppedSourceSequenceNumber,
            LastDropQpc = events.LastDropQpc,
            LastDropReason = events.LastDropReason,
            LastUnderflowQpc = events.LastUnderflowQpc,
            LastUnderflowReason = events.LastUnderflowReason,
            LastUnderflowQueueDepth = events.LastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = events.LastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = events.LastUnderflowOutputAgeMs,
            LastScheduleLateMs = events.LastScheduleLateMs,
            MaxScheduleLateMs = events.MaxScheduleLateMs,
            ScheduleLateCount = events.ScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventFlattenedProjection
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

    private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(
        MjpegPreviewJitterProjection previewJitter)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),
            Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),
            Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),
            Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)
        };

    private readonly record struct MjpegPreviewJitterFlattenedProjection
    {
        public MjpegPreviewJitterQueueFlattenedProjection Queue { get; init; }
        public MjpegPreviewJitterTimingFlattenedProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveFlattenedProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventFlattenedProjection Events { get; init; }
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

    private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(
        MjpegPacketHashProjection packetHash)
        => new()
        {
            SampleCount = packetHash.SampleCount,
            UniqueFrameCount = packetHash.UniqueFrameCount,
            DuplicateFrameCount = packetHash.DuplicateFrameCount,
            LongestDuplicateRun = packetHash.LongestDuplicateRun,
            InputObservedFps = packetHash.InputObservedFps,
            UniqueObservedFps = packetHash.UniqueObservedFps,
            DuplicateFramePercent = packetHash.DuplicateFramePercent,
            LastHash = packetHash.LastHash,
            LastFrameDuplicate = packetHash.LastFrameDuplicate,
            Pattern = packetHash.Pattern,
            RecentInputIntervalsMs = packetHash.RecentInputIntervalsMs,
            RecentUniqueIntervalsMs = packetHash.RecentUniqueIntervalsMs,
            RecentDuplicateFlags = packetHash.RecentDuplicateFlags
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

    private readonly record struct MjpegPacketHashFlattenedProjection
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

    private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(MjpegProjection mjpeg)
    {
        return new()
        {
            TotalDecoded = mjpeg.TotalDecoded,
            TotalEmitted = mjpeg.TotalEmitted,
            TotalDropped = mjpeg.TotalDropped,
            CompressedFramesQueued = mjpeg.CompressedFramesQueued,
            CompressedFramesDequeued = mjpeg.CompressedFramesDequeued,
            CompressedDropsQueueFull = mjpeg.CompressedDropsQueueFull,
            CompressedDropsByteBudget = mjpeg.CompressedDropsByteBudget,
            CompressedDropsDisposed = mjpeg.CompressedDropsDisposed,
            DecodeFailures = mjpeg.DecodeFailures,
            ReorderCollisions = mjpeg.ReorderCollisions,
            EmitFailures = mjpeg.EmitFailures,
            CompressedQueueDepth = mjpeg.CompressedQueueDepth,
            CompressedQueueBytes = mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,
            ReorderSkips = mjpeg.ReorderSkips,
            ReorderBufferDepth = mjpeg.ReorderBufferDepth,
        };
    }

    private readonly record struct MjpegFlattenedProjection
    {
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

    private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(
        RecordingIntegrityProjection recordingIntegrity)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),
            Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),
            Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),
            Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),
            AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)
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

    private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(
        RecordingIntegritySummaryProjection summary)
        => new()
        {
            Status = summary.Status,
            Complete = summary.Complete,
            Backend = summary.Backend,
            CompletedUtc = summary.CompletedUtc,
            Reason = summary.Reason
        };

    private readonly record struct RecordingIntegritySummaryProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryFlattenedProjection
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

    private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(
        RecordingIntegrityVideoProjection video)
        => new()
        {
            SourceFrames = video.SourceFrames,
            AcceptedFrames = video.AcceptedFrames,
            PipelineDroppedFrames = video.PipelineDroppedFrames,
            QueueDroppedFrames = video.QueueDroppedFrames,
            SubmittedFrames = video.SubmittedFrames,
            EncodedFrames = video.EncodedFrames,
            PacketsWritten = video.PacketsWritten,
            EncoderDroppedFrames = video.EncoderDroppedFrames,
            SequenceGaps = video.SequenceGaps
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

    private readonly record struct RecordingIntegrityVideoFlattenedProjection
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

    private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(
        RecordingIntegrityBackpressureProjection backpressure)
        => new()
        {
            QueueMaxDepth = backpressure.QueueMaxDepth,
            QueueOldestFrameAgeMs = backpressure.QueueOldestFrameAgeMs,
            BackpressureWaitMs = backpressure.BackpressureWaitMs,
            BackpressureEvents = backpressure.BackpressureEvents,
            BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs
        };

    private readonly record struct RecordingIntegrityBackpressureProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private readonly record struct RecordingIntegrityBackpressureFlattenedProjection
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

    private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(
        RecordingIntegrityAudioProjection audio)
        => new()
        {
            AudioStatus = audio.AudioStatus,
            AudioEnabled = audio.AudioEnabled,
            AudioCaptureActive = audio.AudioCaptureActive,
            AudioFramesArrived = audio.AudioFramesArrived,
            AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audio.AudioSamplesEncoded,
            AudioDropEvents = audio.AudioDropEvents,
            AudioDiscontinuities = audio.AudioDiscontinuities,
            AudioTimestampErrors = audio.AudioTimestampErrors,
            AudioCallbackGaps = audio.AudioCallbackGaps
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

    private readonly record struct RecordingIntegrityAudioFlattenedProjection
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

    private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(
        RecordingIntegrityAvSyncProjection avSync)
        => new()
        {
            AvSyncDriftMs = avSync.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = avSync.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityAvSyncFlattenedProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityFlattenedProjection
    {
        public RecordingIntegritySummaryFlattenedProjection Summary { get; init; }
        public RecordingIntegrityVideoFlattenedProjection Video { get; init; }
        public RecordingIntegrityBackpressureFlattenedProjection Backpressure { get; init; }
        public RecordingIntegrityAudioFlattenedProjection Audio { get; init; }
        public RecordingIntegrityAvSyncFlattenedProjection AvSync { get; init; }
    }

    private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderProjection(health),
            Ingest = BuildRecordingPipelineIngestProjection(health),
            VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)
        };

    private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),
            Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),
            VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)
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

    private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,
            VideoFramesEncoded = recordingPipeline.Encoder.VideoFramesEncoded,
            LastEnqueueAgeMs = recordingPipeline.Encoder.LastEnqueueAgeMs,
            LastWriteAgeMs = recordingPipeline.Encoder.LastWriteAgeMs,
            EncodingFailed = recordingPipeline.Encoder.EncodingFailed,
            EncodingFailureType = recordingPipeline.Encoder.EncodingFailureType,
            EncodingFailureMessage = recordingPipeline.Encoder.EncodingFailureMessage
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

    private readonly record struct RecordingPipelineEncoderFlattenedProjection
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

    private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
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
            VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction
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

    private readonly record struct RecordingPipelineIngestFlattenedProjection
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

    private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Capacity = recordingPipeline.VideoQueue.Capacity,
            MaxDepth = recordingPipeline.VideoQueue.MaxDepth,
            FramesSubmittedToEncoder = recordingPipeline.VideoQueue.FramesSubmittedToEncoder,
            EncoderPts = recordingPipeline.VideoQueue.EncoderPts,
            EncoderPacketsWritten = recordingPipeline.VideoQueue.EncoderPacketsWritten,
            EncoderDroppedFrames = recordingPipeline.VideoQueue.EncoderDroppedFrames,
            SequenceGaps = recordingPipeline.VideoQueue.SequenceGaps,
            OldestFrameAgeMs = recordingPipeline.VideoQueue.OldestFrameAgeMs,
            LastLatencyMs = recordingPipeline.VideoQueue.LastLatencyMs,
            LatencySampleCount = recordingPipeline.VideoQueue.LatencySampleCount,
            LatencyAvgMs = recordingPipeline.VideoQueue.LatencyAvgMs,
            LatencyP95Ms = recordingPipeline.VideoQueue.LatencyP95Ms,
            LatencyP99Ms = recordingPipeline.VideoQueue.LatencyP99Ms,
            LatencyMaxMs = recordingPipeline.VideoQueue.LatencyMaxMs,
            BackpressureWaitMs = recordingPipeline.VideoQueue.BackpressureWaitMs,
            BackpressureEvents = recordingPipeline.VideoQueue.BackpressureEvents,
            BackpressureLastWaitMs = recordingPipeline.VideoQueue.BackpressureLastWaitMs,
            BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs
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

    private readonly record struct RecordingPipelineVideoQueueFlattenedProjection
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

    private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            GpuQueueDepth = recordingPipeline.HardwareQueues.GpuQueueDepth,
            GpuQueueCapacity = recordingPipeline.HardwareQueues.GpuQueueCapacity,
            GpuQueueMaxDepth = recordingPipeline.HardwareQueues.GpuQueueMaxDepth,
            GpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,
            GpuFramesDropped = recordingPipeline.HardwareQueues.GpuFramesDropped,
            CudaQueueDepth = recordingPipeline.HardwareQueues.CudaQueueDepth,
            CudaQueueCapacity = recordingPipeline.HardwareQueues.CudaQueueCapacity,
            CudaQueueMaxDepth = recordingPipeline.HardwareQueues.CudaQueueMaxDepth,
            CudaFramesEnqueued = recordingPipeline.HardwareQueues.CudaFramesEnqueued,
            CudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped
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

    private readonly record struct RecordingPipelineHardwareQueuesFlattenedProjection
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

    private readonly record struct RecordingPipelineFlattenedProjection
    {
        public RecordingPipelineEncoderFlattenedProjection Encoder { get; init; }
        public RecordingPipelineIngestFlattenedProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueFlattenedProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesFlattenedProjection HardwareQueues { get; init; }
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

    private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(
        RecordingBackendProjection recordingBackend,
        RecordingOutputProjection recordingOutput)
        => new()
        {
            Backend = recordingBackend.Backend,
            AudioPathMode = recordingBackend.AudioPathMode,
            MuxResult = recordingBackend.MuxResult,
            OutputPath = recordingOutput.OutputPath,
            RecordingTime = recordingOutput.RecordingTime,
            RecordingSizeInfo = recordingOutput.RecordingSizeInfo,
            RecordingBitrateInfo = recordingOutput.RecordingBitrateInfo,
            RecordingVideoBytes = recordingOutput.RecordingVideoBytes,
            RecordingAudioBytes = recordingOutput.RecordingAudioBytes,
            RecordingTotalBytes = recordingOutput.RecordingTotalBytes,
            RecordingFileGrowing = recordingOutput.RecordingFileGrowing,
            LastOutputPath = recordingOutput.LastOutputPath,
            LastFinalizeStatus = recordingOutput.LastFinalizeStatus,
            LastFinalizeUtc = recordingOutput.LastFinalizeUtc,
            LastOutputExists = recordingOutput.LastOutputExists,
            LastOutputSizeBytes = recordingOutput.LastOutputSizeBytes,
            LastVerification = recordingOutput.LastVerification
        };

    private readonly record struct RecordingOutputFlattenedProjection
    {
        public string Backend { get; init; }
        public string AudioPathMode { get; init; }
        public string MuxResult { get; init; }
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
        public bool LastOutputExists { get; init; }
        public long? LastOutputSizeBytes { get; init; }
        public RecordingVerificationResult? LastVerification { get; init; }
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

    private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(
        AudioSignalProjection signal)
        => new()
        {
            Peak = signal.Peak,
            Clipping = signal.Clipping,
            SignalPresent = signal.SignalPresent,
            MutedSuspected = signal.MutedSuspected
        };

    private readonly record struct AudioSignalFlattenedProjection
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

    private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            AudioReaderActive = ingest.AudioReaderActive,
            AudioFramesArrived = ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,
            VideoReaderActive = ingest.VideoReaderActive,
            VideoFramesArrived = ingest.VideoFramesArrived,
            VideoFramesWrittenToSink = ingest.VideoFramesWrittenToSink,
            LastVideoFrameAgeMs = ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = ingest.VideoIngestErrorCount
        };

    private readonly record struct CaptureIngestFlattenedProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
    }

    private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            FramesDelivered = ingest.MfSourceReaderFramesDelivered,
            FramesDropped = ingest.MfSourceReaderFramesDropped,
            NegotiatedFormat = ingest.MfSourceReaderNegotiatedFormat,
            ReadOutstanding = ingest.SourceReaderReadOutstanding,
            ReadOutstandingMs = ingest.SourceReaderReadOutstandingMs,
            LastFrameTickMs = ingest.SourceReaderLastFrameTickMs,
            FrameChannelDepth = ingest.SourceReaderFrameChannelDepth
        };

    private readonly record struct SourceReaderFlattenedProjection
    {
        public long FramesDelivered { get; init; }
        public long FramesDropped { get; init; }
        public string? NegotiatedFormat { get; init; }
        public bool ReadOutstanding { get; init; }
        public long ReadOutstandingMs { get; init; }
        public long LastFrameTickMs { get; init; }
        public int FrameChannelDepth { get; init; }
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
            PlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs
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
    }

    private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            CallbackCount = wasapi.CaptureCallbackCount,
            CallbackAvgIntervalMs = wasapi.CaptureCallbackAvgIntervalMs,
            CallbackMaxIntervalMs = wasapi.CaptureCallbackMaxIntervalMs,
            CallbackSevereGapCount = wasapi.CaptureCallbackSevereGapCount,
            AudioDiscontinuityCount = wasapi.CaptureAudioDiscontinuityCount,
            AudioTimestampErrorCount = wasapi.CaptureAudioTimestampErrorCount,
            AudioGlitchCount = wasapi.CaptureAudioGlitchCount,
            CallbackSilenceCount = wasapi.CaptureCallbackSilenceCount,
            LastCallbackTickMs = wasapi.CaptureLastCallbackTickMs,
            AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,
            AudioLevelLastFireTickMs = wasapi.CaptureAudioLevelLastFireTickMs
        };

    private readonly record struct WasapiCaptureFlattenedProjection
    {
        public long CallbackCount { get; init; }
        public double CallbackAvgIntervalMs { get; init; }
        public double CallbackMaxIntervalMs { get; init; }
        public long CallbackSevereGapCount { get; init; }
        public long AudioDiscontinuityCount { get; init; }
        public long AudioTimestampErrorCount { get; init; }
        public long AudioGlitchCount { get; init; }
        public int CallbackSilenceCount { get; init; }
        public long LastCallbackTickMs { get; init; }
        public long AudioLevelEventsFired { get; init; }
        public long AudioLevelLastFireTickMs { get; init; }
    }

    private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            RenderCallbackCount = wasapi.PlaybackRenderCallbackCount,
            RenderSilenceCount = wasapi.PlaybackRenderSilenceCount,
            QueueDepth = wasapi.PlaybackQueueDepth,
            QueueDropCount = wasapi.PlaybackQueueDropCount,
            QueueDurationMs = wasapi.PlaybackQueueDurationMs,
            ActiveChunkDurationMs = wasapi.PlaybackActiveChunkDurationMs,
            EndpointQueuedDurationMs = wasapi.PlaybackEndpointQueuedDurationMs,
            BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,
            StreamLatencyMs = wasapi.PlaybackStreamLatencyMs,
            LastRenderTickMs = wasapi.PlaybackLastRenderTickMs
        };

    private readonly record struct WasapiPlaybackFlattenedProjection
    {
        public long RenderCallbackCount { get; init; }
        public int RenderSilenceCount { get; init; }
        public int QueueDepth { get; init; }
        public int QueueDropCount { get; init; }
        public double QueueDurationMs { get; init; }
        public double ActiveChunkDurationMs { get; init; }
        public double EndpointQueuedDurationMs { get; init; }
        public double BufferedDurationMs { get; init; }
        public double StreamLatencyMs { get; init; }
        public long LastRenderTickMs { get; init; }
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

    private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(AudioDropsProjection audioDrops)
        => new()
        {
            QueueSaturated = audioDrops.QueueSaturated,
            BacklogEviction = audioDrops.BacklogEviction,
            ChunksDropped = audioDrops.ChunksDropped,
            QueueDropsRealtime = audioDrops.QueueDropsRealtime,
            QueueDropsFileWriter = audioDrops.QueueDropsFileWriter
        };

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private readonly record struct AudioDropsFlattenedProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),
            Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),
            SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),
            WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),
            WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)
        };

    private readonly record struct AudioAndIngestFlattenedProjection
    {
        public AudioSignalFlattenedProjection Signal { get; init; }
        public CaptureIngestFlattenedProjection Ingest { get; init; }
        public SourceReaderFlattenedProjection SourceReader { get; init; }
        public WasapiCaptureFlattenedProjection WasapiCapture { get; init; }
        public WasapiPlaybackFlattenedProjection WasapiPlayback { get; init; }
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

    private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),
            Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),
            Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),
            Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),
            Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)
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

    private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(
        PreviewRuntimeFrameProjection frame)
        => new()
        {
            FramesArrived = frame.FramesArrived,
            FramesDisplayed = frame.FramesDisplayed,
            FramesDropped = frame.FramesDropped,
            EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs
        };

    private readonly record struct PreviewRuntimeFrameProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private readonly record struct PreviewRuntimeFrameFlattenedProjection
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

    private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(
        PreviewRuntimeCadenceProjection cadence)
        => new()
        {
            SampleCount = cadence.SampleCount,
            ObservedFps = cadence.ObservedFps,
            ExpectedIntervalMs = cadence.ExpectedIntervalMs,
            AverageIntervalMs = cadence.AverageIntervalMs,
            P95IntervalMs = cadence.P95IntervalMs,
            P99IntervalMs = cadence.P99IntervalMs,
            MaxIntervalMs = cadence.MaxIntervalMs,
            OnePercentLowFps = cadence.OnePercentLowFps,
            FivePercentLowFps = cadence.FivePercentLowFps,
            SampleDurationMs = cadence.SampleDurationMs,
            RecentIntervalsMs = cadence.RecentIntervalsMs,
            JitterStdDevMs = cadence.JitterStdDevMs,
            SlowFrameCount = cadence.SlowFrameCount,
            SlowFramePercent = cadence.SlowFramePercent
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

    private readonly record struct PreviewRuntimeCadenceFlattenedProjection
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

    private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(
        PreviewRuntimeColorProjection color)
        => new()
        {
            HdrInputDetected = color.HdrInputDetected,
            ToneMapMode = color.ToneMapMode,
            ColorContext = color.ColorContext,
            AdapterColorMetadata = color.AdapterColorMetadata
        };

    private readonly record struct PreviewRuntimeColorProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private readonly record struct PreviewRuntimeColorFlattenedProjection
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

    private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(
        PreviewRuntimeSurfaceProjection surface)
        => new()
        {
            GpuActive = surface.GpuActive,
            PlaceholderVisible = surface.PlaceholderVisible,
            GpuElementVisible = surface.GpuElementVisible,
            CpuElementVisible = surface.CpuElementVisible,
            RendererAttached = surface.RendererAttached
        };

    private readonly record struct PreviewRuntimeSurfaceProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }

    private readonly record struct PreviewRuntimeSurfaceFlattenedProjection
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

    private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(
        PreviewRuntimeStartupProjection startup)
        => new()
        {
            State = startup.State,
            AttemptId = startup.AttemptId,
            ElapsedMs = startup.ElapsedMs,
            TimeoutMs = startup.TimeoutMs,
            GpuSignalMediaOpened = startup.GpuSignalMediaOpened,
            GpuSignalFirstFrame = startup.GpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = startup.GpuSignalPlaybackAdvancing,
            RequiredSignals = startup.RequiredSignals,
            ReceivedSignals = startup.ReceivedSignals,
            Strategy = startup.Strategy,
            MissingSignals = startup.MissingSignals,
            RecoveryAttemptCount = startup.RecoveryAttemptCount,
            LastFailureReason = startup.LastFailureReason,
            FirstVisualConfirmed = startup.FirstVisualConfirmed,
            BlankSuspected = startup.BlankSuspected,
            Stalled = startup.Stalled,
            RendererMode = startup.RendererMode
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

    private readonly record struct PreviewRuntimeStartupFlattenedProjection
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

    private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(
        PreviewRuntimeGpuPlaybackProjection gpuPlayback)
        => new()
        {
            PlaybackState = gpuPlayback.PlaybackState,
            NaturalVideoWidth = gpuPlayback.NaturalVideoWidth,
            NaturalVideoHeight = gpuPlayback.NaturalVideoHeight,
            PositionMs = gpuPlayback.PositionMs,
            PositionEventCount = gpuPlayback.PositionEventCount
        };

    private readonly record struct PreviewRuntimeGpuPlaybackProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeGpuPlaybackFlattenedProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeFlattenedProjection
    {
        public PreviewRuntimeFrameFlattenedProjection Frame { get; init; }
        public PreviewRuntimeCadenceFlattenedProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceFlattenedProjection Surface { get; init; }
        public PreviewRuntimeStartupFlattenedProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackFlattenedProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorFlattenedProjection Color { get; init; }
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

    private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(
        PreviewD3DProjection previewD3D)
        => new()
        {
            PresentSyncInterval = previewD3D.PresentSyncInterval,
            MaxFrameLatency = previewD3D.MaxFrameLatency,
            SwapChainBufferCount = previewD3D.SwapChainBufferCount,
            SwapChainAddress = previewD3D.SwapChainAddress,
            FramesSubmitted = previewD3D.FramesSubmitted,
            FramesRendered = previewD3D.FramesRendered,
            FramesDropped = previewD3D.FramesDropped,
            RenderThreadFailureCount = previewD3D.RenderThreadFailureCount,
            LastRenderThreadFailureType = previewD3D.LastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewD3D.LastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewD3D.LastRenderThreadFailureHResult,
            PendingFrameCount = previewD3D.PendingFrameCount,
            InputColorSpace = previewD3D.InputColorSpace,
            OutputColorSpace = previewD3D.OutputColorSpace,
            CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),
            LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(
                previewD3D.PipelineLatency,
                previewD3D.FrameLatencyWait,
                previewD3D.FrameStats),
            FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)
        };

    private readonly record struct PreviewD3DFlattenedProjection
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
        public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }
        public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }
        public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }
    }

    private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(
        PreviewD3DCpuTimingProjection cpuTiming)
        => new()
        {
            SampleCount = cpuTiming.SampleCount,
            InputUploadCpuAvgMs = cpuTiming.InputUploadAvgMs,
            InputUploadCpuP95Ms = cpuTiming.InputUploadP95Ms,
            InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,
            InputUploadCpuMaxMs = cpuTiming.InputUploadMaxMs,
            RenderSubmitCpuAvgMs = cpuTiming.RenderSubmitAvgMs,
            RenderSubmitCpuP95Ms = cpuTiming.RenderSubmitP95Ms,
            RenderSubmitCpuP99Ms = cpuTiming.RenderSubmitP99Ms,
            RenderSubmitCpuMaxMs = cpuTiming.RenderSubmitMaxMs,
            PresentCallAvgMs = cpuTiming.PresentCallAvgMs,
            PresentCallP95Ms = cpuTiming.PresentCallP95Ms,
            PresentCallP99Ms = cpuTiming.PresentCallP99Ms,
            PresentCallMaxMs = cpuTiming.PresentCallMaxMs,
            TotalFrameCpuAvgMs = cpuTiming.TotalFrameAvgMs,
            TotalFrameCpuP95Ms = cpuTiming.TotalFrameP95Ms,
            TotalFrameCpuP99Ms = cpuTiming.TotalFrameP99Ms,
            TotalFrameCpuMaxMs = cpuTiming.TotalFrameMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingFlattenedProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadCpuAvgMs { get; init; }
        public double InputUploadCpuP95Ms { get; init; }
        public double InputUploadCpuP99Ms { get; init; }
        public double InputUploadCpuMaxMs { get; init; }
        public double RenderSubmitCpuAvgMs { get; init; }
        public double RenderSubmitCpuP95Ms { get; init; }
        public double RenderSubmitCpuP99Ms { get; init; }
        public double RenderSubmitCpuMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameCpuAvgMs { get; init; }
        public double TotalFrameCpuP95Ms { get; init; }
        public double TotalFrameCpuP99Ms { get; init; }
        public double TotalFrameCpuMaxMs { get; init; }
    }

    private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(
        PreviewD3DPipelineLatencyProjection pipelineLatency,
        PreviewD3DFrameLatencyWaitProjection frameLatencyWait,
        PreviewD3DFrameStatsProjection frameStats)
        => new()
        {
            PipelineLatencySampleCount = pipelineLatency.SampleCount,
            PipelineLatencyAvgMs = pipelineLatency.AvgMs,
            PipelineLatencyP95Ms = pipelineLatency.P95Ms,
            PipelineLatencyP99Ms = pipelineLatency.P99Ms,
            PipelineLatencyMaxMs = pipelineLatency.MaxMs,
            FrameLatencyWaitEnabled = frameLatencyWait.Enabled,
            FrameLatencyWaitHandleActive = frameLatencyWait.HandleActive,
            FrameLatencyWaitCallCount = frameLatencyWait.CallCount,
            FrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount,
            FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,
            FrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount,
            FrameLatencyWaitLastResult = frameLatencyWait.LastResult,
            FrameLatencyWaitLastMs = frameLatencyWait.LastMs,
            FrameLatencyWaitSampleCount = frameLatencyWait.SampleCount,
            FrameLatencyWaitAvgMs = frameLatencyWait.AvgMs,
            FrameLatencyWaitP95Ms = frameLatencyWait.P95Ms,
            FrameLatencyWaitP99Ms = frameLatencyWait.P99Ms,
            FrameLatencyWaitMaxMs = frameLatencyWait.MaxMs,
            FrameStatsSampleCount = frameStats.SampleCount,
            FrameStatsSuccessCount = frameStats.SuccessCount,
            FrameStatsFailureCount = frameStats.FailureCount,
            FrameStatsLastError = frameStats.LastError,
            FrameStatsPresentCount = frameStats.PresentCount,
            FrameStatsPresentRefreshCount = frameStats.PresentRefreshCount,
            FrameStatsSyncRefreshCount = frameStats.SyncRefreshCount,
            FrameStatsSyncQpcTime = frameStats.SyncQpcTime,
            FrameStatsLastPresentDelta = frameStats.LastPresentDelta,
            FrameStatsLastPresentRefreshDelta = frameStats.LastPresentRefreshDelta,
            FrameStatsLastSyncRefreshDelta = frameStats.LastSyncRefreshDelta,
            FrameStatsMissedRefreshCount = frameStats.MissedRefreshCount,
            FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,
            FrameStatsRecentFailureCount = frameStats.RecentFailureCount
        };

    private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection
    {
        public int PipelineLatencySampleCount { get; init; }
        public double PipelineLatencyAvgMs { get; init; }
        public double PipelineLatencyP95Ms { get; init; }
        public double PipelineLatencyP99Ms { get; init; }
        public double PipelineLatencyMaxMs { get; init; }
        public bool FrameLatencyWaitEnabled { get; init; }
        public bool FrameLatencyWaitHandleActive { get; init; }
        public long FrameLatencyWaitCallCount { get; init; }
        public long FrameLatencyWaitSignaledCount { get; init; }
        public long FrameLatencyWaitTimeoutCount { get; init; }
        public long FrameLatencyWaitUnexpectedResultCount { get; init; }
        public uint FrameLatencyWaitLastResult { get; init; }
        public double FrameLatencyWaitLastMs { get; init; }
        public int FrameLatencyWaitSampleCount { get; init; }
        public double FrameLatencyWaitAvgMs { get; init; }
        public double FrameLatencyWaitP95Ms { get; init; }
        public double FrameLatencyWaitP99Ms { get; init; }
        public double FrameLatencyWaitMaxMs { get; init; }
        public long FrameStatsSampleCount { get; init; }
        public long FrameStatsSuccessCount { get; init; }
        public long FrameStatsFailureCount { get; init; }
        public string FrameStatsLastError { get; init; }
        public long FrameStatsPresentCount { get; init; }
        public long FrameStatsPresentRefreshCount { get; init; }
        public long FrameStatsSyncRefreshCount { get; init; }
        public long FrameStatsSyncQpcTime { get; init; }
        public long FrameStatsLastPresentDelta { get; init; }
        public long FrameStatsLastPresentRefreshDelta { get; init; }
        public long FrameStatsLastSyncRefreshDelta { get; init; }
        public long FrameStatsMissedRefreshCount { get; init; }
        public long FrameStatsRecentMissedRefreshCount { get; init; }
        public long FrameStatsRecentFailureCount { get; init; }
    }

    private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(
        PreviewD3DFrameFlowProjection frameFlow)
        => new()
        {
            LastSubmittedPreviewPresentId = frameFlow.LastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = frameFlow.LastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = frameFlow.LastSubmittedSourcePtsTicks,
            LastSubmittedQpc = frameFlow.LastSubmittedQpc,
            LastSubmittedUtcUnixMs = frameFlow.LastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = frameFlow.LastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = frameFlow.LastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = frameFlow.LastRenderedSourcePtsTicks,
            LastRenderedQpc = frameFlow.LastRenderedQpc,
            LastRenderedUtcUnixMs = frameFlow.LastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = frameFlow.LastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = frameFlow.LastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = frameFlow.LastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = frameFlow.LastDroppedSourcePtsTicks,
            LastDroppedQpc = frameFlow.LastDroppedQpc,
            LastDroppedUtcUnixMs = frameFlow.LastDroppedUtcUnixMs,
            LastDropReason = frameFlow.LastDropReason,
            RecentSlowFrames = frameFlow.RecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowFlattenedProjection
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

    private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackExportActive,
            Id = health.FlashbackExportId,
            Status = health.FlashbackExportStatus,
            OutputPath = health.FlashbackExportOutputPath,
            StartedUtcUnixMs = health.FlashbackExportStartedUtcUnixMs,
            LastProgressUtcUnixMs = health.FlashbackExportLastProgressUtcUnixMs,
            CompletedUtcUnixMs = health.FlashbackExportCompletedUtcUnixMs,
            ElapsedMs = health.FlashbackExportElapsedMs,
            LastProgressAgeMs = health.FlashbackExportLastProgressAgeMs,
            OutputBytes = health.FlashbackExportOutputBytes,
            ThroughputBytesPerSec = health.FlashbackExportThroughputBytesPerSec,
            SegmentsProcessed = health.FlashbackExportSegmentsProcessed,
            TotalSegments = health.FlashbackExportTotalSegments,
            Percent = health.FlashbackExportPercent,
            InPointMs = health.FlashbackExportInPointMs,
            OutPointMs = health.FlashbackExportOutPointMs,
            Message = health.FlashbackExportMessage,
            FailureKind = health.FlashbackExportFailureKind,
            ForceRotateFallbacks = health.FlashbackExportForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = health.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = health.FlashbackExportLastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = health.FlashbackExportLastForceRotateFallbackOutPointMs
        };

    private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)
        => new()
        {
            LastExportId = health.LastExportId,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };

    private readonly record struct FlashbackExportProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
    }

    private readonly record struct FlashbackExportLastResultProjection
    {
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }

    private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(
        FlashbackExportProjection flashbackExport,
        FlashbackExportLastResultProjection lastResult)
        => new()
        {
            Active = flashbackExport.Active,
            Id = flashbackExport.Id,
            Status = flashbackExport.Status,
            OutputPath = flashbackExport.OutputPath,
            StartedUtcUnixMs = flashbackExport.StartedUtcUnixMs,
            LastProgressUtcUnixMs = flashbackExport.LastProgressUtcUnixMs,
            CompletedUtcUnixMs = flashbackExport.CompletedUtcUnixMs,
            ElapsedMs = flashbackExport.ElapsedMs,
            LastProgressAgeMs = flashbackExport.LastProgressAgeMs,
            OutputBytes = flashbackExport.OutputBytes,
            ThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,
            SegmentsProcessed = flashbackExport.SegmentsProcessed,
            TotalSegments = flashbackExport.TotalSegments,
            Percent = flashbackExport.Percent,
            InPointMs = flashbackExport.InPointMs,
            OutPointMs = flashbackExport.OutPointMs,
            Message = flashbackExport.Message,
            FailureKind = flashbackExport.FailureKind,
            ForceRotateFallbacks = flashbackExport.ForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = flashbackExport.LastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = flashbackExport.LastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = flashbackExport.LastForceRotateFallbackOutPointMs,
            LastExportId = lastResult.LastExportId,
            LastExportPath = lastResult.LastExportPath,
            LastExportSuccess = lastResult.LastExportSuccess,
            LastExportMessage = lastResult.LastExportMessage
        };

    private readonly record struct FlashbackExportFlattenedProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }

    private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
    {
        var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);
        var queues = BuildFlashbackRecordingQueuesProjection(health);
        var runtime = BuildFlashbackRecordingRuntimeProjection(health);
        var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);
        var encoder = BuildFlashbackRecordingEncoderProjection(health);

        return new()
        {
            EncodingFailed = health.FlashbackEncodingFailed,
            EncodingFailureType = health.FlashbackEncodingFailureType,
            EncodingFailureMessage = health.FlashbackEncodingFailureMessage,
            FatalCleanupInProgress = health.FatalCleanupInProgress,
            CleanupInProgress = health.FlashbackCleanupInProgress,
            ForceRotateActive = health.FlashbackForceRotateActive,
            ForceRotateRequested = health.FlashbackForceRotateRequested,
            ForceRotateDraining = health.FlashbackForceRotateDraining,
            StartupCache = startupCache,
            Queues = queues,
            Runtime = runtime,
            Backend = backend,
            Encoder = encoder
        };
    }

    private readonly record struct FlashbackRecordingProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public FlashbackRecordingStartupCacheProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesProjection Queues { get; init; }
        public FlashbackRecordingRuntimeProjection Runtime { get; init; }
        public FlashbackRecordingBackendProjection Backend { get; init; }
        public FlashbackRecordingEncoderProjection Encoder { get; init; }
    }

    private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,
            BudgetBytes = health.FlashbackStartupCacheBudgetBytes,
            Bytes = health.FlashbackStartupCacheBytes,
            SessionCount = health.FlashbackStartupCacheSessionCount,
            DeletedSessionCount = health.FlashbackStartupCacheDeletedSessionCount,
            FreedBytes = health.FlashbackStartupCacheFreedBytes,
            OverBudget = health.FlashbackStartupCacheOverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheProjection
    {
        public long TempDriveFreeBytes { get; init; }
        public long BudgetBytes { get; init; }
        public long Bytes { get; init; }
        public int SessionCount { get; init; }
        public int DeletedSessionCount { get; init; }
        public long FreedBytes { get; init; }
        public bool OverBudget { get; init; }
    }

    private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(
        FlashbackRecordingStartupCacheProjection startupCache)
        => new()
        {
            TempDriveFreeBytes = startupCache.TempDriveFreeBytes,
            BudgetBytes = startupCache.BudgetBytes,
            Bytes = startupCache.Bytes,
            SessionCount = startupCache.SessionCount,
            DeletedSessionCount = startupCache.DeletedSessionCount,
            FreedBytes = startupCache.FreedBytes,
            OverBudget = startupCache.OverBudget
        };

    private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection
    {
        public long TempDriveFreeBytes { get; init; }
        public long BudgetBytes { get; init; }
        public long Bytes { get; init; }
        public int SessionCount { get; init; }
        public int DeletedSessionCount { get; init; }
        public long FreedBytes { get; init; }
        public bool OverBudget { get; init; }
    }

    private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            VideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            VideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            VideoEncoderPts = health.FlashbackVideoEncoderPts,
            VideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            VideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            VideoQueueRejectedFrames = health.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason = health.FlashbackVideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = health.FlashbackVideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = health.FlashbackVideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = health.FlashbackVideoBackpressureWaitMs,
            VideoBackpressureEvents = health.FlashbackVideoBackpressureEvents,
            VideoBackpressureLastWaitMs = health.FlashbackVideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = health.FlashbackVideoBackpressureMaxWaitMs,
            GpuQueueDepth = health.FlashbackGpuQueueDepth,
            GpuQueueCapacity = health.FlashbackGpuQueueCapacity,
            GpuQueueMaxDepth = health.FlashbackGpuQueueMaxDepth,
            GpuFramesEnqueued = health.FlashbackGpuFramesEnqueued,
            GpuFramesDropped = health.FlashbackGpuFramesDropped,
            GpuQueueRejectedFrames = health.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,
            VideoQueueDepth = health.FlashbackVideoQueueDepth,
            AudioQueueDepth = health.FlashbackAudioQueueDepth,
            AudioQueueCapacity = health.FlashbackAudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesProjection
    {
        public int VideoQueueCapacity { get; init; }
        public int VideoQueueMaxDepth { get; init; }
        public long VideoFramesSubmittedToEncoder { get; init; }
        public long VideoEncoderPts { get; init; }
        public long VideoEncoderPacketsWritten { get; init; }
        public long VideoEncoderDroppedFrames { get; init; }
        public long VideoSequenceGaps { get; init; }
        public long VideoQueueRejectedFrames { get; init; }
        public string VideoQueueLastRejectReason { get; init; }
        public long VideoQueueOldestFrameAgeMs { get; init; }
        public long VideoQueueLastLatencyMs { get; init; }
        public int VideoQueueLatencySampleCount { get; init; }
        public double VideoQueueLatencyAvgMs { get; init; }
        public double VideoQueueLatencyP95Ms { get; init; }
        public double VideoQueueLatencyP99Ms { get; init; }
        public double VideoQueueLatencyMaxMs { get; init; }
        public long VideoBackpressureWaitMs { get; init; }
        public long VideoBackpressureEvents { get; init; }
        public long VideoBackpressureLastWaitMs { get; init; }
        public long VideoBackpressureMaxWaitMs { get; init; }
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public long GpuQueueRejectedFrames { get; init; }
        public string GpuQueueLastRejectReason { get; init; }
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }

    private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(
        FlashbackRecordingQueuesProjection queues)
        => new()
        {
            VideoQueueCapacity = queues.VideoQueueCapacity,
            VideoQueueMaxDepth = queues.VideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = queues.VideoFramesSubmittedToEncoder,
            VideoEncoderPts = queues.VideoEncoderPts,
            VideoEncoderPacketsWritten = queues.VideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = queues.VideoEncoderDroppedFrames,
            VideoSequenceGaps = queues.VideoSequenceGaps,
            VideoQueueRejectedFrames = queues.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason = queues.VideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = queues.VideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = queues.VideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = queues.VideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = queues.VideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = queues.VideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = queues.VideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = queues.VideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = queues.VideoBackpressureWaitMs,
            VideoBackpressureEvents = queues.VideoBackpressureEvents,
            VideoBackpressureLastWaitMs = queues.VideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = queues.VideoBackpressureMaxWaitMs,
            GpuQueueDepth = queues.GpuQueueDepth,
            GpuQueueCapacity = queues.GpuQueueCapacity,
            GpuQueueMaxDepth = queues.GpuQueueMaxDepth,
            GpuFramesEnqueued = queues.GpuFramesEnqueued,
            GpuFramesDropped = queues.GpuFramesDropped,
            GpuQueueRejectedFrames = queues.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,
            VideoQueueDepth = queues.VideoQueueDepth,
            AudioQueueDepth = queues.AudioQueueDepth,
            AudioQueueCapacity = queues.AudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesFlattenedProjection
    {
        public int VideoQueueCapacity { get; init; }
        public int VideoQueueMaxDepth { get; init; }
        public long VideoFramesSubmittedToEncoder { get; init; }
        public long VideoEncoderPts { get; init; }
        public long VideoEncoderPacketsWritten { get; init; }
        public long VideoEncoderDroppedFrames { get; init; }
        public long VideoSequenceGaps { get; init; }
        public long VideoQueueRejectedFrames { get; init; }
        public string VideoQueueLastRejectReason { get; init; }
        public long VideoQueueOldestFrameAgeMs { get; init; }
        public long VideoQueueLastLatencyMs { get; init; }
        public int VideoQueueLatencySampleCount { get; init; }
        public double VideoQueueLatencyAvgMs { get; init; }
        public double VideoQueueLatencyP95Ms { get; init; }
        public double VideoQueueLatencyP99Ms { get; init; }
        public double VideoQueueLatencyMaxMs { get; init; }
        public long VideoBackpressureWaitMs { get; init; }
        public long VideoBackpressureEvents { get; init; }
        public long VideoBackpressureLastWaitMs { get; init; }
        public long VideoBackpressureMaxWaitMs { get; init; }
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public long GpuQueueRejectedFrames { get; init; }
        public string GpuQueueLastRejectReason { get; init; }
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }

    private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackActive,
            BufferedDurationMs = health.FlashbackBufferedDurationMs,
            DiskBytes = health.FlashbackDiskBytes,
            TotalBytesWritten = health.FlashbackTotalBytesWritten,
            OutputBytes = health.FlashbackOutputBytes,
            FilePath = health.FlashbackFilePath,
            EncodedFrames = health.FlashbackEncodedFrames,
            DroppedFrames = health.FlashbackDroppedFrames,
            GpuEncoding = health.FlashbackGpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeProjection
    {
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
    }

    private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(
        FlashbackRecordingRuntimeProjection runtime)
        => new()
        {
            Active = runtime.Active,
            BufferedDurationMs = runtime.BufferedDurationMs,
            DiskBytes = runtime.DiskBytes,
            TotalBytesWritten = runtime.TotalBytesWritten,
            OutputBytes = runtime.OutputBytes,
            FilePath = runtime.FilePath,
            EncodedFrames = runtime.EncodedFrames,
            DroppedFrames = runtime.DroppedFrames,
            GpuEncoding = runtime.GpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeFlattenedProjection
    {
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
    }

    private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
        => new()
        {
            SettingsStale = health.FlashbackBackendSettingsStale,
            SettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            ActiveFormat = health.FlashbackBackendActiveFormat,
            RequestedFormat = health.FlashbackBackendRequestedFormat,
            ActivePreset = health.FlashbackBackendActivePreset,
            RequestedPreset = health.FlashbackBackendRequestedPreset,
            ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason
        };

    private readonly record struct FlashbackRecordingBackendProjection
    {
        public bool SettingsStale { get; init; }
        public string SettingsStaleReason { get; init; }
        public string ActiveFormat { get; init; }
        public string RequestedFormat { get; init; }
        public string ActivePreset { get; init; }
        public string RequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
    }

    private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(
        FlashbackRecordingBackendProjection backend)
        => new()
        {
            SettingsStale = backend.SettingsStale,
            SettingsStaleReason = backend.SettingsStaleReason,
            ActiveFormat = backend.ActiveFormat,
            RequestedFormat = backend.RequestedFormat,
            ActivePreset = backend.ActivePreset,
            RequestedPreset = backend.RequestedPreset,
            ExportVerificationFormat = backend.ExportVerificationFormat,
            CodecDowngradeReason = backend.CodecDowngradeReason,
        };

    private readonly record struct FlashbackRecordingBackendFlattenedProjection
    {
        public bool SettingsStale { get; init; }
        public string SettingsStaleReason { get; init; }
        public string ActiveFormat { get; init; }
        public string RequestedFormat { get; init; }
        public string ActivePreset { get; init; }
        public string RequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
    }

    private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            CodecName = health.EncoderCodecName,
            TargetBitRate = health.EncoderTargetBitRate,
            Width = health.EncoderWidth,
            Height = health.EncoderHeight,
            FrameRate = health.EncoderFrameRate,
            FrameRateNumerator = health.EncoderFrameRateNumerator,
            FrameRateDenominator = health.EncoderFrameRateDenominator
        };

    private readonly record struct FlashbackRecordingEncoderProjection
    {
        public string? CodecName { get; init; }
        public uint TargetBitRate { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public double FrameRate { get; init; }
        public int? FrameRateNumerator { get; init; }
        public int? FrameRateDenominator { get; init; }
    }

    private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(
        FlashbackRecordingEncoderProjection encoder)
        => new()
        {
            CodecName = encoder.CodecName,
            TargetBitRate = encoder.TargetBitRate,
            Width = encoder.Width,
            Height = encoder.Height,
            FrameRate = encoder.FrameRate,
            FrameRateNumerator = encoder.FrameRateNumerator,
            FrameRateDenominator = encoder.FrameRateDenominator
        };

    private readonly record struct FlashbackRecordingEncoderFlattenedProjection
    {
        public string? CodecName { get; init; }
        public uint TargetBitRate { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public double FrameRate { get; init; }
        public int? FrameRateNumerator { get; init; }
        public int? FrameRateDenominator { get; init; }
    }

    private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            EncodingFailed = flashbackRecording.EncodingFailed,
            EncodingFailureType = flashbackRecording.EncodingFailureType,
            EncodingFailureMessage = flashbackRecording.EncodingFailureMessage,
            FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress,
            CleanupInProgress = flashbackRecording.CleanupInProgress,
            ForceRotateActive = flashbackRecording.ForceRotateActive,
            ForceRotateRequested = flashbackRecording.ForceRotateRequested,
            ForceRotateDraining = flashbackRecording.ForceRotateDraining,
            StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),
            Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),
            Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),
            Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),
            Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)
        };

    private readonly record struct FlashbackRecordingFlattenedProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }
        public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }
        public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }
        public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }
    }

private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)
    {
        var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);
        var timing = BuildFlashbackPlaybackTimingProjection(health);
        var decode = BuildFlashbackPlaybackDecodeProjection(health);
        var commands = BuildFlashbackPlaybackCommandProjection(health);

        return new()
        {
            State = health.FlashbackPlaybackState,
            PositionMs = health.FlashbackPlaybackPositionMs,
            DecoderHwAccel = health.FlashbackDecoderHwAccel,
            FrameCount = health.FlashbackPlaybackFrameCount,
            LateFrames = health.FlashbackPlaybackLateFrames,
            DroppedFrames = health.FlashbackPlaybackDroppedFrames,
            AudioMaster = audioMaster,
            Timing = timing,
            Decode = decode,
            Commands = commands
        };
    }

    private readonly record struct FlashbackPlaybackProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingProjection Timing { get; init; }
        public FlashbackPlaybackDecodeProjection Decode { get; init; }
        public FlashbackPlaybackCommandProjection Commands { get; init; }
    }

    private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)
        => new()
        {
            DelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            LastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs
        };

    private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(
        FlashbackPlaybackAudioMasterProjection audioMaster)
        => new()
        {
            DelayDoubles = audioMaster.DelayDoubles,
            DelayShrinks = audioMaster.DelayShrinks,
            Fallbacks = audioMaster.Fallbacks,
            UnavailableFallbacks = audioMaster.UnavailableFallbacks,
            StaleFallbacks = audioMaster.StaleFallbacks,
            DriftOutlierFallbacks = audioMaster.DriftOutlierFallbacks,
            LastFallbackReason = audioMaster.LastFallbackReason,
            LastFallbackDriftMs = audioMaster.LastFallbackDriftMs,
            LastFallbackClockAgeMs = audioMaster.LastFallbackClockAgeMs
        };

    private readonly record struct FlashbackPlaybackAudioMasterProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }

    private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
            SegmentSwitches = health.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens = health.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits = health.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps = health.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps = health.FlashbackPlaybackDecodeErrorSnaps,
            SubmitFailures = health.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs = health.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason = health.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs = health.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure = health.FlashbackPlaybackLastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = health.FlashbackPlaybackLastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = health.FlashbackPlaybackLastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = health.FlashbackPlaybackLastWriteHeadWaitGapMs,
            TargetFps = health.FlashbackPlaybackTargetFps,
            ObservedFps = health.FlashbackPlaybackObservedFps,
            AvgFrameMs = health.FlashbackPlaybackAvgFrameMs,
            CadenceSampleCount = health.FlashbackPlaybackCadenceSampleCount,
            P95FrameMs = health.FlashbackPlaybackP95FrameMs,
            P99FrameMs = health.FlashbackPlaybackP99FrameMs,
            MaxFrameMs = health.FlashbackPlaybackMaxFrameMs,
            SlowFrames = health.FlashbackPlaybackSlowFrames,
            SlowFramePercent = health.FlashbackPlaybackSlowFramePercent,
            OnePercentLowFps = health.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps = health.FlashbackPlaybackFivePercentLowFps,
            SampleDurationMs = health.FlashbackPlaybackSampleDurationMs,
            RecentFrameIntervalsMs = health.FlashbackPlaybackRecentFrameIntervalsMs,
            PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = health.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = health.FlashbackPlaybackLastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = health.FlashbackPlaybackLastPtsCadenceExpectedMs,
            AvDriftMs = health.FlashbackAvDriftMs
        };

    private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(
        FlashbackPlaybackTimingProjection timing)
        => new()
        {
            SegmentSwitches = timing.SegmentSwitches,
            Fmp4Reopens = timing.Fmp4Reopens,
            WriteHeadWaits = timing.WriteHeadWaits,
            NearLiveSnaps = timing.NearLiveSnaps,
            DecodeErrorSnaps = timing.DecodeErrorSnaps,
            SubmitFailures = timing.SubmitFailures,
            LastDropUtcUnixMs = timing.LastDropUtcUnixMs,
            LastDropReason = timing.LastDropReason,
            LastSubmitFailureUtcUnixMs = timing.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure = timing.LastSubmitFailure,
            LastSegmentSwitchUtcUnixMs = timing.LastSegmentSwitchUtcUnixMs,
            LastFmp4ReopenUtcUnixMs = timing.LastFmp4ReopenUtcUnixMs,
            LastWriteHeadWaitGapMs = timing.LastWriteHeadWaitGapMs,
            TargetFps = timing.TargetFps,
            ObservedFps = timing.ObservedFps,
            AvgFrameMs = timing.AvgFrameMs,
            CadenceSampleCount = timing.CadenceSampleCount,
            P95FrameMs = timing.P95FrameMs,
            P99FrameMs = timing.P99FrameMs,
            MaxFrameMs = timing.MaxFrameMs,
            SlowFrames = timing.SlowFrames,
            SlowFramePercent = timing.SlowFramePercent,
            OnePercentLowFps = timing.OnePercentLowFps,
            FivePercentLowFps = timing.FivePercentLowFps,
            SampleDurationMs = timing.SampleDurationMs,
            RecentFrameIntervalsMs = timing.RecentFrameIntervalsMs,
            PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,
            LastPtsCadenceMismatchUtcUnixMs = timing.LastPtsCadenceMismatchUtcUnixMs,
            LastPtsCadenceDeltaMs = timing.LastPtsCadenceDeltaMs,
            LastPtsCadenceExpectedMs = timing.LastPtsCadenceExpectedMs,
            AvDriftMs = timing.AvDriftMs
        };

    private readonly record struct FlashbackPlaybackTimingProjection
    {
        public long SegmentSwitches { get; init; }
        public long Fmp4Reopens { get; init; }
        public long WriteHeadWaits { get; init; }
        public long NearLiveSnaps { get; init; }
        public long DecodeErrorSnaps { get; init; }
        public long SubmitFailures { get; init; }
        public long LastDropUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public long LastSubmitFailureUtcUnixMs { get; init; }
        public string LastSubmitFailure { get; init; }
        public long LastSegmentSwitchUtcUnixMs { get; init; }
        public long LastFmp4ReopenUtcUnixMs { get; init; }
        public long LastWriteHeadWaitGapMs { get; init; }
        public double TargetFps { get; init; }
        public double ObservedFps { get; init; }
        public double AvgFrameMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double P95FrameMs { get; init; }
        public double P99FrameMs { get; init; }
        public double MaxFrameMs { get; init; }
        public long SlowFrames { get; init; }
        public double SlowFramePercent { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentFrameIntervalsMs { get; init; }
        public long PtsCadenceMismatchCount { get; init; }
        public long LastPtsCadenceMismatchUtcUnixMs { get; init; }
        public double LastPtsCadenceDeltaMs { get; init; }
        public double LastPtsCadenceExpectedMs { get; init; }
        public double AvDriftMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackTimingFlattenedProjection
    {
        public long SegmentSwitches { get; init; }
        public long Fmp4Reopens { get; init; }
        public long WriteHeadWaits { get; init; }
        public long NearLiveSnaps { get; init; }
        public long DecodeErrorSnaps { get; init; }
        public long SubmitFailures { get; init; }
        public long LastDropUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public long LastSubmitFailureUtcUnixMs { get; init; }
        public string LastSubmitFailure { get; init; }
        public long LastSegmentSwitchUtcUnixMs { get; init; }
        public long LastFmp4ReopenUtcUnixMs { get; init; }
        public long LastWriteHeadWaitGapMs { get; init; }
        public double TargetFps { get; init; }
        public double ObservedFps { get; init; }
        public double AvgFrameMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double P95FrameMs { get; init; }
        public double P99FrameMs { get; init; }
        public double MaxFrameMs { get; init; }
        public long SlowFrames { get; init; }
        public double SlowFramePercent { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentFrameIntervalsMs { get; init; }
        public long PtsCadenceMismatchCount { get; init; }
        public long LastPtsCadenceMismatchUtcUnixMs { get; init; }
        public double LastPtsCadenceDeltaMs { get; init; }
        public double LastPtsCadenceExpectedMs { get; init; }
        public double AvDriftMs { get; init; }
    }

    private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)
        => new()
        {
            SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = health.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            SampleCount = health.FlashbackPlaybackDecodeSampleCount,
            AvgMs = health.FlashbackPlaybackDecodeAvgMs,
            P95Ms = health.FlashbackPlaybackDecodeP95Ms,
            P99Ms = health.FlashbackPlaybackDecodeP99Ms,
            MaxMs = health.FlashbackPlaybackDecodeMaxMs,
            MaxPhase = health.FlashbackPlaybackMaxDecodePhase,
            MaxReceiveMs = health.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxFeedMs = health.FlashbackPlaybackMaxDecodeFeedMs,
            MaxReadMs = health.FlashbackPlaybackMaxDecodeReadMs,
            MaxSendMs = health.FlashbackPlaybackMaxDecodeSendMs,
            MaxAudioMs = health.FlashbackPlaybackMaxDecodeAudioMs,
            MaxConvertMs = health.FlashbackPlaybackMaxDecodeConvertMs,
            MaxUtcUnixMs = health.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs
        };

    private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(
        FlashbackPlaybackDecodeProjection decode)
        => new()
        {
            SeekForwardDecodeCapHits = decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap = decode.LastSeekHitForwardDecodeCap,
            SampleCount = decode.SampleCount,
            AvgMs = decode.AvgMs,
            P95Ms = decode.P95Ms,
            P99Ms = decode.P99Ms,
            MaxMs = decode.MaxMs,
            MaxPhase = decode.MaxPhase,
            MaxReceiveMs = decode.MaxReceiveMs,
            MaxFeedMs = decode.MaxFeedMs,
            MaxReadMs = decode.MaxReadMs,
            MaxSendMs = decode.MaxSendMs,
            MaxAudioMs = decode.MaxAudioMs,
            MaxConvertMs = decode.MaxConvertMs,
            MaxUtcUnixMs = decode.MaxUtcUnixMs,
            MaxPositionMs = decode.MaxPositionMs
        };

    private readonly record struct FlashbackPlaybackDecodeProjection
    {
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
        public string MaxPhase { get; init; }
        public double MaxReceiveMs { get; init; }
        public double MaxFeedMs { get; init; }
        public double MaxReadMs { get; init; }
        public double MaxSendMs { get; init; }
        public double MaxAudioMs { get; init; }
        public double MaxConvertMs { get; init; }
        public long MaxUtcUnixMs { get; init; }
        public long MaxPositionMs { get; init; }
    }

    private readonly record struct FlashbackPlaybackDecodeFlattenedProjection
    {
        public long SeekForwardDecodeCapHits { get; init; }
        public bool LastSeekHitForwardDecodeCap { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
        public string MaxPhase { get; init; }
        public double MaxReceiveMs { get; init; }
        public double MaxFeedMs { get; init; }
        public double MaxReadMs { get; init; }
        public double MaxSendMs { get; init; }
        public double MaxAudioMs { get; init; }
        public double MaxConvertMs { get; init; }
        public long MaxUtcUnixMs { get; init; }
        public long MaxPositionMs { get; init; }
    }

    private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)
        => new()
        {
            ThreadAlive = health.FlashbackPlaybackThreadAlive,
            Enqueued = health.FlashbackPlaybackCommandsEnqueued,
            Processed = health.FlashbackPlaybackCommandsProcessed,
            Dropped = health.FlashbackPlaybackCommandsDropped,
            SkippedNotReady = health.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced = health.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced = health.FlashbackPlaybackSeekCommandsCoalesced,
            QueueCapacity = health.FlashbackPlaybackCommandQueueCapacity,
            Pending = health.FlashbackPlaybackPendingCommands,
            MaxPending = health.FlashbackPlaybackMaxPendingCommands,
            LastQueueLatencyMs = health.FlashbackPlaybackLastCommandQueueLatencyMs,
            MaxQueueLatencyMs = health.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxQueueLatencyCommand = health.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastQueued = health.FlashbackPlaybackLastCommandQueued,
            LastProcessed = health.FlashbackPlaybackLastCommandProcessed,
            LastQueuedUtcUnixMs = health.FlashbackPlaybackLastCommandQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = health.FlashbackPlaybackLastCommandProcessedUtcUnixMs,
            LastFailureUtcUnixMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastFailure = health.FlashbackPlaybackLastCommandFailure
        };

    private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(
        FlashbackPlaybackCommandProjection commands)
        => new()
        {
            ThreadAlive = commands.ThreadAlive,
            Enqueued = commands.Enqueued,
            Processed = commands.Processed,
            Dropped = commands.Dropped,
            SkippedNotReady = commands.SkippedNotReady,
            ScrubUpdatesCoalesced = commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced = commands.SeekCommandsCoalesced,
            QueueCapacity = commands.QueueCapacity,
            Pending = commands.Pending,
            MaxPending = commands.MaxPending,
            LastQueueLatencyMs = commands.LastQueueLatencyMs,
            MaxQueueLatencyMs = commands.MaxQueueLatencyMs,
            MaxQueueLatencyCommand = commands.MaxQueueLatencyCommand,
            LastQueued = commands.LastQueued,
            LastProcessed = commands.LastProcessed,
            LastQueuedUtcUnixMs = commands.LastQueuedUtcUnixMs,
            LastProcessedUtcUnixMs = commands.LastProcessedUtcUnixMs,
            LastFailureUtcUnixMs = commands.LastFailureUtcUnixMs,
            LastFailure = commands.LastFailure
        };

    private readonly record struct FlashbackPlaybackCommandProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }

    private readonly record struct FlashbackPlaybackCommandFlattenedProjection
    {
        public bool ThreadAlive { get; init; }
        public long Enqueued { get; init; }
        public long Processed { get; init; }
        public long Dropped { get; init; }
        public long SkippedNotReady { get; init; }
        public long ScrubUpdatesCoalesced { get; init; }
        public long SeekCommandsCoalesced { get; init; }
        public int QueueCapacity { get; init; }
        public int Pending { get; init; }
        public int MaxPending { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string MaxQueueLatencyCommand { get; init; }
        public string LastQueued { get; init; }
        public string LastProcessed { get; init; }
        public long LastQueuedUtcUnixMs { get; init; }
        public long LastProcessedUtcUnixMs { get; init; }
        public long LastFailureUtcUnixMs { get; init; }
        public string LastFailure { get; init; }
    }

    private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(
        FlashbackPlaybackProjection flashbackPlayback)
        => new()
        {
            State = flashbackPlayback.State,
            PositionMs = flashbackPlayback.PositionMs,
            DecoderHwAccel = flashbackPlayback.DecoderHwAccel,
            FrameCount = flashbackPlayback.FrameCount,
            LateFrames = flashbackPlayback.LateFrames,
            DroppedFrames = flashbackPlayback.DroppedFrames,
            AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),
            Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),
            Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),
            Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)
        };

    private readonly record struct FlashbackPlaybackFlattenedProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }
        public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }
        public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }
    }
}
