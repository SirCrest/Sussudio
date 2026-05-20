namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(
        SnapshotEvaluationProjection snapshotEvaluation)
        => new()
        {
            PerformanceScore = snapshotEvaluation.PerformanceScore,
            PerformancePerfectionMet = snapshotEvaluation.PerformancePerfectionMet,
            PerformanceSummary = snapshotEvaluation.PerformanceSummary,
            DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,
            DiagnosticLikelyStage = snapshotEvaluation.DiagnosticLikelyStage,
            DiagnosticSummary = snapshotEvaluation.DiagnosticSummary,
            DiagnosticEvidence = snapshotEvaluation.DiagnosticEvidence,
            DiagnosticSourceLane = snapshotEvaluation.DiagnosticSourceLane,
            DiagnosticDecodeLane = snapshotEvaluation.DiagnosticDecodeLane,
            DiagnosticPreviewLane = snapshotEvaluation.DiagnosticPreviewLane,
            DiagnosticRenderLane = snapshotEvaluation.DiagnosticRenderLane,
            DiagnosticPresentLane = snapshotEvaluation.DiagnosticPresentLane,
            DiagnosticRecordingLane = snapshotEvaluation.DiagnosticRecordingLane,
            DiagnosticAudioLane = snapshotEvaluation.DiagnosticAudioLane,
            PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,
            PreviewPacingSlowStageConfidence = snapshotEvaluation.PreviewPacingSlowStageConfidence,
            PreviewPacingSlowStageEvidence = snapshotEvaluation.PreviewPacingSlowStageEvidence,
            PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,
            PerformanceThresholdCaptureP95Multiplier = snapshotEvaluation.PerformanceThresholdCaptureP95Multiplier,
            PerformanceThresholdPreviewSlowPercent = snapshotEvaluation.PerformanceThresholdPreviewSlowPercent,
            PerformanceThresholdVerificationDropPercent = snapshotEvaluation.PerformanceThresholdVerificationDropPercent
        };

    private readonly record struct SnapshotEvaluationFlattenedProjection
    {
        public double PerformanceScore { get; init; }
        public bool PerformancePerfectionMet { get; init; }
        public string PerformanceSummary { get; init; }
        public string DiagnosticHealthStatus { get; init; }
        public string DiagnosticLikelyStage { get; init; }
        public string DiagnosticSummary { get; init; }
        public string DiagnosticEvidence { get; init; }
        public string DiagnosticSourceLane { get; init; }
        public string DiagnosticDecodeLane { get; init; }
        public string DiagnosticPreviewLane { get; init; }
        public string DiagnosticRenderLane { get; init; }
        public string DiagnosticPresentLane { get; init; }
        public string DiagnosticRecordingLane { get; init; }
        public string DiagnosticAudioLane { get; init; }
        public string PreviewPacingLikelySlowStage { get; init; }
        public string PreviewPacingSlowStageConfidence { get; init; }
        public string PreviewPacingSlowStageEvidence { get; init; }
        public double PerformanceThresholdCaptureDropPercent { get; init; }
        public double PerformanceThresholdCaptureP95Multiplier { get; init; }
        public double PerformanceThresholdPreviewSlowPercent { get; init; }
        public double PerformanceThresholdVerificationDropPercent { get; init; }
    }
}
