using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

internal readonly record struct PerformanceEvaluation(double Score, bool PerfectionMet, string Summary);

public sealed partial class AutomationDiagnosticsHub
{
    private PerformanceEvaluation EvaluatePerformance(
        bool isPreviewing,
        bool isRecording,
        bool recordingFileGrowing,
        bool previewGpuActive,
        bool previewBlankSuspected,
        bool previewStalled,
        int previewCadenceSampleCount,
        double previewCadenceSlowFramePercent,
        int captureCadenceSampleCount,
        double captureCadenceExpectedIntervalMs,
        double captureCadenceP95IntervalMs,
        double captureCadenceExpectedFrameRate,
        double captureCadenceOnePercentLowFps,
        double previewCadenceExpectedIntervalMs,
        double previewCadenceOnePercentLowFps,
        bool visualCadenceHealthy,
        double captureCadenceDropPercent,
        RecordingVerificationResult? lastVerification)
    {
        var reasons = new List<string>();
        var penalty = 0.0;

        if (previewBlankSuspected || previewStalled)
        {
            penalty += 40;
            reasons.Add("preview health degraded (blank/stalled)");
        }

        if (isRecording && !recordingFileGrowing)
        {
            penalty += 25;
            reasons.Add("recording file growth stalled");
        }

        if (captureCadenceSampleCount >= CapturePerfectionMinSamples)
        {
            if (captureCadenceDropPercent > _perfectionCaptureDropPercentThreshold)
            {
                var over = captureCadenceDropPercent - _perfectionCaptureDropPercentThreshold;
                penalty += Math.Min(35, over * 6.0);
                reasons.Add($"capture drop {captureCadenceDropPercent:0.###}%");
            }

            if (captureCadenceExpectedIntervalMs > 0 && captureCadenceP95IntervalMs > 0)
            {
                var p95Ratio = captureCadenceP95IntervalMs / captureCadenceExpectedIntervalMs;
                if (p95Ratio > _perfectionCaptureP95MultiplierThreshold)
                {
                    penalty += Math.Min(25, (p95Ratio - _perfectionCaptureP95MultiplierThreshold) * 45.0);
                    reasons.Add($"capture p95 ratio {p95Ratio:0.###}x");
                }
            }

            if (IsCaptureOnePercentLowDegraded(
                    captureCadenceExpectedFrameRate,
                    captureCadenceSampleCount,
                    captureCadenceOnePercentLowFps))
            {
                var target = captureCadenceExpectedFrameRate * CaptureOnePercentLowWarningRatio;
                var deficit = Math.Max(0.0, target - captureCadenceOnePercentLowFps);
                penalty += Math.Min(25, deficit * 1.5);
                reasons.Add($"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps");
            }
        }
        else if (isRecording)
        {
            penalty += 5;
            reasons.Add("capture cadence samples insufficient");
        }

        if (isPreviewing && !previewGpuActive && previewCadenceSampleCount >= PreviewPerfectionMinSamples)
        {
            if (previewCadenceSlowFramePercent > _perfectionPreviewSlowPercentThreshold)
            {
                var over = previewCadenceSlowFramePercent - _perfectionPreviewSlowPercentThreshold;
                penalty += Math.Min(20, over * 2.0);
                reasons.Add($"preview slow frames {previewCadenceSlowFramePercent:0.###}%");
            }
        }

        if (isPreviewing &&
            !visualCadenceHealthy &&
            IsPreviewOnePercentLowDegraded(
                previewCadenceExpectedIntervalMs,
                previewCadenceSampleCount,
                previewCadenceOnePercentLowFps))
        {
            var target = 1000.0 / previewCadenceExpectedIntervalMs * PreviewOnePercentLowWarningRatio;
            var deficit = Math.Max(0.0, target - previewCadenceOnePercentLowFps);
            penalty += Math.Min(20, deficit * 1.25);
            reasons.Add($"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps");
        }

        if (lastVerification is { CadenceSampleCount: >= VerificationPerfectionMinSamples } verification &&
            verification.CadenceEstimatedDropPercent.GetValueOrDefault() > _perfectionVerificationDropPercentThreshold)
        {
            var verifyDrop = verification.CadenceEstimatedDropPercent.GetValueOrDefault();
            var over = verifyDrop - _perfectionVerificationDropPercentThreshold;
            penalty += Math.Min(25, over * 4.0);
            reasons.Add($"file cadence drop {verifyDrop:0.###}%");
        }

        if (lastVerification != null && !lastVerification.Succeeded)
        {
            penalty += 20;
            reasons.Add("verification failed");
        }

        var score = Math.Clamp(100.0 - penalty, 0.0, 100.0);
        var perfectionMet = reasons.Count == 0 && score >= 99.0;
        var summary = reasons.Count == 0
            ? "Perfection thresholds satisfied."
            : string.Join(", ", reasons.Take(4));

        return new PerformanceEvaluation(score, perfectionMet, summary);
    }
}
