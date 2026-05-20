using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesPreviewDiagnosticsMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "PreviewD3DFrameLatencyWaitTimeoutCount",
            "PreviewD3DFrameLatencyWaitP95Ms",
            "PreviewD3DFrameLatencyWaitMaxMs",
            "PreviewD3DFrameStatsRecentMissedRefreshCount",
            "PreviewD3DFrameStatsRecentFailureCount",
            "PreviewD3DRenderThreadFailureCount",
            "PreviewD3DLastRenderThreadFailureType",
            "PreviewD3DLastRenderThreadFailureMessage",
            "PreviewD3DLastRenderThreadFailureHResult",
            "DiagnosticHealthStatus",
            "DiagnosticLikelyStage",
            "DiagnosticSummary",
            "DiagnosticEvidence",
            "DiagnosticSourceLane",
            "DiagnosticDecodeLane",
            "DiagnosticPreviewLane",
            "DiagnosticRenderLane",
            "DiagnosticPresentLane",
            "DiagnosticRecordingLane",
            "DiagnosticAudioLane",
            "PreviewPacingLikelySlowStage",
            "PreviewPacingSlowStageConfidence",
            "PreviewPacingSlowStageEvidence");
    }
}
