using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;
[McpServerToolType]
// MCP tools for changing capture settings such as resolution, FPS, HDR, codec,
// bitrate, and decoder count.
public static class CaptureSettingsTools
{
    [McpServerTool, Description("Configure capture settings: resolution, frame rate, video format override, recording format, quality, custom bitrate, preset, split encode mode, and MJPEG decoder count. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_capture(
        PipeClient pipeClient,
        [Description("Recording resolution, for example 3840x2160")] string? resolution = null,
        [Description("Frame rate in fps, for example 60")] double? frameRate = null,
        [Description("Video format override, for example Auto, MJPG, NV12, or P010")] string? videoFormat = null,
        [Description("Recording format, for example Hevc")] string? format = null,
        [Description("Quality preset, for example High")] string? quality = null,
        [Description("Custom bitrate in Mbps")] double? bitrateMbps = null,
        [Description("Encoder preset, for example P5 or Quality")] string? preset = null,
        [Description("Split encode mode, for example Auto or ForcedOn")] string? splitEncodeMode = null,
        [Description("Number of MJPEG decoders to use for CPU MJPEG mode")] int? mjpegDecoderCount = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No capture setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetResolution, "SetResolution", "resolution", resolution),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetFrameRate, "SetFrameRate", "frameRate", frameRate),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetVideoFormat, "SetVideoFormat", "videoFormat", videoFormat),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetRecordingFormat, "SetRecordingFormat", "format", format),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetQuality, "SetQuality", "quality", quality),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetCustomBitrate, "SetCustomBitrate", "bitrateMbps", bitrateMbps),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetPreset, "SetPreset", "preset", preset),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetSplitEncodeMode, "SetSplitEncodeMode", "splitEncodeMode", splitEncodeMode),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetMjpegDecoderCount, "SetMjpegDecoderCount", "decoderCount", mjpegDecoderCount))
            .ConfigureAwait(false);

}

[McpServerToolType]
// MCP tools for refreshing and selecting capture/audio devices.
public static class DeviceTools
{
    [McpServerTool, Description("Select capture device, audio input device, refresh device list, or toggle custom audio input")]
    public static async Task<CallToolResult> configure_device(
        PipeClient pipeClient,
        [Description("Capture device id to select")] string? deviceId = null,
        [Description("Capture device name to select when id is unknown")] string? deviceName = null,
        [Description("Audio input device id to select")] string? audioDeviceId = null,
        [Description("Audio input device name to select when id is unknown")] string? audioDeviceName = null,
        [Description("Refresh the device list before making selections")] bool refresh = false,
        [Description("Enable or disable custom audio input")] bool? customAudioInput = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No device configuration changes requested.",
                ToolCommandFormatter.Optional(
                    AutomationCommandKind.RefreshDevices,
                    "RefreshDevices",
                    refresh),
                ToolCommandFormatter.Optional(
                    AutomationCommandKind.SelectDevice,
                    "SelectDevice",
                    !string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName),
                    new Dictionary<string, object?>
                    {
                        ["deviceId"] = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
                        ["deviceName"] = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName
                    }),
                ToolCommandFormatter.Optional(
                    AutomationCommandKind.SelectAudioInputDevice,
                    "SelectAudioInputDevice",
                    !string.IsNullOrWhiteSpace(audioDeviceId) || !string.IsNullOrWhiteSpace(audioDeviceName),
                    new Dictionary<string, object?>
                    {
                        ["deviceId"] = string.IsNullOrWhiteSpace(audioDeviceId) ? null : audioDeviceId,
                        ["deviceName"] = string.IsNullOrWhiteSpace(audioDeviceName) ? null : audioDeviceName
                    }),
                ToolCommandFormatter.Optional(
                    AutomationCommandKind.SetCustomAudioInput,
                    "SetCustomAudioInput",
                    customAudioInput.HasValue,
                    customAudioInput.HasValue ? new Dictionary<string, object?> { ["enabled"] = customAudioInput.Value } : null))
            .ConfigureAwait(false);
}

