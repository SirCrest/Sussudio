using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var sources = ReadMcpPerformanceTimelineSources();

        AssertMcpPerformanceTimelineSourceOwnership(sources);
        AssertMcpPerformanceTimelineRenderingContracts(sources);
        AssertMcpPerformanceTimelineProjectionContracts(sources);

        return Task.CompletedTask;
    }

    private static McpPerformanceTimelineSources ReadMcpPerformanceTimelineSources()
    {
        var rootSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs");
        var rowsSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs");
        var rowsModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs");
        var formattingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs");
        var previewFormattingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Preview.cs");
        var flashbackFormattingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Flashback.cs");
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs");
        var trendSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.cs");
        var previewTrendSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Preview.cs");
        var flashbackTrendSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.cs");
        var flashbackExportTrendSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs");
        var summariesSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs");
        var pressureSummariesSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Summaries.Pressure.cs");

        return new McpPerformanceTimelineSources
        {
            RootSource = rootSource,
            RowsSource = rowsSource,
            RowsModelSource = rowsModelSource,
            FormattingSource = formattingSource,
            PreviewFormattingSource = previewFormattingSource,
            FlashbackFormattingSource = flashbackFormattingSource,
            RenderingSource = renderingSource,
            TrendSource = trendSource,
            PreviewTrendSource = previewTrendSource,
            FlashbackTrendSource = flashbackTrendSource,
            FlashbackExportTrendSource = flashbackExportTrendSource,
            SummariesSource = summariesSource,
            PressureSummariesSource = pressureSummariesSource,
            CombinedSource = string.Join(
                "\n",
                rootSource,
                rowsSource,
                rowsModelSource,
                formattingSource,
                previewFormattingSource,
                flashbackFormattingSource,
                renderingSource,
                trendSource,
                previewTrendSource,
                flashbackTrendSource,
                flashbackExportTrendSource,
                summariesSource,
                pressureSummariesSource)
        };
    }

    private sealed class McpPerformanceTimelineSources
    {
        public string RootSource { get; init; } = string.Empty;
        public string RowsSource { get; init; } = string.Empty;
        public string RowsModelSource { get; init; } = string.Empty;
        public string FormattingSource { get; init; } = string.Empty;
        public string PreviewFormattingSource { get; init; } = string.Empty;
        public string FlashbackFormattingSource { get; init; } = string.Empty;
        public string RenderingSource { get; init; } = string.Empty;
        public string TrendSource { get; init; } = string.Empty;
        public string PreviewTrendSource { get; init; } = string.Empty;
        public string FlashbackTrendSource { get; init; } = string.Empty;
        public string FlashbackExportTrendSource { get; init; } = string.Empty;
        public string SummariesSource { get; init; } = string.Empty;
        public string PressureSummariesSource { get; init; } = string.Empty;
        public string CombinedSource { get; init; } = string.Empty;
    }
}
