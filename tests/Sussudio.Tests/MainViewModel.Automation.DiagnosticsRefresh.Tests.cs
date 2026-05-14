using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEvaluationPolicyText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.EvaluationPolicy.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationFlashbackText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationRealtimeText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationLanesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs")
            .Replace("\r\n", "\n");
        var diagnosticsAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSignalAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackRecordingAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackPlaybackAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackPlaybackCommandAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackPlaybackPerformanceAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEventsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs")
            .Replace("\r\n", "\n");
        var diagnosticsVerificationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.cs")
            .Replace("\r\n", "\n");
        var diagnosticsLifecycleText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var diagnosticsHdrText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionSnapshotStatusText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionSnapshotEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionAvSyncText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionAudioText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionAudioSignalText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionCaptureIngestText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionWasapiAudioText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionCaptureCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionCaptureFormatText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionCaptureTransportText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionCaptureCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionMjpegText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionMjpegTimingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionMjpegPreviewJitterText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionMjpegPacketHashText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackExportText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackPlaybackText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackPlaybackAudioMasterText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackPlaybackDecodeText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackPlaybackCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackRecordingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackRecordingStartupCacheText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionFlashbackRecordingQueuesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewD3DText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewD3DCpuTimingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewD3DFrameFlowText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewD3DFrameLatencyWaitText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewD3DFrameStatsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewRuntimeText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewRuntimeCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionPreviewRuntimeStartupText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionProcessResourcesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionRecordingIntegrityText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionRecordingBackendText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionRecordingPipelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionRecordingOutputText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionSourceSignalText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionSourceTelemetryText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionUserSettingsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionHdrPipelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotStateText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotState.cs")
            .Replace("\r\n", "\n");
        var diagnosticsPreviewPacingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.PreviewPacing.cs")
            .Replace("\r\n", "\n");
        var diagnosticsOutputFilesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.OutputFiles.cs")
            .Replace("\r\n", "\n");
        var diagnosticsProcessMetricsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.ProcessMetrics.cs")
            .Replace("\r\n", "\n");
        var diagnosticsTimelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            .Replace("\r\n", "\n");
        var diagnosticsTimelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = diagnosticsHubText + "\n" + diagnosticsEvaluationText + "\n" + diagnosticsEvaluationPolicyText + "\n" + diagnosticsDiagnosticEvaluationText + "\n" + diagnosticsDiagnosticEvaluationFlashbackText + "\n" + diagnosticsDiagnosticEvaluationRealtimeText + "\n" + diagnosticsDiagnosticEvaluationLanesText + "\n" + diagnosticsAlertsText + "\n" + diagnosticsSignalAlertsText + "\n" + diagnosticsFlashbackAlertsText + "\n" + diagnosticsFlashbackRecordingAlertsText + "\n" + diagnosticsFlashbackPlaybackAlertsText + "\n" + diagnosticsFlashbackPlaybackCommandAlertsText + "\n" + diagnosticsFlashbackPlaybackPerformanceAlertsText + "\n" + diagnosticsEventsText + "\n" + diagnosticsVerificationText + "\n" + diagnosticsLifecycleText + "\n" + diagnosticsHdrText + "\n" + diagnosticsSnapshotsText + "\n" + diagnosticsSnapshotProjectionText + "\n" + diagnosticsSnapshotProjectionSnapshotStatusText + "\n" + diagnosticsSnapshotProjectionSnapshotEvaluationText + "\n" + diagnosticsSnapshotProjectionAvSyncText + "\n" + diagnosticsSnapshotProjectionAudioText + "\n" + diagnosticsSnapshotProjectionAudioSignalText + "\n" + diagnosticsSnapshotProjectionCaptureIngestText + "\n" + diagnosticsSnapshotProjectionWasapiAudioText + "\n" + diagnosticsSnapshotProjectionCaptureCommandsText + "\n" + diagnosticsSnapshotProjectionCaptureFormatText + "\n" + diagnosticsSnapshotProjectionCaptureTransportText + "\n" + diagnosticsSnapshotProjectionCaptureCadenceText + "\n" + diagnosticsSnapshotProjectionMjpegText + "\n" + diagnosticsSnapshotProjectionMjpegPreviewJitterText + "\n" + diagnosticsSnapshotProjectionMjpegPacketHashText + "\n" + diagnosticsSnapshotProjectionFlashbackExportText + "\n" + diagnosticsSnapshotProjectionFlashbackPlaybackText + "\n" + diagnosticsSnapshotProjectionFlashbackPlaybackAudioMasterText + "\n" + diagnosticsSnapshotProjectionFlashbackPlaybackDecodeText + "\n" + diagnosticsSnapshotProjectionFlashbackPlaybackCommandsText + "\n" + diagnosticsSnapshotProjectionFlashbackRecordingText + "\n" + diagnosticsSnapshotProjectionFlashbackRecordingStartupCacheText + "\n" + diagnosticsSnapshotProjectionFlashbackRecordingQueuesText + "\n" + diagnosticsSnapshotProjectionPreviewD3DText + "\n" + diagnosticsSnapshotProjectionPreviewD3DFrameLatencyWaitText + "\n" + diagnosticsSnapshotProjectionPreviewD3DFrameStatsText + "\n" + diagnosticsSnapshotProjectionPreviewRuntimeText + "\n" + diagnosticsSnapshotProjectionProcessResourcesText + "\n" + diagnosticsSnapshotProjectionRecordingIntegrityText + "\n" + diagnosticsSnapshotProjectionRecordingBackendText + "\n" + diagnosticsSnapshotProjectionRecordingPipelineText + "\n" + diagnosticsSnapshotProjectionRecordingOutputText + "\n" + diagnosticsSnapshotProjectionSourceSignalText + "\n" + diagnosticsSnapshotProjectionSourceTelemetryText + "\n" + diagnosticsSnapshotProjectionUserSettingsText + "\n" + diagnosticsSnapshotProjectionHdrPipelineText + "\n" + diagnosticsSnapshotStateText + "\n" + diagnosticsPreviewPacingText + "\n" + diagnosticsOutputFilesText + "\n" + diagnosticsProcessMetricsText + "\n" + diagnosticsTimelineText + "\n" + diagnosticsTimelineProjectionText;
        diagnosticsText += "\n" + diagnosticsSnapshotProjectionPreviewD3DCpuTimingText;
        diagnosticsText += "\n" + diagnosticsSnapshotProjectionPreviewD3DFrameFlowText;
        diagnosticsText += "\n" + diagnosticsSnapshotProjectionPreviewRuntimeCadenceText;
        diagnosticsText += "\n" + diagnosticsSnapshotProjectionPreviewRuntimeStartupText;
        diagnosticsText += "\n" + diagnosticsSnapshotProjectionMjpegTimingText;
        var countersText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(diagnosticsEvaluationPolicyText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertDoesNotContain(diagnosticsEvaluationText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertContains(diagnosticsEvaluationText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "var lanes = BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationFlashbackText, "\"flashback_storage\"");
        AssertContains(diagnosticsDiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationRealtimeText, "\"source_capture\"");
        AssertDoesNotContain(diagnosticsDiagnosticEvaluationText, "\"flashback_storage\"");
        AssertDoesNotContain(diagnosticsDiagnosticEvaluationText, "\"source_capture\"");
        AssertDoesNotContain(diagnosticsDiagnosticEvaluationText, "var sourceTarget =");
        AssertContains(diagnosticsDiagnosticEvaluationLanesText, "private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnosticsDiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationLanes(");
        AssertDoesNotContain(diagnosticsEvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertDoesNotContain(diagnosticsHubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnosticsHubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnosticsAlertsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertDoesNotContain(diagnosticsAlertsText, "private void AddEventThrottled(");
        AssertDoesNotContain(diagnosticsAlertsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnosticsEventsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsEventsText, "private void AddEventThrottled(");
        AssertContains(diagnosticsEventsText, "private void SetAlertState(");
        AssertContains(diagnosticsEventsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnosticsAlertsText, "UpdateSignalAlerts(");
        AssertContains(diagnosticsSignalAlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnosticsSignalAlertsText, "\"preview-blank\"");
        AssertDoesNotContain(diagnosticsAlertsText, "\"preview-blank\"");
        AssertContains(diagnosticsAlertsText, "UpdateFlashbackAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnosticsFlashbackAlertsText, "private void UpdateFlashbackAlerts(");
        AssertContains(diagnosticsFlashbackAlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnosticsFlashbackAlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnosticsFlashbackRecordingAlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnosticsFlashbackRecordingAlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnosticsFlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnosticsFlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnosticsFlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnosticsFlashbackPlaybackCommandAlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnosticsFlashbackPlaybackCommandAlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnosticsFlashbackPlaybackPerformanceAlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnosticsFlashbackPlaybackPerformanceAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnosticsFlashbackAlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnosticsFlashbackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnosticsFlashbackPlaybackAlertsText, "\"flashback-playback-command-stalled\"");
        AssertDoesNotContain(diagnosticsFlashbackPlaybackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnosticsAlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnosticsHubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnosticsVerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnosticsVerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnosticsVerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnosticsVerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnosticsVerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertDoesNotContain(diagnosticsHubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnosticsPreviewPacingText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnosticsSnapshotsText, "ClassifyPreviewPacing(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new PreviewPacingClassificationInput");
        AssertContains(diagnosticsLifecycleText, "public void Start()");
        AssertContains(diagnosticsLifecycleText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "public void Start()");
        AssertContains(diagnosticsHdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnosticsHdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnosticsHdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnosticsHdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertDoesNotContain(diagnosticsHubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnosticsSnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnosticsSnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnosticsSnapshotProjectionText, "private AutomationSnapshot BuildAutomationSnapshot(");
        AssertContains(diagnosticsSnapshotProjectionText, "new AutomationSnapshot");
        AssertContains(diagnosticsSnapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionSnapshotStatusText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(diagnosticsSnapshotProjectionSnapshotStatusText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(diagnosticsSnapshotProjectionSnapshotStatusText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "StatusText = viewModelSnapshot.StatusText,");
        AssertContains(diagnosticsSnapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "PerformanceScore = performance.Score,");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(diagnosticsSnapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(diagnosticsSnapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "Cadence = cadence,");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeCadenceText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeCadenceText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeCadenceText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "Startup = startup,");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeStartupText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeStartupText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeStartupText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeStartupText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnosticsSnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
        AssertContains(diagnosticsSnapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "CpuTiming = cpuTiming,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DCpuTimingText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DCpuTimingText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DCpuTimingText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "FrameFlow = frameFlow");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameFlowText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameFlowText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameFlowText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameLatencyWaitText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameLatencyWaitText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DText, "FrameStats = frameStats,");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameStatsText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(diagnosticsSnapshotProjectionPreviewD3DFrameStatsText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(diagnosticsSnapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackExportText, "private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionFlashbackExportText, "Active = health.FlashbackExportActive,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertContains(diagnosticsSnapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "StartupCache = startupCache,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingStartupCacheText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingStartupCacheText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "Queues = queues,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingQueuesText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingQueuesText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingQueuesText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(diagnosticsSnapshotProjectionText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackRecordingText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(diagnosticsSnapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "AudioMaster = audioMaster,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "Commands = commands");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackAudioMasterText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackAudioMasterText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackDecodeText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackDecodeText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackDecodeText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackCommandsText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionFlashbackPlaybackCommandsText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(diagnosticsSnapshotProjectionAudioText, "private static AudioAndIngestProjection BuildAudioAndIngestProjection(");
        AssertContains(diagnosticsSnapshotProjectionAudioText, "var audioSignalProjection = BuildAudioSignalProjection(viewModelSnapshot, audioSignal);");
        AssertContains(diagnosticsSnapshotProjectionAudioText, "var captureIngest = BuildCaptureIngestProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionAudioText, "var wasapiAudio = BuildWasapiAudioProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionAudioSignalText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(diagnosticsSnapshotProjectionAudioSignalText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(diagnosticsSnapshotProjectionCaptureIngestText, "private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionCaptureIngestText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertContains(diagnosticsSnapshotProjectionWasapiAudioText, "private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionWasapiAudioText, "CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertContains(diagnosticsSnapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(diagnosticsSnapshotProjectionCaptureCommandsText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(diagnosticsSnapshotProjectionCaptureCommandsText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(diagnosticsSnapshotProjectionCaptureCommandsText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");
        AssertContains(diagnosticsSnapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(diagnosticsSnapshotProjectionUserSettingsText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(diagnosticsSnapshotProjectionUserSettingsText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(diagnosticsSnapshotProjectionUserSettingsText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(diagnosticsSnapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionHdrPipelineText, "private static HdrPipelineProjection BuildHdrPipelineProjection(");
        AssertContains(diagnosticsSnapshotProjectionHdrPipelineText, "HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),");
        AssertContains(diagnosticsSnapshotProjectionHdrPipelineText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertContains(diagnosticsSnapshotProjectionCaptureFormatText, "private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionCaptureFormatText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(diagnosticsSnapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionCaptureTransportText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionCaptureTransportText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertContains(diagnosticsSnapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionCaptureCadenceText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionCaptureCadenceText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(diagnosticsSnapshotProjectionCaptureCadenceText, "VisualCenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");
        AssertContains(diagnosticsSnapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "var timing = BuildMjpegTimingProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "Timing = timing,");
        AssertContains(diagnosticsSnapshotProjectionMjpegTimingText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionMjpegTimingText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(diagnosticsSnapshotProjectionMjpegTimingText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "var previewJitter = BuildMjpegPreviewJitterProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "var packetHash = BuildMjpegPacketHashProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "PreviewJitter = previewJitter,");
        AssertContains(diagnosticsSnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionMjpegPreviewJitterText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(diagnosticsSnapshotProjectionMjpegText, "PacketHash = packetHash,");
        AssertContains(diagnosticsSnapshotProjectionMjpegPacketHashText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionMjpegPacketHashText, "Pattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(diagnosticsSnapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionRecordingIntegrityText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(diagnosticsSnapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionRecordingBackendText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionRecordingBackendText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");
        AssertContains(diagnosticsSnapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(diagnosticsSnapshotProjectionRecordingPipelineText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsSnapshotProjectionRecordingPipelineText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertContains(diagnosticsSnapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(diagnosticsSnapshotProjectionRecordingOutputText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(diagnosticsSnapshotProjectionRecordingOutputText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(diagnosticsSnapshotProjectionRecordingOutputText, "LastOutputExists = lastOutput.Exists,");
        AssertContains(diagnosticsSnapshotProjectionRecordingOutputText, "LastVerification = lastVerification");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "LastOutputExists = lastOutput.Exists,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "LastVerification = lastVerification,");
        AssertContains(diagnosticsSnapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionSourceSignalText, "private static SourceSignalProjection BuildSourceSignalProjection(");
        AssertContains(diagnosticsSnapshotProjectionSourceSignalText, "FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),");
        AssertContains(diagnosticsSnapshotProjectionSourceSignalText, "RawTimingHex = captureRuntime.SourceRawTimingHex");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");
        AssertContains(diagnosticsSnapshotProjectionSourceTelemetryText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(diagnosticsSnapshotProjectionSourceTelemetryText, "PreferKnownTelemetryValue(");
        AssertContains(diagnosticsSnapshotProjectionSourceTelemetryText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertContains(diagnosticsSnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnosticsSnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnosticsSnapshotStateText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnosticsSnapshotStateText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnosticsSnapshotStateText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnosticsSnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnosticsSnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnosticsOutputFilesText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnosticsOutputFilesText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnosticsProcessMetricsText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnosticsProcessMetricsText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnosticsProcessMetricsText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnosticsSnapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(diagnosticsSnapshotProjectionProcessResourcesText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(diagnosticsSnapshotProjectionProcessResourcesText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(diagnosticsSnapshotProjectionProcessResourcesText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");
        AssertContains(diagnosticsSnapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(diagnosticsSnapshotProjectionAvSyncText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnosticsSnapshotProjectionAvSyncText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(diagnosticsSnapshotProjectionAvSyncText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(diagnosticsSnapshotProjectionText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");
        AssertContains(diagnosticsTimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnosticsTimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsTimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertDoesNotContain(diagnosticsTimelineText, "new PerformanceTimelineEntry\n        {");
        AssertContains(diagnosticsTimelineProjectionText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsTimelineProjectionText, "FlashbackPlaybackCommandsEnqueued = snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertDoesNotContain(diagnosticsHubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnosticsSnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnosticsSnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "_lastVerification = null;");
        AssertContains(diagnosticsSnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "Automatic recording verification started.");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new FileInfo(lastOutputPath).Length");
        AssertDoesNotContain(diagnosticsSnapshotsText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnosticsHubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnosticsText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnosticsText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnosticsText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n            {\n                var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n                var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
        AssertContains(diagnosticsText, "\"flashback-export-stalled\"");
        AssertContains(diagnosticsText, "DiagnosticsCategory.Flashback");
        AssertContains(diagnosticsText, "health.FlashbackExportActive");
        AssertContains(diagnosticsText, "Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnosticsText, "Math.Max(0, health.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnosticsText, "elapsedMs={health.FlashbackExportElapsedMs}");
        AssertContains(diagnosticsText, "throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##}");
        AssertContains(diagnosticsText, "kind={exportFailureKind}");
        AssertContains(diagnosticsText, "private const int FlashbackExportStallThresholdMs = 30000;");
        AssertContains(diagnosticsText, "exportLastProgressAgeMs >= FlashbackExportStallThresholdMs");
        AssertContains(diagnosticsText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnosticsText, "$\"{lanes.Export} progressAgeMs={exportLastProgressAgeMs}\"");
        AssertContains(diagnosticsText, "private long _lastFlashbackExportCompletionEventId;");
        AssertContains(diagnosticsText, "ObserveFlashbackExportCompletion(snapshot);");
        AssertContains(diagnosticsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsText, "snapshot.FlashbackExportCompletedUtcUnixMs <= 0");
        AssertContains(diagnosticsText, "Interlocked.CompareExchange(\n                ref _lastFlashbackExportCompletionEventId");
        AssertContains(diagnosticsText, "status.Equals(\"Succeeded\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "status.Equals(\"Cancelled\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "snapshot.FlashbackExportFailureKind");
        AssertContains(diagnosticsText, "FlashbackBackendSettingsStale = flashbackRecording.BackendSettingsStale,");
        AssertContains(diagnosticsText, "BackendSettingsStale = health.FlashbackBackendSettingsStale,");
        AssertContains(diagnosticsText, "backendStale={health.FlashbackBackendSettingsStale}");
        AssertContains(diagnosticsText, "kind={failureKind}");
        AssertContains(diagnosticsText, "Flashback export completed: status={status}");
        AssertContains(diagnosticsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackCommandStallThresholdMs = 1000;");
        AssertContains(diagnosticsText, "\"flashback-playback-command-failed\"");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackCommandFailureRecentMs = 30000;");
        AssertContains(diagnosticsText, "playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs");
        AssertContains(diagnosticsText, "Flashback playback command failed recently:");
        AssertContains(diagnosticsText, "\"Flashback playback command failed recently.\"");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackSlowFpsRatio = 0.75;");
        AssertContains(diagnosticsText, "private const double CaptureOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const double PreviewOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackOnePercentLowMinimumFrames = 1200;");
        AssertContains(diagnosticsText, "private const long FlashbackTempDriveLowFreeBytes = 5L * 1024L * 1024L * 1024L;");
        AssertContains(diagnosticsText, "private const long FlashbackRecordingBackpressureWarningMs = 100;");
        AssertContains(diagnosticsText, "private const double FlashbackRecordingQueueDepthWarningRatio = 0.75;");
        AssertContains(diagnosticsText, "private const double FlashbackAudioQueueDepthWarningRatio = 0.90;");
        AssertContains(diagnosticsText, "private const long FlashbackRecordingQueueAgeWarningMs = 500;");
        AssertContains(diagnosticsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackStartupCacheOverBudget");
        AssertContains(diagnosticsText, "snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes");
        AssertContains(diagnosticsText, "\"flashback_storage\"");
        AssertContains(diagnosticsText, "\"Flashback temp storage is under pressure.\"");
        AssertContains(diagnosticsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackEncodingFailed");
        AssertContains(diagnosticsText, "Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? \"Unknown\"}");
        AssertContains(diagnosticsText, "\"flashback-recording-degraded\"");
        AssertContains(countersText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps)");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped)");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents)");
        AssertContains(diagnosticsText, "var recentFlashbackRecording = UpdateFlashbackRecordingRecentCounters(health, nowTick);");
        AssertContains(diagnosticsText, "UpdateAlerts(snapshot, recentFlashbackRecording);");
        AssertContains(diagnosticsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnosticsText, "var flashbackRecordingQueueBacklog =");
        AssertContains(diagnosticsText, "var flashbackAudioQueueBacklog =");
        AssertContains(diagnosticsText, "IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnosticsText, "IsFlashbackAudioQueueBackedUp(");
        AssertContains(diagnosticsText, "flashbackRecordingRecentForceRotateGap");
        AssertContains(diagnosticsText, "IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason)");
        AssertContains(diagnosticsText, "flashbackRecordingRecent.SequenceGaps > 0");
        AssertContains(diagnosticsText, "(flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap)");
        AssertContains(diagnosticsText, "flashbackRecordingRecent.GpuFramesDropped > 0");
        AssertContains(diagnosticsText, "flashbackRecordingRecentBackpressure");
        AssertContains(diagnosticsText, "flashbackRecordingQueueBacklog");
        AssertContains(diagnosticsText, "flashbackAudioQueueBacklog");
        AssertContains(diagnosticsText, "snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnosticsText, "Flashback recording path degraded:");
        AssertContains(diagnosticsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnosticsText, "Flashback export rotation skipped live-edge frames:");
        AssertContains(diagnosticsText, "forceRotate={snapshot.FlashbackForceRotateActive}");
        AssertContains(diagnosticsText, "requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining}");
        AssertContains(diagnosticsText, "FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress");
        AssertContains(diagnosticsText, "FatalCleanupInProgress = health.FatalCleanupInProgress");
        AssertContains(diagnosticsText, "FlashbackCleanupInProgress = flashbackRecording.CleanupInProgress");
        AssertContains(diagnosticsText, "CleanupInProgress = health.FlashbackCleanupInProgress");
        AssertContains(diagnosticsText, "recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents}");
        AssertContains(diagnosticsText, "\"flashback-playback-slow\"");
        AssertContains(diagnosticsText, "\"flashback-playback-target-below-selection\"");
        AssertContains(diagnosticsText, "\"flashback-playback-present-capped\"");
        AssertContains(diagnosticsText, "\"flashback-playback-frametime-degraded\"");
        AssertContains(diagnosticsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnosticsText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnosticsText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnosticsText, "Flashback playback frame submission failed");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackPendingCommands > 0");
        AssertContains(diagnosticsText, "FlashbackPlaybackCommandQueueCapacity");
        AssertContains(diagnosticsText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps");
        AssertContains(diagnosticsText, "TargetFps = health.FlashbackPlaybackTargetFps");
        AssertContains(diagnosticsText, "FlashbackPlaybackTargetFps = snapshot.FlashbackPlaybackTargetFps");
        AssertContains(diagnosticsText, "FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlayback.PtsCadenceMismatchCount");
        AssertContains(diagnosticsText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount");
        AssertContains(diagnosticsText, "ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount}");
        AssertContains(diagnosticsText, "private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)");
        AssertContains(diagnosticsText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            snapshot.FlashbackPlaybackTargetFps,\n            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "IsFlashbackPlaybackFrametimeDegraded(\n                snapshot.FlashbackPlaybackState");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackState,\n                playbackTargetFps,\n                snapshot.FlashbackPlaybackFrameCount");
        AssertContains(diagnosticsText, "IsCaptureOnePercentLowDegraded(\n                snapshot.ExpectedCaptureFrameRate");
        AssertContains(diagnosticsText, "IsPreviewOnePercentLowDegraded(\n                snapshot.PreviewCadenceExpectedIntervalMs");
        AssertContains(diagnosticsText, "\"Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.\"");
        AssertContains(diagnosticsText, "$\"{sourceLane} | {visualLane}\"");
        AssertContains(diagnosticsText, "captureCadenceExpectedFrameRate: health.ExpectedFrameRate");
        AssertContains(diagnosticsText, "captureCadenceOnePercentLowFps: health.CaptureCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "previewCadenceExpectedIntervalMs: previewRuntime.DisplayCadenceExpectedIntervalMs");
        AssertContains(diagnosticsText, "previewCadenceOnePercentLowFps: previewRuntime.DisplayCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "reasons.Add($\"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnosticsText, "reasons.Add($\"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnosticsText, "private static bool IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnosticsText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio)");
        AssertContains(diagnosticsText, "oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs");
        AssertContains(diagnosticsText, "private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)");
        AssertContains(diagnosticsText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio)");
        AssertContains(diagnosticsText, "private static bool IsFlashbackForceRotateRejectReason(string? reason)");
        AssertContains(diagnosticsText, "string.Equals(reason, \"force_rotate_queue_guard\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackOnePercentLowFps");
        AssertContains(diagnosticsText, "frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticsText, "cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio");
        AssertContains(diagnosticsText, "snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth");
        AssertContains(diagnosticsText, "Flashback playback is using wall-clock pacing instead of audio-master pacing");
        AssertContains(diagnosticsText, "Flashback playback audio queue is backing up");
        AssertContains(diagnosticsText, "Flashback playback is below target rate");
        AssertContains(diagnosticsText, "Flashback playback target is below the selected capture rate");
        AssertContains(diagnosticsText, "Flashback playback is targeting HFR but D3D present cadence is below target");
        AssertContains(diagnosticsText, "Flashback playback frametime degraded");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0");
        AssertContains(diagnosticsText, "Flashback playback command queue has not drained");
        AssertContains(diagnosticsText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnosticsText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnosticsText, "\"flashback_playback\"");
        AssertContains(diagnosticsText, "\"Flashback playback command queue is stalled.\"");
        AssertContains(diagnosticsText, "\"Flashback playback is below target rate.\"");
        AssertContains(diagnosticsText, "\"Flashback playback frametime is below target.\"");
        AssertContains(diagnosticsText, "\"Flashback playback frame submission failed.\"");
        AssertContains(diagnosticsText, "flashback recording active={health.FlashbackActive}");
        AssertContains(diagnosticsText, "fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}");
        AssertContains(diagnosticsText, "var recordingIntegrityIncomplete =");
        AssertContains(diagnosticsText, "string.Equals(captureRuntime.RecordingIntegrityStatus, \"Incomplete\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "(recordingIntegrityIncomplete && !isRecording)");
        AssertContains(diagnosticsText, "var flashbackRecordingDegraded =");
        AssertContains(diagnosticsText, "recentFlashbackRecording.EncoderDroppedFrames > 0");
        AssertContains(diagnosticsText, "recentFlashbackRecording.BackpressureEvents > 0");
        AssertContains(diagnosticsText, "health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnosticsText, "var flashbackBackendSettingsUnexpectedlyStale =");
        AssertContains(diagnosticsText, "health.FlashbackBackendSettingsStale &&\n            !isRecording");
        AssertContains(diagnosticsText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnosticsText, "health.FlashbackVideoQueueDepth,\n                 health.FlashbackVideoQueueCapacity,\n                 health.FlashbackVideoQueueOldestFrameAgeMs");
        AssertContains(diagnosticsText, "forceRotate={health.FlashbackForceRotateActive}");
        AssertContains(diagnosticsText, "queueRejects={health.FlashbackVideoQueueRejectedFrames}");
        AssertContains(diagnosticsText, "audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity}");
        AssertContains(diagnosticsText, "lastReject={health.FlashbackVideoQueueLastRejectReason ?? \"None\"}");
        AssertContains(diagnosticsText, "flashbackExportRotationGap");
        AssertContains(diagnosticsText, "\"Flashback export rotation skipped live-edge frames.\"");
        AssertContains(diagnosticsText, "requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining}");
        AssertContains(diagnosticsText, "\"flashback_recording\"");
        AssertContains(diagnosticsText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnosticsText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnosticsText, "queuedAge={playbackCommandQueueAgeMs}ms");
        AssertContains(diagnosticsText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnosticsText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            health.FlashbackPlaybackTargetFps,\n            health.ExpectedFrameRate);");
        AssertContains(diagnosticsText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnosticsText, "playback perf state={health.FlashbackPlaybackState}");
        AssertContains(diagnosticsText, "fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##}");
        AssertContains(diagnosticsText, "target={health.FlashbackPlaybackTargetFps:0.##}");
        AssertContains(diagnosticsText, "encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##}");
        AssertContains(diagnosticsText, "private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsText, "ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms");
        AssertContains(diagnosticsText, "1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackAudioMasterFallbackWarningRatio = 0.50;");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackAudioQueueBacklogWarningDepth = 24;");
        AssertContains(diagnosticsText, "decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms");
        AssertContains(diagnosticsText, "decodePhase={health.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnosticsText, "decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms");
        AssertContains(diagnosticsText, "decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms");
        AssertContains(diagnosticsText, "decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnosticsText, "audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnosticsText, "audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnosticsText, "health.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnosticsText, "\"flashback_export\"");
        AssertContains(diagnosticsText, "var flashbackForceRotateRejectWithoutDamage =");
        AssertContains(diagnosticsText, "!flashbackForceRotateRejectWithoutDamage &&\n              recentFlashbackRecording.SequenceGaps > 0");
        AssertContains(diagnosticsText, "health.FlashbackExportActive ||\n             health.FlashbackForceRotateActive ||\n             health.FlashbackForceRotateRequested ||\n             health.FlashbackForceRotateDraining");
        AssertContains(diagnosticsText, "UpdatePreviewJitterRecentCounters(health, nowTick)");
        AssertContains(diagnosticsText, "UpdateD3DRendererRecentCounters(previewRuntime, nowTick)");
        AssertContains(countersText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted)");
        AssertContains(diagnosticsText, "recentSubmitted={recentRendererSubmitted} recentDropped={recentRenderer.Dropped}");
        AssertContains(diagnosticsText, "var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)");
        AssertContains(diagnosticsText, "clearedDrops={health.MjpegPreviewJitterClearedDropCount}");
        AssertContains(diagnosticsText, "resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}");
        AssertContains(diagnosticsText, "UpdateD3DFrameStatsRecentCounters(previewRuntime, nowTick)");
        AssertContains(diagnosticsText, "recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}");
        AssertContains(diagnosticsText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnosticsText, "\"Capture cadence 1% low is below target:");
        AssertContains(diagnosticsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnosticsText, "previewOnePercentLowDegraded && !visualCadenceHealthy");
        AssertContains(diagnosticsText, "\"Preview/display 1% low is below target:");
        AssertContains(diagnosticsText, "FormatVisualCadenceAlertDetail(snapshot)");
        AssertContains(diagnosticsText, "visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps");
        AssertContains(diagnosticsText, "var previewSubmitFailed = string.Equals(");
        AssertContains(diagnosticsText, "health.MjpegPreviewJitterLastDropReason,\n            \"submit-failed\"");
        AssertContains(diagnosticsText, "if (previewSubmitFailed ||\n            (recentPreviewDeadlineDrops > 0 && !visualCadenceHealthy) ||\n            recentPreviewUnderflows > 3)");
        AssertContains(diagnosticsText, "\"Preview scheduler failed to submit frames.\"");
        AssertContains(diagnosticsText, "var presentCadenceOverBudget =\n            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&\n            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;");
        AssertContains(diagnosticsText, "var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);");
        AssertContains(diagnosticsText, "latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms");
        AssertContains(diagnosticsText, "pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}");
        AssertContains(diagnosticsText, "inputUpload={frame.InputUploadCpuMs:0.##}ms");
        AssertContains(diagnosticsText, "renderSubmit={frame.RenderSubmitCpuMs:0.##}ms");
        AssertContains(diagnosticsText, "var unsyncedPresentCallSlow =\n            previewRuntime.D3DPresentSyncInterval == 0 &&\n            previewRuntime.D3DPresentCallP95Ms > 4.0;");
        AssertContains(diagnosticsText, "if (presentCadenceOverBudget ||\n            unsyncedPresentCallSlow)");
        AssertContains(diagnosticsText, "if (captureOnePercentLowDegraded)");
        AssertContains(diagnosticsText, "\"Source/capture 1% low is below target.\"");
        AssertContains(diagnosticsText, "if (previewOnePercentLowDegraded)");
        AssertContains(diagnosticsText, "var visualCadenceHealthy =\n            IsVisualCadenceHealthy(");
        AssertContains(diagnosticsText, "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.");
        AssertContains(diagnosticsText, "if (visualCadenceHealthy)\n            {\n                return new DiagnosticEvaluation(\n                    \"Healthy\",");
        AssertContains(diagnosticsText, "private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsText, "health.MjpegPacketHashDuplicateFramePercent < 20.0");
        AssertContains(diagnosticsText, "health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);");
        AssertContains(diagnosticsText, "\"source_signal\"");
        AssertContains(diagnosticsText, "\"Captured HFR MJPEG cadence contains repeated source frames.\"");
        AssertContains(diagnosticsText, "$\"{mjpegDuplicateLane} | {visualLane} | {sourceSignalLane}\"");
        AssertContains(diagnosticsText, "!visualCadenceHealthy &&\n            IsPreviewOnePercentLowDegraded(");
        AssertContains(diagnosticsText, "private static bool IsVisualCadenceHealthy(");
        AssertContains(diagnosticsText, "changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio");
        AssertContains(diagnosticsText, "repeatFramePercent <= 1.0");
        AssertContains(diagnosticsText, "longestRepeatRun <= 1");
        AssertContains(diagnosticsText, "\"Present/display 1% low is below target.\"");
        AssertContains(countersText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(diagnosticsText, "var recentMjpeg = UpdateMjpegRecentCounters(health, nowTick);");
        AssertContains(diagnosticsText, "recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}");
        AssertContains(diagnosticsText, "recentMjpeg.TotalDropped > 0");
        AssertContains(diagnosticsText, "if (recentRendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&\n            recentRendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)");
        AssertDoesNotContain(diagnosticsText, "rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||\n            previewRuntime.DisplayCadenceSlowFramePercent > 1.0");

        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
                .Replace("\r\n", "\n");
        var flashbackBackendText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");
        AssertContains(captureServiceText, "private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);");
        AssertContains(captureServiceText, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);");
        AssertContains(captureServiceText, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(captureServiceText, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackExporter ??= new FlashbackExporter();\n            }");
        AssertOccursBefore(captureServiceText, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(captureServiceText, "var sessionLockHeld = false;");
        AssertContains(captureServiceText, "sessionLockHeld = true;");
        AssertContains(captureServiceText, "if (sessionLockHeld)");
        AssertContains(captureServiceText, "var exportOperationLockHeld = false;");
        AssertContains(captureServiceText, "exportOperationLockHeld = true;");
        AssertContains(captureServiceText, "catch (OperationCanceledException) when (ct.IsCancellationRequested)");
        AssertContains(captureServiceText, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(captureServiceText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(captureServiceText, "backendLeaseHeld = false;\n        ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        var exportRangeMethod = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackRangeAsync",
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        var exportLastNMethod = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync",
            "private FinalizeResult FailFlashbackExport");
        AssertContains(exportRangeMethod, "FlashbackExporter? flashbackExporter;");
        AssertContains(exportRangeMethod, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(exportRangeMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(exportRangeMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(exportRangeMethod, "snapshotExporter: flashbackExporter,");
        AssertContains(exportLastNMethod, "FlashbackExporter? flashbackExporter;");
        AssertContains(exportLastNMethod, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(exportLastNMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(exportLastNMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(exportLastNMethod, "snapshotExporter: flashbackExporter,");
        AssertContains(flashbackBackendText, "outerPauseApplied = bufferManager != null;");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback export cancelled.\", inPoint, outPoint);");
        AssertContains(captureServiceText, "var exportId = 0L;");
        AssertContains(captureServiceText, "var evictionPaused = false;");
        AssertContains(captureServiceText, "exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);");
        AssertContains(captureServiceText, "var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);");
        AssertContains(captureServiceText, "segmentPaths = forceRotateResult.SegmentPaths;");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        var forceRotateFailedBlock = ExtractTextBetween(
            captureServiceText,
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)",
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        AssertContains(forceRotateFailedBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(forceRotateFailedBlock, "preserved_segments={preservedArtifacts.Count}");
        AssertContains(forceRotateFailedBlock, "return result;");
        var forceRotateFallbackBlock = ExtractTextBetween(
            captureServiceText,
            "if (segmentPaths.Count == 0)",
            "// Fallback: single-file export if no segments available");
        AssertContains(forceRotateFallbackBlock, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertContains(forceRotateFallbackBlock, "RecordFlashbackExportForceRotateFallback(exportId, segmentPaths.Count, inPoint, outPoint);");
        AssertDoesNotContain(forceRotateFallbackBlock, "force_rotate_failed");
        AssertDoesNotContain(forceRotateFallbackBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(captureServiceText, "captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary)");
        AssertContains(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);");
        AssertOccursBefore(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);", "var exportResult = await exportRecordingAsync(");
        AssertContains(captureServiceText, "counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(captureServiceText, "audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline");
        AssertContains(captureServiceText, "evictionPaused = true;");
        AssertContains(captureServiceText, "if (exportId != 0)");
        AssertContains(captureServiceText, "if (evictionPaused)");
        AssertContains(captureServiceText, "ResumeFlashbackEvictionBestEffort(bufferManager, \"flashback_export\");");
        AssertContains(flashbackBackendText, "resumeEvictionBestEffort(bufferManager, \"flashback_recording_finalize\");");
        AssertContains(captureServiceText, "RecordLastFlashbackExportResult(exportId, failure);");
        AssertContains(captureServiceText, "private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, exportId);");
        AssertContains(captureServiceText, "private FinalizeResult FailFlashbackExport(\n        string outputPath,\n        string statusMessage,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "Logger.Log($\"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'\");");
        AssertContains(captureServiceText, "_lastExportResult = result;");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "private void RecordRejectedFlashbackExportDiagnostics(\n        string outputPath,\n        FinalizeResult result,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "if (_flashbackExportActive)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, 0);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED");
        AssertContains(captureServiceText, "active_id={_flashbackExportId}");
        AssertContains(captureServiceText, "if (_flashbackExportId != exportId || !_flashbackExportActive)");
        AssertContains(captureServiceText, "var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested\n                ? \"Flashback export cancelled.\"\n                : ex.Message;");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name}");
        AssertContains(captureServiceText, "var failure = FinalizeResult.Failure(outputPath, statusMessage);");
        AssertContains(captureServiceText, "CompleteFlashbackExportDiagnostics(exportId, failure);\n            }\n            else\n            {\n                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);\n            }\n            return failure;");
        AssertContains(captureServiceText, "_flashbackExportStartedUtcUnixMs = now;");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = now;");
        AssertContains(captureServiceText, "var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "_flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "ClassifyFlashbackExportFailureKind(result.StatusMessage)");
        AssertContains(captureServiceText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(captureServiceText, "return \"UnavailableDuringRecording\";");
        AssertContains(captureServiceText, "return \"BufferInactive\";");
        AssertContains(captureServiceText, "ContainsFlashbackExportFailureText(statusMessage, \"buffer has no active file\")");
        AssertContains(captureServiceText, "return \"InvalidOutputPath\";");
        AssertContains(captureServiceText, "return \"NoMediaWritten\";");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(captureServiceText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(captureServiceText, "var validStart = manager.ValidStartPts;");
        AssertContains(captureServiceText, "var bufferedDuration = manager.BufferedDuration;");
        AssertContains(captureServiceText, "var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);");
        AssertContains(captureServiceText, "var bufferOutPoint = outPoint.HasValue\n                        ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)\n                        : TimeSpan.MaxValue;");
        AssertContains(captureServiceText, "var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);");
        AssertContains(captureServiceText, "var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);");
        AssertContains(captureServiceText, ".Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))");
        AssertContains(captureServiceText, "var pathKey = TryGetFullPath(path);");
        AssertContains(captureServiceText, "segmentInfo.TryGetValue(pathKey, out var info)");
        AssertContains(captureServiceText, "private static string? TryGetFullPath(string? path)");
        AssertContains(captureServiceText, "FLASHBACK_PATH_NORMALIZE_WARN");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "if (ct.IsCancellationRequested)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export cancelled.\");\n        }\n\n        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export duration must be finite, greater than zero, and within TimeSpan range.\");\n        }");
        AssertContains(dispatcherText, "if (!double.IsFinite(seconds) ||\n                    seconds <= 0 ||\n                    seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(dispatcherText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(captureServiceText, "? \"Cancelled\"");
        AssertContains(captureServiceText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(captureServiceText, "if (exportOperationLockHeld)");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "DisposeCoordinationLocksBestEffort();");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_sessionTransitionLock, \"session_transition\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(captureServiceText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(captureServiceText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(captureServiceText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, \"flashback_export_snapshot_session\");");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_preview_backend_dispose\");");
        AssertDoesNotContain(captureServiceText, "_flashbackBackendLeaseLock.Release();");
        AssertDoesNotContain(captureServiceText, "_flashbackExportOperationLock.Release();");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK");
        AssertContains(captureServiceText, "Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths)");
        AssertContains(captureServiceText, "var startPts = FromSegmentMilliseconds(info.StartPtsMs);");
        AssertContains(captureServiceText, "var endPts = FromSegmentMilliseconds(info.EndPtsMs);");
        AssertContains(captureServiceText, "if (endPts < startPts)\n                {\n                    endPts = startPts;\n                }");
        AssertContains(captureServiceText, "StartPts = startPts,\n                    EndPts = endPts");
        AssertContains(captureServiceText, "private static TimeSpan FromSegmentMilliseconds(long milliseconds)");
        AssertContains(captureServiceText, "return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds\n            ? TimeSpan.MaxValue\n            : TimeSpan.FromMilliseconds(milliseconds);");
        AssertContains(captureServiceText, "private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)");
        AssertContains(captureServiceText, "if (bufferedDuration <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }");
        AssertContains(captureServiceText, "private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)");
        AssertContains(captureServiceText, "if (position < TimeSpan.Zero)\n        {\n            position = TimeSpan.Zero;\n        }");
        AssertContains(captureServiceText, "if (offset <= TimeSpan.Zero)\n        {\n            return position;\n        }");
        AssertContains(captureServiceText, "return position > TimeSpan.MaxValue - offset\n            ? TimeSpan.MaxValue\n            : position + offset;");
        AssertContains(captureServiceText, "var rawTotalSegments = progress.TotalSegments;");
        AssertContains(captureServiceText, "var totalSegments = Math.Max(0, rawTotalSegments);");
        AssertContains(captureServiceText, "if (totalSegments > 0 && segmentsProcessed > totalSegments)");
        AssertContains(captureServiceText, "Math.Clamp(rawPercent, 0.0, 100.0)");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_PROGRESS_NORMALIZED");
        AssertContains(captureServiceText, "raw_segments={rawSegmentsProcessed}/{rawTotalSegments}");
        AssertContains(captureServiceText, "raw_percent={rawPercent:0.###} percent={percent:0.###}");
        AssertContains(captureServiceText, "try\n            {\n                innerProgress?.Report(progress);\n            }\n            catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");

        var flashbackExporterText = ReadFlashbackExporterSource();
        AssertContains(flashbackExporterText, "if (request.Segments is { Count: > 0 })");
        AssertContains(flashbackExporterText, "var useSegmentTimeline = segment.StartPts.HasValue");
        AssertContains(flashbackExporterText, "var comparePtsUs = useSegmentTimeline");
        AssertContains(flashbackExporterText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(flashbackExporterText, "FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR");

        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs")
            .Replace("\r\n", "\n");
        var sourceReaderDiagnosticsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs")
            .Replace("\r\n", "\n");
        var sourceReaderDxgiBuffersText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs")
            .Replace("\r\n", "\n");
        var sourceReaderFrameLayoutText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs")
            .Replace("\r\n", "\n");
        var sourceReaderLifecycleText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var sourceReaderInitializationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs")
            .Replace("\r\n", "\n");
        var sourceReaderReadLoopText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ReadLoop.cs")
            .Replace("\r\n", "\n");
        var sourceReaderFrameDeliveryText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs")
            .Replace("\r\n", "\n");
        var sourceReaderText = sourceReaderRootText
            + "\n" + sourceReaderDiagnosticsText
            + "\n" + sourceReaderDxgiBuffersText
            + "\n" + sourceReaderFrameLayoutText
            + "\n" + sourceReaderLifecycleText
            + "\n" + sourceReaderInitializationText
            + "\n" + sourceReaderReadLoopText
            + "\n" + sourceReaderFrameDeliveryText
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs")
                .Replace("\r\n", "\n");
        AssertContains(sourceReaderText, "Keep source cadence state coherent with diagnostics snapshots");
        AssertContains(sourceReaderText, "lock (_cadenceLock)");
        AssertContains(sourceReaderDiagnosticsText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderDiagnosticsText, "VTABLE_DIAG RAW slot35_GetSampleTime");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderDxgiBuffersText, "private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)");
        AssertContains(sourceReaderDxgiBuffersText, "private static readonly Guid ID3D11Texture2DIid");
        AssertContains(sourceReaderDxgiBuffersText, "MF_SOURCE_READER_D3D_RESOURCE_FAIL");
        AssertDoesNotContain(sourceReaderRootText, "private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)");
        AssertDoesNotContain(sourceReaderRootText, "private static readonly Guid ID3D11Texture2DIid");
        AssertContains(sourceReaderFrameLayoutText, "public static int GetFrameSizeBytes(int width, int height, bool isP010)");
        AssertContains(sourceReaderFrameLayoutText, "private unsafe static void CopyYuvWithStride(");
        AssertContains(sourceReaderFrameLayoutText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertDoesNotContain(sourceReaderRootText, "public static int GetFrameSizeBytes(int width, int height, bool isP010)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe static void CopyYuvWithStride(");
        AssertDoesNotContain(sourceReaderRootText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertContains(sourceReaderLifecycleText, "public void StartReading(RawFrameCallback onFrame, CancellationToken ct)");
        AssertContains(sourceReaderLifecycleText, "public async Task StopAsync()");
        AssertContains(sourceReaderLifecycleText, "private void ReleaseReaderAndSource()");
        AssertContains(sourceReaderLifecycleText, "private void SignalFatalError(Exception ex)");
        AssertDoesNotContain(sourceReaderRootText, "public void StartReading(RawFrameCallback onFrame, CancellationToken ct)");
        AssertDoesNotContain(sourceReaderRootText, "public async Task StopAsync()");
        AssertDoesNotContain(sourceReaderRootText, "private void ReleaseReaderAndSource()");
        AssertDoesNotContain(sourceReaderRootText, "private void SignalFatalError(Exception ex)");
        AssertContains(sourceReaderInitializationText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertContains(sourceReaderInitializationText, "MF_SOURCE_READER_INIT ");
        AssertContains(sourceReaderInitializationText, "SelectConvertedMediaType(");
        AssertDoesNotContain(sourceReaderRootText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertContains(sourceReaderReadLoopText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderReadLoopText, "reader.ReadSample(");
        AssertContains(sourceReaderReadLoopText, "DeliverFrame(sample, onFrame, onDualFrame, arrivalTick);");
        AssertDoesNotContain(sourceReaderRootText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverFrame(");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverDualFrameFromBuffer(");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe bool TryDeliverDualFrameFrom2DBuffer(");
        AssertContains(sourceReaderFrameDeliveryText, "ArrayPool<byte>.Shared.Rent");
        AssertContains(sourceReaderFrameDeliveryText, "Marshal.Release(gpuTexture)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverFrame(");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverDualFrameFromBuffer(");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe bool TryDeliverDualFrameFrom2DBuffer(");

        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionScenarioStartupSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionPresentMonStartup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionResultBuilderSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionSummaryWriter.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionRunState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionFlashbackExportScenariosSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionFlashbackMetricsSource()
            + "\n" + ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionFlashbackStressScenarioSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionFlashbackWaitsSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionMetricsSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadDiagnosticSessionResultFormatterSource()
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionSampler.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionText.cs")
                .Replace("\r\n", "\n");
        var diagnosticSessionModelsText = ReadRepoFile("tools/Common/DiagnosticSessionModels.cs")
            .Replace("\r\n", "\n");
        var diagnosticScenariosText = ReadRepoFile("tools/Common/DiagnosticSessionScenarios.cs")
            .Replace("\r\n", "\n");
        AssertContains(diagnosticSessionText, "var scenario = DiagnosticSessionScenarios.Normalize(options.Scenario);");
        AssertContains(diagnosticSessionText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(diagnosticSessionText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsFlashback(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsPreview(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsRecording(scenario)");
        AssertContains(diagnosticSessionText, "scenarioPlan.RequiresFlashbackRecordingReadiness");
        AssertContains(diagnosticSessionText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(diagnosticSessionText, "var runState = new DiagnosticSessionRunState(");
        AssertContains(diagnosticSessionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertContains(diagnosticSessionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(diagnosticScenariosText, "internal static IReadOnlyList<string> All { get; }");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlayback = \"flashback-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackStress = \"flashback-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackScrubStress = \"flashback-scrub-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRestartCycle = \"flashback-restart-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackEncoderCycle = \"flashback-encoder-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportPlayback = \"flashback-export-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackSegmentPlayback = \"flashback-segment-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExport = \"flashback-range-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExportAudioSwitch = \"flashback-range-export-audio-switch\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackLifecycle = \"flashback-lifecycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportConcurrent = \"flashback-export-concurrent\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackDisableDuringExport = \"flashback-disable-during-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRotatedExport = \"flashback-rotated-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPreviewCycle = \"flashback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlaybackPreviewCycle = \"flashback-playback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecording = \"flashback-recording\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingPreviewCycle = \"flashback-recording-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingSettingsDeferred = \"flashback-recording-settings-deferred\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingExportRejected = \"flashback-recording-export-rejected\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportRejected = \"flashback-export-rejected\";");
        AssertContains(diagnosticSessionText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(diagnosticSessionText, "catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)");
        AssertContains(diagnosticSessionText, "return BuildLocalFailureResponse(command, ex.Message);");
        AssertContains(diagnosticSessionText, "catch (JsonException ex)");
        AssertContains(diagnosticSessionModelsText, "public sealed class DiagnosticSessionResult");
        AssertContains(diagnosticSessionModelsText, "public string TerminalState { get; set; }");
        AssertContains(diagnosticSessionText, "var livePath = runState.LivePath;");
        AssertContains(diagnosticSessionText, "var initialSnapshotKnown = false;");
        AssertContains(diagnosticSessionText, "skipped state-mutating scenario");
        AssertContains(diagnosticSessionText, "CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs))");
        AssertContains(diagnosticSessionText, "\"SetRecordingEnabled\",");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;");
        AssertContains(diagnosticSessionText, "if (startedRecording && (shouldStopRecordingForVerification || !options.LeaveRunning))");
        AssertContains(diagnosticSessionText, "recording stopped for verification");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = false;");
        AssertContains(diagnosticSessionText, "stoppedRecordingForVerification = shouldStopRecordingForVerification &&");
        AssertContains(diagnosticSessionText, "var diagnosticHealthSnapshot = request.StoppedRecordingForVerification");
        AssertContains(diagnosticSessionText, ".WaitAsync(cancellationToken)");
        AssertContains(diagnosticSessionText, "scenarioCts.Cancel();");
        AssertContains(diagnosticSessionText, "WriteSamplingLiveStateBestEffortAsync");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, runState.LastStage);");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(diagnosticSessionText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxPendingCommandsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsDroppedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsSkippedNotReadyAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackScrubUpdatesCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekCommandsCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "internal readonly record struct PlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "BuildPlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "nonCoalescedDropped={commandHealth.NonCoalescedDropped}");
        AssertContains(diagnosticSessionText, "coalescedSeek={commandHealth.CoalescedSeek}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSubmitFailures\")");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSeekCommandsCoalesced\")");
        AssertContains(diagnosticSessionText, "commandHealth.SubmitFailures > 0");
        AssertContains(diagnosticSessionText, "submitFailures={commandHealth.SubmitFailures}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackCommandsDropped\")");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackObservedFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinObservedFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAvgFrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackP99FrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowSampleWindowObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSessionFrameCountObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowOffsetMs");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFrameCount");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxP99FrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxFrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSlowFramePercentObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeAvgMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeP99MsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeP99MsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSlowFramePercentAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayDoublesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayShrinksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayDoublesObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayShrinksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioBufferedDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioQueueDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAbsAvDriftMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSegmentSwitchesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackFmp4ReopensAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackWriteHeadWaitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(diagnosticSessionText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd");
        AssertContains(diagnosticSessionText, "Flashback Playback Commands:");
        AssertContains(diagnosticSessionText, "coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
        AssertContains(diagnosticSessionText, "Flashback Playback Perf:");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"play\", [\"positionMs\"] = 1000 }");
        AssertContains(diagnosticSessionText, "flashback playback started at 1000ms");
        AssertContains(diagnosticSessionText, "flashback playback returned live");
        AssertContains(diagnosticSessionText, "ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "visualCadenceMetrics,");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "flashback playback: no playback frames were observed");
        AssertContains(diagnosticSessionText, "var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);");
        AssertContains(diagnosticSessionText, "if (!visualCadenceHealthy &&");
        AssertContains(diagnosticSessionText, "GetResetAwareCounterDelta(");
        AssertContains(diagnosticSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(diagnosticSessionText, "public long EndSessionFrameCount { get; set; }");
        AssertDoesNotContain(diagnosticSessionText, "flashback playback: observed FPS dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: 1% low dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: audio buffered duration exceeded budget");
        AssertContains(diagnosticSessionText, "flashback playback: absolute A/V drift exceeded budget");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics)");
        AssertContains(diagnosticSessionText, "var baselineFrameCount = GetNullableLong(initialSnapshot, \"FlashbackPlaybackFrameCount\") ?? 0;");
        AssertContains(diagnosticSessionText, "frameCount > baselineFrameCount");
        AssertContains(diagnosticSessionText, "commandsProcessed > baselineCommandsProcessed");
        AssertContains(diagnosticSessionText, "IsPlaybackSnapshotActive(snapshot)");
        AssertContains(diagnosticSessionText, "var sessionFrameCount = frameCount >= baselineFrameCount");
        AssertContains(diagnosticSessionText, "? frameCount - baselineFrameCount");
        AssertContains(diagnosticSessionText, ": frameCount;");
        AssertContains(diagnosticSessionText, "metrics.EndSessionFrameCount = sessionFrameCount;");
        AssertContains(diagnosticSessionText, "targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240");
        AssertContains(diagnosticSessionText, "onePercentLow > 0 && sessionFrameCount >= minimumPlaybackFramesForLowPercentile");
        AssertContains(diagnosticSessionText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(diagnosticSessionText, "metrics.MaxSessionFrameCountObserved = Math.Max(metrics.MaxSessionFrameCountObserved, sessionFrameCount);");
        AssertContains(diagnosticSessionText, "fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames}");
        AssertContains(diagnosticSessionText, "onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs}");
        AssertContains(diagnosticSessionText, "onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(diagnosticSessionText, "p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##}");
        AssertContains(diagnosticSessionText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved}");
        AssertContains(diagnosticSessionText, "audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd}");
        AssertContains(diagnosticSessionText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "Flashback Playback Decode:");
        AssertContains(diagnosticSessionText, "p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##}");
        AssertContains(diagnosticSessionText, "maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved}");
        AssertContains(diagnosticSessionText, "sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "Flashback Playback Stages:");
        AssertContains(diagnosticSessionText, "seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta}");
        AssertContains(diagnosticSessionText, "FlashbackRecordingBackendObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsMissedRefreshDelta");
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsFailureDelta");
        AssertContains(diagnosticSessionText, "SelectedResolutionAtEnd");
        AssertContains(diagnosticSessionText, "SelectedFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "SelectedExactFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SelectedVideoFormatAtEnd");
        AssertContains(diagnosticSessionText, "VideoRequestedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "VideoNegotiatedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SourceTelemetrySummaryAtEnd");
        AssertContains(diagnosticSessionText, "Capture Mode:");
        AssertContains(diagnosticSessionText, "selected={FormatOptional(result.SelectedResolutionAtEnd)}");
        AssertContains(diagnosticSessionText, "source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd}");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerLastDropReasonAtEnd");
        AssertContains(diagnosticSessionText, "Preview Scheduler:");
        AssertContains(diagnosticSessionText, "droppedDelta={result.PreviewSchedulerDroppedDelta}");
        AssertContains(diagnosticSessionText, "clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta}");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta}");
        AssertContains(diagnosticSessionText, "underflowsDelta={result.PreviewSchedulerUnderflowsDelta}");
        AssertContains(diagnosticSessionText, "resumeReprimesDelta={result.PreviewSchedulerResumeReprimesDelta}");
        AssertContains(diagnosticSessionText, "lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
        AssertContains(diagnosticSessionText, "PreviewD3DLatestSlowFrameReason");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceOutputFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceChangeFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceMinChangeFpsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceMaxRepeatPercentObserved");
        AssertContains(diagnosticSessionText, "ProcessCpuPercentAtEnd");
        AssertContains(diagnosticSessionText, "ProcessCpuMaxPercentObserved");
        AssertContains(diagnosticSessionText, "Preview D3D Perf:");
        AssertContains(diagnosticSessionText, "Preview D3D CPU Timing:");
        AssertContains(diagnosticSessionText, "Preview Visual Cadence:");
        AssertContains(diagnosticSessionText, "Process Perf:");
        AssertContains(diagnosticSessionText, "PreviewCadenceOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewCadenceMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples)");
        AssertContains(diagnosticSessionText, "BuildVisualCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(diagnosticSessionText, "BuildPreviewCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##}");
        AssertContains(diagnosticSessionText, "presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###}");
        AssertContains(diagnosticsText, "PreviewCadenceSlowFramePercent = snapshot.PreviewCadenceSlowFramePercent");
        AssertContains(diagnosticsText, "PreviewCadenceOnePercentLowFps = snapshot.PreviewCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps");
        AssertContains(diagnosticsText, "PreviewD3DPresentCallP95Ms = snapshot.PreviewD3DPresentCallP95Ms");
        AssertContains(diagnosticsText, "PreviewD3DTotalFrameCpuP95Ms = snapshot.PreviewD3DTotalFrameCpuP95Ms");
        AssertContains(diagnosticsText, "PreviewD3DInputUploadCpuP99Ms = snapshot.PreviewD3DInputUploadCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DRenderSubmitCpuP99Ms = snapshot.PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DPresentCallP99Ms = snapshot.PreviewD3DPresentCallP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DTotalFrameCpuP99Ms = snapshot.PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DFrameStatsRecentMissedRefreshCount = snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnosticsText, "FlashbackPlaybackP99FrameMs = snapshot.FlashbackPlaybackP99FrameMs");
        AssertContains(diagnosticsText, "FlashbackPlaybackDecodeP99Ms = snapshot.FlashbackPlaybackDecodeP99Ms");
        AssertContains(diagnosticsText, "FlashbackPlaybackPendingCommands = snapshot.FlashbackPlaybackPendingCommands");
        AssertContains(diagnosticsText, "FlashbackPlaybackSubmitFailures = snapshot.FlashbackPlaybackSubmitFailures");
        AssertContains(diagnosticsText, "FlashbackExportPercent = snapshot.FlashbackExportPercent");
        AssertContains(diagnosticsText, "FlashbackExportThroughputBytesPerSec = snapshot.FlashbackExportThroughputBytesPerSec");
        AssertContains(diagnosticsText, "FlashbackExportLastProgressAgeMs = snapshot.FlashbackExportLastProgressAgeMs");
        AssertContains(diagnosticSessionText, "FlashbackRecordingFileGrowthObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoEncoderPacketsWrittenDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegritySequenceGaps\")");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegrityQueueDroppedFrames\")");
        AssertContains(diagnosticSessionText, "Flashback Recording:");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxElapsedMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportFailureKindAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportOutputPathAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(diagnosticSessionText, "FlashbackExportLastForceRotateFallbackSegmentsAtEnd");
        AssertContains(diagnosticSessionText, "LastExportIdAtEnd");
        AssertContains(diagnosticSessionText, "LastExportSuccessAtEnd");
        AssertContains(diagnosticSessionText, "LastExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxLastProgressAgeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxOutputBytesObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxThroughputBytesPerSecObserved");
        AssertContains(diagnosticSessionText, "BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var healthSnapshot = lastSnapshot;");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "exportId > baselineExportId");
        AssertContains(diagnosticSessionText, "baselineExportActive && exportId == baselineExportId");
        AssertContains(diagnosticSessionText, "lastExportId == exportId");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertContains(diagnosticSessionText, "var shouldRunVerification =");
        AssertContains(diagnosticSessionText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(diagnosticSessionText, "verificationCommand = \"VerifyFile\"");
        AssertContains(diagnosticSessionText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExport => Path.Combine(outputDirectory, \"flashback-range-export.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExportAudioSwitch => Path.Combine(outputDirectory, \"flashback-range-export-audio-switch.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackExportConcurrent => Path.Combine(outputDirectory, \"flashback-concurrent-a.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackRotatedExport => Path.Combine(outputDirectory, \"flashback-rotated-export.mp4\")");
        AssertContains(diagnosticScenariosText, "return exportPath.Length > 0;");
        AssertDoesNotContain(diagnosticScenariosText, "return exportPath.Length > 0 && File.Exists(exportPath);");
        AssertContains(diagnosticSessionText, "expected BufferInactive failure kind");
        AssertContains(diagnosticSessionText, "expected UnavailableDuringRecording failure kind");
        AssertContains(diagnosticSessionText, "flashback rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "flashback recording rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "Flashback Export:");
        AssertContains(diagnosticSessionText, "failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)}");
        AssertContains(diagnosticSessionText, "messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta}");
        AssertContains(diagnosticSessionText, "lastResultIdEnd={result.LastExportIdAtEnd}");
        AssertContains(diagnosticSessionText, "lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)}");
        AssertContains(diagnosticSessionText, "lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)}");
        AssertContains(diagnosticSessionText, "maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
        AssertContains(diagnosticSessionText, "BuildFlashbackRecordingMetrics(initialSnapshot, samples)");
        AssertContains(diagnosticSessionText, "seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(diagnosticSessionText, "cleanup: preview remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: Flashback remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: playback did not return live state={state}");
        AssertContains(diagnosticSessionText, "metrics.MaxPendingCommandsObserved = Math.Max(");
        AssertContains(diagnosticSessionText, "if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, \"FlashbackPlaybackMaxCommandQueueLatencyCommand\") ?? string.Empty;");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(diagnosticSessionText, "flashback scrub stress begin requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress update burst requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress end requested");
        AssertContains(diagnosticSessionText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0 &&\n                string.Equals(");
        AssertContains(diagnosticSessionText, "state={GetString(lastSnapshot, \"FlashbackPlaybackState\") ?? \"Unknown\"}");
        AssertContains(diagnosticSessionText, "flashback scrub stress: playback did not settle live with an empty queue within 10s");
        AssertDoesNotContain(diagnosticSessionText, "flashback scrub stress: playback worker still alive after drain wait");
        AssertContains(diagnosticSessionText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback restart cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback encoder preset restored to");
        AssertContains(diagnosticSessionText, "flashback encoder cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(diagnosticSessionText, "flashback export during playback verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(diagnosticSessionText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(diagnosticSessionText, "flashback segment playback live headroom established");
        AssertContains(diagnosticSessionText, "flashback segment playback started near boundary");
        AssertContains(diagnosticSessionText, "frameCount >= 180");
        AssertContains(diagnosticSessionText, "playback FPS below source-rate target after warm sample");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-range-export.mp4\"");
        AssertContains(diagnosticSessionText, "\"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticSessionText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(diagnosticSessionText, "\"SetAudioEnabled\"");
        AssertContains(diagnosticSessionText, "FlashbackExportActive");
        AssertContains(diagnosticSessionText, "[\"useSelectionRange\"] = true");
        AssertContains(diagnosticSessionText, "actions.Add($\"{scenarioLabel} verified\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(diagnosticSessionText, "async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(\"FlashbackExport\");");
        AssertContains(diagnosticSessionText, "var exportTaskA = sendCommandAsync(\"FlashbackExport\", exportPayloadA, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "var exportTaskB = sendCommandAsync(\"FlashbackExport\", exportPayloadB, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "flashback concurrent exports verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(diagnosticSessionText, "var disableTask = SendCommandWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "flashback disable/export requests issued");
        AssertContains(diagnosticSessionText, "flashback disable during export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-rotated-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback rotated segment observed");
        AssertContains(diagnosticSessionText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(diagnosticSessionText, "exportedSegments is null or < 2");
        AssertContains(diagnosticSessionText, "flashback rotated export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback recording preview cycle preview stopped");
        AssertContains(diagnosticSessionText, "const int recordingCleanupTimeoutMs = 300_000;");
        AssertContains(diagnosticSessionText, "\"SetRecordingEnabled\",");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred preset restored to");
        AssertContains(diagnosticSessionText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "Flashback export is unavailable while Flashback is the active recording backend");
        AssertContains(diagnosticSessionText, "flashback lifecycle disabled during playback");
        AssertContains(diagnosticSessionText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(diagnosticSessionText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(diagnosticSessionText, "\"flashback recording: RecordingBackend never reported Flashback\"");
        AssertContains(diagnosticSessionText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(diagnosticSessionText, "submittedDelta");
        AssertContains(diagnosticSessionText, "packetsDelta");
        AssertContains(diagnosticSessionText, "RecordingIntegritySequenceGaps");
        AssertContains(diagnosticSessionText, "RecordingIntegrityQueueDroppedFrames");
        AssertContains(diagnosticSessionText, "GetInt(snapshot, \"FlashbackBufferedDurationMs\") >= requiredBufferedDurationMs");
        AssertContains(diagnosticSessionText, "(GetNullableLong(snapshot, \"FlashbackEncodedFrames\") ?? 0) >= requiredEncodedFrames");
        AssertContains(diagnosticSessionText, "const int liveEdgeSafetyMarginMs = 5_000;");
        AssertContains(diagnosticSessionText, "const int leftEdgeSafetyMarginMs = 10_000;");
        AssertContains(diagnosticSessionText, "outPointMs + liveEdgeSafetyMarginMs + leftEdgeSafetyMarginMs");
        AssertContains(diagnosticSessionText, "var rangeEndMs = (int)Math.Clamp(bufferedDurationMs - liveEdgeSafetyMarginMs, 0, int.MaxValue);");
        AssertContains(diagnosticSessionText, "var rangeStartMs = Math.Max(0, rangeEndMs - outPointMs);");
        AssertContains(diagnosticSessionText, "requiredStartMs>={leftEdgeSafetyMarginMs}");
        AssertContains(diagnosticSessionText, "\"flashback stress: Flashback buffer did not become export-ready within 30s\"");
        AssertContains(diagnosticSessionText, "\"FlashbackAction\", new Dictionary<string, object?> { [\"action\"] = \"pause\" }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"seek\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })");
        AssertContains(diagnosticSessionText, "actions.Add(\"flashback scrub burst requested\");");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = positions[^1] }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"seconds\"] = 1, [\"outputPath\"] = exportPath }");
        AssertContains(diagnosticSessionText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(diagnosticSessionText, "$\"maxPending={GetInt(lastSnapshot, \"FlashbackPlaybackMaxPendingCommands\")} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={GetInt(lastSnapshot, \"FlashbackPlaybackMaxCommandQueueLatencyMs\")} \"");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(diagnosticSessionText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(diagnosticSessionText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(diagnosticSessionText, "var playbackBaselineSnapshot = await WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not enter Playing before warm sample\"");
        AssertContains(diagnosticSessionText, "var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object");
        AssertContains(diagnosticSessionText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(diagnosticSessionText, "flashback playback warmed frames=");
        AssertContains(diagnosticSessionText, "audioFallbackDelta={warmedAudioFallbackDelta}");
        AssertContains(diagnosticSessionText, "staleDelta={warmedAudioStaleDelta}");
        AssertContains(diagnosticSessionText, "driftOutlierDelta={warmedAudioDriftOutlierDelta}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not warm for");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unavailable fallbacks exceeded startup allowance \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler deadline drops increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler underflows increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: D3D frame stats failures increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: present/display pressure \"");
        AssertContains(diagnosticSessionText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(diagnosticSessionText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "var allowedDrops = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 10.0));");
        AssertContains(diagnosticSessionText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "var allowedDeadlineDrops = Math.Max(6, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 45.0));");
        AssertContains(diagnosticSessionText, "var allowedUnderflows = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 120.0));");
        AssertContains(diagnosticSessionText, "bool tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "var onePercentLowFloor = targetFps * 0.80;");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy =");
        AssertContains(diagnosticSessionText, "IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps)");
        AssertContains(diagnosticSessionText, "if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)");
        AssertContains(diagnosticSessionText, "visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "var presentP99BudgetMs = targetFrameMs * 1.25;");
        AssertContains(diagnosticSessionText, "var totalP99BudgetMs = targetFrameMs * 1.35;");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command latency exceeded threshold \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyCommand={FormatOptional(maxLatencyCommand)}\"");
        AssertContains(diagnosticSessionText, "\"flashback-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "$\"flashback export rejected: expected Failed status, got {status}\"");
        AssertContains(diagnosticSessionText, "message.Contains(\"Flashback buffer not active\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "actions.Add(\n            \"flashback segment playback observed \"");
        AssertDoesNotContain(diagnosticSessionText, "flashback segment playback: excessive late frames");
        AssertContains(diagnosticSessionText, "var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "BuildSourceCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var sourceReaderFramesDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(diagnosticSessionText, "var videoIngestErrorsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(diagnosticSessionText, "var sparseSourceCaptureCadenceWarning =");
        AssertContains(diagnosticSessionText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "var toleratesFlashbackForceRotateDrainWarning =");
        AssertContains(diagnosticSessionText, "IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertContains(diagnosticSessionText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "lastReject=force_rotate_draining");
        AssertContains(diagnosticSessionText, "sourceReaderFramesDroppedDelta > 0");
        AssertContains(diagnosticSessionText, "videoIngestErrorsDelta > 0");
        AssertContains(diagnosticSessionText, "var allowedSparseEvents = Math.Max(1, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 180.0));");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticWarmupFraction");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticMaxWarmupMs");
        AssertContains(diagnosticSessionText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(diagnosticSessionText, "diagnosticHealthSucceeded &&");
        AssertContains(diagnosticSessionText, "var toleratesSourceSignalHealthWarning =");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health source-signal warning tolerated for export reliability scenario");
        AssertContains(diagnosticSessionText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario");
        AssertContains(diagnosticSessionText, "var flashbackWarningsSucceeded = !isFlashbackScenario ||");
        AssertContains(diagnosticSessionText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(diagnosticSessionText, "flashbackWarningsSucceeded,");
        AssertContains(diagnosticScenariosText, "internal static string HelpList { get; } = string.Join(\"|\", All);");
        AssertContains(diagnosticScenariosText, "All.Contains(normalized, StringComparer.Ordinal)");

        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var ssctlCommandHandlersText = (ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            + "\n" + ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"))
            .Replace("\r\n", "\n");
        var mcpDiagnosticSessionText = ReadRepoFile("tools/McpServer/Tools/DiagnosticSessionTools.cs")
            .Replace("\r\n", "\n");
        AssertContains(ssctlProgramText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(mcpDiagnosticSessionText, "flashback-export-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-segment-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-encoder-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export-audio-switch");
        AssertContains(mcpDiagnosticSessionText, "flashback-disable-during-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-rotated-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-settings-deferred");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-export-rejected");

        return Task.CompletedTask;
    }

}
