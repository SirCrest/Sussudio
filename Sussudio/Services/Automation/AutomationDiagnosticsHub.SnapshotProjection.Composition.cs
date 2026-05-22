using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
