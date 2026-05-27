using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        var automationControlToolsText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs");
        var captureSettingsToolsText = automationControlToolsText;
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var captureOptionsToolText = captureSettingsToolsText;
        var uiSettingsToolText = automationControlToolsText;
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertContains(captureSettingsToolsText, "string? preset = null");
        AssertContains(captureSettingsToolsText, "string? splitEncodeMode = null");
        AssertContains(captureSettingsToolsText, "int? mjpegDecoderCount = null");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetPreset");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetSplitEncodeMode");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetMjpegDecoderCount");

        AssertContains(appStateToolText, "get_app_state_raw");
        AssertContains(appStateToolText, "UseStructuredContent = true");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetCaptureOptions\")");
        AssertContains(captureOptionsToolText, "get_capture_options");
        AssertContains(captureOptionsToolText, "AutomationCommandKind.GetCaptureOptions");
        AssertContains(captureOptionsToolText, "UseStructuredContent = true");
        AssertContains(uiSettingsToolText, "configure_ui");
        AssertContains(uiSettingsToolText, "\"SetPreviewVolume\"");
        AssertContains(uiSettingsToolText, "\"SetStatsVisible\"");
        AssertDoesNotContain(automationSnapshotText, " Options { get; init;");

        return Task.CompletedTask;
    }

    internal static Task McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds()
    {
        var formatterText = ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var automationControlToolsText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs");
        var captureSettingsToolsText = automationControlToolsText;
        var captureOptionsToolText = captureSettingsToolsText;
        var deviceToolsText = captureSettingsToolsText;
        var diagnosticsToolsText = appStateToolText;
        var flashbackToolsText = automationControlToolsText;
        var flashbackActionsText = flashbackToolsText;
        var flashbackExportText = flashbackToolsText;
        var framePacingVerdictToolsText = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");
        var memoryDiagnosticsToolsText = appStateToolText;
        var pipelineSettingsToolsText = captureSettingsToolsText;
        var performanceToolsText = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs");
        var performanceTimelineToolsText = performanceToolsText;
        var previewToolsText = automationControlToolsText;
        var previewInspectionToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewInspectionTools.cs");
        var previewColorProbeToolsText = previewInspectionToolsText;
        var recordingToolsText = previewToolsText;
        var presentMonToolsText = performanceToolsText;
        var previewFrameCaptureToolsText = previewInspectionToolsText;
        var verificationToolsText = automationControlToolsText;
        var videoSourceProbeToolsText = previewColorProbeToolsText;
        var windowToolsText = automationControlToolsText;
        var windowScreenshotToolsText = previewFrameCaptureToolsText;
        var waitToolsText = previewToolsText;

        AssertContains(formatterText, "AutomationCommandKind Kind,");
        AssertContains(formatterText, "pipeClient.SendCommandAsync(command.Kind, command.Payload)");
        AssertDoesNotContain(formatterText, "string CommandName");
        AssertDoesNotContain(formatterText, "SendCommandAsync(command.CommandName");
        AssertDoesNotContain(formatterText, "pipeClient.SendCommandAsync(commandName");

        AssertContains(appStateToolText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(captureOptionsToolText, "SendCommandAsync(AutomationCommandKind.GetCaptureOptions)");
        AssertDoesNotContain(captureOptionsToolText, "SendCommandAsync(\"GetCaptureOptions\"");
        AssertContains(diagnosticsToolsText, "SendCommandAsync(AutomationCommandKind.GetDiagnostics, payload)");
        AssertDoesNotContain(diagnosticsToolsText, "SendCommandAsync(\"GetDiagnostics\"");

        foreach (var commandName in new[]
        {
            "SetResolution",
            "SetFrameRate",
            "SetVideoFormat",
            "SetRecordingFormat",
            "SetQuality",
            "SetCustomBitrate",
            "SetPreset",
            "SetSplitEncodeMode",
            "SetMjpegDecoderCount"
        })
        {
            AssertContains(captureSettingsToolsText, $"ToolCommandFormatter.Optional(AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(captureSettingsToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[]
        {
            "RefreshDevices",
            "SelectDevice",
            "SelectAudioInputDevice",
            "SetCustomAudioInput"
        })
        {
            AssertContains(deviceToolsText, $"AutomationCommandKind.{commandName}");
            AssertDoesNotContain(deviceToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[]
        {
            "SetHdrEnabled",
            "SetTrueHdrPreviewEnabled",
            "SetAudioEnabled",
            "SetAudioPreviewEnabled",
            "SetOutputPath"
        })
        {
            AssertContains(pipelineSettingsToolsText, $"ToolCommandFormatter.Optional(AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(pipelineSettingsToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[] { "SetDeviceAudioMode", "SetAnalogAudioGain" })
        {
            AssertContains(pipelineSettingsToolsText, $"ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(pipelineSettingsToolsText, $"ExecuteAndFormatResultAsync(pipeClient, \"{commandName}\"");
        }

        AssertContains(previewToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                AutomationCommandKind.SetPreviewEnabled,\n                \"SetPreviewEnabled\",");
        AssertDoesNotContain(previewToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetPreviewEnabled\",");

        AssertContains(recordingToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                AutomationCommandKind.SetRecordingEnabled,\n                \"SetRecordingEnabled\",");
        AssertDoesNotContain(recordingToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetRecordingEnabled\",");

        AssertContains(flashbackToolsText, "AutomationCommandKind.SetFlashbackEnabled");
        AssertContains(flashbackToolsText, "AutomationCommandKind.RestartFlashback");
        AssertDoesNotContain(flashbackToolsText, "commandName: \"SetFlashbackEnabled\"");
        AssertDoesNotContain(flashbackToolsText, "commandName: \"RestartFlashback\"");
        AssertContains(flashbackActionsText, "AutomationCommandKind.FlashbackAction");
        AssertDoesNotContain(flashbackActionsText, "commandName: \"FlashbackAction\"");
        AssertContains(flashbackExportText, "SendCommandAsync(AutomationCommandKind.FlashbackExport, payload)");
        AssertDoesNotContain(flashbackExportText, "SendCommandAsync(\"FlashbackExport\"");
        AssertContains(flashbackToolsText, "SendCommandAsync(AutomationCommandKind.FlashbackGetSegments)");
        AssertDoesNotContain(flashbackToolsText, "SendCommandAsync(\"FlashbackGetSegments\"");
        AssertContains(framePacingVerdictToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(framePacingVerdictToolsText, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertDoesNotContain(framePacingVerdictToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertDoesNotContain(framePacingVerdictToolsText, "SendCommandAsync(\"GetPerformanceTimeline\"");
        AssertContains(memoryDiagnosticsToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(memoryDiagnosticsToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(performanceTimelineToolsText, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, payload)");
        AssertDoesNotContain(performanceTimelineToolsText, "SendCommandAsync(\"GetPerformanceTimeline\"");
        AssertContains(previewColorProbeToolsText, "SendCommandAsync(AutomationCommandKind.ProbePreviewColor)");
        AssertDoesNotContain(previewColorProbeToolsText, "SendCommandAsync(\"ProbePreviewColor\"");
        AssertContains(previewFrameCaptureToolsText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(previewFrameCaptureToolsText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(presentMonToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(presentMonToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.VerifyLastRecording)");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload)");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.VerifyFile, payload)");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"VerifyLastRecording\"");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"AssertSnapshot\"");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"VerifyFile\"");
        AssertContains(videoSourceProbeToolsText, "SendCommandAsync(AutomationCommandKind.ProbeVideoSource)");
        AssertDoesNotContain(videoSourceProbeToolsText, "SendCommandAsync(\"ProbeVideoSource\"");
        AssertContains(windowToolsText, "SendCommandAsync(AutomationCommandKind.ArmClose, armPayload)");
        AssertContains(windowToolsText, "SendCommandAsync(AutomationCommandKind.WindowAction, actionPayload)");
        AssertContains(windowToolsText, "AutomationCommandKind.SetFullScreenEnabled");
        AssertContains(windowToolsText, "AutomationCommandKind.OpenRecordingsFolder");
        AssertDoesNotContain(windowToolsText, "SendCommandAsync(\"ArmClose\"");
        AssertDoesNotContain(windowToolsText, "SendCommandAsync(\"WindowAction\"");
        AssertDoesNotContain(windowToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetFullScreenEnabled\"");
        AssertDoesNotContain(windowToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"OpenRecordingsFolder\"");
        AssertContains(windowScreenshotToolsText, "SendCommandAsync(AutomationCommandKind.CaptureWindowScreenshot, payload)");
        AssertDoesNotContain(windowScreenshotToolsText, "SendCommandAsync(\"CaptureWindowScreenshot\", payload)");
        AssertContains(waitToolsText, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertContains(waitToolsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertDoesNotContain(waitToolsText, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsText, "SendCommandAsync(\"WaitForCondition\"");

        return Task.CompletedTask;
    }

    internal static async Task McpDeviceTools_RouteRefreshSelectionsAndCustomAudio()
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

    internal static async Task McpCaptureSettingsTools_RouteProvidedSettings()
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

    internal static async Task McpPipelineSettingsTools_RoutePipelineAndAudioCommands()
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

    internal static async Task McpRecordingTools_RouteRecordingToggle()
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

    internal static async Task McpUiSettingsTools_RouteUiCommands()
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
                expectedCount: 7,
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
                            "configure_frametime_graph",
                            pipeClient,
                            true)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_flashback_timeline",
                            pipeClient,
                            false)
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
        AssertCommandRequest(requests[4], "SetFrameTimeOverlayVisible", ("visible", true));
        AssertCommandRequest(requests[5], "SetFlashbackTimelineVisible", ("visible", false));
        AssertCommandRequest(requests[6], "SetStatsSectionVisible", ("section", "Source"), ("visible", false));
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] SetShowAllCaptureOptions: ui command 0 ok",
                "[OK] SetPreviewVolume: ui command 1 ok",
                "[OK] SetStatsVisible: ui command 2 ok",
                "[OK] SetSettingsVisible: ui command 3 ok",
                "[OK] SetFrameTimeOverlayVisible: ui command 4 ok",
                "[OK] SetFlashbackTimelineVisible: ui command 5 ok",
                "[OK] SetStatsSectionVisible: ui command 6 ok"),
            result,
            "MCP UI command formatted output");
    }

    internal static async Task McpToolCommandFormatter_BatchesPendingCommands()
    {
        var pipeName = NewMcpToolPipeName("formatter");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var formatterType = RequireMcpType("McpServer.Tools.ToolCommandFormatter");
        var optional = formatterType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .SingleOrDefault(method =>
            {
                if (method.Name != "Optional" || method.IsGenericMethodDefinition)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType.FullName == "Sussudio.Models.AutomationCommandKind" &&
                       parameters[1].ParameterType == typeof(string) &&
                       parameters[2].ParameterType == typeof(bool) &&
                       parameters[3].ParameterType == typeof(Dictionary<string, object?>);
            })
            ?? throw new InvalidOperationException("ToolCommandFormatter.Optional overload was not found.");
        var automationCommandKindType = optional.GetParameters()[0].ParameterType;
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

        var firstPending = optional.Invoke(
            null,
            new object?[]
            {
                Enum.Parse(automationCommandKindType, "SetStatsVisible"),
                "SetStatsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = true }
            });
        var secondPending = optional.Invoke(
            null,
            new object?[]
            {
                Enum.Parse(automationCommandKindType, "SetSettingsVisible"),
                "SetSettingsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = false }
            });
        var commands = Array.CreateInstance(pendingType, 2);
        commands.SetValue(firstPending, 0);
        commands.SetValue(secondPending, 1);

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

    internal static async Task McpHostToolSchema_UsesPipeClientAsService()
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

    internal static async Task McpPipeClient_HonorsSussudioAutomationPipeEnvironment()
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

    internal static async Task McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport()
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

    internal static async Task McpVerificationTools_FormatVerificationResponses()
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
        AssertAutomationCommandId(requests[2], "AssertSnapshot");
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

        AssertEqual(
            """
            == Recording Verification: PASS ==
            Message: last recording verified
            Output: C:\captures\latest.mp4 | Exists: true | Size: 123456 bytes
            Mode: LastRecording | Codec: hevc | Pixel Format: p010le
            Resolution: 3840 x 2160 | FPS: 59.94
            HDR: Level=Strict Metadata=true Colorimetry=true Mastering=false
            Mismatches: None
            """,
            recordingResult.Replace("\r\n", "\n"),
            "verify_recording exact text");
        AssertEqual(
            """
            == File Verification: FAIL ==
            Message: file mismatch
            File: C:\captures\clip.mp4 | Exists: true | Size: 42 bytes
            Codec: h264 | Pixel Format: yuv420p
            Resolution: 1920 x 1080 | FPS: 30
            """,
            fileResult.Replace("\r\n", "\n"),
            "verify_file exact text");
        AssertEqual(
            """
            Snapshot assertions: FAIL
            Message: 1 assertion failed
            Assertions: 1
            Passed: false
            Failures: IsRecording expected false
            """,
            assertResult.Replace("\r\n", "\n"),
            "assert_snapshot exact text");
        AssertEqual("no verification data", missingRecordingResult, "verify_recording missing verification fallback");
        AssertEqual("file not found", missingFileResult, "verify_file missing verification fallback");

        var verificationRootText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs")
            .Replace("\r\n", "\n");

        AssertContains(verificationRootText, "[McpServerToolType]");
        AssertContains(verificationRootText, "public static class VerificationTools");
        AssertDoesNotContain(verificationRootText, "public static partial class VerificationTools");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_recording");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> assert_snapshot");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_file");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.VerifyLastRecording)");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload)");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.VerifyFile, payload)");
        AssertContains(verificationRootText, "TryParseAssertionArray(assertions, out var parsedAssertions, out var parseError)");
        AssertContains(verificationRootText, "BuildRecordingVerificationText(response, verification, message)");
        AssertContains(verificationRootText, "BuildSnapshotAssertionText(response)");
        AssertContains(verificationRootText, "BuildFileVerificationText(filePath, response, verification, message)");

        AssertContains(verificationRootText, "private static bool TryParseAssertionArray(");
        AssertContains(verificationRootText, "string.IsNullOrWhiteSpace(assertions)");
        AssertContains(verificationRootText, "JsonDocument.Parse(assertions)");
        AssertContains(verificationRootText, "RootElement.Clone()");
        AssertContains(verificationRootText, "Invalid assertions JSON: {ex.Message}");
        AssertContains(verificationRootText, "private static bool TryGetVerification(");
        AssertContains(verificationRootText, "response.TryGetProperty(\"Data\", out var data)");
        AssertContains(verificationRootText, "data.TryGetProperty(\"Verification\", out verification)");
        AssertContains(verificationRootText, "response.TryGetProperty(\"Snapshot\", out var snapshot)");
        AssertContains(verificationRootText, "snapshot.TryGetProperty(\"LastVerification\", out verification)");

        AssertContains(verificationRootText, "private static string BuildRecordingVerificationText(");
        AssertContains(verificationRootText, "== Recording Verification: PASS ==");
        AssertContains(verificationRootText, "FormatJsonArrayList(verification, \"Mismatches\", \"Mismatches\")");
        AssertContains(verificationRootText, "private static string BuildSnapshotAssertionText(");
        AssertContains(verificationRootText, "FormatJsonArrayList(failures, \"Failures\")");
        AssertContains(verificationRootText, "\"{label}: None\"");
        AssertContains(verificationRootText, "private static string BuildFileVerificationText(");
        AssertContains(verificationRootText, "== File Verification: PASS ==");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "VerificationTools.Formatting.cs")),
            "MCP verification response formatting lives with the verification tool commands");

        AssertMcpCommandRoutingTestsUseCommandIdHelper();
    }
}
