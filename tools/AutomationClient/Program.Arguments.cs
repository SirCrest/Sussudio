using System.Globalization;

// AutomationClient argument parsing and help text. Keep this near the raw
// client so scripts can audit flags without entering the transport flow.
internal static partial class Program
{
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

}
