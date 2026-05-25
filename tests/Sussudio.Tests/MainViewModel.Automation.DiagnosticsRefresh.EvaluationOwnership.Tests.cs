static partial class Program
{
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
        AssertDoesNotContain(diagnostics.EvaluationText, "\"flashback_storage\"");
        AssertDoesNotContain(diagnostics.EvaluationText, "\"source_capture\"");
        AssertDoesNotContain(diagnostics.EvaluationText, "var sourceTarget =");
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
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.DiagnosticEvaluation.cs")),
            "Root diagnostic evaluation partial folded into Evaluation owner");
        AssertDoesNotContain(diagnostics.HubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnostics.HubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
    }
}