[McpServerToolType]
// MCP tools for reading selectable device, format, codec, and UI options.
public static class CaptureOptionsTools
{
    [McpServerTool(UseStructuredContent = true), Description("Get structured capture options and current selections, including devices, audio inputs, formats, resolutions, frame rates, presets, split encode modes, video formats, and UI-facing automation state.")]
    public static async Task<object> get_capture_options(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetCaptureOptions).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return CreateError(response);
        }

        if (response.TryGetProperty("Data", out var data))
        {
            return data.Clone();
        }

        return new
        {
            success = false,
            message = "Capture options data was not available."
        };
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = AutomationSnapshotFormatter.Get(response, "Message", "Command failed."),
            errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty),
            status = AutomationSnapshotFormatter.Get(response, "Status", "error")
        };
    }
}

[McpServerToolType]
// MCP tools for pipeline/debug knobs that affect capture and preview behavior.
public static class PipelineSettingsTools
{
    [McpServerTool, Description("Configure pipeline settings: HDR, audio capture, audio preview, true HDR preview, and output path. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_pipeline(
        PipeClient pipeClient,
        [Description("Enable or disable HDR")] bool? hdrEnabled = null,
        [Description("Enable or disable audio capture")] bool? audioEnabled = null,
        [Description("Enable or disable audio preview")] bool? audioPreviewEnabled = null,
        [Description("Enable or disable true HDR preview (GPU HDR tone-mapping). Must stop preview first.")] bool? trueHdrPreviewEnabled = null,
        [Description("Output folder path for recordings")] string? outputPath = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No pipeline setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetHdrEnabled, "SetHdrEnabled", "enabled", hdrEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetTrueHdrPreviewEnabled, "SetTrueHdrPreviewEnabled", "enabled", trueHdrPreviewEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetAudioEnabled, "SetAudioEnabled", "enabled", audioEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetAudioPreviewEnabled, "SetAudioPreviewEnabled", "enabled", audioPreviewEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetOutputPath, "SetOutputPath", "outputPath", outputPath))
            .ConfigureAwait(false);

    [McpServerTool, Description("Set device audio mode to HDMI or analog")]
    public static async Task<CallToolResult> configure_audio_mode(
        PipeClient pipeClient,
        [Description("Audio mode: hdmi or analog")] string mode)
    {
        var payload = new Dictionary<string, object?> { ["mode"] = mode.ToLowerInvariant() };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetDeviceAudioMode, "SetDeviceAudioMode", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Set analog audio input gain (0-100%)")]
    public static async Task<CallToolResult> configure_analog_gain(
        PipeClient pipeClient,
        [Description("Gain value as a percentage (0-100)")] double gainPercent)
    {
        var payload = new Dictionary<string, object?> { ["gain"] = gainPercent };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetAnalogAudioGain, "SetAnalogAudioGain", payload).ConfigureAwait(false);
    }

}

[McpServerToolType]
public static class WindowTools
{
    [McpServerTool, Description("Control the application window: minimize, maximize, restore, close (requires arm_close), snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move (requires x,y), resize (requires width,height)")]
    public static async Task<CallToolResult> window_action(
        PipeClient pipeClient,
        [Description("Window action: minimize, maximize, restore, close, snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move, resize")] string action,
        [Description("Arm window close before sending a close action")] bool armClose = false,
        [Description("X position in pixels (required for move)")] int? x = null,
        [Description("Y position in pixels (required for move)")] int? y = null,
        [Description("Width in pixels (required for resize)")] int? width = null,
        [Description("Height in pixels (required for resize)")] int? height = null)
    {
        var results = new List<string>();

        // Normalize snake_case to PascalCase for enum parsing (e.g. snap_left -> SnapLeft)
        var normalizedAction = string.Join("", action.Trim().Split('_').Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : p));

