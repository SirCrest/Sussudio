static partial class Program
{
    private static void AssertDiagnosticsSnapshotStatusProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "TimestampUtc = DateTimeOffset.UtcNow,");
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
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "return new AutomationSnapshotProjectionSet(");

        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var audioDrops = BuildAudioDropsProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var previewD3D = BuildPreviewD3DProjection(");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");

        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
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

        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngestFlattening.AudioPeak,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.RecordingVideoQueueCapacity,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");

        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
    }
}
