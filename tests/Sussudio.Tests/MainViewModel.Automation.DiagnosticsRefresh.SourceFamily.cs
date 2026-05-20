static partial class Program
{
    private static AutomationDiagnosticsHubSourceFamily ReadAutomationDiagnosticsHubSourceFamily()
    {
        return new AutomationDiagnosticsHubSourceFamily
        {
            HubText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            EvaluationModelsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.EvaluationModels.cs"),
            EvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            EvaluationPolicyText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.EvaluationPolicy.cs"),
            DiagnosticEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluation.cs"),
            DiagnosticEvaluationFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs"),
            DiagnosticEvaluationFlashbackStorageText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Storage.cs"),
            DiagnosticEvaluationFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs"),
            DiagnosticEvaluationFlashbackRecordingConditionsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.RecordingConditions.cs"),
            DiagnosticEvaluationFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs"),
            DiagnosticEvaluationFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs"),
            DiagnosticEvaluationRealtimeStateText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.State.cs"),
            DiagnosticEvaluationRealtimeRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Recording.cs"),
            DiagnosticEvaluationRealtimeSourceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Source.cs"),
            DiagnosticEvaluationRealtimeMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Mjpeg.cs"),
            DiagnosticEvaluationRealtimePreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs"),
            DiagnosticEvaluationRealtimePreviewSchedulerText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewScheduler.cs"),
            DiagnosticEvaluationRealtimePreviewPresentText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewPresent.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs"),
            DiagnosticEvaluationLanesRealtimeSourceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Source.cs"),
            DiagnosticEvaluationLanesRealtimeMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Mjpeg.cs"),
            DiagnosticEvaluationLanesRealtimePreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Preview.cs"),
            DiagnosticEvaluationLanesRealtimeRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Recording.cs"),
            DiagnosticEvaluationLanesFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Recording.cs"),
            DiagnosticEvaluationLanesFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Export.cs"),
            DiagnosticEvaluationLanesFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Playback.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            SignalAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.cs"),
            SignalAlertsPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.Preview.cs"),
            SignalAlertsCaptureText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.Capture.cs"),
            SignalAlertsAudioRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.AudioRecording.cs"),
            FlashbackRecordingAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs"),
            FlashbackRecordingAlertsExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.Export.cs"),
            FlashbackRecordingAlertsStorageText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.Storage.cs"),
            FlashbackRecordingAlertsEncoderText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.Encoder.cs"),
            FlashbackRecordingAlertsDegradationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.Degradation.cs"),
            FlashbackPlaybackAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs"),
            FlashbackPlaybackAlertsCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackAlerts.Commands.cs"),
            FlashbackPlaybackPerformanceAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs"),
            FlashbackPlaybackPerformanceAlertsAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Audio.cs"),
            FlashbackPlaybackPerformanceAlertsCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs"),
            FlashbackPlaybackPerformanceAlertsSubmitText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Submit.cs"),
            EventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvents.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.cs"),
            VerificationAutoText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.Auto.cs"),
            VerificationProfileText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.Profile.cs"),
            LifecycleText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Lifecycle.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Hdr.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionCompositionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Composition.cs"),
            SnapshotProjectionFlatteningText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs"),
            SnapshotProjectionFlatteningCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.cs"),
            SnapshotProjectionFlatteningCaptureTransportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureTransport.cs"),
            SnapshotProjectionFlatteningCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCadence.cs"),
            SnapshotProjectionFlatteningVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.VisualCadence.cs"),
            SnapshotProjectionFlatteningMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Mjpeg.cs"),
            SnapshotProjectionFlatteningMjpegTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegTiming.cs"),
            SnapshotProjectionFlatteningMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.cs"),
            SnapshotProjectionFlatteningMjpegPacketHashText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPacketHash.cs"),
            SnapshotProjectionFlatteningSourceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs"),
            SnapshotProjectionFlatteningSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs"),
            SnapshotProjectionFlatteningHdrPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs"),
            SnapshotProjectionFlatteningPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.cs"),
            SnapshotProjectionFlatteningPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs"),
            SnapshotProjectionFlatteningPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.CpuTiming.cs"),
            SnapshotProjectionFlatteningPreviewD3DLatencyAndStatsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.LatencyAndStats.cs"),
            SnapshotProjectionFlatteningPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.FrameFlow.cs"),
            SnapshotProjectionFlatteningFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackExport.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingStartupCacheText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.StartupCache.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Queues.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Runtime.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingBackendText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Backend.cs"),
            SnapshotProjectionFlatteningFlashbackRecordingEncoderText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Encoder.cs"),
            SnapshotProjectionFlatteningFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.cs"),
            SnapshotProjectionFlatteningFlashbackPlaybackAudioMasterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.AudioMaster.cs"),
            SnapshotProjectionFlatteningFlashbackPlaybackTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Timing.cs"),
            SnapshotProjectionFlatteningFlashbackPlaybackDecodeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Decode.cs"),
            SnapshotProjectionFlatteningFlashbackPlaybackCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Commands.cs"),
            SnapshotProjectionSnapshotStatusText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs"),
            SnapshotProjectionFlatteningSnapshotStatusText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotStatus.cs"),
            SnapshotProjectionSnapshotEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs"),
            SnapshotProjectionFlatteningSnapshotEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotEvaluation.cs"),
            SnapshotProjectionAvSyncText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs"),
            SnapshotProjectionFlatteningAvSyncText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AvSync.cs"),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionFlatteningAudioAndIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.cs"),
            SnapshotProjectionFlatteningAudioDropsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioDrops.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs"),
            SnapshotProjectionCaptureCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs"),
            SnapshotProjectionFlatteningCaptureCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCommands.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionCaptureTransportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs"),
            SnapshotProjectionVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs"),
            SnapshotProjectionMjpegTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs"),
            SnapshotProjectionMjpegPacketHashText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubFlashbackPlaybackProjectionSource(),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameFlow.cs"),
            SnapshotProjectionPreviewD3DFrameLatencyWaitText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameLatencyWait.cs"),
            SnapshotProjectionPreviewD3DFrameStatsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameStats.cs"),
            SnapshotProjectionPreviewD3DPipelineLatencyText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.PipelineLatency.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs"),
            SnapshotProjectionFlatteningProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.ProcessResources.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs"),
            SnapshotProjectionFlatteningRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs"),
            SnapshotProjectionFlatteningRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.cs"),
            SnapshotProjectionRecordingOutputText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs"),
            SnapshotProjectionFlatteningRecordingOutputText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingOutput.cs"),
            SnapshotProjectionSourceSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs"),
            SnapshotProjectionSourceTelemetryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs"),
            SnapshotProjectionUserSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs"),
            SnapshotProjectionHdrPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs"),
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

    private static string ReadAutomationDiagnosticsHubFlashbackPlaybackProjectionSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.AudioMaster.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Decode.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Commands.cs"),
            });
    }

    private sealed partial class AutomationDiagnosticsHubSourceFamily
    {
        private string? _sourceFamilyText;

        public string HubText { get; init; } = string.Empty;
        public string EvaluationModelsText { get; init; } = string.Empty;
        public string EvaluationText { get; init; } = string.Empty;
        public string EvaluationPolicyText { get; init; } = string.Empty;
        public string EventsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string VerificationAutoText { get; init; } = string.Empty;
        public string VerificationProfileText { get; init; } = string.Empty;
        public string LifecycleText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotStateText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string OutputFilesText { get; init; } = string.Empty;
        public string ProcessMetricsText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionText { get; init; } = string.Empty;
    }
}
