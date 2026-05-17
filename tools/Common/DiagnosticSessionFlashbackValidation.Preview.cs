using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackValidation
{
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
}
