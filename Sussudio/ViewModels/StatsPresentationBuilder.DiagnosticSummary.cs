using Sussudio.Models;
using Sussudio.Services.Automation;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool isRecording,
        double sourceDropPercent,
        double previewSlowPercent,
        long rendererSubmitted,
        long rendererDrops,
        int presentSampleCount)
    {
        if (!isPreviewing && !isRecording)
        {
            return new StatsDiagnosticSummary(
                HealthStatus: "Idle",
                LikelyStage: "diagnostic_unavailable",
                Evidence: "Start preview or recording to collect live frame-lane diagnostics.");
        }

        var sourceEvidence =
            $"source target={FormatFrameBudgetMs(health.ExpectedFrameRate)} avg={Sanitize(health.CaptureCadenceAverageIntervalMs):0.##}ms p95={Sanitize(health.CaptureCadenceP95IntervalMs):0.##}ms p99={Sanitize(health.CaptureCadenceP99IntervalMs):0.##}ms max={Sanitize(health.CaptureCadenceMaxIntervalMs):0.##}ms rate={Sanitize(health.CaptureCadenceObservedFps):0.##}/{Sanitize(health.ExpectedFrameRate):0.##}fps 1pctLow={Sanitize(health.CaptureCadenceOnePercentLowFps):0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({sourceDropPercent:0.###}%)";

        if (health.CaptureCadenceSampleCount < 30 || (isPreviewing && presentSampleCount == 0))
        {
            return new StatsDiagnosticSummary("WarmingUp", "diagnostic_unavailable", sourceEvidence);
        }

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            sourceDropPercent > 0.1)
        {
            return new StatsDiagnosticSummary("Warning", "source_capture", sourceEvidence);
        }

        if (health.MjpegDecodeFailures > 0 ||
            health.MjpegEmitFailures > 0 ||
            health.MjpegCompressedDropsQueueFull > 0 ||
            health.MjpegTotalDropped > 0)
        {
            var decodeEvidence =
                $"decode p95={Sanitize(health.MjpegDecodeP95Ms):0.##}ms callbackP95={Sanitize(health.MjpegCallbackP95Ms):0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures}";
            return new StatsDiagnosticSummary("Warning", "mjpeg_decode", decodeEvidence);
        }

        var previewQueueBelowTarget =
            health.MjpegPreviewJitterQueueDepth < health.MjpegPreviewJitterTargetDepth;
        if (previewQueueBelowTarget &&
            (health.MjpegPreviewJitterDeadlineDropCount > 0 ||
            health.MjpegPreviewJitterUnderflowCount > 3))
        {
            var previewEvidence =
                $"scheduler target={health.MjpegPreviewJitterTargetDepth} depth={health.MjpegPreviewJitterQueueDepth}/{health.MjpegPreviewJitterMaxDepth} deadlineDrops={health.MjpegPreviewJitterDeadlineDropCount} underflows={health.MjpegPreviewJitterUnderflowCount} resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount}";
            return new StatsDiagnosticSummary("Warning", "preview_scheduler", previewEvidence);
        }

        var rendererDropPercent = DiagnosticThresholds.CalculatePercent(rendererDrops, rendererSubmitted);
        if ((rendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples && rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||
            previewSlowPercent > 1.0)
        {
            var renderEvidence =
                $"render drops={rendererDrops} ({rendererDropPercent:0.###}%) slow={previewSlowPercent:0.##}%";
            return new StatsDiagnosticSummary("Warning", "renderer", renderEvidence);
        }

        return new StatsDiagnosticSummary("Healthy", "none", "All monitored frame lanes are within current thresholds.");
    }
}
