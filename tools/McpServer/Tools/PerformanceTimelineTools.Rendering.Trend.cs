using System.Text;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void AppendTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries, double targetOnePercentLowFps)
    {
        if (entries.Count < 2)
        {
            return;
        }

        var first = entries[0];
        var last = entries[^1];
        var targetFrameBudgetMs = targetOnePercentLowFps > 0
            ? 1000.0 / targetOnePercentLowFps
            : 0;
        builder.AppendLine();
        builder.AppendLine("== Trend Summary (first vs last sample) ==");
        builder.AppendLine($"Capture Avg:    {first.CaptureAvgMs:F1}ms -> {last.CaptureAvgMs:F1}ms (delta: {last.CaptureAvgMs - first.CaptureAvgMs:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture P95:    {first.CaptureP95Ms:F1}ms -> {last.CaptureP95Ms:F1}ms (delta: {last.CaptureP95Ms - first.CaptureP95Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture P99:    {first.CaptureP99Ms:F1}ms -> {last.CaptureP99Ms:F1}ms (delta: {last.CaptureP99Ms - first.CaptureP99Ms:+0.0;-0.0;0.0}ms)");
        builder.AppendLine($"Capture Max:    {first.CaptureMaxMs:F1}ms -> {last.CaptureMaxMs:F1}ms (delta: {last.CaptureMaxMs - first.CaptureMaxMs:+0.0;-0.0;0.0}ms)");
        AppendPreviewTrendSummary(builder, first, last);
        AppendFlashbackTrendSummary(builder, first, last);
        builder.AppendLine($"Capture Rate:   {first.CaptureFps:F1}fps -> {last.CaptureFps:F1}fps (derived avg)");
        builder.AppendLine($"Capture 5% Low: {first.CaptureFivePercentLowFps:F1}fps -> {last.CaptureFivePercentLowFps:F1}fps");
        builder.AppendLine($"Capture 1% Low: {first.CaptureOnePercentLowFps:F1}fps -> {last.CaptureOnePercentLowFps:F1}fps");
        builder.AppendLine($"Preview Rate:   {first.PreviewFps:F1}fps -> {last.PreviewFps:F1}fps (derived avg)");
        builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
        builder.AppendLine($"Working Set:    {first.WorkingMb:F1}MB -> {last.WorkingMb:F1}MB (delta: {last.WorkingMb - first.WorkingMb:+0.0;-0.0;0.0}MB)");
        builder.AppendLine($"Managed Heap:   {first.ManagedMb:F1}MB -> {last.ManagedMb:F1}MB (delta: {last.ManagedMb - first.ManagedMb:+0.0;-0.0;0.0}MB)");
        builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
        builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
        builder.AppendLine($"GC Pause%:      {first.GcPause:F1}% -> {last.GcPause:F1}% (delta: {last.GcPause - first.GcPause:+0.0;-0.0;0.0}%)");

        if (targetOnePercentLowFps > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"== 1% Low Target Summary ({targetOnePercentLowFps:0.##}fps, budget {targetFrameBudgetMs:0.###}ms) ==");
            AppendOnePercentLowTargetSummary(builder, entries, targetOnePercentLowFps, targetFrameBudgetMs);
            AppendPressureSummary(builder, entries, targetFrameBudgetMs);
        }
    }
}
