using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackRecordingDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording,
        DiagnosticEvaluationLanes lanes)
    {
        var conditions = BuildFlashbackRecordingDiagnosticConditions(
            health,
            isRecording,
            recentFlashbackRecording);

        return TryBuildFlashbackEncoderFailureDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackExportRotationDiagnosticEvaluation(conditions, lanes) ??
               TryBuildFlashbackBackendSettingsDiagnosticEvaluation(conditions, lanes) ??
               TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(conditions, lanes);
    }
}
