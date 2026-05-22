using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(snapshotFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(snapshotFlatteningText, "VerificationInProgress = snapshotStatusFlattening.VerificationInProgress,");
        AssertContains(snapshotFlatteningText, "SessionState = snapshotStatusFlattening.SessionState,");
        AssertContains(snapshotFlatteningText, "StatusText = snapshotStatusFlattening.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotFlatteningText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotFlatteningText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = viewModelSnapshot.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = snapshotStatus.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");
        AssertContains(snapshotStatusProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotStatusProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = snapshotStatus.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(snapshotFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(snapshotFlatteningText, "DiagnosticHealthStatus = snapshotEvaluationFlattening.DiagnosticHealthStatus,");
        AssertContains(snapshotFlatteningText, "PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluationFlattening.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotFlatteningText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");

        AssertContains(snapshotEvaluationProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationFlattenedProjection");

        AssertContains(snapshotEvaluationProjectionText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = performance.Score,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticAudioLane = diagnostic.AudioLane,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationProjection");

        return Task.CompletedTask;
    }

}
