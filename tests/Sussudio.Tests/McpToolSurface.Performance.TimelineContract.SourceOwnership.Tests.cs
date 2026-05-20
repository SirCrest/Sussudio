static partial class Program
{
    private static void AssertMcpPerformanceTimelineSourceOwnership(McpPerformanceTimelineSources sources)
    {
        AssertDoesNotContain(sources.RootSource, "private sealed class TimelineRow");
        AssertDoesNotContain(sources.RootSource, "new StringBuilder()");
        AssertDoesNotContain(sources.RootSource, "== Trend Summary");
        AssertDoesNotContain(sources.RowsSource, "private sealed class TimelineRow");
        AssertContains(sources.RowsModelSource, "private sealed partial class TimelineRow");
        AssertContains(sources.RowsPreviewModelSource, "private sealed partial class TimelineRow");
        AssertContains(sources.RowsFlashbackPlaybackModelSource, "private sealed partial class TimelineRow");
        AssertContains(sources.RowsFlashbackExportModelSource, "private sealed partial class TimelineRow");
        AssertContains(sources.RowsSystemModelSource, "private sealed partial class TimelineRow");
        AssertContains(sources.RowsModelSource, "public double PreviewFivePercentLowFps { get; init; }");
        AssertContains(sources.RowsPreviewModelSource, "public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;");
        AssertContains(sources.RowsFlashbackPlaybackModelSource, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(sources.RowsFlashbackExportModelSource, "public double FlashbackExportThroughputBytesPerSec { get; init; }");
        AssertContains(sources.RowsSystemModelSource, "public int IoThreads { get; init; }");
        AssertDoesNotContain(sources.RowsModelSource, "MjpegPreviewJitter");
        AssertDoesNotContain(sources.RowsPreviewModelSource, "FlashbackPlayback");
        AssertDoesNotContain(sources.RowsFlashbackPlaybackModelSource, "FlashbackExportActive");
        AssertDoesNotContain(sources.RowsFlashbackExportModelSource, "GcPause");
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
