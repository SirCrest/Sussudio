using System;
using System.Globalization;

namespace Sussudio.Services.Automation;

public static class PreviewPacingSlowStageClassifier
{
    private const double MinSampleDurationMs = 30_000.0;
    private const double OnePercentLowWarningRatio = 0.98;
    private const double P99OverBudgetRatio = 1.08;
    private const double RendererDropWarningPercent = 1.0;
    private const double VisualRepeatWarningPercent = 8.0;
    private const double MjpegDuplicateWarningPercent = 20.0;
    private const double StageDominanceRatio = 1.15;

    public static PreviewPacingClassification Classify(PreviewPacingClassificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var targetFps = ResolveTargetFps(input);
        var targetFrameMs = ResolveTargetFrameMs(input, targetFps);
        if (!input.IsPreviewing)
        {
            return new PreviewPacingClassification(
                "InsufficientSample",
                "Low",
                "Preview is not active.");
        }

        if (targetFps <= 0 || targetFrameMs <= 0)
        {
            return new PreviewPacingClassification(
                "InsufficientSample",
                "Low",
                "Preview target frame rate is unknown.");
        }

        var previewSampleReady = IsSampleReady(
            input.PreviewCadenceSampleCount,
            input.PreviewCadenceSampleDurationMs,
            targetFps);
        var hardSignal = HasHardPreviewSignal(input);
        if (!previewSampleReady && !hardSignal)
        {
            return new PreviewPacingClassification(
                "InsufficientSample",
                "Low",
                Format(
                    "previewSamples={0} durationMs={1:0.#} requiredDurationMs={2:0.#} target={3:0.##}fps",
                    input.PreviewCadenceSampleCount,
                    input.PreviewCadenceSampleDurationMs,
                    MinSampleDurationMs,
                    targetFps));
        }

        var previewTailWeak =
            previewSampleReady &&
            (IsOnePercentLowDegraded(input.PreviewCadenceOnePercentLowFps, targetFps) ||
             input.PreviewCadenceP99IntervalMs > targetFrameMs * P99OverBudgetRatio);
        if (!previewTailWeak && !hardSignal)
        {
            return new PreviewPacingClassification(
                "Unknown",
                "None",
                Format(
                    "No degraded preview pacing lane; preview1pct={0:0.##}fps p99={1:0.##}ms target={2:0.##}fps/{3:0.###}ms.",
                    input.PreviewCadenceOnePercentLowFps,
                    input.PreviewCadenceP99IntervalMs,
                    targetFps,
                    targetFrameMs));
        }

        var sourceSampleReady = IsSampleReady(
            input.CaptureCadenceSampleCount,
            input.CaptureCadenceSampleDurationMs,
            input.CaptureExpectedFrameRate > 0 ? input.CaptureExpectedFrameRate : targetFps);
        if (TryClassifySourceCapture(input, sourceSampleReady, targetFps, out var sourceCaptureClassification))
        {
            return sourceCaptureClassification;
        }

        if (TryClassifyVisualDuplicateOrLowMotion(input, targetFps, out var visualCadenceClassification))
        {
            return visualCadenceClassification;
        }

        if (TryClassifyMjpegDecode(input, targetFrameMs, out var mjpegDecodeClassification))
        {
            return mjpegDecodeClassification;
        }

        if (TryClassifyPreviewJitterScheduler(input, targetFrameMs, out var previewJitterClassification))
        {
            return previewJitterClassification;
        }

        if (TryClassifyCompositorMiss(input, targetFrameMs, out var compositorMissClassification))
        {
            return compositorMissClassification;
        }

        if (TryClassifyRenderSubmit(input, out var renderSubmitClassification))
        {
            return renderSubmitClassification;
        }

        var dominantStage = ResolveDominantD3DStage(input, targetFrameMs);
        if (!string.IsNullOrWhiteSpace(dominantStage))
        {
            return new PreviewPacingClassification(
                dominantStage,
                "Medium",
                Format(
                    "d3dP99 input={0:0.##}ms render={1:0.##}ms present={2:0.##}ms waitP95={3:0.##}ms total={4:0.##}ms target={5:0.###}ms.",
                    input.PreviewD3DInputUploadCpuP99Ms,
                    input.PreviewD3DRenderSubmitCpuP99Ms,
                    input.PreviewD3DPresentCallP99Ms,
                    input.PreviewD3DFrameLatencyWaitP95Ms,
                    input.PreviewD3DTotalFrameCpuP99Ms,
                    targetFrameMs));
        }

        return new PreviewPacingClassification(
            "Unknown",
            previewSampleReady ? "Low" : "None",
            Format(
                "Preview tail is weak but no source/decode/jitter/render/present lane dominates; preview1pct={0:0.##}fps p99={1:0.##}ms target={2:0.##}fps/{3:0.###}ms.",
                input.PreviewCadenceOnePercentLowFps,
                input.PreviewCadenceP99IntervalMs,
                targetFps,
                targetFrameMs));
    }

