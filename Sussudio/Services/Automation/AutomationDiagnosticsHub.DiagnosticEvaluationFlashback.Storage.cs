using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackStorageDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        var flashbackTempPressure =
            health.FlashbackActive &&
            (health.FlashbackStartupCacheOverBudget ||
             (health.FlashbackTempDriveFreeBytes >= 0 && health.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes));

        if (!flashbackTempPressure)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_storage",
            "Flashback temp storage is under pressure.",
            lanes.TempCache,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
