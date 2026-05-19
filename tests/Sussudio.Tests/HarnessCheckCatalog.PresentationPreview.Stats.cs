using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewStatsChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Stats snapshot construction lives in focused builder",
            StatsSnapshotConstruction_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot builder maps health and renderer metrics",
            StatsSnapshotBuilder_MapsHealthAndRendererMetrics);
    }
}
