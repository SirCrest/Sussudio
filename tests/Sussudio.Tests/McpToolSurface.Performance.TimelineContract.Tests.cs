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
        var rowsSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs");
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs");

        return new McpPerformanceTimelineSources
        {
            RowsSource = rowsSource,
            RenderingSource = renderingSource,
            CombinedSource = string.Join(
                "\n",
                rowsSource,
                renderingSource)
        };
    }

    private sealed class McpPerformanceTimelineSources
    {
        public string RowsSource { get; init; } = string.Empty;
        public string RenderingSource { get; init; } = string.Empty;
        public string CombinedSource { get; init; } = string.Empty;
    }
}
