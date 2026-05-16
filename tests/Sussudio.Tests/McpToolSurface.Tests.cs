using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Tests for MCP tool registration, command names, and surface compatibility.
static partial class Program
{

    private static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        McpToolSurface_CapturePipelineRoutingUsesAutomationCommandKinds();
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
        AssertContains(captureOptionsToolText, "\"GetCaptureOptions\"");
        AssertContains(captureOptionsToolText, "UseStructuredContent = true");
        AssertContains(uiSettingsToolText, "configure_ui");
        AssertContains(uiSettingsToolText, "\"SetShowAllCaptureOptions\"");
        AssertContains(uiSettingsToolText, "\"SetPreviewVolume\"");
        AssertContains(uiSettingsToolText, "\"SetStatsVisible\"");
        if (snapshotType.GetProperty("Options") != null)
        {
            throw new InvalidOperationException("AutomationSnapshot.Options should not be present when capture options are a separate surface.");
        }

        return Task.CompletedTask;
    }

    private static void McpToolSurface_CapturePipelineRoutingUsesAutomationCommandKinds()
    {
        var captureSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/CaptureSettingsTools.cs");
        var pipelineSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/PipelineSettingsTools.cs");
        var previewToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewTools.cs");
        var recordingToolsText = ReadRepoFile("tools/McpServer/Tools/RecordingTools.cs");
        var previewFrameCaptureToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewFrameCaptureTools.cs");
        var windowScreenshotToolsText = ReadRepoFile("tools/McpServer/Tools/WindowScreenshotTools.cs");
        var waitToolsText = ReadRepoFile("tools/McpServer/Tools/WaitTools.cs");

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

        AssertContains(previewFrameCaptureToolsText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(previewFrameCaptureToolsText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(windowScreenshotToolsText, "SendCommandAsync(AutomationCommandKind.CaptureWindowScreenshot, payload)");
        AssertDoesNotContain(windowScreenshotToolsText, "SendCommandAsync(\"CaptureWindowScreenshot\", payload)");
        AssertContains(waitToolsText, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertContains(waitToolsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertDoesNotContain(waitToolsText, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsText, "SendCommandAsync(\"WaitForCondition\"");
    }
}
