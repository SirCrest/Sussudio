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

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
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
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
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

    private static async Task McpPipeClient_HonorsSussudioAutomationPipeEnvironment()
    {
        var pipeName = NewMcpToolPipeName("env");
        var previousPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", pipeName);
        try
        {
            var pipeClient = CreateDefaultMcpPipeClient();
            var appStateTools = RequireMcpType("McpServer.Tools.AppStateTools");

            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 1,
                    async () =>
                    {
                        _ = await InvokeMcpToolResultAsync(
                                appStateTools,
                                "get_app_state_raw",
                                pipeClient)
                            .ConfigureAwait(false);
                    },
                    _ => "{\"Success\":true,\"Snapshot\":{\"SessionState\":\"Ready\"}}")
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", previousPipeName);
        }
    }

    private static async Task McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport()
    {
        var assemblyPath = Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll");
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
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
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
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
                    {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_app_state","arguments":{}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var response = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var resultElement = response.RootElement.GetProperty("result");
            AssertEqual(true, resultElement.GetProperty("isError").GetBoolean(), "get_app_state pipe failure MCP isError");
            var content = resultElement.GetProperty("content");
            var text = content[0].GetProperty("text").GetString() ?? string.Empty;
            AssertContains(text, "Timed out connecting to automation pipe");
            AssertContains(text, "pipe-connect-timeout");

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            using var toolsListResponse = await ReadJsonRpcResponseAsync(process, 3, cts.Token).ConfigureAwait(false);
            AssertEqual(
                true,
                toolsListResponse.RootElement.GetProperty("result").GetProperty("tools").GetArrayLength() > 0,
                "MCP transport remains open after pipe failure");
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
                            @"C:\captures\clip.mp4",
                            "flashback-export")
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
                            @"C:\captures\missing.mp4",
                            null)
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
        AssertCommandRequest(requests[1], "VerifyFile", ("filePath", @"C:\captures\clip.mp4"), ("verificationProfile", "flashback-export"));
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

}
