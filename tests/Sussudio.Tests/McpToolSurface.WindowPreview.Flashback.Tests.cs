using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpFlashbackTools_RouteEnableToggle()
    {
        var pipeName = NewMcpToolPipeName("flashback-enabled");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var flashbackTools = RequireMcpType("McpServer.Tools.FlashbackTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_enabled",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback disabled.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetFlashbackEnabled", ("enabled", false));
        AssertEqual("[OK] SetFlashbackEnabled: Flashback disabled.", result, "flashback_enabled formatted success");

        var actionPipeName = NewMcpToolPipeName("flashback-action-scrub");
        var actionPipeClient = CreateMcpPipeClient(actionPipeName);
        var actionRequests = await CapturePipeRequestsAsync(
                actionPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_action",
                            actionPipeClient,
                            "begin_scrub",
                            1234d)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback scrub begin at 1234ms requested.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(actionRequests[0], "FlashbackAction", ("action", "begin-scrub"), ("positionMs", 1234d));
        AssertContains(result, "[OK] FlashbackAction(begin-scrub): Flashback scrub begin at 1234ms requested.");

        var applyPipeName = NewMcpToolPipeName("flashback-apply");
        var applyPipeClient = CreateMcpPipeClient(applyPipeName);
        var applyRequests = await CapturePipeRequestsAsync(
                applyPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_apply",
                            applyPipeClient)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback restarted.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(applyRequests[0], "RestartFlashback");
        AssertEqual("[OK] RestartFlashback: Flashback restarted.", result, "flashback_apply formatted success");

        var flashbackToolsRootText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.cs")
            .Replace("\r\n", "\n");
        var flashbackToolsActionText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.Actions.cs")
            .Replace("\r\n", "\n");
        var flashbackToolsExportText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.Export.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackToolsRootText, "[McpServerToolType]");
        AssertContains(flashbackToolsRootText, "public static partial class FlashbackTools");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_enabled");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_apply");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_segments");
        AssertContains(flashbackToolsRootText, "FlashbackGetSegments");
        AssertDoesNotContain(flashbackToolsRootText, "flashback_action");
        AssertDoesNotContain(flashbackToolsRootText, "flashback_export");
        AssertContains(flashbackToolsActionText, "public static async Task<CallToolResult> flashback_action");
        AssertContains(flashbackToolsActionText, "if (string.IsNullOrWhiteSpace(action))");
        AssertContains(flashbackToolsActionText, "Flashback action is required. Expected play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points.");
        AssertContains(flashbackToolsActionText, "normalizedAction is not (\"play\" or \"pause\" or \"go-live\" or \"seek\" or \"begin-scrub\" or \"update-scrub\" or \"end-scrub\" or \"set-in-point\" or \"set-out-point\" or \"clear-in-out-points\")");
        AssertContains(flashbackToolsActionText, "Flashback action must be one of: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points.");
        AssertContains(flashbackToolsActionText, "normalizedAction == \"begin-scrub\"");
        AssertContains(flashbackToolsActionText, "normalizedAction == \"update-scrub\"");
        AssertContains(flashbackToolsActionText, "Flashback seek, begin_scrub, and update_scrub require positionMs.");
        AssertContains(flashbackToolsActionText, "if (!double.IsFinite(positionMs.Value) ||\n                positionMs.Value < 0 ||\n                positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds)");
        AssertContains(flashbackToolsActionText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(flashbackToolsExportText, "public static async Task<CallToolResult> flashback_export");
        AssertContains(flashbackToolsExportText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(flashbackToolsExportText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(flashbackToolsExportText, "AutomationSnapshotFormatter.Get(data, \"FailureKind\", string.Empty)");
        AssertContains(flashbackToolsExportText, "FailureKind: {failureKind}");

        var exportPipeName = NewMcpToolPipeName("flashback-export-failure-kind");
        var exportPipeClient = CreateMcpPipeClient(exportPipeName);
        var exportRequests = await CapturePipeRequestsAsync(
                exportPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_export",
                            exportPipeClient,
                            1d,
                            "temp/fb-failure-kind.mp4",
                            false,
                            false)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":false,\"Message\":\"Flashback buffer not active\",\"Data\":{\"Succeeded\":false,\"OutputPath\":\"temp/fb-failure-kind.mp4\",\"StatusMessage\":\"Flashback buffer not active\",\"FailureKind\":\"BufferInactive\",\"FileSizeBytes\":0}}")
            .ConfigureAwait(false);

        AssertCommandRequest(exportRequests[0], "FlashbackExport", ("seconds", 1d), ("outputPath", "temp/fb-failure-kind.mp4"), ("useSelectionRange", false), ("force", false));
        AssertContains(result, "[ERROR] FlashbackExport: Flashback buffer not active");
        AssertContains(result, "FailureKind: BufferInactive");

        var segmentsPipeName = NewMcpToolPipeName("flashback-segments");
        var segmentsPipeClient = CreateMcpPipeClient(segmentsPipeName);
        var segmentsRequests = await CapturePipeRequestsAsync(
                segmentsPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_segments",
                            segmentsPipeClient)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"1 segment.\",\"Data\":{\"Segments\":[{\"Path\":\"temp/segment-000.mp4\",\"DurationMs\":1000,\"FrameCount\":60}]}}")
            .ConfigureAwait(false);

        AssertCommandRequest(segmentsRequests[0], "FlashbackGetSegments");
        AssertContains(result, "[OK] FlashbackGetSegments: 1 segment.");
        AssertContains(result, "\"FrameCount\":60");
    }
}
