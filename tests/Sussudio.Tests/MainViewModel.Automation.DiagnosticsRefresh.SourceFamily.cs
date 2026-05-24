static partial class Program
{
    private static AutomationDiagnosticsHubSourceFamily ReadAutomationDiagnosticsHubSourceFamily()
    {
        return new AutomationDiagnosticsHubSourceFamily
        {
            HubText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            EvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs"),
            DiagnosticEvaluationFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            FlashbackRecordingAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs"),
            EventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvents.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Hdr.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSnapshotsSource(),
            SnapshotsCoreText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionFlatteningText = ReadAutomationSnapshotFlatteningFamilyText(),
            SnapshotProjectionFlatteningAutomationSnapshotText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs"),
            SnapshotProjectionVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubFlashbackPlaybackProjectionSource(),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs"),
            SnapshotProjectionSourceSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs"),
            SnapshotProjectionSourceTelemetryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs"),
            SnapshotProjectionUserSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs"),
            PreviewPacingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            TimelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Timeline.cs"),
            TimelineProjectionPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Timeline.cs"),
            TimelineProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Timeline.cs"),
        };
    }

    private static string ReadAutomationDiagnosticsHubSourceFile(string fileName)
    {
        return ReadRepoFile("Sussudio/Services/Automation/" + fileName)
            .Replace("\r\n", "\n");
    }

    private static string ReadAutomationDiagnosticsHubFlashbackPlaybackProjectionSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs"),
            });
    }

    private static string ReadAutomationDiagnosticsHubSnapshotsSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            });
    }

    private sealed partial class AutomationDiagnosticsHubSourceFamily
    {
        private string? _sourceFamilyText;

        public string HubText { get; init; } = string.Empty;
        public string EvaluationText { get; init; } = string.Empty;
        public string EventsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotsCoreText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionPreviewText { get; init; } = string.Empty;
        public string TimelineProjectionFlashbackPlaybackText { get; init; } = string.Empty;
    }
}
