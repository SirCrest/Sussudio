using System.Text;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
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
}
