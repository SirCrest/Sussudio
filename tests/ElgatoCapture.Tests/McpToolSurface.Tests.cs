using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpCaptureSettingsTools_RouteProvidedSettings()
    {
        var pipeName = NewMcpToolPipeName("capture");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var captureSettingsTools = RequireMcpType("McpServer.Tools.CaptureSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            captureSettingsTools,
            "configure_capture",
            pipeClient,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No capture setting changes requested.", empty, "configure_capture empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 9,
                () => InvokeMcpToolStringAsync(
                    captureSettingsTools,
                    "configure_capture",
                    pipeClient,
                    "3840x2160",
                    59.94d,
                    "MJPG",
                    "Hevc",
                    "High",
                    80d,
                    "P5",
                    "ForcedOn",
                    4))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetResolution", ("resolution", "3840x2160"));
        AssertCommandRequest(requests[1], "SetFrameRate", ("frameRate", 59.94d));
        AssertCommandRequest(requests[2], "SetVideoFormat", ("videoFormat", "MJPG"));
        AssertCommandRequest(requests[3], "SetRecordingFormat", ("format", "Hevc"));
        AssertCommandRequest(requests[4], "SetQuality", ("quality", "High"));
        AssertCommandRequest(requests[5], "SetCustomBitrate", ("bitrateMbps", 80d));
        AssertCommandRequest(requests[6], "SetPreset", ("preset", "P5"));
        AssertCommandRequest(requests[7], "SetSplitEncodeMode", ("splitEncodeMode", "ForcedOn"));
        AssertCommandRequest(requests[8], "SetMjpegDecoderCount", ("decoderCount", 4));
    }

    private static async Task McpHostToolSchema_UsesPipeClientAsService()
    {
        var assemblyPath = Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll");
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(assemblyPath);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        });

        try
        {
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"ElgatoCapture.Tests","version":"1.0"}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await ReadJsonRpcResponseAsync(process, 1, cts.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","method":"notifications/initialized","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var toolsListDocument = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var tools = toolsListDocument.RootElement.GetProperty("result").GetProperty("tools");
            AssertNoToolSchemaExposesPipeClient(tools);
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
        }
    }

    private static async Task McpRecordingTools_RouteRecordingToggle()
    {
        var pipeName = NewMcpToolPipeName("recording");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var recordingTools = RequireMcpType("McpServer.Tools.RecordingTools");

        string successResult = string.Empty;
        string failureResult = string.Empty;
        string missingMessageResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 3,
                async () =>
                {
                    successResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                    failureResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                    missingMessageResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                },
                i => i switch
                {
                    0 => "{\"Success\":true,\"Message\":\"recording started\"}",
                    1 => "{\"Success\":false,\"Message\":\"stop failed\"}",
                    _ => "{\"Success\":false}"
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetRecordingEnabled", ("enabled", true));
        AssertCommandRequest(requests[1], "SetRecordingEnabled", ("enabled", false));
        AssertCommandRequest(requests[2], "SetRecordingEnabled", ("enabled", false));
        AssertEqual("[OK] SetRecordingEnabled: recording started", successResult, "control_recording formatted success");
        AssertEqual("[ERROR] SetRecordingEnabled: stop failed", failureResult, "control_recording formatted failure");
        AssertEqual("[ERROR] SetRecordingEnabled: No message.", missingMessageResult, "control_recording missing message fallback");
    }

    private static async Task McpToolCommandFormatter_BatchesPendingCommands()
    {
        var pipeName = NewMcpToolPipeName("formatter");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var formatterType = RequireMcpType("McpServer.Tools.ToolCommandFormatter");
        var optional = formatterType.GetMethod(
                "Optional",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types:
                [
                    typeof(string),
                    typeof(string),
                    typeof(bool),
                    typeof(Dictionary<string, object?>)
                ],
                modifiers: null)
            ?? throw new InvalidOperationException("ToolCommandFormatter.Optional overload was not found.");
        var pendingType = optional.ReturnType;
        var executeBatch = formatterType.GetMethod(
                "ExecuteBatchAsync",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types:
                [
                    pipeClient.GetType(),
                    typeof(string),
                    pendingType.MakeArrayType()
                ],
                modifiers: null)
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync was not found.");
        var emptyCommands = Array.CreateInstance(pendingType, 0);
        var emptyResult = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", emptyCommands).ConfigureAwait(false);
        AssertEqual("nothing to do", emptyResult, "ToolCommandFormatter empty batch result");

        var skipped = optional.Invoke(
            null,
            new object?[]
            {
                "SetShowAllCaptureOptions",
                "SetShowAllCaptureOptions",
                false,
                new Dictionary<string, object?> { ["enabled"] = true }
            });
        var firstPending = optional.Invoke(
            null,
            new object?[]
            {
                "SetStatsVisible",
                "SetStatsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = true }
            });
        var secondPending = optional.Invoke(
            null,
            new object?[]
            {
                "SetSettingsVisible",
                "SetSettingsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = false }
            });
        var commands = Array.CreateInstance(pendingType, 3);
        commands.SetValue(skipped, 0);
        commands.SetValue(firstPending, 1);
        commands.SetValue(secondPending, 2);

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    result = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", commands).ConfigureAwait(false);
                },
                i => i == 0
                    ? "{\"Success\":true,\"Message\":\"stats updated\"}"
                    : "{\"Success\":false,\"Message\":\"settings blocked\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetStatsVisible", ("visible", true));
        AssertCommandRequest(requests[1], "SetSettingsVisible", ("visible", false));
        AssertEqual(
            "[OK] SetStatsVisible: stats updated" + Environment.NewLine + "[ERROR] SetSettingsVisible: settings blocked",
            result,
            "ToolCommandFormatter ordered joined batch result");
    }

    private static async Task McpDeviceTools_RouteRefreshSelectionsAndCustomAudio()
    {
        var pipeName = NewMcpToolPipeName("device");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var deviceTools = RequireMcpType("McpServer.Tools.DeviceTools");

        var empty = await InvokeMcpToolStringAsync(
            deviceTools,
            "configure_device",
            pipeClient,
            null,
            null,
            null,
            null,
            false,
            null).ConfigureAwait(false);
        AssertEqual("No device configuration changes requested.", empty, "configure_device empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 4,
                () => InvokeMcpToolStringAsync(
                    deviceTools,
                    "configure_device",
                    pipeClient,
                    "capture-id",
                    "Capture Name",
                    "audio-id",
                    "Audio Name",
                    true,
                    true))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "RefreshDevices");
        AssertCommandRequest(
            requests[1],
            "SelectDevice",
            ("deviceId", "capture-id"),
            ("deviceName", "Capture Name"));
        AssertCommandRequest(
            requests[2],
            "SelectAudioInputDevice",
            ("deviceId", "audio-id"),
            ("deviceName", "Audio Name"));
        AssertCommandRequest(requests[3], "SetCustomAudioInput", ("enabled", true));
    }

    private static async Task McpPipelineSettingsTools_RoutePipelineAndAudioCommands()
    {
        var pipeName = NewMcpToolPipeName("pipeline");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var pipelineTools = RequireMcpType("McpServer.Tools.PipelineSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            pipelineTools,
            "configure_pipeline",
            pipeClient,
            null,
            null,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No pipeline setting changes requested.", empty, "configure_pipeline empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 7,
                async () =>
                {
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_pipeline",
                        pipeClient,
                        true,
                        false,
                        true,
                        false,
                        @"C:\captures").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_audio_mode",
                        pipeClient,
                        "Analog").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_analog_gain",
                        pipeClient,
                        42.5d).ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetHdrEnabled", ("enabled", true));
        AssertCommandRequest(requests[1], "SetTrueHdrPreviewEnabled", ("enabled", false));
        AssertCommandRequest(requests[2], "SetAudioEnabled", ("enabled", false));
        AssertCommandRequest(requests[3], "SetAudioPreviewEnabled", ("enabled", true));
        AssertCommandRequest(requests[4], "SetOutputPath", ("outputPath", @"C:\captures"));
        AssertCommandRequest(requests[5], "SetDeviceAudioMode", ("mode", "analog"));
        AssertCommandRequest(requests[6], "SetAnalogAudioGain", ("gain", 42.5d));
    }

    private static async Task McpUiSettingsTools_RouteUiCommands()
    {
        var pipeName = NewMcpToolPipeName("ui");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var uiSettingsTools = RequireMcpType("McpServer.Tools.UiSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            uiSettingsTools,
            "configure_ui",
            pipeClient,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No UI setting changes requested.", empty, "configure_ui empty result");

        var results = new List<string>();
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 5,
                async () =>
                {
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_ui",
                            pipeClient,
                            true,
                            33.5d,
                            false)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_settings_panel",
                            pipeClient,
                            true)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_stats_section",
                            pipeClient,
                            "Source",
                            false)
                        .ConfigureAwait(false));
                    result = string.Join(Environment.NewLine, results);
                },
                i => $$"""{"Success":true,"Message":"ui command {{i}} ok"}""")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetShowAllCaptureOptions", ("enabled", true));
        AssertCommandRequest(requests[1], "SetPreviewVolume", ("previewVolumePercent", 33.5d));
        AssertCommandRequest(requests[2], "SetStatsVisible", ("visible", false));
        AssertCommandRequest(requests[3], "SetSettingsVisible", ("visible", true));
        AssertCommandRequest(requests[4], "SetStatsSectionVisible", ("section", "Source"), ("visible", false));
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] SetShowAllCaptureOptions: ui command 0 ok",
                "[OK] SetPreviewVolume: ui command 1 ok",
                "[OK] SetStatsVisible: ui command 2 ok",
                "[OK] SetSettingsVisible: ui command 3 ok",
                "[OK] SetStatsSectionVisible: ui command 4 ok"),
            result,
            "MCP UI command formatted output");
    }

    private static async Task McpVerificationTools_FormatVerificationResponses()
    {
        var verificationTools = RequireMcpType("McpServer.Tools.VerificationTools");

        var blankAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-empty")),
            string.Empty).ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", blankAssertions, "assert_snapshot blank input");

        var invalidAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-invalid")),
            "{\"field\":\"IsRecording\"}").ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", invalidAssertions, "assert_snapshot non-array input");

        var recordingResult = string.Empty;
        var fileResult = string.Empty;
        var assertResult = string.Empty;
        var missingRecordingResult = string.Empty;
        var missingFileResult = string.Empty;
        var pipeName = NewMcpToolPipeName("verification");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 5,
                async () =>
                {
                    recordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    fileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\clip.mp4")
                        .ConfigureAwait(false);
                    assertResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "assert_snapshot",
                            pipeClient,
                            """[{"field":"IsRecording","op":"eq","value":false}]""")
                        .ConfigureAwait(false);
                    missingRecordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    missingFileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\missing.mp4")
                        .ConfigureAwait(false);
                },
                i => i switch
                {
                    0 => """
                         {
                           "Success": true,
                           "Message": "last recording verified",
                           "Data": {
                             "Verification": {
                               "OutputPath": "C:\\captures\\latest.mp4",
                               "FileExists": true,
                               "FileSizeBytes": 123456,
                               "VerificationMode": "LastRecording",
                               "DetectedVideoCodec": "hevc",
                               "DetectedPixelFormat": "p010le",
                               "DetectedWidth": 3840,
                               "DetectedHeight": 2160,
                               "DetectedFrameRate": 59.94,
                               "HdrVerificationLevel": "Strict",
                               "HdrMetadataPresent": true,
                               "HdrColorimetryValid": true,
                               "HdrMasteringMetadataPresent": false,
                               "Mismatches": []
                             }
                           }
                         }
                         """,
                    1 => """
                         {
                           "Success": false,
                           "Message": "file mismatch",
                           "Snapshot": {
                             "LastVerification": {
                               "FileExists": true,
                               "FileSizeBytes": 42,
                               "DetectedVideoCodec": "h264",
                               "DetectedPixelFormat": "yuv420p",
                               "DetectedWidth": 1920,
                               "DetectedHeight": 1080,
                               "DetectedFrameRate": 30
                             }
                           }
                         }
                         """,
                    2 => """
                         {
                           "Success": false,
                           "Message": "1 assertion failed",
                           "Data": {
                             "assertions": 1,
                             "passed": false,
                             "failures": ["IsRecording expected false"]
                           }
                         }
                         """,
                    3 => "{\"Success\":true,\"Message\":\"no verification data\"}",
                    _ => "{\"Success\":false,\"Message\":\"file not found\"}"
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "VerifyLastRecording");
        AssertCommandRequest(requests[1], "VerifyFile", ("filePath", @"C:\captures\clip.mp4"));
        AssertEqual(GetExpectedAutomationCommandValue("AssertSnapshot"), requests[2].GetProperty("command").GetInt32(), "AssertSnapshot command id");
        AssertCommandRequest(requests[3], "VerifyLastRecording");
        AssertCommandRequest(requests[4], "VerifyFile", ("filePath", @"C:\captures\missing.mp4"));
        var assertPayload = requests[2].GetProperty("payload");
        AssertJsonObjectPropertyNames(assertPayload, "assertions");
        var assertions = requests[2].GetProperty("payload").GetProperty("assertions");
        AssertEqual(JsonValueKind.Array, assertions.ValueKind, "AssertSnapshot assertions payload kind");
        AssertEqual(1, assertions.GetArrayLength(), "AssertSnapshot assertions payload count");
        var assertion = assertions[0];
        AssertJsonObjectPropertyNames(assertion, "field", "op", "value");
        AssertEqual("IsRecording", assertion.GetProperty("field").GetString(), "AssertSnapshot field payload");
        AssertEqual("eq", assertion.GetProperty("op").GetString(), "AssertSnapshot op payload");
        AssertEqual(JsonValueKind.False, assertion.GetProperty("value").ValueKind, "AssertSnapshot value payload kind");

        AssertContains(recordingResult, "== Recording Verification: PASS ==");
        AssertContainsOrdinal(recordingResult, "Output: C:\\captures\\latest.mp4 | Exists: true | Size: 123456 bytes");
        AssertContains(recordingResult, "Mismatches: None");
        AssertContains(fileResult, "== File Verification: FAIL ==");
        AssertContainsOrdinal(fileResult, "File: C:\\captures\\clip.mp4 | Exists: true | Size: 42 bytes");
        AssertContains(assertResult, "Snapshot assertions: FAIL");
        AssertContains(assertResult, "Failures: IsRecording expected false");
        AssertEqual("no verification data", missingRecordingResult, "verify_recording missing verification fallback");
        AssertEqual("file not found", missingFileResult, "verify_file missing verification fallback");
    }

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
                expectedCount: 7,
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
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] WindowAction: window command 0 ok",
                "[OK] WindowAction: window command 1 ok",
                "[OK] WindowAction: window command 2 ok",
                "[OK] ArmClose: window command 3 ok",
                "[OK] WindowAction: window command 4 ok",
                "[OK] ArmClose: window command 5 ok",
                "[OK] WindowAction: window command 6 ok"),
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
    }

    private static async Task McpPreviewColorProbeTool_FormatsProbeResponses()
    {
        var previewColorProbeTool = RequireMcpType("McpServer.Tools.PreviewColorProbeTool");

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

    private static async Task McpVideoSourceProbeTool_FormatsProbeResponses()
    {
        var videoSourceProbeTool = RequireMcpType("McpServer.Tools.VideoSourceProbeTool");

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

    private static Process StartMcpServerProcess(string assemblyPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = GetRepoRoot(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(Path.GetFullPath(assemblyPath));

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process.");
        }

        return process;
    }

    private static async Task WriteJsonRpcLineAsync(Process process, string json, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteLineAsync(CompactJsonLine(json))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ReadJsonRpcResponseAsync(Process process, int id, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (line is null)
            {
                var exitText = process.HasExited ? $" Process exited with code {process.ExitCode}." : string.Empty;
                throw new InvalidOperationException($"MCP server closed stdout before response id {id}.{exitText}");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var responseId) ||
                responseId.ValueKind != JsonValueKind.Number ||
                responseId.GetInt32() != id)
            {
                document.Dispose();
                continue;
            }

            if (root.TryGetProperty("error", out var error))
            {
                var errorText = error.GetRawText();
                document.Dispose();
                throw new InvalidOperationException($"MCP server returned error for response id {id}: {errorText}");
            }

            return document;
        }
    }

    private static async Task StopMcpServerProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
            }
        }
        catch
        {
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        try
        {
            await process.WaitForExitAsync()
                .WaitAsync(TimeSpan.FromSeconds(3))
                .ConfigureAwait(false);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static void AssertNoToolSchemaExposesPipeClient(JsonElement tools)
    {
        var checkedCount = 0;
        foreach (var tool in tools.EnumerateArray())
        {
            checkedCount++;
            var toolName = tool.GetProperty("name").GetString() ?? "<unnamed>";
            var inputSchema = tool.GetProperty("inputSchema");
            if (inputSchema.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("pipeClient", out _))
            {
                throw new InvalidOperationException($"{toolName} exposes pipeClient in the MCP input schema.");
            }

            if (inputSchema.TryGetProperty("required", out var required))
            {
                foreach (var item in required.EnumerateArray())
                {
                    if (string.Equals(item.GetString(), "pipeClient", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"{toolName} requires pipeClient in the MCP input schema.");
                    }
                }
            }
        }

        if (checkedCount == 0)
        {
            throw new InvalidOperationException("MCP host did not list any tools.");
        }
    }

    private static Type RequireMcpType(string typeName)
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in McpServer.dll.");
    }

    private static object CreateMcpPipeClient(string pipeName)
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(
                   type,
                   BindingFlags.Instance | BindingFlags.NonPublic,
                   binder: null,
                   args: new object?[] { pipeName },
                   culture: null)
               ?? throw new InvalidOperationException("Failed to create MCP PipeClient.");
    }

    private static async Task<string> InvokeMcpToolStringAsync(Type type, string methodName, params object?[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} was not found.");
        var task = method.Invoke(null, args) as Task<string>
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return Task<string>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<string> InvokeFormatterBatchAsync(
        MethodInfo executeBatch,
        object pipeClient,
        string emptyMessage,
        Array commands)
    {
        var task = executeBatch.Invoke(null, new object?[] { pipeClient, emptyMessage, commands }) as Task<string>
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync did not return Task<string>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement[]> CapturePipeRequestsAsync(
        string pipeName,
        int expectedCount,
        Func<Task> clientAction,
        Func<int, string>? responseFactory = null)
    {
        var requests = new List<JsonElement>();
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        for (var i = 0; i < expectedCount; i++)
        {
            using var serverPipe = new System.IO.Pipes.NamedPipeServerStream(
                pipeName,
                System.IO.Pipes.PipeDirection.InOut,
                1,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            var connectTask = serverPipe.WaitForConnectionAsync(cts.Token);
            if (await Task.WhenAny(connectTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                if (clientTask.IsFaulted || clientTask.IsCanceled)
                {
                    await clientTask.ConfigureAwait(false);
                }

                throw new InvalidOperationException(
                    $"Expected pipe request {i + 1} of {expectedCount}, but the client action completed after {requests.Count} request(s).");
            }

            try
            {
                await connectTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    $"Expected pipe request {i + 1} of {expectedCount}, but no connection arrived after {requests.Count} request(s).",
                    ex);
            }

            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                if (clientTask.IsFaulted || clientTask.IsCanceled)
                {
                    await clientTask.ConfigureAwait(false);
                }

                throw new InvalidOperationException(
                    $"Expected request payload {i + 1} of {expectedCount}, but the client action completed after {requests.Count} complete request(s).");
            }

            string? requestLine;
            try
            {
                requestLine = await readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    $"Expected request payload {i + 1} of {expectedCount}, but no payload arrived after {requests.Count} complete request(s).",
                    ex);
            }

            if (requestLine is null)
            {
                throw new InvalidOperationException(
                    $"Expected request payload {i + 1} of {expectedCount}, but the pipe closed after {requests.Count} complete request(s).");
            }

            using var document = JsonDocument.Parse(requestLine);
            requests.Add(document.RootElement.Clone());

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(CompactJsonLine(responseFactory?.Invoke(i) ?? "{\"Success\":true,\"Message\":\"ok\"}"))
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, expectedCount, requests.Count, clientTask, cts.Token).ConfigureAwait(false);
        return requests.ToArray();
    }

    private static async Task EnsureNoUnexpectedPipeRequestAsync(
        string pipeName,
        int expectedCount,
        int capturedCount,
        Task clientTask,
        CancellationToken cancellationToken)
    {
        using var extraServerPipe = new System.IO.Pipes.NamedPipeServerStream(
            pipeName,
            System.IO.Pipes.PipeDirection.InOut,
            1,
            System.IO.Pipes.PipeTransmissionMode.Byte,
            System.IO.Pipes.PipeOptions.Asynchronous);

        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var extraConnectTask = extraServerPipe.WaitForConnectionAsync(probeCts.Token);
        var completed = await Task.WhenAny(clientTask, extraConnectTask).ConfigureAwait(false);
        if (completed == clientTask)
        {
            probeCts.Cancel();
            try
            {
                await extraConnectTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            await clientTask.ConfigureAwait(false);
            return;
        }

        try
        {
            await extraConnectTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new TimeoutException(
                $"Client action did not complete after {capturedCount} expected request(s).",
                ex);
        }

        using var reader = new StreamReader(extraServerPipe, leaveOpen: true);
        var extraRequestLine = await reader.ReadLineAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        using var writer = new StreamWriter(extraServerPipe, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync("{\"Success\":true,\"Message\":\"unexpected request acknowledged\"}")
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        Exception? clientException = null;
        try
        {
            await clientTask.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
        catch (Exception ex)
        {
            clientException = ex;
        }

        var message =
            $"Unexpected pipe request {expectedCount + 1} received after the expected {expectedCount} request(s): {extraRequestLine ?? "<no payload>"}";
        if (clientException is not null)
        {
            throw new InvalidOperationException(message, clientException);
        }

        throw new InvalidOperationException(message);
    }

    private static string NewMcpToolPipeName(string suffix)
        => $"ec-mcp-{suffix}-{Guid.NewGuid():N}";

    private static string CompactJsonLine(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static void AssertCommandRequest(JsonElement request, string commandName, params (string Key, object? Value)[] expectedPayload)
    {
        AssertEqual(GetExpectedAutomationCommandValue(commandName), request.GetProperty("command").GetInt32(), $"{commandName} command id");
        var payload = request.GetProperty("payload");
        if (expectedPayload.Length == 0)
        {
            if (payload.ValueKind == JsonValueKind.Object && payload.EnumerateObject().Any())
            {
                throw new InvalidOperationException($"{commandName} payload contained unexpected properties.");
            }

            if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{commandName} payload had unexpected kind {payload.ValueKind}.");
            }

            return;
        }

        AssertJsonObjectPropertyNames(payload, expectedPayload.Select(item => item.Key).ToArray());
        foreach (var (key, value) in expectedPayload)
        {
            AssertJsonPropertyEquals(payload, key, value, $"{commandName}.{key}");
        }
    }

    private static void AssertContainsOrdinal(string value, string token)
    {
        if (!value.Contains(token, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}' with ordinal casing.");
        }
    }

    private static void AssertJsonObjectPropertyNames(JsonElement element, params string[] expectedPropertyNames)
    {
        AssertEqual(JsonValueKind.Object, element.ValueKind, "JSON object property-name assertion kind");
        var actual = element.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedPropertyNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        AssertEqual(string.Join(",", expected), string.Join(",", actual), "JSON object property names");
    }

    private static int GetExpectedAutomationCommandValue(string commandName)
    {
        foreach (var (name, value) in ExpectedAutomationCommands())
        {
            if (string.Equals(name, commandName, StringComparison.Ordinal))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Expected automation command '{commandName}' was not found.");
    }

    private static void AssertJsonPropertyEquals(JsonElement element, string propertyName, object? expected, string fieldName)
    {
        if (!element.TryGetProperty(propertyName, out var actual))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: property was missing.");
        }

        switch (expected)
        {
            case null:
                AssertEqual(JsonValueKind.Null, actual.ValueKind, fieldName);
                break;
            case bool expectedBool:
                AssertEqual(expectedBool, actual.GetBoolean(), fieldName);
                break;
            case int expectedInt:
                AssertEqual(expectedInt, actual.GetInt32(), fieldName);
                break;
            case double expectedDouble:
                AssertEqual(expectedDouble, actual.GetDouble(), fieldName);
                break;
            case string expectedString:
                AssertEqual(expectedString, actual.GetString(), fieldName);
                break;
            default:
                throw new InvalidOperationException($"Unsupported expected JSON value type for {fieldName}: {expected.GetType().FullName}.");
        }
    }
}
