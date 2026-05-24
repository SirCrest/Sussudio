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
        AssertDoesNotContain(sources.FormattingSource, "FormatD3DP99Bottleneck");
        AssertDoesNotContain(sources.FormattingSource, "FormatJitterDepthCell");
        AssertDoesNotContain(sources.FormattingSource, "FormatFlashbackStageCell");
        AssertDoesNotContain(sources.FormattingSource, "FormatExportFailureKind");
        AssertContains(sources.PreviewFormattingSource, "private static string FormatJitterDepthCell(TimelineRow row)");
        AssertContains(sources.PreviewFormattingSource, "private static string FormatD3DP99Bottleneck(TimelineRow row)");
        AssertContains(sources.FlashbackFormattingSource, "private static string FormatFlashbackStageCell(TimelineRow row)");
        AssertContains(sources.FlashbackFormattingSource, "private static string FormatExportFailureKind(string failureKind)");
        AssertContains(sources.FlashbackFormattingSource, "private static string FormatBytesPerSecond(double bytesPerSecond)");
        AssertContains(sources.TrendSource, "AppendTrendSummary");
        AssertContains(sources.TrendSource, "== Trend Summary");
        AssertContains(sources.TrendSource, "AppendPreviewTrendSummary(builder, first, last);");
        AssertContains(sources.TrendSource, "AppendFlashbackTrendSummary(builder, first, last);");
        AssertDoesNotContain(sources.TrendSource, "Preview Slow Stage:");
        AssertDoesNotContain(sources.TrendSource, "Flashback Cmd Counters:");
        AssertContains(sources.PreviewTrendSource, "private static void AppendPreviewTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.PreviewTrendSource, "Preview Slow Stage:");
        AssertContains(sources.PreviewTrendSource, "D3D P99 Bottleneck:");
        AssertContains(sources.PreviewTrendSource, "Jitter Drops:");
        AssertContains(sources.FlashbackTrendSource, "private static void AppendFlashbackTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.FlashbackTrendSource, "Flashback Cmd Counters:");
        AssertContains(sources.FlashbackTrendSource, "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertDoesNotContain(sources.FlashbackTrendSource, "Export Output:");
        AssertContains(sources.FlashbackExportTrendSource, "private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.FlashbackExportTrendSource, "Export Output:");
        AssertContains(sources.SummariesSource, "AppendOnePercentLowTargetSummary");
        AssertDoesNotContain(sources.SummariesSource, "== Pressure Summary ==");
        AssertContains(sources.PressureSummariesSource, "private static void AppendPressureSummary(");
        AssertContains(sources.PressureSummariesSource, "== Pressure Summary ==");
        AssertContains(sources.PressureSummariesSource, "private static int CountOverBudget(");
    }
}
