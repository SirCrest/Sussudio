static partial class Program
{
    private static void AssertMcpPerformanceTimelineSourceOwnership(McpPerformanceTimelineSources sources)
    {
        AssertDoesNotContain(sources.RootSource, "private sealed class TimelineRow");
        AssertDoesNotContain(sources.RootSource, "new StringBuilder()");
        AssertDoesNotContain(sources.RootSource, "== Trend Summary");
        AssertDoesNotContain(sources.RowsSource, "private sealed class TimelineRow");
        AssertContains(sources.RowsSource, "PopulatePreviewTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackPlaybackTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackExportTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateSystemTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "private static void PopulatePreviewTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateSystemTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(sources.RowsSource, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(sources.RowsSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sources.RowsSource, "ThreadPoolIoAvailable");
        AssertOccursBefore(sources.RowsSource, "private static void PopulatePreviewTimelineRow", "private static void PopulateFlashbackPlaybackTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow", "private static void PopulateFlashbackExportTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow", "private static void PopulateSystemTimelineRow");
        AssertContains(sources.RowsModelSource, "private sealed class TimelineRow");
        AssertContains(sources.RowsModelSource, "public double PreviewFivePercentLowFps { get; set; }");
        AssertContains(sources.RowsModelSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;");
        AssertContains(sources.RowsModelSource, "public string FlashbackPlaybackLastCommandFailure { get; set; } = string.Empty;");
        AssertContains(sources.RowsModelSource, "public double FlashbackExportThroughputBytesPerSec { get; set; }");
        AssertContains(sources.RowsModelSource, "public int IoThreads { get; set; }");
        AssertOccursBefore(sources.RowsModelSource, "public double PreviewSlowPct { get; set; }", "public double VisualCadenceChangeObservedFps { get; set; }");
        AssertOccursBefore(sources.RowsModelSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;", "public string FlashbackPlaybackState { get; set; } = string.Empty;");
        AssertOccursBefore(sources.RowsModelSource, "public bool FlashbackForceRotateDraining { get; set; }", "public bool FlashbackExportActive { get; set; }");
        AssertOccursBefore(sources.RowsModelSource, "public string FlashbackExportMessage { get; set; } = string.Empty;", "public long LatencyMs { get; set; }");
        AssertContains(sources.RenderingSource, "BuildPerformanceTimelineText");
        AssertDoesNotContain(sources.RenderingSource, "== Trend Summary");
        AssertContains(sources.FormattingSource, "FormatOptional");
        AssertContains(sources.FormattingSource, "CompactCell");
        AssertContains(sources.FormattingSource, "private static string FormatJitterDepthCell(TimelineRow row)");
        AssertContains(sources.FormattingSource, "private static string FormatD3DP99Bottleneck(TimelineRow row)");
        AssertContains(sources.FormattingSource, "private static string FormatFlashbackStageCell(TimelineRow row)");
        AssertContains(sources.FormattingSource, "private static string FormatExportFailureKind(string failureKind)");
        AssertContains(sources.FormattingSource, "private static string FormatBytesPerSecond(double bytesPerSecond)");
        AssertContains(sources.TrendSource, "AppendTrendSummary");
        AssertContains(sources.TrendSource, "== Trend Summary");
        AssertContains(sources.TrendSource, "AppendPreviewTrendSummary(builder, first, last);");
        AssertContains(sources.TrendSource, "AppendFlashbackTrendSummary(builder, first, last);");
        AssertContains(sources.TrendSource, "private static void AppendPreviewTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.TrendSource, "Preview Slow Stage:");
        AssertContains(sources.TrendSource, "D3D P99 Bottleneck:");
        AssertContains(sources.TrendSource, "Jitter Drops:");
        AssertContains(sources.TrendSource, "private static void AppendFlashbackTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.TrendSource, "Flashback Cmd Counters:");
        AssertContains(sources.TrendSource, "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertContains(sources.TrendSource, "private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.TrendSource, "Export Output:");
        AssertOccursBefore(sources.TrendSource, "Cleanup State:", "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertOccursBefore(sources.TrendSource, "AppendFlashbackExportTrendSummary(builder, first, last);", "private static void AppendFlashbackExportTrendSummary");
        AssertContains(sources.SummariesSource, "AppendOnePercentLowTargetSummary");
        AssertContains(sources.SummariesSource, "private static void AppendPressureSummary(");
        AssertContains(sources.SummariesSource, "== Pressure Summary ==");
        AssertContains(sources.SummariesSource, "private static int CountOverBudget(");
    }
}
