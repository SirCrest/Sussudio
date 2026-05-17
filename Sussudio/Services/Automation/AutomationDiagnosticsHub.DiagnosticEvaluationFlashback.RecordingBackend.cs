namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
