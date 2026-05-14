static partial class Program
{
    private static AutomationDiagnosticsHubSourceFamily ReadAutomationDiagnosticsHubSourceFamily()
    {
        return new AutomationDiagnosticsHubSourceFamily
        {
            HubText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            EvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            EvaluationPolicyText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.EvaluationPolicy.cs"),
            DiagnosticEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluation.cs"),
            DiagnosticEvaluationFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            SignalAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.cs"),
            FlashbackAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackAlerts.cs"),
            FlashbackRecordingAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs"),
            FlashbackPlaybackAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs"),
            FlashbackPlaybackCommandAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs"),
            FlashbackPlaybackPerformanceAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs"),
            EventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvents.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.cs"),
            LifecycleText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Lifecycle.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Hdr.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionSnapshotStatusText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs"),
            SnapshotProjectionSnapshotEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs"),
            SnapshotProjectionAvSyncText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs"),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionAudioDropsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs"),
            SnapshotProjectionAudioSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs"),
            SnapshotProjectionCaptureCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionCaptureTransportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs"),
            SnapshotProjectionMjpegTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs"),
            SnapshotProjectionMjpegPacketHashText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs"),
            SnapshotProjectionFlashbackExportLastResultText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackExportLastResult.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs"),
            SnapshotProjectionFlashbackPlaybackAudioMasterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs"),
            SnapshotProjectionFlashbackPlaybackDecodeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs"),
            SnapshotProjectionFlashbackPlaybackCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs"),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs"),
            SnapshotProjectionFlashbackRecordingStartupCacheText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs"),
            SnapshotProjectionPreviewD3DFrameLatencyWaitText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs"),
            SnapshotProjectionPreviewD3DFrameStatsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs"),
            SnapshotProjectionPreviewRuntimeCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs"),
            SnapshotProjectionPreviewRuntimeStartupText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs"),
            SnapshotProjectionRecordingBackendText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs"),
            SnapshotProjectionRecordingOutputText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs"),
            SnapshotProjectionSourceSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs"),
            SnapshotProjectionSourceTelemetryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs"),
            SnapshotProjectionUserSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs"),
            SnapshotProjectionRecordingSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingSettings.cs"),
            SnapshotProjectionHdrPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs"),
            SnapshotProjectionHdrTruthText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.HdrTruth.cs"),
            SnapshotStateText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotState.cs"),
            PreviewPacingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.PreviewPacing.cs"),
            OutputFilesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.OutputFiles.cs"),
            ProcessMetricsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.ProcessMetrics.cs"),
            TimelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Timeline.cs"),
            TimelineProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.TimelineProjection.cs"),
        };
    }

    private static string ReadAutomationDiagnosticsHubSourceFile(string fileName)
    {
        return ReadRepoFile("Sussudio/Services/Automation/" + fileName)
            .Replace("\r\n", "\n");
    }

    private sealed class AutomationDiagnosticsHubSourceFamily
    {
        private string? _sourceFamilyText;

        public string HubText { get; init; } = string.Empty;
        public string EvaluationText { get; init; } = string.Empty;
        public string EvaluationPolicyText { get; init; } = string.Empty;
        public string DiagnosticEvaluationText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesText { get; init; } = string.Empty;
        public string AlertsText { get; init; } = string.Empty;
        public string SignalAlertsText { get; init; } = string.Empty;
        public string FlashbackAlertsText { get; init; } = string.Empty;
        public string FlashbackRecordingAlertsText { get; init; } = string.Empty;
        public string FlashbackPlaybackAlertsText { get; init; } = string.Empty;
        public string FlashbackPlaybackCommandAlertsText { get; init; } = string.Empty;
        public string FlashbackPlaybackPerformanceAlertsText { get; init; } = string.Empty;
        public string EventsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string LifecycleText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotProjectionText { get; init; } = string.Empty;
        public string SnapshotProjectionSnapshotStatusText { get; init; } = string.Empty;
        public string SnapshotProjectionSnapshotEvaluationText { get; init; } = string.Empty;
        public string SnapshotProjectionAvSyncText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioDropsText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioSignalText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureIngestText { get; init; } = string.Empty;
        public string SnapshotProjectionWasapiAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCommandsText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureFormatText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureTransportText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPreviewJitterText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPacketHashText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackExportText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackExportLastResultText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackAudioMasterText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackDecodeText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackCommandsText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingStartupCacheText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingQueuesText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DCpuTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameFlowText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameLatencyWaitText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameStatsText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeStartupText { get; init; } = string.Empty;
        public string SnapshotProjectionProcessResourcesText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingIntegrityText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingBackendText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingOutputText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceSignalText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceTelemetryText { get; init; } = string.Empty;
        public string SnapshotProjectionUserSettingsText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingSettingsText { get; init; } = string.Empty;
        public string SnapshotProjectionHdrPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionHdrTruthText { get; init; } = string.Empty;
        public string SnapshotStateText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string OutputFilesText { get; init; } = string.Empty;
        public string ProcessMetricsText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionText { get; init; } = string.Empty;

        public string SourceFamilyText => _sourceFamilyText ??= string.Join(
            "\n",
            new[]
            {
                HubText,
                EvaluationText,
                EvaluationPolicyText,
                DiagnosticEvaluationText,
                DiagnosticEvaluationFlashbackText,
                DiagnosticEvaluationRealtimeText,
                DiagnosticEvaluationLanesText,
                AlertsText,
                SignalAlertsText,
                FlashbackAlertsText,
                FlashbackRecordingAlertsText,
                FlashbackPlaybackAlertsText,
                FlashbackPlaybackCommandAlertsText,
                FlashbackPlaybackPerformanceAlertsText,
                EventsText,
                VerificationText,
                LifecycleText,
                HdrText,
                SnapshotsText,
                SnapshotProjectionText,
                SnapshotProjectionSnapshotStatusText,
                SnapshotProjectionSnapshotEvaluationText,
                SnapshotProjectionAvSyncText,
                SnapshotProjectionAudioText,
                SnapshotProjectionAudioDropsText,
                SnapshotProjectionAudioSignalText,
                SnapshotProjectionCaptureIngestText,
                SnapshotProjectionWasapiAudioText,
                SnapshotProjectionCaptureCommandsText,
                SnapshotProjectionCaptureFormatText,
                SnapshotProjectionCaptureTransportText,
                SnapshotProjectionCaptureCadenceText,
                SnapshotProjectionMjpegText,
                SnapshotProjectionMjpegPreviewJitterText,
                SnapshotProjectionMjpegPacketHashText,
                SnapshotProjectionFlashbackExportText,
                SnapshotProjectionFlashbackExportLastResultText,
                SnapshotProjectionFlashbackPlaybackText,
                SnapshotProjectionFlashbackPlaybackAudioMasterText,
                SnapshotProjectionFlashbackPlaybackDecodeText,
                SnapshotProjectionFlashbackPlaybackCommandsText,
                SnapshotProjectionFlashbackRecordingText,
                SnapshotProjectionFlashbackRecordingStartupCacheText,
                SnapshotProjectionFlashbackRecordingQueuesText,
                SnapshotProjectionPreviewD3DText,
                SnapshotProjectionPreviewD3DFrameLatencyWaitText,
                SnapshotProjectionPreviewD3DFrameStatsText,
                SnapshotProjectionPreviewRuntimeText,
                SnapshotProjectionProcessResourcesText,
                SnapshotProjectionRecordingIntegrityText,
                SnapshotProjectionRecordingBackendText,
                SnapshotProjectionRecordingPipelineText,
                SnapshotProjectionRecordingOutputText,
                SnapshotProjectionSourceSignalText,
                SnapshotProjectionSourceTelemetryText,
                SnapshotProjectionUserSettingsText,
                SnapshotProjectionRecordingSettingsText,
                SnapshotProjectionHdrPipelineText,
                SnapshotProjectionHdrTruthText,
                SnapshotStateText,
                PreviewPacingText,
                OutputFilesText,
                ProcessMetricsText,
                TimelineText,
                TimelineProjectionText,
                SnapshotProjectionPreviewD3DCpuTimingText,
                SnapshotProjectionPreviewD3DFrameFlowText,
                SnapshotProjectionPreviewRuntimeCadenceText,
                SnapshotProjectionPreviewRuntimeStartupText,
                SnapshotProjectionMjpegTimingText,
            });
    }
}
