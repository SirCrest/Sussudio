using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private AutomationSnapshot BuildAutomationSnapshot(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health,
        RecordingStats recordingStats,
        PreviewRuntimeSnapshot previewRuntime,
        PerformanceEvaluation performance,
        DiagnosticEvaluation diagnostic,
        PreviewPacingClassification previewPacingClassification,
        PreviewHdrState previewHdrState,
        AudioSignalState audioSignal,
        bool recordingFileGrowing,
        HdrTruthVerdict hdrTruthVerdict,
        LastOutputProbe lastOutput,
        ProcessResourceSnapshot processResources,
        RecordingVerificationResult? lastVerification,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var projections = BuildAutomationSnapshotProjectionSet(
            viewModelSnapshot,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            performance,
            diagnostic,
            previewPacingClassification,
            previewHdrState,
            audioSignal,
            recordingFileGrowing,
            hdrTruthVerdict,
            lastOutput,
            processResources,
            lastVerification,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return BuildAutomationSnapshotFromProjections(projections);
    }
}
