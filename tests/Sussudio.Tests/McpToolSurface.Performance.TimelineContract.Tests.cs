using System.Threading.Tasks;

static partial class Program
{
    private static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
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
        var rowsPreviewSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Preview.cs");
        var rowsFlashbackPlaybackSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.FlashbackPlayback.cs");
        var rowsFlashbackExportSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.FlashbackExport.cs");
        var rowsSystemSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.System.cs");
        var rowsModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs");
        var rowsPreviewModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.Preview.cs");
        var rowsFlashbackPlaybackModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.FlashbackPlayback.cs");
        var rowsFlashbackExportModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.FlashbackExport.cs");
        var rowsSystemModelSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.System.cs");
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
            RowsPreviewSource = rowsPreviewSource,
            RowsFlashbackPlaybackSource = rowsFlashbackPlaybackSource,
            RowsFlashbackExportSource = rowsFlashbackExportSource,
            RowsSystemSource = rowsSystemSource,
            RowsModelSource = rowsModelSource,
            RowsPreviewModelSource = rowsPreviewModelSource,
            RowsFlashbackPlaybackModelSource = rowsFlashbackPlaybackModelSource,
            RowsFlashbackExportModelSource = rowsFlashbackExportModelSource,
            RowsSystemModelSource = rowsSystemModelSource,
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
                rowsPreviewSource,
                rowsFlashbackPlaybackSource,
                rowsFlashbackExportSource,
                rowsSystemSource,
                rowsModelSource,
                rowsPreviewModelSource,
                rowsFlashbackPlaybackModelSource,
                rowsFlashbackExportModelSource,
                rowsSystemModelSource,
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
        public string RowsPreviewSource { get; init; } = string.Empty;
        public string RowsFlashbackPlaybackSource { get; init; } = string.Empty;
        public string RowsFlashbackExportSource { get; init; } = string.Empty;
        public string RowsSystemSource { get; init; } = string.Empty;
        public string RowsModelSource { get; init; } = string.Empty;
        public string RowsPreviewModelSource { get; init; } = string.Empty;
        public string RowsFlashbackPlaybackModelSource { get; init; } = string.Empty;
        public string RowsFlashbackExportModelSource { get; init; } = string.Empty;
        public string RowsSystemModelSource { get; init; } = string.Empty;
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
