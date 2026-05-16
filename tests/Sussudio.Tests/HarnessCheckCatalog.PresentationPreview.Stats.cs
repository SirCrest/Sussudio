using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewStatsInitialChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Stats overlay lifecycle lives in controller",
            StatsOverlayLifecycle_LivesInController);
        await AddCheckAsync(results,
            "Stats dock refresh orchestration lives in controller",
            StatsDockPresentationApplication_LivesInController);
        await AddCheckAsync(results,
            "Stats section chrome lives in focused partial",
            StatsSectionChrome_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Stats dock row chrome lives in focused controller",
            StatsDockRowChrome_LivesInFocusedController);
        await AddCheckAsync(results,
            "Stats hardware row presentation formats decode and GPU rows",
            StatsHardwareRowsBuilder_FormatsDecodeAndGpuRows);
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
            "Stats window presentation formats detached text",
            StatsWindowPresentation_FormatsDetachedWindowText);
        await AddCheckAsync(results,
            "Stats dock encoder presentation formats codec and bitrate",
            StatsDockEncoderPresentation_FormatsCodecAndBitrate);
        await AddCheckAsync(results,
            "Stats visual presentation treats expected display repeat as good",
            StatsVisualPresentation_TreatsExpectedDisplayRepeatAsGood);
        await AddCheckAsync(results,
            "Stats snapshot construction lives in focused builder",
            StatsSnapshotConstruction_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot builder maps health and renderer metrics",
            StatsSnapshotBuilder_MapsHealthAndRendererMetrics);
        await AddCheckAsync(results,
            "Stats live summary shows current preview frame time and 1 percent low",
            StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow);
        await AddCheckAsync(results,
            "Frame-time overlay uses detected-FPS bounded millisecond range",
            FrameTimeOverlay_UsesDetectedFpsBoundedRange);
    }
}
