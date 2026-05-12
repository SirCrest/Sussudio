using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Tests for MCP tool registration, command names, and surface compatibility.
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

    private static async Task McpDiagnosticSessionTool_RecordsSnapshotArtifacts()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-test-{Guid.NewGuid():N}");
        var result = string.Empty;
        object? toolResult = null;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": false,
                                 "IsRecording": false,
                                 "FlashbackActive": false,
                                 "DiagnosticHealthStatus": "Idle",
                                 "DiagnosticLikelyStage": "diagnostic_unavailable",
                                 "DiagnosticSummary": "Preview and recording are idle.",
                                 "DiagnosticEvidence": "Start preview or recording to collect live frame-lane diagnostics.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 4,
                                 "PreviewD3DFrameStatsFailureCount": 1,
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 7,
                                 "PreviewD3DFrameStatsFailureCount": 2,
                                 "PreviewD3DRecentSlowFrames": [
                                   {
                                     "SlowReason": "present_interval",
                                     "WorstOverBudgetMs": 1.5,
                                     "PresentIntervalMs": 9.8,
                                     "TotalFrameCpuMs": 4.2,
                                     "PresentCallMs": 0.7,
                                     "PendingFrameCount": 1
                                   }
                                 ],
                                 "FrameLedgerRecentEvents": [
                                   {
                                     "SourceSequence": 7,
                                     "Stage": "CaptureArrived",
                                     "QpcTimestamp": 123456,
                                     "Accepted": true
                                   }
                                 ]
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": [
                                 {
                                   "TimestampUtc": "2026-04-26T00:00:00Z",
                                   "PerformanceScore": 100
                                 }
                               ]
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(false, GetMcpToolResultIsError(toolResult), "diagnostic session success MCP isError");
            AssertContains(result, "== Diagnostic Session: PASS ==");
            AssertContains(result, "Health: Healthy | Stage: none");
            AssertContains(result, "Preview D3D Perf: onePercentLowFpsEnd=0 onePercentLowFpsMin=0 missedRefreshDelta=3 statsFailureDelta=1 maxRecentSlowFrames=1 latestSlowReason=present_interval overBudgetMs=1.5 presentIntervalMs=9.8 totalFrameCpuMs=4.2 presentCallMs=0.7 pending=1");
            AssertContains(result, "Frame Ledger:");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            var samplesPath = Path.Combine(outputDirectory, "samples.json");
            var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic session summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic session live artifact");
            AssertEqual(true, File.Exists(samplesPath), "diagnostic session samples artifact");
            AssertEqual(true, File.Exists(frameLedgerPath), "diagnostic session frame ledger artifact");
            AssertContains(result, $"Live: {livePath}");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic session terminal state");
            AssertEqual("summary", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic session last stage");
            AssertEqual(true, summaryDocument.RootElement.GetProperty("RunnerProcessId").GetInt32() > 0, "diagnostic session runner pid");
            AssertEqual(livePath, summaryDocument.RootElement.GetProperty("LivePath").GetString(), "diagnostic session live path");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("completed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic live terminal state");
            AssertEqual("summary-written", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic live last stage");
            AssertEqual("Healthy", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic live health status");
            AssertEqual("none", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic live likely stage");
            AssertEqual(0, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic live warning count");
            AssertEqual(string.Empty, liveDocument.RootElement.GetProperty("LastWarning").GetString(), "diagnostic live last warning");

            using var frameLedgerDocument = JsonDocument.Parse(File.ReadAllText(frameLedgerPath));
            AssertEqual(1, frameLedgerDocument.RootElement.GetProperty("EventCount").GetInt32(), "diagnostic session frame ledger event count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static async Task DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-failure-test-{Guid.NewGuid():N}");
        var getSnapshotCount = 0;

        try
        {
            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "observe");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotCount++;
                    if (getSnapshotCount == 3)
                    {
                        throw new InvalidOperationException("simulated final snapshot failure");
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
            var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic failure result success");
            AssertEqual("failed", GetPropertyValue(result, "TerminalState") as string, "diagnostic failure terminal state");
            AssertEqual("final-snapshot", GetPropertyValue(result, "LastStage") as string, "diagnostic failure last stage");
            AssertContains(GetPropertyValue(result, "UnhandledException") as string ?? string.Empty, "InvalidOperationException");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic failure summary success");
            AssertEqual("failed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure summary terminal state");
            AssertEqual("final-snapshot", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure summary last stage");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "final-snapshot");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("failed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure live terminal state");
            AssertEqual("final-snapshot", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure live last stage");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    private static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("DiagnosticSessionSample type was not found.");
        var buildObservation = healthPolicyType.GetMethod(
                "BuildSessionDiagnosticHealthObservation",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildSessionDiagnosticHealthObservation was not found.");

        var samples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Warning", "flashback_playback", "startup 1% low")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var finalSnapshot = CreateDiagnosticSnapshot("Healthy", "none", "final");
        var transientWarningObservation = buildObservation.Invoke(
                null,
                new object?[] { samples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Transient warning observation was null.");
        AssertEqual("Healthy", GetPropertyValue(transientWarningObservation, "HealthStatus") as string, "flashback warmup health status");
        AssertEqual("none", GetPropertyValue(transientWarningObservation, "LikelyStage") as string, "flashback warmup likely stage");

        var criticalSamples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Critical", "flashback_playback", "startup crash")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var criticalObservation = buildObservation.Invoke(
                null,
                new object?[] { criticalSamples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Critical observation was null.");
        AssertEqual("Critical", GetPropertyValue(criticalObservation, "HealthStatus") as string, "flashback critical health status");
        AssertEqual("flashback_playback", GetPropertyValue(criticalObservation, "LikelyStage") as string, "flashback critical likely stage");

        return Task.CompletedTask;

        static object CreateDiagnosticSessionSampleList(Type sampleType, params (long OffsetMs, JsonElement Snapshot)[] values)
        {
            var listType = typeof(List<>).MakeGenericType(sampleType);
            var list = (System.Collections.IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("DiagnosticSessionSample list could not be created."));
            foreach (var value in values)
            {
                var sample = Activator.CreateInstance(sampleType)
                    ?? throw new InvalidOperationException("DiagnosticSessionSample instance could not be created.");
                sampleType.GetProperty("OffsetMs")!.SetValue(sample, value.OffsetMs);
                sampleType.GetProperty("TimestampUtc")!.SetValue(sample, DateTimeOffset.UtcNow);
                sampleType.GetProperty("Snapshot")!.SetValue(sample, value.Snapshot);
                list.Add(sample);
            }

            return list;
        }

        static JsonElement CreateDiagnosticSnapshot(string health, string stage, string evidence)
        {
            using var document = JsonDocument.Parse($$"""
                {
                  "DiagnosticHealthStatus": "{{health}}",
                  "DiagnosticLikelyStage": "{{stage}}",
                  "DiagnosticEvidence": "{{evidence}}"
                }
                """);
            return document.RootElement.Clone();
        }
    }

    private static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadRepoFile("tools/Common/DiagnosticSessionModels.cs")
            .Replace("\r\n", "\n");

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed class DiagnosticSessionResult");
        AssertContains(modelText, "public sealed class DiagnosticSessionSample");
        AssertContains(modelText, "public string TerminalState { get; set; }");
        AssertContains(modelText, "public JsonElement Snapshot { get; init; }");
        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionOptions");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionSample");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var formatterText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterText, "public static class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterText, "private static string FormatFrameRate(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionText_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var formatterText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");
        var textHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionText.cs")
            .Replace("\r\n", "\n");

        AssertContains(textHelpersText, "internal static class DiagnosticSessionText");
        AssertContains(textHelpersText, "internal static string FormatOptional(string value)");
        AssertContains(textHelpersText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(formatterText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var retryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(artifactsText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(artifactsText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(artifactsText, "internal static object BuildFrameLedgerTrace(");
        AssertContains(artifactsText, "internal static bool TryGetSnapshot(");
        AssertContains(artifactsText, "internal static bool TryGetVerification(");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(runnerText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(runnerText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupText, "internal static class DiagnosticSessionCleanupPolicy");
        AssertContains(cleanupText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
        AssertDoesNotContain(runnerText, "private static void ValidateCleanupLifecycleRestored(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionSampler_OwnsSampleLoopOrdering()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var samplerText = ReadRepoFile("tools/Common/DiagnosticSessionSampler.cs")
            .Replace("\r\n", "\n");

        AssertContains(samplerText, "internal static class DiagnosticSessionSampler");
        AssertContains(samplerText, "internal static async Task SampleLoopAsync(");
        AssertContains(samplerText, "var response = await sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(samplerText, "samples.Add(new DiagnosticSessionSample");
        AssertContains(samplerText, "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertOccursBefore(samplerText, "samples.Add(new DiagnosticSessionSample", "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionSampler;");
        AssertDoesNotContain(runnerText, "private static async Task SampleLoopAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionMetrics_OwnsSessionMetricProjection()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("tools/Common/DiagnosticSessionMetrics.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "internal static class DiagnosticSessionMetrics");
        AssertContains(metricsText, "internal sealed class SourceCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewD3DMetrics");
        AssertContains(metricsText, "internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewD3DMetrics BuildPreviewD3DMetrics(");
        AssertContains(metricsText, "internal static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertContains(metricsText, "internal static long GetResetAwareCounterDelta(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionMetrics;");
        AssertDoesNotContain(runnerText, "private sealed class SourceCadenceSessionMetrics");
        AssertDoesNotContain(runnerText, "private sealed class PreviewD3DMetrics");
        AssertDoesNotContain(runnerText, "private static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertDoesNotContain(runnerText, "private static long GetCounterDelta(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "internal static class DiagnosticSessionFlashbackMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(metricsText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var exportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportsText, "internal static class DiagnosticSessionFlashbackExports");
        AssertContains(exportsText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportsText, "const string marker = \" from \";");
        AssertContains(exportsText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportsText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportsText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportsText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportsText, "\"SetAudioEnabled\"");
        AssertContains(exportsText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportsText, "\"clear-in-out-points\"");
        AssertContains(exportsText, "\"go-live\"");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static bool TryGetFlashbackSegments(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var waitsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.cs")
            .Replace("\r\n", "\n");

        AssertContains(waitsText, "internal static class DiagnosticSessionFlashbackWaits");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(waitsText, "FlashbackPlaybackPendingCommands");
        AssertContains(waitsText, "FlashbackPlaybackFrameCount");
        AssertContains(waitsText, "RecordingBackend");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "internal static class DiagnosticSessionFlashbackValidation");
        AssertContains(validationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(validationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(validationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(validationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(validationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(validationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(validationText, "\"flashback preview: present/display pressure \"");
        AssertContains(validationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var observationType = assembly.GetType("Sussudio.Tools.DiagnosticHealthObservation")
            ?? throw new InvalidOperationException("DiagnosticHealthObservation type was not found.");
        var sourceMetricsType = assembly.GetType("Sussudio.Tools.SourceCadenceSessionMetrics")
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics type was not found.");
        var sparseSourceWarning = healthPolicyType.GetMethod(
                "IsSparseSourceCaptureCadenceWarningRun",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sparse source-cadence classifier was not found.");

        var observation = Activator.CreateInstance(
                observationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "Warning", "source_capture", "source gaps=1 drops=1", 85_727L, 2 },
                culture: null)
            ?? throw new InvalidOperationException("DiagnosticHealthObservation instance could not be created.");
        var metrics = Activator.CreateInstance(sourceMetricsType, nonPublic: true)
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics instance could not be created.");
        sourceMetricsType.GetProperty("MaxSevereGapCountObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxDropPercentObserved")!.SetValue(metrics, 0.042);

        AssertEqual(
            true,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "sparse source cadence warning without source counter deltas");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 1L, 0L, 300, true })!,
            "source reader drop delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 1L, 300, true })!,
            "video ingest error delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, false })!,
            "unhealthy visual cadence blocks sparse source cadence tolerance");

        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 3L);
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "repeated source cadence drops block sparse source cadence tolerance");

        return Task.CompletedTask;
    }

    private static async Task McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session-failure");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-health-test-{Guid.NewGuid():N}");
        object? toolResult = null;
        var result = string.Empty;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Critical",
                                 "DiagnosticLikelyStage": "flashback_playback",
                                 "DiagnosticSummary": "Playback cadence collapsed.",
                                 "DiagnosticEvidence": "1pctLow=5fps target=120fps",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": []
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(true, GetMcpToolResultIsError(toolResult), "diagnostic session failure MCP isError");
            AssertContains(result, "== Diagnostic Session: FAIL ==");
            AssertContains(result, "diagnostic health degraded during session");
            AssertContains(result, "health=Critical");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic health failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic health failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic health failure summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "diagnostic health degraded during session");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("Critical", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic health failure live health");
            AssertEqual("flashback_playback", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic health failure live stage");
            AssertEqual(1, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic health failure live warning count");
            AssertContains(liveDocument.RootElement.GetProperty("LastWarning").GetString() ?? string.Empty, "health=Critical");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic health warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    private static async Task DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-export-playback-test-{Guid.NewGuid():N}");
        var requests = new List<(string Command, Dictionary<string, object?>? Payload)>();
        var getSnapshotCount = 0;
        var goLiveRequested = false;

        try
        {
            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "flashback-export-playback");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);
            optionsType.GetProperty("LeaveRunning")!.SetValue(options, true);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, payload, _) =>
            {
                requests.Add((command, payload));
                if (command == "FlashbackAction" &&
                    string.Equals(GetPayloadString(payload, "action"), "go-live", StringComparison.OrdinalIgnoreCase))
                {
                    goLiveRequested = true;
                }

                return Task.FromResult(command switch
                {
                    "GetSnapshot" => CreateSnapshotResponse(++getSnapshotCount),
                    "GetPerformanceTimeline" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """),
                    "WaitForCondition" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "condition met"
                        }
                        """),
                    "FlashbackExport" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Exported 120 packets from 1 segments"
                        }
                        """),
                    "VerifyFile" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Strict verification passed.",
                          "Data": {
                            "Succeeded": true,
                            "Message": "Strict verification passed."
                          }
                        }
                        """),
                    _ => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "ok"
                        }
                        """)
                });
            };

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
            var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");

            if (!GetBoolProperty(result, "Success"))
            {
                var warnings = GetPropertyValue(result, "Warnings") as System.Collections.IEnumerable;
                var warningText = warnings == null
                    ? string.Empty
                    : string.Join(" | ", warnings.Cast<object?>().Select(item => item?.ToString() ?? string.Empty));
                throw new InvalidOperationException($"Assertion failed for flashback export playback diagnostic success: warnings={warningText}");
            }

            AssertEqual(true, requests.Any(request => request.Command == "SetFlashbackEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback enabled Flashback");
            AssertEqual(true, requests.Any(request => request.Command == "SetPreviewEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback started preview");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "pause"), "flashback export playback pauses before seek");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "seek" && GetPayloadDouble(request.Payload, "positionMs") == 1000d), "flashback export playback seeks to 1000ms");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "play"), "flashback export playback starts playback");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackExport" && GetPayloadDouble(request.Payload, "seconds") == 1d), "flashback export playback exports one second");
            AssertEqual(true, requests.Any(request => request.Command == "VerifyFile" && GetPayloadString(request.Payload, "verificationProfile") == "flashback-export"), "flashback export playback verifies export");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "go-live"), "flashback export playback returns live");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "flashback export playback summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export during playback verified");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export playback go-live requested");
            AssertEqual(0, summaryDocument.RootElement.GetProperty("Warnings").GetArrayLength(), "flashback export playback summary warning count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        JsonElement CreateSnapshotResponse(int snapshotIndex)
        {
            if (snapshotIndex == 1)
            {
                return ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Snapshot": {
                        "IsPreviewing": false,
                        "IsRecording": false,
                        "FlashbackActive": false,
                        "FlashbackPlaybackState": "Live",
                        "DiagnosticHealthStatus": "Healthy",
                        "DiagnosticLikelyStage": "none",
                        "DiagnosticSummary": "No degraded frame lane detected.",
                        "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                        "FrameLedgerRecentEvents": []
                      }
                    }
                    """);
            }

            var playbackState = !goLiveRequested && snapshotIndex >= 5 ? "Playing" : "Live";
            var playbackFrames = snapshotIndex <= 4 ? 0 : snapshotIndex * 16;
            return ParseDiagnosticSessionJson($$"""
                {
                  "Success": true,
                  "Snapshot": {
                    "IsPreviewing": true,
                    "IsRecording": false,
                    "FlashbackActive": true,
                    "FlashbackBufferedDurationMs": 12000,
                    "FlashbackEncodedFrames": 360,
                    "FlashbackPlaybackState": "{{playbackState}}",
                    "FlashbackPlaybackFrameCount": {{playbackFrames}},
                    "FlashbackPlaybackPendingCommands": 0,
                    "FlashbackPlaybackCommandsDropped": 0,
                    "FlashbackPlaybackCommandsSkippedNotReady": 0,
                    "FlashbackPlaybackSubmitFailures": 0,
                    "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                    "FlashbackPlaybackSeekCommandsCoalesced": 0,
                    "FlashbackExportActive": false,
                    "FlashbackExportStatus": "Succeeded",
                    "FlashbackExportMessage": "Exported 120 packets from 1 segments",
                    "FlashbackExportOutputPath": "flashback-export-playback.mp4",
                    "ExpectedCaptureFrameRate": 120,
                    "SelectedExactFrameRate": 120,
                    "PreviewCadenceObservedFps": 120,
                    "VisualCadenceChangeFps": 120,
                    "VisualCadenceRepeatPercent": 0,
                    "DiagnosticHealthStatus": "Healthy",
                    "DiagnosticLikelyStage": "none",
                    "DiagnosticSummary": "No degraded frame lane detected.",
                    "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                    "FrameLedgerRecentEvents": []
                  }
                }
                """);
        }

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        static string? GetPayloadString(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) ? value?.ToString() : null;

        static bool? GetPayloadBool(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is bool boolValue ? boolValue : null;

        static double? GetPayloadDouble(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is IConvertible convertible
                ? convertible.ToDouble(CultureInfo.InvariantCulture)
                : null;

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "flashback export playback action array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected array to contain '{token}'.");
        }
    }

    private static async Task DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-unknown-initial-test-{Guid.NewGuid():N}");
        var commands = new List<string>();

        try
        {
            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "preview-only");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                commands.Add(command);
                if (command is "SetPreviewEnabled" or "SetRecordingEnabled" or "SetFlashbackEnabled")
                {
                    throw new InvalidOperationException($"Unexpected state mutation command: {command}");
                }

                return Task.FromResult(ParseDiagnosticSessionJson(command == "GetPerformanceTimeline"
                    ? """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "ok"
                      }
                      """));
            };

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
            var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic unknown initial result success");
            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "skipped state-mutating scenario");
            AssertEqual(false, commands.Contains("SetPreviewEnabled"), "diagnostic unknown initial did not start preview");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    private static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-connect-retry-test-{Guid.NewGuid():N}");
        var getSnapshotAttempts = 0;

        try
        {
            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "observe");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotAttempts++;
                    if (getSnapshotAttempts <= 2)
                    {
                        return Task.FromResult(ParseDiagnosticSessionJson("""
                            {
                              "Success": false,
                              "Status": "error",
                              "CommandLifecycle": "failed",
                              "Message": "Sussudio is not running or not responding. Start the app and try again.",
                              "ErrorCode": "pipe-connect-failed"
                            }
                            """));
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
            var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");

            AssertEqual(true, GetBoolProperty(result, "Success"), "diagnostic synthetic connect retry result success");
            AssertEqual(true, getSnapshotAttempts >= 3, "diagnostic synthetic connect failure was retried");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic synthetic connect retry summary success");
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic synthetic connect retry terminal state");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }

    private static async Task DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-concurrent-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");

        // Simulate a concurrent in-flight diagnostic session by holding the same exclusive
        // lock file the runner uses. A second RunAsync against this OutputDirectory must
        // fail fast with InvalidOperationException rather than corrupt the artifact set.
        FileStream? holderLock = null;
        try
        {
            holderLock = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
            var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
                ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");
            optionsType.GetProperty("Scenario")!.SetValue(options, "observe");
            optionsType.GetProperty("DurationSeconds")!.SetValue(options, 0);
            optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, 100);
            optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (_, _, _) =>
                Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "should-not-be-called"
                    }
                    """));

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");

            Exception? captured = null;
            try
            {
                var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                    ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                captured = ex.InnerException;
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            if (captured is null)
            {
                throw new InvalidOperationException("Assertion failed: expected concurrent invocation to throw, but RunAsync completed.");
            }

            AssertEqual(typeof(InvalidOperationException), captured.GetType(), "diagnostic concurrent invocation exception type");
            AssertContains(captured.Message ?? string.Empty, "Another diagnostic session");

            // Artifacts must NOT have been written; only the lock file should exist.
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "summary.json")), "diagnostic concurrent invocation must not write summary");
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "session-live.json")), "diagnostic concurrent invocation must not write live state");
        }
        finally
        {
            holderLock?.Dispose();
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static JsonElement ParseDiagnosticSessionJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }

    private static Task DiagnosticSessionRunner_ClassifiesFlashbackStressAudioMasterFallbacks()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
            ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
        var classify = runnerType.GetMethod(
                "ClassifyFlashbackStressAudioMasterFallbackWarning",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Audio-master fallback classifier was not found.");

        AssertEqual((string?)null, Invoke(0, 0, 0, 0), "no audio-master fallback warning");
        AssertEqual((string?)null, Invoke(4, 4, 0, 0), "startup unavailable fallback allowance");

        var unavailable = Invoke(5, 5, 0, 0)
            ?? throw new InvalidOperationException("Expected unavailable fallback warning.");
        AssertContains(unavailable, "audio-master unavailable fallbacks exceeded startup allowance");
        AssertContains(unavailable, "unavailableDelta=5");
        AssertContains(unavailable, "allowance=4");
        AssertContains(unavailable, "totalDelta=5");

        var stale = Invoke(2, 0, 1, 0)
            ?? throw new InvalidOperationException("Expected stale fallback warning.");
        AssertContains(stale, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(stale, "staleDelta=1");
        AssertContains(stale, "driftOutlierDelta=0");

        var driftOutlier = Invoke(2, 0, 0, 1)
            ?? throw new InvalidOperationException("Expected drift-outlier fallback warning.");
        AssertContains(driftOutlier, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(driftOutlier, "staleDelta=0");
        AssertContains(driftOutlier, "driftOutlierDelta=1");

        var unclassified = Invoke(2, 0, 0, 0)
            ?? throw new InvalidOperationException("Expected unclassified fallback warning.");
        AssertContains(unclassified, "audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(unclassified, "delta=2");

        return Task.CompletedTask;

        string? Invoke(long totalDelta, long unavailableDelta, long staleDelta, long driftOutlierDelta)
            => classify.Invoke(null, new object?[] { totalDelta, unavailableDelta, staleDelta, driftOutlierDelta }) as string;
    }

    private static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var source = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs");
        var diagnosticsHubSource = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs");
        var entryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");

        AssertContains(source, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(source, "targetOnePercentLowFps");
        AssertContains(source, "== 1% Low Target Summary");
        AssertContains(source, "AppendOnePercentLowTargetSummary");
        AssertContains(source, "AppendPressureSummary");
        AssertContains(source, "misses={belowTarget}/{valid.Length}");
        AssertContains(source, "PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceP99Ms\")");
        AssertContains(source, "PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceFivePercentLowFps\")");
        AssertContains(source, "VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"VisualCadenceChangeObservedFps\")");
        AssertContains(source, "MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"MjpegPacketHashUniqueObservedFps\")");
        AssertContains(source, "FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"FlashbackPlaybackFivePercentLowFps\")");
        AssertContains(source, "Preview 5% Low:");
        AssertContains(source, "Visual Cadence:");
        AssertContains(source, "MJPEG Fingerprint:");
        AssertContains(source, "Preview P99:");
        AssertContains(source, "PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(source, "PreviewD3DPresentCallP99Ms");
        AssertContains(source, "PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(source, "PreviewD3DFrameLatencyWaitTimeoutCount");
        AssertContains(source, "PreviewD3DFrameLatencyWaitP95Ms");
        AssertContains(source, "PreviewD3DFrameLatencyWaitMaxMs");
        AssertContains(source, "InP99 | RsP99 | PrP99 | TotP99");
        AssertContains(source, "FlashbackPlaybackP99FrameMs");
        AssertContains(source, "FlashbackPlaybackTargetFps");
        AssertContains(source, "Flashback target:");
        AssertContains(source, "FlashbackPlaybackDecodeP99Ms");
        AssertContains(source, "FlashbackPlaybackPendingCommands");
        AssertContains(source, "FlashbackPlaybackCommandsEnqueued");
        AssertContains(source, "FlashbackPlaybackCommandsProcessed");
        AssertContains(source, "FlashbackPlaybackCommandsDropped");
        AssertContains(source, "FlashbackPlaybackCommandsSkippedNotReady");
        AssertContains(source, "FlashbackPlaybackScrubUpdatesCoalesced");
        AssertContains(source, "FlashbackPlaybackSeekCommandsCoalesced");
        AssertContains(source, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(source, "FlashbackPlaybackLastCommandQueued");
        AssertContains(source, "FlashbackPlaybackLastCommandProcessed");
        AssertContains(source, "FlashbackPlaybackSubmitFailures");
        AssertContains(source, "FlashbackPlaybackLastDropUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastDropReason");
        AssertContains(source, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastSubmitFailure");
        AssertContains(source, "lastSubmitFailure");
        AssertContains(source, "FlashbackPlaybackSegmentSwitches");
        AssertContains(source, "FlashbackPlaybackFmp4Reopens");
        AssertContains(source, "FlashbackPlaybackWriteHeadWaits");
        AssertContains(source, "FlashbackPlaybackNearLiveSnaps");
        AssertContains(source, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        AssertContains(source, "FlashbackPlaybackLastCommandFailure");
        AssertContains(source, "FlashbackVideoQueueRejectedFrames");
        AssertContains(source, "FlashbackVideoQueueLastRejectReason");
        AssertContains(source, "FlashbackGpuQueueRejectedFrames");
        AssertContains(source, "FlashbackGpuQueueLastRejectReason");
        AssertContains(source, "Flashback Enqueue Rejects");
        AssertContains(source, "FatalCleanupInProgress");
        AssertContains(source, "FlashbackCleanupInProgress");
        AssertContains(source, "FlashbackForceRotateRequested");
        AssertContains(source, "FlashbackForceRotateDraining");
        AssertContains(source, "FlashbackExportFailureKind");
        AssertContains(source, "FlashbackExportPercent");
        AssertContains(source, "FlashbackExportInPointMs");
        AssertContains(source, "FlashbackExportOutPointMs");
        AssertContains(source, "FlashbackExportMessage");
        AssertContains(source, "FlashbackExportThroughputBytesPerSec");
        AssertContains(source, "FlashbackExportLastProgressAgeMs");
        AssertContains(source, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(source, "MjpegPreviewJitterDeadlineDropCount");
        AssertContains(source, "MjpegPreviewJitterClearedDropCount");
        AssertContains(source, "MjpegPreviewJitterResumeReprimeCount");
        AssertContains(source, "MjpegPreviewJitterLastDropReason");
        AssertContains(source, "JitD  | JitLat | JitDrop | JitUF | JitWhy");
        AssertContains(source, "FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage");
        AssertContains(source, "FormatFlashbackStageCell");
        AssertContains(source, "Cln | ExStat");
        AssertContains(source, "ExStat  | ExKind | Ex%");
        AssertContains(source, "FormatJitterDepthCell");
        AssertContains(source, "FormatExportFailureKind");
        AssertContains(source, "Jitter Depth:");
        AssertContains(source, "Jitter Latency:");
        AssertContains(source, "Jitter Drops:");
        AssertContains(source, "D3D Input P99:");
        AssertContains(source, "D3D Render P99:");
        AssertContains(source, "D3D Present P99:");
        AssertContains(source, "D3D Total P99:");
        AssertContains(source, "D3D P99 Bottleneck:");
        AssertContains(source, "PreviewPacingLikelySlowStage");
        AssertContains(source, "Preview Slow Stage:");
        AssertContains(source, "FormatD3DP99Bottleneck");
        AssertContains(source, "== Pressure Summary ==");
        AssertContains(source, "Preview Pressure:");
        AssertContains(source, "overBudgetSamples input=");
        AssertContains(source, "dxgiMissedSamples=");
        AssertContains(source, "jitterDropsDelta=");
        AssertContains(source, "Flashback Pressure:");
        AssertContains(source, "decodeOverBudget=");
        AssertContains(source, "pendingCmdSamples=");
        AssertContains(source, "System Pressure:");
        AssertContains(source, "gcPauseSamples=");
        AssertContains(source, "CountOverBudget");
        AssertContains(source, "NonNegativeDelta");
        AssertContains(source, "Flashback P99:");
        AssertContains(source, "Flashback Decode:");
        AssertContains(source, "phase={FormatOptional(last.FlashbackPlaybackMaxDecodePhase)}");
        AssertContains(source, "send={last.FlashbackPlaybackMaxDecodeSendMs:F1}ms");
        AssertContains(source, "audio={last.FlashbackPlaybackMaxDecodeAudioMs:F1}ms");
        AssertContains(source, "Flashback Cmds:");
        AssertContains(source, "maxLatencyCommand={FormatOptional(last.FlashbackPlaybackMaxCommandQueueLatencyCommand)}");
        AssertContains(source, "Flashback Cmd Counters:");
        AssertContains(source, "lastQueued={FormatOptional(last.FlashbackPlaybackLastCommandQueued)}");
        AssertContains(source, "lastProcessed={FormatOptional(last.FlashbackPlaybackLastCommandProcessed)}");
        foreach (var propertyName in new[]
                 {
                     "CaptureCadenceFivePercentLowFps",
                     "PreviewCadenceFivePercentLowFps",
                     "VisualCadenceChangeObservedFps",
                     "VisualCadenceRepeatFramePercent",
                     "VisualCadenceMotionConfidence",
                     "MjpegPacketHashInputObservedFps",
                     "MjpegPacketHashUniqueObservedFps",
                     "MjpegPacketHashDuplicateFramePercent",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "FlashbackPlaybackFivePercentLowFps",
                     "FlashbackPlaybackCommandsEnqueued",
                     "FlashbackPlaybackCommandsProcessed",
                     "FlashbackPlaybackCommandsDropped",
                     "FlashbackPlaybackCommandsSkippedNotReady",
                     "FlashbackPlaybackScrubUpdatesCoalesced",
                     "FlashbackPlaybackSeekCommandsCoalesced",
                     "FlashbackPlaybackLastCommandQueued",
                     "FlashbackPlaybackLastCommandProcessed"
                 })
        {
            AssertNotNull(entryType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{propertyName}");
            AssertContains(diagnosticsHubSource, $"{propertyName} = snapshot.{propertyName}");
        }
        AssertContains(source, "Flashback Failure:");
        AssertContains(source, "Flashback Stages:");
        AssertContains(source, "failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
        AssertContains(source, "Cleanup State:");
        AssertContains(source, "forceRotateRequested={last.FlashbackForceRotateRequested}");
        AssertContains(source, "forceRotateDraining={last.FlashbackForceRotateDraining}");
        AssertContains(source, "kind={FormatOptional(last.FlashbackExportFailureKind)}");
        AssertContains(source, "Export Message:");
        AssertContains(source, "Export Progress:");
        AssertContains(source, "Export Range:");
        AssertContains(source, "FormatExportOutPoint");
        AssertContains(source, "Export Output:");

        return Task.CompletedTask;
    }

    private static async Task McpPerformanceTimelineTool_RendersFlashbackCommandCounters()
    {
        var pipeName = NewMcpToolPipeName("timeline-counters");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var timelineTools = RequireMcpType("McpServer.Tools.PerformanceTimelineTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            timelineTools,
                            "get_performance_timeline",
                            pipeClient,
                            2,
                            118d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Flashback Cmd Counters: enqueued 1 -> 9, processed 0 -> 8, dropped 0 -> 2, skippedNotReady 0 -> 1, scrubCoalesced 0 -> 4, seekCoalesced 0 -> 3, lastQueued=Seek, lastProcessed=Pause");
                    AssertContains(output, "cmdDropsDelta=2");
                    AssertContains(output, "Preview Slow Stage: Unknown/None -> CompositorMiss/High evidence=dxgiRecentMissed=4");
                },
                _ => """
                     {
                       "Success": true,
                       "Data": [
                         {
                           "TimestampUtc": "2026-05-04T12:00:00Z",
                           "PreviewPacingLikelySlowStage": "Unknown",
                           "PreviewPacingSlowStageConfidence": "None",
                           "PreviewPacingSlowStageEvidence": "",
                           "FlashbackPlaybackCommandsEnqueued": 1,
                           "FlashbackPlaybackCommandsProcessed": 0,
                           "FlashbackPlaybackCommandsDropped": 0,
                           "FlashbackPlaybackCommandsSkippedNotReady": 0,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                           "FlashbackPlaybackSeekCommandsCoalesced": 0,
                           "FlashbackPlaybackLastCommandQueued": "Play",
                           "FlashbackPlaybackLastCommandProcessed": "None"
                         },
                         {
                           "TimestampUtc": "2026-05-04T12:00:01Z",
                           "PreviewPacingLikelySlowStage": "CompositorMiss",
                           "PreviewPacingSlowStageConfidence": "High",
                           "PreviewPacingSlowStageEvidence": "dxgiRecentMissed=4",
                           "FlashbackPlaybackPendingCommands": 2,
                           "FlashbackPlaybackCommandsEnqueued": 9,
                           "FlashbackPlaybackCommandsProcessed": 8,
                           "FlashbackPlaybackCommandsDropped": 2,
                           "FlashbackPlaybackCommandsSkippedNotReady": 1,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 4,
                           "FlashbackPlaybackSeekCommandsCoalesced": 3,
                           "FlashbackPlaybackLastCommandQueued": "Seek",
                           "FlashbackPlaybackLastCommandProcessed": "Pause"
                         }
                       ]
                     }
                     """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetPerformanceTimeline", ("maxEntries", 2));
    }

    private static async Task McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: HalfRatePreviewAndPlaybackSuspected");
                    AssertContains(output, "SampleQuality: Ready");
                    AssertContains(output, "SourceToPreviewRatio: 0.5");
                    AssertContains(output, "SourceToPlaybackRatio: 0.5");
                    AssertContains(output, "HalfRatePreviewSuspected: true");
                    AssertContains(output, "HalfRatePlaybackSuspected: true");
                    AssertContains(output, "VisualChangeFps: 60");
                    AssertContains(output, "MjpegUniqueFps: 60");
                    AssertContains(output, "PreviewDropDelta: 4");
                    AssertContains(output, "PlaybackDropDelta: 2");
                    AssertContains(output, "PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion");
                    AssertContains(output, "PreviewPacingSlowStageConfidence: Medium");
                    AssertContains(output, "PreviewPacingSlowStageEvidence: synthetic duplicate cadence");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 3600,
                          "CaptureCadenceSampleDurationMs": 30000,
                          "PreviewCadenceObservedFps": 60,
                          "PreviewCadenceFivePercentLowFps": 60,
                          "PreviewCadenceOnePercentLowFps": 58,
                          "PreviewCadenceSampleCount": 1800,
                          "PreviewCadenceSampleDurationMs": 30000,
                          "PreviewCadenceRecentIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "FlashbackPlaybackTargetFps": 120,
                          "FlashbackPlaybackObservedFps": 60,
                          "FlashbackPlaybackFivePercentLowFps": 60,
                          "FlashbackPlaybackOnePercentLowFps": 58,
                          "FlashbackPlaybackCadenceSampleCount": 1800,
                          "FlashbackPlaybackSampleDurationMs": 30000,
                          "FlashbackPlaybackRecentFrameIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "VisualCadenceChangeObservedFps": 60,
                          "VisualCadenceRepeatFramePercent": 50,
                          "VisualCadenceMotionConfidence": "High",
                          "MjpegPacketHashInputObservedFps": 120,
                          "MjpegPacketHashUniqueObservedFps": 60,
                          "MjpegPacketHashDuplicateFramePercent": 50,
                          "PreviewPacingLikelySlowStage": "VisualDuplicateOrLowMotion",
                          "PreviewPacingSlowStageConfidence": "Medium",
                          "PreviewPacingSlowStageEvidence": "synthetic duplicate cadence"
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": [
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                            "MjpegPreviewJitterTotalDropped": 1,
                            "FlashbackPlaybackDroppedFrames": 0
                          },
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 4,
                            "MjpegPreviewJitterTotalDropped": 5,
                            "FlashbackPlaybackDroppedFrames": 2
                          }
                        ]
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }

    private static async Task McpFramePacingVerdictTool_FlagsInsufficientSampleDuration()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing-short");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: InsufficientSample");
                    AssertContains(output, "SampleQuality: Insufficient");
                    AssertContains(output, "ready=false");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 240,
                          "CaptureCadenceSampleDurationMs": 2000,
                          "PreviewCadenceObservedFps": 120,
                          "PreviewCadenceFivePercentLowFps": 120,
                          "PreviewCadenceOnePercentLowFps": 119,
                          "PreviewCadenceSampleCount": 240,
                          "PreviewCadenceSampleDurationMs": 2000
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
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

    private static async Task McpPreviewColorProbeTool_FormatsProbeResponses()
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

    private static async Task McpVideoSourceProbeTool_FormatsProbeResponses()
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

    private static Process StartMcpServerProcess(string assemblyPath, string? pipeName = null)
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
        if (!string.IsNullOrWhiteSpace(pipeName))
        {
            startInfo.Environment["SUSSUDIO_AUTOMATION_PIPE"] = pipeName;
        }

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

    private static object CreateDefaultMcpPipeClient()
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(type)
               ?? throw new InvalidOperationException("Failed to create default MCP PipeClient.");
    }

    private static async Task<string> InvokeMcpToolStringAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
        return result is string text
            ? text
            : GetMcpToolResultText(result);
    }

    private static async Task<object> InvokeMcpToolResultAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
    }

    private static MethodInfo ResolveMcpToolMethod(Type type, string methodName, int argumentCount)
    {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .ToArray();
        if (methods.Length == 0)
        {
            throw new InvalidOperationException($"{type.FullName}.{methodName} was not found.");
        }

        var matchingMethod = methods.SingleOrDefault(method => method.GetParameters().Length == argumentCount);
        if (matchingMethod != null)
        {
            return matchingMethod;
        }

        var shapes = string.Join(
            ", ",
            methods.Select(method => $"{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})"));
        throw new InvalidOperationException(
            $"{type.FullName}.{methodName} had no overload accepting {argumentCount} argument(s). Available: {shapes}");
    }

    private static string GetMcpToolResultText(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        var content = GetPropertyValue(result, "Content") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("MCP tool result content was not enumerable.");
        foreach (var item in content)
        {
            var text = GetPropertyValue(item, "Text") as string;
            if (text is not null)
            {
                return text;
            }
        }

        throw new InvalidOperationException("MCP tool result did not contain text content.");
    }

    private static bool GetMcpToolResultIsError(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        return Convert.ToBoolean(GetPropertyValue(result, "IsError"));
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
