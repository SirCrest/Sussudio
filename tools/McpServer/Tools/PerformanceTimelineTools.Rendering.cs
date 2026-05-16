using System.Globalization;
using System.Text;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static string BuildPerformanceTimelineText(IReadOnlyList<TimelineRow> entries, double targetOnePercentLowFps)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Performance Timeline ({entries.Count} samples)");
        builder.AppendLine();
        builder.AppendLine("Timestamp                | CapAvg | CapP95 | CapP99 | Cap1% | PrvAvg | PrvP95 | PrvSlow | JitD  | JitLat | JitDrop | JitUF | JitWhy       | D3DQ | D3DPrs | D3DTot | InP99 | RsP99 | PrP99 | TotP99 | D3DSch | D3DMiss | D3DDrop      | FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage        | Cln | ExStat  | ExKind | Ex%   | ExMBps | VidQ | VidDrop | LatMs | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 409));

        foreach (var e in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,6:F1} | {4,5:F1} | {5,6:F1} | {6,6:F1} | {7,7:F1} | {8,-5} | {9,6:F1} | {10,7} | {11,5} | {12,-12} | {13,4} | {14,6:F1} | {15,6:F1} | {16,5:F1} | {17,5:F1} | {18,5:F1} | {19,6:F1} | {20,6:F1} | {21,7} | {22,-12} | {23,-7} | {24,5:F1} | {25,5:F1} | {26,5:F1} | {27,5} | {28,6} | {29,-14} | {30,-3} | {31,-7} | {32,-6} | {33,5:F1} | {34,6:F1} | {35,4} | {36,7} | {37,5} | {38,6:F1} | {39,6:F1} | {40,4} | {41,4} | {42,4} | {43,4:F1} | {44,4} | {45,4}",
                e.Timestamp,
                e.CaptureAvgMs,
                e.CaptureP95Ms,
                e.CaptureP99Ms,
                e.CaptureOnePercentLowFps,
                e.PreviewAvgMs,
                e.PreviewP95Ms,
                e.PreviewSlowPct,
                FormatJitterDepthCell(e),
                e.MjpegPreviewJitterLatencyP95Ms,
                e.MjpegPreviewJitterTotalDropped,
                e.MjpegPreviewJitterUnderflowCount,
                CompactCell(string.IsNullOrWhiteSpace(e.MjpegPreviewJitterLastUnderflowReason)
                    ? e.MjpegPreviewJitterLastDropReason
                    : e.MjpegPreviewJitterLastUnderflowReason, 12),
                e.PreviewD3DPending,
                e.PreviewD3DPresentP95Ms,
                e.PreviewD3DTotalP95Ms,
                e.PreviewD3DInputUploadP99Ms,
                e.PreviewD3DRenderSubmitP99Ms,
                e.PreviewD3DPresentP99Ms,
                e.PreviewD3DTotalP99Ms,
                e.PreviewD3DSchedulerToPresentMs,
                e.PreviewD3DRecentMissed,
                CompactCell(e.PreviewD3DLastDropReason, 12),
                CompactCell(e.FlashbackPlaybackState, 7),
                e.FlashbackPlaybackOnePercentLowFps,
                e.FlashbackPlaybackP99FrameMs,
                e.FlashbackPlaybackDecodeP99Ms,
                e.FlashbackPlaybackPendingCommands,
                e.FlashbackPlaybackSubmitFailures,
                FormatFlashbackStageCell(e),
                FormatCleanupCell(e.FatalCleanupInProgress, e.FlashbackCleanupInProgress, e.FlashbackForceRotateRequested, e.FlashbackForceRotateDraining),
                CompactCell(e.FlashbackExportStatus, 7),
                FormatExportFailureKind(e.FlashbackExportFailureKind),
                e.FlashbackExportPercent,
                e.FlashbackExportThroughputBytesPerSec / (1024.0 * 1024.0),
                e.VidQueue,
                e.VidDrops,
                e.LatencyMs,
                e.WorkingMb,
                e.ManagedMb,
                e.Gen0,
                e.Gen1,
                e.Gen2,
                e.GcPause,
                e.Workers,
                e.IoThreads));
        }

        AppendTrendSummary(builder, entries, targetOnePercentLowFps);

        return builder.ToString().TrimEnd();
    }
}
