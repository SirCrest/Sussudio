namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
