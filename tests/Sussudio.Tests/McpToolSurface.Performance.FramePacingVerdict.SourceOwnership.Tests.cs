using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpFramePacingVerdictTool_SourceOwnershipIsSplit()
    {
        var rootSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");
        var channelsSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Channels.cs");
        var policySource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Policy.cs");
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Rendering.cs");

        AssertContains(rootSource, "[McpServerToolType]");
        AssertContains(rootSource, "[McpServerTool, Description(\"Get a compact frame pacing verdict");
        AssertContains(rootSource, "public static async Task<CallToolResult> get_frame_pacing_verdict");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertContains(rootSource, "BuildFramePacingVerdictText(");
        AssertContains(rootSource, "private static IReadOnlyList<TimelineRow> ReadTimeline");
        AssertContains(rootSource, "private sealed record TimelineRow");
        AssertContains(rootSource, "PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertDoesNotContain(rootSource, "new StringBuilder()");
        AssertDoesNotContain(rootSource, "private sealed record FramePacingChannel");
        AssertDoesNotContain(rootSource, "private static bool IsHalfRate");
        AssertDoesNotContain(rootSource, "private static double[] GetDoubleArray");

        AssertContains(channelsSource, "private static FramePacingChannel ReadChannel(");
        AssertContains(channelsSource, "private sealed record FramePacingChannel");
        AssertContains(channelsSource, "private static double[] GetDoubleArray");
        AssertContains(policySource, "private static double ResolveTargetFps");
        AssertContains(policySource, "private static bool IsSampleReady");
        AssertContains(policySource, "private static bool IsHalfRate");
        AssertContains(policySource, "private static bool HasHalfRateIntervals");
        AssertContains(policySource, "private static bool IsHiddenStutter");
        AssertContains(policySource, "private static string ResolveVerdict");
        AssertContains(policySource, "private static double Ratio");
        AssertContains(renderingSource, "private static string BuildFramePacingVerdictText(");
        AssertContains(renderingSource, "new StringBuilder()");
        AssertContains(renderingSource, "Verdict: {verdict}");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Timeline.cs")),
            "Frame pacing timeline reader lives with the MCP tool orchestration");

        return Task.CompletedTask;
    }
}
