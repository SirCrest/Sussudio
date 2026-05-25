using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpPreviewTools_RoutePreviewToggle()
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

    internal static async Task McpWindowScreenshotTool_FormatsScreenshotResponses()
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

    internal static async Task McpWindowTools_RouteWindowActions()
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

    internal static Task McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits()
    {
        var waitToolsSource = ReadRepoFile("tools/McpServer/Tools/WindowTools.cs");
        AssertContains(waitToolsSource, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertContains(waitToolsSource, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertDoesNotContain(waitToolsSource, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsSource, "SendCommandAsync(\"WaitForCondition\"");
        AssertDoesNotContain(waitToolsSource, "AutomationPipeProtocol.DefaultResponseTimeoutMs");

        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");
        var timeoutMethod = RequireNonPublicStaticMethod(waitTools, "GetWaitForConditionResponseTimeoutMs");

        AssertEqual(
            Sussudio.Tools.AutomationPipeProtocol.ExtendedResponseTimeoutMs,
            (int)timeoutMethod.Invoke(null, new object[] { 10000 })!,
            "MCP wait default pipe response timeout follows catalog policy");
        AssertEqual(
            65000,
            (int)timeoutMethod.Invoke(null, new object[] { 60000 })!,
            "MCP wait explicit timeout keeps response buffer");
        AssertEqual(
            int.MaxValue,
            (int)timeoutMethod.Invoke(null, new object[] { int.MaxValue })!,
            "MCP wait response timeout saturates on extreme input");

        return Task.CompletedTask;
    }

    internal static async Task McpWaitTools_RouteConditionWaits()
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
        var flashbackToolsActionText = flashbackToolsRootText;
        var flashbackToolsExportText = flashbackToolsRootText;
        AssertContains(flashbackToolsRootText, "[McpServerToolType]");
        AssertContains(flashbackToolsRootText, "public static class FlashbackTools");
        AssertDoesNotContain(flashbackToolsRootText, "public static partial class FlashbackTools");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_enabled");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_apply");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_segments");
        AssertContains(flashbackToolsRootText, "FlashbackGetSegments");
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

    internal static async Task McpPreviewColorProbeTool_FormatsProbeResponses()
    {
        var previewColorProbeTool = RequireMcpType("McpServer.Tools.PreviewColorProbeTools");

        var failureText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "probe_preview_color failure message");

        var missingDataText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_preview_color missing data");

        var inactiveText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Preview Color Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active preview session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "RendererMode": "D3D11VideoProcessor",
                             "NegotiatedSubtype": "P010",
                             "SourceWidth": 3840,
                             "SourceHeight": 2160,
                             "SourceFrameRate": 59.94,
                             "NominalRangeLabel": "Full",
                             "NominalRange": 2,
                             "TransferFunctionLabel": "PQ",
                             "TransferFunction": 16,
                             "VideoPrimariesLabel": "BT.2020",
                             "VideoPrimaries": 9,
                             "YuvMatrixLabel": "BT.2020",
                             "YuvMatrix": 9,
                             "D3DInputColorSpace": "BT2020_PQ",
                             "D3DOutputColorSpace": "RGB_Full",
                             "LumaSampleCount": 100,
                             "LumaMin": 0,
                             "LumaMax": 255,
                             "LumaMean": 128.5,
                             "LumaBelow16Count": 5,
                             "LumaAbove235Count": 10,
                             "FormatProperties": {
                               "MF_MT_SUBTYPE": "P010"
                             }
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            activeText = await InvokePreviewColorProbeAsync(previewColorProbeTool, activeJson).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(activeText, "Renderer: D3D11VideoProcessor");
        AssertContains(activeText, "Format: P010 3840x2160 @ 59.94fps");
        AssertContains(activeText, "== Color Attributes ==");
        AssertContains(activeText, "Nominal Range: Full (raw=2)");
        AssertContains(activeText, "== D3D11 Video Processor ==");
        AssertContains(activeText, "== Luma (Y Plane) Analysis ==");
        AssertContains(activeText, "Diagnosis: Data uses FULL range (0-255). 10.0% super-white, 5.0% super-black.");
        AssertContains(activeText, "== Raw MF Properties ==");
        AssertContains(activeText, "MF_MT_SUBTYPE = P010");
    }

    internal static async Task McpVideoSourceProbeTool_FormatsProbeResponses()
    {
        var videoSourceProbeTool = RequireMcpType("McpServer.Tools.VideoSourceProbeTools");

        var failureText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":false,\"Message\":\"source unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("source unavailable", failureText, "probe_video_source failure message");

        var missingDataText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_video_source missing data");

        var inactiveText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Video Source Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active ingest session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "MemoryPreference": "D3D11",
                             "CurrentSubtype": "P010",
                             "CurrentWidth": 3840,
                             "CurrentHeight": 2160,
                             "CurrentFrameRate": 59.94,
                             "P010Available": true,
                             "Nv12Available": true,
                             "SupportedSubtypes": ["P010", "NV12", ""],
                             "TotalFormatCount": 2,
                             "Formats": [
                               { "Summary": "3840x2160 P010 59.94fps" },
                               { "Summary": "1920x1080 NV12 60fps" }
                             ]
                           }
                         }
                         """;
        var activeText = await InvokeVideoSourceProbeAsync(videoSourceProbeTool, activeJson).ConfigureAwait(false);
        AssertContains(activeText, "Memory Preference: D3D11");
        AssertContains(activeText, "Current Format: P010 3840x2160@59.94fps");
        AssertContainsOrdinal(activeText, "P010 Available: true | NV12 Available: true");
        AssertContains(activeText, "Supported Subtypes: P010, NV12");
        AssertContains(activeText, "Total Format Count: 2");
        AssertContains(activeText, "== Format Table ==");
        AssertContains(activeText, "[0] 3840x2160 P010 59.94fps");
        AssertContains(activeText, "[1] 1920x1080 NV12 60fps");
    }

    private static async Task<string> InvokePreviewColorProbeAsync(Type previewColorProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("color");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewColorProbeTool,
                            "probe_preview_color",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbePreviewColor");
        return result;
    }

    private static async Task<string> InvokeVideoSourceProbeAsync(Type videoSourceProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("source");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            videoSourceProbeTool,
                            "probe_video_source",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbeVideoSource");
        return result;
    }

    internal static async Task McpPreviewFrameCaptureTool_FormatsCaptureResponses()
    {
        var previewFrameCaptureTool = RequireMcpType("McpServer.Tools.PreviewFrameCaptureTools");
        var defaultOutputPath = Path.Combine(Environment.CurrentDirectory, "temp", "preview_capture.bmp");

        var failureText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-failure",
                outputPath: null,
                expectedOutputPath: defaultOutputPath,
                responseJson: "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "capture_preview_frame failure message");

        var missingDataText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-missing",
                outputPath: @"C:\captures\missing.bmp",
                expectedOutputPath: @"C:\captures\missing.bmp",
                responseJson: "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No frame capture data returned.", missingDataText, "capture_preview_frame missing data");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "FilePath": "C:\\captures\\preview.bmp",
                             "CapturedWidth": 640,
                             "CapturedHeight": 360,
                             "RendererMode": "D3D11",
                             "AverageR": 10,
                             "AverageG": 20,
                             "AverageB": 30,
                             "AverageLuminance": 25.5,
                             "MinLuminance": 10,
                             "MaxLuminance": 34,
                             "NearBlackPercent": 12.5,
                             "NearWhitePercent": 0,
                             "PureBlackPercent": 96.5,
                             "LetterboxTopRows": 12,
                             "LetterboxBottomRows": 12,
                             "PillarboxLeftCols": 3,
                             "PillarboxRightCols": 4,
                             "ContentWidth": 640,
                             "ContentHeight": 360,
                             "ContentAspectRatio": 1.333,
                             "TotalPixels": 230400,
                             "LuminanceHistogram": [0, 10, 20, 40, 80, 160, 80, 40, 20, 10, 5, 0, 0, 0, 0, 0]
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            activeText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-active",
                    outputPath: @"C:\captures\preview.bmp",
                    expectedOutputPath: @"C:\captures\preview.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertEqual(
            """
            == Preview Frame Capture ==
            File: C:\captures\preview.bmp
            Resolution: 640 x 360
            Renderer: D3D11

            == Pixel Summary ==
            Average RGB: R=10 G=20 B=30
            Luminance: avg=25.5 min=10 max=34
            Near Black (<16): 12.5%
            Near White (>240): 0%
            Pure Black: 96.5%

            == Framing ==
            Letterbox: top=12 bottom=12 rows
            Pillarbox: left=3 right=4 cols
            Content Area: 640 x 360
            Content Aspect Ratio: 1.333
            Total Pixels: 230400

            == Luminance Histogram (16 bins) ==
              0- 15:  (0)
             16- 31: ## (10)
             32- 47: ### (20)
             48- 63: ###### (40)
             64- 79: ############ (80)
             80- 95: ######################## (160)
             96-111: ############ (80)
            112-127: ###### (40)
            128-143: ### (20)
            144-159: ## (10)
            160-175: # (5)
            176-191:  (0)
            192-207:  (0)
            208-223:  (0)
            224-239:  (0)
            240-255:  (0)

            == Diagnosis ==
            - BLANK FRAME: >95% of pixels are pure black.
            - VERY DARK: average luminance is below 30.
            - LETTERBOXED: top=12, bottom=12, estimated source aspect=1.333 (640x360).
            - PILLARBOXED: left=3, right=4, estimated source aspect=1.333 (640x360).
            - LOW CONTRAST: luminance range is under 30.
            - ASPECT RATIO ALERT: content aspect 1.333 is not close to 16:9 or 16:10.
            """,
            activeText.Replace("\r\n", "\n"),
            "capture_preview_frame exact report");

        var noAnomalyJson = """
                            {
                              "Success": true,
                              "Data": {
                                "FilePath": "temp/preview_capture.bmp",
                                "CapturedWidth": 1920,
                                "CapturedHeight": 1080,
                                "RendererMode": "D3D11VideoProcessor",
                                "AverageR": 120,
                                "AverageG": 130,
                                "AverageB": 140,
                                "AverageLuminance": 128,
                                "MinLuminance": 0,
                                "MaxLuminance": 255,
                                "NearBlackPercent": 0,
                                "NearWhitePercent": 0,
                                "PureBlackPercent": 0,
                                "LetterboxTopRows": 0,
                                "LetterboxBottomRows": 0,
                                "PillarboxLeftCols": 0,
                                "PillarboxRightCols": 0,
                                "ContentWidth": 1920,
                                "ContentHeight": 1080,
                                "ContentAspectRatio": 1.777,
                                "TotalPixels": 2073600
                              }
                            }
                            """;
        var noAnomalyText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-no-anomaly",
                outputPath: "temp/preview_capture.bmp",
                expectedOutputPath: "temp/preview_capture.bmp",
                responseJson: noAnomalyJson)
            .ConfigureAwait(false);
        AssertContains(noAnomalyText, "Histogram unavailable.");
        AssertContains(noAnomalyText, "No obvious anomalies detected.");

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var frenchCultureText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-culture",
                    outputPath: "temp/preview_capture_culture.bmp",
                    expectedOutputPath: "temp/preview_capture_culture.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
            AssertContains(frenchCultureText, "estimated source aspect=1.333 (640x360).");
            AssertContains(frenchCultureText, "content aspect 1.333 is not close to 16:9 or 16:10.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        var rootText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static class PreviewFrameCaptureTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_preview_frame");
        AssertContains(rootText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"preview_capture.bmp\")");
        AssertContains(rootText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(rootText, "BuildPreviewFrameCaptureText(data)");

        AssertContains(rootText, "private static string BuildPreviewFrameCaptureText(");
        AssertContains(rootText, "== Preview Frame Capture ==");
        AssertContains(rootText, "== Pixel Summary ==");
        AssertContains(rootText, "AppendLuminanceHistogram(builder, data)");
        AssertContains(rootText, "AppendPreviewFrameCaptureDiagnosis(builder, data)");
        AssertContains(rootText, "private static void AppendLuminanceHistogram(");
        AssertContains(rootText, "LuminanceHistogram");
        AssertContains(rootText, "while (bins.Count < 16)");
        AssertContains(rootText, "* 24.0");
        AssertContains(rootText, "new string('#', Math.Max(0, barLength))");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Histogram.cs")),
            "preview frame histogram rendering lives with the preview frame report renderer");

        AssertContains(rootText, "private static List<string> BuildPreviewFrameCaptureDiagnosis(");
        AssertContains(rootText, "pureBlackPercent > 95.0");
        AssertContains(rootText, "averageLuminance < 30.0");
        AssertContains(rootText, "averageLuminance > 230.0");
        AssertContains(rootText, "(maxLuminance - minLuminance) < 30.0");
        AssertContains(rootText, "private static string FormatAspectRatio(");
        AssertContains(rootText, "AutomationSnapshotFormatter.FormatNumber(aspectRatio, \"0.###\")");
        AssertContains(rootText, "private static bool IsNear(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Rendering.cs")),
            "preview frame report rendering lives with the preview frame MCP tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Diagnosis.cs")),
            "preview frame diagnosis policy lives with the preview frame MCP tool");
    }

    private static async Task<string> InvokePreviewFrameCaptureAsync(
        Type previewFrameCaptureTool,
        string pipeSuffix,
        string? outputPath,
        string expectedOutputPath,
        string responseJson)
    {
        var pipeName = NewMcpToolPipeName(pipeSuffix);
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewFrameCaptureTool,
                            "capture_preview_frame",
                            pipeClient,
                            outputPath)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "CapturePreviewFrame", ("outputPath", expectedOutputPath));
        return result;
    }
}
