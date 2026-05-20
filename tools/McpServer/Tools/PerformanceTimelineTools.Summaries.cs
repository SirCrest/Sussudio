using System.Globalization;
using System.Text;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void AppendOnePercentLowTargetSummary(
        StringBuilder builder,
        IReadOnlyList<TimelineRow> entries,
        double targetOnePercentLowFps,
        double targetFrameBudgetMs)
    {
        AppendChannelTargetSummary(
            builder,
            "Preview",
            entries,
            static row => row.PreviewOnePercentLowFps,
            static row => row.PreviewP99Ms,
            static row => row.PreviewP95Ms,
            static row => FormatD3DP99Bottleneck(row),
            targetOnePercentLowFps,
            targetFrameBudgetMs);
        AppendChannelTargetSummary(
            builder,
            "Capture",
            entries,
            static row => row.CaptureOnePercentLowFps,
            static row => row.CaptureP95Ms,
            static row => row.CaptureP99Ms,
            static row => "capture",
            targetOnePercentLowFps,
            targetFrameBudgetMs);
        AppendChannelTargetSummary(
            builder,
            "Flashback",
            entries,
            static row => row.FlashbackPlaybackOnePercentLowFps,
            static row => row.FlashbackPlaybackP99FrameMs,
            static row => row.FlashbackPlaybackDecodeP99Ms,
            static row => FormatFlashbackStageCell(row),
            targetOnePercentLowFps,
            targetFrameBudgetMs);
    }

    private static void AppendChannelTargetSummary(
        StringBuilder builder,
        string label,
        IReadOnlyList<TimelineRow> entries,
        Func<TimelineRow, double> onePercentLowSelector,
        Func<TimelineRow, double> primaryMsSelector,
        Func<TimelineRow, double> secondaryMsSelector,
        Func<TimelineRow, string> clueSelector,
        double targetOnePercentLowFps,
        double targetFrameBudgetMs)
    {
        var valid = entries
            .Where(row => IsPositiveFinite(onePercentLowSelector(row)))
            .ToArray();
        if (valid.Length == 0)
        {
            builder.AppendLine($"{label}: no 1% low samples yet.");
            return;
        }

        var belowTarget = valid.Count(row => onePercentLowSelector(row) < targetOnePercentLowFps);
        var worst = valid.OrderBy(onePercentLowSelector).First();
        var latest = valid[^1];
        var targetMissPercent = belowTarget * 100.0 / valid.Length;
        var latestPrimaryOverBudgetMs = targetFrameBudgetMs > 0
            ? Math.Max(0, primaryMsSelector(latest) - targetFrameBudgetMs)
            : 0;

        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: latest={onePercentLowSelector(latest):0.##}fps worst={onePercentLowSelector(worst):0.##}fps misses={belowTarget}/{valid.Length} ({targetMissPercent:0.#}%) latestPrimary={primaryMsSelector(latest):0.##}ms overBudget={latestPrimaryOverBudgetMs:0.##}ms secondary={secondaryMsSelector(latest):0.##}ms clue={clueSelector(latest)} worstAt={worst.Timestamp}"));
    }

    private static long NonNegativeDelta(long latest, long first)
        => latest >= first ? latest - first : 0;

    private static bool IsPositiveFinite(double value)
        => double.IsFinite(value) && value > 0;
}
