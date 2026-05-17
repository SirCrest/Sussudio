using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording,
        DiagnosticEvaluationLanes lanes,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        bool playbackCommandFailedRecently)
    {
        return TryBuildFlashbackStorageDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackRecordingDiagnosticEvaluation(health, isRecording, recentFlashbackRecording, lanes) ??
               TryBuildFlashbackExportDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackPlaybackDiagnosticEvaluation(
                   health,
                   lanes,
                   playbackTargetFps,
                   playbackCommandQueueAgeMs,
                   playbackCommandFailedRecently);
    }
}
