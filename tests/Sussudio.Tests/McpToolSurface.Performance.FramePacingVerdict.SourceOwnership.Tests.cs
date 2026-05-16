using System.Threading.Tasks;

static partial class Program
{
    private static Task McpFramePacingVerdictTool_SourceOwnershipIsSplit()
    {
        var rootSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");
        var channelsSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Channels.cs");
        var modelsSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Models.cs");
        var policySource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Policy.cs");
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Rendering.cs");
        var timelineSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.Timeline.cs");

        AssertContains(rootSource, "[McpServerToolType]");
        AssertContains(rootSource, "[McpServerTool, Description(\"Get a compact frame pacing verdict");
        AssertContains(rootSource, "public static async Task<CallToolResult> get_frame_pacing_verdict");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertContains(rootSource, "BuildFramePacingVerdictText(");
        AssertDoesNotContain(rootSource, "new StringBuilder()");
        AssertDoesNotContain(rootSource, "private sealed record FramePacingChannel");
        AssertDoesNotContain(rootSource, "private sealed record TimelineRow");
        AssertDoesNotContain(rootSource, "private static bool IsHalfRate");
        AssertDoesNotContain(rootSource, "private static double[] GetDoubleArray");

        AssertContains(channelsSource, "private static FramePacingChannel ReadChannel(");
        AssertContains(channelsSource, "private static double[] GetDoubleArray");
        AssertContains(modelsSource, "private sealed record FramePacingChannel");
        AssertContains(modelsSource, "private sealed record TimelineRow");
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
        AssertContains(timelineSource, "private static IReadOnlyList<TimelineRow> ReadTimeline");
        AssertContains(timelineSource, "PreviewD3DFrameStatsRecentMissedRefreshCount");

        return Task.CompletedTask;
    }
}
