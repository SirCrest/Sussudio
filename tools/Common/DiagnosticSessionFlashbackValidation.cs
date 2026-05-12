using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackValidation
{
    internal static void ValidateFlashbackPlaybackSession(
        JsonElement lastSnapshot,
        FlashbackPlaybackSessionMetrics metrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        int durationSeconds,
        List<string> warnings)
    {
        var targetFps = GetDouble(lastSnapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);
        if (frameCount <= 0)
        {
            warnings.Add("flashback playback: no playback frames were observed");
            return;
        }

        if (targetFps > 0 && durationSeconds > 0)
        {
            var minimumExpectedFrames = Math.Max(1, (long)Math.Floor(targetFps * durationSeconds * 0.80));
            if (frameCount < minimumExpectedFrames)
            {
                warnings.Add($"flashback playback: frame count below expected floor frames={frameCount} min={minimumExpectedFrames} targetFps={targetFps:0.##}");
            }

            var minimumOnePercentLow = targetFps * 0.80;
            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
            if (!visualCadenceHealthy &&
                metrics.MinOnePercentLowFpsObserved > 0 &&
                metrics.MinOnePercentLowFpsObserved < minimumOnePercentLow)
            {
                warnings.Add($"flashback playback: 1% low dipped below floor min={metrics.MinOnePercentLowFpsObserved:0.##} floor={minimumOnePercentLow:0.##}");
            }
        }

        if (metrics.DroppedFramesDelta > 0)
        {
            var droppedFrames = GetNullableLong(lastSnapshot, "FlashbackPlaybackDroppedFrames") ?? 0;
            warnings.Add($"flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta} end={droppedFrames}");
        }

        if (metrics.SubmitFailuresDelta > 0)
        {
            var submitFailures = GetNullableLong(lastSnapshot, "FlashbackPlaybackSubmitFailures") ?? 0;
            warnings.Add($"flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta} end={submitFailures}");
        }

        const double maxHealthyAudioBufferedMs = 250.0;
        if (metrics.MaxAudioBufferedDurationMsObserved > maxHealthyAudioBufferedMs)
        {
            warnings.Add($"flashback playback: audio buffered duration exceeded budget max={metrics.MaxAudioBufferedDurationMsObserved:0.##}ms budget={maxHealthyAudioBufferedMs:0.##}ms");
        }

        const double maxHealthyAvDriftMs = 250.0;
        if (metrics.MaxAbsAvDriftMsObserved > maxHealthyAvDriftMs)
        {
            warnings.Add($"flashback playback: absolute A/V drift exceeded budget max={metrics.MaxAbsAvDriftMsObserved:0.##}ms budget={maxHealthyAvDriftMs:0.##}ms");
        }
    }

    internal static void ValidateFlashbackPreviewScheduler(
        long deadlineDropsDelta,
        long underflowsDelta,
        long d3dStatsFailureDelta,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        double targetFps,
        bool tolerateSchedulerTransitionsWithHealthyVisualCadence,
        List<string> warnings)
    {
        if (deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler deadline drops increased delta={deadlineDropsDelta}");
        }

        if (underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler underflows increased delta={underflowsDelta}");
        }

        if (d3dStatsFailureDelta > 0)
        {
            warnings.Add($"flashback preview: D3D frame stats failures increased delta={d3dStatsFailureDelta}");
        }

        if (targetFps < 100)
        {
            return;
        }

        var targetFrameMs = 1000.0 / targetFps;
        var onePercentLowFloor = targetFps * 0.80;
        var presentP99BudgetMs = targetFrameMs * 1.25;
        var totalP99BudgetMs = targetFrameMs * 1.35;
        var onePercentLowMiss =
            previewCadenceMetrics.MinOnePercentLowFpsObserved > 0 &&
            previewCadenceMetrics.MinOnePercentLowFpsObserved < onePercentLowFloor;
        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
        var presentP99Miss =
            previewD3DMetrics.PresentCallP99MsAtEnd > presentP99BudgetMs;
        var totalP99Miss =
            previewD3DMetrics.TotalFrameCpuP99MsAtEnd > totalP99BudgetMs;

        if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)
        {
            warnings.Add(
                "flashback preview: present/display pressure " +
                $"targetFps={targetFps:0.##} " +
                $"onePercentLowFpsMin={previewCadenceMetrics.MinOnePercentLowFpsObserved:0.##}/{onePercentLowFloor:0.##} " +
                $"visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##} " +
                $"visualRepeatPctMax={visualCadenceMetrics.MaxRepeatPercentObserved:0.###} " +
                $"visualLongestRepeatRun={visualCadenceMetrics.LongestRepeatRunAtEnd} " +
                $"presentCallP99Ms={previewD3DMetrics.PresentCallP99MsAtEnd:0.##}/{presentP99BudgetMs:0.##} " +
                $"totalFrameCpuP99Ms={previewD3DMetrics.TotalFrameCpuP99MsAtEnd:0.##}/{totalP99BudgetMs:0.##} " +
                $"missedRefreshDelta={previewD3DMetrics.MissedRefreshDelta} " +
                $"underflowsDelta={underflowsDelta} " +
                $"latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)} " +
                $"latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##} " +
                $"latestSlowTotalFrameCpuMs={previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs:0.##} " +
                $"latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        }
    }

    private static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}
