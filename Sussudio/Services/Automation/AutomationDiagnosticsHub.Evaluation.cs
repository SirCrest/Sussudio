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

    private static string FormatPreviewSlowFrameAlertDetail(AutomationSnapshot snapshot)
    {
        if (snapshot.PreviewD3DRecentSlowFrames.Length <= 0)
        {
            return string.Empty;
        }

        var frame = snapshot.PreviewD3DRecentSlowFrames[^1];
        var reason = string.IsNullOrWhiteSpace(frame.SlowReason) ? "unknown" : frame.SlowReason;
        return $" latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms interval={frame.PresentIntervalMs:0.##}ms inputUpload={frame.InputUploadCpuMs:0.##}ms renderSubmit={frame.RenderSubmitCpuMs:0.##}ms total={frame.TotalFrameCpuMs:0.##}ms presentCall={frame.PresentCallMs:0.##}ms pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}";
    }

    private static string FormatVisualCadenceAlertDetail(AutomationSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSampleCount <= 0)
        {
            return string.Empty;
        }

        return $" visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps visualOutput={snapshot.VisualCadenceOutputObservedFps:0.##}fps repeat={snapshot.VisualCadenceRepeatFramePercent:0.###}% longestRepeatRun={snapshot.VisualCadenceLongestRepeatRun} confidence={snapshot.VisualCadenceMotionConfidence}";
    }

    private static string FormatMjpegDuplicateCadenceDetail(CaptureHealthSnapshot health)
        =>
            $"mjpg fingerprint samples={health.MjpegPacketHashSampleCount} input={health.MjpegPacketHashInputObservedFps:0.##}fps unique={health.MjpegPacketHashUniqueObservedFps:0.##}fps dup={health.MjpegPacketHashDuplicateFramePercent:0.###}% pattern={health.MjpegPacketHashPattern} longestDup={health.MjpegPacketHashLongestDuplicateRun}";

    private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)
    {
        if (health.EncoderFrameRateNumerator is int numerator &&
            health.EncoderFrameRateDenominator is int denominator &&
            denominator > 0)
        {
            return $"{health.EncoderFrameRate:0.##}fps({numerator}/{denominator})";
        }

        return $"{health.EncoderFrameRate:0.##}fps";
    }

    private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)
        => flashbackPlaybackTargetFps > 0 ? flashbackPlaybackTargetFps : fallbackFrameRate;

    private static bool IsFlashbackPlaybackFrametimeDegraded(
        string state,
        double targetFrameRate,
        long frameCount,
        int cadenceSampleCount,
        double onePercentLowFps)
        =>
            string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) &&
            targetFrameRate > 0 &&
            frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames &&
            cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames &&
            onePercentLowFps > 0 &&
            onePercentLowFps < targetFrameRate * FlashbackPlaybackOnePercentLowWarningRatio;

    private static bool IsCaptureOnePercentLowDegraded(
        double targetFrameRate,
        int cadenceSampleCount,
        double onePercentLowFps)
        =>
            targetFrameRate > 0 &&
            cadenceSampleCount >= CapturePerfectionMinSamples &&
            onePercentLowFps > 0 &&
            onePercentLowFps < targetFrameRate * CaptureOnePercentLowWarningRatio;

    private static bool IsPreviewOnePercentLowDegraded(
        double expectedIntervalMs,
        int cadenceSampleCount,
        double onePercentLowFps)
    {
        if (expectedIntervalMs <= 0 ||
            cadenceSampleCount < PreviewPerfectionMinSamples ||
            onePercentLowFps <= 0)
        {
            return false;
        }

        var targetFrameRate = 1000.0 / expectedIntervalMs;
        return onePercentLowFps < targetFrameRate * PreviewOnePercentLowWarningRatio;
    }

    private static bool IsVisualCadenceHealthy(
        double targetFrameRate,
        int sampleCount,
        double changeObservedFps,
        double repeatFramePercent,
        long longestRepeatRun)
        =>
            targetFrameRate > 0 &&
            sampleCount >= PreviewPerfectionMinSamples &&
            changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio &&
            repeatFramePercent <= 1.0 &&
            longestRepeatRun <= 1;

    private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)
    {
        if (health.ExpectedFrameRate < 90 ||
            health.MjpegPacketHashSampleCount < PreviewPerfectionMinSamples ||
            health.MjpegPacketHashInputObservedFps < health.ExpectedFrameRate * 0.90 ||
            health.MjpegPacketHashDuplicateFramePercent < 20.0)
        {
            return false;
        }

        var uniqueCadenceBelowTarget =
            health.MjpegPacketHashUniqueObservedFps > 0 &&
            health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75;
        var visualCadenceBelowTarget =
            health.VisualCadenceSampleCount >= PreviewPerfectionMinSamples &&
            health.VisualCadenceChangeObservedFps > 0 &&
            health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75 &&
            health.VisualCadenceRepeatFramePercent >= 20.0;
        var telemetryBelowTarget =
            health.SourceFrameRateExact is > 0 &&
            health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75;

        return uniqueCadenceBelowTarget || visualCadenceBelowTarget || telemetryBelowTarget;
    }

    private static bool IsFlashbackRecordingQueueBackedUp(
        int queueDepth,
        int queueCapacity,
        long oldestFrameAgeMs)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio) &&
            oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs;

    private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio);

    private static bool IsFlashbackForceRotateRejectReason(string? reason)
        =>
            string.Equals(reason, "force_rotate_draining", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reason, "force_rotate_queue_guard", StringComparison.OrdinalIgnoreCase);
}
