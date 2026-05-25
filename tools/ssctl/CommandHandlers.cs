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
}
