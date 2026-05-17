using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
