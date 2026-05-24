using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Tests for MCP tool registration, command names, and surface compatibility.
static partial class Program
{

    internal static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        var captureSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/CaptureSettingsTools.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var captureOptionsToolText = ReadRepoFile("tools/McpServer/Tools/CaptureOptionsTools.cs");
        var uiSettingsToolText = ReadRepoFile("tools/McpServer/Tools/UiSettingsTools.cs");
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

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
        if (snapshotType.GetProperty("Options") != null)
        {
            throw new InvalidOperationException("AutomationSnapshot.Options should not be present when capture options are a separate surface.");
        }

        return Task.CompletedTask;
    }

    internal static Task McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds()
    {
        var formatterText = ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var captureSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/CaptureSettingsTools.cs");
        var captureOptionsToolText = ReadRepoFile("tools/McpServer/Tools/CaptureOptionsTools.cs");
        var deviceToolsText = ReadRepoFile("tools/McpServer/Tools/DeviceTools.cs");
        var diagnosticsToolsText = ReadRepoFile("tools/McpServer/Tools/DiagnosticsTools.cs");
        var flashbackToolsText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.cs");
        var flashbackActionsText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.Actions.cs");
        var flashbackExportText = ReadRepoFile("tools/McpServer/Tools/FlashbackTools.Export.cs");
        var framePacingVerdictToolsText = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");
        var memoryDiagnosticsToolsText = ReadRepoFile("tools/McpServer/Tools/MemoryDiagnosticsTools.cs");
        var pipelineSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/PipelineSettingsTools.cs");
        var performanceTimelineToolsText = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs");
        var previewToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewTools.cs");
        var previewColorProbeToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewColorProbeTools.cs");
        var recordingToolsText = ReadRepoFile("tools/McpServer/Tools/RecordingTools.cs");
        var presentMonCorrelationText = ReadRepoFile("tools/McpServer/Tools/PresentMonTools.Correlation.cs");
        var previewFrameCaptureToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.cs");
        var verificationToolsText = ReadRepoFile("tools/McpServer/Tools/VerificationTools.cs");
        var videoSourceProbeToolsText = ReadRepoFile("tools/McpServer/Tools/VideoSourceProbeTools.cs");
        var windowToolsText = ReadRepoFile("tools/McpServer/Tools/WindowTools.cs");
        var windowScreenshotToolsText = ReadRepoFile("tools/McpServer/Tools/WindowScreenshotTools.cs");
        var waitToolsText = ReadRepoFile("tools/McpServer/Tools/WaitTools.cs");

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
        AssertContains(presentMonCorrelationText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(presentMonCorrelationText, "SendCommandAsync(\"GetSnapshot\"");
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
}
