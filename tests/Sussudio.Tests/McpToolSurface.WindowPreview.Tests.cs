using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpWaitTools_RouteConditionWaits()
    {
        var pipeName = NewMcpToolPipeName("wait");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");

        string metResult = string.Empty;
        string notMetResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    metResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "PreviewFramesActive",
                            750,
                            50)
                        .ConfigureAwait(false);
                    notMetResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "RecordingStopped",
                            100,
                            10)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Message": "preview frames flowing",
                        "Data": {
                          "condition": "PreviewFramesActive",
                          "met": true,
                          "timeoutMs": 750,
                          "pollMs": 50
                        }
                      }
                      """
                    : """
                      {
                        "Success": false,
                        "Message": "recording still active",
                        "Data": {
                          "condition": "RecordingStopped",
                          "met": false,
                          "timeoutMs": 250,
                          "pollMs": 25
                        }
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(
            requests[0],
            "WaitForCondition",
            ("condition", "PreviewFramesActive"),
            ("timeoutMs", 750),
            ("pollMs", 50));
        AssertCommandRequest(
            requests[1],
            "WaitForCondition",
            ("condition", "RecordingStopped"),
            ("timeoutMs", 100),
            ("pollMs", 10));
        AssertContainsOrdinal(metResult, "Condition result: MET");
        AssertContainsOrdinal(metResult, "Met: true");
        AssertContainsOrdinal(metResult, "Condition: PreviewFramesActive");
        AssertContainsOrdinal(notMetResult, "Condition result: NOT MET");
        AssertContainsOrdinal(notMetResult, "Met: false");
        AssertContainsOrdinal(notMetResult, "TimeoutMs: 250");
        AssertContainsOrdinal(notMetResult, "PollMs: 25");
    }

    private static async Task McpWindowScreenshotTool_FormatsScreenshotResponses()
    {
        var screenshotTools = RequireMcpType("McpServer.Tools.WindowScreenshotTools");

        var failureText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\fail.png",
                "{\"Success\":false,\"Message\":\"window not available\"}")
            .ConfigureAwait(false);
        AssertEqual("window not available", failureText, "capture_window_screenshot failure message");

        var missingDataText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\missing.png",
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No screenshot data returned.", missingDataText, "capture_window_screenshot missing data");

        var successText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\window.png",
                """
                {
                  "Success": true,
                  "Data": {
                    "FilePath": "C:\\captures\\actual-window.png",
                    "CapturedWidth": 1280,
                    "CapturedHeight": 720,
                    "FileSizeBytes": 4096
                  }
                }
                """)
            .ConfigureAwait(false);
        AssertEqual(
            "Window screenshot saved: C:\\captures\\actual-window.png (1280x720, 4096 bytes)",
            successText,
            "capture_window_screenshot formatted success");
    }

    private static async Task McpWindowTools_RouteWindowActions()
    {
        var pipeName = NewMcpToolPipeName("window");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var windowTools = RequireMcpType("McpServer.Tools.WindowTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 9,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "snap_top_left",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "resize",
                            false,
                            null,
                            null,
                            1024,
                            768)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "move",
                            false,
                            42,
                            84,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "close",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            " close ",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "set_full_screen",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "open_recordings_folder",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                i => $$"""{"Success":true,"Message":"window command {{i}} ok"}""")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "WindowAction", ("action", "SnapTopLeft"));
        AssertCommandRequest(requests[1], "WindowAction", ("action", "Resize"), ("width", 1024), ("height", 768));
        AssertCommandRequest(requests[2], "WindowAction", ("action", "Move"), ("x", 42), ("y", 84));
        AssertCommandRequest(requests[3], "ArmClose", ("armed", true));
        AssertCommandRequest(requests[4], "WindowAction", ("action", "Close"));
        AssertCommandRequest(requests[5], "ArmClose", ("armed", true));
        AssertCommandRequest(requests[6], "WindowAction", ("action", "Close"));
        AssertCommandRequest(requests[7], "SetFullScreenEnabled", ("enabled", true));
        AssertCommandRequest(requests[8], "OpenRecordingsFolder");
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] WindowAction: window command 0 ok",
                "[OK] WindowAction: window command 1 ok",
                "[OK] WindowAction: window command 2 ok",
                "[OK] ArmClose: window command 3 ok",
                "[OK] WindowAction: window command 4 ok",
                "[OK] ArmClose: window command 5 ok",
                "[OK] WindowAction: window command 6 ok",
                "[OK] SetFullScreenEnabled: window command 7 ok",
                "[OK] OpenRecordingsFolder: window command 8 ok"),
            result,
            "window_action ordered formatted output");
    }

    private static async Task McpPreviewTools_RoutePreviewToggle()
    {
        var pipeName = NewMcpToolPipeName("preview");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var previewTools = RequireMcpType("McpServer.Tools.PreviewTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewTools,
                            "control_preview",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"preview started\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetPreviewEnabled", ("enabled", true));
        AssertEqual("[OK] SetPreviewEnabled: preview started", result, "control_preview formatted success");
    }

    private static async Task McpFlashbackTools_RouteEnableToggle()
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

        var flashbackToolsText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackToolsText, "if (string.IsNullOrWhiteSpace(action))");
        AssertContains(flashbackToolsText, "Flashback action is required. Expected play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points.");
        AssertContains(flashbackToolsText, "normalizedAction is not (\"play\" or \"pause\" or \"go-live\" or \"seek\" or \"begin-scrub\" or \"update-scrub\" or \"end-scrub\" or \"set-in-point\" or \"set-out-point\" or \"clear-in-out-points\")");
        AssertContains(flashbackToolsText, "Flashback action must be one of: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points.");
        AssertContains(flashbackToolsText, "normalizedAction == \"begin-scrub\"");
        AssertContains(flashbackToolsText, "normalizedAction == \"update-scrub\"");
        AssertContains(flashbackToolsText, "Flashback seek, begin_scrub, and update_scrub require positionMs.");
        AssertContains(flashbackToolsText, "if (!double.IsFinite(positionMs.Value) ||\n                positionMs.Value < 0 ||\n                positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds)");
        AssertContains(flashbackToolsText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(flashbackToolsText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(flashbackToolsText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(flashbackToolsText, "AutomationSnapshotFormatter.Get(data, \"FailureKind\", string.Empty)");
        AssertContains(flashbackToolsText, "FailureKind: {failureKind}");

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
    }

    private static async Task<string> InvokeWindowScreenshotAsync(
        Type screenshotTools,
        string outputPath,
        string responseJson)
    {
        var pipeName = NewMcpToolPipeName("screenshot");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            screenshotTools,
                            "capture_window_screenshot",
                            pipeClient,
                            outputPath)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "CaptureWindowScreenshot", ("outputPath", outputPath));
        return result;
    }

}
