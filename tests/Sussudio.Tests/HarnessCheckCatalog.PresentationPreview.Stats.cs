using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewStatsInitialChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Stats dock refresh orchestration lives in controller",
            StatsDockPresentationApplication_LivesInController);
        await AddCheckAsync(results,
            "Stats dock row chrome lives in focused controller",
            StatsDockRowChrome_LivesInFocusedController);
    }

    private static async Task AddPresentationPreviewStatsChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Stats panels use source telemetry for HDMI input format and HDR",
            StatsPanels_UseSourceTelemetry_ForHdmiInput);
        await AddCheckAsync(results,
            "Stats presentation logic lives in focused builder",
            StatsPresentationLogic_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot construction lives in focused builder",
            StatsSnapshotConstruction_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot builder maps health and renderer metrics",
            StatsSnapshotBuilder_MapsHealthAndRendererMetrics);
        await AddCheckAsync(results,
            "Stats live summary shows current preview frame time and 1 percent low",
            StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow);
    }
}
