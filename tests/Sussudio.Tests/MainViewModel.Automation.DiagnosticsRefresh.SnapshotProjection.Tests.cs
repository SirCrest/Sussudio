static partial class Program
{
    private static void AssertDiagnosticsSnapshotStatusProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(diagnostics.SnapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
    }

    private static void AssertDiagnosticsRefreshSnapshotProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshSnapshotCompositionRoutesThroughProjectionSet(diagnostics);
        AssertDiagnosticsRefreshSnapshotFlatteningRoutesThroughFlattenedProjections(diagnostics);
    }

    private static void AssertDiagnosticsRefreshSnapshotCompositionRoutesThroughProjectionSet(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFromProjections(projections);");
        AssertContains(diagnostics.SnapshotProjectionText, "return new AutomationSnapshotProjectionSet(");

        AssertContains(diagnostics.SnapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(diagnostics.SnapshotProjectionText, "var audioDrops = BuildAudioDropsProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(diagnostics.SnapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(diagnostics.SnapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(");
        AssertContains(diagnostics.SnapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(diagnostics.SnapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");

        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
    }

    private static void AssertDiagnosticsRefreshSnapshotFlatteningRoutesThroughFlattenedProjections(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var audioAndIngestFlattening = BuildAudioAndIngestFlattenedProjection(audioAndIngest);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var audioDropsFlattening = BuildAudioDropsFlattenedProjection(audioDrops);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");

        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngestFlattening.Signal.Peak,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");

        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
    }
}
