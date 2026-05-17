using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackExportDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        if (!health.FlashbackExportActive)
        {
            return null;
        }

        var exportLastProgressAgeMs = Math.Max(0, health.FlashbackExportLastProgressAgeMs);
        if (exportLastProgressAgeMs >= FlashbackExportStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_export",
                "Flashback export progress is stalled.",
                $"{lanes.Export} progressAgeMs={exportLastProgressAgeMs}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        return new DiagnosticEvaluation(
            "Busy",
            "flashback_export",
            "Flashback export is running.",
            lanes.Export,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
