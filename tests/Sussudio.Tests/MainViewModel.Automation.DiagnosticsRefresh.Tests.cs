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
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private readonly record struct AutomationSnapshotFlattenedProjectionSet(");
        AssertContains(diagnostics.SnapshotProjectionText, "private static AutomationSnapshot BuildAutomationSnapshotFromFlattenedProjections(");
        AssertContains(diagnostics.SnapshotProjectionText, "return new AutomationSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs")),
            "final automation snapshot DTO initialization folded into AutomationDiagnosticsHub.SnapshotProjection.cs");
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

    private static void AssertDiagnosticsRefreshFlashbackRecordingAndStorageAlertCoverage(
        AutomationDiagnosticsHubSourceFamily diagnostics,
        AutomationDiagnosticsHubCountersSourceFamily counters)
    {
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.SourceFamilyText, "DiagnosticsCategory.Flashback");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackExportActive");
        AssertContains(diagnostics.SourceFamilyText, "Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnostics.SourceFamilyText, "Math.Max(0, health.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnostics.SourceFamilyText, "elapsedMs={health.FlashbackExportElapsedMs}");
        AssertContains(diagnostics.SourceFamilyText, "throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "kind={exportFailureKind}");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackExportStallThresholdMs = 30000;");
        AssertContains(diagnostics.AlertsText, "exportLastProgressAgeMs >= FlashbackExportStallThresholdMs");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.Export} progressAgeMs={exportLastProgressAgeMs}\"");
        AssertContains(diagnostics.SourceFamilyText, "private long _lastFlashbackExportCompletionEventId;");
        AssertContains(diagnostics.SourceFamilyText, "ObserveFlashbackExportCompletion(snapshot);");
        AssertContains(diagnostics.SourceFamilyText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackExportCompletedUtcUnixMs <= 0");
        AssertContains(diagnostics.SourceFamilyText, "Interlocked.CompareExchange(\n                ref _lastFlashbackExportCompletionEventId");
        AssertContains(diagnostics.SourceFamilyText, "status.Equals(\"Succeeded\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "status.Equals(\"Cancelled\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackExportFailureKind");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "SettingsStale = backend.SettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "SettingsStale = health.FlashbackBackendSettingsStale,");
        AssertContains(diagnostics.SourceFamilyText, "backendStale={health.FlashbackBackendSettingsStale}");
        AssertContains(diagnostics.SourceFamilyText, "kind={failureKind}");
        AssertContains(diagnostics.SourceFamilyText, "Flashback export completed: status={status}");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackTempDriveLowFreeBytes = 5L * 1024L * 1024L * 1024L;");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackRecordingBackpressureWarningMs = 100;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackRecordingQueueDepthWarningRatio = 0.75;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackAudioQueueDepthWarningRatio = 0.90;");
        AssertContains(diagnostics.SourceFamilyText, "private const long FlashbackRecordingQueueAgeWarningMs = 500;");
        AssertContains(diagnostics.AlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackStartupCacheOverBudget");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_storage\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback temp storage is under pressure.\"");
        AssertContains(diagnostics.AlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackEncodingFailed");
        AssertContains(diagnostics.AlertsText, "Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? \"Unknown\"}");
        AssertContains(diagnostics.AlertsText, "\"flashback-recording-degraded\"");
        AssertContains(counters.SourceFamilyText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps)");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped)");
        AssertContains(counters.SourceFamilyText, "Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents)");
        AssertContains(counters.SourceFamilyText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(counters.SourceFamilyText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(diagnostics.SourceFamilyText, "var recentFlashbackRecording = UpdateFlashbackRecordingRecentCounters(health, nowTick);");
        AssertContains(diagnostics.SourceFamilyText, "UpdateAlerts(snapshot, recentFlashbackRecording);");
        AssertContains(diagnostics.SourceFamilyText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackRecordingQueueBacklog =");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackAudioQueueBacklog =");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackAudioQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "flashbackRecordingRecentForceRotateGap");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason)");
        AssertContains(diagnostics.SourceFamilyText, "flashbackRecordingRecent.SequenceGaps > 0");
        AssertContains(diagnostics.AlertsText, "(flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap)");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingRecent.GpuFramesDropped > 0");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingRecentBackpressure");
        AssertContains(diagnostics.AlertsText, "flashbackRecordingQueueBacklog");
        AssertContains(diagnostics.AlertsText, "flashbackAudioQueueBacklog");
        AssertContains(diagnostics.AlertsText, "snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnostics.AlertsText, "Flashback recording path degraded:");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.AlertsText, "Flashback export rotation skipped live-edge frames:");
        AssertContains(diagnostics.SourceFamilyText, "forceRotate={snapshot.FlashbackForceRotateActive}");
        AssertContains(diagnostics.SourceFamilyText, "requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining}");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = flashbackRecordingFlattening.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = flashbackRecording.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FatalCleanupInProgress = health.FatalCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackCleanupInProgress = flashbackRecordingFlattening.CleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "CleanupInProgress = flashbackRecording.CleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "CleanupInProgress = health.FlashbackCleanupInProgress");
        AssertContains(diagnostics.SourceFamilyText, "recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents}");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnostics.SourceFamilyText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio)");
        AssertContains(diagnostics.SourceFamilyText, "oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)");
        AssertContains(diagnostics.SourceFamilyText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio)");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsFlashbackForceRotateRejectReason(string? reason)");
        AssertContains(diagnostics.SourceFamilyText, "string.Equals(reason, \"force_rotate_queue_guard\"");
        AssertContains(diagnostics.SourceFamilyText, "flashback recording active={health.FlashbackActive}");
        AssertContains(diagnostics.SourceFamilyText, "fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}");
        AssertContains(diagnostics.SourceFamilyText, "var recordingIntegrityIncomplete =");
        AssertContains(diagnostics.SourceFamilyText, "string.Equals(captureRuntime.RecordingIntegrityStatus, \"Incomplete\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnostics.SourceFamilyText, "(recordingIntegrityIncomplete && !isRecording)");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackRecordingDegraded =");
        AssertContains(diagnostics.SourceFamilyText, "recentFlashbackRecording.EncoderDroppedFrames > 0");
        AssertContains(diagnostics.SourceFamilyText, "recentFlashbackRecording.BackpressureEvents > 0");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackBackendSettingsUnexpectedlyStale =");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackBackendSettingsStale &&\n            !isRecording");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackVideoQueueDepth,\n                 health.FlashbackVideoQueueCapacity,\n                 health.FlashbackVideoQueueOldestFrameAgeMs");
        AssertContains(diagnostics.SourceFamilyText, "forceRotate={health.FlashbackForceRotateActive}");
        AssertContains(diagnostics.SourceFamilyText, "queueRejects={health.FlashbackVideoQueueRejectedFrames}");
        AssertContains(diagnostics.SourceFamilyText, "audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity}");
        AssertContains(diagnostics.SourceFamilyText, "lastReject={health.FlashbackVideoQueueLastRejectReason ?? \"None\"}");
        AssertContains(diagnostics.SourceFamilyText, "flashbackExportRotationGap");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback export rotation skipped live-edge frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining}");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_recording\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_export\"");
        AssertContains(diagnostics.SourceFamilyText, "var flashbackForceRotateRejectWithoutDamage =");
        AssertContains(diagnostics.SourceFamilyText, "!flashbackForceRotateRejectWithoutDamage &&\n              recentFlashbackRecording.SequenceGaps > 0");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackExportActive ||\n             health.FlashbackForceRotateActive ||\n             health.FlashbackForceRotateRequested ||\n             health.FlashbackForceRotateDraining");
    }

    private static void AssertDiagnosticsRefreshFlashbackPlaybackAndPreviewAlertCoverage(
        AutomationDiagnosticsHubSourceFamily diagnostics,
        AutomationDiagnosticsHubCountersSourceFamily counters)
    {
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackCommandStallThresholdMs = 1000;");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-command-failed\"");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackCommandFailureRecentMs = 30000;");
        AssertContains(diagnostics.SourceFamilyText, "playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback command failed recently:");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback command failed recently.\"");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackSlowFpsRatio = 0.75;");
        AssertContains(diagnostics.SourceFamilyText, "private const double CaptureOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const double PreviewOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackOnePercentLowMinimumFrames = 1200;");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-slow\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-target-below-selection\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-present-capped\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-frametime-degraded\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback frame submission failed");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackPendingCommands > 0");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackCommandQueueCapacity");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps = timing.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps = health.FlashbackPlaybackTargetFps");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps");
        AssertContains(diagnostics.SourceFamilyText, "TargetFps: snapshot.FlashbackPlaybackTargetFps");
        AssertContains(diagnostics.SourceFamilyText, "FlashbackPlaybackPtsCadenceMismatchCount = flashbackPlaybackFlattening.Timing.PtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount");
        AssertContains(diagnostics.SourceFamilyText, "ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount}");
        AssertContains(diagnostics.SourceFamilyText, "private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)");
        AssertContains(diagnostics.SourceFamilyText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            snapshot.FlashbackPlaybackTargetFps,\n            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnostics.SourceFamilyText, "IsFlashbackPlaybackFrametimeDegraded(\n                snapshot.FlashbackPlaybackState");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackState,\n                playbackTargetFps,\n                snapshot.FlashbackPlaybackFrameCount");
        AssertContains(diagnostics.SourceFamilyText, "IsCaptureOnePercentLowDegraded(\n                snapshot.ExpectedCaptureFrameRate");
        AssertContains(diagnostics.SourceFamilyText, "IsPreviewOnePercentLowDegraded(\n                snapshot.PreviewCadenceExpectedIntervalMs");
        AssertContains(diagnostics.SourceFamilyText, "\"Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.Source} | {lanes.Visual}\"");
        AssertContains(diagnostics.SourceFamilyText, "captureCadenceExpectedFrameRate: health.ExpectedFrameRate");
        AssertContains(diagnostics.SourceFamilyText, "captureCadenceOnePercentLowFps: health.CaptureCadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "previewCadenceExpectedIntervalMs: previewRuntime.DisplayCadenceExpectedIntervalMs");
        AssertContains(diagnostics.SourceFamilyText, "previewCadenceOnePercentLowFps: previewRuntime.DisplayCadenceOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "reasons.Add($\"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnostics.SourceFamilyText, "reasons.Add($\"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackOnePercentLowFps");
        AssertContains(diagnostics.SourceFamilyText, "frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnostics.SourceFamilyText, "cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is using wall-clock pacing instead of audio-master pacing");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback audio queue is backing up");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is below target rate");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback target is below the selected capture rate");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback is targeting HFR but D3D present cadence is below target");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback frametime degraded");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertContains(diagnostics.SourceFamilyText, "snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0");
        AssertContains(diagnostics.SourceFamilyText, "Flashback playback command queue has not drained");
        AssertContains(diagnostics.SourceFamilyText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnostics.SourceFamilyText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnostics.SourceFamilyText, "\"flashback_playback\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback command queue is stalled.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback is below target rate.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback frametime is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Flashback playback frame submission failed.\"");
        AssertContains(diagnostics.SourceFamilyText, "queuedAge={playbackCommandQueueAgeMs}ms");
        AssertContains(diagnostics.SourceFamilyText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnostics.SourceFamilyText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            health.FlashbackPlaybackTargetFps,\n            health.ExpectedFrameRate);");
        AssertContains(diagnostics.SourceFamilyText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnostics.SourceFamilyText, "playback perf state={health.FlashbackPlaybackState}");
        AssertContains(diagnostics.SourceFamilyText, "fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "target={health.FlashbackPlaybackTargetFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##}");
        AssertContains(diagnostics.SourceFamilyText, "private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.SourceFamilyText, "ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps");
        AssertContains(diagnostics.SourceFamilyText, "private const double FlashbackPlaybackAudioMasterFallbackWarningRatio = 0.50;");
        AssertContains(diagnostics.SourceFamilyText, "private const int FlashbackPlaybackAudioQueueBacklogWarningDepth = 24;");
        AssertContains(diagnostics.SourceFamilyText, "decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodePhase={health.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnostics.SourceFamilyText, "decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnostics.SourceFamilyText, "audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnostics.SourceFamilyText, "audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnostics.SourceFamilyText, "health.FlashbackPlaybackSubmitFailures <= 0");
        AssertContains(diagnostics.SourceFamilyText, "UpdatePreviewJitterRecentCounters(health, nowTick)");
        AssertContains(diagnostics.SourceFamilyText, "UpdateD3DRendererRecentCounters(previewRuntime, nowTick)");
        AssertContains(counters.RealtimePreviewText, "private PreviewJitterRecentCounters UpdatePreviewJitterRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "private long _lastPreviewJitterTotalDropped;");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastPreviewJitterTotalDropped, totalDropped)");
        AssertContains(counters.RealtimePreviewText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "private long _lastD3DFramesSubmitted;");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted)");
        AssertContains(counters.RealtimePreviewText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastMjpegCompressedDropsQueueFull, compressedQueueDrops)");
        AssertContains(counters.RealtimePreviewText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertDoesNotContain(diagnostics.HubText, "private long _lastPreviewJitterTotalDropped;");
        AssertDoesNotContain(diagnostics.HubText, "private long _lastD3DFramesSubmitted;");
        AssertContains(diagnostics.SourceFamilyText, "recentSubmitted={recentRendererSubmitted} recentDropped={recentRenderer.Dropped}");
        AssertContains(diagnostics.SourceFamilyText, "var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)");
        AssertContains(diagnostics.SourceFamilyText, "clearedDrops={health.MjpegPreviewJitterClearedDropCount}");
        AssertContains(diagnostics.SourceFamilyText, "resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}");
        AssertContains(diagnostics.SourceFamilyText, "UpdateD3DFrameStatsRecentCounters(previewRuntime, nowTick)");
        AssertContains(counters.RealtimePreviewText, "private long UpdateD3DFrameLatencyWaitRecentCounters(");
        AssertContains(counters.RealtimePreviewText, "Interlocked.Exchange(ref _lastD3DFrameLatencyWaitTimeouts, timeouts)");
        AssertContains(diagnostics.SourceFamilyText, "recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}");
        AssertContains(diagnostics.SourceFamilyText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Capture cadence 1% low is below target:");
        AssertContains(diagnostics.SourceFamilyText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.SourceFamilyText, "previewOnePercentLowDegraded && !visualCadenceHealthy");
        AssertContains(diagnostics.SourceFamilyText, "\"Preview/display 1% low is below target:");
        AssertContains(diagnostics.SourceFamilyText, "FormatVisualCadenceAlertDetail(snapshot)");
        AssertContains(diagnostics.SourceFamilyText, "visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps");
        AssertContains(diagnostics.SourceFamilyText, "var previewSubmitFailed = string.Equals(");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPreviewJitterLastDropReason,\n            \"submit-failed\"");
        AssertContains(diagnostics.SourceFamilyText, "if (!previewSubmitFailed &&\n            (recentPreviewDeadlineDrops <= 0 || visualCadenceHealthy) &&\n            recentPreviewUnderflows <= 3)");
        AssertContains(diagnostics.SourceFamilyText, "\"Preview scheduler failed to submit frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "var presentCadenceOverBudget =\n            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&\n            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;");
        AssertContains(diagnostics.SourceFamilyText, "var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);");
        AssertContains(diagnostics.SourceFamilyText, "latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}");
        AssertContains(diagnostics.SourceFamilyText, "inputUpload={frame.InputUploadCpuMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "renderSubmit={frame.RenderSubmitCpuMs:0.##}ms");
        AssertContains(diagnostics.SourceFamilyText, "var unsyncedPresentCallSlow =\n            previewRuntime.D3DPresentSyncInterval == 0 &&\n            previewRuntime.D3DPresentCallP95Ms > 4.0;");
        AssertContains(diagnostics.SourceFamilyText, "if (presentCadenceOverBudget ||\n            unsyncedPresentCallSlow)");
        AssertContains(diagnostics.SourceFamilyText, "if (!captureOnePercentLowDegraded)");
        AssertContains(diagnostics.SourceFamilyText, "\"Source/capture 1% low is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "if (!previewOnePercentLowDegraded)");
        AssertContains(diagnostics.SourceFamilyText, "var visualCadenceHealthy =\n            IsVisualCadenceHealthy(");
        AssertContains(diagnostics.SourceFamilyText, "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.");
        AssertContains(diagnostics.SourceFamilyText, "if (visualCadenceHealthy)\n        {\n            return new DiagnosticEvaluation(\n                \"Healthy\",");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPacketHashDuplicateFramePercent < 20.0");
        AssertContains(diagnostics.SourceFamilyText, "health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnostics.SourceFamilyText, "var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);");
        AssertContains(diagnostics.SourceFamilyText, "\"source_signal\"");
        AssertContains(diagnostics.SourceFamilyText, "\"Captured HFR MJPEG cadence contains repeated source frames.\"");
        AssertContains(diagnostics.SourceFamilyText, "$\"{lanes.MjpegDuplicate} | {lanes.Visual} | {lanes.SourceSignal}\"");
        AssertContains(diagnostics.SourceFamilyText, "!visualCadenceHealthy &&\n            IsPreviewOnePercentLowDegraded(");
        AssertContains(diagnostics.SourceFamilyText, "private static bool IsVisualCadenceHealthy(");
        AssertContains(diagnostics.SourceFamilyText, "changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio");
        AssertContains(diagnostics.SourceFamilyText, "repeatFramePercent <= 1.0");
        AssertContains(diagnostics.SourceFamilyText, "longestRepeatRun <= 1");
        AssertContains(diagnostics.SourceFamilyText, "\"Present/display 1% low is below target.\"");
        AssertContains(diagnostics.SourceFamilyText, "var recentMjpeg = UpdateMjpegRecentCounters(health, nowTick);");
        AssertContains(diagnostics.SourceFamilyText, "recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}");
        AssertContains(diagnostics.SourceFamilyText, "recentMjpeg.TotalDropped <= 0");
        AssertContains(diagnostics.SourceFamilyText, "if (recentRendererSubmitted < DiagnosticThresholds.RendererDropWarningMinSamples ||\n            recentRendererDropPercent <= DiagnosticThresholds.RendererDropWarningPercent)");
        AssertDoesNotContain(diagnostics.SourceFamilyText, "rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||\n            previewRuntime.DisplayCadenceSlowFramePercent > 1.0");
    }

    private static void AssertDiagnosticsRefreshFlashbackExportOwnership(string dispatcherText)
    {
        var captureServiceText = ReadCaptureServiceDiagnosticsRefreshSource();
        var flashbackBackendText = ReadFlashbackBackendResourcesSource();
        var exportOperationsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs");
        var exportCoreText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs");
        var exportDiagnosticsText = ReadNormalizedRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs");
        AssertContains(captureServiceText, "private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportCoreText, "PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "ForceRotateForExport");
        AssertContains(exportCoreText, "CreateFlashbackExportThrottleDelayProvider");
        AssertContains(exportDiagnosticsText, "private long BeginFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private void RecordRejectedFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private void CompleteFlashbackExportDiagnostics(");
        AssertContains(exportDiagnosticsText, "private IProgress<ExportProgress> CreateFlashbackExportProgressSink(");
        AssertContains(exportDiagnosticsText, "private void UpdateFlashbackExportProgress(");
        AssertContains(exportDiagnosticsText, "private void RecordFlashbackExportForceRotateFallback(");
        AssertContains(exportDiagnosticsText, "private sealed class FlashbackExportProgressForwarder");
        AssertContains(captureServiceText, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);");
        AssertContains(captureServiceText, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(captureServiceText, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();\n            }");
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
        var exportRangeMethod = ExtractMemberCode(exportOperationsText, "ExportFlashbackRangeAsync");
        var exportLastNMethod = ExtractMemberCode(exportOperationsText, "ExportFlashbackLastNSecondsAsync");
        AssertContains(exportRangeMethod, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportRangeMethod, "operationName: \"range\",");
        AssertContains(exportRangeMethod, "snapshotExporter: snapshot.Exporter,");
        AssertContains(exportRangeMethod, "resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(");
        AssertContains(exportLastNMethod, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportLastNMethod, "operationName: \"last_n\",");
        AssertContains(exportLastNMethod, "snapshotExporter: snapshot.Exporter,");
        AssertContains(exportLastNMethod, "resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds)");
        var backendSnapshotMethod = ExtractMemberCode(exportOperationsText, "SnapshotFlashbackExportBackendAsync");
        AssertContains(backendSnapshotMethod, "new FlashbackExporter()");
        AssertContains(backendSnapshotMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(backendSnapshotMethod, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
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
            exportCoreText,
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)",
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        AssertContains(forceRotateFailedBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(forceRotateFailedBlock, "preserved_segments={preservedArtifacts.Count}");
        AssertContains(forceRotateFailedBlock, "return FlashbackExportForceRotatePreparation.Failure(result);");
        var forceRotateFallbackBlock = ExtractTextBetween(
            exportCoreText,
            "if (segmentPaths.Count == 0)",
            "return FlashbackExportForceRotatePreparation.Ready");
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
        AssertContains(exportCoreText, "ResolveFlashbackExportRangeAfterEvictionPaused(");
        AssertContains(exportCoreText, "var validStart = manager.ValidStartPts;");
        AssertContains(exportCoreText, "var bufferedDuration = manager.BufferedDuration;");
        AssertContains(exportCoreText, "var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);");
        AssertContains(exportCoreText, "var bufferOutPoint = outPoint.HasValue\n            ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)\n            : TimeSpan.MaxValue;");
        AssertContains(exportCoreText, "var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);");
        AssertContains(exportCoreText, "var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);");
        AssertContains(captureServiceText, ".Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))");
        AssertContains(captureServiceText, "var pathKey = TryGetFullPath(path);");
        AssertContains(captureServiceText, "segmentInfo.TryGetValue(pathKey, out var info)");
        AssertContains(captureServiceText, "private static string? TryGetFullPath(string? path)");
        AssertContains(captureServiceText, "FLASHBACK_PATH_NORMALIZE_WARN");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "if (ct.IsCancellationRequested)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export cancelled.\");\n        }\n\n        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export duration must be finite, greater than zero, and within TimeSpan range.\");\n        }");
        AssertRegex(
            dispatcherText,
            "if \\(!double\\.IsFinite\\(seconds\\) \\|\\|\\n\\s*seconds <= 0 \\|\\|\\n\\s*seconds > TimeSpan\\.MaxValue\\.TotalSeconds\\)",
            "Flashback export duration guard");
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
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, sessionReleaseOperation);");
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
        AssertContains(exportCoreText, "private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)");
        AssertContains(captureServiceText, "if (bufferedDuration <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }");
        AssertContains(exportCoreText, "private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)");
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
        AssertContains(flashbackExporterText, "var comparePtsUs = state.UseSegmentTimeline");
        AssertContains(flashbackExporterText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(flashbackExporterText, "FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR");

    }

    private static void AssertDiagnosticSessionPlaybackMetricsOwnership(string diagnosticSessionText)
    {
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
        AssertContains(diagnosticSessionText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(diagnosticSessionText, "var sessionFrameCount = frameCount >= baselineFrameCount");
        AssertContains(diagnosticSessionText, "? frameCount - baselineFrameCount");
        AssertContains(diagnosticSessionText, ": frameCount;");
        AssertContains(diagnosticSessionText, "metrics.EndSessionFrameCount = relevance.SessionFrameCount;");
        AssertContains(diagnosticSessionText, "targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240");
        AssertContains(diagnosticSessionText, "if (onePercentLow <= 0 || sessionFrameCount < minimumPlaybackFramesForLowPercentile)");
        AssertContains(diagnosticSessionText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(diagnosticSessionText, "relevance.SessionFrameCount);");
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
    }

    private static void AssertDiagnosticSessionFlashbackScenarioOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticModelsText = diagnosticSessionSources.ModelsText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;

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
        AssertContains(diagnosticSessionText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred preset restored to");
        AssertContains(diagnosticSessionText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "Flashback export is unavailable while Flashback is the active recording backend");
        AssertContains(diagnosticSessionText, "flashback lifecycle disabled during playback");
        AssertContains(diagnosticSessionText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(diagnosticSessionText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackExportRejectedAsync(");
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
        AssertContains(diagnosticSessionText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(diagnosticSessionText, "return positions[^1];");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
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
        AssertContains(diagnosticSessionText, "CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(diagnosticSessionText, "audioFallbackDelta={warmedAudioFallbacks.TotalDelta}");
        AssertContains(diagnosticSessionText, "staleDelta={warmedAudioFallbacks.StaleDelta}");
        AssertContains(diagnosticSessionText, "driftOutlierDelta={warmedAudioFallbacks.DriftOutlierDelta}");
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
        AssertContains(diagnosticSessionText, "BuildDiagnosticHealthSourceWarningCounters(initialSnapshot, lastSnapshot)");
        AssertContains(diagnosticSessionText, "SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(diagnosticSessionText, "VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(diagnosticSessionText, "var sparseSourceCaptureCadenceWarning =");
        AssertContains(diagnosticSessionText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
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
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health source-signal warning tolerated for export reliability scenario");
        AssertContains(diagnosticSessionText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario");
        AssertContains(diagnosticSessionText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(diagnosticSessionText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(diagnosticSessionText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(diagnosticSessionText, "FlashbackWarningsSucceeded: EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(diagnosticScenariosText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(diagnosticScenariosText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(diagnosticScenariosText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(diagnosticScenariosText, "internal const string HelpList =");
        AssertContains(diagnosticScenariosText, "internal const string Description =");
        AssertContains(diagnosticModelsText, "internal const string CliUsage =");
        AssertContains(diagnosticModelsText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(diagnosticScenariosText, "TryGetEntry(normalized, out _)");
        AssertContains(diagnosticScenariosText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(diagnosticScenariosText, "entry.FlashbackExportVerificationFileName");
    }
}
