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
            DiagnosticEvaluationLanesRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.cs"),
            DiagnosticEvaluationLanesFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            SignalAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.cs"),
            FlashbackRecordingAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs"),
            FlashbackPlaybackAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs"),
            FlashbackPlaybackPerformanceAlertsCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs"),
            EventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvents.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.cs"),
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
            SnapshotProjectionFlatteningMjpegTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegTiming.cs"),
            SnapshotProjectionFlatteningMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.cs"),
            SnapshotProjectionFlatteningMjpegPacketHashText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPacketHash.cs"),
            SnapshotProjectionFlatteningSourceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs"),
            SnapshotProjectionFlatteningSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs"),
            SnapshotProjectionFlatteningHdrPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs"),
            SnapshotProjectionFlatteningPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.cs"),
            SnapshotProjectionFlatteningPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs"),
            SnapshotProjectionFlatteningFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackExport.cs"),
            SnapshotProjectionSnapshotStatusText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs"),
            SnapshotProjectionSnapshotEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs"),
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

    private sealed class AutomationDiagnosticsHubSourceFamily
    {
        private string? _sourceFamilyText;

        public string HubText { get; init; } = string.Empty;
        public string EvaluationModelsText { get; init; } = string.Empty;
        public string EvaluationText { get; init; } = string.Empty;
        public string EvaluationPolicyText { get; init; } = string.Empty;
        public string DiagnosticEvaluationText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackStorageText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackRecordingText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackRecordingConditionsText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackExportText { get; init; } = string.Empty;
        public string DiagnosticEvaluationFlashbackPlaybackText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeStateText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeRecordingText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeSourceText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeMjpegText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimePreviewText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimePreviewSchedulerText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimePreviewPresentText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesRealtimeText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesFlashbackText { get; init; } = string.Empty;
        public string AlertsText { get; init; } = string.Empty;
        public string SignalAlertsText { get; init; } = string.Empty;
        public string FlashbackRecordingAlertsText { get; init; } = string.Empty;
        public string FlashbackPlaybackAlertsText { get; init; } = string.Empty;
        public string FlashbackPlaybackPerformanceAlertsCadenceText { get; init; } = string.Empty;
        public string EventsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string LifecycleText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotProjectionText { get; init; } = string.Empty;
        public string SnapshotProjectionCompositionText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningCaptureFormatText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningCaptureTransportText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningCaptureCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningVisualCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningMjpegTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningMjpegPreviewJitterText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningMjpegPacketHashText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningSourceText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningSettingsText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningHdrPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningPreviewRuntimeText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningPreviewD3DText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningFlashbackExportText { get; init; } = string.Empty;
        public string SnapshotProjectionSnapshotStatusText { get; init; } = string.Empty;
        public string SnapshotProjectionSnapshotEvaluationText { get; init; } = string.Empty;
        public string SnapshotProjectionAvSyncText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningAvSyncText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningAudioAndIngestText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningAudioDropsText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureIngestText { get; init; } = string.Empty;
        public string SnapshotProjectionWasapiAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCommandsText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningCaptureCommandsText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureFormatText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureTransportText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionVisualCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPreviewJitterText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPacketHashText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackExportText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingQueuesText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameFlowText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameLatencyWaitText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameStatsText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DPipelineLatencyText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DCpuTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeText { get; init; } = string.Empty;
        public string SnapshotProjectionProcessResourcesText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningProcessResourcesText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingIntegrityText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningRecordingIntegrityText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingOutputText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningRecordingOutputText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceSignalText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceTelemetryText { get; init; } = string.Empty;
        public string SnapshotProjectionUserSettingsText { get; init; } = string.Empty;
        public string SnapshotProjectionHdrPipelineText { get; init; } = string.Empty;
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
                EvaluationModelsText,
                EvaluationText,
                EvaluationPolicyText,
                DiagnosticEvaluationText,
                DiagnosticEvaluationFlashbackText,
                DiagnosticEvaluationFlashbackStorageText,
                DiagnosticEvaluationFlashbackRecordingText,
                DiagnosticEvaluationFlashbackRecordingConditionsText,
                DiagnosticEvaluationFlashbackExportText,
                DiagnosticEvaluationFlashbackPlaybackText,
                DiagnosticEvaluationRealtimeText,
                DiagnosticEvaluationRealtimeStateText,
                DiagnosticEvaluationRealtimeRecordingText,
                DiagnosticEvaluationRealtimeSourceText,
                DiagnosticEvaluationRealtimeMjpegText,
                DiagnosticEvaluationRealtimePreviewText,
                DiagnosticEvaluationRealtimePreviewSchedulerText,
                DiagnosticEvaluationRealtimePreviewPresentText,
                DiagnosticEvaluationLanesText,
                DiagnosticEvaluationLanesRealtimeText,
                DiagnosticEvaluationLanesFlashbackText,
                AlertsText,
                SignalAlertsText,
                FlashbackRecordingAlertsText,
                FlashbackPlaybackAlertsText,
                FlashbackPlaybackPerformanceAlertsCadenceText,
                EventsText,
                VerificationText,
                LifecycleText,
                HdrText,
                SnapshotsText,
                SnapshotProjectionText,
                SnapshotProjectionCompositionText,
                SnapshotProjectionFlatteningText,
                SnapshotProjectionFlatteningCaptureFormatText,
                SnapshotProjectionFlatteningCaptureTransportText,
                SnapshotProjectionFlatteningCaptureCadenceText,
                SnapshotProjectionFlatteningVisualCadenceText,
                SnapshotProjectionFlatteningMjpegTimingText,
                SnapshotProjectionFlatteningMjpegPreviewJitterText,
                SnapshotProjectionFlatteningMjpegPacketHashText,
                SnapshotProjectionFlatteningSourceText,
                SnapshotProjectionFlatteningSettingsText,
                SnapshotProjectionFlatteningHdrPipelineText,
                SnapshotProjectionFlatteningPreviewRuntimeText,
                SnapshotProjectionFlatteningPreviewD3DText,
                SnapshotProjectionFlatteningFlashbackExportText,
                SnapshotProjectionSnapshotStatusText,
                SnapshotProjectionSnapshotEvaluationText,
                SnapshotProjectionAvSyncText,
                SnapshotProjectionFlatteningAvSyncText,
                SnapshotProjectionAudioText,
                SnapshotProjectionFlatteningAudioAndIngestText,
                SnapshotProjectionFlatteningAudioDropsText,
                SnapshotProjectionCaptureIngestText,
                SnapshotProjectionWasapiAudioText,
                SnapshotProjectionCaptureCommandsText,
                SnapshotProjectionFlatteningCaptureCommandsText,
                SnapshotProjectionCaptureFormatText,
                SnapshotProjectionCaptureTransportText,
                SnapshotProjectionCaptureCadenceText,
                SnapshotProjectionMjpegText,
                SnapshotProjectionMjpegPreviewJitterText,
                SnapshotProjectionMjpegPacketHashText,
                SnapshotProjectionFlashbackExportText,
                SnapshotProjectionFlashbackPlaybackText,
                SnapshotProjectionFlashbackRecordingText,
                SnapshotProjectionFlashbackRecordingQueuesText,
                SnapshotProjectionPreviewD3DText,
                SnapshotProjectionPreviewD3DFrameFlowText,
                SnapshotProjectionPreviewD3DFrameLatencyWaitText,
                SnapshotProjectionPreviewD3DFrameStatsText,
                SnapshotProjectionPreviewD3DPipelineLatencyText,
                SnapshotProjectionPreviewRuntimeText,
                SnapshotProjectionProcessResourcesText,
                SnapshotProjectionFlatteningProcessResourcesText,
                SnapshotProjectionRecordingIntegrityText,
                SnapshotProjectionFlatteningRecordingIntegrityText,
                SnapshotProjectionRecordingPipelineText,
                SnapshotProjectionRecordingOutputText,
                SnapshotProjectionFlatteningRecordingOutputText,
                SnapshotProjectionSourceSignalText,
                SnapshotProjectionSourceTelemetryText,
                SnapshotProjectionUserSettingsText,
                SnapshotProjectionHdrPipelineText,
                SnapshotStateText,
                PreviewPacingText,
                OutputFilesText,
                ProcessMetricsText,
                TimelineText,
                TimelineProjectionText,
                SnapshotProjectionPreviewD3DCpuTimingText,
                SnapshotProjectionMjpegTimingText,
            });
    }
}
