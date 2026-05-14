using System;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    private const double VisualRepeatTolerancePercent = 0.25;

    private static StatsMetricStatus ResolveDropStatus(double dropPercent)
        => dropPercent <= 0.01 ? StatsMetricStatus.Good :
           dropPercent <= 0.25 ? StatsMetricStatus.Warning :
           StatsMetricStatus.Bad;

    private static StatsMetricStatus ResolveFpsStatus(double observedFps, double expectedFps)
    {
        if (observedFps <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        if (expectedFps <= 0)
        {
            return StatsMetricStatus.Info;
        }

        var ratio = observedFps / expectedFps;
        return ratio >= 0.985 ? StatsMetricStatus.Good :
               ratio >= 0.95 ? StatsMetricStatus.Warning :
               StatsMetricStatus.Bad;
    }

    private static StatsMetricStatus ResolveFrameLaneStatus(double p95IntervalMs, double expectedFps, double issuePercent)
    {
        if (p95IntervalMs <= 0 && issuePercent <= 0.01)
        {
            return StatsMetricStatus.Neutral;
        }

        var timingStatus = ResolveFrameTimeStatus(p95IntervalMs, expectedFps);
        var issueStatus = ResolveDropStatus(issuePercent);
        return ResolveWorstStatus(timingStatus, issueStatus);
    }

    private static StatsMetricStatus ResolvePreviewFrameLaneStatus(StatsSnapshot snapshot)
    {
        var currentFrameTimeMs = ResolveCurrentPreviewFrameTimeMs(snapshot);
        if (currentFrameTimeMs <= 0 && snapshot.PreviewOnePercentLowFps <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        var timingStatus = ResolveFrameTimeStatus(currentFrameTimeMs, snapshot.SourceExpectedFps);
        var lowFpsStatus = ResolveFpsStatus(snapshot.PreviewOnePercentLowFps, snapshot.SourceExpectedFps);
        return ResolveWorstStatus(timingStatus, lowFpsStatus);
    }

    private static StatsMetricStatus ResolveWorstStatus(StatsMetricStatus first, StatsMetricStatus second)
    {
        if (first == StatsMetricStatus.Bad || second == StatsMetricStatus.Bad)
        {
            return StatsMetricStatus.Bad;
        }

        if (first == StatsMetricStatus.Warning || second == StatsMetricStatus.Warning)
        {
            return StatsMetricStatus.Warning;
        }

        if (first == StatsMetricStatus.Good || second == StatsMetricStatus.Good)
        {
            return StatsMetricStatus.Good;
        }

        return first == StatsMetricStatus.Info || second == StatsMetricStatus.Info
            ? StatsMetricStatus.Info
            : StatsMetricStatus.Neutral;
    }

    private static StatsMetricStatus ResolveFrameTimeStatus(double p95IntervalMs, double expectedFps)
    {
        if (p95IntervalMs <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        expectedFps = Sanitize(expectedFps);
        if (expectedFps <= 0)
        {
            return StatsMetricStatus.Info;
        }

        var budgetMs = 1000.0 / expectedFps;
        return p95IntervalMs <= budgetMs * 1.10 ? StatsMetricStatus.Good :
               p95IntervalMs <= budgetMs * 1.50 ? StatsMetricStatus.Warning :
               StatsMetricStatus.Bad;
    }

    private static StatsMetricStatus ResolveDecodedVisualStatus(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return StatsMetricStatus.Good;
        }

        if (string.Equals(snapshot.VisualCadenceMotionConfidence, "LowMotion", StringComparison.OrdinalIgnoreCase) &&
            snapshot.VisualCadenceChangeFps < snapshot.SourceExpectedFps * 0.95)
        {
            return StatsMetricStatus.Info;
        }

        return ResolveFpsStatus(snapshot.VisualCadenceChangeFps, snapshot.SourceExpectedFps);
    }

    private static StatsMetricStatus ResolveLatencyStatus(double latencyMs)
        => latencyMs <= 0 ? StatsMetricStatus.Neutral :
           latencyMs <= 100 ? StatsMetricStatus.Good :
           latencyMs <= 150 ? StatsMetricStatus.Warning :
           StatsMetricStatus.Bad;

    private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return false;
        }

        var expectedRepeatPercent = GetExpectedVisualRepeatPercent(snapshot);
        var allowedRepeatPercent = expectedRepeatPercent + VisualRepeatTolerancePercent;
        return snapshot.VisualCadenceLongestRepeatRun <= 1 &&
               snapshot.VisualCadenceRepeatPercent <= allowedRepeatPercent;
    }

    private static double GetExpectedVisualRepeatPercent(StatsSnapshot snapshot)
    {
        var sourceFps = Sanitize(snapshot.SourceFrameRateExact ?? snapshot.SourceExpectedFps);
        var outputFps = Sanitize(snapshot.VisualCadenceOutputFps);
        if (sourceFps <= 0 || outputFps <= sourceFps)
        {
            return 0;
        }

        return Math.Clamp((outputFps - sourceFps) / outputFps * 100.0, 0.0, 100.0);
    }
}
