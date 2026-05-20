using System.Text;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
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
