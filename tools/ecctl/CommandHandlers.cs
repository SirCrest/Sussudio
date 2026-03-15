using System.Globalization;
using System.Text.Json;

namespace EcCtl;

internal static class CommandHandlers
{
    public static Task<int> ExecuteAsync(PipeTransport transport, IReadOnlyList<string> arguments, bool globalJson)
    {
        var context = new CommandContext(transport, arguments, globalJson);
        return arguments[0].ToLowerInvariant() switch
        {
            "state" => HandleStateAsync(context),
            "diagnostics" => HandleDiagnosticsAsync(context),
            "options" => HandleOptionsAsync(context),
            "timeline" => HandleTimelineAsync(context),
            "memory" => HandleMemoryAsync(context),
            "preview" => HandlePreviewAsync(context),
            "record" => HandleRecordAsync(context),
            "screenshot" => HandleCaptureAsync(context, "CaptureWindowScreenshot", "temp/window_screenshot.png"),
            "frame" => HandleCaptureAsync(context, "CapturePreviewFrame", "temp/preview_capture.bmp"),
            "set" => HandleSetAsync(context),
            "device" => HandleDeviceAsync(context),
            "window" => HandleWindowAsync(context),
            "wait" => HandleWaitAsync(context),
            "verify" => HandleSimpleCommandAsync(context, "VerifyLastRecording", includeData: true),
            "assert" => HandleAssertAsync(context),
            "probe" => HandleProbeAsync(context),
            "stats" => HandleStatsAsync(context),
            "settings" => HandleSettingsAsync(context),
            _ => throw new UsageException($"Unknown command '{arguments[0]}'.")
        };
    }

