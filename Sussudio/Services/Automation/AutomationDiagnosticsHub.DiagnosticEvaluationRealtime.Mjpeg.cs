using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimeMjpegDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg,
        DiagnosticEvaluationLanes lanes)
    {
        var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);

        if (mjpegDuplicateCadenceDetected)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_signal",
                "Captured HFR MJPEG cadence contains repeated source frames.",
                $"{lanes.MjpegDuplicate} | {lanes.Visual} | {lanes.SourceSignal}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (recentMjpeg.DecodeFailures <= 0 &&
            recentMjpeg.EmitFailures <= 0 &&
            recentMjpeg.CompressedQueueDrops <= 0 &&
            recentMjpeg.TotalDropped <= 0)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "mjpeg_decode",
            "MJPEG decode/reorder is dropping or failing frames.",
            lanes.Decode,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
