using System.Text.Json;
using System.Globalization;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// CLI command layer over the shared automation pipe. Handlers keep console
// parsing, command payload shape, and human-readable output together while
// leaving transport details in AutomationPipeClient.
internal static partial class CommandHandlers
{
    public static Task<int> ExecuteAsync(PipeTransport transport, IReadOnlyList<string> arguments, bool globalJson)
    {
        var context = new CommandContext(transport, arguments, globalJson);
        return arguments[0].ToLowerInvariant() switch
        {
            "state" => HandleStateAsync(context),
            "diagnostics" => HandleDiagnosticsAsync(context),
            "options" => HandleOptionsAsync(context),
            "manifest" => HandleManifestAsync(context),
            "timeline" => HandleTimelineAsync(context),
            "memory" => HandleMemoryAsync(context),
            "audio-ramp-trace" => HandleAudioRampTraceAsync(context),
            "presentmon" => HandlePresentMonAsync(context),
            "diagnostic-session" or "session" => HandleDiagnosticSessionAsync(context),
            "preview" => HandlePreviewAsync(context),
            "record" => HandleRecordAsync(context),
            "screenshot" => HandleCaptureAsync(context, AutomationCommandKind.CaptureWindowScreenshot, "temp/window_screenshot.png"),
            "frame" => HandleCaptureAsync(context, AutomationCommandKind.CapturePreviewFrame, "temp/preview_capture.bmp"),
            "recordings" => HandleRecordingsAsync(context),
            "set" => HandleSetAsync(context),
            "device" => HandleDeviceAsync(context),
            "window" => HandleWindowAsync(context),
            "wait" => HandleWaitAsync(context),
            "verify" => HandleVerifyAsync(context),
            "assert" => HandleAssertAsync(context),
            "probe" => HandleProbeAsync(context),
            "stats" => HandleStatsAsync(context),
            "frametime" or "frame-time" => HandleFrameTimeAsync(context),
            "settings" => HandleSettingsAsync(context),
            "flashback" => HandleFlashbackAsync(context),
            _ => throw new UsageException($"Unknown command '{arguments[0]}'.")
        };
    }

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

    private static async Task<int> HandleSimpleCommandAsync(
        CommandContext context,
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        bool includeData = false)
    {
        var response = await context.Transport.SendCommandAsync(kind, payload).ConfigureAwait(false);
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

    private static string? ParseOptionalStringFlag(List<string> args, string flag)
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

        var value = args[index + 1];
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static long? ParseOptionalLongFlag(List<string> args, string flag)
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

        var value = ParseLong(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string PrettyJson<T>(T value)
        => JsonSerializer.Serialize(value, ToolJsonOptions.Pretty);

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static int ParseInt(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid integer value '{value}'.");
        }

        return parsed;
    }

    private static long ParseLong(string value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
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

    private static double ParseFlashbackExportSeconds(string value)
    {
        var parsed = ParseDouble(value);
        if (!double.IsFinite(parsed) || parsed <= 0 || parsed > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new UsageException("Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        return parsed;
    }

    private static object? ParseAssertionValue(string value)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        return value;
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

    private static string NormalizeRecordingFormat(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "h264" or "h.264" or "avc" => "H.264",
            "hevc" or "h265" or "h.265" => "HEVC",
            "av1" => "AV1",
            _ => value, // pass through as-is for server-side validation
        };
    }

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
            AutomationCommandKind.WaitForCondition,
            payload,
            responseTimeoutMs).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleAssertAsync(CommandContext context)
    {
        object assertionsPayload;
        if (context.Rest.Count == 3 && !LooksLikeJson(context.Rest[0]))
        {
            assertionsPayload = new[]
            {
                new Dictionary<string, object?>
                {
                    ["field"] = context.Rest[0],
                    ["op"] = context.Rest[1],
                    ["value"] = ParseAssertionValue(context.Rest[2])
                }
            };
        }
        else
        {
            var assertionsJson = JoinRemaining(context.Rest, 0);
            if (string.IsNullOrWhiteSpace(assertionsJson))
            {
                throw new UsageException("assert <json> OR assert <field> <op> <value>");
            }

            using var document = JsonDocument.Parse(assertionsJson);
            assertionsPayload = document.RootElement.Clone();
        }

        var response = await context.Transport.SendCommandAsync(
            AutomationCommandKind.AssertSnapshot,
            new Dictionary<string, object?> { ["assertions"] = assertionsPayload }).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static Task<int> HandleProbeAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "probe source|color").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "probe source|color");
        return subcommand switch
        {
            "source" => HandleSimpleCommandAsync(context, AutomationCommandKind.ProbeVideoSource, includeData: true),
            "color" => HandleSimpleCommandAsync(context, AutomationCommandKind.ProbePreviewColor, includeData: true),
            _ => throw new UsageException($"Unknown probe command '{subcommand}'.")
        };
    }

