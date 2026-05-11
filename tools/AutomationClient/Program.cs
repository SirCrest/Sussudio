using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

// Generic automation-pipe client used by scripts and ad hoc debugging. ssctl
// is the friendlier CLI; this tool stays close to the raw command/payload
// protocol for low-level contract tests.
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Operator Ctrl-C / CI SIGTERM must give the in-flight pipe call a
        // chance to print a cleanup hint instead of leaving the app in a
        // recording state. Handler is idempotent across repeated Ctrl-C.
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, e) =>
        {
            if (cts.IsCancellationRequested)
            {
                // Second Ctrl-C: let the runtime terminate so a hung pipe
                // call does not trap the operator forever.
                return;
            }

            e.Cancel = true;
            try
            {
                Console.Error.WriteLine("AutomationClient: Ctrl-C received; cancelling. If a recording was started, send SetRecordingEnabled enabled=false to clean up.");
            }
            catch
            {
                // Console may already be torn down during shutdown.
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS already disposed; nothing more to do.
            }
        };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            var options = ParseArgs(args);
            if (options.ShowHelp)
            {
                WriteHelp();
                return 0;
            }

            if (string.IsNullOrWhiteSpace(options.Command))
            {
                Console.Error.WriteLine("Missing required --command.");
                WriteHelp();
                return 2;
            }

            var commandValue = AutomationPipeProtocol.ResolveCommand(options.Command);
            var payload = BuildPayload(options);
            var timeoutCommandName = AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)
                ? canonicalCommandName
                : options.Command;
            var responseTimeoutMs = options.ResponseTimeoutMs ??
                AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName);

            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    options.PipeName,
                    options.Command,
                    payload,
                    options.ConnectTimeoutMs,
                    responseTimeoutMs,
                    options.AuthToken)
                .ConfigureAwait(false);
            var responseLine = result.ResponseJson;

            if (options.Pretty)
            {
                using var responseDocument = JsonDocument.Parse(responseLine);
                var pretty = JsonSerializer.Serialize(responseDocument.RootElement, JsonOptions.Pretty);
                Console.WriteLine(pretty);
            }
            else
            {
                Console.WriteLine(responseLine);
            }

            try
            {
                if (result.StateRead && result.Success)
                {
                    return 0;
                }
            }
            catch
            {
                // Keep zero exit behavior only for valid JSON success payloads.
            }

            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static object BuildPayload(Options options)
    {
        if (!string.IsNullOrWhiteSpace(options.PayloadBase64))
        {
            if (options.PayloadKv.Count > 0 ||
                !string.Equals(options.PayloadJson, "{}", StringComparison.Ordinal))
            {
                throw new ArgumentException("Use only one of --payload, --payload-base64, or --payload-kv.");
            }

            var payloadBytes = Convert.FromBase64String(options.PayloadBase64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var decodedPayloadDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
            return decodedPayloadDocument.RootElement.Clone();
        }

        if (options.PayloadKv.Count > 0)
        {
            if (!string.Equals(options.PayloadJson, "{}", StringComparison.Ordinal))
            {
                throw new ArgumentException("Use either --payload or --payload-kv, not both.");
            }

            var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var entry in options.PayloadKv)
            {
                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    throw new ArgumentException($"Invalid --payload-kv entry '{entry}'. Expected key=value.");
                }

                var key = entry.Substring(0, separatorIndex).Trim();
                var rawValue = entry[(separatorIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException($"Invalid --payload-kv entry '{entry}'. Key is empty.");
                }

                payload[key] = ParsePayloadValue(rawValue);
            }

            return payload;
        }

        using var payloadDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(options.PayloadJson) ? "{}" : options.PayloadJson);
        return payloadDocument.RootElement.Clone();
    }

    private static object? ParsePayloadValue(string rawValue)
    {
        if (string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if ((rawValue.StartsWith("\"", StringComparison.Ordinal) && rawValue.EndsWith("\"", StringComparison.Ordinal)) ||
            (rawValue.StartsWith("'", StringComparison.Ordinal) && rawValue.EndsWith("'", StringComparison.Ordinal)))
        {
            rawValue = rawValue[1..^1];
        }

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        if ((rawValue.StartsWith("{", StringComparison.Ordinal) && rawValue.EndsWith("}", StringComparison.Ordinal)) ||
            (rawValue.StartsWith("[", StringComparison.Ordinal) && rawValue.EndsWith("]", StringComparison.Ordinal)))
        {
            using var jsonValueDocument = JsonDocument.Parse(rawValue);
            return jsonValueDocument.RootElement.Clone();
        }

        return rawValue;
    }

    private static Options ParseArgs(string[] args)
    {
        var options = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--command":
                case "-c":
                    options.Command = NextValue(args, ref i, arg);
                    break;
                case "--pipe":
                case "-p":
                    options.PipeName = NextValue(args, ref i, arg);
                    break;
                case "--token":
                case "-t":
                    options.AuthToken = NextValue(args, ref i, arg);
                    break;
                case "--payload":
                    options.PayloadJson = NextValue(args, ref i, arg);
                    break;
                case "--payload-base64":
                    options.PayloadBase64 = NextValue(args, ref i, arg);
                    break;
                case "--payload-kv":
                    options.PayloadKv.Add(NextValue(args, ref i, arg));
                    break;
                case "--connect-timeout-ms":
                    options.ConnectTimeoutMs = ParsePositiveInt(NextValue(args, ref i, arg), arg);
                    break;
                case "--response-timeout-ms":
                    options.ResponseTimeoutMs = ParsePositiveInt(NextValue(args, ref i, arg), arg);
                    break;
                case "--pretty":
                    options.Pretty = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }
        }

        return options;
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid value '{value}' for {option}.");
        }

        return parsed;
    }

    private static void WriteHelp()
    {
        Console.WriteLine("AutomationClient");
        Console.WriteLine("Usage:");
        Console.WriteLine("  AutomationClient --command <name|id> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --command, -c             Required command name or numeric id");
        Console.WriteLine("  --pipe, -p                Pipe name (default: SussudioAutomation)");
        Console.WriteLine("  --token, -t               Auth token when server token is configured");
        Console.WriteLine("  --payload                 JSON object payload (default: {})");
        Console.WriteLine("  --payload-base64          UTF-8 JSON object payload encoded as base64");
        Console.WriteLine("  --payload-kv              Payload entry key=value (repeatable, quote-safe)");
        Console.WriteLine("  --connect-timeout-ms      Pipe connect timeout (default: 5000)");
        Console.WriteLine("  --response-timeout-ms     Response read timeout (default: command-specific)");
        Console.WriteLine("  --pretty                  Pretty-print JSON response");
        Console.WriteLine("  --help, -h, /?            Show help");
    }

    private sealed class Options
    {
        public string? Command { get; set; }
        public string PipeName { get; set; } = AutomationPipeProtocol.DefaultPipeName;
        public string? AuthToken { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public string PayloadBase64 { get; set; } = string.Empty;
        public List<string> PayloadKv { get; } = [];
        public int ConnectTimeoutMs { get; set; } = AutomationPipeProtocol.DefaultConnectTimeoutMs;
        public int? ResponseTimeoutMs { get; set; }
        public bool Pretty { get; set; }
        public bool ShowHelp { get; set; }
    }
}
