using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotProjectionText, "StatusText = snapshotStatus.StatusText,");
        AssertDoesNotContain(snapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotProjectionText, "StatusText = viewModelSnapshot.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotProjectionText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");

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