    private static async Task<int> HandleVerifyAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var verificationProfile =
            ParseOptionalStringFlag(context.Rest, "--profile") ??
            ParseOptionalStringFlag(context.Rest, "--verification-profile");
        if (context.Rest.Count > 0)
        {
            var filePath = JoinRemaining(context.Rest, 0);
            var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
            if (!string.IsNullOrWhiteSpace(verificationProfile))
            {
                payload["verificationProfile"] = verificationProfile;
            }

            var response = await context.Transport.SendCommandAsync(
                AutomationCommandKind.VerifyFile,
                payload,
                60000).ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
        else
        {
            // Verify last recording.
            var response = await context.Transport.SendCommandAsync(AutomationCommandKind.VerifyLastRecording).ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
    }

    // Observability command family.
    private static async Task<int> HandleStateAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatSnapshot);
    }

    private static async Task<int> HandleDiagnosticsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 100;
        EnsureNoArgs(context.Rest, "diagnostics [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            Sussudio.Models.AutomationCommandKind.GetDiagnostics,
            new Dictionary<string, object?> { ["maxEvents"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatDiagnostics);
    }

    private static async Task<int> HandleOptionsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "options [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetCaptureOptions).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatOptions);
    }

    private static async Task<int> HandleManifestAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "manifest [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetAutomationManifest).ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleTimelineAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 240;
        EnsureNoArgs(context.Rest, "timeline [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            Sussudio.Models.AutomationCommandKind.GetPerformanceTimeline,
            new Dictionary<string, object?> { ["maxEntries"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatTimeline);
    }

    private static async Task<int> HandleMemoryAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "memory [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatMemory);
    }

    private static async Task<int> HandleAudioRampTraceAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "audio-ramp-trace [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetAudioRampTrace).ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandlePresentMonAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var seconds = ParseOptionalIntFlag(context.Rest, "--seconds") ?? 10;
        var pid = ParseOptionalIntFlag(context.Rest, "--pid");
        var processName = ParseOptionalStringFlag(context.Rest, "--process") ?? "Sussudio";
        var presentMonPath = ParseOptionalStringFlag(context.Rest, "--presentmon");
        var outputPath = ParseOptionalStringFlag(context.Rest, "--output");
        var swapChainAddress = ParseOptionalStringFlag(context.Rest, "--swapchain");
        var appPresentId = ParseOptionalLongFlag(context.Rest, "--app-present-id");
        var appSourceSequenceNumber = ParseOptionalLongFlag(context.Rest, "--app-source-seq");
        var appPresentUtcUnixMs = ParseOptionalLongFlag(context.Rest, "--app-present-utc-ms");
        var captureStartUtcUnixMs = ParseOptionalLongFlag(context.Rest, "--capture-start-utc-ms");
        var keepCsv = ConsumeFlag(context.Rest, "--keep-csv");
        var noGpuVideo = ConsumeFlag(context.Rest, "--no-gpu-video");
        EnsureNoArgs(context.Rest, "presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        var resolved = await TryResolvePreviewPresentCorrelationAsync(context).ConfigureAwait(false);

        var result = await PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            seconds,
            pid,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            captureStartUtcUnixMs,
            presentMonPath,
            outputPath,
            keepCsv,
            !noGpuVideo,
            resolved)).ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : PresentMonProbe.Format(result));
        return result.Success ? 0 : 3;
    }

    private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(CommandContext context)
    {
        try
        {
            var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            return PresentMonProbe.ReadPreviewCorrelation(snapshot);
        }
        catch
        {
            return default;
        }
    }

    private static async Task<int> HandleDiagnosticSessionAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var scenario = ParseOptionalStringFlag(context.Rest, "--scenario") ?? DiagnosticSessionOptions.DefaultScenario;
        var seconds = ParseOptionalIntFlag(context.Rest, "--seconds") ?? DiagnosticSessionOptions.DefaultDurationSeconds;
        var sampleIntervalMs = ParseOptionalIntFlag(context.Rest, "--sample-ms") ?? DiagnosticSessionOptions.DefaultSampleIntervalMs;
        var outputDirectory = ParseOptionalStringFlag(context.Rest, "--output");
        var presentMonPath = ParseOptionalStringFlag(context.Rest, "--presentmon-path");
        var includePresentMon = ConsumeFlag(context.Rest, "--presentmon");
        var verify = ConsumeFlag(context.Rest, "--verify");
        var leaveRunning = ConsumeFlag(context.Rest, "--leave-running");
        EnsureNoArgs(context.Rest, DiagnosticSessionOptions.CliUsage);

        var result = await DiagnosticSessionRunner.RunAsync(
                new DiagnosticSessionOptions
                {
                    Scenario = scenario,
                    DurationSeconds = seconds,
                    SampleIntervalMs = sampleIntervalMs,
                    OutputDirectory = outputDirectory,
                    IncludePresentMon = includePresentMon,
                    PresentMonPath = presentMonPath,
                    VerifyRecording = verify,
                    LeaveRunning = leaveRunning
                },
                (command, payload, responseTimeoutMs) => context.Transport.SendCommandAsync(command, payload, responseTimeoutMs))
            .ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : DiagnosticSessionRunner.Format(result));
        return result.Success ? 0 : 3;
    }

    // CaptureControls command family.
    private static Task<int> HandlePreviewAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "preview start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "preview start|stop");
        return HandleSimpleCommandAsync(
            context,
            AutomationCommandKind.SetPreviewEnabled,
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("preview expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleRecordAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "record start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "record start|stop");
        return HandleSimpleCommandAsync(
            context,
            AutomationCommandKind.SetRecordingEnabled,
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("record expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleCaptureAsync(CommandContext context, AutomationCommandKind kind, string defaultPath)
    {
        var outputPath = context.Rest.Count == 0 ? defaultPath : JoinRemaining(context.Rest, 0);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(
            context,
            kind,
            new Dictionary<string, object?> { ["outputPath"] = outputPath },
            includeData: true);
    }

    private static async Task<int> HandleDeviceAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "device <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "refresh":
                EnsureArgCount(context.Rest, 1, "device refresh");
                return await HandleSimpleCommandAsync(
                    context,
                    AutomationCommandKind.RefreshDevices,
                    includeData: false).ConfigureAwait(false);
            case "list":
            {
                EnsureArgCount(context.Rest, 1, "device list");
                var refreshResponse = await context.Transport.SendCommandAsync(AutomationCommandKind.RefreshDevices).ConfigureAwait(false);
                if (!IsSuccess(refreshResponse))
                {
                    return WriteResponse(refreshResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                var optionsResponse = await context.Transport.SendCommandAsync(AutomationCommandKind.GetCaptureOptions).ConfigureAwait(false);
                return WriteResponse(optionsResponse, context.GlobalJson, Formatters.FormatDeviceList);
            }
            case "select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    AutomationCommandKind.SelectDevice,
                    new Dictionary<string, object?> { ["deviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "audio-select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device audio-select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    AutomationCommandKind.SelectAudioInputDevice,
                    new Dictionary<string, object?> { ["deviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "custom-audio":
                EnsureArgCount(context.Rest, 2, "device custom-audio on|off");
                return await HandleSimpleCommandAsync(
                    context,
                    AutomationCommandKind.SetCustomAudioInput,
                    new Dictionary<string, object?> { ["enabled"] = ParseOnOff(RequireWord(context.Rest, 1, "device custom-audio on|off")) },
                    includeData: false).ConfigureAwait(false);
            default:
                throw new UsageException($"Unknown device command '{subcommand}'.");
        }
    }

    private static Task<int> HandleSetAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "set <option> ...").ToLowerInvariant();
        return subcommand switch
        {
            "resolution" => SendSetValueAsync(context, AutomationCommandKind.SetResolution, "resolution", JoinRemaining(context.Rest, 1), "set resolution <value>"),
            "fps" => SendSetValueAsync(context, AutomationCommandKind.SetFrameRate, "frameRate", ParseDouble(RequireWord(context.Rest, 1, "set fps <value>")), "set fps <value>"),
            "format" => SendSetValueAsync(context, AutomationCommandKind.SetRecordingFormat, "format", NormalizeRecordingFormat(JoinRemaining(context.Rest, 1)), "set format <value>"),
            "quality" => SendSetValueAsync(context, AutomationCommandKind.SetQuality, "quality", JoinRemaining(context.Rest, 1), "set quality <value>"),
            "bitrate" => SendSetValueAsync(context, AutomationCommandKind.SetCustomBitrate, "bitrateMbps", ParseDouble(RequireWord(context.Rest, 1, "set bitrate <value>")), "set bitrate <value>"),
            "preset" => SendSetValueAsync(context, AutomationCommandKind.SetPreset, "preset", JoinRemaining(context.Rest, 1), "set preset <value>"),
            "split" => SendSetValueAsync(context, AutomationCommandKind.SetSplitEncodeMode, "splitEncodeMode", JoinRemaining(context.Rest, 1), "set split <value>"),
            "video-format" => SendSetValueAsync(context, AutomationCommandKind.SetVideoFormat, "videoFormat", JoinRemaining(context.Rest, 1), "set video-format <value>"),
            "decoders" => SendSetValueAsync(context, AutomationCommandKind.SetMjpegDecoderCount, "decoderCount", ParseInt(RequireWord(context.Rest, 1, "set decoders <value>")), "set decoders <value>"),
            "hdr" => SendSetValueAsync(context, AutomationCommandKind.SetHdrEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr on|off")), "set hdr on|off"),
            "hdr-preview" => SendSetValueAsync(context, AutomationCommandKind.SetTrueHdrPreviewEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr-preview on|off")), "set hdr-preview on|off"),
            "audio" => SendSetValueAsync(context, AutomationCommandKind.SetAudioEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio on|off")), "set audio on|off"),
            "audio-preview" => SendSetValueAsync(context, AutomationCommandKind.SetAudioPreviewEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio-preview on|off")), "set audio-preview on|off"),
            "volume" => SendSetValueAsync(context, AutomationCommandKind.SetPreviewVolume, "previewVolumePercent", ParseDouble(RequireWord(context.Rest, 1, "set volume <value>")), "set volume <value>"),
            "audio-mode" => SendSetValueAsync(context, AutomationCommandKind.SetDeviceAudioMode, "mode", RequireWord(context.Rest, 1, "set audio-mode hdmi|analog"), "set audio-mode hdmi|analog"),
            "gain" => SendSetValueAsync(context, AutomationCommandKind.SetAnalogAudioGain, "gain", ParseDouble(RequireWord(context.Rest, 1, "set gain <value>")), "set gain <value>"),
            "output" => SendSetValueAsync(context, AutomationCommandKind.SetOutputPath, "outputPath", JoinRemaining(context.Rest, 1), "set output <path>"),
            "show-all" => SendSetValueAsync(context, AutomationCommandKind.SetShowAllCaptureOptions, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set show-all on|off")), "set show-all on|off"),
            "mic" or "microphone" => SendSetValueAsync(context, AutomationCommandKind.SetMicrophoneEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set mic on|off")), "set mic on|off"),
            _ => throw new UsageException($"Unknown set command '{subcommand}'.")
        };
    }

    private static Task<int> SendSetValueAsync(
        CommandContext context,
        AutomationCommandKind kind,
        string propertyName,
        object value,
        string usage)
    {
        if (context.Rest.Count < 2)
        {
            throw new UsageException(usage);
        }

        return HandleSimpleCommandAsync(
            context,
            kind,
            new Dictionary<string, object?> { [propertyName] = value },
            includeData: false);
    }

    // Window command family.
    private static async Task<int> HandleWindowAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "window <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "close":
            {
                EnsureArgCount(context.Rest, 1, "window close");
                var armResponse = await context.Transport.SendCommandAsync(
                    Sussudio.Models.AutomationCommandKind.ArmClose,
                    new Dictionary<string, object?> { ["armed"] = true }).ConfigureAwait(false);
                if (!IsSuccess(armResponse))
                {
                    return WriteResponse(armResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
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
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?> { ["action"] = Capitalize(subcommand) },
                    includeData: false).ConfigureAwait(false);
            case "fullscreen":
            case "full-screen":
                EnsureArgCount(context.Rest, 2, "window fullscreen on|off");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.SetFullScreenEnabled,
                    new Dictionary<string, object?> { ["enabled"] = ParseOnOff(RequireWord(context.Rest, 1, "window fullscreen on|off")) },
                    includeData: false).ConfigureAwait(false);
            case "snap":
                EnsureArgCount(context.Rest, 2, "window snap <dir>");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?> { ["action"] = MapSnapAction(RequireWord(context.Rest, 1, "window snap <dir>")) },
                    includeData: false).ConfigureAwait(false);
            case "move":
                EnsureArgCount(context.Rest, 3, "window move <x> <y>");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
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
                    Sussudio.Models.AutomationCommandKind.WindowAction,
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

    private static Task<int> HandleRecordingsAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "recordings open").ToLowerInvariant();
        switch (subcommand)
        {
            case "open":
                EnsureArgCount(context.Rest, 1, "recordings open");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.OpenRecordingsFolder, includeData: false);
            default:
                throw new UsageException($"Unknown recordings command '{subcommand}'.");
        }
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
                Sussudio.Models.AutomationCommandKind.SetStatsSectionVisible,
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
            Sussudio.Models.AutomationCommandKind.SetStatsVisible,
            new Dictionary<string, object?> { ["visible"] = ParseShowHide(subcommand, "stats show|hide") },
            includeData: false);
    }

    private static Task<int> HandleSettingsAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "settings show|hide");
        var visible = ParseShowHide(context.Rest[0], "settings show|hide");
        return HandleSimpleCommandAsync(
            context,
            Sussudio.Models.AutomationCommandKind.SetSettingsVisible,
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }

    private static Task<int> HandleFrameTimeAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "frametime show|hide");
        var visible = ParseShowHide(context.Rest[0], "frametime show|hide");
        return HandleSimpleCommandAsync(
            context,
            Sussudio.Models.AutomationCommandKind.SetFrameTimeOverlayVisible,
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }

    // Flashback command family.
    private static Task<int> HandleFlashbackAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "flashback on|off|timeline|play|pause|go-live|seek|begin-scrub|update-scrub|end-scrub|set-in|set-out|clear-range|export|segments|apply").ToLowerInvariant();
        switch (subcommand)
        {
            case "timeline":
            {
                EnsureArgCount(context.Rest, 2, "flashback timeline show|hide");
                var visible = ParseShowHide(context.Rest[1], "flashback timeline show|hide");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackTimelineVisible,
                    new Dictionary<string, object?> { ["visible"] = visible }, includeData: false);
            }
            case "on":
            case "enable":
                EnsureArgCount(context.Rest, 1, "flashback on|off");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true }, includeData: false);
            case "off":
            case "disable":
                EnsureArgCount(context.Rest, 1, "flashback on|off");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false }, includeData: false);
            case "apply":
            case "restart":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.RestartFlashback, includeData: false);
            case "play":
            case "pause":
            case "go-live":
            case "seek":
            case "begin-scrub":
            case "update-scrub":
            case "end-scrub":
            case "set-in":
            case "set-in-point":
            case "set-out":
            case "set-out-point":
            case "clear-range":
            case "clear-in-out":
                return HandleFlashbackActionAsync(context, subcommand);
            case "export":
                return HandleFlashbackExportAsync(context);
            case "segments":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackGetSegments, includeData: true);
            default:
                throw new UsageException($"Unknown flashback command '{subcommand}'. Expected on, off, play, pause, go-live, seek, begin-scrub, update-scrub, end-scrub, set-in, set-out, clear-range, export, or segments.");
        }
    }

    private static Task<int> HandleFlashbackExportAsync(CommandContext context)
    {
        var useSelectionRange = ConsumeFlag(context.Rest, "--range");
        var force = ConsumeFlag(context.Rest, "--force");
        var seconds = context.Rest.Count >= 2
            ? ParseFlashbackExportSeconds(context.Rest[1])
            : 300;
        var outputPath = context.Rest.Count >= 3
            ? JoinRemaining(context.Rest, 2)
            : $"temp/flashback_export_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackExport,
            new Dictionary<string, object?>
            {
                ["seconds"] = seconds,
                ["outputPath"] = outputPath,
                ["useSelectionRange"] = useSelectionRange,
                ["force"] = force
            }, includeData: true);
    }

    private static Task<int> HandleFlashbackActionAsync(CommandContext context, string subcommand)
    {
        switch (subcommand)
        {
            case "play":
            {
                var playPayload = new Dictionary<string, object?> { ["action"] = "play" };
                if (context.Rest.Count >= 2)
                    playPayload["positionMs"] = ParseFlashbackPositionMs(context.Rest[1]);
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, playPayload, includeData: true);
            }
            case "pause":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "pause" }, includeData: true);
            case "go-live":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "go-live" }, includeData: true);
            case "seek":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "seek",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback seek <ms>"))
                    }, includeData: true);
            case "begin-scrub":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "begin-scrub",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback begin-scrub <ms>"))
                    }, includeData: true);
            case "update-scrub":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "update-scrub",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback update-scrub <ms>"))
                    }, includeData: true);
            case "end-scrub":
            {
                var payload = new Dictionary<string, object?> { ["action"] = "end-scrub" };
                if (context.Rest.Count >= 2)
                    payload["positionMs"] = ParseFlashbackPositionMs(context.Rest[1]);
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, payload, includeData: true);
            }
            case "set-in":
            case "set-in-point":
                EnsureArgCount(context.Rest, 1, "flashback set-in");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "set-in-point" }, includeData: true);
            case "set-out":
            case "set-out-point":
                EnsureArgCount(context.Rest, 1, "flashback set-out");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "set-out-point" }, includeData: true);
            case "clear-range":
            case "clear-in-out":
                EnsureArgCount(context.Rest, 1, "flashback clear-range");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, includeData: true);
            default:
                throw new UsageException($"Unknown flashback action '{subcommand}'.");
        }
    }

    private static double ParseFlashbackPositionMs(string value)
    {
        var parsed = ParseDouble(value);
        if (!double.IsFinite(parsed) || parsed < 0 || parsed > TimeSpan.MaxValue.TotalMilliseconds)
        {
            throw new UsageException("Flashback position must be finite, non-negative, and within TimeSpan range.");
        }

        return parsed;
    }
}
