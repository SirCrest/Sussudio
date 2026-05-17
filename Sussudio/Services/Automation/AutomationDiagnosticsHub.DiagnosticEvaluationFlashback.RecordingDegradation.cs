namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
