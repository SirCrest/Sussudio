static partial class Program
{
    private static void AssertDiagnosticsRefreshCoreOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.EvaluationPolicyText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertDoesNotContain(diagnostics.EvaluationText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertContains(diagnostics.EvaluationText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertContains(diagnostics.DiagnosticEvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationText, "var lanes = BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.DiagnosticEvaluationText, "var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationText, "var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationFlashbackText, "\"flashback_storage\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "\"source_capture\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimeText, "var previewDiagnostic = TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimePreviewText, "private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimePreviewText, "\"preview_scheduler\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimePreviewText, "\"renderer\"");
        AssertContains(diagnostics.DiagnosticEvaluationRealtimePreviewText, "\"present_display\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "\"preview_scheduler\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "\"renderer\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "\"present_display\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "Preview scheduler failed to submit frames.");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "Renderer pacing is the likely preview bottleneck.");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display cadence is the likely preview bottleneck.");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationRealtimeText, "Present/display 1% low is below target.");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationText, "\"flashback_storage\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationText, "\"source_capture\"");
        AssertDoesNotContain(diagnostics.DiagnosticEvaluationText, "var sourceTarget =");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnostics.DiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationLanes(");
        AssertDoesNotContain(diagnostics.EvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertDoesNotContain(diagnostics.HubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnostics.HubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.VerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnostics.VerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnostics.VerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnostics.VerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertDoesNotContain(diagnostics.HubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.PreviewPacingText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnostics.SnapshotsText, "ClassifyPreviewPacing(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new PreviewPacingClassificationInput");
        AssertContains(diagnostics.LifecycleText, "public void Start()");
        AssertContains(diagnostics.LifecycleText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertDoesNotContain(diagnostics.HubText, "public void Start()");
        AssertContains(diagnostics.HdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.HdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertDoesNotContain(diagnostics.HubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.SnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnostics.SnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotProjectionText, "private AutomationSnapshot BuildAutomationSnapshot(");
        AssertContains(diagnostics.SnapshotProjectionText, "return BuildAutomationSnapshotFromProjections(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "private static AutomationSnapshot BuildAutomationSnapshotFromProjections(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "new AutomationSnapshot");
    }
}
