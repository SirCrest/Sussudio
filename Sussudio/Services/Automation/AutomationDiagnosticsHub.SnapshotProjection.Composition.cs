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
}
