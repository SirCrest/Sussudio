using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
