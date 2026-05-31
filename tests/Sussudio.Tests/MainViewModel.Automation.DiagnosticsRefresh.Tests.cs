using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnostics = ReadAutomationDiagnosticsHubSourceFamily();
        var countersText = ReadAutomationDiagnosticsHubCountersSource();
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDiagnosticsRefreshCoreOwnership(diagnostics);
        AssertDiagnosticsAlertEventOwnership(diagnostics);
        AssertDiagnosticsSnapshotStatusProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshPipelineOwnership(diagnostics, dispatcherText);
        AssertDiagnosticsRefreshFlashbackRecordingAndStorageAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackPlaybackAndPreviewAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackExportOwnership(dispatcherText);
        AssertDiagnosticsRefreshSourceReaderOwnership();

        var diagnosticSessionSources = ReadDiagnosticSessionSourceFamily();
        AssertDiagnosticSessionCoreOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionPlaybackMetricsOwnership(diagnosticSessionSources.SourceFamilyText);
        AssertDiagnosticSessionPreviewMetricsOwnership(diagnosticSessionSources.SourceFamilyText, diagnostics);
        AssertDiagnosticSessionExportRecordingOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionFlashbackScenarioOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionToolSurfaceOwnership();

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticsRefreshCoreOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshEvaluationOwnership(diagnostics);
        AssertDiagnosticsRefreshRuntimeOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotConstructionOwnership(diagnostics);
    }

    private static void AssertDiagnosticsRefreshEvaluationOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.EvaluationText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertContains(diagnostics.EvaluationText, "private static bool IsCaptureOnePercentLowDegraded(");
        AssertContains(diagnostics.EvaluationText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertContains(diagnostics.EvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnostics.EvaluationText, "var lanes = BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.EvaluationText, "var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.EvaluationText, "var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackStorageDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackRecordingDiagnosticEvaluation(health, isRecording, recentFlashbackRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackExportDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackPlaybackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackStorageDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_storage\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackExportDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback export is running.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackPlaybackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_playback\"");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs")),
            "Flashback playback diagnostic evaluation partial folded into Flashback evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs")),
            "Flashback recording diagnostic evaluation folded into Flashback evaluation owner");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackRecordingDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "BuildFlashbackRecordingDiagnosticConditions(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackEncoderFailureDiagnosticEvaluation(health, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackExportRotationDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackBackendSettingsDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(conditions, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static FlashbackRecordingDiagnosticConditions BuildFlashbackRecordingDiagnosticConditions(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackEncoderFailureDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackExportRotationDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_export\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackBackendSettingsDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeStateDiagnosticEvaluation(health, isPreviewing, isRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeRecordingDiagnosticEvaluation(captureRuntime, health, isRecording, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeSourceDiagnosticEvaluation(health, isPreviewing, visualCadenceHealthy, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimeMjpegDiagnosticEvaluation(health, recentMjpeg, lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeStateDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"diagnostic_unavailable\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeRecordingDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"recording\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"audio\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeSourceDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"source_capture\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeMjpegDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"source_signal\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"mjpeg_decode\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewRendererDiagnosticEvaluation(lanes)");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "TryBuildRealtimePreviewPresentDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"preview_scheduler\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewRendererDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"renderer\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewPresentDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"present_display\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Preview scheduler failed to submit frames.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Renderer pacing is the likely preview bottleneck.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display cadence is the likely preview bottleneck.");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display 1% low is below target.");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs")),
            "Realtime preview diagnostic evaluation helpers folded into realtime evaluation owner");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildSourceLane(health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildPreviewLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildRenderLane(previewRuntime, recentRenderer)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildRecordingLane(captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildAudioLane(captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "BuildFlashbackRecordingLane(health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildDecodeLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildRecordingLane(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildAudioLane(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackRecordingLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackExportLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackTempCacheLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackPlaybackCommandLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildFlashbackPlaybackPerformanceLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildSourceLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildSourceSignalLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildPreviewLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static DiagnosticEvaluationRenderLane BuildRenderLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildPresentLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static string BuildVisualLane(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationRenderLane(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "var sourceTarget =");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationLanes(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs")),
            "Diagnostic lane text builders folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluation.cs")),
            "Diagnostic verdict branch policies folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs")),
            "Flashback diagnostic evaluation folded into Evaluation owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs")),
            "Realtime diagnostic evaluation folded into Evaluation owner");
        AssertDoesNotContain(diagnostics.HubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnostics.HubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
    }

    private static void AssertDiagnosticsAlertEventOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.AlertsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnostics.AlertsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.AlertsText, "private void AddEventThrottled(");
        AssertContains(diagnostics.AlertsText, "private void SetAlertState(");
        AssertContains(diagnostics.AlertsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvents.cs")),
            "diagnostic event state helpers folded into AutomationDiagnosticsHub.Alerts.cs");
        AssertContains(diagnostics.AlertsText, "UpdateSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateAudioSignalAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateRecordingGrowthAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "private void UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.AlertsText, "\"preview-stall\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-timeout\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-failed\"");
        AssertContains(diagnostics.AlertsText, "\"preview-cadence-slow\"");
        AssertContains(diagnostics.AlertsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-drop\"");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateAudioSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"audio-muted-suspect\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateRecordingGrowthAlerts(");
        AssertContains(diagnostics.AlertsText, "\"recording-not-growing\"");
        AssertContains(diagnostics.AlertsText, "var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackStorageAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackEncoderAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-recording-degraded\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackStorageAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackEncoderAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackSubmitFailureAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAudioAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-slow\"");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs")),
            "Flashback recording alert rules folded into the main alerts owner");
        AssertDoesNotContain(diagnostics.HubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
    }

    private static void AssertDiagnosticsRefreshSourceReaderOwnership()
    {
        var sourceReaderSources = ReadMfSourceReaderVideoCaptureSourceFamily();
        var sourceReaderRootText = sourceReaderSources.RootText;
        var sourceReaderFrameLayoutText = sourceReaderSources.FrameLayoutText;
        var sourceReaderLifecycleText = sourceReaderSources.LifecycleText;
        var sourceReaderInitializationText = sourceReaderSources.InitializationText;
        var sourceReaderInitializedSessionText = sourceReaderSources.InitializedSessionText;
        var sourceReaderReadLoopText = sourceReaderSources.ReadLoopText;
        var sourceReaderFrameDeliveryText = sourceReaderSources.FrameDeliveryText;
        var sourceReaderText = sourceReaderSources.SourceFamilyText;
        AssertContains(sourceReaderText, "Keep source cadence state coherent with diagnostics snapshots");
        AssertContains(sourceReaderText, "lock (_cadenceLock)");
        AssertContains(sourceReaderLifecycleText, "public SourceCadenceMetrics GetSourceCadenceMetrics()");
        AssertContains(sourceReaderLifecycleText, "private void TrackSourceCadence(long mfTimestamp100ns)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Cadence.cs")),
            "source-reader cadence metrics folded into active lifecycle owner");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderFrameDeliveryText, "VTABLE_DIAG RAW slot35_GetSampleTime");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Diagnostics.cs")),
            "source-reader vtable diagnostic folded into frame delivery");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderFrameDeliveryText, "private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)");
        AssertContains(sourceReaderFrameDeliveryText, "private static readonly Guid ID3D11Texture2DIid");
        AssertContains(sourceReaderFrameDeliveryText, "MF_SOURCE_READER_D3D_RESOURCE_FAIL");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.DxgiBuffers.cs")),
            "MfSourceReaderVideoCapture DXGI texture extraction folded into frame delivery");
        AssertDoesNotContain(sourceReaderRootText, "private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)");
        AssertDoesNotContain(sourceReaderRootText, "private static readonly Guid ID3D11Texture2DIid");
        AssertContains(sourceReaderFrameLayoutText, "public static int GetFrameSizeBytes(int width, int height, bool isP010)");
        AssertContains(sourceReaderFrameLayoutText, "private unsafe static void CopyYuvWithStride(");
        AssertContains(sourceReaderFrameLayoutText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.FrameLayout.cs")),
            "shared source-reader frame layout helpers folded into the root source-reader state");
        AssertContains(sourceReaderLifecycleText, "public void StartReading(RawFrameCallback onFrame, CancellationToken ct)");
        AssertContains(sourceReaderLifecycleText, "public async Task StopAsync()");
        AssertContains(sourceReaderLifecycleText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderLifecycleText, "private void ReleaseReaderAndSource()");
        AssertContains(sourceReaderLifecycleText, "private void SignalFatalError(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.ReadLoop.cs")),
            "source-reader read loop folded into active lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Lifecycle.cs")),
            "source-reader lifecycle folded into root source-reader state");
        AssertContains(sourceReaderInitializationText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertContains(sourceReaderInitializationText, "MF_SOURCE_READER_INIT ");
        AssertContains(sourceReaderInitializationText, "SelectConvertedMediaType(");
        AssertContains(sourceReaderInitializationText, "ApplyCurrentMediaTypeAndReconcileActualOutput(");
        AssertContains(sourceReaderInitializationText, "CommitInitializedRuntimeState(");
        AssertContains(sourceReaderInitializedSessionText, "private readonly record struct SourceReaderNegotiatedMode(");
        AssertContains(sourceReaderInitializedSessionText, "private SourceReaderNegotiatedMode ApplyCurrentMediaTypeAndReconcileActualOutput(");
        AssertContains(sourceReaderInitializedSessionText, "sourceReader.GetCurrentMediaType(");
        AssertContains(sourceReaderInitializedSessionText, "private void ValidateNegotiatedOutputMode(");
        AssertContains(sourceReaderInitializedSessionText, "private void CommitInitializedRuntimeState(");
        AssertContains(sourceReaderInitializedSessionText, "MF_NATIVE_FORMAT_OVERRIDE");
        AssertContains(sourceReaderInitializedSessionText, "Volatile.Write(ref _nativeInputFormat");
        AssertContains(sourceReaderInitializedSessionText, "Interlocked.Exchange(ref _framesDelivered");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.InitializedSession.cs")),
            "source-reader initialized-session handoff folded into active lifecycle owner");
        AssertContains(sourceReaderRootText, "public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Initialization.cs")),
            "source-reader initialization folded into active lifecycle owner");
        AssertContains(sourceReaderReadLoopText, "private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)");
        AssertContains(sourceReaderReadLoopText, "reader.ReadSample(");
        AssertContains(sourceReaderReadLoopText, "DeliverFrame(sample, onFrame, onDualFrame, arrivalTick);");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverFrame(");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverDualFrameFromBuffer(");
        AssertContains(sourceReaderFrameDeliveryText, "Marshal.Release(gpuTexture)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertContains(sourceReaderFrameDeliveryText, "private unsafe bool TryDeliverDualFrameFrom2DBuffer(");
        AssertContains(sourceReaderFrameDeliveryText, "ArrayPool<byte>.Shared.Rent");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.RawFrameDelivery.cs")),
            "raw/compressed source-reader frame extraction folded into frame delivery");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverFrame(");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DeliverDualFrameFromBuffer(");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe bool TryDeliverDualFrameFrom2DBuffer(");
    }

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

    private static void AssertDiagnosticSessionPreviewMetricsOwnership(string diagnosticSessionText, AutomationDiagnosticsHubSourceFamily diagnostics)
    {
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
        AssertContains(diagnostics.TimelineText, "PreviewCadenceSlowFramePercent = preview.CadenceSlowFramePercent");
        AssertContains(diagnostics.TimelineText, "PreviewCadenceOnePercentLowFps = preview.CadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DTotalFrameCpuP95Ms = preview.D3DTotalFrameCpuP95Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DInputUploadCpuP99Ms = preview.D3DInputUploadCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DRenderSubmitCpuP99Ms = preview.D3DRenderSubmitCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP99Ms = preview.D3DPresentCallP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DTotalFrameCpuP99Ms = preview.D3DTotalFrameCpuP99Ms");
        AssertContains(diagnostics.TimelineText, "PreviewD3DFrameStatsRecentMissedRefreshCount = preview.D3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "CadenceSlowFramePercent: snapshot.PreviewCadenceSlowFramePercent");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "D3DFrameStatsRecentMissedRefreshCount: snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackP99FrameMs = flashbackPlayback.P99FrameMs");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackDecodeP99Ms = flashbackPlayback.DecodeP99Ms");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackPendingCommands = flashbackPlayback.PendingCommands");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackSubmitFailures = flashbackPlayback.SubmitFailures");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "P99FrameMs: snapshot.FlashbackPlaybackP99FrameMs");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "DecodeP99Ms: snapshot.FlashbackPlaybackDecodeP99Ms");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "PendingCommands: snapshot.FlashbackPlaybackPendingCommands");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "SubmitFailures: snapshot.FlashbackPlaybackSubmitFailures");
        AssertContains(diagnostics.TimelineText, "FlashbackExportPercent = flashbackExport.Percent");
        AssertContains(diagnostics.TimelineText, "FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec");
        AssertContains(diagnostics.TimelineText, "FlashbackExportLastProgressAgeMs = flashbackExport.LastProgressAgeMs");
        AssertContains(diagnostics.TimelineText, "Percent: snapshot.FlashbackExportPercent");
        AssertContains(diagnostics.TimelineText, "ThroughputBytesPerSec: snapshot.FlashbackExportThroughputBytesPerSec");
        AssertContains(diagnostics.TimelineText, "LastProgressAgeMs: snapshot.FlashbackExportLastProgressAgeMs");
    }

    internal static Task Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected()
    {
        var diagnosticsType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var runtimeType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var verifierResultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        var method = diagnosticsType.GetMethod(
            "BuildHdrTruthVerdict",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { runtimeType, typeof(bool), verifierResultType },
            modifiers: null)
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict not found.");

        var runtime = Activator.CreateInstance(runtimeType)!;
        SetPropertyBackingField(runtime, "LatestObservedFramePixelFormat", "NV12");
        SetPropertyBackingField(runtime, "ObservedNv12FrameCount", 1L);
        SetPropertyBackingField(runtime, "SourceIsHdr", (bool?)true);

        var verdict = method.Invoke(null, new object?[] { runtime, false, null })
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict returned null.");

        AssertEqual("expected-sdr-capture", GetStringProperty(verdict, "SourceVsCaptureParity"), "SourceVsCaptureParity");
        AssertEqual("sdr-8bit", GetStringProperty(verdict, "FinalClassification"), "FinalClassification");

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticsRefreshRuntimeOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyFileAsync");
        AssertContains(diagnostics.VerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnostics.VerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnostics.VerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertContains(diagnostics.VerificationText, "Automatic recording verification started.");
        AssertContains(diagnostics.VerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnostics.VerificationText, "string.Equals(verificationProfile, \"flashback-export\"");
        AssertDoesNotContain(diagnostics.HubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.HubText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(diagnostics.HubText, "IAutomationSnapshotQueryPort snapshotQueryPort,");
        AssertContains(diagnostics.HubText, "_snapshotQueryPort = snapshotQueryPort ?? throw new ArgumentNullException(nameof(snapshotQueryPort));");
        AssertDoesNotContain(diagnostics.HubText, "IAutomationViewModel viewModel,");
        AssertDoesNotContain(diagnostics.HubText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetViewModelRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.VerificationText, "await _snapshotQueryPort\n                .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnostics.SnapshotsText, "ClassifyPreviewPacing(");
        AssertContains(diagnostics.HubText, "public void Start()");
        AssertContains(diagnostics.HubText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertContains(diagnostics.HdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.HdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertContains(diagnostics.HdrText, "static string NormalizeFormatToken(string? text)");
        AssertDoesNotContain(diagnostics.HubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.SnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnostics.SnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
    }

    private static void AssertDiagnosticsRefreshPipelineOwnership(AutomationDiagnosticsHubSourceFamily diagnostics, string dispatcherText)
    {
        AssertContains(diagnostics.SnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnostics.SnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnostics.SnapshotsCoreText, "public AutomationSnapshot GetLatestSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "public Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default)");
        AssertContains(diagnostics.SnapshotsCoreText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsCoreText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnostics.SnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnostics.SnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnostics.SnapshotsCoreText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnostics.SnapshotsCoreText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnostics.TimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnostics.TimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "var core = BuildPerformanceTimelineCoreProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var preview = BuildPerformanceTimelinePreviewProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackPlayback = BuildPerformanceTimelineFlashbackPlaybackProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackExport = BuildPerformanceTimelineFlashbackExportProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var system = BuildPerformanceTimelineSystemProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps = core.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "FlashbackExportPercent = flashbackExport.Percent");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent = system.ProcessCpuPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineCoreProjection BuildPerformanceTimelineCoreProjection(");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps: snapshot.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "private static PerformanceTimelinePreviewProjection BuildPerformanceTimelinePreviewProjection(");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "D3DPresentCallP95Ms: snapshot.PreviewD3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var cadence = BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var decode = BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var commands = BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var audioMaster = BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var stages = BuildPerformanceTimelineFlashbackPlaybackStagesProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var backend = BuildPerformanceTimelineFlashbackPlaybackBackendProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineFlashbackExportProjection BuildPerformanceTimelineFlashbackExportProjection(");
        AssertContains(diagnostics.TimelineText, "Percent: snapshot.FlashbackExportPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineSystemProjection BuildPerformanceTimelineSystemProjection(");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent: snapshot.ProcessCpuPercent");
        AssertDoesNotContain(diagnostics.HubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertContains(diagnostics.SnapshotsText, "_lastVerification = null;");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "Automatic recording verification started.");
        AssertContains(diagnostics.SnapshotsCoreText, "new FileInfo(lastOutputPath).Length");
        AssertContains(diagnostics.SnapshotsCoreText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnostics.HubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SourceFamilyText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnostics.SourceFamilyText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SourceFamilyText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                return await ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n        var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
    }

    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = surface.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = startup.Strategy,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewSummary.AdapterColorMetadata,");
    }

    private static void AssertDiagnosticSessionToolSurfaceOwnership()
    {
        var diagnosticSessionToolSources = ReadDiagnosticSessionToolSurfaceSourceFamily();
        var ssctlProgramText = diagnosticSessionToolSources.SsctlProgramText;
        var ssctlHelpText = diagnosticSessionToolSources.SsctlHelpText;
        var ssctlCommandHandlersText = diagnosticSessionToolSources.SsctlCommandHandlersText;
        var mcpDiagnosticSessionText = diagnosticSessionToolSources.McpDiagnosticSessionText;
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertContains(ssctlProgramText, "internal static class SsctlHelpWriter");
        AssertDoesNotContain(ssctlProgramText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(ssctlHelpText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionScenarioCatalog.Description");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertDoesNotContain(mcpDiagnosticSessionText, "Session scenario: observe,");
        AssertDoesNotContain(mcpDiagnosticSessionText, "string scenario = \"observe\"");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int seconds = 10");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int sampleIntervalMs = 1000");
    }
}
static partial class Program
{
    private static void AssertDiagnosticSessionCoreOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticSessionModelsText = diagnosticSessionSources.ModelsText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;
        AssertContains(diagnosticSessionText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(diagnosticSessionText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(diagnosticSessionText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(diagnosticSessionText, "scenarioPlan.RequiresFlashbackRecordingReadiness");
        AssertContains(diagnosticSessionText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertContains(diagnosticSessionText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(diagnosticSessionText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(diagnosticSessionText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(");
        AssertContains(diagnosticSessionText, ".CompleteRegisteredScenarioWorkAsync(");
        AssertContains(diagnosticSessionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(diagnosticScenariosText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(diagnosticScenariosText, "internal static partial class DiagnosticSessionScenarioCatalog");
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
        AssertDoesNotContain(diagnosticSessionModelsText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(diagnosticSessionModelsText, "public string TerminalState { get; set; }");
        AssertContains(diagnosticSessionText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(diagnosticSessionText, "CreateUnknownInitialSnapshot()");
        AssertContains(diagnosticSessionText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(diagnosticSessionText, "skipped state-mutating scenario");
        AssertContains(diagnosticSessionText, "CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs))");
        AssertContains(diagnosticSessionText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;");
        AssertContains(diagnosticSessionText, "if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))");
        AssertContains(diagnosticSessionText, "recording stopped for verification");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = shouldStopRecordingForVerification &&");
        AssertContains(diagnosticSessionText, "var diagnosticHealthSnapshot = request.StoppedRecordingForVerification");
        AssertContains(diagnosticSessionText, ".WaitAsync(cancellationToken)");
        AssertContains(diagnosticSessionText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(diagnosticSessionText, "WriteSamplingLiveStateBestEffortAsync");
        AssertContains(diagnosticSessionText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(diagnosticSessionText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
    }
}
static partial class Program
{
    private static void AssertDiagnosticSessionExportRecordingOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;

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
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(diagnosticSessionText, "var shouldRunVerification =");
        AssertContains(diagnosticSessionText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(diagnosticSessionText, "verificationCommand = \"VerifyFile\"");
        AssertContains(diagnosticSessionText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExportAudioSwitch,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackExportConcurrent,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-concurrent-a.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRotatedExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-rotated-export.mp4\"");
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
        AssertContains(diagnosticSessionText, "private static void ValidateCleanupLifecycleRestored(");
        AssertContains(diagnosticSessionText, "cleanup: preview remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: Flashback remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: playback did not return live state={state}");
        AssertContains(diagnosticSessionText, "metrics.MaxPendingCommandsObserved = Math.Max(");
        AssertContains(diagnosticSessionText, "if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, \"FlashbackPlaybackMaxCommandQueueLatencyCommand\") ?? string.Empty;");
    }


    private static void AssertDiagnosticsRefreshSnapshotConstructionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "private AutomationSnapshot BuildAutomationSnapshot(");
        AssertContains(diagnostics.SnapshotProjectionText, "var projections = BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "return BuildAutomationSnapshotFromProjections(projections);");
        AssertContains(diagnostics.SnapshotProjectionText, "private AutomationSnapshotProjectionSet BuildAutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private readonly record struct AutomationSnapshotProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshot BuildAutomationSnapshotFromProjections(");
        AssertContains(diagnostics.SnapshotProjectionText, "AutomationSnapshotProjectionSet projections");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFlattenedProjectionSet(projections)");
        AssertContains(diagnostics.SnapshotProjectionText, "BuildAutomationSnapshotFromFlattenedProjections(flattened)");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "return new AutomationSnapshot\n        {");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private readonly record struct AutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningAutomationSnapshotText, "private static AutomationSnapshot BuildAutomationSnapshotFromFlattenedProjections(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningAutomationSnapshotText, "return new AutomationSnapshot");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSnapshotStatusFlattenedProjection(snapshotStatus)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureFormatFlattenedProjection(captureFormat)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatRequestedProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatHdrRequestProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatActualProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatNegotiatedProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatReaderObservationProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "BuildCaptureFormatEncoderProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureTransportFlattenedProjection(captureTransport)");
        AssertContains(diagnostics.SnapshotProjectionCaptureFormatText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureCadenceFlattenedProjection(captureCadence)");
        AssertContains(diagnostics.SnapshotProjectionCaptureCadenceText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildVisualCadenceFlattenedProjection(visualCadence)");
        AssertContains(diagnostics.SnapshotProjectionVisualCadenceText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegFlattenedProjection(mjpeg)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegTimingFlattenedProjection(mjpeg.Timing)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter)");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionMjpegPreviewJitterText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash)");
        AssertContains(diagnostics.SnapshotProjectionMjpegText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry)");
        AssertContains(diagnostics.SnapshotProjectionSourceSignalText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionSourceSignalText, "private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionSourceTelemetryText, "private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildSettingsFlattenedProjection(userSettings, recordingSettings)");
        AssertContains(diagnostics.SnapshotProjectionUserSettingsText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildHdrPipelineFlattenedProjection(hdrPipeline)");
        AssertContains(diagnostics.HdrText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildPreviewRuntimeFlattenedProjection(previewSummary)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeFrameProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeCadenceProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeSurfaceProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeStartupProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildPreviewD3DFlattenedProjection(previewD3D)");
        AssertContains(diagnostics.SnapshotProjectionPreviewD3DText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackExportText, "private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackRecordingFlattenedProjection(flashbackRecording)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingStartupCacheProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingRuntimeProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingBackendProjection(captureRuntime, health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "BuildFlashbackRecordingEncoderProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingQueuesText, "private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackRecordingText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackTimingProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode)");
        AssertContains(diagnostics.SnapshotProjectionFlashbackPlaybackText, "BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingIntegrityFlattenedProjection(recordingIntegrity)");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingIntegrityText, "private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingPipelineFlattenedProjection(recordingPipeline)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineEncoderProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineIngestProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineVideoQueueProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "BuildRecordingPipelineHardwareQueuesProjection(health)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput)");
        AssertContains(diagnostics.SnapshotProjectionRecordingPipelineText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildProcessResourceFlattenedProjection(processResourceProjection)");
        AssertContains(diagnostics.SnapshotProjectionProcessResourcesText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAvSyncFlattenedProjection(avSync)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAudioAndIngestFlattenedProjection(audioAndIngest)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildAudioSignalProjection(viewModelSnapshot, audioSignal)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildCaptureIngestProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "BuildWasapiAudioProjection(captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioDropsProjection BuildAudioDropsProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureIngestText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionCaptureIngestText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionWasapiAudioText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionWasapiAudioText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildAudioDropsFlattenedProjection(audioDrops)");
        AssertContains(diagnostics.SnapshotProjectionAudioText, "private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "BuildCaptureCommandFlattenedProjection(captureCommands)");
        AssertContains(diagnostics.SnapshotProjectionText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "new AutomationSnapshot");
    }

    private static AutomationDiagnosticsHubSourceFamily ReadAutomationDiagnosticsHubSourceFamily()
    {
        return new AutomationDiagnosticsHubSourceFamily
        {
            HubText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            EvaluationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationFlashbackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationRealtimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            DiagnosticEvaluationLanesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Evaluation.cs"),
            AlertsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Alerts.cs"),
            VerificationText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            HdrText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotsText = ReadAutomationDiagnosticsHubSnapshotsSource(),
            SnapshotsCoreText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            SnapshotProjectionText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionFlatteningText = ReadAutomationSnapshotFlatteningFamilyText(),
            SnapshotProjectionFlatteningAutomationSnapshotText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"),
            SnapshotProjectionAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Media.cs"),
            SnapshotProjectionCaptureIngestText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Media.cs"),
            SnapshotProjectionWasapiAudioText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Media.cs"),
            SnapshotProjectionCaptureFormatText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionCaptureCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionVisualCadenceText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionMjpegText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionMjpegPreviewJitterText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs"),
            SnapshotProjectionFlashbackExportText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs"),
            SnapshotProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs"),
            SnapshotProjectionFlashbackRecordingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs"),
            SnapshotProjectionFlashbackRecordingQueuesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs"),
            SnapshotProjectionPreviewD3DText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Preview.cs"),
            SnapshotProjectionPreviewD3DFrameFlowText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Preview.cs"),
            SnapshotProjectionPreviewD3DCpuTimingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Preview.cs"),
            SnapshotProjectionPreviewRuntimeText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Preview.cs"),
            SnapshotProjectionProcessResourcesText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionRecordingIntegrityText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Media.cs"),
            SnapshotProjectionRecordingPipelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.Media.cs"),
            SnapshotProjectionSourceSignalText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionSourceTelemetryText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            SnapshotProjectionUserSettingsText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.SnapshotProjection.cs"),
            PreviewPacingText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.Snapshots.cs"),
            TimelineText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            TimelineProjectionPreviewText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
            TimelineProjectionFlashbackPlaybackText = ReadAutomationDiagnosticsHubSourceFile("AutomationDiagnosticsHub.cs"),
        };
    }

    private static string ReadAutomationDiagnosticsHubSourceFile(string fileName)
    {
        return ReadRepoFile("Sussudio/Services/Automation/" + fileName)
            .Replace("\r\n", "\n");
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
        public string DiagnosticEvaluationFlashbackText { get; init; } = string.Empty;
        public string DiagnosticEvaluationRealtimeText { get; init; } = string.Empty;
        public string DiagnosticEvaluationLanesText { get; init; } = string.Empty;
        public string AlertsText { get; init; } = string.Empty;
        public string VerificationText { get; init; } = string.Empty;
        public string HdrText { get; init; } = string.Empty;
        public string SnapshotsText { get; init; } = string.Empty;
        public string SnapshotsCoreText { get; init; } = string.Empty;
        public string SnapshotProjectionText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningText { get; init; } = string.Empty;
        public string SnapshotProjectionFlatteningAutomationSnapshotText { get; init; } = string.Empty;
        public string SnapshotProjectionAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureIngestText { get; init; } = string.Empty;
        public string SnapshotProjectionWasapiAudioText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureFormatText { get; init; } = string.Empty;
        public string SnapshotProjectionCaptureCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionVisualCadenceText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegText { get; init; } = string.Empty;
        public string SnapshotProjectionMjpegPreviewJitterText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackExportText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackPlaybackText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingText { get; init; } = string.Empty;
        public string SnapshotProjectionFlashbackRecordingQueuesText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DFrameFlowText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewD3DCpuTimingText { get; init; } = string.Empty;
        public string SnapshotProjectionPreviewRuntimeText { get; init; } = string.Empty;
        public string SnapshotProjectionProcessResourcesText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingIntegrityText { get; init; } = string.Empty;
        public string SnapshotProjectionRecordingPipelineText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceSignalText { get; init; } = string.Empty;
        public string SnapshotProjectionSourceTelemetryText { get; init; } = string.Empty;
        public string SnapshotProjectionUserSettingsText { get; init; } = string.Empty;
        public string PreviewPacingText { get; init; } = string.Empty;
        public string TimelineText { get; init; } = string.Empty;
        public string TimelineProjectionPreviewText { get; init; } = string.Empty;
        public string TimelineProjectionFlashbackPlaybackText { get; init; } = string.Empty;

        public string SourceFamilyText => _sourceFamilyText ??= string.Join(
            "\n",
            new[]
            {
                HubText,
                EvaluationText,
                DiagnosticEvaluationFlashbackText,
                DiagnosticEvaluationRealtimeText,
                DiagnosticEvaluationLanesText,
                AlertsText,
                VerificationText,
                HdrText,
                SnapshotsText,
                SnapshotProjectionText,
                SnapshotProjectionFlatteningText,
                SnapshotProjectionAudioText,
                SnapshotProjectionCaptureIngestText,
                SnapshotProjectionWasapiAudioText,
                SnapshotProjectionCaptureFormatText,
                SnapshotProjectionCaptureCadenceText,
                SnapshotProjectionMjpegText,
                SnapshotProjectionMjpegPreviewJitterText,
                SnapshotProjectionFlashbackExportText,
                SnapshotProjectionFlashbackPlaybackText,
                SnapshotProjectionFlashbackRecordingText,
                SnapshotProjectionFlashbackRecordingQueuesText,
                SnapshotProjectionPreviewD3DText,
                SnapshotProjectionPreviewD3DFrameFlowText,
                SnapshotProjectionPreviewRuntimeText,
                SnapshotProjectionProcessResourcesText,
                SnapshotProjectionRecordingIntegrityText,
                SnapshotProjectionRecordingPipelineText,
                SnapshotProjectionSourceSignalText,
                SnapshotProjectionSourceTelemetryText,
                SnapshotProjectionUserSettingsText,
                HdrText,
                PreviewPacingText,
                TimelineText,
                TimelineProjectionPreviewText,
                TimelineProjectionFlashbackPlaybackText,
                SnapshotProjectionPreviewD3DCpuTimingText,
            });
    }

    private static AutomationDiagnosticsHubCountersSourceFamily ReadAutomationDiagnosticsHubCountersSource()
    {
        var countersText = ReadNormalizedRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");

        return new AutomationDiagnosticsHubCountersSourceFamily(
            countersText,
            countersText);
    }

    private static string ReadCaptureServiceDiagnosticsRefreshSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
    }

    private static string ReadFlashbackBackendResourcesSource()
    {
        return ReadNormalizedRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
    }

    private static MfSourceReaderVideoCaptureSourceFamily ReadMfSourceReaderVideoCaptureSourceFamily()
    {
        var rootText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs");
        var diagnosticsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs");
        var frameLayoutText = rootText;
        var lifecycleText = rootText;
        var initializationText = rootText;
        var initializedSessionText = initializationText;
        var readLoopText = lifecycleText;
        var frameDeliveryText = ReadNormalizedRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs");

        return new MfSourceReaderVideoCaptureSourceFamily(
            rootText,
            diagnosticsText,
            frameLayoutText,
            lifecycleText,
            initializationText,
            initializedSessionText,
            readLoopText,
            frameDeliveryText,
            string.Join(
                "\n",
                new[]
                {
                    rootText,
                    lifecycleText,
                    frameDeliveryText,
                }));
    }

    private static DiagnosticSessionSourceFamily ReadDiagnosticSessionSourceFamily()
    {
        return new DiagnosticSessionSourceFamily(
            ReadDiagnosticSessionRunnerSource()
                + "\n" + ReadDiagnosticSessionScenarioStartupSource()
                + "\n" + ReadDiagnosticSessionCleanupActionsSource()
                + "\n" + ReadDiagnosticSessionResultBuilderSource()
                + "\n" + ReadDiagnosticSessionBackgroundTasksSource()
                + "\n" + ReadDiagnosticSessionFlashbackCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackExportScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackMetricsSource()
                + "\n" + ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
                + "\n" + ReadDiagnosticSessionFlashbackSupportSource()
                + "\n" + ReadDiagnosticSessionFlashbackStressScenarioSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
                + "\n" + ReadDiagnosticSessionMetricsSource()
                + "\n" + ReadNormalizedRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
                + "\n" + ReadDiagnosticSessionResultFormatterSource()
                + "\n" + ReadDiagnosticSessionScenarioCatalogSource(),
            ReadDiagnosticSessionModelsSource(),
            ReadDiagnosticSessionScenarioCatalogSource());
    }

    private static DiagnosticSessionToolSurfaceSourceFamily ReadDiagnosticSessionToolSurfaceSourceFamily()
    {
        return new DiagnosticSessionToolSurfaceSourceFamily(
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/Program.cs"),
            ReadNormalizedRepoFile("tools/ssctl/CommandHandlers.cs"),
            ReadNormalizedRepoFile("tools/McpServer/Tools/AppStateTools.cs"));
    }

    private static string ReadDiagnosticSessionScenarioCatalogSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs");
    }

    private static string ReadDiagnosticSessionFlashbackSupportSource()
    {
        return ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs");
    }

    private static string ReadNormalizedRepoFile(string path)
    {
        return ReadRepoFile(path).Replace("\r\n", "\n");
    }

    private readonly record struct MfSourceReaderVideoCaptureSourceFamily(
        string RootText,
        string DiagnosticsText,
        string FrameLayoutText,
        string LifecycleText,
        string InitializationText,
        string InitializedSessionText,
        string ReadLoopText,
        string FrameDeliveryText,
        string SourceFamilyText);

    private readonly record struct DiagnosticSessionSourceFamily(
        string SourceFamilyText,
        string ModelsText,
        string ScenariosText);

    private readonly record struct DiagnosticSessionToolSurfaceSourceFamily(
        string SsctlProgramText,
        string SsctlHelpText,
        string SsctlCommandHandlersText,
        string McpDiagnosticSessionText);

    private readonly record struct AutomationDiagnosticsHubCountersSourceFamily(
        string RealtimePreviewText,
        string SourceFamilyText);
}
