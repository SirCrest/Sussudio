using System.Globalization;
using System.Text;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static string RenderTimeline(IReadOnlyList<TimelineRow> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Performance Timeline ({entries.Count} samples)");
        builder.AppendLine();
        builder.AppendLine("Timestamp                | CapAvg | CapP95 | CapP99 | Cap1% | PrvAvg | PrvP95 | PrvSlow | D3DQ | D3DPrs | D3DTot | D3DPipe | D3DMiss | VidQ | VidDrop | LatMs | CPU% | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 200));

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,6:F1} | {4,5:F1} | {5,6:F1} | {6,6:F1} | {7,7:F1} | {8,4} | {9,6:F1} | {10,6:F1} | {11,7:F1} | {12,7} | {13,4} | {14,7} | {15,5} | {16,5:F1} | {17,6:F1} | {18,6:F1} | {19,4} | {20,4} | {21,4} | {22,4:F1} | {23,4} | {24,4}",
                entry.Timestamp,
                entry.CaptureAvgMs,
                entry.CaptureP95Ms,
                entry.CaptureP99Ms,
                entry.CaptureOnePercentLowFps,
                entry.PreviewAvgMs,
                entry.PreviewP95Ms,
                entry.PreviewSlowPct,
                entry.PreviewD3DPending,
                entry.PreviewD3DPresentP95Ms,
                entry.PreviewD3DTotalP95Ms,
                entry.PreviewD3DPipelineP95Ms,
                entry.PreviewD3DRecentMissed,
                entry.VidQueue,
                entry.VidDrops,
                entry.LatencyMs,
                entry.CpuPct,
                entry.WorkingMb,
                entry.ManagedMb,
                entry.Gen0,
                entry.Gen1,
                entry.Gen2,
                entry.GcPause,
                entry.Workers,
                entry.IoThreads));
        }

        AppendTimelineTrendSummary(builder, entries);

        return builder.ToString().TrimEnd();
    }
}
