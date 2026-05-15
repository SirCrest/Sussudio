using System.Text;

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
        builder.AppendLine($"Capture Avg:    {first.CaptureAvgMs:F1}ms -> {last.CaptureAvgMs:F1}ms (delta: {last.CaptureAvgMs - first.CaptureAvgMs:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture P95:    {first.CaptureP95Ms:F1}ms -> {last.CaptureP95Ms:F1}ms (delta: {last.CaptureP95Ms - first.CaptureP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture P99:    {first.CaptureP99Ms:F1}ms -> {last.CaptureP99Ms:F1}ms (delta: {last.CaptureP99Ms - first.CaptureP99Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture Max:    {first.CaptureMaxMs:F1}ms -> {last.CaptureMaxMs:F1}ms (delta: {last.CaptureMaxMs - first.CaptureMaxMs:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Preview Avg:    {first.PreviewAvgMs:F1}ms -> {last.PreviewAvgMs:F1}ms (delta: {last.PreviewAvgMs - first.PreviewAvgMs:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Preview P95:    {first.PreviewP95Ms:F1}ms -> {last.PreviewP95Ms:F1}ms (delta: {last.PreviewP95Ms - first.PreviewP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Preview Max:    {first.PreviewMaxMs:F1}ms -> {last.PreviewMaxMs:F1}ms (delta: {last.PreviewMaxMs - first.PreviewMaxMs:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Preview 1% Low: {first.PreviewOnePercentLowFps:F1}fps -> {last.PreviewOnePercentLowFps:F1}fps");
        builder.AppendLine($"Preview Slow%:  {first.PreviewSlowPct:F1}% -> {last.PreviewSlowPct:F1}% (delta: {last.PreviewSlowPct - first.PreviewSlowPct:+0.0;-0.0;0.0}%)");
        builder.AppendLine($"D3D Present P95:{first.PreviewD3DPresentP95Ms:F1}ms -> {last.PreviewD3DPresentP95Ms:F1}ms (delta: {last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"D3D Total P95:  {first.PreviewD3DTotalP95Ms:F1}ms -> {last.PreviewD3DTotalP95Ms:F1}ms (delta: {last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"D3D Pipe P95:   {first.PreviewD3DPipelineP95Ms:F1}ms -> {last.PreviewD3DPipelineP95Ms:F1}ms (delta: {last.PreviewD3DPipelineP95Ms - first.PreviewD3DPipelineP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"D3D Wait P95:   {first.PreviewD3DFrameLatencyWaitP95Ms:F1}ms -> {last.PreviewD3DFrameLatencyWaitP95Ms:F1}ms (timeouts: {first.PreviewD3DFrameLatencyWaitTimeouts} -> {last.PreviewD3DFrameLatencyWaitTimeouts})");
        builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
        builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
        builder.AppendLine($"Capture Rate:   {first.CaptureFps:F1}fps -> {last.CaptureFps:F1}fps (derived avg)");
        builder.AppendLine($"Capture 1% Low: {first.CaptureOnePercentLowFps:F1}fps -> {last.CaptureOnePercentLowFps:F1}fps");
        builder.AppendLine($"Preview Rate:   {first.PreviewFps:F1}fps -> {last.PreviewFps:F1}fps (derived avg)");
        builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
        builder.AppendLine($"Process CPU:    {first.CpuPct:F1}% -> {last.CpuPct:F1}% (delta: {last.CpuPct - first.CpuPct:+0.0;-0.0;0.0}%)");
        builder.AppendLine($"Working Set:    {first.WorkingMb:F1}MB -> {last.WorkingMb:F1}MB (delta: {last.WorkingMb - first.WorkingMb:+0.0;-0.0;0.0}MB)");
        builder.AppendLine($"Managed Heap:   {first.ManagedMb:F1}MB -> {last.ManagedMb:F1}MB (delta: {last.ManagedMb - first.ManagedMb:+0.0;-0.0;0.0}MB)");
        builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
        builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
        builder.AppendLine($"GC Pause%:      {first.GcPause:F1}% -> {last.GcPause:F1}% (delta: {last.GcPause - first.GcPause:+0.0;-0.0;0.0}%)");
    }
}
