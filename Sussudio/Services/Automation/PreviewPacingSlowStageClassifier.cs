using System;
using System.Globalization;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
    private const double MinSampleDurationMs = 30_000.0;
    private const double OnePercentLowWarningRatio = 0.98;
    private const double P99OverBudgetRatio = 1.08;
    private const double RendererDropWarningPercent = 1.0;
    private const double VisualRepeatWarningPercent = 8.0;
    private const double MjpegDuplicateWarningPercent = 20.0;

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

    private static string Format(string format, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, format, args);
}
