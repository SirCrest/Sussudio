using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class FramePacingVerdictTools
{
    private static double ResolveTargetFps(JsonElement snapshot, double targetFpsOverride)
    {
        if (targetFpsOverride > 0)
        {
            return targetFpsOverride;
        }

        return new[]
            {
                AutomationSnapshotFormatter.GetDouble(snapshot, "ExpectedCaptureFrameRate"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "SourceFrameRateExact"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackPlaybackTargetFps"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "EncoderFrameRate")
            }
            .Where(value => double.IsFinite(value) && value > 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static bool IsSampleReady(FramePacingChannel channel, double minSampleMs, double targetFps)
    {
        if (minSampleMs > 0 && channel.SampleDurationMs < minSampleMs)
        {
            return false;
        }

        var minSamples = targetFps >= 100 ? targetFps * 10 : 60;
        return channel.SampleCount >= minSamples;
    }

    private static bool IsHalfRate(double targetFps, double observedFps, double fivePercentLowFps, IReadOnlyList<double> intervalsMs)
    {
        if (targetFps < 100)
        {
            return false;
        }

        var fps = fivePercentLowFps > 0 ? Math.Min(observedFps > 0 ? observedFps : fivePercentLowFps, fivePercentLowFps) : observedFps;
        var ratio = Ratio(fps, targetFps);
        if (ratio >= 0.45 && ratio <= 0.62)
        {
            return true;
        }

        var targetFrameMs = 1000.0 / targetFps;
        return HasHalfRateIntervals(intervalsMs, targetFrameMs);
    }

    private static bool HasHalfRateIntervals(IReadOnlyList<double> intervalsMs, double targetFrameMs)
    {
        if (intervalsMs.Count < 6 || targetFrameMs <= 0)
        {
            return false;
        }

        var halfRateFrameMs = targetFrameMs * 2;
        var halfRateCount = intervalsMs.Count(value => value >= halfRateFrameMs * 0.80 && value <= halfRateFrameMs * 1.20);
        return halfRateCount >= Math.Max(3, intervalsMs.Count / 3);
    }

    private static bool IsHiddenStutter(double targetFps, FramePacingChannel channel)
    {
        if (targetFps <= 0 || channel.ObservedFps < targetFps * 0.92)
        {
            return false;
        }

        return channel.FivePercentLowFps > 0 && channel.FivePercentLowFps < targetFps * 0.90 ||
               channel.OnePercentLowFps > 0 && channel.OnePercentLowFps < targetFps * 0.85;
    }

    private static string ResolveVerdict(bool sampleReady, bool previewHalfRate, bool playbackHalfRate, bool hiddenStutter)
    {
        if (!sampleReady)
        {
            return "InsufficientSample";
        }

        if (previewHalfRate && playbackHalfRate)
        {
            return "HalfRatePreviewAndPlaybackSuspected";
        }

        if (previewHalfRate)
        {
            return "HalfRatePreviewSuspected";
        }

        if (playbackHalfRate)
        {
            return "HalfRatePlaybackSuspected";
        }

        return hiddenStutter ? "HiddenStutterSuspected" : "FramePacingLooksGood";
    }

    private static long NonNegativeDelta(long latest, long first)
        => latest >= first ? latest - first : 0;

    private static double Ratio(double value, double target)
        => target > 0 && double.IsFinite(value) ? value / target : 0;

    private static string FormatBool(bool value)
        => value ? "true" : "false";
}
