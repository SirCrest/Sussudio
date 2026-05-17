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

    private static DiagnosticEvaluation? TryBuildFlashbackEncoderFailureDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        if (!health.FlashbackEncodingFailed)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Critical",
            "flashback_recording",
            "Flashback encoder has failed.",
            lanes.FlashbackRecording,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackExportRotationDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.ExportRotationGap)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_export",
            "Flashback export rotation skipped live-edge frames.",
            lanes.FlashbackRecording,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackBackendSettingsDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.BackendSettingsUnexpectedlyStale)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_recording",
            "Flashback backend settings differ from requested settings.",
            lanes.FlashbackRecording,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.RecordingDegraded)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_recording",
            "Flashback recording path is dropping or backing up.",
            lanes.FlashbackRecording,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
