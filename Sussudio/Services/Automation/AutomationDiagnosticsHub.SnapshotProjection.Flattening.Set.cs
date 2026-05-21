namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
