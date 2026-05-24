using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpFramePacingVerdictTool_SourceOwnershipIsSplit()
    {
        var rootSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");

        AssertContains(rootSource, "[McpServerToolType]");
        AssertContains(rootSource, "[McpServerTool, Description(\"Get a compact frame pacing verdict");
        AssertContains(rootSource, "public static async Task<CallToolResult> get_frame_pacing_verdict");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertContains(rootSource, "BuildFramePacingVerdictText(");
        AssertContains(rootSource, "private static IReadOnlyList<TimelineRow> ReadTimeline");
        AssertContains(rootSource, "private sealed record TimelineRow");
        AssertContains(rootSource, "PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(rootSource, "private static FramePacingChannel ReadChannel(");
        AssertContains(rootSource, "private sealed record FramePacingChannel");
        AssertContains(rootSource, "private static double[] GetDoubleArray");
        AssertContains(rootSource, "private static double ResolveTargetFps");
        AssertContains(rootSource, "private static bool IsSampleReady");
        AssertContains(rootSource, "private static bool IsHalfRate");
        AssertContains(rootSource, "private static bool HasHalfRateIntervals");
        AssertContains(rootSource, "private static bool IsHiddenStutter");
        AssertContains(rootSource, "private static string ResolveVerdict");
        AssertContains(rootSource, "private static double Ratio");
        AssertContains(rootSource, "private static string BuildFramePacingVerdictText(");
        AssertContains(rootSource, "new StringBuilder()");
        AssertContains(rootSource, "Verdict: {verdict}");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Timeline.cs")),
            "Frame pacing timeline reader lives with the MCP tool orchestration");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Channels.cs")),
            "Frame pacing channel projection lives with the MCP verdict tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Policy.cs")),
            "Frame pacing readiness and verdict policy lives with the MCP verdict tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Rendering.cs")),
            "Frame pacing verdict rendering lives with the MCP verdict tool");

        return Task.CompletedTask;
    }
}