    private static async Task<int> HandleStateAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var response = await context.Transport.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatSnapshot);
    }

    private static async Task<int> HandleDiagnosticsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 100;
        EnsureNoArgs(context.Rest, "diagnostics [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            "GetDiagnostics",
            new Dictionary<string, object?> { ["maxEvents"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatDiagnostics);
    }

    private static async Task<int> HandleOptionsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "options [--json]");

        var response = await context.Transport.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatOptions);
    }

    private static async Task<int> HandleTimelineAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 240;
        EnsureNoArgs(context.Rest, "timeline [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            "GetPerformanceTimeline",
            new Dictionary<string, object?> { ["maxEntries"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatTimeline);
    }

    private static async Task<int> HandleMemoryAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "memory [--json]");

        var response = await context.Transport.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatMemory);
    }

    private static Task<int> HandlePreviewAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "preview start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "preview start|stop");
        return HandleSimpleCommandAsync(
            context,
            "SetPreviewEnabled",
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("preview expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleRecordAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "record start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "record start|stop");
        return HandleSimpleCommandAsync(
            context,
            "SetRecordingEnabled",
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("record expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleCaptureAsync(CommandContext context, string commandName, string defaultPath)
    {
        var outputPath = context.Rest.Count == 0 ? defaultPath : JoinRemaining(context.Rest, 0);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(
            context,
            commandName,
            new Dictionary<string, object?> { ["outputPath"] = outputPath },
            includeData: true);
    }

    private static Task<int> HandleSetAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "set <option> ...").ToLowerInvariant();
        return subcommand switch
        {
            "resolution" => SendSetValueAsync(context, "SetResolution", "resolution", JoinRemaining(context.Rest, 1), "set resolution <value>"),
            "fps" => SendSetValueAsync(context, "SetFrameRate", "frameRate", ParseDouble(RequireWord(context.Rest, 1, "set fps <value>")), "set fps <value>"),
            "format" => SendSetValueAsync(context, "SetRecordingFormat", "format", JoinRemaining(context.Rest, 1), "set format <value>"),
            "quality" => SendSetValueAsync(context, "SetQuality", "quality", JoinRemaining(context.Rest, 1), "set quality <value>"),
            "bitrate" => SendSetValueAsync(context, "SetCustomBitrate", "bitrateMbps", ParseDouble(RequireWord(context.Rest, 1, "set bitrate <value>")), "set bitrate <value>"),
            "preset" => SendSetValueAsync(context, "SetPreset", "preset", JoinRemaining(context.Rest, 1), "set preset <value>"),
            "split" => SendSetValueAsync(context, "SetSplitEncodeMode", "splitEncodeMode", JoinRemaining(context.Rest, 1), "set split <value>"),
            "video-format" => SendSetValueAsync(context, "SetVideoFormat", "videoFormat", JoinRemaining(context.Rest, 1), "set video-format <value>"),
            "decoders" => SendSetValueAsync(context, "SetMjpegDecoderCount", "decoderCount", ParseInt(RequireWord(context.Rest, 1, "set decoders <value>")), "set decoders <value>"),
            "hdr" => SendSetValueAsync(context, "SetHdrEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr on|off")), "set hdr on|off"),
            "hdr-preview" => SendSetValueAsync(context, "SetTrueHdrPreviewEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr-preview on|off")), "set hdr-preview on|off"),
            "audio" => SendSetValueAsync(context, "SetAudioEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio on|off")), "set audio on|off"),
            "audio-preview" => SendSetValueAsync(context, "SetAudioPreviewEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio-preview on|off")), "set audio-preview on|off"),
            "volume" => SendSetValueAsync(context, "SetPreviewVolume", "previewVolumePercent", ParseDouble(RequireWord(context.Rest, 1, "set volume <value>")), "set volume <value>"),
            "audio-mode" => SendSetValueAsync(context, "SetDeviceAudioMode", "mode", RequireWord(context.Rest, 1, "set audio-mode hdmi|analog"), "set audio-mode hdmi|analog"),
            "gain" => SendSetValueAsync(context, "SetAnalogAudioGain", "gain", ParseDouble(RequireWord(context.Rest, 1, "set gain <value>")), "set gain <value>"),
            "output" => SendSetValueAsync(context, "SetOutputPath", "outputPath", JoinRemaining(context.Rest, 1), "set output <path>"),
            "show-all" => SendSetValueAsync(context, "SetShowAllCaptureOptions", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set show-all on|off")), "set show-all on|off"),
            _ => throw new UsageException($"Unknown set command '{subcommand}'.")
        };
    }

    private static async Task<int> HandleDeviceAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "device <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "list":
            {
                EnsureArgCount(context.Rest, 1, "device list");
                var refreshResponse = await context.Transport.SendCommandAsync("RefreshDevices").ConfigureAwait(false);
                if (!IsSuccess(refreshResponse))
                {
                    return WriteResponse(refreshResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                var optionsResponse = await context.Transport.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
                return WriteResponse(optionsResponse, context.GlobalJson, Formatters.FormatDeviceList);
            }
            case "select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    "SelectDevice",
                    new Dictionary<string, object?> { ["deviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "audio-select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device audio-select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    "SelectAudioInputDevice",
                    new Dictionary<string, object?> { ["audioDeviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "custom-audio":
                EnsureArgCount(context.Rest, 2, "device custom-audio on|off");
                return await HandleSimpleCommandAsync(
                    context,
                    "SetCustomAudioInput",
                    new Dictionary<string, object?> { ["enabled"] = ParseOnOff(RequireWord(context.Rest, 1, "device custom-audio on|off")) },
                    includeData: false).ConfigureAwait(false);
            default:
                throw new UsageException($"Unknown device command '{subcommand}'.");
        }
    }

    private static async Task<int> HandleWindowAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "window <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "close":
            {
                EnsureArgCount(context.Rest, 1, "window close");
                var armResponse = await context.Transport.SendCommandAsync(
                    "ArmClose",
                    new Dictionary<string, object?> { ["armed"] = true }).ConfigureAwait(false);
                if (!IsSuccess(armResponse))
                {
                    return WriteResponse(armResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                return await HandleSimpleCommandAsync(
                    context,
                    "WindowAction",
                    new Dictionary<string, object?> { ["action"] = "Close" },
                    includeData: false).ConfigureAwait(false);
            }
            case "minimize":
            case "maximize":
            case "restore":
            case "center":
                EnsureArgCount(context.Rest, 1, $"window {subcommand}");
                return await HandleSimpleCommandAsync(
                    context,
                    "WindowAction",
                    new Dictionary<string, object?> { ["action"] = Capitalize(subcommand) },
                    includeData: false).ConfigureAwait(false);
            case "snap":
                EnsureArgCount(context.Rest, 2, "window snap <dir>");
                return await HandleSimpleCommandAsync(
                    context,
                    "WindowAction",
                    new Dictionary<string, object?> { ["action"] = MapSnapAction(RequireWord(context.Rest, 1, "window snap <dir>")) },
                    includeData: false).ConfigureAwait(false);
            case "move":
                EnsureArgCount(context.Rest, 3, "window move <x> <y>");
                return await HandleSimpleCommandAsync(
                    context,
                    "WindowAction",
                    new Dictionary<string, object?>
                    {
                        ["action"] = "Move",
                        ["x"] = ParseInt(RequireWord(context.Rest, 1, "window move <x> <y>")),
                        ["y"] = ParseInt(RequireWord(context.Rest, 2, "window move <x> <y>"))
                    },
                    includeData: false).ConfigureAwait(false);
            case "resize":
                EnsureArgCount(context.Rest, 3, "window resize <w> <h>");
                return await HandleSimpleCommandAsync(
                    context,
                    "WindowAction",
                    new Dictionary<string, object?>
                    {
                        ["action"] = "Resize",
                        ["width"] = ParseInt(RequireWord(context.Rest, 1, "window resize <w> <h>")),
                        ["height"] = ParseInt(RequireWord(context.Rest, 2, "window resize <w> <h>"))
                    },
                    includeData: false).ConfigureAwait(false);
            default:
                throw new UsageException($"Unknown window command '{subcommand}'.");
        }
    }

    private static async Task<int> HandleWaitAsync(CommandContext context)
    {
        var condition = RequireWord(context.Rest, 0, "wait <condition> [--timeout ms] [--poll ms]");
        var args = context.Rest.Skip(1).ToList();
        var timeoutMs = ParseOptionalIntFlag(args, "--timeout");
        var pollMs = ParseOptionalIntFlag(args, "--poll");
        EnsureNoArgs(args, "wait <condition> [--timeout ms] [--poll ms]");

        var payload = new Dictionary<string, object?> { ["condition"] = condition };
        if (timeoutMs.HasValue)
        {
            payload["timeoutMs"] = timeoutMs.Value;
        }

        if (pollMs.HasValue)
        {
            payload["pollMs"] = pollMs.Value;
        }

        var responseTimeoutMs = Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000);
        var response = await context.Transport.SendCommandAsync(
            "WaitForCondition",
            payload,
            responseTimeoutMs).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleAssertAsync(CommandContext context)
    {
        var assertionsJson = JoinRemaining(context.Rest, 0);
        if (string.IsNullOrWhiteSpace(assertionsJson))
        {
            throw new UsageException("assert <json>");
        }

        using var document = JsonDocument.Parse(assertionsJson);
        var response = await context.Transport.SendCommandAsync(
            "AssertSnapshot",
            new Dictionary<string, object?> { ["assertions"] = document.RootElement.Clone() }).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static Task<int> HandleProbeAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "probe source|color").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "probe source|color");
        return subcommand switch
        {
            "source" => HandleSimpleCommandAsync(context, "ProbeVideoSource", includeData: true),
            "color" => HandleSimpleCommandAsync(context, "ProbePreviewColor", includeData: true),
            _ => throw new UsageException($"Unknown probe command '{subcommand}'.")
        };
    }

    private static Task<int> HandleStatsAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "stats show|hide").ToLowerInvariant();
        if (subcommand == "section")
        {
            if (context.Rest.Count < 3)
            {
                throw new UsageException("stats section <name> show|hide");
            }

            var sectionName = JoinRange(context.Rest, 1, context.Rest.Count - 1);
            var visible = ParseShowHide(context.Rest[^1], "stats section <name> show|hide");
            return HandleSimpleCommandAsync(
                context,
                "SetStatsSectionVisible",
                new Dictionary<string, object?>
                {
                    ["section"] = sectionName,
                    ["visible"] = visible
                },
                includeData: false);
        }

        EnsureArgCount(context.Rest, 1, "stats show|hide");
        return HandleSimpleCommandAsync(
            context,
            "SetStatsVisible",
            new Dictionary<string, object?> { ["visible"] = ParseShowHide(subcommand, "stats show|hide") },
            includeData: false);
    }

    private static Task<int> HandleSettingsAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "settings show|hide");
        var visible = ParseShowHide(context.Rest[0], "settings show|hide");
        return HandleSimpleCommandAsync(
            context,
            "SetSettingsVisible",
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }

    private static Task<int> SendSetValueAsync(CommandContext context, string commandName, string propertyName, object value, string usage)
    {
        if (context.Rest.Count < 2)
        {
            throw new UsageException(usage);
        }

        return HandleSimpleCommandAsync(
            context,
            commandName,
            new Dictionary<string, object?> { [propertyName] = value },
            includeData: false);
    }

    private static async Task<int> HandleSimpleCommandAsync(
        CommandContext context,
        string commandName,
        Dictionary<string, object?>? payload = null,
        bool includeData = false)
    {
        var response = await context.Transport.SendCommandAsync(commandName, payload).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, value => Formatters.FormatResult(value, includeData));
    }

    private static int WriteResponse(JsonElement response, bool json, Func<JsonElement, string> formatter)
    {
        Console.WriteLine(json ? Formatters.PrettyJson(response) : formatter(response));
        return IsSuccess(response) ? 0 : 3;
    }

    private static bool IsSuccess(JsonElement response)
        => response.ValueKind == JsonValueKind.Object &&
           response.TryGetProperty("Success", out var success) &&
           success.ValueKind == JsonValueKind.True;

    private static bool ConsumeFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        args.RemoveAt(index);
        return true;
    }

    private static int? ParseOptionalIntFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = ParseInt(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string RequireWord(IReadOnlyList<string> args, int index, string usage)
    {
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new UsageException(usage);
        }

        return args[index];
    }

    private static void EnsureArgCount(IReadOnlyList<string> args, int expected, string usage)
    {
        if (args.Count != expected)
        {
            throw new UsageException(usage);
        }
    }

    private static void EnsureNoArgs(IReadOnlyList<string> args, string usage)
    {
        if (args.Count != 0)
        {
            throw new UsageException(usage);
        }
    }

    private static int ParseInt(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid integer value '{value}'.");
        }

        return parsed;
    }

    private static double ParseDouble(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid numeric value '{value}'.");
        }

        return parsed;
    }

    private static bool ParseOnOff(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => throw new UsageException($"Invalid boolean value '{value}'. Use on/off, true/false, or 1/0.")
        };
    }

    private static bool ParseShowHide(string value, string usage)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "show" => true,
            "hide" => false,
            _ => throw new UsageException(usage)
        };
    }

    private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)
    {
        if (startIndex >= args.Count)
        {
            throw new UsageException("Missing required value.");
        }

        return JoinRange(args, startIndex, args.Count);
    }

    private static string JoinRange(IReadOnlyList<string> args, int startIndex, int endExclusive)
        => string.Join(" ", args.Skip(startIndex).Take(endExclusive - startIndex));

    private static string MapSnapAction(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "left" => "SnapLeft",
            "right" => "SnapRight",
            "top-left" => "SnapTopLeft",
            "top-right" => "SnapTopRight",
            "bottom-left" => "SnapBottomLeft",
            "bottom-right" => "SnapBottomRight",
            _ => throw new UsageException("window snap left|right|top-left|top-right|bottom-left|bottom-right")
        };
    }

    private static string Capitalize(string value)
        => char.ToUpperInvariant(value[0]) + value[1..];

    private sealed class CommandContext
    {
        public CommandContext(PipeTransport transport, IReadOnlyList<string> arguments, bool globalJson)
        {
            Transport = transport;
            GlobalJson = globalJson;
            Rest = arguments.Skip(1).ToList();
        }

        public PipeTransport Transport { get; }
        public bool GlobalJson { get; }
        public List<string> Rest { get; }
    }
}
