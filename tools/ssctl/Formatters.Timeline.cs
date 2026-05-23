using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    public static string FormatTimeline(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "No timeline data available.");
        }

        var entries = ReadTimelineRows(data);
        if (entries.Count == 0)
        {
            return "No timeline entries collected yet.";
        }

        return RenderTimeline(entries);
    }

    private static List<TimelineRow> ReadTimelineRows(JsonElement data)
    {
        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            entries.Add(new TimelineRow
            {
                Timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc"),
                CaptureFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureFps"),
                PreviewFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewFps"),
                VidQueue = AutomationSnapshotFormatter.GetInt(item, "VideoQueueDepth"),
                VidDrops = AutomationSnapshotFormatter.GetLong(item, "VideoDrops"),
                CaptureAvgMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceAverageMs"),
                CaptureP95Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP95Ms"),
                CaptureP99Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP99Ms"),
                CaptureMaxMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceMaxMs"),
                CaptureOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceOnePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
                PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount"),
                PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms"),
                PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms"),
                PreviewD3DPipelineP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyP95Ms"),
                PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount"),
                PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms"),
                PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount"),
                LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs"),
                CpuPct = AutomationSnapshotFormatter.GetDouble(item, "ProcessCpuPercent"),
                WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb"),
                ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb"),
                Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections"),
                Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections"),
                Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections"),
                GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent"),
                Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable"),
                IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable")
            });
        }

        return entries;
    }

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

    private static void AppendTimelineTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries)
    {
        if (entries.Count < 2)
        {
            return;
        }

        var first = entries[0];
        var last = entries[^1];
        builder.AppendLine();
        builder.AppendLine("== Trend Summary (first vs last sample) ==");
        builder.AppendLine($"Capture Avg:    {FormatOneDecimalInvariant(first.CaptureAvgMs)}ms -> {FormatOneDecimalInvariant(last.CaptureAvgMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureAvgMs - first.CaptureAvgMs)}ms)");
        builder.AppendLine($"Capture P95:    {FormatOneDecimalInvariant(first.CaptureP95Ms)}ms -> {FormatOneDecimalInvariant(last.CaptureP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureP95Ms - first.CaptureP95Ms)}ms)");
        builder.AppendLine($"Capture P99:    {FormatOneDecimalInvariant(first.CaptureP99Ms)}ms -> {FormatOneDecimalInvariant(last.CaptureP99Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureP99Ms - first.CaptureP99Ms)}ms)");
        builder.AppendLine($"Capture Max:    {FormatOneDecimalInvariant(first.CaptureMaxMs)}ms -> {FormatOneDecimalInvariant(last.CaptureMaxMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.CaptureMaxMs - first.CaptureMaxMs)}ms)");
        builder.AppendLine($"Preview Avg:    {FormatOneDecimalInvariant(first.PreviewAvgMs)}ms -> {FormatOneDecimalInvariant(last.PreviewAvgMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewAvgMs - first.PreviewAvgMs)}ms)");
        builder.AppendLine($"Preview P95:    {FormatOneDecimalInvariant(first.PreviewP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewP95Ms - first.PreviewP95Ms)}ms)");
        builder.AppendLine($"Preview Max:    {FormatOneDecimalInvariant(first.PreviewMaxMs)}ms -> {FormatOneDecimalInvariant(last.PreviewMaxMs)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewMaxMs - first.PreviewMaxMs)}ms)");
        builder.AppendLine($"Preview 1% Low: {FormatOneDecimalInvariant(first.PreviewOnePercentLowFps)}fps -> {FormatOneDecimalInvariant(last.PreviewOnePercentLowFps)}fps");
        builder.AppendLine($"Preview Slow%:  {FormatOneDecimalInvariant(first.PreviewSlowPct)}% -> {FormatOneDecimalInvariant(last.PreviewSlowPct)}% (delta: {FormatSignedOneDecimalInvariant(last.PreviewSlowPct - first.PreviewSlowPct)}%)");
        builder.AppendLine($"D3D Present P95:{FormatOneDecimalInvariant(first.PreviewD3DPresentP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DPresentP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms)}ms)");
        builder.AppendLine($"D3D Total P95:  {FormatOneDecimalInvariant(first.PreviewD3DTotalP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DTotalP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms)}ms)");
        builder.AppendLine($"D3D Pipe P95:   {FormatOneDecimalInvariant(first.PreviewD3DPipelineP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DPipelineP95Ms)}ms (delta: {FormatSignedOneDecimalInvariant(last.PreviewD3DPipelineP95Ms - first.PreviewD3DPipelineP95Ms)}ms)");
        builder.AppendLine($"D3D Wait P95:   {FormatOneDecimalInvariant(first.PreviewD3DFrameLatencyWaitP95Ms)}ms -> {FormatOneDecimalInvariant(last.PreviewD3DFrameLatencyWaitP95Ms)}ms (timeouts: {first.PreviewD3DFrameLatencyWaitTimeouts} -> {last.PreviewD3DFrameLatencyWaitTimeouts})");
        builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
        builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
        builder.AppendLine($"Capture Rate:   {FormatOneDecimalInvariant(first.CaptureFps)}fps -> {FormatOneDecimalInvariant(last.CaptureFps)}fps (derived avg)");
        builder.AppendLine($"Capture 1% Low: {FormatOneDecimalInvariant(first.CaptureOnePercentLowFps)}fps -> {FormatOneDecimalInvariant(last.CaptureOnePercentLowFps)}fps");
        builder.AppendLine($"Preview Rate:   {FormatOneDecimalInvariant(first.PreviewFps)}fps -> {FormatOneDecimalInvariant(last.PreviewFps)}fps (derived avg)");
        builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
        builder.AppendLine($"Process CPU:    {FormatOneDecimalInvariant(first.CpuPct)}% -> {FormatOneDecimalInvariant(last.CpuPct)}% (delta: {FormatSignedOneDecimalInvariant(last.CpuPct - first.CpuPct)}%)");
        builder.AppendLine($"Working Set:    {FormatOneDecimalInvariant(first.WorkingMb)}MB -> {FormatOneDecimalInvariant(last.WorkingMb)}MB (delta: {FormatSignedOneDecimalInvariant(last.WorkingMb - first.WorkingMb)}MB)");
        builder.AppendLine($"Managed Heap:   {FormatOneDecimalInvariant(first.ManagedMb)}MB -> {FormatOneDecimalInvariant(last.ManagedMb)}MB (delta: {FormatSignedOneDecimalInvariant(last.ManagedMb - first.ManagedMb)}MB)");
        builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
        builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
        builder.AppendLine($"GC Pause%:      {FormatOneDecimalInvariant(first.GcPause)}% -> {FormatOneDecimalInvariant(last.GcPause)}% (delta: {FormatSignedOneDecimalInvariant(last.GcPause - first.GcPause)}%)");
    }

    private static string FormatOneDecimalInvariant(double value)
        => AutomationSnapshotFormatter.FormatNumber(value, "F1");

    private static string FormatSignedOneDecimalInvariant(double value)
        => AutomationSnapshotFormatter.FormatNumber(value, "+0.0;-0.0;0.0");

    private sealed class TimelineRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public double CaptureFps { get; init; }
        public double PreviewFps { get; init; }
        public int VidQueue { get; init; }
        public long VidDrops { get; init; }
        public double CaptureAvgMs { get; init; }
        public double CaptureP95Ms { get; init; }
        public double CaptureP99Ms { get; init; }
        public double CaptureMaxMs { get; init; }
        public double CaptureOnePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public double PreviewD3DPipelineP95Ms { get; init; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; init; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public long LatencyMs { get; init; }
        public double CpuPct { get; init; }
        public double WorkingMb { get; init; }
        public double ManagedMb { get; init; }
        public int Gen0 { get; init; }
        public int Gen1 { get; init; }
        public int Gen2 { get; init; }
        public double GcPause { get; init; }
        public int Workers { get; init; }
        public int IoThreads { get; init; }
    }
}