        if (armClose && string.Equals(normalizedAction, "Close", StringComparison.Ordinal))
        {
            var armPayload = new Dictionary<string, object?>
            {
                ["armed"] = true
            };
            var armResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.ArmClose, armPayload).ConfigureAwait(false);
            results.Add(ToolCommandFormatter.FormatCommandResponse(armResponse, "ArmClose"));
            if (!Sussudio.Tools.AutomationSnapshotFormatter.IsSuccess(armResponse))
            {
                return McpToolResultFactory.FromText(string.Join(Environment.NewLine, results), isError: true);
            }
        }

        var actionPayload = new Dictionary<string, object?>
        {
            ["action"] = normalizedAction
        };

        if (x.HasValue) actionPayload["x"] = x.Value;
        if (y.HasValue) actionPayload["y"] = y.Value;
        if (width.HasValue) actionPayload["width"] = width.Value;
        if (height.HasValue) actionPayload["height"] = height.Value;

        var actionResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.WindowAction, actionPayload).ConfigureAwait(false);
        results.Add(ToolCommandFormatter.FormatCommandResponse(actionResponse, "WindowAction"));

        return McpToolResultFactory.FromResponse(actionResponse, string.Join(Environment.NewLine, results));
    }

    [McpServerTool, Description("Enter or exit full-screen mode")]
    public static async Task<CallToolResult> set_full_screen(
        PipeClient pipeClient,
        [Description("True to enter full-screen mode, false to exit")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetFullScreenEnabled,
                "SetFullScreenEnabled",
                new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Open the current recordings output folder in Explorer")]
    public static async Task<CallToolResult> open_recordings_folder(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.OpenRecordingsFolder,
                "OpenRecordingsFolder")
            .ConfigureAwait(false);
    }

}

