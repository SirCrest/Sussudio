using System.Globalization;
using System.Text.Json;
using ElgatoCapture.Tools;

internal static class Program
{
    private const int DefaultConnectTimeoutMs = 5000;
    private const int DefaultNotReadyRetries = 15;
    private const int DefaultNotReadyDelayMs = 1000;

    public static async Task<int> Main(string[] args)
    {
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

            string responseLine;
            for (var attempt = 0; ; attempt++)
            {
                var request = AutomationPipeProtocol.CreateRequestEnvelope(commandValue, authToken: options.AuthToken);
                request["payload"] = payload;

                var requestJson = JsonSerializer.Serialize(request);
                responseLine = await SendAsync(
                    requestJson,
                    options.PipeName,
                    options.ConnectTimeoutMs,
                    options.ResponseTimeoutMs).ConfigureAwait(false);

                if (!TryReadResponseState(responseLine, out var success, out var status, out var retryAfterMs))
                {
                    break;
                }

                if (success)
                {
                    break;
                }

                if (!string.Equals(status, "not_ready", StringComparison.OrdinalIgnoreCase) ||
                    attempt >= DefaultNotReadyRetries)
                {
                    break;
                }

                var delayMs = Math.Clamp(retryAfterMs ?? DefaultNotReadyDelayMs, 100, 30000);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }

            if (options.Pretty)
            {
                using var responseDocument = JsonDocument.Parse(responseLine);
                var pretty = JsonSerializer.Serialize(responseDocument.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine(pretty);
            }
            else
            {
                Console.WriteLine(responseLine);
            }

            try
            {
                if (TryReadResponseState(responseLine, out var success, out _, out _) && success)
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
    }

    private static bool TryReadResponseState(
        string responseJson,
        out bool success,
        out string? status,
        out int? retryAfterMs)
    {
        success = false;
        status = null;
        retryAfterMs = null;

        try
        {
            using var responseDocument = JsonDocument.Parse(responseJson);
            return AutomationResponseState.TryRead(
                responseDocument.RootElement, out success, out status, out retryAfterMs);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> SendAsync(
        string requestJson,
        string pipeName,
        int connectTimeoutMs,
        int responseTimeoutMs)
    {
        return await AutomationPipeClient.SendRequestAsync(
            pipeName,
            requestJson,
            connectTimeoutMs,
            responseTimeoutMs).ConfigureAwait(false);
    }

    private static object BuildPayload(Options options)
    {
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
        Console.WriteLine("  --pipe, -p                Pipe name (default: ElgatoCaptureAutomation)");
        Console.WriteLine("  --token, -t               Auth token when server token is configured");
        Console.WriteLine("  --payload                 JSON object payload (default: {})");
        Console.WriteLine("  --payload-kv              Payload entry key=value (repeatable, quote-safe)");
        Console.WriteLine("  --connect-timeout-ms      Pipe connect timeout (default: 5000)");
        Console.WriteLine("  --response-timeout-ms     Response read timeout (default: 15000)");
        Console.WriteLine("  --pretty                  Pretty-print JSON response");
        Console.WriteLine("  --help, -h, /?            Show help");
    }

    private sealed class Options
    {
        public string? Command { get; set; }
        public string PipeName { get; set; } = AutomationPipeProtocol.DefaultPipeName;
        public string? AuthToken { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public List<string> PayloadKv { get; } = [];
        public int ConnectTimeoutMs { get; set; } = DefaultConnectTimeoutMs;
        public int ResponseTimeoutMs { get; set; } = AutomationPipeProtocol.DefaultResponseTimeoutMs;
        public bool Pretty { get; set; }
        public bool ShowHelp { get; set; }
    }
}
