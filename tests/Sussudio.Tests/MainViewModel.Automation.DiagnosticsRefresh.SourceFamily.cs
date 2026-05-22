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
            DiagnosticEvaluationFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs"),
            DiagnosticEvaluationFlashbackRecordingConditionsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.RecordingConditions.cs"),
            DiagnosticEvaluationFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs"),
            DiagnosticEvaluationFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs"),
            DiagnosticEvaluationRealtimeRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Recording.cs"),
            DiagnosticEvaluationRealtimeSourceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Source.cs"),
            DiagnosticEvaluationRealtimeMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Mjpeg.cs"),
            DiagnosticEvaluationRealtimePreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs"),
            DiagnosticEvaluationRealtimePreviewSchedulerText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewScheduler.cs"),
            DiagnosticEvaluationRealtimePreviewPresentText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewPresent.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs"),
            DiagnosticEvaluationLanesRealtimePreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Preview.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            SignalAlertsPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SignalAlerts.Preview.cs"),
            FlashbackRecordingAlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs"),
            FlashbackRecordingAlertsDegradationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackRecordingAlerts.Degradation.cs"),
            FlashbackPlaybackAlertsCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackAlerts.Commands.cs"),
            FlashbackPlaybackPerformanceAlertsAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Audio.cs"),
            FlashbackPlaybackPerformanceAlertsCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs"),
            EventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.DiagnosticEvents.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.cs"),
            VerificationAutoText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.Auto.cs"),
            VerificationProfileText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Verification.Profile.cs"),
            LifecycleText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Lifecycle.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Hdr.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSnapshotsSource(),
            SnapshotsCoreText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionCompositionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Composition.cs"),
            SnapshotProjectionFlatteningText = string.Join(
                "\n",
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Set.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs")),
            SnapshotProjectionFlatteningSetText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.Set.cs"),
            SnapshotProjectionFlatteningAutomationSnapshotText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"),
            SnapshotProjectionSnapshotStatusText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs"),
            SnapshotProjectionSnapshotEvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs"),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.cs"),
            SnapshotProjectionAudioSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Audio.Signal.cs"),
            SnapshotProjectionAudioDropsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs"),
            SnapshotProjectionCaptureCommandsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionCaptureFormatRequestedText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Requested.cs"),
            SnapshotProjectionCaptureFormatHdrRequestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.HdrRequest.cs"),
            SnapshotProjectionCaptureFormatActualText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Actual.cs"),
            SnapshotProjectionCaptureFormatNegotiatedText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Negotiated.cs"),
            SnapshotProjectionCaptureFormatReaderObservationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.ReaderObservation.cs"),
            SnapshotProjectionCaptureFormatEncoderText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Encoder.cs"),
            SnapshotProjectionCaptureTransportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs"),
            SnapshotProjectionVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs"),
            SnapshotProjectionMjpegTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs"),
            SnapshotProjectionMjpegPreviewJitterQueueText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Queue.cs"),
            SnapshotProjectionMjpegPreviewJitterTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Timing.cs"),
            SnapshotProjectionMjpegPreviewJitterAdaptiveText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Adaptive.cs"),
            SnapshotProjectionMjpegPreviewJitterEventsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Events.cs"),
            SnapshotProjectionMjpegPacketHashText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubFlashbackPlaybackProjectionSource(),
            SnapshotProjectionFlashbackPlaybackTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Timing.cs"),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs"),
            SnapshotProjectionFlashbackRecordingStartupCacheText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.StartupCache.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs"),
            SnapshotProjectionFlashbackRecordingRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Runtime.cs"),
            SnapshotProjectionFlashbackRecordingBackendText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Backend.cs"),
            SnapshotProjectionFlashbackRecordingEncoderText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Encoder.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameFlow.cs"),
            SnapshotProjectionPreviewD3DFrameLatencyWaitText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameLatencyWait.cs"),
            SnapshotProjectionPreviewD3DFrameStatsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameStats.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs"),
            SnapshotProjectionPreviewRuntimeFrameText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Frame.cs"),
            SnapshotProjectionPreviewRuntimeCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Cadence.cs"),
            SnapshotProjectionPreviewRuntimeSurfaceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Surface.cs"),
            SnapshotProjectionPreviewRuntimeStartupText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Startup.cs"),
            SnapshotProjectionPreviewRuntimeGpuPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.GpuPlayback.cs"),
            SnapshotProjectionPreviewRuntimeColorText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Color.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs"),
            SnapshotProjectionRecordingIntegritySummaryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Summary.cs"),
            SnapshotProjectionRecordingIntegrityVideoText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Video.cs"),
            SnapshotProjectionRecordingIntegrityBackpressureText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Backpressure.cs"),
            SnapshotProjectionRecordingIntegrityAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Audio.cs"),
            SnapshotProjectionRecordingIntegrityAvSyncText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.AvSync.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs"),
            SnapshotProjectionRecordingPipelineEncoderText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Encoder.cs"),
            SnapshotProjectionRecordingPipelineIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Ingest.cs"),
            SnapshotProjectionRecordingPipelineVideoQueueText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.VideoQueue.cs"),
            SnapshotProjectionRecordingPipelineHardwareQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.HardwareQueues.cs"),
            SnapshotProjectionRecordingOutputText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs"),
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
            TimelineProjectionPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.TimelineProjection.Preview.cs"),
            TimelineProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubTimelineProjectionFlashbackPlaybackSource(),
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
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Timing.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Decode.cs"),
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Commands.cs"),
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

    private static string ReadAutomationDiagnosticsHubTimelineProjectionFlashbackPlaybackSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.cs"),
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
        public string SnapshotsCoreText { get; init; } = string.Empty;
        public string SnapshotStateText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string OutputFilesText { get; init; } = string.Empty;
        public string ProcessMetricsText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionText { get; init; } = string.Empty;
        public string TimelineProjectionPreviewText { get; init; } = string.Empty;
        public string TimelineProjectionFlashbackPlaybackText { get; init; } = string.Empty;
    }
}