[McpServerToolType]
// MCP tools for UI-only settings like stats visibility and window layout.
public static class UiSettingsTools
{
    [McpServerTool, Description("Configure UI-facing settings that matter to automation: show-all compatibility, preview monitoring volume, and stats panel visibility. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_ui(
        PipeClient pipeClient,
        [Description("Compatibility setting. Show-all capture options are always enabled; provided values are acknowledged as a no-op.")] bool? showAllCaptureOptions = null,
        [Description("Preview volume percentage from 0 to 100")] double? previewVolumePercent = null,
        [Description("Show or hide the stats panel")] bool? statsVisible = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No UI setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetShowAllCaptureOptions, "SetShowAllCaptureOptions", "enabled", showAllCaptureOptions),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetPreviewVolume, "SetPreviewVolume", "previewVolumePercent", previewVolumePercent),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetStatsVisible, "SetStatsVisible", "visible", statsVisible))
            .ConfigureAwait(false);

    [McpServerTool, Description("Show or hide the settings panel")]
    public static async Task<CallToolResult> configure_settings_panel(
        PipeClient pipeClient,
        [Description("True to show the settings panel, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetSettingsVisible, "SetSettingsVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide the frametime graph overlay")]
    public static async Task<CallToolResult> configure_frametime_graph(
        PipeClient pipeClient,
        [Description("True to show the frametime graph, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFrameTimeOverlayVisible, "SetFrameTimeOverlayVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide the Flashback timeline UI")]
    public static async Task<CallToolResult> configure_flashback_timeline(
        PipeClient pipeClient,
        [Description("True to show the Flashback timeline, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFlashbackTimelineVisible, "SetFlashbackTimelineVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide a specific stats section by name")]
    public static async Task<CallToolResult> configure_stats_section(
        PipeClient pipeClient,
        [Description("Section name (e.g. Capture, Audio, Pipeline, Recording, Flashback, Performance, Memory, Preview, Source)")] string section,
        [Description("True to show the section, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?>
        {
            ["section"] = section,
            ["visible"] = visible
        };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetStatsSectionVisible, "SetStatsSectionVisible", payload).ConfigureAwait(false);
    }
}

[McpServerToolType]
// MCP tools for preview start/stop and preview-related toggles.
public static class PreviewTools
{
    [McpServerTool, Description("Start or stop the live preview")]
    public static async Task<CallToolResult> control_preview(
        PipeClient pipeClient,
        [Description("True to start preview, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetPreviewEnabled,
                "SetPreviewEnabled",
                payload)
            .ConfigureAwait(false);
    }
}

[McpServerToolType]
// MCP tools for starting and stopping user recordings.
public static class RecordingTools
{
    [McpServerTool, Description("Start or stop recording")]
    public static async Task<CallToolResult> control_recording(
        PipeClient pipeClient,
        [Description("True to start recording, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetRecordingEnabled,
                "SetRecordingEnabled",
                payload)
            .ConfigureAwait(false);
    }
}

[McpServerToolType]
// MCP wait helper for polling automation conditions until the app reaches a
// requested observable state.
public static class WaitTools
{
    private const int ResponseTimeoutBufferMs = 5000;

    [McpServerTool, Description("Wait for a condition to be met. Blocks until satisfied or timeout. Conditions: PreviewFramesActive, PreviewRendererHealthy, AudioSignalPresent, RecordingFileGrowing, RecordingStopped, VerificationReady, HdrModeApplied, PerformancePerfectionMet, HdrVerificationReady, AudioFramesFlowing, VideoFramesFlowing")]
    public static async Task<CallToolResult> wait_for_condition(
        PipeClient pipeClient,
        [Description("Condition name to wait for")] string condition,
        [Description("Timeout in milliseconds (default: 10000)")] int timeoutMs = 10000,
        [Description("Polling interval in milliseconds (default: 250)")] int pollMs = 250)
    {
        var payload = new Dictionary<string, object?>
        {
            ["condition"] = condition,
            ["timeoutMs"] = timeoutMs,
            ["pollMs"] = pollMs
        };

        var responseTimeoutMs = GetWaitForConditionResponseTimeoutMs(timeoutMs);
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "Condition result: MET" : "Condition result: NOT MET");
        builder.AppendLine($"Message: {AutomationSnapshotFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Condition: {AutomationSnapshotFormatter.Get(data, "condition")}");
            builder.AppendLine($"Met: {AutomationSnapshotFormatter.Get(data, "met")}");
            builder.AppendLine($"TimeoutMs: {AutomationSnapshotFormatter.Get(data, "timeoutMs")}");
            builder.AppendLine($"PollMs: {AutomationSnapshotFormatter.Get(data, "pollMs")}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    internal static int GetWaitForConditionResponseTimeoutMs(int timeoutMs)
    {
        var requestedResponseTimeoutMs = (long)timeoutMs + ResponseTimeoutBufferMs;
        var catalogResponseTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition);
        var responseTimeoutMs = Math.Max(requestedResponseTimeoutMs, catalogResponseTimeoutMs);
        return responseTimeoutMs > int.MaxValue
            ? int.MaxValue
            : (int)responseTimeoutMs;
    }
}

[McpServerToolType]
// MCP tools for Flashback timeline playback, export, and backend settings.
public static class FlashbackTools
{
    [McpServerTool, Description("Enable or disable the Flashback rolling buffer. Disable it before dedicated LibAv recording verification.")]
    public static async Task<CallToolResult> flashback_enabled(
        PipeClient pipeClient,
        [Description("True to enable Flashback, false to disable it")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetFlashbackEnabled,
                label: "SetFlashbackEnabled",
                payload: new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Restart Flashback to apply deferred settings. This clears the current rolling buffer.")]
    public static async Task<CallToolResult> flashback_apply(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.RestartFlashback,
                label: "RestartFlashback")
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("List all flashback buffer segments with their file paths, durations, and frame counts")]
    public static async Task<CallToolResult> flashback_segments(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.FlashbackGetSegments).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackGetSegments: {message}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    [McpServerTool, Description("Control flashback playback: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points")]
    public static async Task<CallToolResult> flashback_action(
        PipeClient pipeClient,
        [Description("Action: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points")] string action,
        [Description("Position in milliseconds (required for seek, begin_scrub, and update_scrub; optional for end_scrub)")] double? positionMs = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "Flashback action is required. Expected play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points.",
                nameof(action));
        }

        var normalizedAction = action.Replace("_", "-").ToLowerInvariant();
        if (normalizedAction is not ("play" or "pause" or "go-live" or "seek" or "begin-scrub" or "update-scrub" or "end-scrub" or "set-in-point" or "set-out-point" or "clear-in-out-points"))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action),
                "Flashback action must be one of: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points.");
        }

        if ((normalizedAction == "seek" ||
             normalizedAction == "begin-scrub" ||
             normalizedAction == "update-scrub") &&
            !positionMs.HasValue)
        {
            throw new ArgumentException("Flashback seek, begin_scrub, and update_scrub require positionMs.", nameof(positionMs));
        }

        var payload = new Dictionary<string, object?>
        {
            ["action"] = normalizedAction
        };

        if (positionMs.HasValue)
        {
            if (!double.IsFinite(positionMs.Value) ||
                positionMs.Value < 0 ||
                positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(positionMs), "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
            }

            payload["positionMs"] = positionMs.Value;
        }

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.FlashbackAction,
                label: $"FlashbackAction({normalizedAction})",
                payload: payload)
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Export flashback buffer to an MP4 file. Exports the most recent N seconds of the rolling buffer. Refuses to overwrite an existing destination file unless force=true.")]
    public static async Task<CallToolResult> flashback_export(
        PipeClient pipeClient,
        [Description("Number of seconds to export from the buffer (default: 300)")] double seconds = 300,
        [Description("Output file path (default: temp/flashback_export_<timestamp>.mp4)")] string? outputPath = null,
        [Description("True to export the current in/out selection instead of the most recent N seconds")] bool useSelectionRange = false,
        [Description("True to overwrite an existing file at outputPath. Default false: the export is refused if the destination already exists, preserving any prior take.")] bool force = false)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        outputPath ??= $"temp/flashback_export_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var payload = new Dictionary<string, object?>
        {
            ["seconds"] = seconds,
            ["outputPath"] = outputPath,
            ["useSelectionRange"] = useSelectionRange,
            ["force"] = force
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.FlashbackExport, payload).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackExport: {message}");
        builder.AppendLine(useSelectionRange
            ? $"Requested: selected range -> {outputPath}"
            : $"Requested: {seconds}s -> {outputPath}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            var failureKind = AutomationSnapshotFormatter.Get(data, "FailureKind", string.Empty);
            if (!string.IsNullOrWhiteSpace(failureKind))
            {
                builder.AppendLine($"FailureKind: {failureKind}");
            }

            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }
}

[McpServerToolType]
// MCP tools for verifying recordings and exported Flashback files.
public static class VerificationTools
{
    [McpServerTool, Description("Run ffprobe validation on the last recording. Checks codec, resolution, HDR metadata parity.")]
    public static async Task<CallToolResult> verify_recording(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.VerifyLastRecording).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return McpToolResultFactory.FromResponse(response, message);
        }

        return McpToolResultFactory.FromResponse(response, BuildRecordingVerificationText(response, verification, message));
    }

    [McpServerTool, Description("Run programmatic assertions against the current app state snapshot. Each assertion has a field name, operator (eq/neq/gt/gte/lt/lte/contains), and expected value.")]
    public static async Task<CallToolResult> assert_snapshot(
        PipeClient pipeClient,
        [Description("JSON array of assertion objects with field, op, value")] string assertions)
    {
        if (!TryParseAssertionArray(assertions, out var parsedAssertions, out var parseError))
        {
            return McpToolResultFactory.FromText(parseError!, isError: true);
        }

        var payload = new Dictionary<string, object?>
        {
            ["assertions"] = parsedAssertions
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload).ConfigureAwait(false);
        return McpToolResultFactory.FromResponse(response, BuildSnapshotAssertionText(response));
    }

    [McpServerTool, Description("Run ffprobe validation on an arbitrary file path. Checks codec, resolution, HDR metadata.")]
    public static async Task<CallToolResult> verify_file(
        PipeClient pipeClient,
        [Description("Absolute path to the media file to verify")] string filePath,
        [Description("Optional verifier profile, e.g. flashback-export for Flashback exports whose codec may differ from the selected recording format.")] string? verificationProfile = null)
    {
        var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
        if (!string.IsNullOrWhiteSpace(verificationProfile))
        {
            payload["verificationProfile"] = verificationProfile;
        }

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.VerifyFile, payload).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return McpToolResultFactory.FromResponse(response, message);
        }

        return McpToolResultFactory.FromResponse(response, BuildFileVerificationText(filePath, response, verification, message));
    }

    private static bool TryParseAssertionArray(string assertions, out JsonElement parsedAssertions, out string? error)
    {
        parsedAssertions = default;
        error = null;

        if (string.IsNullOrWhiteSpace(assertions))
        {
            error = "The assertions parameter must be a JSON array string.";
            return false;
        }

        try
        {
            using var assertionsDocument = JsonDocument.Parse(assertions);
            if (assertionsDocument.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The assertions parameter must be a JSON array string.";
                return false;
            }

            parsedAssertions = assertionsDocument.RootElement.Clone();
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid assertions JSON: {ex.Message}";
            return false;
        }
    }

    private static bool TryGetVerification(JsonElement response, out JsonElement verification)
    {
        verification = default;

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("Verification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Snapshot", out var snapshot) &&
            snapshot.ValueKind == JsonValueKind.Object &&
            snapshot.TryGetProperty("LastVerification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return false;
    }

    private static string BuildRecordingVerificationText(JsonElement response, JsonElement verification, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== Recording Verification: PASS ==" : "== Recording Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"Output: {AutomationSnapshotFormatter.Get(verification, "OutputPath")} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Mode: {AutomationSnapshotFormatter.Get(verification, "VerificationMode")} | Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");
        builder.AppendLine($"HDR: Level={AutomationSnapshotFormatter.Get(verification, "HdrVerificationLevel")} Metadata={AutomationSnapshotFormatter.Get(verification, "HdrMetadataPresent")} Colorimetry={AutomationSnapshotFormatter.Get(verification, "HdrColorimetryValid")} Mastering={AutomationSnapshotFormatter.Get(verification, "HdrMasteringMetadataPresent")}");
        builder.AppendLine(FormatJsonArrayList(verification, "Mismatches", "Mismatches"));

        return builder.ToString().TrimEnd();
    }

    private static string BuildSnapshotAssertionText(JsonElement response)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "Snapshot assertions: PASS" : "Snapshot assertions: FAIL");
        builder.AppendLine($"Message: {AutomationSnapshotFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Assertions: {AutomationSnapshotFormatter.Get(data, "assertions")}");
            builder.AppendLine($"Passed: {AutomationSnapshotFormatter.Get(data, "passed")}");

            if (data.TryGetProperty("failures", out var failures) && failures.ValueKind == JsonValueKind.Array)
            {
                builder.AppendLine(FormatJsonArrayList(failures, "Failures"));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildFileVerificationText(string filePath, JsonElement response, JsonElement verification, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== File Verification: PASS ==" : "== File Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"File: {filePath} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");

        return builder.ToString().TrimEnd();
    }

    private static string FormatJsonArrayList(JsonElement parent, string propertyName, string label)
    {
        if (parent.TryGetProperty(propertyName, out var values) && values.ValueKind == JsonValueKind.Array)
        {
            return FormatJsonArrayList(values, label);
        }

        return $"{label}: None";
    }

    private static string FormatJsonArrayList(JsonElement values, string label)
    {
        var valueList = values.EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return valueList.Count == 0
            ? $"{label}: None"
            : $"{label}: {string.Join("; ", valueList)}";
    }
}