    private static bool HasHardPreviewSignal(PreviewPacingClassificationInput input)
        => input.RecentMjpegDropped > 0 ||
           input.RecentMjpegFailures > 0 ||
           input.RecentPreviewJitterDropped > 0 ||
           input.RecentPreviewJitterDeadlineDrops > 0 ||
           input.RecentPreviewJitterUnderflows > 0 ||
           input.RecentRendererDropped > 0 ||
           input.RecentD3DMissedRefreshes > 0 ||
           input.RecentD3DStatsFailures > 0 ||
           input.RecentD3DFrameLatencyWaitTimeoutCount > 0;

    private static bool IsSampleReady(int sampleCount, double sampleDurationMs, double targetFps)
    {
        var minSamples = targetFps >= 100
            ? Math.Max(120, (int)Math.Round(targetFps * 10.0))
            : 60;
        return sampleCount >= minSamples && sampleDurationMs >= MinSampleDurationMs;
    }

    private static bool IsOnePercentLowDegraded(double onePercentLowFps, double targetFps)
        => targetFps > 0 &&
           double.IsFinite(onePercentLowFps) &&
           onePercentLowFps > 0 &&
           onePercentLowFps < targetFps * OnePercentLowWarningRatio;

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

    private static bool TryClassifyMjpegDecode(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (IsMjpegDecodeSuspect(input, targetFrameMs))
        {
            classification = new PreviewPacingClassification(
                "MjpegDecode",
                input.RecentMjpegDropped > 0 || input.RecentMjpegFailures > 0 ? "High" : "Medium",
                Format(
                    "mjpegDecodeP95={0:0.##}ms pipelineP95={1:0.##}ms pipelineMax={2:0.##}ms recentDropped={3} recentFailures={4}.",
                    input.MjpegDecodeP95Ms,
                    input.MjpegPipelineP95Ms,
                    input.MjpegPipelineMaxMs,
                    input.RecentMjpegDropped,
                    input.RecentMjpegFailures));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyPreviewJitterScheduler(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (IsPreviewJitterSuspect(input, targetFrameMs))
        {
            classification = new PreviewPacingClassification(
                "PreviewJitterScheduler",
                input.RecentPreviewJitterDeadlineDrops > 0 ||
                input.RecentPreviewJitterDropped > 0
                    ? "High"
                    : "Medium",
                Format(
                    "jitter recentDrops={0} deadlineDrops={1} underflows={2} recentScheduleLate={3}/{4:0.##}ms lifetimeScheduleLate={5} maxScheduleLate={6:0.##}ms latencyP95={7:0.##}ms lastDrop={8}.",
                    input.RecentPreviewJitterDropped,
                    input.RecentPreviewJitterDeadlineDrops,
                    input.RecentPreviewJitterUnderflows,
                    input.RecentPreviewJitterScheduleLateCount,
                    input.RecentPreviewJitterScheduleLateMs,
                    input.MjpegPreviewJitterScheduleLateCount,
                    input.MjpegPreviewJitterMaxScheduleLateMs,
                    input.MjpegPreviewJitterLatencyP95Ms,
                    string.IsNullOrWhiteSpace(input.MjpegPreviewJitterLastDropReason) ? "none" : input.MjpegPreviewJitterLastDropReason));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyCompositorMiss(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (input.RecentD3DMissedRefreshes > 0 || input.RecentD3DStatsFailures > 0)
        {
            classification = new PreviewPacingClassification(
                "CompositorMiss",
                "High",
                Format(
                    "dxgiRecentMissed={0} dxgiRecentFailures={1} previewP99={2:0.##}ms target={3:0.###}ms.",
                    input.RecentD3DMissedRefreshes,
                    input.RecentD3DStatsFailures,
                    input.PreviewCadenceP99IntervalMs,
                    targetFrameMs));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyRenderSubmit(
        PreviewPacingClassificationInput input,
        out PreviewPacingClassification classification)
    {
        var rendererDropPercent = CalculatePercent(input.RecentRendererDropped, input.RecentRendererSubmitted);
        if (rendererDropPercent > RendererDropWarningPercent ||
            input.PreviewD3DPendingFrameCount > 1 ||
            input.RecentRendererDropped > 0 && !string.IsNullOrWhiteSpace(input.PreviewD3DLastDropReason))
        {
            classification = new PreviewPacingClassification(
                "RenderSubmit",
                rendererDropPercent > RendererDropWarningPercent ? "High" : "Medium",
                Format(
                    "rendererDropped={0}/{1} ({2:0.###}%) pending={3} lastDrop={4}.",
                    input.RecentRendererDropped,
                    input.RecentRendererSubmitted,
                    rendererDropPercent,
                    input.PreviewD3DPendingFrameCount,
                    string.IsNullOrWhiteSpace(input.PreviewD3DLastDropReason) ? "none" : input.PreviewD3DLastDropReason));
            return true;
        }

        classification = default;
        return false;
    }

    private static string ResolveDominantD3DStage(
        PreviewPacingClassificationInput input,
        double targetFrameMs)
    {
        var threshold = Math.Max(1.0, targetFrameMs * 0.25);
        var inputUpload = Positive(input.PreviewD3DInputUploadCpuP99Ms);
        var renderSubmit = Positive(input.PreviewD3DRenderSubmitCpuP99Ms);
        var presentCall = Positive(input.PreviewD3DPresentCallP99Ms);
        var wait = Math.Max(
            Positive(input.PreviewD3DFrameLatencyWaitP95Ms),
            Positive(input.PreviewD3DFrameLatencyWaitMaxMs) * 0.50);

        if (input.RecentD3DFrameLatencyWaitTimeoutCount > 0)
        {
            return "PresentBlocked";
        }

        var max = Math.Max(Math.Max(inputUpload, renderSubmit), Math.Max(presentCall, wait));
        if (max < threshold)
        {
            if (input.PreviewD3DTotalFrameCpuP99Ms > targetFrameMs * P99OverBudgetRatio)
            {
                return "RenderSubmit";
            }

            return string.Empty;
        }

        if (inputUpload >= max && IsDominant(inputUpload, renderSubmit, presentCall, wait))
        {
            return "RenderUpload";
        }

        if (renderSubmit >= max && IsDominant(renderSubmit, inputUpload, presentCall, wait))
        {
            return "RenderSubmit";
        }

        if (presentCall >= max && IsDominant(presentCall, inputUpload, renderSubmit, wait) ||
            wait >= max && IsDominant(wait, inputUpload, renderSubmit, presentCall))
        {
            return "PresentBlocked";
        }

        return string.Empty;
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

    private static bool IsMjpegDecodeSuspect(PreviewPacingClassificationInput input, double targetFrameMs)
    {
        if (input.RecentMjpegDropped > 0 || input.RecentMjpegFailures > 0)
        {
            return true;
        }

        if (input.MjpegPipelineSampleCount <= 0)
        {
            return false;
        }

        return input.MjpegPipelineP95Ms > targetFrameMs * 0.90 ||
               input.MjpegDecodeP95Ms > targetFrameMs * 0.65 ||
               input.MjpegPipelineMaxMs > targetFrameMs * 1.50;
    }

    private static bool IsPreviewJitterSuspect(PreviewPacingClassificationInput input, double targetFrameMs)
    {
        if (!input.MjpegPreviewJitterEnabled)
        {
            return false;
        }

        if (input.RecentPreviewJitterDropped > 0 ||
            input.RecentPreviewJitterDeadlineDrops > 0 ||
            input.RecentPreviewJitterUnderflows > 3)
        {
            return true;
        }

        return input.RecentPreviewJitterScheduleLateCount > 0 &&
               input.RecentPreviewJitterScheduleLateMs > Math.Max(1.0, targetFrameMs * 0.25);
    }

    private static bool IsDominant(double candidate, params double[] others)
    {
        foreach (var other in others)
        {
            if (other > 0 && candidate < other * StageDominanceRatio)
            {
                return false;
            }
        }

        return true;
    }

    private static double ResolveTargetFps(PreviewPacingClassificationInput input)
    {
        if (IsPositiveFinite(input.TargetFrameRate))
        {
            return input.TargetFrameRate;
        }

        if (IsPositiveFinite(input.CaptureExpectedFrameRate))
        {
            return input.CaptureExpectedFrameRate;
        }

        if (IsPositiveFinite(input.PreviewCadenceExpectedIntervalMs))
        {
            return 1000.0 / input.PreviewCadenceExpectedIntervalMs;
        }

        return 0.0;
    }

    private static double ResolveTargetFrameMs(PreviewPacingClassificationInput input, double targetFps)
    {
        if (IsPositiveFinite(input.PreviewCadenceExpectedIntervalMs))
        {
            return input.PreviewCadenceExpectedIntervalMs;
        }

        return targetFps > 0 ? 1000.0 / targetFps : 0.0;
    }

    private static bool IsPositiveFinite(double value)
        => double.IsFinite(value) && value > 0;

    private static double Positive(double value)
        => double.IsFinite(value) && value > 0 ? value : 0.0;

    private static double CalculatePercent(long count, long total)
        => total > 0 ? Math.Max(0.0, count) * 100.0 / total : 0.0;

    private static string Format(string format, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, format, args);
}
