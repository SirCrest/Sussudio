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

    private static void AppendPressureSummary(
        StringBuilder builder,
        IReadOnlyList<TimelineRow> entries,
        double targetFrameBudgetMs)
    {
        if (entries.Count == 0 || targetFrameBudgetMs <= 0)
        {
            return;
        }

        var first = entries[0];
        var last = entries[^1];
        builder.AppendLine();
        builder.AppendLine("== Pressure Summary ==");
        builder.AppendLine(
            "Preview Pressure: " +
            $"overBudgetSamples input={CountOverBudget(entries, static row => row.PreviewD3DInputUploadP99Ms, targetFrameBudgetMs)} " +
            $"render={CountOverBudget(entries, static row => row.PreviewD3DRenderSubmitP99Ms, targetFrameBudgetMs)} " +
            $"present={CountOverBudget(entries, static row => row.PreviewD3DPresentP99Ms, targetFrameBudgetMs)} " +
            $"total={CountOverBudget(entries, static row => row.PreviewD3DTotalP99Ms, targetFrameBudgetMs)} " +
            $"wait={CountOverBudget(entries, static row => row.PreviewD3DFrameLatencyWaitP95Ms, targetFrameBudgetMs)} " +
            $"dxgiMissedSamples={CountWhere(entries, static row => row.PreviewD3DRecentMissed > 0)} " +
            $"dxgiFailureSamples={CountWhere(entries, static row => row.PreviewD3DRecentFailures > 0)} " +
            $"jitterDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterTotalDropped, first.MjpegPreviewJitterTotalDropped)} " +
            $"clearedDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterClearedDropCount, first.MjpegPreviewJitterClearedDropCount)} " +
            $"deadlineDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterDeadlineDropCount, first.MjpegPreviewJitterDeadlineDropCount)} " +
            $"underflowsDelta={NonNegativeDelta(last.MjpegPreviewJitterUnderflowCount, first.MjpegPreviewJitterUnderflowCount)} " +
            $"resumeReprimesDelta={NonNegativeDelta(last.MjpegPreviewJitterResumeReprimeCount, first.MjpegPreviewJitterResumeReprimeCount)} " +
            $"visualChangeLatest={last.VisualCadenceChangeObservedFps:0.##}fps " +
            $"mjpegUniqueLatest={last.MjpegPacketHashUniqueObservedFps:0.##}fps");

        builder.AppendLine(
            "Flashback Pressure: " +
            $"p99OverBudget={CountOverBudget(entries, static row => row.FlashbackPlaybackP99FrameMs, targetFrameBudgetMs)} " +
            $"decodeOverBudget={CountOverBudget(entries, static row => row.FlashbackPlaybackDecodeP99Ms, targetFrameBudgetMs)} " +
            $"pendingCmdSamples={CountWhere(entries, static row => row.FlashbackPlaybackPendingCommands > 0)} " +
            $"cmdDropsDelta={NonNegativeDelta(last.FlashbackPlaybackCommandsDropped, first.FlashbackPlaybackCommandsDropped)} " +
            $"submitFailuresDelta={NonNegativeDelta(last.FlashbackPlaybackSubmitFailures, first.FlashbackPlaybackSubmitFailures)} " +
            $"droppedFramesDelta={NonNegativeDelta(last.FlashbackPlaybackDroppedFrames, first.FlashbackPlaybackDroppedFrames)} " +
            $"writeHeadWaitsDelta={NonNegativeDelta(last.FlashbackPlaybackWriteHeadWaits, first.FlashbackPlaybackWriteHeadWaits)} " +
            $"forceRotateSamples={CountWhere(entries, static row => row.FlashbackForceRotateRequested || row.FlashbackForceRotateDraining)}");

        builder.AppendLine(
            "System Pressure: " +
            $"videoDropsDelta={NonNegativeDelta(last.VidDrops, first.VidDrops)} " +
            $"gcGen2Delta={NonNegativeDelta(last.Gen2, first.Gen2)} " +
            $"gcPauseSamples={CountWhere(entries, static row => row.GcPause > 0)} " +
            $"lowWorkerSamples={CountWhere(entries, static row => row.Workers > 0 && row.Workers < 16)} " +
            $"managedHeapDeltaMb={(last.ManagedMb - first.ManagedMb):+0.0;-0.0;0.0} " +
            $"workingSetDeltaMb={(last.WorkingMb - first.WorkingMb):+0.0;-0.0;0.0}");
    }

    private static int CountOverBudget(
        IReadOnlyList<TimelineRow> entries,
        Func<TimelineRow, double> selector,
        double targetFrameBudgetMs)
        => CountWhere(entries, row => IsPositiveFinite(selector(row)) && selector(row) > targetFrameBudgetMs);

    private static int CountWhere(IReadOnlyList<TimelineRow> entries, Func<TimelineRow, bool> predicate)
    {
        var count = 0;
        foreach (var entry in entries)
        {
            if (predicate(entry))
            {
                count++;
            }
        }

        return count;
    }
}
