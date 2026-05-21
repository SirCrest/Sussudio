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
        var snapshotStatusFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotStatus.cs")
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

        AssertContains(snapshotStatusFlatteningText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(snapshotStatusFlatteningText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotStatusFlatteningText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotStatusFlatteningText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotStatusFlatteningText, "StatusText = snapshotStatus.StatusText");
        AssertContains(snapshotStatusFlatteningText, "private readonly record struct SnapshotStatusFlattenedProjection");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs")
            .Replace("\r\n", "\n");
        var snapshotEvaluationFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotEvaluation.cs")
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

        AssertContains(snapshotEvaluationFlatteningText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(snapshotEvaluationFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotEvaluationFlatteningText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotEvaluationFlatteningText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotEvaluationFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertContains(snapshotEvaluationFlatteningText, "private readonly record struct SnapshotEvaluationFlattenedProjection");

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
