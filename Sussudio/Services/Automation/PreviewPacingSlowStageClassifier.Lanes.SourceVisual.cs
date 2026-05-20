using System;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
    private static bool TryClassifySourceCapture(
        PreviewPacingClassificationInput input,
        bool sourceSampleReady,
        double targetFps,
        out PreviewPacingClassification classification)
    {
        if (IsSourceCaptureSuspect(input, sourceSampleReady, targetFps))
        {
            classification = new PreviewPacingClassification(
                "SourceCapture",
                input.CaptureCadenceEstimatedDroppedFrames > 0 || input.CaptureCadenceSevereGapCount > 0 ? "High" : "Medium",
                Format(
                    "capture1pct={0:0.##}fps p99={1:0.##}ms drops={2} gaps={3} dropPct={4:0.###} preview1pct={5:0.##}fps.",
                    input.CaptureCadenceOnePercentLowFps,
                    input.CaptureCadenceP99IntervalMs,
                    input.CaptureCadenceEstimatedDroppedFrames,
                    input.CaptureCadenceSevereGapCount,
                    input.CaptureCadenceEstimatedDropPercent,
                    input.PreviewCadenceOnePercentLowFps));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyVisualDuplicateOrLowMotion(
        PreviewPacingClassificationInput input,
        double targetFps,
        out PreviewPacingClassification classification)
    {
        if (IsVisualDuplicateOrLowMotionSuspect(input, targetFps))
        {
            classification = new PreviewPacingClassification(
                "VisualDuplicateOrLowMotion",
                "Medium",
                Format(
                    "visualChange={0:0.##}fps repeat={1:0.###}% longestRun={2} confidence={3}; mjpgInput={4:0.##}fps unique={5:0.##}fps dup={6:0.###}%.",
                    input.VisualCadenceChangeObservedFps,
                    input.VisualCadenceRepeatFramePercent,
                    input.VisualCadenceLongestRepeatRun,
                    string.IsNullOrWhiteSpace(input.VisualCadenceMotionConfidence) ? "Unknown" : input.VisualCadenceMotionConfidence,
                    input.MjpegPacketHashInputObservedFps,
                    input.MjpegPacketHashUniqueObservedFps,
                    input.MjpegPacketHashDuplicateFramePercent));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool IsSourceCaptureSuspect(
        PreviewPacingClassificationInput input,
        bool sourceSampleReady,
        double targetFps)
    {
        if (input.CaptureCadenceEstimatedDroppedFrames > 0 ||
            input.CaptureCadenceSevereGapCount > 0 ||
            input.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return true;
        }

        var sourceTarget = input.CaptureExpectedFrameRate > 0 ? input.CaptureExpectedFrameRate : targetFps;
        return sourceSampleReady && IsOnePercentLowDegraded(input.CaptureCadenceOnePercentLowFps, sourceTarget);
    }

    private static bool IsVisualDuplicateOrLowMotionSuspect(
        PreviewPacingClassificationInput input,
        double targetFps)
    {
        var visualReady = input.VisualCadenceSampleCount >= Math.Max(60, (int)Math.Round(targetFps));
        if (visualReady &&
            input.VisualCadenceChangeObservedFps > 0 &&
            input.VisualCadenceChangeObservedFps < targetFps * 0.90 &&
            (input.VisualCadenceRepeatFramePercent >= VisualRepeatWarningPercent ||
             input.VisualCadenceLongestRepeatRun > 2))
        {
            return true;
        }

        return input.MjpegPacketHashSampleCount >= Math.Max(60, (int)Math.Round(targetFps)) &&
               input.MjpegPacketHashInputObservedFps >= targetFps * 0.90 &&
               input.MjpegPacketHashUniqueObservedFps > 0 &&
               input.MjpegPacketHashUniqueObservedFps < targetFps * 0.90 &&
               input.MjpegPacketHashDuplicateFramePercent >= MjpegDuplicateWarningPercent;
    }
}
